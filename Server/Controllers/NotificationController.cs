using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VentyTime.Server.Services;
using VentyTime.Shared.Models;

namespace VentyTime.Server.Controllers;

[ApiController]
[Route("api/users/notifications")]
[Authorize]
public class NotificationController : ControllerBase
{
    private readonly INotificationService _notificationService;
    private readonly ILogger<NotificationController> _logger;

    public NotificationController(INotificationService notificationService, ILogger<NotificationController> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<NotificationSettingsModel>> GetNotificationSettings()
    {
        try
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var settings = await _notificationService.GetNotificationSettingsAsync(userId);
            if (settings == null)
            {
                return NotFound();
            }

            return Ok(settings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting notification settings");
            return StatusCode(500, "An error occurred while getting notification settings");
        }
    }

    [HttpPut]
    public async Task<IActionResult> UpdateNotificationSettings([FromBody] NotificationSettingsModel settings)
    {
        try
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var success = await _notificationService.UpdateNotificationSettingsAsync(userId, settings);
            if (!success)
            {
                return NotFound();
            }

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating notification settings");
            return StatusCode(500, "An error occurred while updating notification settings");
        }
    }

    [HttpPost("clear")]
    public async Task<IActionResult> ClearNotifications()
    {
        try
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var success = await _notificationService.ClearNotificationsAsync(userId);
            if (!success)
            {
                return StatusCode(500, "An error occurred while clearing notifications");
            }

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing notifications");
            return StatusCode(500, "An error occurred while clearing notifications");
        }
    }
}
