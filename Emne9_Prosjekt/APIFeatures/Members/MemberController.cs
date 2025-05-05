using System.Data;
using Emne9_Prosjekt.Features.Members.Interfaces;
using Emne9_Prosjekt.Features.Members.Models;
using Emne9_Prosjekt.Validators.Interfaces;
using Emne9_Prosjekt.Validators.MemberValidators;
using FluentValidation;
using Google.Apis.Auth;
using Google.Apis.Auth.OAuth2.Requests;
using Microsoft.AspNetCore.Authentication;
using Emne9_Prosjekt.Features.Members.Models;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace Emne9_Prosjekt.Features.Members;



[ApiController]
[Route("api/members")]
public class MemberController : ControllerBase
{
    private readonly ILogger<MemberController> _logger;
    private readonly IMemberService _memberService;
    private readonly string _clientId;
    private readonly IAsyncMemberRegistrationValidator _asyncRegisterValidator;
    private readonly IAsyncMemberUpdateValidator _asyncUpdateValidator;

    public MemberController(ILogger<MemberController> logger, 
        IMemberService memberService,
        IConfiguration config,
        IAsyncMemberRegistrationValidator asyncValidator,
        IAsyncMemberUpdateValidator asyncUpdateValidator)
    {
        _logger = logger;
        _memberService = memberService;
        _clientId = config["Google:ClientId"]!;
        _asyncRegisterValidator = asyncValidator;
        _asyncUpdateValidator = asyncUpdateValidator;
    }

