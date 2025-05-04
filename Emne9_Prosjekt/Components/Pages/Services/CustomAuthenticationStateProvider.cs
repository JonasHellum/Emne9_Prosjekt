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
    private readonly IConfiguration _config;
    

    public CustomAuthenticationStateProvider(IJSRuntime jsRuntime, 
        HttpClient httpClient,
        ILogger<CustomAuthenticationStateProvider> logger,
        IHttpContextAccessor httpContextAccessor,
        IAuthStateService authStateService,
        IConfiguration config)
    {
        _config = config;
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
        var encryptedRefreshTokenFromStorage = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "RefreshToken");

        // If no refresh token, the user is not authenticated
        if (string.IsNullOrWhiteSpace(encryptedRefreshTokenFromStorage))
        {
            _logger.LogDebug("No refresh token in local storage.");
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }
        
        try
        {
            var refreshToken = Decrypt(encryptedRefreshTokenFromStorage);
            _logger.LogDebug($"Refresh token: {refreshToken}");
            // Attempt to refresh the access token using the refresh token
            var response = await _httpClient.PostAsJsonAsync("http://localhost:80/api/members/RefreshToken", refreshToken);
            

            if (response.IsSuccessStatusCode)
            {
                // Deserialize new tokens
                var tokens = await response.Content.ReadFromJsonAsync<MemberTokenResponse>();

                if (tokens != null)
                {
                    // Update token in localStorage
                    if (!string.IsNullOrWhiteSpace(tokens.RefreshToken))
                    {
                        var encryptedRefreshToken = Encrypt(tokens.RefreshToken);
                        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "RefreshToken", encryptedRefreshToken);
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
                        _authStateService.SetUserName(_cachedAuthState.User.Identity!.Name!);
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
        var encryptedRefreshToken = Encrypt(refreshToken);
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "RefreshToken", encryptedRefreshToken);
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

    private string Encrypt(string refreshToken)
    {
        using var aes = System.Security.Cryptography.Aes.Create();
        aes.Key = System.Text.Encoding.UTF8.GetBytes(_config.GetValue<string>("AppSettings:JWTKey"));;
        aes.IV = new byte[16]; // Initialization vector for AES is typically 16 bytes of zero for simplicity
    
        using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
        using var ms = new MemoryStream();
        using (var cs = new System.Security.Cryptography.CryptoStream(ms, encryptor, System.Security.Cryptography.CryptoStreamMode.Write))
        using (var writer = new StreamWriter(cs))
        {
            writer.Write(refreshToken);
        }
    
        return Convert.ToBase64String(ms.ToArray());
    }
    
    private string Decrypt(string cipherText)
    {
        using var aes = System.Security.Cryptography.Aes.Create();
        aes.Key = System.Text.Encoding.UTF8.GetBytes(_config.GetValue<string>("AppSettings:JWTKey"));
        aes.IV = new byte[16]; // Must match the IV used during encryption

        using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
        var buffer = Convert.FromBase64String(cipherText);
        using var ms = new MemoryStream(buffer);
        using var cs = new System.Security.Cryptography.CryptoStream(ms, decryptor, System.Security.Cryptography.CryptoStreamMode.Read);
        using var reader = new StreamReader(cs);
    
        return reader.ReadToEnd();
    }


}