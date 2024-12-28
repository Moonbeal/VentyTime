using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using VentyTime.Server.Data;
using VentyTime.Shared.Models;
using Microsoft.AspNetCore.Identity;

namespace VentyTime.Server.Services
{
    public interface IEventService
    {
        Task<(IEnumerable<Event> Events, int TotalCount)> GetEventsAsync(int page = 1, int pageSize = 10, string? category = null, DateTime? startDate = null, DateTime? endDate = null);
        Task<Event?> GetEventByIdAsync(int id);
        Task<Event> CreateEventAsync(Event @event, string userIdOrEmail);
        Task<Event> UpdateEventAsync(Event @event, string userId);
        Task DeleteEventAsync(int id, string userId);
        Task<IEnumerable<Event>> SearchEventsAsync(string query);
        Task<IEnumerable<Event>> GetUpcomingEventsAsync(int count);
        Task<bool> IsUserAuthorizedForEvent(int eventId, string userId, string[] allowedRoles);
        Task<IEnumerable<Event>> GetEventsByOrganizerAsync(string organizerId);
        Task<IEnumerable<string>> GetCategoriesAsync();
        Task<Registration> RegisterUserForEventAsync(int eventId, string userId);
        Task<IEnumerable<Event>> GetRegisteredEventsAsync(string userId);
    }

    public class EventService : IEventService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly ILogger<EventService> _logger;
        private readonly UserManager<ApplicationUser> _userManager;
        private const string EventsCacheKey = "AllEvents";
        private const string CategoriesCacheKey = "AllCategories";
        private const string EventCacheKeyPrefix = "Event_";
        private const int CacheExpirationMinutes = 5;

        public EventService(
            ApplicationDbContext context,
            IMemoryCache cache,
            ILogger<EventService> logger,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _cache = cache;
            _logger = logger;
            _userManager = userManager;
        }

