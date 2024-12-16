using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using VentyTime.Server.Data;
using VentyTime.Shared.Models;

namespace VentyTime.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]s")]
    public class RegistrationController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public RegistrationController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("event/{eventId}")]
        public async Task<ActionResult<IEnumerable<Registration>>> GetEventRegistrations(int eventId)
        {
            return await _context.Registrations
                .Include(r => r.User)
                .Where(r => r.EventId == eventId)
                .ToListAsync();
        }

        [HttpGet("user/{userId}")]
        public async Task<ActionResult<IEnumerable<Registration>>> GetUserRegistrations(string userId)
        {
            return await _context.Registrations
                .Include(r => r.Event)
                .Where(r => r.UserId == userId)
                .ToListAsync();
        }

        [Authorize]
        [HttpPost("event/{eventId}")]
        public async Task<IActionResult> RegisterForEvent(int eventId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var @event = await _context.Events
                .Include(e => e.Registrations)
                .FirstOrDefaultAsync(e => e.Id == eventId);

            if (@event == null)
            {
                return NotFound("Event not found");
            }

            if (@event.IsFull)
            {
                return BadRequest("Event is full");
            }

            if (@event.Registrations.Any(r => r.UserId == userId))
            {
                return BadRequest("Already registered for this event");
            }

            var registration = new Registration
            {
                EventId = eventId,
                UserId = userId,
                RegistrationDate = DateTime.UtcNow
            };

            _context.Registrations.Add(registration);
            await _context.SaveChangesAsync();

            return Ok();
        }

        [Authorize]
        [HttpDelete("{registrationId}")]
        public async Task<IActionResult> CancelRegistration(int registrationId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var registration = await _context.Registrations
                .FirstOrDefaultAsync(r => r.Id == registrationId && r.UserId == userId);

            if (registration == null)
            {
                return NotFound("Registration not found");
            }

            _context.Registrations.Remove(registration);
            await _context.SaveChangesAsync();

            return Ok();
        }
    }
}
