using VentyTime.Shared.Models;

namespace VentyTime.Client.Services
{
    public interface IUserService
    {
        Task<bool> LoginAsync(LoginRequest request);
        Task<bool> RegisterAsync(RegisterRequest request);
        Task LogoutAsync();
        Task<string> GetCurrentUserIdAsync();
        Task<ApplicationUser?> GetUserProfileAsync();
        
        // Chat methods
        Task<List<Conversation>> GetConversationsAsync();
        Task<List<Message>> GetMessagesAsync(int conversationId);
        Task SendMessageAsync(int conversationId, string content);
        
        // User profile methods
        Task<User> GetCurrentUserAsync();
        Task<List<Event>> GetUserEventsAsync();
        Task<List<Event>> GetCreatedEventsAsync();
        Task UpdateUserAsync(User user, string newPassword);
        
        // Admin methods
        Task<List<ApplicationUser>> GetAllUsersAsync();
        Task<ApplicationUser?> GetUserByIdAsync(string userId);
        Task<bool> UpdateUserRoleAsync(string userId, UserRole role);
        Task<bool> UpdateUserStatusAsync(ApplicationUser user);
        Task<bool> DeleteUserAsync(string userId);
        Task<bool> UpdateUserProfileAsync(ApplicationUser user);
    }
}
