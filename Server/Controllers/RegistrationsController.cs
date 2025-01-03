using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using VentyTime.Server.Services;
using VentyTime.Shared.Models;

namespace VentyTime.Server.Controllers
{
    [ApiController]
    [Route("api/registrations")]
    [Authorize]
    public class RegistrationsController : ControllerBase
    {
        private readonly IRegistrationService _registrationService;
        private readonly ILogger<RegistrationsController> _logger;

        public RegistrationsController(
            IRegistrationService registrationService,
            ILogger<RegistrationsController> logger)
        {
            _registrationService = registrationService;
            _logger = logger;
        }

        [HttpGet("event/{eventId}/status")]
        public async Task<ActionResult<object>> GetRegistrationStatus(int eventId)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                var registrations = await _registrationService.GetRegistrationsByUserAsync(userId);
                var registration = registrations.FirstOrDefault(r => r.EventId == eventId);

                if (registration == null)
                {
                    return Ok(new { status = RegistrationStatus.None });
                }

                return Ok(new { status = registration.Status });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking registration status for event {EventId}", eventId);
                return StatusCode(500, new { message = "An error occurred while checking registration status" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateRegistration([FromBody] UserEventRegistration registration)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                // Set the current user ID and timestamps
                registration.UserId = userId;
                registration.RegisteredAt = DateTime.UtcNow;
                registration.CreatedAt = DateTime.UtcNow;
                registration.Status = RegistrationStatus.Pending;

                var result = await _registrationService.CreateRegistrationAsync(registration, userId);
                return CreatedAtAction(nameof(GetRegistration), new { id = result.Id }, result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while creating the registration.");
                return StatusCode(500, new { message = "An error occurred while creating the registration." });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<UserEventRegistration>> GetRegistration(int id)
        {
            try
            {
                var registration = await _registrationService.GetRegistrationByIdAsync(id);
                if (registration == null)
                {
                    return NotFound();
                }

                return Ok(registration);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving registration {RegistrationId}", id);
                return StatusCode(500, new { message = "An error occurred while retrieving the registration" });
            }
        }

        [HttpGet("event/{eventId}")]
        public async Task<ActionResult<IEnumerable<UserEventRegistration>>> GetRegistrationsByEvent(int eventId)
        {
            try
            {
                var registrations = await _registrationService.GetRegistrationsByEventAsync(eventId);
                return Ok(registrations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving registrations for event {EventId}", eventId);
                return StatusCode(500, new { message = "An error occurred while retrieving the registrations" });
            }
        }

        [HttpGet("user")]
        public async Task<ActionResult<IEnumerable<UserEventRegistration>>> GetUserRegistrations()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                var registrations = await _registrationService.GetRegistrationsByUserAsync(userId);
                return Ok(registrations);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user registrations");
                return StatusCode(500, new { message = "An error occurred while retrieving the registrations" });
            }
        }

        [HttpPut("{id}/cancel")]
        public async Task<IActionResult> CancelRegistration(int id)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                var success = await _registrationService.CancelRegistrationAsync(id, userId);
                if (success)
                {
                    return Ok();
                }

                return NotFound();
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling registration {RegistrationId}", id);
                return StatusCode(500, new { message = "An error occurred while cancelling the registration" });
            }
        }

        [HttpPut("{id}/confirm")]
        [Authorize(Roles = "Admin,Organizer")]
        public async Task<IActionResult> ConfirmRegistration(int id)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                var success = await _registrationService.ConfirmRegistrationAsync(id, userId);
                if (success)
                {
                    return Ok();
                }

                return NotFound();
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error confirming registration {RegistrationId}", id);
                return StatusCode(500, new { message = "An error occurred while confirming the registration" });
            }
        }
    }
}
