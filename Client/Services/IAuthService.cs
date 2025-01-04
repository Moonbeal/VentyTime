using VentyTime.Shared.Models;
using VentyTime.Shared.Models.Auth;

namespace VentyTime.Client.Services
{
    public interface IAuthService
    {
        Task<AuthResponse> LoginAsync(LoginRequest request);
        Task<AuthResponse> RegisterAsync(RegisterRequest request);
        Task<bool> LogoutAsync();
        Task<ApplicationUser?> GetUserAsync();
        Task<bool> IsAuthenticated();
        Task<bool> IsInRoleAsync(ApplicationUser user, string role);
        Task<bool> UpdateProfileAsync(UpdateProfileRequest request);
        Task<bool> ChangePasswordAsync(ChangePasswordRequest request);
        Task<bool> RequestPasswordResetAsync(string email);
        Task<bool> ResetPasswordAsync(ResetPasswordRequest request);
        Task<string?> GetUserId();
        Task<string?> GetUsername();
        Task<string?> GetToken();
    }
}
