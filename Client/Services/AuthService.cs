using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Logging;
using MudBlazor;
using VentyTime.Client.Auth;
using VentyTime.Shared.Models;
using VentyTime.Shared.Models.Auth;

namespace VentyTime.Client.Services
{
    public class AuthService : IAuthService
    {
        private readonly HttpClient _httpClient;
        private readonly HttpClient _noAuthClient;
        private readonly CustomAuthStateProvider _authStateProvider;
        private readonly Blazored.LocalStorage.ILocalStorageService _localStorage;
        private readonly ILogger<AuthService> _logger;
        private readonly NavigationManager _navigationManager;

        public AuthService(
            IHttpClientFactory httpClientFactory,
            AuthenticationStateProvider authStateProvider,
            Blazored.LocalStorage.ILocalStorageService localStorage,
            ILogger<AuthService> logger,
            NavigationManager navigationManager)
        {
            _httpClient = httpClientFactory.CreateClient("VentyTime.ServerAPI");
            _noAuthClient = httpClientFactory.CreateClient("VentyTime.ServerAPI.NoAuth");
            _authStateProvider = (CustomAuthStateProvider)authStateProvider 
                ?? throw new ArgumentNullException(nameof(authStateProvider));
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
                    await _authStateProvider.NotifyUserAuthenticationAsync(result.Token);
                    _navigationManager.NavigateTo("/");
                    _logger.LogInformation("User {Email} logged in successfully.", request.Email);
                    return result;
                }

                _logger.LogWarning("Login failed for user {Email}. Status code: {StatusCode}", 
                    request.Email, response.StatusCode);
                return new AuthResponse { Success = false, Message = "Login failed.", User = new UserDto() };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while logging in user {Email}", request.Email);
                return new AuthResponse { Success = false, Message = "An error occurred during login.", User = new UserDto() };
            }
        }

        public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
        {
            try
            {
                _logger.LogInformation("Attempting to register user: {Email}", request.Email);
                
                var response = await _noAuthClient.PostAsJsonAsync("api/auth/register", request);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Registration failed for user {Email}. Status code: {StatusCode}, Error: {Error}", 
                        request.Email, response.StatusCode, errorContent);
                    throw new HttpRequestException($"Registration failed: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadFromJsonAsync<AuthResponse>();
                if (result?.Token != null)
                {
                    await _localStorage.SetItemAsync("authToken", result.Token);
                    await _authStateProvider.NotifyUserAuthenticationAsync(result.Token);
                    _navigationManager.NavigateTo("/");
                    _logger.LogInformation("User {Email} registered successfully.", request.Email);
                    return result;
                }

                throw new Exception("Invalid response from server during registration");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during registration for user {Email}", request.Email);
                throw;
            }
        }

        public async Task<bool> LogoutAsync()
        {
            try
            {
                await _localStorage.RemoveItemAsync("authToken");
                await _authStateProvider.NotifyUserLogoutAsync();
                _navigationManager.NavigateTo("/");
                _logger.LogInformation("User logged out successfully.");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during logout");
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

        public async Task<User?> GetCurrentUserAsync()
        {
            try
            {
                var token = await _localStorage.GetItemAsync<string>("authToken");
                if (string.IsNullOrEmpty(token))
                {
                    return null;
                }

                var response = await _httpClient.GetAsync("api/auth/current-user");
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<User>();
                }

                _logger.LogWarning("Failed to get user info. Status code: {StatusCode}", response.StatusCode);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user info");
                return null;
            }
        }

        public async Task<ApplicationUser?> GetUserAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("api/auth/user");
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<ApplicationUser>();
                }
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current user");
                return null;
            }
        }

        public async Task<bool> IsInRoleAsync(ApplicationUser user, string role)
        {
            if (user == null) return false;
            
            try
            {
                var response = await _httpClient.GetAsync($"api/auth/is-in-role/{user.Id}/{role}");
                return response.IsSuccessStatusCode && await response.Content.ReadFromJsonAsync<bool>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking user role");
                return false;
            }
        }

        public async Task<bool> UpdateProfileAsync(UpdateProfileRequest request)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync("api/auth/update-profile", request);
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("User profile updated successfully");
                    return true;
                }

                _logger.LogWarning("Failed to update profile. Status code: {StatusCode}", response.StatusCode);
                return false;
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
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Password changed successfully");
                    return true;
                }

                _logger.LogWarning("Failed to change password. Status code: {StatusCode}", response.StatusCode);
                return false;
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
                var response = await _noAuthClient.PostAsJsonAsync("api/auth/request-password-reset", new { Email = email });
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Password reset requested for {Email}", email);
                    return true;
                }

                _logger.LogWarning("Failed to request password reset for {Email}. Status code: {StatusCode}", 
                    email, response.StatusCode);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error requesting password reset for {Email}", email);
                return false;
            }
        }

        public async Task<bool> ResetPasswordAsync(ResetPasswordRequest request)
        {
            try
            {
                var response = await _noAuthClient.PostAsJsonAsync("api/auth/reset-password", request);
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Password reset successfully for token");
                    return true;
                }

                _logger.LogWarning("Failed to reset password. Status code: {StatusCode}", response.StatusCode);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting password");
                return false;
            }
        }

        public async Task<UserRole> GetUserRole()
        {
            try
            {
                var user = await GetCurrentUserAsync();
                return user?.Role ?? UserRole.User;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user role");
                return UserRole.User;
            }
        }

        public async Task<string?> GetUserId()
        {
            try
            {
                var user = await GetCurrentUserAsync();
                return user?.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user ID");
                return null;
            }
        }

        public async Task<string?> GetUsername()
        {
            try
            {
                var user = await GetCurrentUserAsync();
                return user?.Username;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting username");
                return null;
            }
        }

        public async Task<string?> GetToken()
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
