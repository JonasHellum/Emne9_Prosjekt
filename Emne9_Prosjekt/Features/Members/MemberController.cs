using Emne9_Prosjekt.Features.Members.Interfaces;
using Emne9_Prosjekt.Features.Members.Models;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Authentication;
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

    public MemberController(ILogger<MemberController> logger, 
        IMemberService memberService,
        IConfiguration config)
    {
        _logger = logger;
        _memberService = memberService;
        _clientId = config["Google:ClientId"]!;
    }

    [AllowAnonymous]
    [HttpPost("Register", Name = "RegisterMemberAsync")]
    public async Task<ActionResult<MemberDTO>> RegisterMemberAsync([FromBody] MemberRegistrationDTO registrationDTO)
    {
        _logger.LogInformation("Doing a Post on member registration");
        var member = await _memberService.RegistrationAsync(registrationDTO);
        return member is null
            ? BadRequest("Failed to register new user")
            : Ok(member);
    }
    
    [AllowAnonymous]
    [HttpPost("Login", Name = "Login")]
    public async Task<ActionResult<MemberDTO>> LoginAsync([FromBody] MemberDTO memberDTO)
    {
        Console.WriteLine($"Login attempt for user: {memberDTO.UserName}");
        var member = await _memberService.LoginMemberAsync(memberDTO.UserName, memberDTO.Password);
        if (member == null)
        {
            Console.WriteLine($"Login failed for user: {memberDTO.UserName}");
            return Unauthorized("Username or password is incorrect");
        }
        
        var memberToken = _memberService.MakeToken(member);
        Response.Headers.Add("Authorization", memberToken);

        return Ok(member);
    }
    
    
    [AllowAnonymous]
    [HttpPost("GoogleCallBack", Name = "GoogleCallBack")]
    public async Task<IActionResult> GoogleCallback([FromBody] string credential)
    {
        _logger.LogInformation("Doing a post on GoogleCallBack");
        
        // Validate the credential (ID Token)
        if (string.IsNullOrEmpty(credential))
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
            
            GoogleJsonWebSignature.Payload payload = await GoogleJsonWebSignature.ValidateAsync(credential, settings);
            
            if (payload == null)
            {
                _logger.LogWarning("Failed to validate Google ID Token.");
                return BadRequest("Invalid Google ID Token.");
            }
            
            _logger.LogInformation($"Google ID Token validated successfully for email: {payload.Email}");
            
            
            var member = await _memberService.GoogleLoginAsync(payload);
            var memberToken = _memberService.MakeToken(member!);
            Response.Headers.Add("Authorization", memberToken);
            
            return member is null
                ? BadRequest("Failed to login with Google")
                : Ok(member);
        }
        catch (Exception e)
        {
            _logger.LogError($"An error occurred during Google authentication: {e}");
            return StatusCode(501, "An error occurred during Google login.");

        }
    }
    
    [AllowAnonymous]
    [HttpGet("user-info")]
    public string GetUserInfo()
    {
        var userName = HttpContext.Items["UserName"] as string;

        // Fallback to use claims if Items are gone "poof"
        if (string.IsNullOrEmpty(userName))
        {
            userName = User?.Identity?.Name; 
        }

        if (string.IsNullOrEmpty(userName))
        {
            Console.WriteLine("From user-info controller: No authenticated user found.");
        }

        return userName is null
            ? null
            : userName;
    }
}