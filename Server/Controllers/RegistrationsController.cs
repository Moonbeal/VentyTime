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

        [HttpPost]
        public async Task<ActionResult<Registration>> CreateRegistration(Registration registration)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

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
                _logger.LogError(ex, "Error creating registration");
                return StatusCode(500, new { message = "An error occurred while creating the registration" });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Registration>> GetRegistration(int id)
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
        public async Task<ActionResult<IEnumerable<Registration>>> GetRegistrationsByEvent(int eventId)
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
        public async Task<ActionResult<IEnumerable<Registration>>> GetUserRegistrations()
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

        [HttpPut("{id}")]
        public async Task<ActionResult<Registration>> UpdateRegistration(int id, [FromBody] Registration registration)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                var existingRegistration = await _registrationService.GetRegistrationByIdAsync(id);
                if (existingRegistration == null)
                {
                    return NotFound();
                }

                if (existingRegistration.UserId != userId)
                {
                    return Forbid();
                }

                registration.Id = id;
                var updatedRegistration = await _registrationService.UpdateRegistrationAsync(registration, registration.Status, userId);
                return Ok(updatedRegistration);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating registration");
                return StatusCode(500, "An error occurred while updating the registration");
            }
        }

        [HttpPost("{id}/cancel")]
        public async Task<IActionResult> CancelRegistration(int id)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                var result = await _registrationService.CancelRegistrationAsync(id, userId);
                if (!result)
                {
                    return BadRequest(new { message = "Registration is already cancelled" });
                }

                return Ok();
            }
            catch (KeyNotFoundException)
            {
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

        [HttpPost("{id}/confirm")]
        [Authorize(Roles = "Admin,Organizer")]
        public async Task<IActionResult> ConfirmRegistration(int id)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                var result = await _registrationService.ConfirmRegistrationAsync(id, userId);
                if (!result)
                {
                    return BadRequest(new { message = "Registration cannot be confirmed" });
                }

                return Ok();
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error confirming registration {RegistrationId}", id);
                return StatusCode(500, new { message = "An error occurred while confirming the registration" });
            }
        }
    }
}
