using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using VentyTime.Server.Data;
using VentyTime.Shared.Models;

namespace VentyTime.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class EventsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<EventsController> _logger;
        private readonly UserManager<ApplicationUser> _userManager;

        public EventsController(
            ApplicationDbContext context,
            ILogger<EventsController> logger,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _logger = logger;
            _userManager = userManager;
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
                _logger.LogError(ex, "Error occurred while getting events: {Message}", ex.Message);
                return StatusCode(500, new { message = "Internal server error while retrieving events", details = ex.Message });
            }
        }

        [HttpGet("{id}")]
        [AllowAnonymous] // Keep this endpoint anonymous
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
                _logger.LogError(ex, "Error getting event {Id}: {Message}", id, ex.Message);
                return StatusCode(500, new { message = "Internal server error while retrieving event", details = ex.Message });
            }
        }

        [HttpPost]
        public async Task<ActionResult<Event>> CreateEvent([FromBody] Event @event)
        {
            _logger.LogInformation("=== Початок створення події ===");
            _logger.LogInformation("Метод: {Method}", Request.Method);
            _logger.LogInformation("Шлях: {Path}", Request.Path);
            _logger.LogInformation("Content-Type: {ContentType}", Request.ContentType);
            _logger.LogInformation("Authorization: {Auth}", Request.Headers.Authorization.ToString());

            try 
            {
                _logger.LogInformation("Початок створення події. Метод: {Method}, Шлях: {Path}", Request.Method, Request.Path);
                _logger.LogInformation("Отримані дані події: {@Event}", @event);

                // Базова валідація
                if (@event == null)
                {
                    _logger.LogError("Отримано null замість об'єкту події");
                    return BadRequest(new { message = "Дані події відсутні" });
                }

                // Валідація обов'язкових полів
                var validationErrors = new List<string>();
                
                if (string.IsNullOrWhiteSpace(@event.Title))
                    validationErrors.Add("Назва події обов'язкова");
                
                if (string.IsNullOrWhiteSpace(@event.Description))
                    validationErrors.Add("Опис події обов'язковий");
                
                if (string.IsNullOrWhiteSpace(@event.Category))
                    validationErrors.Add("Категорія події обов'язкова");
                
                if (string.IsNullOrWhiteSpace(@event.Location))
                    validationErrors.Add("Місце проведення обов'язкове");

                if (@event.StartDate == default)
                    validationErrors.Add("Дата початку події обов'язкова");

                if (@event.Price < 0)
                    validationErrors.Add("Ціна не може бути від'ємною");

                if (@event.MaxAttendees <= 0)
                    validationErrors.Add("Максимальна кількість учасників повинна бути більше 0");

                if (validationErrors.Any())
                {
                    _logger.LogError("Помилки валідації: {@Errors}", validationErrors);
                    return BadRequest(new { message = "Помилки валідації", errors = validationErrors });
                }

                // Get the current user's ID from claims
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                _logger.LogInformation("Отримано ID користувача: {UserId}", userId);

                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogError("Не вдалося отримати ID користувача");
                    return Unauthorized(new { message = "Користувач не авторизований" });
                }

                // Перевіряємо чи існує користувач
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogError("Користувача з ID {UserId} не знайдено", userId);
                    return NotFound(new { message = "Користувача не знайдено" });
                }

                _logger.LogInformation("Знайдено користувача: {UserEmail}", user.Email);

                TimeSpan offset;
                // Отримуємо зміщення часового поясу з заголовків
                if (Request.Headers.TryGetValue("X-TimeZone-Offset", out var tzOffset))
                {
                    _logger.LogInformation("Отримано зміщення часового поясу з заголовків: {Offset}", tzOffset.ToString());
                    if (int.TryParse(tzOffset.FirstOrDefault(), out var offsetMinutes))
                    {
                        offset = TimeSpan.FromMinutes(offsetMinutes);
                        _logger.LogInformation("Розпарсене зміщення часового поясу: {OffsetMinutes} хвилин", offsetMinutes);
                    }
                    else
                    {
                        _logger.LogWarning("Не вдалося розпарсити значення зміщення часового поясу: {Value}", tzOffset.ToString());
                        offset = TimeSpan.FromHours(2);
                        _logger.LogInformation("Використовуємо зміщення за замовчуванням +2 години");
                    }
                }
                else
                {
                    _logger.LogWarning("Зміщення часового поясу не знайдено в заголовках");
                    offset = TimeSpan.FromHours(2);
                    _logger.LogInformation("Використовуємо зміщення за замовчуванням +2 години");
                }

                _logger.LogInformation("Вхідний локальний час: {LocalTime}", @event.StartDate);
                _logger.LogInformation("Вхідний Kind: {Kind}", @event.StartDate.Kind);
                
                try
                {
                    // Спочатку перевіряємо, чи дата вже не в UTC
                    if (@event.StartDate.Kind == DateTimeKind.Utc)
                    {
                        _logger.LogInformation("Дата вже в UTC форматі, пропускаємо конвертацію");
                    }
                    else
                    {
                        // Якщо дата в локальному форматі або невизначена, конвертуємо її
                        var localDateTime = DateTime.SpecifyKind(@event.StartDate, DateTimeKind.Local);
                        _logger.LogInformation("Локальний час з встановленим Kind: {LocalTime} ({Kind})", 
                            localDateTime, localDateTime.Kind);

                        // Конвертуємо в UTC
                        var utcDateTime = localDateTime.ToUniversalTime();
                        _logger.LogInformation("UTC час після конвертації: {UtcTime}", utcDateTime);
                        
                        @event.StartDate = DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);
                        _logger.LogInformation("Фінальне UTC значення: {UtcTime} ({Kind})", 
                            @event.StartDate, @event.StartDate.Kind);
                    }

                    if (@event.StartDate == default)
                    {
                        _logger.LogError("StartDate має значення за замовчуванням після конвертації");
                        return StatusCode(500, new { message = "Некоректне значення дати/часу після конвертації" });
                    }

                    // Перевіряємо, чи дата в майбутньому
                    if (@event.StartDate <= DateTime.UtcNow)
                    {
                        _logger.LogError("Дата події повинна бути в майбутньому. Поточна дата: {CurrentDate}, Дата події: {EventDate}", 
                            DateTime.UtcNow, @event.StartDate);
                        return BadRequest(new { message = "Дата події повинна бути в майбутньому" });
                    }
                }
                catch (Exception timeEx)
                {
                    _logger.LogError(timeEx, "Помилка при конвертації часу: {Message}", timeEx.Message);
                    return StatusCode(500, new { message = $"Помилка при конвертації часу: {timeEx.Message}" });
                }

                // Set the organizer
                @event.OrganizerId = userId;

                @event.CreatedAt = DateTime.UtcNow;

                try
                {
                    _logger.LogInformation("Спроба додати подію до бази даних");
                    _logger.LogInformation("Значення StartDate: {StartDate}", @event.StartDate);
                    
                    // Переконуємося, що StartTime встановлено
                    @event.StartTime = @event.StartDate;
                    _logger.LogInformation("Значення StartTime: {StartTime}", @event.StartTime);
                    
                    _context.Events.Add(@event);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Подію успішно додано до бази даних. EventId: {@EventId}", @event.Id);
                    return Ok(@event);
                }
                catch (DbUpdateException dbEx)
                {
                    _logger.LogError(dbEx, "Помилка оновлення бази даних: {Message}", dbEx.Message);
                    if (dbEx.InnerException != null)
                    {
                        _logger.LogError(dbEx.InnerException, "Внутрішня помилка: {Message}", dbEx.InnerException.Message);
                    }
                    return StatusCode(500, new { message = "Помилка при збереженні в базу даних", details = dbEx.Message });
                }
                catch (Exception dbEx)
                {
                    _logger.LogError(dbEx, "Помилка при збереженні події в базу даних: {Message}", dbEx.Message);
                    return StatusCode(500, new { message = $"Помилка при збереженні події: {dbEx.Message}" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Загальна помилка при створенні події: {Message}", ex.Message);
                return StatusCode(500, new { message = $"Помилка при створенні події: {ex.Message}" });
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
                if (Request.Headers.TryGetValue("X-TimeZone-Offset", out var tzOffset))
                {
                    _logger.LogInformation("Отримано зміщення часового поясу з заголовків: {Offset}", tzOffset.ToString());
                    if (int.TryParse(tzOffset.FirstOrDefault(), out var offsetMinutes))
                    {
                        offset = TimeSpan.FromMinutes(offsetMinutes);
                        _logger.LogInformation("Розпарсене зміщення часового поясу: {OffsetMinutes} хвилин", offsetMinutes);
                    }
                    else
                    {
                        _logger.LogWarning("Не вдалося розпарсити значення зміщення часового поясу: {Value}", tzOffset.ToString());
                        offset = TimeSpan.FromHours(2);
                        _logger.LogInformation("Використовуємо зміщення за замовчуванням +2 години");
                    }
                }
                else
                {
                    _logger.LogWarning("Зміщення часового поясу не знайдено в заголовках");
                    offset = TimeSpan.FromHours(2);
                    _logger.LogInformation("Використовуємо зміщення за замовчуванням +2 години");
                }

                _logger.LogInformation("Вхідний локальний час: {LocalTime}", @event.StartDate);
                _logger.LogInformation("Вхідний Kind: {Kind}", @event.StartDate.Kind);
                
                // Спочатку перевіряємо, чи дата вже не в UTC
                if (@event.StartDate.Kind == DateTimeKind.Utc)
                {
                    _logger.LogInformation("Дата вже в UTC форматі, пропускаємо конвертацію");
                    // Нічого не робимо, дата вже в UTC
                }
                else
                {
                    // Якщо дата в локальному форматі або невизначена, конвертуємо її
                    var localDateTime = DateTime.SpecifyKind(@event.StartDate, DateTimeKind.Local);
                    _logger.LogInformation("Локальний час з встановленим Kind: {LocalTime} ({Kind})", 
                        localDateTime, localDateTime.Kind);

                    // Конвертуємо в UTC
                    var utcDateTime = localDateTime.ToUniversalTime();
                    _logger.LogInformation("UTC час після конвертації: {UtcTime}", utcDateTime);
                    
                    @event.StartDate = DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);
                    _logger.LogInformation("Фінальне UTC значення: {UtcTime} ({Kind})", 
                        @event.StartDate, @event.StartDate.Kind);
                }

                if (@event.StartDate == default)
                {
                    _logger.LogError("StartDate має значення за замовчуванням після конвертації");
                    return StatusCode(500, new { message = "Некоректне значення дати/часу після конвертації" });
                }
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
                _logger.LogError(ex, "Error deleting event with ID {Id}: {Message}", id, ex.Message);
                return StatusCode(500, new { message = "Internal server error while deleting event", details = ex.Message });
            }
        }

        [Authorize]
        [HttpPost("upload-image")]
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
                _logger.LogError(ex, "Error uploading event image: {Message}", ex.Message);
                return StatusCode(500, new { message = "Error uploading image", details = ex.Message });
            }
        }

        [HttpGet("search")]
        [AllowAnonymous] // Keep this endpoint anonymous
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
                _logger.LogError(ex, "Error searching events: {Query}, {Message}", q, ex.Message);
                return StatusCode(500, new { message = "Internal server error while searching events", details = ex.Message });
            }
        }

        [HttpGet("upcoming")]
        [AllowAnonymous] // Keep this endpoint anonymous
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
                _logger.LogError(ex, "Error getting upcoming events: {Message}", ex.Message);
                return StatusCode(500, new { message = "Internal server error while retrieving upcoming events", details = ex.Message });
            }
        }

        [HttpGet("organizer/{organizerId}")]
        [AllowAnonymous] // Keep this endpoint anonymous
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
                _logger.LogError(ex, "Error getting events for organizer {OrganizerId}: {Message}", organizerId, ex.Message);
                return StatusCode(500, new { message = "Internal server error while retrieving events by organizer", details = ex.Message });
            }
        }

        private bool EventExists(int id)
        {
            return _context.Events.Any(e => e.Id == id);
        }
    }
}