        public async Task<(IEnumerable<Event> Events, int TotalCount)> GetEventsAsync(
            int page = 1, 
            int pageSize = 10, 
            string? category = null, 
            DateTime? startDate = null, 
            DateTime? endDate = null)
        {
            try
            {
                _logger.LogInformation("Getting events with pagination and filters");

                var query = _context.Events
                    .Include(e => e.Organizer)
                    .Include(e => e.Registrations)
                    .AsQueryable();

                // Apply filters
                if (!string.IsNullOrEmpty(category))
                {
                    query = query.Where(e => e.Category == category);
                }

                if (startDate.HasValue)
                {
                    query = query.Where(e => e.StartDate >= startDate.Value);
                }

                if (endDate.HasValue)
                {
                    query = query.Where(e => e.StartDate <= endDate.Value);
                }

                // Get total count for pagination
                var totalCount = await query.CountAsync();

                // Apply pagination
                var events = await query
                    .OrderByDescending(e => e.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                _logger.LogInformation("Retrieved {Count} events from page {Page}", events.Count, page);

                return (events, totalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting events");
                throw;
            }
        }

        public async Task<Event?> GetEventByIdAsync(int id)
        {
            try
            {
                var cacheKey = $"{EventCacheKeyPrefix}{id}";

                if (_cache.TryGetValue(cacheKey, out Event? cachedEvent))
                {
                    _logger.LogInformation("Retrieved event {EventId} from cache", id);
                    return cachedEvent;
                }

                var @event = await _context.Events
                    .Include(e => e.Organizer)
                    .Include(e => e.Registrations)
                    .FirstOrDefaultAsync(e => e.Id == id);

                if (@event != null)
                {
                    var cacheOptions = new MemoryCacheEntryOptions()
                        .SetAbsoluteExpiration(TimeSpan.FromMinutes(CacheExpirationMinutes));

                    _cache.Set(cacheKey, @event, cacheOptions);
                    _logger.LogInformation("Cached event {EventId}", id);
                }

                return @event;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting event {EventId}", id);
                throw;
            }
        }

        public async Task<IEnumerable<string>> GetCategoriesAsync()
        {
            try
            {
                if (_cache.TryGetValue(CategoriesCacheKey, out List<string>? cachedCategories) && cachedCategories != null)
                {
                    return cachedCategories;
                }

                var categories = await _context.Events
                    .Where(e => e.Category != null)
                    .Select(e => e.Category!)
                    .Distinct()
                    .ToListAsync();

                var cacheOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromHours(1));

                _cache.Set(CategoriesCacheKey, categories, cacheOptions);

                return categories;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting categories");
                throw;
            }
        }

        public async Task<Event> CreateEventAsync(Event @event, string userIdOrEmail)
        {
            if (@event == null) throw new ArgumentNullException(nameof(@event));
            if (string.IsNullOrEmpty(userIdOrEmail)) throw new ArgumentException("User ID/Email cannot be empty", nameof(userIdOrEmail));

            try
            {
                _logger.LogInformation("Creating event by user {UserIdOrEmail} with date {StartDate}", userIdOrEmail, @event.StartDate);

                // Validate required fields
                if (string.IsNullOrEmpty(@event.Title))
                    throw new ArgumentException("Event title is required");
                if (string.IsNullOrEmpty(@event.Description))
                    throw new ArgumentException("Event description is required");
                if (string.IsNullOrEmpty(@event.Location))
                    throw new ArgumentException("Event location is required");
                if (string.IsNullOrEmpty(@event.Category))
                    throw new ArgumentException("Event category is required");
                if (@event.StartDate == default)
                    throw new ArgumentException("Event start date is required");
                if (@event.EndDate == default)
                    throw new ArgumentException("Event end date is required");
                if (@event.MaxAttendees <= 0)
                    throw new ArgumentException("Event max attendees must be greater than 0");

                // Convert dates to UTC
                if (@event.StartDate.Kind != DateTimeKind.Utc)
                {
                    _logger.LogInformation("Converting StartDate from {Kind} to UTC", @event.StartDate.Kind);
                    @event.StartDate = @event.StartDate.ToUniversalTime();
                }

                if (@event.EndDate.Kind != DateTimeKind.Utc)
                {
                    _logger.LogInformation("Converting EndDate from {Kind} to UTC", @event.EndDate.Kind);
                    @event.EndDate = @event.EndDate.ToUniversalTime();
                }

                // Set StartTime from StartDate
                @event.StartTime = @event.StartDate.TimeOfDay;

                // Load and set the creator/organizer
                var user = await _userManager.FindByIdAsync(userIdOrEmail) ?? 
                    await _userManager.FindByEmailAsync(userIdOrEmail) ??
                    throw new InvalidOperationException("User not found");

                @event.CreatorId = user.Id;
                @event.OrganizerId = user.Id;
                @event.Creator = user;
                @event.Organizer = user;

                @event.CreatedAt = DateTime.UtcNow;
                @event.UpdatedAt = null;
                @event.IsActive = true;
                @event.CurrentCapacity = 0;

                // Ensure EndDate is at least StartDate plus one hour if not set
                if (@event.EndDate <= @event.StartDate)
                {
                    @event.EndDate = @event.StartDate.Add(TimeSpan.FromHours(1));
                }

                _logger.LogInformation("Saving event with UTC dates - Start: {StartDate}, End: {EndDate}, StartTime: {StartTime}", 
                    @event.StartDate, @event.EndDate, @event.StartTime);

                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    _context.Events.Add(@event);
                    try
                    {
                        await _context.SaveChangesAsync();
                    }
                    catch (DbUpdateException dbEx)
                    {
                        _logger.LogError(dbEx, "Database error details: {Message}", dbEx.Message);
                        if (dbEx.InnerException != null)
                        {
                            _logger.LogError("Inner exception details: {Message}", dbEx.InnerException.Message);
                            _logger.LogError("Inner exception stack trace: {StackTrace}", dbEx.InnerException.StackTrace);
                        }

                        var entries = _context.ChangeTracker.Entries()
                            .Where(e => e.State is EntityState.Added or EntityState.Modified)
                            .Select(e => new 
                            { 
                                Entity = e.Entity.GetType().Name,
                                e.State,
                                Properties = e.CurrentValues.Properties
                                    .Select(p => new { p.Name, Value = e.CurrentValues[p] })
                            });

                        _logger.LogError("Change tracker entries: {@Entries}", entries);
                        throw;
                    }
                    await transaction.CommitAsync();

                    InvalidateEventCache();
                    _logger.LogInformation("Successfully created event {EventId}", @event.Id);

                    return @event;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Error saving event to database: {Error}", ex.Message);
                    if (ex.InnerException != null)
                    {
                        _logger.LogError("Inner exception: {Error}", ex.InnerException.Message);
                        _logger.LogError("Inner exception stack trace: {StackTrace}", ex.InnerException.StackTrace);
                    }
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating event: {Error}", ex.Message);
                if (ex.InnerException != null)
                {
                    _logger.LogError("Inner exception: {Error}", ex.InnerException.Message);
                }
                throw;
            }
        }

        public async Task<Event> UpdateEventAsync(Event @event, string userId)
        {
            try
            {
                _logger.LogInformation("Updating event {EventId} for user {UserId}", @event.Id, userId);
                
                var existingEvent = await _context.Events.FindAsync(@event.Id) ?? 
                    throw new KeyNotFoundException($"Event with ID {@event.Id} not found");

                if (!await IsUserAuthorizedForEvent(@event.Id, userId, new[] { "Admin", "Organizer" }))
                {
                    throw new UnauthorizedAccessException("User is not authorized to update this event");
                }

                @event.OrganizerId = existingEvent.OrganizerId;
                @event.CreatedAt = existingEvent.CreatedAt;
                @event.UpdatedAt = DateTime.UtcNow;

                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    _context.Entry(existingEvent).CurrentValues.SetValues(@event);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    InvalidateEventCache(@event.Id);
                    _logger.LogInformation("Successfully updated event {EventId}", @event.Id);

                    return @event;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Error updating event {EventId}", @event.Id);
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating event");
                throw;
            }
        }

        public async Task DeleteEventAsync(int id, string userId)
        {
            try
            {
                _logger.LogInformation("Deleting event {EventId} for user {UserId}", id, userId);
                
                var @event = await _context.Events
                    .Include(e => e.Registrations)
                    .FirstOrDefaultAsync(e => e.Id == id) ?? 
                    throw new KeyNotFoundException($"Event with ID {id} not found");

                if (!await IsUserAuthorizedForEvent(id, userId, new[] { "Admin", "Organizer" }))
                {
                    throw new UnauthorizedAccessException("User is not authorized to delete this event");
                }

                if (@event.Registrations?.Any(r => r.Status == RegistrationStatus.Confirmed) == true)
                {
                    throw new InvalidOperationException("Cannot delete event with confirmed registrations");
                }

                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    if (@event.Registrations != null)
                    {
                        _context.Registrations.RemoveRange(@event.Registrations);
                    }

                    _context.Events.Remove(@event);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    InvalidateEventCache(id);
                    _logger.LogInformation("Successfully deleted event {EventId}", id);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Error deleting event {EventId}", id);
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting event");
                throw;
            }
        }

        public async Task<IEnumerable<Event>> SearchEventsAsync(string query)
        {
            try
            {
                _logger.LogInformation("Searching events with query: {Query}", query);
                
                var cacheKey = $"Search_{query.ToLower()}";
                if (_cache.TryGetValue(cacheKey, out List<Event>? cachedResults))
                {
                    _logger.LogInformation("Retrieved {Count} search results from cache", cachedResults?.Count ?? 0);
                    return cachedResults ??= new List<Event>();
                }

                query = query.ToLower();
                var events = await _context.Events
                    .Include(e => e.Organizer)
                    .Include(e => e.Registrations)
                    .Where(e => e.IsActive &&
                           (e.Title.ToLower().Contains(query) ||
                            e.Description.ToLower().Contains(query) ||
                            e.Category.ToLower().Contains(query) ||
                            e.Location.ToLower().Contains(query)))
                    .AsNoTracking()
                    .ToListAsync();

                var cacheOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromMinutes(5));
                
                _cache.Set(cacheKey, events, cacheOptions);
                _logger.LogInformation("Retrieved and cached {Count} search results from database", events.Count);

                return events;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching events");
                throw;
            }
        }

