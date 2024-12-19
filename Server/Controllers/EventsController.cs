using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VentyTime.Server.Data;
using VentyTime.Shared.Models;

namespace VentyTime.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EventsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<EventsController> _logger;

        public EventsController(ApplicationDbContext context, ILogger<EventsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<Event>>> GetEvents()
        {
            try
            {
                var events = await _context.Events
                    .Include(e => e.Organizer)
                    .OrderByDescending(e => e.StartDate)
                    .ToListAsync();

                return Ok(events);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting events");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<Event>> GetEvent(int id)
        {
            try
            {
                var @event = await _context.Events
                    .Include(e => e.Organizer)
                    .FirstOrDefaultAsync(e => e.Id == id);

                if (@event == null)
                {
                    return NotFound();
                }

                return Ok(@event);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting event {EventId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult<Event>> CreateEvent(Event @event)
        {
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                @event.OrganizerId = userId;
                @event.CreatedAt = DateTime.UtcNow;

                _context.Events.Add(@event);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetEvent), new { id = @event.Id }, @event);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating event");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateEvent(int id, Event @event)
        {
            try
            {
                if (id != @event.Id)
                {
                    return BadRequest();
                }

                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                var existingEvent = await _context.Events.FindAsync(id);
                if (existingEvent == null)
                {
                    return NotFound();
                }

                if (existingEvent.OrganizerId != userId)
                {
                    return Forbid();
                }

                existingEvent.Title = @event.Title;
                existingEvent.Description = @event.Description;
                existingEvent.StartDate = @event.StartDate;
                existingEvent.StartTime = @event.StartTime;
                existingEvent.Location = @event.Location;
                existingEvent.Category = @event.Category;
                existingEvent.MaxAttendees = @event.MaxAttendees;
                existingEvent.Price = @event.Price;
                existingEvent.ImageUrl = @event.ImageUrl;
                existingEvent.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating event {EventId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            try
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                var @event = await _context.Events.FindAsync(id);
                if (@event == null)
                {
                    return NotFound();
                }

                if (@event.OrganizerId != userId)
                {
                    return Forbid();
                }

                _context.Events.Remove(@event);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting event {EventId}", id);
                return StatusCode(500, "Internal server error");
            }
        }
    }
}
