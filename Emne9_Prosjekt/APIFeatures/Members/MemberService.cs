using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Linq.Expressions;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Emne9_Prosjekt.Features.Common.Interfaces;
using Emne9_Prosjekt.Features.Members.Interfaces;
using Emne9_Prosjekt.Features.Members.Models;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Http.Connections.Features;
using Microsoft.IdentityModel.Tokens;
using NetTools;

namespace Emne9_Prosjekt.Features.Members;

public class MemberService : IMemberService
{
    private readonly IMemberRepository _memberRepository;
    private readonly ILogger<MemberService> _logger;
    private readonly IMapper<Member, MemberDTO> _memberMapper;
    private readonly IMapper<Member, MemberRegistrationDTO> _registrationMapper;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IConfiguration _config;

    public MemberService(IMemberRepository memberRepository, 
        ILogger<MemberService> logger, 
        IMapper<Member, MemberDTO> memberMapper, 
        IMapper<Member, MemberRegistrationDTO> registrationMapper, 
        IHttpContextAccessor httpContextAccessor,
        IConfiguration config)
    {
        _memberRepository = memberRepository;
        _logger = logger;
        _memberMapper = memberMapper;
        _registrationMapper = registrationMapper;
        _httpContextAccessor = httpContextAccessor;
        _config = config;
    }


    public async Task<bool> DeleteByIdAsync(Guid id)
    {
        throw new NotImplementedException("Will be implemented later");
    }

    /// <summary>
    /// Registers a new member using the provided registration information.
    /// </summary>
    /// <param name="registrationDTO">The registration information for the new member.</param>
    /// <returns>An instance of <see cref="MemberDTO"/> representing the newly registered member, or null if the registration fails.</returns>
    /// <exception cref="DataException">Thrown if there is a failure in adding the new member to the database.</exception>
    public async Task<MemberDTO?> RegistrationAsync(MemberRegistrationDTO registrationDTO)
    {
        var member = _registrationMapper.MapToModel(registrationDTO);
        
        _logger.LogInformation($"Trying to add a new member with username: {member.UserName}");
        member.Created = DateOnly.FromDateTime(DateTime.UtcNow);
        member.Updated = DateOnly.FromDateTime(DateTime.UtcNow);
        member.HashedPassword = BCrypt.Net.BCrypt.HashPassword(registrationDTO.Password);
        
        var addedMember = await _memberRepository.AddAsync(member);
        if (addedMember is null)
        {
            _logger.LogError("Failed to add member.");
            throw new DataException("Failed to add member.");
        }
        
        return _memberMapper.MapToDTO(addedMember);
    }

    /// <summary>
    /// Logs in or registers a member using their Google account information.
    /// </summary>
    /// <param name="googleUser">The Google user's info payload obtained during authentication.</param>
    /// <returns>An instance of <see cref="MemberDTO"/> representing the Google-logged-in member.</returns>
    /// <exception cref="DataException">Thrown when there is a failure in adding the new Google member to the database.</exception>
    public async Task<MemberDTO?> GoogleLoginAsync(GoogleJsonWebSignature.Payload googleUser)
    {
        _logger.LogInformation("Logging in with Google.");
        var email = googleUser.Email;

        var existingMember = await _memberRepository.GetByEmailAsync(email);
        if (existingMember != null)
        {
            _logger.LogInformation($"User with email {email} already exists.");
            GenerateJwtAccessToken(existingMember);
            
            return _memberMapper.MapToDTO(existingMember); 
        }
        
        _logger.LogInformation("Creating a new member from Google account.");
        var googleId = googleUser.Subject;
        var firstName = googleUser.GivenName;
        var lastName = googleUser.FamilyName;
        
        var baseUsername = GenerateBaseUsername(firstName, lastName);
        var uniqueUsername = await UniqueUsernameAsync(baseUsername);

        var newGoogleMember = new Member()
        {
            GoogleId = googleId,
            UserName = uniqueUsername,
            FirstName = firstName,
            LastName = lastName,
            Email = email,
            GoogleUser = true,
            Created = DateOnly.FromDateTime(DateTime.Now),
            Updated = DateOnly.FromDateTime(DateTime.Now),
        };
        
        var newMember = await _memberRepository.AddAsync(newGoogleMember);
        if (newMember is null)
        {
            _logger.LogError("Failed to add member.");
            throw new DataException("Failed to add member.");
        }

        GenerateJwtAccessToken(newMember);
        return _memberMapper.MapToDTO(newMember);
    }

