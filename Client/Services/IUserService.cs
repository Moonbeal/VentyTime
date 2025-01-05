using System.Net.Http;
using Microsoft.AspNetCore.Components.Forms;
using VentyTime.Shared.Models;
using VentyTime.Shared.Models.Auth;

namespace VentyTime.Client.Services
{
    public interface IUserService
    {
        Task<List<ApplicationUser>> GetAllUsersAsync();
        Task<ApplicationUser?> GetUserByIdAsync(string userId);
        Task<bool> UpdateUserRoleAsync(string userId, UserRole role);
        Task<bool> UpdateUserStatusAsync(ApplicationUser user);
        Task<bool> DeleteUserAsync(string userId);
        Task<bool> UpdateUserProfileAsync(ApplicationUser user);
        Task<AuthResponse> LoginAsync(LoginRequest request);
        Task<(bool Success, string[] Errors)> RegisterAsync(RegisterRequest request);
        Task LogoutAsync();
        Task<ApplicationUser?> GetUserProfileAsync();
        Task<string> GetCurrentUserIdAsync();
        Task<User?> GetCurrentUserAsync();
        Task<HttpResponseMessage> UpdateUserAsync(string userId, UpdateProfileRequest request);
        Task<bool> UpdateUserAsync(ApplicationUser user);
        Task<List<Event>> GetUserEventsAsync();
        Task<List<Message>> GetMessagesAsync(int conversationId);
        Task SendMessageAsync(int conversationId, string content);
        Task<List<Conversation>> GetConversationsAsync();
        Task<bool> DeleteAccountAsync();
        Task<IList<string>> GetUserRolesAsync(string userId);
        Task<HttpResponseMessage> UploadAvatarAsync(string userId, IBrowserFile file);
        Task<bool> UpdateProfileAsync(string firstName, string lastName, string email, string phoneNumber);
        Task<bool> ChangePasswordAsync(string currentPassword, string newPassword);
        Task<bool> UpdateNotificationSettingsAsync(bool emailNotifications, bool pushNotifications, bool eventReminders,
            bool newFollowerNotifications, bool newLikeNotifications, bool newCommentNotifications);
        Task<NotificationSettingsModel?> GetNotificationSettingsAsync();
    }
}
