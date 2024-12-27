using VentyTime.Shared.Models;
using VentyTime.Shared.Models.Auth;

namespace VentyTime.Client.Services;

public interface IUserService
{
    Task<AuthResponse> LoginAsync(LoginRequest request);
    Task<(bool Success, string[] Errors)> RegisterAsync(RegisterRequest request);
    Task LogoutAsync();
    Task<ApplicationUser?> GetUserProfileAsync();
    Task<ApplicationUser?> GetUserByIdAsync(string userId);
    Task<string> GetCurrentUserIdAsync();
    Task<User> GetCurrentUserAsync();
    Task UpdateUserAsync(User user, string newPassword);
    Task<bool> UpdateUserStatusAsync(ApplicationUser user);
    Task<bool> DeleteUserAsync(string userId);
    Task<List<Event>> GetUserEventsAsync();
    Task<List<Message>> GetMessagesAsync(int conversationId);
    Task SendMessageAsync(int conversationId, string content);
    Task<List<Conversation>> GetConversationsAsync();
    Task<List<ApplicationUser>> GetAllUsersAsync();
    Task<IList<string>> GetUserRolesAsync(string userId);
}
