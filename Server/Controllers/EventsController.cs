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
        public async Task<ActionResult<IEnumerable<Event>>> GetEvents()
        {
            return await _context.Events
                .Include(e => e.Organizer)
                .Include(e => e.Registrations)
                .ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Event>> GetEvent(int id)
        {
            var @event = await _context.Events
                .Include(e => e.Organizer)
                .Include(e => e.Registrations)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (@event == null)
            {
                return NotFound();
            }

            return @event;
        }

        [Authorize(Policy = "RequireOrganizerRole")]
        [HttpPost]
        public async Task<ActionResult<Event>> CreateEvent(Event @event)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("User ID not found in token");
                    return Unauthorized();
                }

                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
                _logger.LogInformation($"User Role from token: {userRole}");

                if (userRole != UserRole.Organizer.ToString() && userRole != UserRole.Admin.ToString())
                {
                    _logger.LogWarning($"User with role {userRole} attempted to create event");
                    return Forbid();
                }

                @event.OrganizerId = userId;
                _context.Events.Add(@event);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetEvent), new { id = @event.Id }, @event);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating event");
                return StatusCode(500, "An error occurred while creating the event");
            }
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
                return Forbid();
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
                return Forbid();
            }

            _context.Events.Remove(@event);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [Authorize]
        [HttpPost("upload-image")]
        public async Task<IActionResult> UploadImage([FromForm] IFormFile file)
        {
            try
            {
                _logger.LogInformation($"Starting image upload. Content type: {file?.ContentType}, Length: {file?.Length}");

                if (file == null || file.Length == 0)
                {
                    _logger.LogWarning("No file uploaded");
                    return BadRequest("No file uploaded");
                }

                // Check file type
                var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png" };
                if (!allowedTypes.Contains(file.ContentType.ToLower()))
                {
                    _logger.LogWarning($"Invalid file type: {file.ContentType}");
                    return BadRequest("Invalid file type. Only JPG and PNG files are allowed.");
                }

                // Check file size (max 4MB)
                if (file.Length > 4 * 1024 * 1024)
                {
                    _logger.LogWarning($"File size too large: {file.Length} bytes");
                    return BadRequest("File size exceeds maximum limit of 4MB");
                }

                // Create uploads directory if it doesn't exist
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads");
                _logger.LogInformation($"Uploads folder path: {uploadsFolder}");
                
                if (!Directory.Exists(uploadsFolder))
                {
                    _logger.LogInformation("Creating uploads directory");
                    Directory.CreateDirectory(uploadsFolder);
                }

                // Generate unique filename
                var uniqueFileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                _logger.LogInformation($"Saving file to: {filePath}");

                // Save file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Return the URL to the uploaded file
                var baseUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}";
                var imageUrl = $"{baseUrl}/uploads/{uniqueFileName}";
                _logger.LogInformation($"File uploaded successfully. URL: {imageUrl}");

                return Ok(imageUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading image");
                return StatusCode(500, "An error occurred while uploading the image");
            }
        }

        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<Event>>> SearchEvents([FromQuery] string q)
        {
            if (string.IsNullOrWhiteSpace(q))
            {
                return await GetEvents();
            }

            var searchTerm = q.ToLower();
            return await _context.Events
                .Include(e => e.Organizer)
                .Include(e => e.Registrations)
                .Where(e => 
                    e.Title.ToLower().Contains(searchTerm) ||
                    e.Description.ToLower().Contains(searchTerm) ||
                    e.Location.ToLower().Contains(searchTerm) ||
                    e.Category.ToLower().Contains(searchTerm))
                .ToListAsync();
        }

        [HttpGet("upcoming")]
        public async Task<ActionResult<IEnumerable<Event>>> GetUpcomingEvents()
        {
            var now = DateTime.UtcNow;
            return await _context.Events
                .Include(e => e.Organizer)
                .Include(e => e.Registrations)
                .Where(e => e.StartDate > now)
                .OrderBy(e => e.StartDate)
                .ToListAsync();
        }

        [Authorize]
        [HttpGet("organizer/{organizerId}")]
        public async Task<ActionResult<IEnumerable<Event>>> GetEventsByOrganizer(string organizerId)
        {
            return await _context.Events
                .Include(e => e.Organizer)
                .Include(e => e.Registrations)
                .Where(e => e.OrganizerId == organizerId)
                .OrderByDescending(e => e.StartDate)
                .ToListAsync();
        }

        private bool EventExists(int id)
        {
            return _context.Events.Any(e => e.Id == id);
        }
    }
}
