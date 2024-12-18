using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Authorization;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Logging;
using Blazored.LocalStorage;

namespace VentyTime.Client.Services;

public class CustomAuthStateProvider : AuthenticationStateProvider
{
    private readonly Blazored.LocalStorage.ILocalStorageService _localStorage;
    private readonly JwtSecurityTokenHandler _tokenHandler;
    private readonly ILogger<CustomAuthStateProvider> _logger;

    public CustomAuthStateProvider(
        Blazored.LocalStorage.ILocalStorageService localStorage,
        ILogger<CustomAuthStateProvider> logger)
    {
        _localStorage = localStorage ?? throw new ArgumentNullException(nameof(localStorage));
        _tokenHandler = new JwtSecurityTokenHandler();
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            var token = await _localStorage.GetItemAsync<string>("authToken");
            if (string.IsNullOrEmpty(token))
            {
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
            }

            var tokenContent = _tokenHandler.ReadJwtToken(token);
            var claims = tokenContent.Claims.ToList();

            // Add the token itself as a claim
            claims.Add(new Claim("access_token", token));

            var identity = new ClaimsIdentity(claims, "jwt");
            var user = new ClaimsPrincipal(identity);
            return new AuthenticationState(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting authentication state");
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }
    }

    public async Task NotifyUserAuthentication(string token)
    {
        try
        {
            if (string.IsNullOrEmpty(token))
                throw new ArgumentNullException(nameof(token));

            await _localStorage.SetItemAsync("authToken", token);
            var authenticatedUser = new ClaimsPrincipal(new ClaimsIdentity(GetClaimsFromToken(token), "jwt"));
            var authState = Task.FromResult(new AuthenticationState(authenticatedUser));
            NotifyAuthenticationStateChanged(authState);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during user authentication notification");
            throw;
        }
    }

    public async Task NotifyUserLogout()
    {
        try
        {
            await _localStorage.RemoveItemAsync("authToken");
            var anonymousUser = new ClaimsPrincipal(new ClaimsIdentity());
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(anonymousUser)));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during user logout notification");
            throw;
        }
    }

    private IEnumerable<Claim> GetClaimsFromToken(string token)
    {
        var tokenContent = _tokenHandler.ReadJwtToken(token);
        var claims = tokenContent.Claims.ToList();
        claims.Add(new Claim("access_token", token));
        return claims;
    }
}
