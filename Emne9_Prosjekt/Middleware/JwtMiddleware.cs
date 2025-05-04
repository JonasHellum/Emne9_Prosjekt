using System.Security.Claims;
using Emne9_Prosjekt.Features.Members.Interfaces;

namespace Emne9_Prosjekt.Middleware;

public class JwtMiddleware : IMiddleware
{
    private readonly IMemberService _memberService;
    private readonly ILogger<JwtMiddleware> _logger;

    public JwtMiddleware(IMemberService memberService, ILogger<JwtMiddleware> logger)
    {
        _memberService = memberService;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            string? token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
            _logger.LogDebug($"Token from Authorization header: {token}");
            

            if (!string.IsNullOrEmpty(token))
            {
                // Validate the token using the service
                var (memberId, userName) = _memberService.ValidateAccessToken(token);

                if (!string.IsNullOrEmpty(memberId) && !string.IsNullOrEmpty(userName))
                {
                    // Add to HttpContext.Items
                    context.Items["MemberId"] = memberId;
                    context.Items["UserName"] = userName;

                    _logger.LogDebug($"Token validated. HttpContext.Items: MemberId: {memberId}, Username: {userName}");
                    
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, memberId),
                        new Claim(ClaimTypes.Name, userName)
                    };
                    var identity = new ClaimsIdentity(claims, "Bearer");
                    context.User = new ClaimsPrincipal(identity);
                    _logger.LogDebug($"ClaimsPrincipal created. MemberId: {memberId}, Username: {userName}");
                }
                else
                {
                    _logger.LogWarning("Invalid token or missing claims.");
                } 
            }
            else
            {
                _logger.LogWarning("No token found in headers or cookies.");
            }
        }
        await next(context);
    }
}