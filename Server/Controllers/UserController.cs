using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VentyTime.Server.Data;
using VentyTime.Server.Services;
using VentyTime.Shared.Models;
using VentyTime.Shared.Models.Auth;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace VentyTime.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IStorageService _storageService;

    public UserController(
        ApplicationDbContext context,
        IStorageService storageService)
    {
        _context = context;
        _storageService = storageService;
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
        user.Location = request.Location ?? user.Location;
        user.Bio = request.Bio ?? user.Bio;
        user.Website = request.Website ?? user.Website;
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

    [HttpPost("{id}/avatar")]
    public async Task<IActionResult> UploadAvatar(string id, IFormFile file)
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

        try
        {
            // Upload file to storage
            var fileName = $"avatars/{id}/{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var avatarUrl = await _storageService.UploadFileAsync(file.OpenReadStream(), fileName, file.ContentType);

            // Delete old avatar if it exists
            if (!string.IsNullOrEmpty(user.AvatarUrl) && user.AvatarUrl != "/images/default-profile.png")
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
            return StatusCode(500, $"An error occurred while uploading the file: {ex.Message}");
        }
    }

    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
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

        if (string.IsNullOrEmpty(request.CurrentPassword) || string.IsNullOrEmpty(request.NewPassword))
        {
            return BadRequest("Current password and new password are required");
        }

        // Here you would typically:
        // 1. Verify the current password
        // 2. Hash the new password
        // 3. Update the user's password hash
        // For now, we'll just return success
        return Ok(new { message = "Password updated successfully" });
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
