using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Logging;
using MudBlazor;
using VentyTime.Client.Auth;
using VentyTime.Shared.Models;
using VentyTime.Shared.Models.Auth;

namespace VentyTime.Client.Services
{
    public class UserService : IUserService
    {
        private readonly HttpClient _httpClient;
        private readonly Blazored.LocalStorage.ILocalStorageService _localStorage;
        private readonly CustomAuthStateProvider _authStateProvider;
        private readonly ISnackbar _snackbar;
        private readonly ILogger<UserService> _logger;

        public UserService(
            IHttpClientFactory httpClientFactory,
            Blazored.LocalStorage.ILocalStorageService localStorage,
            AuthenticationStateProvider authStateProvider,
            ISnackbar snackbar,
            ILogger<UserService> logger)
        {
            _httpClient = httpClientFactory.CreateClient("VentyTime.ServerAPI");
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
                var response = await _httpClient.PutAsJsonAsync($"api/user/{userId}/role", role);
                if (response.IsSuccessStatusCode)
                {
                    _snackbar.Add("User role updated successfully", Severity.Success);
                    return true;
                }

                var error = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Failed to update user role: {Error}", error);
                _snackbar.Add(error, Severity.Error);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user role");
                _snackbar.Add("An error occurred while updating user role", Severity.Error);
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
                    await _authStateProvider.NotifyUserLogoutAsync();
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
                return await UpdateProfileAsync(
                    user.FirstName ?? string.Empty,
                    user.LastName ?? string.Empty,
                    user.Email ?? string.Empty,
                    user.PhoneNumber ?? string.Empty);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user profile");
                _snackbar.Add("An error occurred while updating profile", Severity.Error);
                return false;
            }
        }

