using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
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
        private readonly IWebHostEnvironment _environment;

        public EventsController(
            ApplicationDbContext context,
            ILogger<EventsController> logger,
            IWebHostEnvironment environment)
        {
            _context = context;
            _logger = logger;
            _environment = environment;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<Event>>> GetEvents()
        {
            try 
            {
                _logger.LogInformation("Starting GetEvents request");
                
                var events = await _context.Events
                    .Include(e => e.Organizer)
                    .Include(e => e.Registrations)
                    .ToListAsync();
                
                _logger.LogInformation("Successfully retrieved {Count} events", events.Count);
                return Ok(events);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting events");
                return StatusCode(500, new { message = "Internal server error while retrieving events", details = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Event>> GetEvent(int id)
        {
            try
            {
                _logger.LogInformation("Getting event with ID: {Id}", id);
                var @event = await _context.Events
                    .Include(e => e.Organizer)
                    .Include(e => e.Registrations)
                    .FirstOrDefaultAsync(e => e.Id == id);

                if (@event == null)
                {
                    _logger.LogWarning("Event with ID {Id} not found", id);
                    return NotFound();
                }

                return Ok(@event);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting event with ID {Id}", id);
                return StatusCode(500, new { message = "Internal server error while retrieving event", details = ex.Message });
            }
        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult<Event>> CreateEvent(Event @event)
        {
            try
            {
                _logger.LogInformation("Starting CreateEvent request");
                _logger.LogInformation("Raw event details: {@Event}", @event);

                // Get the current user's ID from claims
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("User ID not found in claims");
                    return Unauthorized(new { message = "User ID not found" });
                }

                _logger.LogInformation("Creating event for user {UserId}", userId);

                // Validate required fields
                if (string.IsNullOrEmpty(@event.Title))
                {
                    _logger.LogWarning("Title is required");
                    return BadRequest(new { message = "Title is required" });
                }

                if (string.IsNullOrEmpty(@event.Description))
                {
                    _logger.LogWarning("Description is required");
                    return BadRequest(new { message = "Description is required" });
                }

                if (string.IsNullOrEmpty(@event.Category))
                {
                    _logger.LogWarning("Category is required");
                    return BadRequest(new { message = "Category is required" });
                }

                if (string.IsNullOrEmpty(@event.Location))
                {
                    _logger.LogWarning("Location is required");
                    return BadRequest(new { message = "Location is required" });
                }

                // Set the organizer ID and created date
                @event.OrganizerId = userId;
                @event.CreatedAt = DateTime.UtcNow;

                // Convert local time to UTC using the provided time offset
                try 
                {
                    TimeSpan offset;
                    // Try to get the time zone offset from the request headers
                    if (Request.Headers.TryGetValue("X-TimeZone-Offset", out var tzOffset) && 
                        int.TryParse(tzOffset.FirstOrDefault(), out var offsetMinutes))
                    {
                        offset = TimeSpan.FromMinutes(offsetMinutes);
                    }
                    else
                    {
                        // Default to UTC+2 (Eastern European Time) if no offset provided
                        offset = TimeSpan.FromHours(2);
                    }

                    // Convert to UTC by subtracting the offset
                    var utcDateTime = @event.StartDate.Subtract(offset);
                    _logger.LogInformation("UTC DateTime after conversion: {UtcDateTime}", utcDateTime);

                    // Store the UTC date
                    @event.StartDate = DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);
                    _logger.LogInformation("Final UTC value - Date: {Date}, DateKind: {Kind}", 
                        @event.StartDate, @event.StartDate.Kind);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error converting time to UTC: {Message}", ex.Message);
                    return StatusCode(500, new { message = "Error processing date/time", details = ex.Message });
                }

                if (@event.StartDate <= DateTime.UtcNow)
                {
                    _logger.LogWarning("Invalid start date/time: {StartDateTime}. Must be in the future.", @event.StartDate);
                    return BadRequest(new { message = "Event start date/time must be in the future" });
                }

                if (@event.Price < 0)
                {
                    _logger.LogWarning("Invalid price: {Price}", @event.Price);
                    return BadRequest(new { message = "Price cannot be negative" });
                }

                if (@event.MaxAttendees < 0)
                {
                    _logger.LogWarning("Invalid max attendees: {MaxAttendees}", @event.MaxAttendees);
                    return BadRequest(new { message = "Maximum attendees cannot be negative" });
                }

                // Fix image URL if it contains escaped quotes
                if (!string.IsNullOrEmpty(@event.ImageUrl))
                {
                    @event.ImageUrl = @event.ImageUrl.Trim('"');
                    _logger.LogInformation("Processed image URL: {ImageUrl}", @event.ImageUrl);
                }

                _logger.LogInformation("Validated event data. Adding to database...");

                try
                {
                    _logger.LogInformation("Adding event to context");
                    _logger.LogInformation("Event data before save: {@Event}", new 
                    {
                        @event.Title,
                        @event.Description,
                        @event.Category,
                        @event.Location,
                        @event.StartDate,
                        StartDateKind = @event.StartDate.Kind,
                        @event.Price,
                        @event.MaxAttendees,
                        @event.OrganizerId,
                        @event.ImageUrl,
                        CombinedDateTime = @event.GetStartDateTime(),
                        CombinedDateTimeKind = @event.GetStartDateTime().Kind
                    });

                    _context.Events.Add(@event);
                    _logger.LogInformation("Saving changes to database");
                    
                    try
                    {
                        await _context.SaveChangesAsync();
                        _logger.LogInformation("Event saved successfully with ID: {EventId}", @event.Id);
                        _logger.LogInformation("Event saved successfully with details: {@Event}", @event);
                    }
                    catch (DbUpdateException dbEx)
                    {
                        var message = $"Database update error: {dbEx.Message}";
                        if (dbEx.InnerException != null)
                        {
                            message += $"\nInner exception: {dbEx.InnerException.Message}";
                            _logger.LogError(dbEx.InnerException, "Inner exception details");
                        }
                        _logger.LogError(dbEx, message);
                        return StatusCode(500, new { message = "Помилка бази даних", details = message });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Помилка під час збереження: {Message}", ex.Message);
                        return StatusCode(500, new { message = "Помилка збереження в базі даних", details = ex.Message });
                    }

                    try
                    {
                        await _context.Entry(@event)
                            .Reference(e => e.Organizer)
                            .LoadAsync();
                        _logger.LogInformation("Organizer details loaded successfully");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Помилка завантаження даних організатора: {Message}", ex.Message);
                        // Don't fail the request if loading organizer fails
                    }

                    return CreatedAtAction(nameof(GetEvent), new { id = @event.Id }, @event);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Необроблена помилка в CreateEvent: {Message}", ex.Message);
                    return StatusCode(500, new { message = "Необроблена помилка сервера", details = ex.Message });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating event. Exception details: {Message}", ex.Message);
                if (ex.InnerException != null)
                {
                    _logger.LogError("Inner exception: {Message}", ex.InnerException.Message);
                }
                return StatusCode(500, new { message = "An error occurred while creating the event", details = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateEvent(int id, Event @event)
        {
            _logger.LogInformation("Updating event with ID: {Id}", id);

            if (id != @event.Id)
            {
                _logger.LogWarning("ID mismatch: {Id} vs {EventId}", id, @event.Id);
                return BadRequest();
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("User ID not found in claims");
                return Unauthorized();
            }

            var existingEvent = await _context.Events
                .Include(e => e.Organizer)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (existingEvent == null)
            {
                _logger.LogWarning("Event not found with ID: {Id}", id);
                return NotFound();
            }

            if (existingEvent.OrganizerId != userId)
            {
                _logger.LogWarning("User {UserId} attempted to update event {EventId} owned by {OwnerId}", 
                    userId, id, existingEvent.OrganizerId);
                return Forbid();
            }

            // Convert local time to UTC using the provided time offset
            try 
            {
                TimeSpan offset;
                // Try to get the time zone offset from the request headers
                if (Request.Headers.TryGetValue("X-TimeZone-Offset", out var tzOffset) && 
                    int.TryParse(tzOffset.FirstOrDefault(), out var offsetMinutes))
                {
                    offset = TimeSpan.FromMinutes(offsetMinutes);
                }
                else
                {
                    // Default to UTC+2 (Eastern European Time) if no offset provided
                    offset = TimeSpan.FromHours(2);
                }

                // Convert to UTC by subtracting the offset
                var utcDateTime = @event.StartDate.Subtract(offset);
                _logger.LogInformation("UTC DateTime after conversion: {UtcDateTime}", utcDateTime);

                // Store the UTC date
                @event.StartDate = DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);
                _logger.LogInformation("Final UTC value - Date: {Date}, DateKind: {Kind}", 
                    @event.StartDate, @event.StartDate.Kind);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error converting time to UTC: {Message}", ex.Message);
                return StatusCode(500, new { message = "Error processing date/time", details = ex.Message });
            }

            // Update fields
            existingEvent.Title = @event.Title;
            existingEvent.Description = @event.Description;
            existingEvent.Category = @event.Category;
            existingEvent.Location = @event.Location;
            existingEvent.StartDate = @event.StartDate;
            existingEvent.Price = @event.Price;
            existingEvent.MaxAttendees = @event.MaxAttendees;
            if (!string.IsNullOrEmpty(@event.ImageUrl))
            {
                existingEvent.ImageUrl = @event.ImageUrl.Trim('"');
            }

            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("Event updated successfully");
            }
            catch (DbUpdateConcurrencyException ex)
            {
                if (!EventExists(id))
                {
                    _logger.LogWarning("Event not found during update: {Id}", id);
                    return NotFound();
                }
                else
                {
                    _logger.LogError(ex, "Concurrency error updating event: {Id}", id);
                    throw;
                }
            }

            return Ok(existingEvent);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEvent(int id)
        {
            try
            {
                _logger.LogInformation("Starting DeleteEvent request for ID: {Id}", id);
                
                var @event = await _context.Events.FindAsync(id);
                if (@event == null)
                {
                    _logger.LogWarning("Event with ID {Id} not found", id);
                    return NotFound();
                }

                _context.Events.Remove(@event);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Event deleted successfully");
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting event with ID {Id}", id);
                return StatusCode(500, new { message = "Internal server error while deleting event", details = ex.Message });
            }
        }

        [HttpPost("upload")]
        [Authorize]
        public async Task<ActionResult<string>> UploadEventImage()
        {
            try
            {
                _logger.LogInformation("Starting event image upload");

                var file = Request.Form.Files[0];
                if (file == null || file.Length == 0)
                {
                    _logger.LogWarning("No file was uploaded");
                    return BadRequest("No file was uploaded");
                }

                if (file.Length > 4096000) // 4MB limit
                {
                    _logger.LogWarning("File size exceeds limit");
                    return BadRequest("File size must be less than 4MB");
                }

                // Get file extension
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                if (!allowedExtensions.Contains(extension))
                {
                    _logger.LogWarning("Invalid file type: {Extension}", extension);
                    return BadRequest("Only image files (jpg, jpeg, png, gif) are allowed");
                }

                // Create uploads directory if it doesn't exist
                var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                if (!Directory.Exists(uploadsDir))
                {
                    Directory.CreateDirectory(uploadsDir);
                }

                // Generate unique filename
                var fileName = $"{Guid.NewGuid()}{extension}";
                var filePath = Path.Combine(uploadsDir, fileName);

                // Save file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Return URL path
                var url = $"/uploads/{fileName}";
                _logger.LogInformation("Image uploaded successfully: {Url}", url);
                return Ok(url);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading event image");
                return StatusCode(500, "Error uploading image");
            }
        }

        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<Event>>> SearchEvents([FromQuery] string q)
        {
            try
            {
                _logger.LogInformation("Starting search request with query: {Query}", q);
                
                if (string.IsNullOrWhiteSpace(q))
                {
                    return await GetEvents();
                }

                var searchTerm = q.ToLower();
                var events = await _context.Events
                    .Include(e => e.Organizer)
                    .Include(e => e.Registrations)
                    .Where(e =>
                        e.Title.ToLower().Contains(searchTerm) ||
                        e.Description.ToLower().Contains(searchTerm) ||
                        e.Location.ToLower().Contains(searchTerm) ||
                        e.Category.ToLower().Contains(searchTerm))
                    .ToListAsync();

                _logger.LogInformation("Search results: {Count} events", events.Count);
                return Ok(events);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching events");
                return StatusCode(500, new { message = "Internal server error while searching events", details = ex.Message });
            }
        }

        [HttpGet("upcoming")]
        public async Task<ActionResult<IEnumerable<Event>>> GetUpcomingEvents()
        {
            try
            {
                _logger.LogInformation("Starting GetUpcomingEvents request");
                
                var now = DateTime.UtcNow;
                var events = await _context.Events
                    .Include(e => e.Organizer)
                    .Include(e => e.Registrations)
                    .Where(e => e.StartDate > now)
                    .OrderBy(e => e.StartDate)
                    .ToListAsync();

                _logger.LogInformation("Upcoming events: {Count} events", events.Count);
                return Ok(events);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting upcoming events");
                return StatusCode(500, new { message = "Internal server error while retrieving upcoming events", details = ex.Message });
            }
        }

        [HttpGet("organizer/{organizerId}")]
        public async Task<ActionResult<IEnumerable<Event>>> GetEventsByOrganizer(string organizerId)
        {
            try
            {
                _logger.LogInformation("Starting GetEventsByOrganizer request for ID: {Id}", organizerId);
                
                var events = await _context.Events
                    .Include(e => e.Organizer)
                    .Include(e => e.Registrations)
                    .Where(e => e.OrganizerId == organizerId)
                    .OrderByDescending(e => e.StartDate)
                    .ToListAsync();

                _logger.LogInformation("Events by organizer: {Count} events", events.Count);
                return Ok(events);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting events by organizer with ID {Id}", organizerId);
                return StatusCode(500, new { message = "Internal server error while retrieving events by organizer", details = ex.Message });
            }
        }

        private bool EventExists(int id)
        {
            return _context.Events.Any(e => e.Id == id);
        }
    }
}
