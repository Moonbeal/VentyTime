using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using VentyTime.Server.Data;
using VentyTime.Shared.Models;
using Microsoft.AspNetCore.Identity;

namespace VentyTime.Server.Services
{
    public interface IEventService
    {
        Task<List<Event>> GetEventsAsync();
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
        Task<IEnumerable<Event>> GetPopularEventsAsync();
        Task SeedTestEventsAsync();
        Task<IEnumerable<Registration>> GetEventRegistrationsAsync(int eventId);
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

        public async Task<List<Event>> GetEventsAsync()
        {
            try
            {
                _logger.LogInformation("Getting all events");

                var events = await _context.Events
                    .Include(e => e.Organizer)
                    .Include(e => e.Registrations)
                    .OrderByDescending(e => e.CreatedAt)
                    .ToListAsync();

                _logger.LogInformation("Retrieved {Count} events", events.Count);

                return events;
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

                // Check if user is admin
                var user = await _userManager.FindByIdAsync(userId) ?? throw new UnauthorizedAccessException("User not found");

                var userRoles = await _userManager.GetRolesAsync(user);
                bool isAdmin = userRoles.Contains("Admin");

                // Check if user is the organizer of this event
                bool isOrganizer = existingEvent.OrganizerId == userId;

                if (!isAdmin && !isOrganizer)
                {
                    _logger.LogWarning("User {UserId} attempted to update event {EventId} without permission", userId, @event.Id);
                    throw new UnauthorizedAccessException("Only administrators and event organizers can update this event");
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

                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // Видаляємо всі реєстрації, якщо вони є
                    if (@event.Registrations != null && @event.Registrations.Any())
                    {
                        _context.Registrations.RemoveRange(@event.Registrations);
                    }

                    _context.Events.Remove(@event);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    InvalidateEventCache(id);
                    _logger.LogInformation("Successfully deleted event {EventId} and all its registrations", id);
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

                // First check if user is admin
                var user = await _userManager.FindByIdAsync(userId) ?? throw new UnauthorizedAccessException("User not found");

                var userRoles = await _userManager.GetRolesAsync(user);
                if (userRoles.Contains("Admin"))
                {
                    _logger.LogInformation("User {UserId} is admin, granting access", userId);
                    return true;
                }

                var @event = await _context.Events
                    .AsNoTracking()
                    .FirstOrDefaultAsync(e => e.Id == eventId);

                if (@event == null)
                {
                    _logger.LogWarning("Event {EventId} not found", eventId);
                    return false;
                }

                // Check if user is the organizer
                if (@event.OrganizerId == userId)
                {
                    _logger.LogInformation("User {UserId} is the organizer of event {EventId}", userId, eventId);
                    return true;
                }

                // Check other roles
                return userRoles.Any(role => allowedRoles.Contains(role));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking user authorization for event {EventId} and user {UserId}", eventId, userId);
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
            try
            {
                _logger.LogInformation("Getting registered events for user {UserId}", userId);

                var events = await _context.Events
                    .Include(e => e.Organizer)
                    .Include(e => e.Registrations)
                    .Where(e => e.Registrations != null && 
                               e.Registrations.Any(r => r.UserId == userId && 
                                                      r.Status == RegistrationStatus.Confirmed))
                    .OrderBy(e => e.StartDate)
                    .ToListAsync();

                _logger.LogInformation("Found {Count} registered events for user {UserId}", events.Count, userId);
                return events;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting registered events for user {UserId}", userId);
                throw;
            }
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

        public async Task<IEnumerable<Event>> GetPopularEventsAsync()
        {
            const string cacheKey = "popular_events";
            
            if (_cache.TryGetValue(cacheKey, out IEnumerable<Event>? cachedEvents) && cachedEvents is not null)
            {
                return cachedEvents;
            }

            var events = await _context.Events
                .Include(e => e.Registrations)
                .OrderByDescending(e => e.Registrations != null ? e.Registrations.Count : 0)
                .Take(10)
                .ToListAsync();

            var cacheOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(30));

            _cache.Set(cacheKey, events, cacheOptions);

            return events;
        }

        public async Task SeedTestEventsAsync()
        {
            try
            {
                var random = new Random();
                var imageUrls = new[]
                {
                    "https://images.unsplash.com/photo-1485827404703-89b55fcc595e",
                    "https://images.unsplash.com/photo-1639322537228-f710d846310a",
                    "https://images.unsplash.com/photo-1550751827-4bd374c3f58b",
                    "https://images.unsplash.com/photo-1415201364774-f6f0bb35f28f",
                    "https://images.unsplash.com/photo-1470225620780-d164df4dedc6",
                    "https://images.unsplash.com/photo-1465847899084-d164df4dedc6",
                    "https://images.unsplash.com/photo-1452626038306-9aae5e071dd3",
                    "https://images.unsplash.com/photo-1544367567-0f2fcb009e0b",
                    "https://images.unsplash.com/photo-1546519638-68e109498ffc",
                    "https://images.unsplash.com/photo-1510812431401-41d2bd2722f3",
                    "https://images.unsplash.com/photo-1505826759037-406b40feb4cd",
                    "https://images.unsplash.com/photo-1436076863939-06870fe779c2",
                    "https://images.unsplash.com/photo-1536924940846-227afb31e2a5",
                    "https://images.unsplash.com/photo-1507676184212-d03ab07a01bf",
                    "https://images.unsplash.com/photo-1452587925148-ce544e77e70d",
                    "https://images.unsplash.com/photo-1515187029135-18ee286d815b",
                    "https://images.unsplash.com/photo-1557804506-669a67965ba0",
                    "https://images.unsplash.com/photo-1444653614773-995cb1ef9efa"
                };

                var locations = new[]
                {
                    "UNIT.City, Kyiv",
                    "Kyiv Expo Center",
                    "President Hotel, Kyiv",
                    "Atlas Club, Kyiv",
                    "Art-zavod Platforma",
                    "National Philharmonic",
                    "Khreshchatyk Street",
                    "Mariinsky Park",
                    "Sports Complex Meridian",
                    "Good Wine, Kyiv",
                    "VDNH",
                    "Varvar Brew",
                    "Mystetskyi Arsenal, Kyiv",
                    "Ivan Franko Theater",
                    "Izone Creative Space",
                    "Platforma Art-Zavod, Kyiv",
                    "Hilton Kyiv",
                    "IQ Business Center"
                };

                // Генеруємо 100 подій
                for (int i = 0; i < 100; i++)
                {
                    var category = EventCategories.All[random.Next(EventCategories.All.Length)];
                    var type = (EventType)random.Next(Enum.GetValues(typeof(EventType)).Length);
                    var startDate = DateTime.UtcNow.AddDays(random.Next(1, 60));
                    var duration = random.Next(1, 5);
                    var price = random.Next(0, 1000);
                    var maxAttendees = random.Next(20, 1000);

                    var newEvent = new Event
                    {
                        Title = $"{category} Event #{i + 1}",
                        Description = $"This is a {category.ToLower()} event with amazing content and opportunities for networking and learning. Join us for an unforgettable experience!",
                        StartDate = startDate,
                        EndDate = startDate.AddDays(duration),
                        StartTime = TimeSpan.FromHours(random.Next(9, 20)),
                        Location = locations[random.Next(locations.Length)],
                        MaxAttendees = maxAttendees,
                        CurrentCapacity = random.Next(0, maxAttendees),
                        Category = category,
                        Type = type,
                        Price = price,
                        ImageUrl = imageUrls[random.Next(imageUrls.Length)],
                        IsActive = true,
                        IsFeatured = random.Next(100) < 20, // 20% шанс бути featured
                        CreatedAt = DateTime.UtcNow,
                        CreatorId = "1",
                        OrganizerId = "1"
                    };

                    await _context.Events.AddAsync(newEvent);
                }

                await _context.SaveChangesAsync();
                _cache.Remove(EventsCacheKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error seeding test events");
                throw;
            }
        }

        public async Task<IEnumerable<Registration>> GetEventRegistrationsAsync(int eventId)
        {
            try
            {
                var registrations = await _context.Registrations
                    .Include(r => r.User)
                    .Include(r => r.Event)
                    .Where(r => r.EventId == eventId)
                    .OrderByDescending(r => r.CreatedAt)
                    .ToListAsync();

                return registrations;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving registrations for event {EventId}", eventId);
                throw;
            }
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
