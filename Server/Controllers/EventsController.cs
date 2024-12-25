using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using VentyTime.Server.Services;
using VentyTime.Shared.Models;

namespace VentyTime.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class EventsController : ControllerBase
    {
        private readonly IEventService _eventService;
        private readonly ILogger<EventsController> _logger;

        public EventsController(IEventService eventService, ILogger<EventsController> logger)
        {
            _eventService = eventService;
            _logger = logger;
        }

        [HttpGet]
        [AllowAnonymous]
        [ResponseCache(Duration = 60)] // Кешування на стороні клієнта на 1 хвилину
        public async Task<ActionResult<IEnumerable<Event>>> GetEvents()
        {
            try
            {
                var (events, totalCount) = await _eventService.GetEventsAsync();
                return Ok(events);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving events");
                return StatusCode(500, new { message = "An error occurred while retrieving events" });
            }
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<Event>> GetEvent(int id)
        {
            try
            {
                var @event = await _eventService.GetEventByIdAsync(id);
                if (@event == null)
                {
                    return NotFound(new { message = $"Event with ID {id} not found" });
                }
                return Ok(@event);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving event {EventId}", id);
                return StatusCode(500, new { message = "An error occurred while retrieving the event" });
            }
        }

        [HttpGet("search")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<Event>>> SearchEvents([FromQuery] string query)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(query))
                {
                    return BadRequest(new { message = "Search query cannot be empty" });
                }

                var events = await _eventService.SearchEventsAsync(query);
                return Ok(events);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching events with query: {Query}", query);
                return StatusCode(500, new { message = "An error occurred while searching events" });
            }
        }

        [HttpGet("upcoming")]
        [AllowAnonymous]
        [ResponseCache(Duration = 60)]
        public async Task<ActionResult<IEnumerable<Event>>> GetUpcomingEvents([FromQuery] int count = 10)
        {
            try
            {
                var events = await _eventService.GetUpcomingEventsAsync(count);
                return Ok(events);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving upcoming events");
                return StatusCode(500, new { message = "An error occurred while retrieving upcoming events" });
            }
        }

        [HttpGet("organizer/{organizerId}")]
        public async Task<ActionResult<IEnumerable<Event>>> GetEventsByOrganizer(string organizerId)
        {
            try
            {
                var events = await _eventService.GetEventsByOrganizerAsync(organizerId);
                return Ok(events);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving events for organizer {OrganizerId}", organizerId);
                return StatusCode(500, new { message = "An error occurred while retrieving events by organizer" });
            }
        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult<Event>> CreateEvent([FromBody] Event eventItem)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                eventItem.CreatorId = userId;

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var createdEvent = await _eventService.CreateEventAsync(eventItem, userId);
                return CreatedAtAction(nameof(GetEvent), new { id = createdEvent.Id }, createdEvent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating event");
                return StatusCode(500, new { message = "An error occurred while creating the event" });
            }
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateEvent(int id, [FromBody] Event @event)
        {
            try
            {
                if (id != @event.Id)
                {
                    return BadRequest(new { message = "Event ID mismatch" });
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                try
                {
                    var updatedEvent = await _eventService.UpdateEventAsync(@event, userId);
                    return Ok(updatedEvent);
                }
                catch (KeyNotFoundException)
                {
                    return NotFound(new { message = $"Event with ID {id} not found" });
                }
                catch (UnauthorizedAccessException)
                {
                    return Forbid();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating event {EventId}", id);
                return StatusCode(500, new { message = "An error occurred while updating the event" });
            }
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                try
                {
                    await _eventService.DeleteEventAsync(id, userId);
                    return NoContent();
                }
                catch (KeyNotFoundException)
                {
                    return NotFound(new { message = $"Event with ID {id} not found" });
                }
                catch (UnauthorizedAccessException)
                {
                    return Forbid();
                }
                catch (InvalidOperationException ex)
                {
                    return BadRequest(new { message = ex.Message });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting event {EventId}", id);
                return StatusCode(500, new { message = "An error occurred while deleting the event" });
            }
        }
    }
}
