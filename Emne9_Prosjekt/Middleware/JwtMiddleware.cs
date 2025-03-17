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
            var memberId = _memberService.ValidateAccessToken(token);
            _logger.LogInformation($"User: {memberId}" 
                                   //$"Roles: {roles}"
                                   );
            context.Items["MemberId"] = memberId;
            // context.Items["Roles"] = roles;
            Console.WriteLine(context.Items["MemberId"]);
        }
        
        await next(context);
    }
}