        public async Task<IEnumerable<Event>> GetUpcomingEventsAsync(int count)
        {
            try
            {
                _logger.LogInformation("Getting upcoming events with count: {Count}", count);
                
                if (count <= 0 || count > 50)
                {
                    throw new ArgumentException("Count must be between 1 and 50", nameof(count));
                }

                var cacheKey = $"UpcomingEvents_{count}";
                if (_cache.TryGetValue(cacheKey, out List<Event>? cachedEvents))
                {
                    _logger.LogInformation("Retrieved {Count} upcoming events from cache", cachedEvents?.Count ?? 0);
                    return cachedEvents ??= new List<Event>();
                }

                var now = DateTime.UtcNow;
                var events = await _context.Events
                    .Include(e => e.Registrations)
                    .Where(e => e.IsActive && e.StartDate > now)
                    .OrderBy(e => e.StartDate)
                    .Take(count)
                    .AsNoTracking()
                    .ToListAsync();

                var cacheOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromMinutes(5));
                
                _cache.Set(cacheKey, events, cacheOptions);
                _logger.LogInformation("Retrieved and cached {Count} upcoming events from database", events.Count);

                return events;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting upcoming events");
                throw;
            }
        }

        public async Task<bool> IsUserAuthorizedForEvent(int eventId, string userId, string[] allowedRoles)
        {
            try
            {
                _logger.LogInformation("Checking user authorization for event {EventId} and user {UserId}", eventId, userId);
                
                var @event = await _context.Events
                    .AsNoTracking()
                    .FirstOrDefaultAsync(e => e.Id == eventId);

                if (@event == null)
                {
                    return false;
                }

                // Власник події завжди має доступ
                if (@event.OrganizerId == userId)
                {
                    return true;
                }

                // Перевіряємо ролі
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return false;
                }

                var userRoles = await _userManager.GetRolesAsync(user);
                return userRoles.Any(role => allowedRoles.Contains(role));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking user authorization for event");
                throw;
            }
        }

        public async Task<IEnumerable<Event>> GetEventsByOrganizerAsync(string organizerId)
        {
            try
            {
                _logger.LogInformation("Getting events for organizer {OrganizerId}", organizerId);
                
                return await _context.Events
                    .Include(e => e.Organizer)
                    .Include(e => e.Registrations)
                    .Where(e => e.OrganizerId == organizerId)
                    .OrderByDescending(e => e.StartDate)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting events for organizer {OrganizerId}", organizerId);
                throw;
            }
        }

        public async Task<Registration> RegisterUserForEventAsync(int eventId, string userId)
        {
            // Load event with active registrations
            var @event = await _context.Events
                .Include(e => e.Registrations)
                .FirstOrDefaultAsync(e => e.Id == eventId);

            if (@event == null)
            {
                _logger.LogError("Event {EventId} not found during registration", eventId);
                throw new InvalidOperationException("Event not found");
            }

            // Check if event is full
            var activeRegistrations = @event.Registrations?
                .Count(r => r.Status == RegistrationStatus.Confirmed) ?? 0;

            if (activeRegistrations >= @event.MaxAttendees)
            {
                _logger.LogWarning("Event {EventId} is full. Cannot register user {UserId}", eventId, userId);
                throw new InvalidOperationException("Event is full");
            }

            // Check if user is already registered
            var existingRegistration = @event.Registrations?
                .FirstOrDefault(r => r.UserId == userId && r.Status != RegistrationStatus.Cancelled);

            if (existingRegistration != null)
            {
                _logger.LogWarning("User {UserId} is already registered for event {EventId}", userId, eventId);
                throw new InvalidOperationException("You are already registered for this event");
            }

            try
            {
                var registration = new Registration
                {
                    EventId = eventId,
                    UserId = userId,
                    Status = RegistrationStatus.Confirmed,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Registrations.Add(registration);
                await _context.SaveChangesAsync();

                _logger.LogInformation("User {UserId} successfully registered for event {EventId}", userId, eventId);
                return registration;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error registering user {UserId} for event {EventId}", userId, eventId);
                throw new InvalidOperationException("You are already registered for this event.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering user {UserId} for event {EventId}", userId, eventId);
                throw;
            }
        }

        public async Task<IEnumerable<Event>> GetRegisteredEventsAsync(string userId)
        {
            return await _context.Events
                .Include(e => e.Organizer)
                .Include(e => e.Registrations)
                .Where(e => e.Registrations != null && e.Registrations.Any(r => r.UserId == userId && r.Status == RegistrationStatus.Confirmed))
                .OrderBy(e => e.StartDate)
                .ToListAsync();
        }

        public async Task<bool> HasAvailableSpacesAsync(int eventId)
        {
            var eventItem = await _context.Events
                .Include(e => e.Registrations)
                .FirstOrDefaultAsync(e => e.Id == eventId);

            if (eventItem == null || eventItem.Registrations == null)
                return false;

            var activeRegistrations = eventItem.Registrations
                .Count(r => r.Status == RegistrationStatus.Confirmed);

            return activeRegistrations < eventItem.MaxAttendees;
        }

        private void InvalidateEventCache()
        {
            _cache.Remove(EventsCacheKey);
            _cache.Remove(CategoriesCacheKey);
        }

        private void InvalidateEventCache(int eventId)
        {
            _cache.Remove($"{EventCacheKeyPrefix}{eventId}");
            InvalidateEventCache();
        }
    }
}
