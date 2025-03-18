using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Text;
using Emne9_Prosjekt.Features.Common.Interfaces;
using Emne9_Prosjekt.Features.Members.Interfaces;
using Emne9_Prosjekt.Features.Members.Models;
using Google.Apis.Auth;
using Microsoft.IdentityModel.Tokens;

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

    public async Task<MemberDTO?> RegistrationAsync(MemberRegistrationDTO registrationDTO)
    {
        var member = _registrationMapper.MapToModel(registrationDTO);
        
        _logger.LogInformation($"trying to add a new member with id: {member.MemberId}");
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
    
    public async Task<MemberDTO?> GoogleLoginAsync(GoogleJsonWebSignature.Payload googleUser)
    {

        var googleId = googleUser.Subject;
        var firstName = googleUser.GivenName;
        var lastName = googleUser.FamilyName;
        var email = googleUser.Email;

        var baseUsername = GenerateBaseUsername(firstName, lastName);
        var uniqueUsername = await UniqueUsernameAsync(baseUsername);

        var existingMember = await _memberRepository.GetByEmailAsync(email);
        if (existingMember != null)
        {
            _logger.LogInformation($"User with email {email} already exists.");
            GenerateJwtToken(existingMember);
            
            return _memberMapper.MapToDTO(existingMember); 
        }

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

        GenerateJwtToken(newMember);
        return _memberMapper.MapToDTO(newMember);
    }
    
    public async Task<MemberDTO?> LoginMemberAsync(string username, string password)
    {
        _logger.LogInformation($"Trying to login member with username: {username}");
        Expression<Func<Member, bool>> expr = member => member.UserName == username; 
        var memb = (await _memberRepository.FindAsync(expr)).FirstOrDefault();
        
        if (memb is null)
        {
            _logger.LogWarning($"Member with username: {username} does not exist.");
            throw new DataException($"Member with username: {username} does not exist.");
        }
        
        if (!BCrypt.Net.BCrypt.Verify(password, memb.HashedPassword))
        {
            _logger.LogWarning("Member has entered incorrect password.");
            throw new DataException("Incorrect password.");
        }
        
        _logger.LogInformation("Member has entered correct password.");
        
        return _memberMapper.MapToDTO(memb);
    }

    public string ValidateAccessToken(string accessToken)
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

            // IEnumerable<string>? roles = jwtSecurityToken?.Claims
            //     .Where(x => x.Type == "role")
            //     .Select(x => x.Value);

            return memberId;
        }
        catch (Exception e)
        {
            // Legg til logging !!!
            return null!;
        }
    }

    public string MakeToken(MemberDTO member)
    {
        var memberModel = _memberMapper.MapToModel(member);
        return GenerateJwtToken(memberModel);
    }


    #region Private methods
    private string GenerateBaseUsername(string firstName, string lastName)
    {
        if (!string.IsNullOrEmpty(firstName) && !string.IsNullOrEmpty(lastName))
        {
            return $"{firstName.ToLower()}.{lastName.ToLower()}";
        }

        if (!string.IsNullOrEmpty(firstName))
        {
            return firstName.ToLower();
        }

        return "googleUser"; 
    }
    
    private async Task<string> UniqueUsernameAsync(string baseUsername)
    {
        var username = baseUsername;
        var counter = 1;

        while (await _memberRepository.UsernameExistsAsync(username))
        {
            username = $"{baseUsername}{counter}";
            counter++;
        }

        return username;
    }

    private string GenerateJwtToken(Member member)
    {
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
            Expires = DateTime.UtcNow.AddHours(1),
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
    #endregion

}