    /// <summary>
    /// Authenticates a member using the provided username and password.
    /// </summary>
    /// <param name="username">The username of the member attempting to log in.</param>
    /// <param name="password">The password associated with the member's username.</param>
    /// <returns>An instance of <see cref="MemberDTO"/> if login is successful; otherwise, null.</returns>
    /// <exception cref="DataException">Thrown if the member does not exist or the password is incorrect.</exception>
    public async Task<MemberDTO?> LoginMemberAsync(string username, string password)
    {
        _logger.LogDebug($"Trying to login member with username: {username}");
        Expression<Func<Member, bool>> expr = member => member.UserName == username; 
        var memb = (await _memberRepository.FindAsync(expr)).FirstOrDefault();
        
        if (memb is null)
        {
            _logger.LogError($"Member with username: {username} does not exist.");
            throw new DataException($"Member with username: {username} does not exist.");
        }

        if (memb.HashedPassword.IsNullOrEmpty())
        {
            _logger.LogWarning("Member does not have a password set.");
            throw new InvalidOperationException("Member does not have a password set.");
        }
        
        if (!BCrypt.Net.BCrypt.Verify(password, memb.HashedPassword))
        {
            _logger.LogWarning("Member has entered incorrect password.");
            throw new DataException("Incorrect password.");
        }
        
        _logger.LogInformation("Member has entered correct password.");
        
        return _memberMapper.MapToDTO(memb);
    }

    /// <summary>
    /// Validates the provided access token and extracts the associated member ID and username.
    /// </summary>
    /// <param name="accessToken">The access token to be validated.</param>
    /// <returns>A tuple containing the member ID and username if validation is successful.</returns>
    public (string memberId, string userName) ValidateAccessToken(string accessToken)
    {
        try
        {
            JwtSecurityTokenHandler tokenHandler = new();
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["JWT:SecretKey"]));
            // var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.Value.Key!));
            
