using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VentyTime.Server.Data;
using VentyTime.Server.Services;
using VentyTime.Shared.Models;
using VentyTime.Shared.Models.Auth;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;

namespace VentyTime.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IStorageService _storageService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger _logger;

    public UserController(
        ApplicationDbContext context,
        IStorageService storageService,
        UserManager<ApplicationUser> userManager,
        ILogger<UserController> logger)
    {
        _context = context;
        _storageService = storageService;
        _userManager = userManager;
        _logger = logger;
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IEnumerable<ApplicationUser>>> GetUsers()
    {
        return await _context.Users.ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApplicationUser>> GetUser(string id)
    {
        var user = await _context.Users.FindAsync(id);

        if (user == null)
        {
            return NotFound();
        }

        // Only allow users to view their own profile unless they're an admin
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (id != currentUserId && !User.IsInRole("Admin"))
        {
            return Forbid();
        }

        return user;
    }

    [HttpGet("current")]
    public async Task<ActionResult<User>> GetCurrentUser()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            return NotFound();
        }

        var userRoles = await _context.UserRoles
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.RoleId)
            .ToListAsync();

        var roleName = (await _context.Roles
            .Where(r => userRoles.Contains(r.Id))
            .Select(r => r.Name)
            .FirstOrDefaultAsync()) ?? "User";

        var userRole = roleName switch
        {
            "Admin" => UserRole.Admin,
            "Organizer" => UserRole.Organizer,
            "Participant" => UserRole.Participant,
            _ => UserRole.User
        };

        var organizedEvents = await _context.Events
            .Where(e => e.OrganizerId == userId)
            .Select(e => e.Id.ToString())
            .ToListAsync();

        var registeredEvents = await _context.Registrations
            .Where(r => r.UserId == userId)
            .Select(r => r.EventId.ToString())
            .ToListAsync();

        return Ok(new User
        {
            Id = user.Id,
            Username = user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            FirstName = user.FirstName,
            LastName = user.LastName,
            AvatarUrl = user.AvatarUrl ?? string.Empty,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt,
            IsActive = user.IsActive,
            IsEmailVerified = user.EmailConfirmed,
            Role = userRole,
            PhoneNumber = user.PhoneNumber ?? string.Empty,
            OrganizedEventIds = organizedEvents,
            RegisteredEventIds = registeredEvents
        });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(string id, UpdateProfileRequest request)
    {
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (id != currentUserId && !User.IsInRole("Admin"))
        {
            return Forbid();
        }

        var user = await _context.Users.FindAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        // Update user properties with null checks
        user.FirstName = request.FirstName ?? user.FirstName;
        user.LastName = request.LastName ?? user.LastName;
        user.PhoneNumber = request.PhoneNumber ?? user.PhoneNumber;
        user.UpdatedAt = DateTime.UtcNow;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!UserExists(id))
            {
                return NotFound();
            }
            throw;
        }

        return NoContent();
    }

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        try
        {
            var userEmail = User.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrEmpty(userEmail))
            {
                return Unauthorized();
            }

            var user = await _userManager.FindByEmailAsync(userEmail);
            if (user == null)
            {
                return NotFound();
            }

            user.FirstName = request.FirstName;
            user.LastName = request.LastName;
            user.PhoneNumber = request.PhoneNumber;
            user.UpdatedAt = DateTime.UtcNow;

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                return Ok();
            }

            return BadRequest(new { message = "Failed to update profile", errors = result.Errors.Select(e => e.Description) });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating profile for user {UserEmail}", User.FindFirstValue(ClaimTypes.Email));
            return StatusCode(500, new { message = "An error occurred while updating the profile" });
        }
    }

    [HttpPost("avatar")]
    public async Task<IActionResult> UploadAvatar(IFormFile file)
    {
        try
        {
            var userEmail = User.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrEmpty(userEmail))
            {
                return Unauthorized();
            }

            var user = await _userManager.FindByEmailAsync(userEmail);
            if (user == null)
            {
                return NotFound();
            }

            if (file == null || file.Length == 0)
            {
                return BadRequest("No file uploaded");
            }

            // Validate file type
            var allowedTypes = new[] { "image/jpeg", "image/png", "image/gif" };
            if (!allowedTypes.Contains(file.ContentType.ToLower()))
            {
                return BadRequest("Invalid file type. Only JPEG, PNG and GIF are allowed.");
            }

            // Create a unique file name
            var extension = Path.GetExtension(file.FileName);
            var fileName = $"avatars/{user.Id}/{Guid.NewGuid()}{extension}";

            // Upload file to storage
            var avatarUrl = await _storageService.UploadFileAsync(file.OpenReadStream(), fileName, file.ContentType);

            // Delete old avatar if it exists
            if (!string.IsNullOrEmpty(user.AvatarUrl) && !user.AvatarUrl.Contains("default-profile"))
            {
                var oldFileName = user.AvatarUrl.Replace("/uploads/", "");
                await _storageService.DeleteFileAsync(oldFileName);
            }

            // Update user avatar URL
            user.AvatarUrl = avatarUrl;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { avatarUrl });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading avatar for user {UserEmail}", User.FindFirstValue(ClaimTypes.Email));
            return StatusCode(500, $"An error occurred while uploading the file: {ex.Message}");
        }
    }

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        try
        {
            var userEmail = User.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrEmpty(userEmail))
            {
                _logger.LogWarning("Unauthorized attempt to change password");
                return Unauthorized();
            }

            var user = await _userManager.FindByEmailAsync(userEmail);
            if (user == null)
            {
                _logger.LogWarning("User not found for email: {UserEmail}", userEmail);
                return NotFound(new { message = "User not found" });
            }

            if (string.IsNullOrEmpty(request.CurrentPassword) || string.IsNullOrEmpty(request.NewPassword))
            {
                _logger.LogWarning("Invalid password change request for user {UserId}: missing passwords", user.Id);
                return BadRequest(new { message = "Current password and new password are required" });
            }

            // Verify current password
            var isCurrentPasswordValid = await _userManager.CheckPasswordAsync(user, request.CurrentPassword);
            if (!isCurrentPasswordValid)
            {
                _logger.LogWarning("Invalid current password for user {UserId}", user.Id);
                return BadRequest(new { message = "Current password is incorrect" });
            }

            // Change password
            var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
            if (result.Succeeded)
            {
                _logger.LogInformation("Password changed successfully for user {UserId}", user.Id);
                return Ok(new { message = "Password updated successfully" });
            }

            _logger.LogWarning("Failed to change password for user {UserId}. Errors: {Errors}", 
                user.Id, string.Join(", ", result.Errors.Select(e => e.Description)));
            return BadRequest(new { message = "Failed to change password", errors = result.Errors.Select(e => e.Description) });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing password for user {UserEmail}", User.FindFirstValue(ClaimTypes.Email));
            return StatusCode(500, new { message = "An error occurred while changing the password" });
        }
    }

    [HttpPost("notification-settings")]
    public async Task<IActionResult> UpdateNotificationSettings([FromBody] NotificationSettingsRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            return NotFound();
        }

        return Ok(new
        {
            emailNotifications = request.EmailNotifications,
            pushNotifications = request.PushNotifications,
            eventReminders = request.EventReminders,
            message = "Notification settings updated successfully"
        });
    }

    [HttpPut("{id}/role")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateUserRole(string id, [FromBody] UserRole newRole)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            // Get current roles
            var currentRoles = await _userManager.GetRolesAsync(user);

            // Remove current roles
            if (currentRoles.Any())
            {
                var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
                if (!removeResult.Succeeded)
                {
                    _logger.LogError("Failed to remove existing roles for user {UserId}. Errors: {Errors}",
                        id, string.Join(", ", removeResult.Errors.Select(e => e.Description)));
                    return BadRequest(new { message = "Failed to update user role" });
                }
            }

            // Add new role
            var newRoleName = newRole.ToString();
            var addResult = await _userManager.AddToRoleAsync(user, newRoleName);
            if (!addResult.Succeeded)
            {
                _logger.LogError("Failed to add role {Role} to user {UserId}. Errors: {Errors}",
                    newRoleName, id, string.Join(", ", addResult.Errors.Select(e => e.Description)));
                return BadRequest(new { message = "Failed to update user role" });
            }

            _logger.LogInformation("Successfully updated role to {Role} for user {UserId}", newRoleName, id);
            return Ok(new { message = "User role updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating role for user {UserId}", id);
            return StatusCode(500, new { message = "An error occurred while updating the user role" });
        }
    }

    public class ChangePasswordRequest
    {
        public string CurrentPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }

    public class NotificationSettingsRequest
    {
        public bool EmailNotifications { get; set; }
        public bool PushNotifications { get; set; }
        public bool EventReminders { get; set; }
    }

    private bool UserExists(string id)
    {
        return _context.Users.Any(e => e.Id == id);
    }
}
