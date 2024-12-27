using VentyTime.Shared.Models;
using VentyTime.Shared.Models.Auth;

namespace VentyTime.Client.Services
{
    public interface IAuthService
    {
        Task<AuthResponse> LoginAsync(LoginRequest request);
        Task<AuthResponse> RegisterAsync(RegisterRequest request);
        Task<bool> LogoutAsync();
        Task<User?> GetCurrentUserAsync();
        Task<bool> IsAuthenticated();
        Task<UserRole> GetUserRole();
        Task<bool> UpdateProfileAsync(UpdateProfileRequest request);
        Task<bool> ChangePasswordAsync(ChangePasswordRequest request);
        Task<bool> RequestPasswordResetAsync(string email);
        Task<bool> ResetPasswordAsync(ResetPasswordRequest request);
        Task<string?> GetUserId();
        Task<string?> GetUsername();
        Task<string?> GetToken();
    }
}
