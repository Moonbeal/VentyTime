using System.Net.Http.Json;
using System.Security.Claims;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor;
using VentyTime.Shared.Models;

namespace VentyTime.Client.Services;

public class UserService : IUserService
{
    private readonly HttpClient _http;
    private readonly ILocalStorageService _localStorage;
    private readonly AuthenticationStateProvider _authStateProvider;
    private readonly ISnackbar _snackbar;
    private readonly NotificationService _notificationService;

    public UserService(
        HttpClient http,
        ILocalStorageService localStorage,
        AuthenticationStateProvider authStateProvider,
        ISnackbar snackbar,
        NotificationService notificationService)
    {
        _http = http;
        _localStorage = localStorage;
        _authStateProvider = authStateProvider;
        _snackbar = snackbar;
        _notificationService = notificationService;
    }

    public async Task<List<ApplicationUser>> GetUsers()
    {
        try
        {
            var token = await GetAuthTokenAsync();
            if (string.IsNullOrEmpty(token)) return new List<ApplicationUser>();

            _http.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var result = await _http.GetFromJsonAsync<List<ApplicationUser>>("api/users");
            return result ?? new List<ApplicationUser>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting users: {ex.Message}");
            return new List<ApplicationUser>();
        }
    }

    public async Task<ApplicationUser?> GetUserById(string userId)
    {
        try
        {
            var token = await GetAuthTokenAsync();
            if (string.IsNullOrEmpty(token)) return null;

            _http.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            return await _http.GetFromJsonAsync<ApplicationUser>($"api/users/{userId}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting user by ID: {ex.Message}");
            return null;
        }
    }

    public async Task<ApplicationUser?> GetCurrentUser()
    {
        try
        {
            var userId = await GetCurrentUserIdAsync();
            return await GetUserById(userId);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting current user: {ex.Message}");
            _notificationService.ShowError("Failed to load user profile");
            return null;
        }
    }

    public async Task<string> GetCurrentUserIdAsync()
    {
        try
        {
            var authState = await _authStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;
            var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            if (string.IsNullOrEmpty(userId))
                throw new UnauthorizedAccessException("User is not authenticated");
                
            return userId;
        }
        catch (Exception ex)
        {
            _snackbar.Add($"Error getting current user ID: {ex.Message}", Severity.Error);
            throw;
        }
    }

    public async Task<bool> UpdateUser(ApplicationUser user)
    {
        try
        {
            var token = await GetAuthTokenAsync();
            if (string.IsNullOrEmpty(token)) return false;

            _http.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _http.PutAsJsonAsync($"api/users/{user.Id}", user);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating user: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> DeleteUser(string userId)
    {
        try
        {
            var token = await GetAuthTokenAsync();
            if (string.IsNullOrEmpty(token)) return false;

            _http.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _http.DeleteAsync($"api/users/{userId}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting user: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> ChangePassword(ChangePasswordRequest request)
    {
        try
        {
            var token = await GetAuthTokenAsync();
            if (string.IsNullOrEmpty(token)) return false;

            _http.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var response = await _http.PostAsJsonAsync("api/users/change-password", request);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error changing password: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> LoginAsync(LoginRequest request)
    {
        try
        {
            var result = await _http.PostAsJsonAsync("api/auth/login", request);
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
            var result = await _http.PostAsJsonAsync("api/auth/register", request);
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

    public async Task<ApplicationUser?> GetUserProfileAsync()
    {
        try
        {
            var token = await _localStorage.GetItemAsync<string>("authToken");
            if (string.IsNullOrEmpty(token))
                throw new UnauthorizedAccessException();

            _http.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var userId = await GetCurrentUserIdAsync();
            return await _http.GetFromJsonAsync<ApplicationUser>($"api/users/{userId}");
        }
        catch (Exception ex)
        {
            _snackbar.Add($"Error getting user profile: {ex.Message}", Severity.Error);
            return null;
        }
    }

    public async Task<ApplicationUser?> GetUserByIdAsync(string userId)
    {
        try
        {
            var response = await _http.GetAsync($"api/user/{userId}");
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ApplicationUser>();
            }
            return null;
        }
        catch (Exception ex)
        {
            _snackbar.Add($"Error fetching user: {ex.Message}", Severity.Error);
            return null;
        }
    }

    private async Task<string?> GetAuthTokenAsync()
    {
        try
        {
            return await _localStorage.GetItemAsync<string>("authToken");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting auth token: {ex.Message}");
            return null;
        }
    }
}
