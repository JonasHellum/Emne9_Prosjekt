using System.Security.Claims;
using Emne9_Prosjekt.Components.Pages.Interfaces;
using Emne9_Prosjekt.Features.Members.Models;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;

namespace Emne9_Prosjekt.Components.Pages.Services;

public class CustomAuthenticationStateProvider : AuthenticationStateProvider, ICustomAuthenticationStateProvider
{
    private AuthenticationState _cachedAuthState;
    private DateTime? _lastTokenRefresh;
    private readonly ILogger<CustomAuthenticationStateProvider> _logger;
    private readonly IAuthStateService _authStateService;
    
    private readonly IHttpContextAccessor _httpContextAccessor;

    private readonly IJSRuntime _jsRuntime;
    private readonly HttpClient _httpClient;
    

    public CustomAuthenticationStateProvider(IJSRuntime jsRuntime, 
        HttpClient httpClient,
        ILogger<CustomAuthenticationStateProvider> logger,
        IHttpContextAccessor httpContextAccessor,
        IAuthStateService authStateService)
    {
        _jsRuntime = jsRuntime;
        _httpClient = httpClient;
        _logger = logger;
        _cachedAuthState = new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        
        _httpContextAccessor = httpContextAccessor;
        _authStateService = authStateService;

    }

    public Task<ClaimsPrincipal> GetLoggedInUserAsync()
    {
        // If the cached state is already authenticated, return the User directly
        if (_cachedAuthState?.User.Identity?.IsAuthenticated == true && _lastTokenRefresh.HasValue && 
            DateTime.UtcNow.Subtract(_lastTokenRefresh.Value).TotalMinutes < 12)
        {
            _authStateService.SetUserName(_cachedAuthState.User.Identity.Name);
            return Task.FromResult(_cachedAuthState.User);
        }

        // Return an empty (unauthenticated) user
        return Task.FromResult(new ClaimsPrincipal(new ClaimsIdentity()));
    }


    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        // Retrieve the refresh token from localStorage
        var refreshToken = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "RefreshToken");

        // If no refresh token, the user is not authenticated
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            _logger.LogDebug("No refresh token in local storage.");
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }

        try
        {
            _logger.LogDebug($"Refresh token: {refreshToken}");
            // Attempt to refresh the access token using the refresh token
            var response = await _httpClient.PostAsJsonAsync("http://localhost:80/api/members/RefreshToken", refreshToken);
            

            if (response.IsSuccessStatusCode)
            {
                // Deserialize new tokens
                var tokens = await response.Content.ReadFromJsonAsync<MemberTokenResponse>();

                if (tokens != null)
                {
                    // Update tokens in localStorage
                    if (!string.IsNullOrWhiteSpace(tokens.RefreshToken))
                    {
                        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "RefreshToken", tokens.RefreshToken);
                    }

                    // Update the Authorization header for the HTTP client
                    _httpClient.DefaultRequestHeaders.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokens.AccessToken);

                    // Fetch user claims
                    var userNameTask = _httpClient.GetStringAsync("http://localhost:80/api/members/Username-info");
                    var memberIdTask = _httpClient.GetStringAsync("http://localhost:80/api/members/MemberId-info");
                    
                    await Task.WhenAll(userNameTask, memberIdTask);
                    var userName = userNameTask.Result;
                    var memberId = memberIdTask.Result;
                    
                    if (!string.IsNullOrWhiteSpace(userName) && !string.IsNullOrWhiteSpace(memberId))
                    {
                        var claims = new[]
                        {
                            new Claim(ClaimTypes.Name, userName),
                            new Claim(ClaimTypes.NameIdentifier, memberId)
                        };
                        var identity = new ClaimsIdentity(claims, "Bearer");
                        var principal = new ClaimsPrincipal(identity);
                        
                        _cachedAuthState = new AuthenticationState(principal);
                        _lastTokenRefresh = DateTime.UtcNow;
                        NotifyAuthenticationStateChanged(Task.FromResult(_cachedAuthState));
                        _authStateService.SetUserName(_cachedAuthState.User.Identity.Name);
                        return _cachedAuthState;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error during token refresh: {ex.Message}");
        }
        
        await MarkUserAsLoggedOut();
        return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
    }
    
    // Store the authenticated state and token
    public async Task MarkUserAsAuthenticated(string accessToken, string refreshToken)
    {
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "RefreshToken", refreshToken);
        _httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        // Fetch user claims
        var userNameTask = _httpClient.GetStringAsync("http://localhost:80/api/members/Username-info");
        var memberIdTask = _httpClient.GetStringAsync("http://localhost:80/api/members/MemberId-info");
                    
        await Task.WhenAll(userNameTask, memberIdTask);
        var userName = userNameTask.Result;
        var memberId = memberIdTask.Result;
                    
        if (!string.IsNullOrWhiteSpace(userName) && !string.IsNullOrWhiteSpace(memberId))
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.Name, userName),
                new Claim(ClaimTypes.NameIdentifier, memberId)
            };
            var identity = new ClaimsIdentity(claims, "Bearer");
            var principal = new ClaimsPrincipal(identity);
                        
            _cachedAuthState = new AuthenticationState(principal);
            _lastTokenRefresh = DateTime.UtcNow;
        }
        _authStateService.SetUserName(_cachedAuthState.User.Identity.Name);
        NotifyAuthenticationStateChanged(Task.FromResult(_cachedAuthState));
        Console.WriteLine("User authenticated and state updated.");
    }

    // Clear tokens and unauthenticate the user
    public async Task MarkUserAsLoggedOut()
    {
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "RefreshToken");
        _httpClient.DefaultRequestHeaders.Authorization = null;
        _authStateService.ClearUserName();

        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()))));
        Console.WriteLine("User logged out and state cleared.");
    }

}