            tokenHandler.ValidateToken(accessToken, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                IssuerSigningKey = key,
                ValidIssuer = _config["JWT:Issuer"],
                ValidAudience = _config["JWT:Audience"],
                // ValidIssuer = _jwtOptions.Value.Issuer,
                // ValidAudience = _jwtOptions.Value.Audience,
                ClockSkew = TimeSpan.Zero
            }, out var validatedToken);
            
            JwtSecurityToken jwtSecurityToken = (JwtSecurityToken)validatedToken;
            
            string? memberId = jwtSecurityToken?.Claims
                .FirstOrDefault(claim => claim.Type == "nameid")?.Value;
            
            string? userName = jwtSecurityToken?.Claims
                .FirstOrDefault(claim => claim.Type == "unique_name")?.Value;
            
            _logger.LogDebug($"[ValidateAccessToken] memberId: {memberId}, userName: {userName}");
            
            return (memberId, userName);
        }
        catch (Exception e)
        {
            _logger.LogError($"An error occurred during token validation: {e}");
            return (null!, null!);
        }
    }

    /// <summary>
    /// Updates an existing member with the provided information.
    /// </summary>
    /// <param name="memberId">The unique identifier of the member to be updated.</param>
    /// <param name="updateDTO">The updated properties for the member.</param>
    /// <returns>An instance of <see cref="MemberDTO"/> representing the updated member, or null if the update fails.</returns>
    /// <exception cref="KeyNotFoundException">Thrown if a member with the specified ID is not found.</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown if the caller is not authorized to update the specified member.</exception>
    /// <exception cref="DataException">Thrown if the update operation fails due to a data persistence issue.</exception>
    public async Task<MemberDTO?> UpdateAsync(Guid memberId, MemberUpdateDTO updateDTO)
    {
        var loggedMember = await GetLoggedInMemberAsync();
        _logger.LogDebug($"Trying to update member by memberId: {memberId} by logged in member memberId: {loggedMember.MemberId}");

        _logger.LogDebug($"Trying to find member to update based on memberId: {memberId}");
        var memberToUpdate = await _memberRepository.GetByIdAsync(memberId);
        if (memberToUpdate is null)
        {
            _logger.LogWarning($"Member with memberId: {memberId} not found.");
            throw new KeyNotFoundException($"Member with memberId: {memberId} not found.");
        }
        
        _logger.LogDebug($"Checking if member with memberId: {loggedMember.MemberId} is " +
                         $"authorized to update member with memberId: {memberToUpdate.MemberId}");
        if (memberToUpdate.MemberId != loggedMember.MemberId)
        {
            _logger.LogWarning($"Member with memberId: {loggedMember.MemberId} is not authorized to update " +
                               $"member with memberId: {memberToUpdate.MemberId}");
            throw new UnauthorizedAccessException($"Member with memberId: {loggedMember.MemberId} is not authorized to update " +
                                                  $"member with memberId: {memberToUpdate.MemberId}");
        }

        if (!string.IsNullOrWhiteSpace(updateDTO.UserName) && updateDTO.UserName != memberToUpdate.UserName)
        {
            // Username shall be changed when leaderboards is fixed with updaing usernames.
            
            // memberToUpdate.UserName = updateDTO.UserName;
            memberToUpdate.UserName = memberToUpdate.UserName;
        }
        
        if (!string.IsNullOrWhiteSpace(updateDTO.FirstName) && updateDTO.FirstName != memberToUpdate.FirstName)
        {
            // memberToUpdate.FirstName = updateDTO.FirstName;
            memberToUpdate.FirstName = memberToUpdate.FirstName;
        }

        if (!string.IsNullOrWhiteSpace(updateDTO.LastName) && updateDTO.LastName != memberToUpdate.LastName)
        {
            // memberToUpdate.LastName = updateDTO.LastName;
            memberToUpdate.LastName = memberToUpdate.LastName;
        }

        if (!default(DateOnly).Equals(updateDTO.BirthYear) && updateDTO.BirthYear != memberToUpdate.BirthYear)
        {
            // memberToUpdate.BirthYear = updateDTO.BirthYear;
            memberToUpdate.BirthYear = memberToUpdate.BirthYear;
        }

        if (!string.IsNullOrWhiteSpace(updateDTO.Email) && updateDTO.Email != memberToUpdate.Email)
        {
            memberToUpdate.Email = updateDTO.Email;
        }
        
        if (!string.IsNullOrWhiteSpace(updateDTO.Password))
        {
            memberToUpdate.HashedPassword = BCrypt.Net.BCrypt.HashPassword(updateDTO.Password);
        }
        memberToUpdate.Updated = DateOnly.FromDateTime(DateTime.UtcNow);
        
        var updatedMember = await _memberRepository.UpdateAsync(memberToUpdate);
        
        if (updatedMember == null)
        {
            _logger.LogWarning($"Did not update member with memberId: {memberId}.");
            throw new DataException($"Did not update member with memberId: {memberId}.");
        }
        
        return _memberMapper.MapToDTO(updatedMember);
    }

    /// <summary>
    /// Retrieves a member by their unique identifier.
    /// </summary>
    /// <param name="memberId">The unique identifier of the member to retrieve.</param>
    /// <returns>An instance of <see cref="MemberDTO"/> representing the member if found, or null if the member does not exist.</returns>
    /// <exception cref="DataException">Thrown if there is an issue accessing the data source.</exception>
    public async Task<MemberDTO?> GetByIdAsync(Guid memberId)
    {
        _logger.LogDebug($"Getting member in service by memberId: {memberId}");
        var member = await _memberRepository.GetByIdAsync(memberId);
        return member is null
            ? null
            : _memberMapper.MapToDTO(member);
    }
    
    /// <summary>
    /// Checks if a username already exists in the system.
    /// </summary>
    /// <param name="username">The username to check for existence.</param>
    /// <returns>A boolean value indicating whether the username exists.</returns>
    public async Task<bool> UserNameExistsAsync(string username)
    {
        if (username.IsNullOrEmpty())
        {
            return false;
        }
        var exists = await _memberRepository.UserNameExistsAsync(username);
        _logger.LogDebug($"UserName exists: {exists}");
        return exists;
    }

    /// <summary>
    /// Checks if an email address exists in the system.
    /// </summary>
    /// <param name="email">The email address to be checked for existence.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a boolean value indicating whether the email exists (true) or not (false).</returns>
    public async Task<bool> EmailExistsAsync(string email)
    {
        if (email.IsNullOrEmpty())
        {
            return false;
        }
        var exists = await _memberRepository.EmailExistsAsync(email);
        _logger.LogDebug($"Email exists: {exists}");
        return exists;
    }
    
    /// <summary>
    /// Generates a JWT Access Token token for the given member.
    /// </summary>
    /// <param name="member">The member for whom the token will be generated.</param>
    /// <returns>A string containing the generated JWT token.</returns>
    public string MakeAccessToken(MemberDTO member)
    {
        _logger.LogDebug("Generating new access token.");
        var memberModel = _memberMapper.MapToModel(member);
        return GenerateJwtAccessToken(memberModel);
    }

    /// <summary>
    /// Generates a new refresh token for authentication purposes.
    /// </summary>
    /// <returns>A string representing the newly generated refresh token.</returns>
    public string MakeRefreshToken()
    {
        _logger.LogDebug("Generating new refresh token.");
        return GenerateJwtRefreshToken();
    }

    /// <summary>
    /// Saves a refresh token associated with a specific member.
    /// </summary>
    /// <param name="memberId">The unique identifier of the member.</param>
    /// <param name="refreshToken">The refresh token to be saved.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="ArgumentException">Thrown if the provided arguments are invalid or null.</exception>
    /// <exception cref="DataException">Thrown if an error occurs while saving the refresh token in the database.</exception>
    public async Task SaveRefreshTokenAsync(Guid memberId, string refreshToken, string ipAddress)
    {
        var token = new MemberRefreshToken
        {
            Token = refreshToken,
            MemberId = memberId,
            IpAddress = ipAddress,
            Created = DateTime.UtcNow,
            Expires = DateTime.UtcNow.AddDays(1),
            Revoked = false
        };

        await _memberRepository.SaveRefreshTokenAsync(token);
        _logger.LogDebug($"Saved refresh token for member with memberId: {memberId}.");
    }

    /// <summary>
    /// Validates the provided refresh token and retrieves the associated member's identifier.
    /// </summary>
    /// <param name="token">The refresh token to be validated.</param>
    /// <returns>The unique identifier of the member associated with the valid token, or <see cref="Guid.Empty"/> if the token is invalid or not found.</returns>
    /// <exception cref="ArgumentNullException">Thrown if the provided token is null or empty.</exception>
    public async Task<Guid> ValidateRefreshTokenAsync(MemberTokenRequest token)
    {
        var storedToken = await _memberRepository.ValidateRefreshTokenAsync(token.RefreshToken);

        if (storedToken == null)
        {
            _logger.LogWarning("Refresh token is invalid or not found.");
            return Guid.Empty;
        }
        
        var storedTokenIpAddress = storedToken.IpAddress;
        var clientTokenIpAddress = token.IpAddress;
        
        Console.WriteLine($"Stored token IP address inside database: {storedTokenIpAddress}");
        Console.WriteLine($"Gotten token IP address from client {clientTokenIpAddress}");

        if (IsIpInSameRange(storedTokenIpAddress, clientTokenIpAddress) == false)
        {
            return Guid.Empty;
        }

        _logger.LogDebug($"Refresh token is valid for member with memberId: {storedToken.MemberId}.");
        return storedToken.MemberId; 
    }

    /// <summary>
    /// Revokes the specified refresh token to terminate its validity.
    /// </summary>
    /// <param name="token">The refresh token to be revoked.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public async Task RevokeRefreshTokenAsync(string token)
    {
        _logger.LogDebug($"Revoking refresh token.");
        await _memberRepository.RevokeRefreshTokenAsync(token);
    }

    /// <summary>
    /// Retrieves a stored refresh token from the database using the provided token string.
    /// </summary>
    /// <param name="token">The refresh token string to be retrieved.</param>
    /// <returns>An instance of <see cref="MemberRefreshToken"/> representing the stored refresh token, or null if it does not exist in the database.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the database operation fails or multiple tokens are found for the given string.</exception>
    public async Task<MemberRefreshToken> GetStoredRefreshTokenAsync(string token)
    {
        _logger.LogDebug($"Retrieving stored refresh token.");
        return await _memberRepository.GetStoredRefreshTokenAsync(token);
    }



    #region Private methods

    /// <summary>
    /// Generates a base username by combining the provided first name and last name.
    /// </summary>
    /// <param name="firstName">The first name of the user.</param>
    /// <param name="lastName">The last name of the user.</param>
    /// <returns>A base username in lowercase format derived from the names, or "googleUser" if both names are empty.</returns>
    private string GenerateBaseUsername(string firstName, string lastName)
    {
        _logger.LogDebug("Generating base username.");
        if (!string.IsNullOrEmpty(firstName) && !string.IsNullOrEmpty(lastName))
        {
            
            return $"{firstName} {lastName}";
        }

        if (!string.IsNullOrEmpty(firstName))
        {
            return firstName;
        }

        return "googleUser"; 
    }

    /// <summary>
    /// Generates a unique username by appending a counter to the provided base username
    /// if the base username already exists in the system.
    /// </summary>
    /// <param name="baseUsername">The initial username to be checked for uniqueness.</param>
    /// <returns>A unique username that does not already exist in the system.</returns>
    private async Task<string> UniqueUsernameAsync(string baseUsername)
    {
        _logger.LogDebug("Generating unique username.");
        var username = baseUsername;
        var counter = 1;

        while (await _memberRepository.UserNameExistsAsync(username))
        {
            username = $"{baseUsername}{counter}";
            counter++;
        }

        return username;
    }

    /// <summary>
    /// Generates a JWT token for the specified member.
    /// </summary>
    /// <param name="member">The member for whom the JWT token is to be generated.</param>
    /// <returns>A string representing the generated JWT token.</returns>
    private string GenerateJwtAccessToken(Member member)
    {
        _logger.LogDebug("Generating JwtAccessToken for member.");
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.NameIdentifier, member.MemberId.ToString()),
            new Claim(ClaimTypes.Name, member.UserName),
        };
        
        // foreach (var role in member.Roles)
        // {
        //     claims.Add(new Claim(ClaimTypes.Role, role.Name!));
        // }
        
        var key = Encoding.UTF8.GetBytes(_config["JWT:SecretKey"]);
        // var key = Encoding.UTF8.GetBytes(_jwtOptions.Value.Key!);
        SecurityTokenDescriptor tokenDescriptor = new()
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(15),
            Issuer = _config["JWT:Issuer"],
            Audience = _config["JWT:Audience"],
            // Issuer = _jwtOptions.Value.Issuer,
            // Audience = _jwtOptions.Value.Audience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), 
                SecurityAlgorithms.HmacSha256)
        };
        
        JwtSecurityTokenHandler tokenHandler = new();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        Console.WriteLine(tokenHandler.WriteToken(token));
        return tokenHandler.WriteToken(token);
    }

    /// <summary>
    /// Generates a new JWT refresh token.
    /// </summary>
    /// <returns>A base64-encoded string representing the refresh token.</returns>
    private string GenerateJwtRefreshToken()
    {
        _logger.LogDebug("Generating JwtRefreshToken.");
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
    }

    /// <summary>
    /// Retrieves the currently logged-in member based on the information stored in the HTTP context.
    /// </summary>
    /// <returns>An instance of <see cref="Member"/> representing the logged-in member, or throws an exception if no member is logged in.</returns>
    /// <exception cref="UnauthorizedAccessException">Thrown when no member is logged in or the member cannot be found.</exception>
    private async Task<Member?> GetLoggedInMemberAsync()
    {
        var loggedInMemberId = _httpContextAccessor.HttpContext?.Items["MemberId"] as string;
        _logger.LogDebug("Logged in member ID: {LoggedInMemberId}", loggedInMemberId);
    
        if (string.IsNullOrEmpty(loggedInMemberId))
        {
            _logger.LogWarning("No logged in member.");
            throw new UnauthorizedAccessException("No logged in member.");
        }
        
        var loggedInMember = (await _memberRepository.FindAsync(m => m.MemberId.ToString() == loggedInMemberId)).FirstOrDefault();
        if (loggedInMember == null)
        {
            _logger.LogWarning("Logged in member not found: {LoggedInMemberId}", loggedInMemberId);
            throw new UnauthorizedAccessException("Logged in member ID not found.");
        }
        
        return loggedInMember;
    }

    /// <summary>
    /// Determines whether two IP addresses belong to the same network range based on a default CIDR prefix.
    /// For IPv4 addresses, a /24 subnet is used; for IPv6, a /64 subnet is used.
    /// </summary>
    /// <param name="ipAddress1">The first IP address in string format.</param>
    /// <param name="ipAddress2">The second IP address in string format.</param>
    /// <returns>True if both IP addresses are within the same subnet based on the default CIDR; otherwise, false.</returns>
    /// <exception cref="DataException">Thrown when either <paramref name="ipAddress1"/> or <paramref name="ipAddress2"/> is not a valid IP address.</exception>
    private bool IsIpInSameRange(string ipAddress1, string ipAddress2)
    {
        // Validate and parse the first IP
        if (!IPAddress.TryParse(ipAddress1, out var ip1))
        {
            _logger.LogError($"Invalid IP address: {ipAddress1}");
            throw new DataException($"Invalid IP address: {ipAddress1}");
        }

        // Validate and parse the second IP
        if (!IPAddress.TryParse(ipAddress2, out var ip2))
        {
            _logger.LogError($"Invalid IP address: {ipAddress2}");
            throw new DataException($"Invalid IP address: {ipAddress2}");
        }

        // Determine default CIDR
        int cidr = ip1.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork ? 24 : 64;

        // Create an IP range from the first IP and CIDR
        var ipRange = IPAddressRange.Parse($"{ip1}/{cidr}");

        // Check if the second IP address is part of the range
        return ipRange.Contains(ip2);
    }
    
    #endregion

}