using VentyTime.Shared.Models;

namespace VentyTime.Client.Services;

public interface IUserService
{
    Task<bool> LoginAsync(LoginRequest request);
    Task<bool> RegisterAsync(RegisterRequest request);
    Task LogoutAsync();
    Task<string> GetCurrentUserIdAsync();
    Task<ApplicationUser?> GetUserProfileAsync();
}
