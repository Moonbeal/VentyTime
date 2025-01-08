using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using VentyTime.Server.Services;
using VentyTime.Shared.Models;
using VentyTime.Shared.Models.Auth;

namespace VentyTime.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<UsersController> _logger;

        public UsersController(
            IUserService userService,
            UserManager<ApplicationUser> userManager,
            ILogger<UsersController> logger)
        {
            _userService = userService;
            _userManager = userManager;
            _logger = logger;
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<IEnumerable<ApplicationUser>>> GetAllUsers()
        {
            _logger.LogInformation("Retrieving all users");
            var users = await _userService.GetAllUsersAsync();
            return Ok(users);
        }

        [HttpGet("{userId}")]
        [Authorize]
        public async Task<ActionResult<ApplicationUser>> GetUser(string userId)
        {
            var user = await _userService.GetUserByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User not found: {UserId}", userId);
                return NotFound();
            }
            return Ok(user);
        }

        [HttpGet("{userId}/roles")]
        [Authorize]
        public async Task<ActionResult<IList<string>>> GetUserRoles(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User not found while retrieving roles: {UserId}", userId);
                return NotFound();
            }

            var roles = await _userManager.GetRolesAsync(user);
            return Ok(roles);
        }

        [HttpPut("{userId}/status")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateUserStatus(string userId, [FromBody] bool isActive)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User not found while updating status: {UserId}", userId);
                return NotFound();
            }

            user.IsActive = isActive;
            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                _logger.LogInformation("User status updated: {UserId}, IsActive: {IsActive}", userId, isActive);
                return Ok();
            }

            _logger.LogError("Failed to update user status: {UserId}, Errors: {Errors}", userId, string.Join(", ", result.Errors.Select(e => e.Description)));
            return BadRequest(result.Errors);
        }

        [HttpPut("{userId}/profile")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateUserProfile(string userId, [FromBody] UpdateProfileRequest request)
        {
            _logger.LogInformation("Updating profile for user {UserId}", userId);
            
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User not found while updating profile: {UserId}", userId);
                return NotFound("User not found");
            }

            user.FirstName = request.FirstName;
            user.LastName = request.LastName;
            user.PhoneNumber = request.PhoneNumber;
            
            // Email можна оновити тільки через спеціальну процедуру з підтвердженням
            if (user.Email != request.Email)
            {
                _logger.LogWarning("Email update is not supported through this endpoint");
                return BadRequest("Email update is not supported through this endpoint");
            }

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                _logger.LogInformation("User profile updated successfully: {UserId}", userId);
                return Ok();
            }

            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            _logger.LogError("Failed to update user profile: {UserId}, Errors: {Errors}", userId, errors);
            return BadRequest(errors);
        }

        [HttpDelete("{userId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User not found while attempting deletion: {UserId}", userId);
                return NotFound();
            }

            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                _logger.LogInformation("User deleted successfully: {UserId}", userId);
                return Ok();
            }

            _logger.LogError("Failed to delete user: {UserId}, Errors: {Errors}", userId, string.Join(", ", result.Errors.Select(e => e.Description)));
            return BadRequest(result.Errors);
        }
    }
}
