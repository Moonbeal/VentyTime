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
    public class EventController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<EventController> _logger;

        public EventController(ApplicationDbContext context, ILogger<EventController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Event>>> GetEvents()
        {
            return await _context.Events
                .Include(e => e.Organizer)
                .ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Event>> GetEvent(int id)
        {
            var @event = await _context.Events
                .Include(e => e.Organizer)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (@event == null)
            {
                return NotFound();
            }

            return @event;
        }

        [Authorize]
        [HttpPost]
        public async Task<ActionResult<Event>> CreateEvent(Event @event)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            @event.OrganizerId = userId;
            _context.Events.Add(@event);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetEvent), new { id = @event.Id }, @event);
        }

        [Authorize]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateEvent(int id, Event @event)
        {
            if (id != @event.Id)
            {
                return BadRequest();
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var existingEvent = await _context.Events.FindAsync(id);

            if (existingEvent == null)
            {
                return NotFound();
            }

            if (existingEvent.OrganizerId != userId)
            {
                return Unauthorized();
            }

            _context.Entry(existingEvent).State = EntityState.Detached;
            _context.Entry(@event).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!EventExists(id))
                {
                    return NotFound();
                }
                throw;
            }

            return NoContent();
        }

        [Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            var @event = await _context.Events.FindAsync(id);
            if (@event == null)
            {
                return NotFound();
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (@event.OrganizerId != userId)
            {
                return Unauthorized();
            }

            _context.Events.Remove(@event);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [Authorize]
        [HttpPost("{id}/register")]
        public async Task<IActionResult> RegisterForEvent(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var @event = await _context.Events
                .Include(e => e.Registrations)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (@event == null)
            {
                return NotFound();
            }

            if (@event.IsFull)
            {
                return BadRequest("Event is full");
            }

            if (@event.Registrations.Any(r => r.UserId == userId))
            {
                return BadRequest("Already registered");
            }

            var registration = new Registration
            {
                EventId = id,
                UserId = userId,
                RegistrationDate = DateTime.UtcNow
            };

            _context.Registrations.Add(registration);
            await _context.SaveChangesAsync();

            return Ok();
        }

        [Authorize]
        [HttpPost("{id}/unregister")]
        public async Task<IActionResult> UnregisterFromEvent(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var registration = await _context.Registrations
                .FirstOrDefaultAsync(r => r.EventId == id && r.UserId == userId);

            if (registration == null)
            {
                return NotFound();
            }

            _context.Registrations.Remove(registration);
            await _context.SaveChangesAsync();

            return Ok();
        }

        [HttpGet("user/{userId}")]
        public async Task<ActionResult<IEnumerable<Event>>> GetUserEvents(string userId)
        {
            return await _context.Events
                .Include(e => e.Organizer)
                .Where(e => e.OrganizerId == userId)
                .ToListAsync();
        }

        [HttpGet("registrations/{userId}")]
        public async Task<ActionResult<IEnumerable<Event>>> GetUserRegistrations(string userId)
        {
            return await _context.Events
                .Include(e => e.Organizer)
                .Include(e => e.Registrations)
                .Where(e => e.Registrations.Any(r => r.UserId == userId))
                .ToListAsync();
        }

        private bool EventExists(int id)
        {
            return _context.Events.Any(e => e.Id == id);
        }
    }
}
