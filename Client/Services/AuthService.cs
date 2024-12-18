using System.Net.Http.Json;
using System.Security.Claims;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Logging;
using VentyTime.Shared.Models;

namespace VentyTime.Client.Services
{
    public class AuthService : IAuthService
    {
        private readonly HttpClient _httpClient;
        private readonly AuthenticationStateProvider _authStateProvider;
        private readonly Blazored.LocalStorage.ILocalStorageService _localStorage;
        private readonly ILogger<AuthService> _logger;

        public AuthService(
            HttpClient httpClient, 
            AuthenticationStateProvider authStateProvider, 
            Blazored.LocalStorage.ILocalStorageService localStorage, 
            ILogger<AuthService> logger)
        {
            _httpClient = httpClient;
            _authStateProvider = authStateProvider;
            _localStorage = localStorage;
            _logger = logger;
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            try
            {
                _logger.LogInformation("Attempting to login user: {Email}", request.Email);
                var response = await _httpClient.PostAsJsonAsync("api/auth/login", request);
                if (response.IsSuccessStatusCode)
                {
                    var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
                    if (authResponse != null && authResponse.Success)
                    {
                        await _localStorage.SetItemAsync("authToken", authResponse.Token);
                        ((CustomAuthStateProvider)_authStateProvider).NotifyUserAuthentication(authResponse.Token);
                        _logger.LogInformation("User {Email} logged in successfully.", request.Email);
                    }
                    return authResponse ?? new AuthResponse(false, "Failed to deserialize response", string.Empty);
                }
                _logger.LogWarning("Login failed for user: {Email}", request.Email);
                return new AuthResponse(false, "Login failed", string.Empty);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for user: {Email}", request.Email);
                return new AuthResponse(false, $"Error during login: {ex.Message}", string.Empty);
            }
        }

        public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
        {
            try
            {
                _logger.LogInformation("Attempting to register user: {Email}", request.Email);
                var response = await _httpClient.PostAsJsonAsync("api/auth/register", request);
                if (response.IsSuccessStatusCode)
                {
                    var authResponse = await response.Content.ReadFromJsonAsync<AuthResponse>();
                    if (authResponse != null && authResponse.Success)
                    {
                        await _localStorage.SetItemAsync("authToken", authResponse.Token);
                        ((CustomAuthStateProvider)_authStateProvider).NotifyUserAuthentication(authResponse.Token);
                        _logger.LogInformation("User {Email} registered successfully.", request.Email);
                    }
                    return authResponse ?? new AuthResponse(false, "Failed to deserialize response", string.Empty);
                }
                _logger.LogWarning("Registration failed for user: {Email}", request.Email);
                return new AuthResponse(false, "Registration failed", string.Empty);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during registration for user: {Email}", request.Email);
                return new AuthResponse(false, $"Error during registration: {ex.Message}", string.Empty);
            }
        }

        public async Task LogoutAsync()
        {
            await _localStorage.RemoveItemAsync("authToken");
            ((CustomAuthStateProvider)_authStateProvider).NotifyUserLogout();
            await _httpClient.PostAsync("api/auth/logout", null);
        }

        public async Task<User> GetCurrentUserAsync()
        {
            try 
            {
                var authState = await _authStateProvider.GetAuthenticationStateAsync();
                if (authState.User.Identity?.IsAuthenticated != true)
                    return new User();

                var userId = authState.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return new User();

                var response = await _httpClient.GetAsync($"api/users/{userId}");
                if (response.IsSuccessStatusCode)
                {
                    var user = await response.Content.ReadFromJsonAsync<User>();
                    return user ?? new User();
                }
                return new User();
            }
            catch (Exception)
            {
                return new User();
            }
        }

        public async Task<bool> IsAuthenticated()
        {
            var authState = await _authStateProvider.GetAuthenticationStateAsync();
            return authState.User.Identity?.IsAuthenticated ?? false;
        }

        public async Task<UserRole> GetUserRole()
        {
            var authState = await _authStateProvider.GetAuthenticationStateAsync();
            var roleString = authState.User.FindFirst(ClaimTypes.Role)?.Value;
            return roleString != null ? Enum.Parse<UserRole>(roleString) : UserRole.None;
        }

        public async Task<bool> UpdateProfileAsync(UpdateProfileRequest request)
        {
            var response = await _httpClient.PutAsJsonAsync("api/users/profile", request);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> ChangePasswordAsync(ChangePasswordRequest request)
        {
            var response = await _httpClient.PostAsJsonAsync("api/users/change-password", request);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> RequestPasswordResetAsync(string email)
        {
            var response = await _httpClient.PostAsJsonAsync("api/users/reset-password-request", new { Email = email });
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> ResetPasswordAsync(ResetPasswordRequest request)
        {
            var response = await _httpClient.PostAsJsonAsync("api/users/reset-password", request);
            return response.IsSuccessStatusCode;
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

        public async Task<string> GetToken()
        {
            var authState = await _authStateProvider.GetAuthenticationStateAsync();
            return authState.User.FindFirst("access_token")?.Value ?? string.Empty;
        }
    }
}
