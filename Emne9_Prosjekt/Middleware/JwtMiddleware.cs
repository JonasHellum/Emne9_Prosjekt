using System.Security.Claims;
using Emne9_Prosjekt.Features.Members.Interfaces;

namespace Emne9_Prosjekt.Middleware;

public class JwtMiddleware : IMiddleware
{
    private readonly IMemberService _tokenValidationService;
    private readonly ILogger<JwtMiddleware> _logger;

    public JwtMiddleware(IMemberService memberService, ILogger<JwtMiddleware> logger)
    {
        _tokenValidationService = memberService;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            string? token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
            Console.WriteLine($"Token PLEASE FROM MIDDLEWARE BEFORE COOKIE CHECK: {token}");

            // If no Authorization header exists, fallback to cookies
            if (string.IsNullOrEmpty(token))
            {
                token = context.Request.Cookies["AuthTokenCOMON"];
                Console.WriteLine($"Token PLEASE FROM MIDDLEWARE WHEN COOKIE CHECK: {token}");
            }

            if (!string.IsNullOrEmpty(token))
            {
                // if (await IsTokenBlacklisted(token))
                // {
                //     _logger.LogWarning("Token is blacklisted. Removing authentication context.");
                //     context.User = new ClaimsPrincipal(); // Clear the user claims
                //     await next(context);
                //     return;
                // }

            
                // Validate the token using the service
                var (memberId, userName) = _tokenValidationService.ValidateAccessToken(token);

                if (!string.IsNullOrEmpty(memberId) && !string.IsNullOrEmpty(userName))
                {
                    // Add to HttpContext.Items
                    context.Items["MemberId"] = memberId;
                    context.Items["UserName"] = userName;

                    _logger.LogInformation($"Token validated. MemberId: {memberId}, Username: {userName}");

                    // Create a ClaimsPrincipal and attach it to HttpContext.User
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, memberId),
                        new Claim(ClaimTypes.Name, userName)
                    };
                    var identity = new ClaimsIdentity(claims, "Bearer");
                    context.User = new ClaimsPrincipal(identity);
                    
                    Console.WriteLine($"HttpContext.User initialized with Name: {context.User.Identity?.Name}");

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

// private async Task<bool> IsTokenBlacklisted(string token)
// {
//     // Implement a mechanism to check if the token has been invalidated or blacklisted.
//     // For example, calling a database or in-memory cache (e.g., Redis or MemoryCache).
//     // This ensures that even valid tokens cannot be reused after logout.
//     return await Task.FromResult(false); // Replace with actual logic
// }