    /// <summary>
    /// Handles user registration by creating a new Member entity in the database.
    /// </summary>
    /// <param name="registrationDTO">The data transfer object containing the user's registration details.</param>
    /// <returns>A MemberDTO object containing the newly created user's details if successful; otherwise, returns a BadRequest status.</returns>
    [AllowAnonymous]
    [HttpPost("Register", Name = "RegisterMemberAsync")]
    public async Task<ActionResult<MemberDTO>> RegisterMemberAsync([FromBody] MemberRegistrationDTO registrationDTO)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }
        
        _logger.LogInformation("Doing a Post on member registration");
        var asyncValidationResult = await _asyncRegisterValidator.ValidateAsync(registrationDTO);
        if (!asyncValidationResult.IsValid)
        {
            return BadRequest(new
            {
                Message = "Validation failed",
                Errors = asyncValidationResult.Errors.Select(e => new { e.PropertyName, e.ErrorMessage, e.AttemptedValue })

            });
        }
        var member = await _memberService.RegistrationAsync(registrationDTO);
        return member is null
            ? BadRequest("Failed to register new user")
            : Ok(member);
    }

    /// <summary>
    /// Handles user login by validating credentials and generating a JWToken upon successful authentication.
    /// </summary>
    /// <param name="memberDTO">The data transfer object containing the username and password of the user attempting to log in.</param>
    /// <returns>A MemberDTO object containing the authenticated user's details if successful; otherwise, returns an Unauthorized status.</returns>
    [AllowAnonymous]
    [HttpPost("Login", Name = "Login")]
    public async Task<ActionResult<MemberTokenResponse>> LoginAsync([FromBody] MemberDTO memberDTO)
    {
        _logger.LogInformation($"Login attempt for user: {memberDTO.UserName}");
        var member = await _memberService.LoginMemberAsync(memberDTO.UserName, memberDTO.Password);
        if (member == null)
        {
            _logger.LogWarning($"Login failed for user: {memberDTO.UserName}");
            return Unauthorized("Username or password is incorrect");
        }
        
        var memberAccessToken = _memberService.MakeAccessToken(member);
        var memberRefreshToken = _memberService.MakeRefreshToken();
        if (memberDTO.IpAddress.IsNullOrEmpty())
        {
            _logger.LogWarning("IP Address is null or empty.");
            return BadRequest("IP Address is null or empty.");
        }
        await _memberService.SaveRefreshTokenAsync(member.MemberId, memberRefreshToken, memberDTO.IpAddress);

        return Ok(new MemberTokenResponse
        {
            AccessToken = memberAccessToken,
            RefreshToken = memberRefreshToken
        });
    }

    /// <summary>
    /// Refreshes the user's access token by validating the provided refresh token
    /// and issuing a new access token. If necessary, generates a new refresh token as well.
    /// </summary>
    /// <param name="request">The refresh token provided by the client for validation.</param>
    /// <returns>An HTTP response containing a new access token, and optionally a new refresh token, if successful;
    /// otherwise, returns an Unauthorized status for invalid or expired tokens.</returns>
    [AllowAnonymous]
    [HttpPost("RefreshToken", Name = "RefreshToken")]
    public async Task<ActionResult> RefreshTokenAsync([FromBody] MemberTokenRequest request)
    {
        _logger.LogInformation("Doing a post on RefreshToken");
        var memberId = await _memberService.ValidateRefreshTokenAsync(request);
        if (memberId == Guid.Empty)
        {
            _logger.LogWarning("Invalid or expired refresh token");
            return Unauthorized("Invalid or expired refresh token");
        }
        
        var member = await _memberService.GetByIdAsync(memberId);
        if (member == null)
        {
            _logger.LogWarning("User not found");
            return Unauthorized("User not found");
        }
        
        _logger.LogDebug("Generating a new access token.");
        var newAccessToken = _memberService.MakeAccessToken(member);
        var storedToken = await _memberService.GetStoredRefreshTokenAsync(request.RefreshToken);
        
        if (storedToken.Expires.Subtract(DateTime.UtcNow).TotalHours <= 1)
        {
            _logger.LogDebug("Generating a new refresh token because the old one is about to expire.");
            // Generate a new refresh token and save it
            var newRefreshToken = _memberService.MakeRefreshToken();
            await _memberService.SaveRefreshTokenAsync(memberId, newRefreshToken, request.IpAddress);

            return Ok(new
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken 
            });
        }
        
        return Ok(new
        {
            AccessToken = newAccessToken
        });
    }

    /// <summary>
    /// Logs out a user by revoking their refresh token.
    /// </summary>
    /// <param name="request">The request containing the refresh token to be revoked.</param>
    /// <returns>A response indicating the success of the logout operation.</returns>
    [Authorize]
    [HttpPost("Logout")]
    public async Task<IActionResult> LogoutAsync([FromBody] MemberTokenRequest request)
    {
        _logger.LogInformation("Doing a post on Logout");
        await _memberService.RevokeRefreshTokenAsync(request.RefreshToken);
        return Ok("Logged out successfully");
    }
            
    /// <summary>
    /// Google Call Back, redirects to this after logging in through Google
    /// so it saves all the userinfo, makes a jwtoken etc.
    /// </summary>
    /// <param name="credential"></param>
    /// <returns>MemberDTO</returns>
    [AllowAnonymous]
    [HttpPost("GoogleCallBack", Name = "GoogleCallBack")]
    public async Task<ActionResult<MemberTokenResponse>> GoogleCallback([FromBody] GoogleCallbackRequest credential)
    {
        _logger.LogInformation("Doing a post on GoogleCallBack");
        
        // Validate the credential (ID Token)
        if (string.IsNullOrEmpty(credential.IdToken))
        {
            _logger.LogWarning("Credential (ID Token) is null or empty.");
            return BadRequest("Credential is required.");
        }

        try
        {
            GoogleJsonWebSignature.ValidationSettings settings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = new List<string>() { _clientId }
            };
            
            GoogleJsonWebSignature.Payload payload = await GoogleJsonWebSignature.ValidateAsync(credential.IdToken, settings);
            
            if (payload == null)
            {
                _logger.LogWarning("Failed to validate Google ID Token.");
                return BadRequest("Invalid Google ID Token.");
            }
            
            _logger.LogInformation($"Google ID Token validated successfully for email: {payload.Email}");
            
            
            var member = await _memberService.GoogleLoginAsync(payload);
            if (member == null)
            {
                _logger.LogWarning("Failed to login with Google");
                return BadRequest("Failed to login with Google");
            }
            
            var memberAccessToken = _memberService.MakeAccessToken(member);
            var memberRefreshToken = _memberService.MakeRefreshToken();
        
            await _memberService.SaveRefreshTokenAsync(member.MemberId, memberRefreshToken, credential.IpAddress);

            return Ok(new MemberTokenResponse
            {
                AccessToken = memberAccessToken,
                RefreshToken = memberRefreshToken
            });
        }
        catch (Exception e)
        {
            _logger.LogError($"An error occurred during Google authentication: {e}");
            return StatusCode(501, "An error occurred during Google login.");

        }
    }

    /// <summary>
    /// Updates an existing member's information in the system using the provided details.
    /// </summary>
    /// <param name="memberId">The unique identifier of the member to be updated.</param>
    /// <param name="memberDTO">The data transfer object containing the updated member details.</param>
    /// <returns>A MemberDTO object with the updated member details if the operation is successful; otherwise, returns a BadRequest status.</returns>
    [Authorize]
    [HttpPut("update/{memberId}", Name = "UpdateMemberAsync")]
    public async Task<ActionResult<MemberDTO>> UpdateMemberAsync(Guid memberId, [FromBody] MemberUpdateDTO memberDTO)
    {
        _logger.LogInformation($"Doing a Put on member with id: {memberId}");
        var asyncValidationResult = await _asyncUpdateValidator.ValidateAsync(memberDTO);
        if (!asyncValidationResult.IsValid)
        {
            return BadRequest(new
            {
                Message = "Validation failed",
                Errors = asyncValidationResult.Errors.Select(e => new { e.PropertyName, e.ErrorMessage, e.AttemptedValue })

            });
        }
        
        var updatedMember = await _memberService.UpdateAsync(memberId, memberDTO);
        
        return updatedMember is null
            ? BadRequest("Failed to update member")
            : Ok(updatedMember);
    }

    /// <summary>
    /// Retrieves a member's details by their unique identifier.
    /// </summary>
    /// <param name="memberId">The unique identifier of the member.</param>
    /// <returns>The MemberDTO object containing the member's details if found; otherwise, returns a BadRequest status.</returns>
    [Authorize]
    [HttpGet("get/{memberId}", Name = "GetMemberByIdAsync")]
    public async Task<ActionResult<MemberDTO>> GetMemberByIdAsync(Guid memberId)
    {
        _logger.LogInformation($"Doing a Get on member with id: {memberId}");
        var memberDto = await _memberService.GetByIdAsync(memberId);
        return memberDto is null
            ? BadRequest("Failed to get member")
            : Ok(memberDto);
    }

    /// <summary>
    /// Retrieves the username of the currently authenticated user.
    /// </summary>
    /// <returns>The username of the authenticated user if available; otherwise, null if no user is authenticated.</returns>
    [AllowAnonymous]
    [HttpGet("Username-info")]
    public string GetUserNameFromJWT()
    {
        _logger.LogDebug($"[Controller] User.Identity.Name: {User?.Identity?.Name}");
        _logger.LogDebug($"[Controller] IsAuthenticated: {User?.Identity?.IsAuthenticated}");

        
        var userName = HttpContext.Items["UserName"] as string;
        _logger.LogDebug($"[Controller] ITEMS: {userName}");

        // Fallback to use claims if Items are gone "poof"
        if (string.IsNullOrEmpty(userName))
        {
            userName = User?.Identity?.Name; 
        }

        if (string.IsNullOrEmpty(userName))
        {
            _logger.LogWarning("[Controller] No authenticated user found.");
        }

        return userName is null
            ? null
            : userName;
    }

    /// <summary>
    /// Retrieves the MemberId associated with the current authenticated user.
    /// </summary>
    /// <returns>The MemberId as a string if the user is authenticated; otherwise, null if no user is authenticated.</returns>
    [AllowAnonymous]
    [HttpGet("MemberId-info")]
    public string GetMemberIdFromJWT()
    {
        _logger.LogDebug($"[Controller] User.Claims: {string.Join(", ", User?.Claims?.Select(c => c.Type + "=" + c.Value))}");

        
        var memberId = HttpContext.Items["MemberId"] as string;
        _logger.LogDebug($"[Controller] ITEMS: {memberId}");

        // Fallback to use claims if Items are gone "poof"
        if (string.IsNullOrEmpty(memberId))
        {
            memberId = User?.Claims.FirstOrDefault(claim => claim.Type == "nameid")?.Value;
        }

        if (string.IsNullOrEmpty(memberId))
        {
            _logger.LogWarning("[Controller] No authenticated user found.");
        }

        return memberId is null
            ? null
            : memberId;
    }

    /// <summary>
    /// Checks if the provided username already exists in the system.
    /// </summary>
    /// <param name="username">The username to check for existence.</param>
    /// <returns>A boolean value indicating whether the username exists in the system.</returns>
    [AllowAnonymous]
    [HttpGet("username/{username}")]
    public async Task<IActionResult> UserNameExistsAsync(string username)
    {
        _logger.LogInformation("Doing a post on UserNameExistsAsync");
        var exists = await _memberService.UserNameExistsAsync(username);
        return Ok(exists);
    }

    /// <summary>
    /// Checks if the given email address is already registered in the system.
    /// </summary>
    /// <param name="email">The email address to verify for existence.</param>
    /// <returns>A boolean value indicating whether the email address exists in the system.</returns>
    [AllowAnonymous]
    [HttpGet("email/{email}")]
    public async Task<IActionResult> EmailExistsAsync(string email)
    {
        _logger.LogInformation("Doing a post on EmailExistsAsync");
        var exists = await _memberService.EmailExistsAsync(email);
        return Ok(exists);
    }
}