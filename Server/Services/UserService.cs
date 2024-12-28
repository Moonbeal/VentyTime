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
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UserService> _logger;

        public UserService(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context,
            ILogger<UserService> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
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
            try
            {
                _logger.LogInformation("Validating user {Email} with role {Role}", model.Email, model.SelectedRole);

                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user == null || !user.IsActive)
                {
                    _logger.LogWarning("User {Email} not found or inactive", model.Email);
                    return false;
                }

                var result = await _signInManager.CheckPasswordSignInAsync(user, model.Password, false);
                if (!result.Succeeded)
                {
                    _logger.LogWarning("Password validation failed for user {Email}", model.Email);
                    return false;
                }

                // Check if the role exists
                var roleName = model.SelectedRole.ToString();
                var roleExists = await _roleManager.RoleExistsAsync(roleName);
                if (!roleExists)
                {
                    _logger.LogError("Role {Role} does not exist", roleName);
                    return false;
                }

                // Get current user roles
                var currentRoles = await _userManager.GetRolesAsync(user);
                _logger.LogInformation("Current roles for user {Email}: {Roles}", model.Email, string.Join(", ", currentRoles));
                
                // Remove any existing roles
                if (currentRoles.Any())
                {
                    _logger.LogInformation("Removing existing roles for user {Email}: {Roles}", 
                        model.Email, string.Join(", ", currentRoles));
                    var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
                    if (!removeResult.Succeeded)
                    {
                        _logger.LogError("Failed to remove existing roles for user {Email}. Errors: {Errors}",
                            model.Email, string.Join(", ", removeResult.Errors.Select(e => e.Description)));
                        return false;
                    }
                }

                // Add the selected role
                _logger.LogInformation("Adding role {Role} to user {Email}", roleName, model.Email);
                var addRoleResult = await _userManager.AddToRoleAsync(user, roleName);
                if (!addRoleResult.Succeeded)
                {
                    _logger.LogError("Failed to add role {Role} to user {Email}. Errors: {Errors}",
                        roleName, model.Email, string.Join(", ", addRoleResult.Errors.Select(e => e.Description)));
                    return false;
                }

                // Verify that the role was added successfully
                var updatedRoles = await _userManager.GetRolesAsync(user);
                _logger.LogInformation("Updated roles for user {Email}: {Roles}", model.Email, string.Join(", ", updatedRoles));

                // Update last login timestamp
                user.LastLoginAt = DateTime.UtcNow;
                await _userManager.UpdateAsync(user);

                _logger.LogInformation("User {Email} validated successfully with role {Role}", 
                    model.Email, model.SelectedRole);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating user {Email}", model.Email);
                return false;
            }
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
