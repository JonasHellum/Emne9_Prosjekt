using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace Emne9_Prosjekt.Components.Pages.Interfaces;

public interface ICustomAuthenticationStateProvider
{
    Task<ClaimsPrincipal> GetLoggedInUserAsync();
    Task<AuthenticationState> GetAuthenticationStateAsync();
    Task MarkUserAsAuthenticated(string accessToken, string refreshToken);
    Task MarkUserAsLoggedOut();
    void SetIpAddress(string ipAddress);

}