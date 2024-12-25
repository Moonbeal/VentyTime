using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using VentyTime.Server.Data;
using VentyTime.Shared.Models;
using VentyTime.Shared.Models.Auth;

namespace VentyTime.Server.Services
{
    public interface IUserService
    {
        Task<(bool Succeeded, string[] Errors)> CreateUserAsync(RegisterRequest model);
        Task<ApplicationUser?> GetUserByEmailAsync(string email);
        Task<bool> ValidateUserAsync(LoginRequest model);
        Task<IList<string>> GetUserRolesAsync(ApplicationUser user);
        Task<bool> IsInRoleAsync(ApplicationUser user, string role);
        Task<bool> AddToRoleAsync(ApplicationUser user, string role);
        Task<bool> RemoveFromRolesAsync(ApplicationUser user, IEnumerable<string> roles);
        Task UpdateUserLastLoginAsync(ApplicationUser user);
    }

    public class UserService : IUserService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UserService> _logger;

        public UserService(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext context,
            ILogger<UserService> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
            _logger = logger;
        }

        public async Task<(bool Succeeded, string[] Errors)> CreateUserAsync(RegisterRequest model)
        {
            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName,
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
                EmailConfirmed = true // Тимчасово, поки не налаштована відправка email
            };

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var result = await _userManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    // Додаємо користувача до ролі за замовчуванням
                    await _userManager.AddToRoleAsync(user, UserRole.User.ToString());
                    await transaction.CommitAsync();
                    return (true, Array.Empty<string>());
                }

                await transaction.RollbackAsync();
                return (false, result.Errors.Select(e => e.Description).ToArray());
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error creating user {Email}", model.Email);
                throw;
            }
        }

        public async Task<ApplicationUser?> GetUserByEmailAsync(string email)
        {
            return await _userManager.FindByEmailAsync(email);
        }

        public async Task<bool> ValidateUserAsync(LoginRequest model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null || !user.IsActive)
            {
                return false;
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, model.Password, true);
            return result.Succeeded;
        }

        public async Task<IList<string>> GetUserRolesAsync(ApplicationUser user)
        {
            return await _userManager.GetRolesAsync(user);
        }

        public async Task<bool> IsInRoleAsync(ApplicationUser user, string role)
        {
            return await _userManager.IsInRoleAsync(user, role);
        }

        public async Task<bool> AddToRoleAsync(ApplicationUser user, string role)
        {
            var result = await _userManager.AddToRoleAsync(user, role);
            return result.Succeeded;
        }

        public async Task<bool> RemoveFromRolesAsync(ApplicationUser user, IEnumerable<string> roles)
        {
            var result = await _userManager.RemoveFromRolesAsync(user, roles);
            return result.Succeeded;
        }

        public async Task UpdateUserLastLoginAsync(ApplicationUser user)
        {
            user.LastLoginAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);
        }
    }
}
