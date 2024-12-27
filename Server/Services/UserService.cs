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
        Task<List<ApplicationUser>> GetAllUsersAsync();
        Task<ApplicationUser?> GetUserByIdAsync(string userId);
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
                EmailConfirmed = true // Temporary until email confirmation is set up
            };

            try
            {
                var result = await _userManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    // Ensure we have a valid role, defaulting to User if not specified
                    var selectedRole = !string.IsNullOrEmpty(model.Role) && (model.Role == "User" || model.Role == "Organizer") 
                        ? model.Role 
                        : "User";

                    var roleResult = await _userManager.AddToRoleAsync(user, selectedRole);
                    
                    if (roleResult.Succeeded)
                    {
                        _logger.LogInformation("Successfully created user {Email} with {Role} role", 
                            model.Email, selectedRole);
                        return (true, Array.Empty<string>());
                    }
                    
                    _logger.LogError("Failed to assign {Role} role to {Email}. Errors: {Errors}", 
                        selectedRole, model.Email, string.Join(", ", roleResult.Errors.Select(e => e.Description)));
                    
                    // If role assignment fails, delete the user and return the error
                    await _userManager.DeleteAsync(user);
                    return (false, roleResult.Errors.Select(e => e.Description).ToArray());
                }

                _logger.LogError("Failed to create user {Email}. Errors: {Errors}", 
                    model.Email, string.Join(", ", result.Errors.Select(e => e.Description)));
                return (false, result.Errors.Select(e => e.Description).ToArray());
            }
            catch (Exception ex)
            {
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
                _logger.LogWarning("Login failed - user not found or inactive: {Email}", model.Email);
                return false;
            }

            // First check if the password is correct
            var result = await _signInManager.CheckPasswordSignInAsync(user, model.Password, true);
            if (!result.Succeeded)
            {
                _logger.LogWarning("Login failed - invalid password for user: {Email}", model.Email);
                return false;
            }

            // Then check if the user has the selected role
            var hasRole = await _userManager.IsInRoleAsync(user, model.SelectedRole.ToString());
            if (!hasRole)
            {
                _logger.LogWarning("Login failed - user {Email} does not have the selected role: {Role}", 
                    model.Email, model.SelectedRole);
                return false;
            }

            // Update last login timestamp
            user.LastLoginAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            _logger.LogInformation("User {Email} logged in successfully with role {Role}", 
                model.Email, model.SelectedRole);
            return true;
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

        public async Task<List<ApplicationUser>> GetAllUsersAsync()
        {
            return await _context.Users.ToListAsync();
        }

        public async Task<ApplicationUser?> GetUserByIdAsync(string userId)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        }
    }
}
