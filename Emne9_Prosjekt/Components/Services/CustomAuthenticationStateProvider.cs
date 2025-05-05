using System.Security.Claims;
using Emne9_Prosjekt.Components.Pages.Interfaces;
using Emne9_Prosjekt.Features.Members.Models;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.IdentityModel.Tokens;
using Microsoft.JSInterop;

namespace Emne9_Prosjekt.Components.Pages.Services;

public class CustomAuthenticationStateProvider : AuthenticationStateProvider, ICustomAuthenticationStateProvider
{
    private AuthenticationState _cachedAuthState;
    private DateTime? _lastTokenRefresh;
    private readonly ILogger<CustomAuthenticationStateProvider> _logger;
    private readonly IAuthStateService _authStateService;
    
    
    private readonly MemberTokenRequest _memberTokenRequest;
    
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
        _memberTokenRequest = new MemberTokenRequest();

    }

    /// <summary>
    /// Set the IpAddress from a .razor component, send it to CustomAtuehnticationStateProvider to be used in future calls to API about refreshtoken.
    /// </summary>
    /// <param name="ipAddress">The IP address.</param>
    public void SetIpAddress(string ipAddress)
    {
        _memberTokenRequest.IpAddress = ipAddress;
        _memberTokenRequest.RefreshToken = string.Empty;
    }

    /// <summary>
    /// Retrieves the currently logged-in user's claims principal.
    /// Checks the cached authentication state to ensure the user is authenticated and the token has not exceeded a specified expiration time.
    /// If authenticated, updates the user's name in the authentication state service.
    /// If not authenticated or if the necessary conditions are not met, returns an unauthenticated claims principal.
    /// </summary>
    /// <returns>A task representing the asynchronous operation, containing the current <see cref="ClaimsPrincipal"/> representing the logged-in user or an empty claims principal if no valid user is found.</returns>
    public Task<ClaimsPrincipal> GetLoggedInUserAsync()
    {
        if (_cachedAuthState?.User.Identity?.IsAuthenticated == true && _lastTokenRefresh.HasValue && 
            DateTime.UtcNow.Subtract(_lastTokenRefresh.Value).TotalMinutes < 12)
        {
            _logger.LogDebug("Setting the user name in the authentication state service and returns the user's claims principal.");
            _authStateService.SetUserName(_cachedAuthState.User.Identity.Name);
            return Task.FromResult(_cachedAuthState.User);
        }

        _logger.LogDebug("No logged in user found");
        return Task.FromResult(new ClaimsPrincipal(new ClaimsIdentity()));
    }

    /// <summary>
    /// Retrieves the current authentication state for the application.
    /// Checks local storage for an encrypted refresh token, decryping it and refreshes the access token with refresh token.
    /// If the refresh token is about to expire then we get a new one, encrypts it and saves it to local storage.
    /// Gets the username and memberId from APi, creates a ClaimsPrincipal representing the authenticated user.
    /// If no valid token exists, updates the authentication state to represent an anonymous user.
    /// </summary>
    /// <returns>A task representing the asynchronous operation, containing the current <see cref="AuthenticationState"/>.</returns>
    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        if (_memberTokenRequest.IpAddress.IsNullOrEmpty())
        {
            _logger.LogDebug("No Ip address found.");
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }        
        var encryptedRefreshTokenFromStorage = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "RefreshToken");
        
        if (string.IsNullOrWhiteSpace(encryptedRefreshTokenFromStorage))
        {
            _logger.LogDebug("No refresh token in local storage.");
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }
        
        try
        {
            var refreshToken = Decrypt(encryptedRefreshTokenFromStorage);
            _memberTokenRequest.RefreshToken = refreshToken;
            _logger.LogDebug($"Refreshing the access token using Refresh token: {refreshToken}");
            var response = await _httpClient.PostAsJsonAsync("http://localhost:80/api/members/RefreshToken", _memberTokenRequest);
            

            if (response.IsSuccessStatusCode)
            {
                var tokens = await response.Content.ReadFromJsonAsync<MemberTokenResponse>();

                if (tokens != null)
                {
                    if (!string.IsNullOrWhiteSpace(tokens.RefreshToken))
                    {
                        _logger.LogDebug("Setting encrypted refresh token in local storage.");
                        var encryptedRefreshToken = Encrypt(tokens.RefreshToken);
                        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "RefreshToken", encryptedRefreshToken);
                    }
                    
                    _logger.LogDebug("Setting access token into the Authorization header.");
                    _httpClient.DefaultRequestHeaders.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokens.AccessToken);

                    _logger.LogDebug("getting username and memberId from api"); 
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
                        
                        _logger.LogDebug($"Set user to authenticated with username: {userName} and memberId: {memberId} in a ClaimsPrincipal.");
                        _cachedAuthState = new AuthenticationState(principal);
                        _lastTokenRefresh = DateTime.UtcNow;
                        NotifyAuthenticationStateChanged(Task.FromResult(_cachedAuthState));
                        _authStateService.SetUserName(_cachedAuthState.User.Identity!.Name!);
                        _logger.LogDebug("User authenticated and state updated.");
                        return _cachedAuthState;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning($"Error during token refresh: {ex.Message}");
        }
        
        await MarkUserAsLoggedOutAsync();
        return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
    }
    
    /// <summary>
    /// Marks the user as authenticated and updates the authentication state.
    /// Stores the provided access and refresh tokens, sets up the user claims, and updates the authentication state.
    /// </summary>
    /// <param name="accessToken">The access token issued for the user.</param>
    /// <param name="refreshToken">The refresh token issued for the user.</param>
    public async Task MarkUserAsAuthenticated(string accessToken, string refreshToken)
    {
        var encryptedRefreshToken = Encrypt(refreshToken);
        _logger.LogDebug("Setting encrypted refresh token in local storage and access token into the Authorization header.");
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "RefreshToken", encryptedRefreshToken);
        _httpClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        
        _logger.LogDebug("getting username and memberId from api");        
        var userNameTask = _httpClient.GetStringAsync("/api/members/Username-info");
        var memberIdTask = _httpClient.GetStringAsync("/api/members/MemberId-info");
                    
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
            
            _logger.LogDebug($"Set user to authenticated with username: {userName} and memberId: {memberId} in a ClaimsPrincipal.");
            _cachedAuthState = new AuthenticationState(principal);
            _lastTokenRefresh = DateTime.UtcNow;
        }
        _authStateService.SetUserName(_cachedAuthState.User.Identity.Name);
        NotifyAuthenticationStateChanged(Task.FromResult(_cachedAuthState));
        _logger.LogInformation("User authenticated and state updated.");
    }
    
    /// <summary>
    /// Marks the user as logged out and clears the authentication state.
    /// Removes the stored refresh token, clears the authorization header,
    /// resets the user data, and notifies the application of the updated authentication state.
    /// </summary>
    /// <returns>A completed task once the user's logout process is finalized.</returns>
    public async Task MarkUserAsLoggedOutAsync()
    {
        _logger.LogDebug("Clearing refresh token from local storage and access token from Authorization header.");
        var refreshToken = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "RefreshToken");
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "RefreshToken");
        _httpClient.DefaultRequestHeaders.Authorization = null;
        _authStateService.ClearUserName();
        
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()))));
        _logger.LogInformation("User logged out and state cleared.");
    }

    public async Task<string> GetRefreshTokenAsync()
    {
        _logger.LogDebug("Getting refresh token from local storage.");
        var refreshToken = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "RefreshToken");
        var decryptedRefreshToken = Decrypt(refreshToken);
        
        return decryptedRefreshToken;
    }

    /// <summary>
    /// Encrypts the provided refresh token using AES encryption and returns the encrypted data as a Base64 string.
    /// The method appends the generated Initialization Vector (IV) to the encrypted data and uses a Key.
    /// </summary>
    /// <param name="refreshToken">The plaintext refresh token to be encrypted.</param>
    /// <returns>A Base64-encoded string containing the encrypted refresh token combined with the IV.</returns>
    private string Encrypt(string refreshToken)
    {
        _logger.LogDebug("Encrypting refresh token.");
        using var aes = System.Security.Cryptography.Aes.Create();
        aes.Key = System.Text.Encoding.UTF8.GetBytes(_config.GetValue<string>("AppSettings:JWTKey"));
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
        using var ms = new MemoryStream();

        // Prepend the IV to the encrypted data
        ms.Write(aes.IV, 0, aes.IV.Length);

        using (var cs = new System.Security.Cryptography.CryptoStream(ms, encryptor, System.Security.Cryptography.CryptoStreamMode.Write))
        using (var writer = new StreamWriter(cs))
        {
            writer.Write(refreshToken);
        }

        _logger.LogDebug("Refresh token encrypted.");
        // Convert the MemoryStream (ciphertext + prepended IV) to a Base64 string
        return Convert.ToBase64String(ms.ToArray());
    }

    /// <summary>
    /// Decrypts the provided cipher text using AES encryption, retrieving the original plain text.
    /// </summary>
    /// <param name="cipherText">The encrypted text to be decrypted.</param>
    /// <returns>The decrypted plain text representation of the input cipher text.</returns>
    private string Decrypt(string cipherText)
    {
        _logger.LogDebug("Decrypting refresh token.");
        using var aes = System.Security.Cryptography.Aes.Create();
        aes.Key = System.Text.Encoding.UTF8.GetBytes(_config.GetValue<string>("AppSettings:JWTKey"));

        // Convert the cipherText back to a byte array
        var buffer = Convert.FromBase64String(cipherText);

        // Get the IV from the first 16 bytes
        byte[] iv = new byte[16];
        Array.Copy(buffer, 0, iv, 0, iv.Length);
        aes.IV = iv;

        // Get the actual encrypted data (everything after the IV)
        byte[] cipherBytes = new byte[buffer.Length - iv.Length];
        Array.Copy(buffer, iv.Length, cipherBytes, 0, cipherBytes.Length);

        using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
        using var ms = new MemoryStream(cipherBytes);
        using var cs = new System.Security.Cryptography.CryptoStream(ms, decryptor, System.Security.Cryptography.CryptoStreamMode.Read);
        using var reader = new StreamReader(cs);

        _logger.LogDebug("Refresh token decrypted.");
        return reader.ReadToEnd();
    }
}