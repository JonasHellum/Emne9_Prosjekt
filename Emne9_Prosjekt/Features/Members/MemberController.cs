using Emne9_Prosjekt.Features.Members.Interfaces;
using Emne9_Prosjekt.Features.Members.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;

namespace Emne9_Prosjekt.Features.Members;



[ApiController]
[Route("api/members")]
public class MemberController : ControllerBase
{
    private readonly ILogger<MemberController> _logger;
    private readonly IMemberService _memberService;

    public MemberController(ILogger<MemberController> logger, 
        IMemberService memberService)
    {
        _logger = logger;
        _memberService = memberService;
    }

    [HttpPost("Register", Name = "RegisterMemberAsync")]
    public async Task<ActionResult<MemberDTO>> RegisterMemberAsync([FromBody] MemberRegistrationDTO registrationDTO)
    {
        _logger.LogInformation("Doing a Post on member registration");
        var member = await _memberService.RegistrationAsync(registrationDTO);
        return member is null
            ? BadRequest("Failed to register new user")
            : Ok(member);
    }

    [HttpGet("Login-Google", Name = "LoginWithGoogle")]
    public IActionResult LoginWithGoogle()
    {
        var properties = new AuthenticationProperties { RedirectUri = "/api/members/GoogleCallback" };
        return Challenge(properties, GoogleDefaults.AuthenticationScheme);
    }
    
    [HttpGet("GoogleCallback", Name = "GoogleCallback")]
    public async Task<IActionResult> GoogleCallback()
    {
        _logger.LogInformation("Doing a get on GoogleCallback");
        var authResult = await HttpContext.AuthenticateAsync();
        if (!authResult.Succeeded)
        {
            _logger.LogWarning("Google Authentication failed");
            return BadRequest("Google Authentication failed");
        }

        try
        {
            var googleUser = authResult.Principal;
            var user = await _memberService.GoogleLoginAsync(googleUser);
            return user is null
                ? BadRequest("Failed to login with Google")
                : Ok(user);
        }
        catch (Exception e)
        {
            _logger.LogError($"An error occurred during Google authentication: {e.Message}");
            return StatusCode(500, "An error occurred during Google login.");

        }
    }
}