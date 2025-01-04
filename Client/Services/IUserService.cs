using VentyTime.Shared.Models;
using VentyTime.Shared.Models.Auth;
using System.Net.Http;

namespace VentyTime.Client.Services;

public interface IUserService
{
    Task<AuthResponse> LoginAsync(LoginRequest request);
    Task<(bool Success, string[] Errors)> RegisterAsync(RegisterRequest request);
    Task LogoutAsync();
    Task<ApplicationUser?> GetUserProfileAsync();
    Task<ApplicationUser?> GetUserByIdAsync(string userId);
    Task<string> GetCurrentUserIdAsync();
    Task<User?> GetCurrentUserAsync();
    Task<bool> UpdateProfileAsync(string firstName, string lastName, string email, string phoneNumber);
    Task<bool> ChangePasswordAsync(string currentPassword, string newPassword);
    Task<bool> UpdateNotificationSettingsAsync(bool emailNotifications, bool pushNotifications, bool eventReminders);
    Task<HttpResponseMessage> UpdateUserAsync(string userId, UpdateProfileRequest request);
    Task<bool> UpdateUserStatusAsync(ApplicationUser user);
    Task<bool> DeleteUserAsync(string userId);
    Task<List<Event>> GetUserEventsAsync();
    Task<List<Message>> GetMessagesAsync(int conversationId);
    Task SendMessageAsync(int conversationId, string content);
    Task<List<Conversation>> GetConversationsAsync();
    Task<List<ApplicationUser>> GetAllUsersAsync();
    Task<IList<string>> GetUserRolesAsync(string userId);
    Task<HttpResponseMessage> UploadAvatarAsync(string userId, MultipartFormDataContent content);
    Task<bool> IsInRoleAsync(string role);
}
