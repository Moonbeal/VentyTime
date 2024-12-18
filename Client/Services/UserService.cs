using System.Net.Http.Json;
using System.Security.Claims;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor;
using VentyTime.Shared.Models;

namespace VentyTime.Client.Services;

public class UserService : IUserService
{
    private readonly HttpClient _httpClient;
    private readonly Blazored.LocalStorage.ILocalStorageService _localStorage;
    private readonly AuthenticationStateProvider _authStateProvider;
    private readonly ISnackbar _snackbar;

    public UserService(
        HttpClient httpClient,
        Blazored.LocalStorage.ILocalStorageService localStorage,
        AuthenticationStateProvider authStateProvider,
        ISnackbar snackbar)
    {
        _httpClient = httpClient;
        _localStorage = localStorage;
        _authStateProvider = authStateProvider;
        _snackbar = snackbar;
    }

    public async Task<List<ApplicationUser>> GetAllUsersAsync()
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<List<ApplicationUser>>("api/users");
            return response ?? new List<ApplicationUser>();
        }
        catch (Exception)
        {
            return new List<ApplicationUser>();
        }
    }

    public async Task<ApplicationUser?> GetUserByIdAsync(string userId)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<ApplicationUser>($"api/users/{userId}");
        }
        catch (Exception)
        {
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
        catch (Exception)
        {
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
        catch (Exception)
        {
            return false;
        }
    }

    public async Task<bool> DeleteUserAsync(string userId)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"api/users/{userId}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception)
        {
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
        catch (Exception)
        {
            return false;
        }
    }

    public async Task<bool> LoginAsync(LoginRequest request)
    {
        try
        {
            var result = await _httpClient.PostAsJsonAsync("api/auth/login", request);
            if (result.IsSuccessStatusCode)
            {
                var token = await result.Content.ReadAsStringAsync();
                await _localStorage.SetItemAsync("authToken", token);
                ((CustomAuthStateProvider)_authStateProvider).NotifyUserAuthentication(token);
                _snackbar.Add("Successfully logged in!", Severity.Success);
                return true;
            }
            else
            {
                var error = await result.Content.ReadAsStringAsync();
                _snackbar.Add(error, Severity.Error);
                return false;
            }
        }
        catch (Exception ex)
        {
            _snackbar.Add($"An error occurred: {ex.Message}", Severity.Error);
            return false;
        }
    }

    public async Task<bool> RegisterAsync(RegisterRequest request)
    {
        try
        {
            var result = await _httpClient.PostAsJsonAsync("api/auth/register", request);
            if (result.IsSuccessStatusCode)
            {
                var token = await result.Content.ReadAsStringAsync();
                await _localStorage.SetItemAsync("authToken", token);
                ((CustomAuthStateProvider)_authStateProvider).NotifyUserAuthentication(token);
                _snackbar.Add("Successfully registered!", Severity.Success);
                return true;
            }
            else
            {
                var error = await result.Content.ReadAsStringAsync();
                _snackbar.Add(error, Severity.Error);
                return false;
            }
        }
        catch (Exception ex)
        {
            _snackbar.Add($"An error occurred: {ex.Message}", Severity.Error);
            return false;
        }
    }

    public async Task LogoutAsync()
    {
        await _localStorage.RemoveItemAsync("authToken");
        ((CustomAuthStateProvider)_authStateProvider).NotifyUserLogout();
        _snackbar.Add("Successfully logged out!", Severity.Success);
    }

    public async Task<string> GetCurrentUserIdAsync()
    {
        var authState = await _authStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;
        return user.FindFirst(c => c.Type == "sub")?.Value ?? string.Empty;
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

    public async Task<List<Event>> GetCreatedEventsAsync()
    {
        try
        {
            var token = await GetAuthTokenAsync();
            if (string.IsNullOrEmpty(token)) return new List<Event>();

            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var result = await _httpClient.GetFromJsonAsync<List<Event>>("api/users/events/created");
            return result ?? new List<Event>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting created events: {ex.Message}");
            return new List<Event>();
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

    private async Task<string> GetAuthTokenAsync()
    {
        try
        {
            var token = await _localStorage.GetItemAsync<string>("authToken");
            return token ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }
}
