using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using VentyTime.Shared.Models;

namespace VentyTime.Client.Services
{
    public class AuthService : IAuthService
    {
        private readonly AuthenticationStateProvider _authStateProvider;

        public AuthService(AuthenticationStateProvider authStateProvider)
        {
            _authStateProvider = authStateProvider;
        }

        public async Task<string> GetUserId()
        {
            var authState = await _authStateProvider.GetAuthenticationStateAsync();
            return authState.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
        }

        public async Task<string> GetUsername()
        {
            var authState = await _authStateProvider.GetAuthenticationStateAsync();
            return authState.User.FindFirst(ClaimTypes.Name)?.Value ?? string.Empty;
        }

        public async Task<bool> IsAuthenticated()
        {
            var authState = await _authStateProvider.GetAuthenticationStateAsync();
            return authState.User.Identity?.IsAuthenticated ?? false;
        }

        public async Task<string> GetToken()
        {
            var authState = await _authStateProvider.GetAuthenticationStateAsync();
            return authState.User.FindFirst("access_token")?.Value ?? string.Empty;
        }
    }
}