        private async Task<string?> GetAuthTokenAsync()
        {
            return await _localStorage.GetItemAsync<string>("authToken");
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            try
            {
                _logger.LogInformation("Attempting login for user: {Email} with role {Role}",
                    request.Email, request.SelectedRole);

                var response = await _httpClient.PostAsJsonAsync("api/auth/login", request);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<AuthResponse>(responseContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (result != null && !string.IsNullOrEmpty(result.Token))
                    {
                        await _localStorage.SetItemAsync("authToken", result.Token);
                        await _authStateProvider.NotifyUserAuthenticationAsync(result.Token);
                        _logger.LogInformation("User {Email} logged in successfully with role {Role}",
                            request.Email, request.SelectedRole);
                        return result;
                    }
                }

                _logger.LogWarning("Login response status: {Status}", response.StatusCode);
                if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    var error = JsonSerializer.Deserialize<Dictionary<string, string>>(responseContent,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    _logger.LogWarning("Login failed: {Error}", error?["message"] ?? "Unknown error");
                    return new AuthResponse
                    {
                        Success = false,
                        Message = error?["message"] ?? "Invalid email, password, or selected role"
                    };
                }

                return new AuthResponse
                {
                    Success = false,
                    Message = "An error occurred during login"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for user: {Email}", request.Email);
                return new AuthResponse
                {
                    Success = false,
                    Message = "An error occurred during login"
                };
            }
        }

        public async Task<(bool Success, string[] Errors)> RegisterAsync(RegisterRequest request)
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
                        await _localStorage.SetItemAsync("userId", authResponse.User?.Id);
                        await _localStorage.SetItemAsync("userRole", authResponse.User?.Role.ToString() ?? UserRole.User.ToString());
                        await _authStateProvider.NotifyUserAuthenticationAsync(authResponse.Token);
                        _snackbar.Add("Successfully registered!", Severity.Success);
                        return (true, Array.Empty<string>());
                    }
                }

                if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    try
                    {
                        var errorResponse = JsonSerializer.Deserialize<Dictionary<string, object>>(responseContent, 
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                        if (errorResponse != null)
                        {
                            if (errorResponse.TryGetValue("errors", out var errorsObj) && 
                                errorsObj is JsonElement errorsElement && 
                                errorsElement.ValueKind == JsonValueKind.Array)
                            {
                                var errors = errorsElement.EnumerateArray()
                                    .Select(e => e.GetString() ?? "Unknown error")
                                    .ToArray();
                                return (false, errors);
                            }
                            

                            if (errorResponse.TryGetValue("message", out var messageObj))
                            {
                                var message = messageObj.ToString();
                                return (false, new[] { message ?? "Unknown error" });
                            }
                        }
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogWarning(ex, "Failed to parse error response: {Content}", responseContent);
                    }
                }

                return (false, new[] { "Registration failed. Please try again." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during registration");
                return (false, new[] { $"An error occurred during registration: {ex.Message}" });
            }
        }

        public async Task LogoutAsync()
        {
            await _localStorage.RemoveItemAsync("authToken");
            await _authStateProvider.NotifyUserLogoutAsync();
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
            return user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
        }

        public async Task<User?> GetCurrentUserAsync()
        {
            try
            {
                var token = await GetAuthTokenAsync();
                if (string.IsNullOrEmpty(token))
                {
                    _logger.LogWarning("No auth token found");
                    return null;
                }

                _httpClient.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                var response = await _httpClient.GetAsync("api/auth/current");
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<User>();
                }
                
                _logger.LogWarning("Failed to get current user. Status code: {StatusCode}", response.StatusCode);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current user");
                return null;
            }
        }

        public async Task<HttpResponseMessage> UpdateUserAsync(string userId, UpdateProfileRequest request)
        {
            try
            {
                var token = await GetAuthTokenAsync();
                if (string.IsNullOrEmpty(token)) 
                    return new HttpResponseMessage(System.Net.HttpStatusCode.Unauthorized);

                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                return await _httpClient.PutAsJsonAsync($"api/user/{userId}", request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user");
                return new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError) 
                { 
                    Content = new StringContent(ex.Message) 
                };
            }
        }

        public async Task<bool> UpdateUserAsync(ApplicationUser user)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync($"api/user/{user.Id}", user);
                if (response.IsSuccessStatusCode)
                {
                    // Оновлюємо локальний стан користувача
                    await _localStorage.SetItemAsync("user", JsonSerializer.Serialize(user));
                    return true;
                }
                return false;
            }
            catch (Exception)
            {
                return false;
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
                    await _localStorage.RemoveItemAsync("authToken");
                    await _authStateProvider.NotifyUserLogoutAsync();
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

        public async Task<IList<string>> GetUserRolesAsync(string userId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/users/{userId}/roles");
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<List<string>>() ?? new List<string>();
                }
                _logger.LogWarning("Failed to get user roles");
                return new List<string>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user roles");
                return new List<string>();
            }
        }

        public async Task<HttpResponseMessage> UploadAvatarAsync(string userId, IBrowserFile file)
        {
            try
            {
                // Create form data
                var content = new MultipartFormDataContent();
                var fileContent = new StreamContent(file.OpenReadStream(maxAllowedSize: 5 * 1024 * 1024));
                fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);
                content.Add(fileContent, "file", file.Name);

                // Upload the file
                return await _httpClient.PostAsync($"api/user/avatar", content);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading avatar");
                throw;
            }
        }

        public async Task<bool> UpdateProfileAsync(string firstName, string lastName, string email, string phoneNumber)
        {
            try
            {
                var request = new UpdateProfileRequest
                {
                    FirstName = firstName,
                    LastName = lastName,
                    Email = email,
                    PhoneNumber = phoneNumber
                };

                var response = await _httpClient.PutAsJsonAsync($"api/user/profile", request);
                if (response.IsSuccessStatusCode)
                {
                    _snackbar.Add("Profile updated successfully", Severity.Success);
                    return true;
                }

                var error = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Failed to update profile: {Error}", error);
                _snackbar.Add(error, Severity.Error);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating profile");
                _snackbar.Add("An error occurred while updating profile", Severity.Error);
                return false;
            }
        }

        public async Task<bool> ChangePasswordAsync(string currentPassword, string newPassword)
        {
            try
            {
                var request = new ChangePasswordRequest
                {
                    CurrentPassword = currentPassword,
                    NewPassword = newPassword
                };

                var response = await _httpClient.PostAsJsonAsync("api/user/change-password", request);
                if (response.IsSuccessStatusCode)
                {
                    _snackbar.Add("Password changed successfully", Severity.Success);
                    return true;
                }

                var error = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Failed to change password: {Error}", error);
                _snackbar.Add(error, Severity.Error);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password");
                _snackbar.Add("An error occurred while changing password", Severity.Error);
                return false;
            }
        }

        public async Task<bool> UpdateNotificationSettingsAsync(
            bool emailNotifications, 
            bool pushNotifications, 
            bool eventReminders,
            bool newFollowerNotifications,
            bool newLikeNotifications,
            bool newCommentNotifications)
        {
            try
            {
                var request = new NotificationSettingsModel
                {
                    EmailNotifications = emailNotifications,
                    PushNotifications = pushNotifications,
                    EventReminders = eventReminders,
                    NewFollowerNotifications = newFollowerNotifications,
                    NewLikeNotifications = newLikeNotifications,
                    NewCommentNotifications = newCommentNotifications
                };

                var response = await _httpClient.PutAsJsonAsync("api/users/notifications", request);
                if (response.IsSuccessStatusCode)
                {
                    _snackbar.Add("Notification settings updated successfully", Severity.Success);
                    return true;
                }

                _snackbar.Add("Failed to update notification settings", Severity.Error);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating notification settings");
                _snackbar.Add("An error occurred while updating notification settings", Severity.Error);
                return false;
            }
        }

        public async Task<NotificationSettingsModel?> GetNotificationSettingsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("api/users/notifications");
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<NotificationSettingsModel>();
                }

                _logger.LogWarning("Failed to get notification settings");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting notification settings");
                return null;
            }
        }
    }
}
