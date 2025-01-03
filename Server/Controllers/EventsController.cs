using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using VentyTime.Server.Services;
using VentyTime.Shared.Models;
using Microsoft.EntityFrameworkCore;
using VentyTime.Server.Data;
using Microsoft.AspNetCore.Identity;

namespace VentyTime.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class EventsController : ControllerBase
    {
        private readonly IEventService _eventService;
        private readonly ILogger<EventsController> _logger;
        private readonly ApplicationDbContext _context;

        public EventsController(
            IEventService eventService,
            ILogger<EventsController> logger,
            ApplicationDbContext context)
        {
            _eventService = eventService;
            _logger = logger;
            _context = context;
        }

        [HttpGet]
        [AllowAnonymous]
        [ResponseCache(Duration = 60)]
        public async Task<ActionResult<ApiResponse<EventsResponse>>> GetEvents(
            [FromQuery] int page = 1,
            [FromQuery] int? pageSize = null,
            [FromQuery] string? category = null,
            [FromQuery] string? searchQuery = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var result = await _eventService.GetEventsAsync(page, pageSize, category, searchQuery, startDate, endDate);
                return Ok(new ApiResponse<EventsResponse>
                {
                    IsSuccessful = true,
                    Data = result,
                    Message = $"Found {result.Events.Count()} events"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving events");
                return StatusCode(500, new ApiResponse<EventsResponse>
                {
                    IsSuccessful = false,
                    Message = "An error occurred while retrieving events"
                });
            }
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<EventDto>> GetEvent(int id)
        {
            try
            {
                var @event = await _eventService.GetEventByIdAsync(id);

                if (@event == null)
                {
                    return NotFound();
                }

                var eventDto = new EventDto
                {
                    Id = @event.Id,
                    Title = @event.Title,
                    Description = @event.Description,
                    StartDate = @event.StartDate,
                    EndDate = @event.EndDate,
                    StartTime = @event.StartTime,
                    Location = @event.Location,
                    VenueDetails = @event.VenueDetails,
                    OnlineUrl = @event.OnlineUrl,
                    IsOnline = @event.IsOnline,
                    MaxAttendees = @event.MaxAttendees,
                    Category = @event.Category,
                    Type = @event.Type,
                    Accessibility = @event.Accessibility,
                    ImageUrl = @event.ImageUrl,
                    Price = @event.Price,
                    IsActive = @event.IsActive,
                    IsCancelled = @event.IsCancelled,
                    CreatorId = @event.CreatorId,
                    OrganizerId = @event.OrganizerId,
                    CurrentParticipants = @event.Registrations?.Count ?? 0,
                    AllowWaitlist = @event.AllowWaitlist,
                    WaitlistCapacity = @event.WaitlistCapacity,
                    Tags = @event.Tags
                };

                return Ok(eventDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving event {EventId}", id);
                return StatusCode(500, new { message = "An error occurred while retrieving the event" });
            }
        }

        [HttpGet("search")]
        [AllowAnonymous]
        public async Task<ActionResult<ApiResponse<SearchEventsResponse>>> SearchEvents([FromQuery] string query)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(query))
                {
                    return BadRequest(new ApiResponse<SearchEventsResponse>
                    {
                        IsSuccessful = false,
                        Message = "Search query cannot be empty"
                    });
                }

                var result = await _eventService.SearchEventsAsync(query);
                return Ok(new ApiResponse<SearchEventsResponse>
                {
                    IsSuccessful = true,
                    Data = result,
                    Message = $"Found {result.TotalResults} events matching '{result.SearchQuery}'"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching events with query: {Query}", query);
                return StatusCode(500, new ApiResponse<SearchEventsResponse>
                {
                    IsSuccessful = false,
                    Message = "An error occurred while searching events"
                });
            }
        }

        [HttpGet("upcoming")]
        [AllowAnonymous]
        [ResponseCache(Duration = 60)]
        public async Task<ActionResult<ApiResponse<UpcomingEventsResponse>>> GetUpcomingEvents([FromQuery] int count = 10)
        {
            try
            {
                var result = await _eventService.GetUpcomingEventsAsync(count);
                return Ok(new ApiResponse<UpcomingEventsResponse>
                {
                    IsSuccessful = true,
                    Data = result,
                    Message = $"Found {result.Count} upcoming events out of {result.TotalUpcoming} total"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving upcoming events");
                return StatusCode(500, new ApiResponse<UpcomingEventsResponse>
                {
                    IsSuccessful = false,
                    Message = "An error occurred while retrieving upcoming events"
                });
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
        [Authorize(Roles = "Admin,Organizer")]
        public async Task<ActionResult<Event>> CreateEvent([FromBody] Event eventModel)
        {
            try
            {
                _logger.LogInformation("Creating event. User: {User}, Role: {Role}, Claims: {@Claims}",
                    User.Identity?.Name,
                    User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value,
                    User.Claims.Select(c => new { c.Type, c.Value }));

                if (eventModel == null)
                {
                    _logger.LogWarning("Event model is null");
                    return BadRequest("Event data is required");
                }

                var userId = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogError("User ID not found in claims");
                    return BadRequest("User ID not found");
                }

                _logger.LogInformation("Creating event with data: {@EventModel}", eventModel);

                // Validate required fields
                if (string.IsNullOrEmpty(eventModel.Title))
                {
                    _logger.LogWarning("Event title is required");
                    return BadRequest("Event title is required");
                }

                if (string.IsNullOrEmpty(eventModel.Description))
                {
                    _logger.LogWarning("Event description is required");
                    return BadRequest("Event description is required");
                }

                if (string.IsNullOrEmpty(eventModel.Location))
                {
                    _logger.LogWarning("Event location is required");
                    return BadRequest("Event location is required");
                }

                if (string.IsNullOrEmpty(eventModel.Category))
                {
                    _logger.LogWarning("Event category is required");
                    return BadRequest("Event category is required");
                }

                if (eventModel.StartTime == default)
                {
                    _logger.LogWarning("Event start time is required");
                    return BadRequest("Event start time is required");
                }

                if (eventModel.EndDate == default)
                {
                    _logger.LogWarning("Event end date is required");
                    return BadRequest("Event end date is required");
                }

                if (eventModel.MaxAttendees <= 0)
                {
                    _logger.LogWarning("Event max attendees must be greater than 0");
                    return BadRequest("Event max attendees must be greater than 0");
                }

                var createdEvent = await _eventService.CreateEventAsync(eventModel, userId);
                _logger.LogInformation("Event created successfully: {@CreatedEvent}", createdEvent);

                return CreatedAtAction(nameof(GetEvent), new { id = createdEvent.Id }, createdEvent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating event. Message: {Error}, Stack Trace: {StackTrace}, Inner Exception: {InnerException}",
                    ex.Message,
                    ex.StackTrace,
                    ex.InnerException?.Message);

                return StatusCode(500, new
                {
                    message = "An error occurred while creating the event",
                    error = ex.Message,
                    innerError = ex.InnerException?.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }

        [HttpPost("seed")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SeedEvents()
        {
            try
            {
                var categories = new[] { "Technology", "Music", "Sports", "Food & Drink", "Arts & Culture", 
                    "Business", "Education", "Entertainment", "Health & Wellness", "Charity", "Fashion", "Lifestyle" };
                
                var locations = new[] { "Convention Center", "Sports Complex", "Hotel Grand Ballroom", 
                    "University Campus", "Public Library", "Art Gallery", "Community Center", "Tech Hub", 
                    "Music Hall", "Outdoor Park", "Business Center", "Innovation Lab" };

                var eventTypes = new[] { "Workshop", "Conference", "Seminar", "Festival", "Exhibition", 
                    "Tournament", "Meetup", "Fair", "Show", "Concert", "Retreat", "Hackathon" };

                var imageUrls = new[] {
                    "https://loremflickr.com/800/400/event",
                    "https://loremflickr.com/800/400/conference",
                    "https://loremflickr.com/800/400/concert",
                    "https://loremflickr.com/800/400/workshop",
                    "https://loremflickr.com/800/400/meetup"
                };

                var random = new Random();
                var events = new List<Event>();
                var baseDateTime = DateTime.UtcNow;

                for (int i = 1; i <= 100; i++)
                {
                    var category = categories[random.Next(categories.Length)];
                    var location = locations[random.Next(locations.Length)];
                    var type = eventTypes[random.Next(eventTypes.Length)];
                    var daysOffset = random.Next(1, 365);
                    var price = random.Next(0, 1000);
                    var maxAttendees = random.Next(10, 500);
                    var isOnline = random.Next(2) == 0;

                    var imageIndex = random.Next(imageUrls.Length);
                    var imageUrl = imageUrls[imageIndex];

                    // Add a random number to make each URL unique
                    imageUrl = $"{imageUrl}?random={Guid.NewGuid().ToString("N")[..8]}";

                    var startDate = baseDateTime.AddDays(daysOffset);
                    var startTime = TimeSpan.FromHours(random.Next(9, 18));
                    var duration = TimeSpan.FromHours(random.Next(1, 8));
                    var endDate = startDate.Add(duration);

                    var evt = new Event
                    {
                        Title = $"{type} {i}",
                        Description = $"Experience a unique opportunity to learn and network with professionals. {(isOnline ? "Join us online for this interactive session." : "Join us at our venue for this engaging gathering.")}",
                        StartDate = startDate,
                        EndDate = endDate,
                        StartTime = startTime,
                        Location = isOnline ? string.Empty : location,
                        VenueDetails = isOnline ? null : $"Room {random.Next(100, 999)}",
                        OnlineUrl = isOnline ? $"https://meet.ventytime.com/event-{i}" : null,
                        IsOnline = isOnline,
                        MaxAttendees = maxAttendees,
                        Category = category,
                        Type = EventType.Other,  
                        Accessibility = EventAccessibility.Public,  
                        ImageUrl = imageUrl,
                        Price = price,
                        IsActive = true,
                        IsCancelled = false,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        CreatorId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty,
                        OrganizerId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty,
                        IsFeatured = random.Next(5) == 0, // 20% chance to be featured
                        RequiresRegistration = random.Next(2) == 0,
                        HasAgeRestriction = random.Next(5) == 0,
                        MinimumAge = random.Next(5) == 0 ? random.Next(13, 21) : null,
                        HasEarlyBirdPrice = price > 0 && random.Next(3) == 0,
                        EarlyBirdPrice = price > 0 && random.Next(3) == 0 ? price * 0.8m : null,
                        EarlyBirdDeadline = random.Next(3) == 0 ? baseDateTime.AddDays(daysOffset - 14) : null,
                        RefundPolicy = random.Next(2) == 0 ? "Full refund up to 24 hours before the event" : null,
                        Requirements = random.Next(2) == 0 ? "Please bring your own laptop and enthusiasm!" : null,
                        Schedule = $"9:00 AM - Registration\n10:00 AM - Main Event\n12:00 PM - Lunch Break\n1:00 PM - Workshops\n4:00 PM - Networking\n5:00 PM - Closing",
                        Tags = new List<string> { category, type, isOnline ? "Online" : "In-Person" },
                        AllowWaitlist = maxAttendees > 50 && random.Next(2) == 0,
                        WaitlistCapacity = maxAttendees > 50 && random.Next(2) == 0 ? maxAttendees / 5 : null
                    };

                    events.Add(evt);
                }

                await _context.Events.AddRangeAsync(events);
                await _context.SaveChangesAsync();

                return Ok(new { message = $"Successfully seeded {events.Count} events" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while seeding events");
                return StatusCode(500, new { message = "An error occurred while seeding events", error = ex.Message });
            }
        }

        [HttpPost("update-images")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateEventImages()
        {
            try
            {
                var events = await _context.Events.ToListAsync();
                foreach (var @event in events)
                {
                    if (@event.ImageUrl != null && (@event.ImageUrl.StartsWith("http") || @event.ImageUrl.Contains("seeded")))
                    {
                        @event.ImageUrl = "/images/default-event.jpg";
                    }
                }
                await _context.SaveChangesAsync();
                return Ok(new { message = "Event images updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating event images");
                return StatusCode(500, new { message = "An error occurred while updating event images" });
            }
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Organizer")]
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
        [Authorize(Roles = "Admin,Organizer")]
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

        [HttpPost("{eventId}/register")]
        [Authorize]
        public async Task<ActionResult<Registration>> RegisterForEvent(int eventId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            try
            {
                var registration = await _eventService.RegisterUserForEventAsync(eventId, userId);
                return Ok(registration);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("registered")]
        [Authorize]
        public async Task<ActionResult<ApiResponse<RegisteredEventsResponse>>> GetRegisteredEvents()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new ApiResponse<RegisteredEventsResponse>
                {
                    IsSuccessful = false,
                    Message = "User not authenticated"
                });
            }

            try
            {
                var result = await _eventService.GetRegisteredEventsAsync(userId);
                return Ok(new ApiResponse<RegisteredEventsResponse>
                {
                    IsSuccessful = true,
                    Data = result,
                    Message = $"Found {result.TotalCount} registered events for user {userId}"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving registered events for user {UserId}", userId);
                return StatusCode(500, new ApiResponse<RegisteredEventsResponse>
                {
                    IsSuccessful = false,
                    Message = "An error occurred while retrieving registered events"
                });
            }
        }
    }
}
