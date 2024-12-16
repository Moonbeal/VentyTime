using VentyTime.Shared.Models;
using System.Threading.Tasks;

namespace VentyTime.Client.Services
{
    public interface IAuthService
    {
        Task<AuthResponse> LoginAsync(LoginRequest request);
        Task<AuthResponse> RegisterAsync(RegisterRequest request);
        Task LogoutAsync();
        Task<User> GetCurrentUserAsync();
        Task<bool> IsAuthenticated();
        Task<UserRole> GetUserRole();
        Task<bool> UpdateProfileAsync(UpdateProfileRequest request);
        Task<bool> ChangePasswordAsync(ChangePasswordRequest request);
        Task<bool> RequestPasswordResetAsync(string email);
        Task<bool> ResetPasswordAsync(ResetPasswordRequest request);
        Task<string> GetUserId();
        Task<string> GetUsername();
        Task<string> GetToken();
    }
}
