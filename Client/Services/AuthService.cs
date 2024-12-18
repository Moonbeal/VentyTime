using System.Net.Http.Json;
using System.Security.Claims;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Logging;
using VentyTime.Shared.Models;
using VentyTime.Shared.Models.Auth;
using Microsoft.AspNetCore.Components;

namespace VentyTime.Client.Services
{
    public class AuthService : IAuthService
    {
        private readonly HttpClient _httpClient;
        private readonly CustomAuthStateProvider _authStateProvider;
        private readonly Blazored.LocalStorage.ILocalStorageService _localStorage;
        private readonly ILogger<AuthService> _logger;
        private readonly NavigationManager _navigationManager;

        public AuthService(
            IHttpClientFactory clientFactory,
            AuthenticationStateProvider authStateProvider,
            Blazored.LocalStorage.ILocalStorageService localStorage,
            ILogger<AuthService> logger,
            NavigationManager navigationManager)
        {
            _httpClient = clientFactory.CreateClient("VentyTime.ServerAPI.NoAuth");
            _authStateProvider = (CustomAuthStateProvider)authStateProvider;
            _localStorage = localStorage ?? throw new ArgumentNullException(nameof(localStorage));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _navigationManager = navigationManager ?? throw new ArgumentNullException(nameof(navigationManager));
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            try
            {
                _logger.LogInformation("Attempting to login user: {Email}", request.Email);
                var response = await _httpClient.PostAsJsonAsync("api/auth/login", request);
                var result = await response.Content.ReadFromJsonAsync<AuthResponse>();

                if (response.IsSuccessStatusCode && result?.Token != null)
                {
                    await _localStorage.SetItemAsync("authToken", result.Token);
                    await _authStateProvider.NotifyUserAuthentication(result.Token);
                    _navigationManager.NavigateTo("/");
                    _logger.LogInformation("User {Email} logged in successfully.", request.Email);
                    return result;
                }

                _logger.LogWarning("Login failed for user: {Email}", request.Email);
                return result ?? new AuthResponse { Success = false, Message = "An error occurred during login." };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for user: {Email}", request.Email);
                return new AuthResponse { Success = false, Message = "An error occurred during login." };
            }
        }

        public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
        {
            try
            {
                _logger.LogInformation("Attempting to register user: {Email}", request.Email);
                var response = await _httpClient.PostAsJsonAsync("api/auth/register", request);
                var result = await response.Content.ReadFromJsonAsync<AuthResponse>();

                if (response.IsSuccessStatusCode && result?.Token != null)
                {
                    await _localStorage.SetItemAsync("authToken", result.Token);
                    await _authStateProvider.NotifyUserAuthentication(result.Token);
                    _navigationManager.NavigateTo("/");
                    _logger.LogInformation("User {Email} registered successfully.", request.Email);
                    return result;
                }

                _logger.LogWarning("Registration failed for user: {Email}", request.Email);
                return result ?? new AuthResponse { Success = false, Message = "An error occurred during registration." };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during registration for user: {Email}", request.Email);
                return new AuthResponse { Success = false, Message = "An error occurred during registration." };
            }
        }

        public async Task<bool> LogoutAsync()
        {
            try
            {
                await _authStateProvider.NotifyUserLogout();
                _navigationManager.NavigateTo("/");
                _logger.LogInformation("User logged out successfully.");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout");
                return false;
            }
        }

        public async Task<bool> IsAuthenticated()
        {
            try
            {
                var token = await _localStorage.GetItemAsync<string>("authToken");
                return !string.IsNullOrEmpty(token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking authentication status");
                return false;
            }
        }

        public async Task<User> GetCurrentUserAsync()
        {
            try
            {
                var token = await _localStorage.GetItemAsync<string>("authToken");
                if (string.IsNullOrEmpty(token))
                    return null;

                var response = await _httpClient.GetAsync("api/auth/current-user");
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<User>();
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current user");
                return null;
            }
        }

        public async Task<UserRole> GetUserRole()
        {
            try
            {
                var authState = await _authStateProvider.GetAuthenticationStateAsync();
                var user = authState.User;

                if (user.Identity?.IsAuthenticated != true)
                    return UserRole.None;

                if (user.IsInRole("Admin"))
                    return UserRole.Admin;
                
                return UserRole.User;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user role");
                return UserRole.None;
            }
        }

        public async Task<bool> UpdateProfileAsync(UpdateProfileRequest request)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync("api/auth/update-profile", request);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating profile");
                return false;
            }
        }

        public async Task<bool> ChangePasswordAsync(ChangePasswordRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/auth/change-password", request);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password");
                return false;
            }
        }

        public async Task<bool> RequestPasswordResetAsync(string email)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/auth/request-password-reset", new { Email = email });
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error requesting password reset");
                return false;
            }
        }

        public async Task<bool> ResetPasswordAsync(ResetPasswordRequest request)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/auth/reset-password", request);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting password");
                return false;
            }
        }

        public async Task<string> GetUserId()
        {
            try
            {
                var authState = await _authStateProvider.GetAuthenticationStateAsync();
                return authState.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user ID");
                return null;
            }
        }

        public async Task<string> GetUsername()
        {
            try
            {
                var authState = await _authStateProvider.GetAuthenticationStateAsync();
                return authState.User.FindFirst(ClaimTypes.Name)?.Value;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting username");
                return null;
            }
        }

        public async Task<string> GetToken()
        {
            try
            {
                return await _localStorage.GetItemAsync<string>("authToken");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting token");
                return null;
            }
        }
    }
}
