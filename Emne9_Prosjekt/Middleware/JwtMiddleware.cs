using Emne9_Prosjekt.Features.Members.Interfaces;

namespace Emne9_Prosjekt.Middleware;

public class JwtMiddleware : IMiddleware
{
    private readonly ILogger<JwtMiddleware> _logger;
    private readonly IMemberService _memberService;

    public JwtMiddleware(ILogger<JwtMiddleware> logger, IMemberService memberService)
    {
        _logger = logger;
        _memberService = memberService;
    }

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        string? token = context.Request
            .Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();

        if (token is not null)
        {
            var userId = _memberService.ValidateAccessToken(token);
            _logger.LogInformation($"User: {userId}" 
                                   //$"Roles: {roles}"
                                   );
            context.Items["UserId"] = userId;
            // context.Items["Roles"] = roles;
        }
        
        await next(context);
    }
}