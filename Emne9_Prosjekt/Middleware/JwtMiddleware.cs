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

    /// <summary>
    /// Processes an HTTP request by validating an optional JWT access token,
    /// extracting the user information, and setting it in the current request context.
    /// Calls the next middleware in the pipeline upon completion.
    /// </summary>
    /// <param name="context">The current HTTP request context.</param>
    /// <param name="next">A delegate to invoke the next middleware in the pipeline.</param>
    /// <returns>A task that represents the asynchronous operation of the middleware.</returns>
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        string? token = context.Request
            .Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
    
        if (token is not null)
        {
            var (memberId, userName) = _memberService.ValidateAccessToken(token);
            _logger.LogInformation($"User: {memberId}" 
                                   //$"Roles: {roles}"
                                   );
            context.Items["MemberId"] = memberId;
            context.Items["UserName"] = userName;
            // context.Items["Roles"] = roles;
            Console.WriteLine($"Member ID from middleware: {context.Items["MemberId"]}");
            Console.WriteLine($"Username from middleware: {context.Items["UserName"]}");
        }
        
        await next(context);
    }
    
    // public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    // {
    //     // Debug: Check if the user is authenticated and claims exist
    //     if (context.User?.Identity?.IsAuthenticated == true)
    //     {
    //         string memberId = context.User.FindFirst("nameid")?.Value;
    //         string userName = context.User.FindFirst("unique_name")?.Value;
    //
    //         // Log debug information
    //         _logger.LogInformation($"JWT Middleware: MemberId={memberId}, UserName={userName}");
    //
    //         // Attach data to HttpContext.Items
    //         context.Items["MemberId"] = memberId;
    //         context.Items["UserName"] = userName;
    //     }
    //     else
    //     {
    //         _logger.LogWarning("JWT Middleware: User is not authenticated.");
    //     }
    //
    //     await next(context);
    // }

}