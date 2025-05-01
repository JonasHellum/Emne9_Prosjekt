using BitzArt.Blazor.Cookies;
using Emne9_Prosjekt.Components.Pages.Interfaces;
using SameSiteMode = Microsoft.AspNetCore.Http.SameSiteMode;

namespace Emne9_Prosjekt.Components.Pages.Services;

public class CookieService : ICookieSettingService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CookieService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }


    private void AppendApiCookieToResponse(HttpContext context, string cookieHeader)
    {
        // Extract the token name and value
        var tokenPair = cookieHeader.Split(';')[0]; // "AuthTokenCOMON=jwt_token_value"
        var tokenName = tokenPair.Split('=')[0];    // AuthTokenCOMON
        var tokenValue = tokenPair.Split('=')[1];   // The JWT value

        // Define cookie options
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,                // Prevent access through JavaScript
            SameSite = SameSiteMode.Lax,    // Same-origin requests
            Secure = false,                 // Set to true in production for HTTPS
            Expires = DateTime.UtcNow.AddHours(2)  // Cookie expiration
        };

        // Add cookie to the response
        context.Response.Cookies.Append(tokenName, tokenValue, cookieOptions);

        Console.WriteLine($"Cookie appended to response: {tokenName} = {tokenValue}");
    }

    public async Task ProcessApiResponse(HttpResponseMessage response)
    {
        if (response.Headers.TryGetValues("Set-Cookie", out var cookies))
        {
            foreach (var cookieHeader in cookies)
            {
                var context = _httpContextAccessor.HttpContext;
                if (context == null)
                {
                    throw new InvalidOperationException("HttpContext is not available.");
                }

                // Append API cookies to the server response
                AppendApiCookieToResponse(context, cookieHeader);
            }
        }
        else
        {
            Console.WriteLine("No cookies were returned by the API.");
        }
    }



}