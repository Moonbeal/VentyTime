using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor;
using VentyTime.Shared.Models;
using VentyTime.Shared.Models.Auth;
using Microsoft.Extensions.Logging;

namespace VentyTime.Client.Services;

public class UserService : IUserService
{
    private readonly HttpClient _httpClient;
    private readonly Blazored.LocalStorage.ILocalStorageService _localStorage;
    private readonly CustomAuthStateProvider _authStateProvider;
    private readonly ISnackbar _snackbar;
    private readonly ILogger<UserService> _logger;

    public UserService(
        HttpClient httpClient,
        Blazored.LocalStorage.ILocalStorageService localStorage,
        AuthenticationStateProvider authStateProvider,
        ISnackbar snackbar,
        ILogger<UserService> logger)
    {
        _httpClient = httpClient;
        _localStorage = localStorage;
        _authStateProvider = (CustomAuthStateProvider)authStateProvider;
        _snackbar = snackbar;
        _logger = logger;
    }

    public async Task<List<ApplicationUser>> GetAllUsersAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("api/users");
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<List<ApplicationUser>>() ?? new List<ApplicationUser>();
            }
            _logger.LogWarning("Failed to get all users");
            return new List<ApplicationUser>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all users");
            return new List<ApplicationUser>();
        }
    }

    public async Task<ApplicationUser?> GetUserByIdAsync(string userId)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<ApplicationUser>($"api/users/{userId}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user by ID");
            return null;
        }
    }

    public async Task<bool> UpdateUserRoleAsync(string userId, UserRole role)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"api/users/{userId}/role", role);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user role");
            return false;
        }
    }

    public async Task<bool> UpdateUserStatusAsync(ApplicationUser user)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"api/users/{user.Id}/status", user.IsActive);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user status");
            return false;
        }
    }

    public async Task<bool> DeleteUserAsync(string userId)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"api/users/{userId}");
            if (response.IsSuccessStatusCode)
            {
                await _authStateProvider.NotifyUserLogout();
                return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user");
            return false;
        }
    }

    public async Task<bool> UpdateUserProfileAsync(ApplicationUser user)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"api/users/{user.Id}", user);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user profile");
            return false;
        }
    }

    private async Task<string> GetAuthTokenAsync()
    {
        return await _localStorage.GetItemAsync<string>("authToken");
    }

    public async Task<bool> LoginAsync(LoginRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/auth/login", request);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var authResponse = JsonSerializer.Deserialize<AuthResponse>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (authResponse?.Token != null)
                {
                    await _localStorage.SetItemAsync("authToken", authResponse.Token);
                    await _localStorage.SetItemAsync("userId", authResponse.UserId);
                    await _localStorage.SetItemAsync("userRole", authResponse.Role.ToString());
                    await ((CustomAuthStateProvider)_authStateProvider).NotifyUserAuthentication(authResponse.Token);
                    _snackbar.Add("Successfully logged in!", Severity.Success);
                    return true;
                }
            }

            var errorMessage = "Invalid email or password";
            try
            {
                var errorResponse = JsonSerializer.Deserialize<AuthResponse>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                if (!string.IsNullOrEmpty(errorResponse?.Message))
                {
                    errorMessage = errorResponse.Message;
                }
            }
            catch { }

            _snackbar.Add(errorMessage, Severity.Error);
            return false;
        }
        catch (Exception ex)
        {
            _snackbar.Add($"An error occurred during login: {ex.Message}", Severity.Error);
            return false;
        }
    }

    public async Task<bool> RegisterAsync(RegisterRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/auth/register", request);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var authResponse = JsonSerializer.Deserialize<AuthResponse>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (authResponse?.Token != null)
                {
                    await _localStorage.SetItemAsync("authToken", authResponse.Token);
                    await _localStorage.SetItemAsync("userId", authResponse.UserId);
                    await _localStorage.SetItemAsync("userRole", authResponse.Role.ToString());
                    await ((CustomAuthStateProvider)_authStateProvider).NotifyUserAuthentication(authResponse.Token);
                    _snackbar.Add("Successfully registered!", Severity.Success);
                    return true;
                }
            }

            var errorMessage = "Registration failed";
            try
            {
                var errorResponse = JsonSerializer.Deserialize<AuthResponse>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                if (!string.IsNullOrEmpty(errorResponse?.Message))
                {
                    errorMessage = errorResponse.Message;
                }
            }
            catch { }

            _snackbar.Add(errorMessage, Severity.Error);
            return false;
        }
        catch (Exception ex)
        {
            _snackbar.Add($"An error occurred during registration: {ex.Message}", Severity.Error);
            return false;
        }
    }

    public async Task LogoutAsync()
    {
        await _localStorage.RemoveItemAsync("authToken");
        await ((CustomAuthStateProvider)_authStateProvider).NotifyUserLogout();
        _snackbar.Add("Successfully logged out!", Severity.Success);
    }

    public async Task<ApplicationUser?> GetUserProfileAsync()
    {
        try
        {
            var token = await _localStorage.GetItemAsync<string>("authToken");
            if (string.IsNullOrEmpty(token))
                throw new UnauthorizedAccessException();

            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            return await _httpClient.GetFromJsonAsync<ApplicationUser>("api/users/profile");
        }
        catch (Exception ex)
        {
            _snackbar.Add($"Error getting user profile: {ex.Message}", Severity.Error);
            return null;
        }
    }

    public async Task<string> GetCurrentUserIdAsync()
    {
        var authState = await _authStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;
        return user.FindFirst(c => c.Type == "sub")?.Value ?? string.Empty;
    }

    public async Task<User> GetCurrentUserAsync()
    {
        try
        {
            var token = await GetAuthTokenAsync();
            if (string.IsNullOrEmpty(token)) return new User();

            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            return await _httpClient.GetFromJsonAsync<User>("api/users/current") ?? new User();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting current user: {ex.Message}");
            return new User();
        }
    }

    public async Task UpdateUserAsync(User user, string newPassword)
    {
        try
        {
            var token = await GetAuthTokenAsync();
            if (string.IsNullOrEmpty(token)) return;

            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            await _httpClient.PutAsJsonAsync($"api/users/{user.Id}", new { User = user, NewPassword = newPassword });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating user: {ex.Message}");
            _snackbar.Add("Failed to update user", Severity.Error);
        }
    }

    public async Task<List<Event>> GetUserEventsAsync()
    {
        try
        {
            var token = await GetAuthTokenAsync();
            if (string.IsNullOrEmpty(token)) return new List<Event>();

            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var result = await _httpClient.GetFromJsonAsync<List<Event>>("api/users/events");
            return result ?? new List<Event>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting user events: {ex.Message}");
            return new List<Event>();
        }
    }

    public async Task<List<Message>> GetMessagesAsync(int conversationId)
    {
        try
        {
            var token = await GetAuthTokenAsync();
            if (string.IsNullOrEmpty(token)) return new List<Message>();

            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var result = await _httpClient.GetFromJsonAsync<List<Message>>($"api/chat/conversations/{conversationId}/messages");
            return result ?? new List<Message>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting messages: {ex.Message}");
            return new List<Message>();
        }
    }

    public async Task SendMessageAsync(int conversationId, string content)
    {
        try
        {
            var token = await GetAuthTokenAsync();
            if (string.IsNullOrEmpty(token)) return;

            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            await _httpClient.PostAsJsonAsync($"api/chat/conversations/{conversationId}/messages", new { Content = content });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending message: {ex.Message}");
            _snackbar.Add("Failed to send message", Severity.Error);
        }
    }

    public async Task<List<Conversation>> GetConversationsAsync()
    {
        try
        {
            var token = await GetAuthTokenAsync();
            if (string.IsNullOrEmpty(token)) return new List<Conversation>();

            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var result = await _httpClient.GetFromJsonAsync<List<Conversation>>("api/chat/conversations");
            return result ?? new List<Conversation>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting conversations: {ex.Message}");
            return new List<Conversation>();
        }
    }

    public async Task<bool> DeleteAccountAsync()
    {
        try
        {
            var response = await _httpClient.DeleteAsync("api/users/me");
            if (response.IsSuccessStatusCode)
            {
                await _authStateProvider.NotifyUserLogout();
                _snackbar.Add("Account deleted successfully", Severity.Success);
                return true;
            }
            _snackbar.Add("Failed to delete account", Severity.Error);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting account");
            _snackbar.Add("An error occurred while deleting your account", Severity.Error);
            return false;
        }
    }
}
