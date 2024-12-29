using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using VentyTime.Server.Services;
using VentyTime.Shared.Models;

namespace VentyTime.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
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

        [HttpPost("{eventId}")]
        public async Task<ActionResult<EventRegistration>> CreateRegistration(int eventId)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                var registration = await _registrationService.CreateRegistrationAsync(eventId, userId);
                return Ok(registration);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating registration");
                return StatusCode(500, new { message = "An error occurred while creating the registration" });
            }
        }

        [HttpGet("event/{eventId}")]
        public async Task<ActionResult<IEnumerable<EventRegistration>>> GetRegistrationsByEvent(int eventId)
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
        public async Task<ActionResult<IEnumerable<EventRegistration>>> GetUserRegistrations()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
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

        [HttpGet("{eventId}/user")]
        public async Task<ActionResult<EventRegistration>> GetRegistration(int eventId)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                var registration = await _registrationService.GetRegistrationAsync(eventId, userId);
                if (registration == null)
                {
                    return NotFound();
                }

                return Ok(registration);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving registration {RegistrationId}", eventId);
                return StatusCode(500, new { message = "An error occurred while retrieving the registration" });
            }
        }

        [HttpPut("{eventId}/status")]
        public async Task<ActionResult<EventRegistration>> UpdateRegistration(int eventId, [FromBody] RegistrationStatus newStatus)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                var registration = await _registrationService.UpdateRegistrationStatusAsync(eventId, userId, newStatus);
                return Ok(registration);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating registration");
                return StatusCode(500, "An error occurred while updating the registration");
            }
        }

        [HttpPost("{eventId}/cancel")]
        public async Task<ActionResult> CancelRegistration(int eventId)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                var success = await _registrationService.CancelRegistrationAsync(eventId, userId);
                if (!success)
                {
                    return NotFound();
                }

                return Ok();
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling registration {RegistrationId}", eventId);
                return StatusCode(500, new { message = "An error occurred while cancelling the registration" });
            }
        }

        [HttpPost("{eventId}/confirm")]
        [Authorize(Roles = "Admin,Organizer")]
        public async Task<ActionResult> ConfirmRegistration(int eventId)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                var success = await _registrationService.ConfirmRegistrationAsync(eventId, userId);
                if (!success)
                {
                    return NotFound();
                }

                return Ok();
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error confirming registration {RegistrationId}", eventId);
                return StatusCode(500, new { message = "An error occurred while confirming the registration" });
            }
        }
    }
}
