using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VentyTime.Server.Services;
using VentyTime.Shared.Models;

namespace VentyTime.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationsController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        [HttpGet]
        public async Task<ActionResult<List<NotificationMessage>>> GetUserNotifications()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var notifications = await _notificationService.GetNotificationsForUserAsync(userId);
            return Ok(notifications);
        }

        [HttpPost("events/participants/{userId}")]
        [Authorize(Roles = "Admin,Organizer")]
        public async Task<IActionResult> SendNotificationToParticipant(string userId, NotificationMessage notification)
        {
            try
            {
                notification.UserId = userId;
                await _notificationService.CreateNotificationAsync(notification);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("events/{eventId}/participants")]
        [Authorize(Roles = "Admin,Organizer")]
        public async Task<IActionResult> SendNotificationToAllParticipants(int eventId, NotificationMessage notification)
        {
            try
            {
                await _notificationService.CreateNotificationsForEventParticipantsAsync(eventId, notification);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}/read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            try
            {
                await _notificationService.DismissNotificationAsync(id);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteNotification(int id)
        {
            try
            {
                await _notificationService.DeleteNotificationAsync(id);
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
