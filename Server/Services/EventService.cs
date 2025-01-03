using Microsoft.EntityFrameworkCore;
using VentyTime.Server.Data;
using VentyTime.Shared.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;

namespace VentyTime.Server.Services
{
    public interface IEventService
    {
        Task<EventsResponse> GetEventsAsync(int page = 1, int? pageSize = null, string? category = null, string? searchQuery = null, DateTime? startDate = null, DateTime? endDate = null);
        Task<Event?> GetEventByIdAsync(int id);
        Task<Event> CreateEventAsync(Event @event, string userIdOrEmail);
        Task<Event> UpdateEventAsync(Event @event, string userId);
        Task DeleteEventAsync(int id, string userId);
        Task<SearchEventsResponse> SearchEventsAsync(string query);
        Task<UpcomingEventsResponse> GetUpcomingEventsAsync(int count);
        Task<bool> IsUserAuthorizedForEvent(int eventId, string userId, string[] allowedRoles);
        Task<IEnumerable<Event>> GetEventsByOrganizerAsync(string organizerId);
        Task<IEnumerable<string>> GetCategoriesAsync();
        Task<UserEventRegistration> RegisterUserForEventAsync(int eventId, string userId);
        Task<RegisteredEventsResponse> GetRegisteredEventsAsync(string userId);
    }

    public class EventService : IEventService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<EventService> _logger;
        private readonly UserManager<ApplicationUser> _userManager;

        public EventService(
            ApplicationDbContext context,
            ILogger<EventService> logger,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _logger = logger;
            _userManager = userManager;
        }

        public async Task<EventsResponse> GetEventsAsync(
            int page = 1,
            int? pageSize = null,
            string? category = null,
            string? searchQuery = null,
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            try
            {
                _logger.LogInformation("Getting events with filters");

                var query = _context.Events
                    .Include(e => e.Organizer)
                    .Include(e => e.Creator)
                    .Include(e => e.Registrations)
                    .AsNoTracking()
                    .AsQueryable();

                // Apply filters
                if (!string.IsNullOrEmpty(category))
                {
                    query = query.Where(e => e.Category == category);
                }

                if (!string.IsNullOrEmpty(searchQuery))
                {
                    query = query.Where(e => 
                        e.Title.Contains(searchQuery) || 
                        e.Description.Contains(searchQuery) ||
                        e.Location.Contains(searchQuery));
                }

                if (startDate.HasValue)
                {
                    query = query.Where(e => e.StartDate >= startDate.Value);
                }

                if (endDate.HasValue)
                {
                    query = query.Where(e => e.StartDate <= endDate.Value);
                }

                // Get total count
                var totalCount = await query.CountAsync();

                // Apply pagination only if pageSize is specified
                if (pageSize.HasValue && pageSize.Value > 0)
                {
                    query = query
                        .Skip((page - 1) * pageSize.Value)
                        .Take(pageSize.Value);
                }

                var events = await query
                    .OrderBy(e => e.StartDate)
                    .Select(e => new EventDto
                    {
                        Id = e.Id,
                        Title = e.Title,
                        Description = e.Description,
                        StartDate = e.StartDate,
                        EndDate = e.EndDate,
                        StartTime = e.StartTime,
                        Location = e.Location,
                        VenueDetails = e.VenueDetails,
                        OnlineUrl = e.OnlineUrl,
                        IsOnline = e.IsOnline,
                        MaxAttendees = e.MaxAttendees,
                        Category = e.Category,
                        Type = e.Type,
                        Accessibility = e.Accessibility,
                        ImageUrl = e.ImageUrl,
                        Price = e.Price,
                        IsActive = e.IsActive ?? true,
                        IsCancelled = e.IsCancelled,
                        CreatedAt = e.CreatedAt,
                        UpdatedAt = e.UpdatedAt,
                        CreatorId = e.CreatorId,
                        OrganizerId = e.OrganizerId,
                        Creator = e.Creator != null ? new ApplicationUser
                        {
                            Id = e.Creator.Id,
                            UserName = e.Creator.UserName,
                            Email = e.Creator.Email,
                            FirstName = e.Creator.FirstName,
                            LastName = e.Creator.LastName,
                            AvatarUrl = e.Creator.AvatarUrl
                        } : null,
                        Organizer = e.Organizer != null ? new ApplicationUser
                        {
                            Id = e.Organizer.Id,
                            UserName = e.Organizer.UserName,
                            Email = e.Organizer.Email,
                            FirstName = e.Organizer.FirstName,
                            LastName = e.Organizer.LastName,
                            AvatarUrl = e.Organizer.AvatarUrl
                        } : null,
                        CurrentParticipants = e.Registrations.Count,
                        IsFeatured = e.IsFeatured,
                        RequiresRegistration = e.RequiresRegistration,
                        HasAgeRestriction = e.HasAgeRestriction,
                        MinimumAge = e.MinimumAge,
                        HasEarlyBirdPrice = e.HasEarlyBirdPrice,
                        EarlyBirdPrice = e.EarlyBirdPrice,
                        EarlyBirdDeadline = e.EarlyBirdDeadline,
                        RefundPolicy = e.RefundPolicy ?? "Standard refund policy applies",
                        Requirements = e.Requirements ?? "No special requirements",
                        Schedule = e.Schedule ?? "Detailed schedule will be provided closer to the event",
                        Tags = e.Tags ?? new List<string>(),
                        AllowWaitlist = e.AllowWaitlist,
                        WaitlistCapacity = e.WaitlistCapacity
                    })
                    .ToListAsync();

                _logger.LogInformation("Found {Count} events", events.Count);

                return new EventsResponse
                {
                    Events = events,
                    TotalCount = totalCount
                };
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
                var @event = await _context.Events
                    .Include(e => e.Creator)
                    .Include(e => e.Organizer)
                    .Include(e => e.Registrations)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(e => e.Id == id);

                return @event is null ? null : @event;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting event {EventId}", id);
                throw;
            }
        }

        public async Task<Event> CreateEventAsync(Event @event, string userIdOrEmail)
        {
            try
            {
                var user = await _context.Users.FindAsync(userIdOrEmail) 
                    ?? await _userManager.FindByEmailAsync(userIdOrEmail)
                    ?? throw new KeyNotFoundException($"User with ID/Email {userIdOrEmail} not found");

                @event.CreatorId = user.Id;
                @event.OrganizerId = user.Id;
                @event.CreatedAt = DateTime.UtcNow;

                _context.Events.Add(@event);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Created event {EventId} by user {UserId}", @event.Id, user.Id);

                return @event;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating event by user {UserId}", userIdOrEmail);
                throw;
            }
        }

        public async Task<Event> UpdateEventAsync(Event @event, string userId)
        {
            try
            {
                var existingEvent = await _context.Events
                    .Include(e => e.Creator)
                    .Include(e => e.Organizer)
                    .FirstOrDefaultAsync(e => e.Id == @event.Id);

                return existingEvent is null 
                    ? throw new KeyNotFoundException($"Event with ID {@event.Id} not found")
                    : await UpdateEventAsync(existingEvent, @event, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating event {EventId}", @event.Id);
                throw;
            }
        }

        private async Task<Event> UpdateEventAsync(Event existingEvent, Event @event, string userId)
        {
            if (existingEvent.CreatorId != userId && existingEvent.OrganizerId != userId)
            {
                throw new UnauthorizedAccessException("User is not authorized to update this event");
            }

            // Update only allowed fields
            existingEvent.Title = @event.Title;
            existingEvent.Description = @event.Description;
            existingEvent.StartDate = @event.StartDate;
            existingEvent.EndDate = @event.EndDate;
            existingEvent.StartTime = @event.StartTime;
            existingEvent.Location = @event.Location;
            existingEvent.VenueDetails = @event.VenueDetails;
            existingEvent.OnlineUrl = @event.OnlineUrl;
            existingEvent.MaxAttendees = @event.MaxAttendees;
            existingEvent.Category = @event.Category;
            existingEvent.Type = @event.Type;
            existingEvent.Accessibility = @event.Accessibility;
            existingEvent.ImageUrl = @event.ImageUrl;
            existingEvent.Price = @event.Price;
            existingEvent.IsActive = @event.IsActive;
            existingEvent.IsCancelled = @event.IsCancelled;
            existingEvent.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Updated event {EventId} by user {UserId}", @event.Id, userId);

            return existingEvent;
        }

        public async Task DeleteEventAsync(int id, string userId)
        {
            try
            {
                var @event = await _context.Events.FindAsync(id)
                    ?? throw new KeyNotFoundException($"Event with ID {id} not found");

                await DeleteEventAsync(@event, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting event {EventId} by user {UserId}", id, userId);
                throw;
            }
        }

        private async Task DeleteEventAsync(Event @event, string userId)
        {
            if (@event.CreatorId != userId)
            {
                throw new UnauthorizedAccessException("User is not authorized to delete this event");
            }

            _context.Events.Remove(@event);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted event {EventId} by user {UserId}", @event.Id, userId);
        }

        public async Task<SearchEventsResponse> SearchEventsAsync(string query)
        {
            try
            {
                _logger.LogInformation("Searching events with query: {Query}", query);

                var searchQuery = query.ToLower();
                var events = await _context.Events
                    .Include(e => e.Organizer)
                    .Include(e => e.Creator)
                    .Include(e => e.Registrations)
                    .Where(e => e.Title.ToLower().Contains(searchQuery) ||
                               e.Description.ToLower().Contains(searchQuery) ||
                               e.Category.ToLower().Contains(searchQuery) ||
                               e.Location.ToLower().Contains(searchQuery))
                    .Select(e => new EventDto
                    {
                        Id = e.Id,
                        Title = e.Title,
                        Description = e.Description,
                        StartDate = e.StartDate,
                        EndDate = e.EndDate,
                        StartTime = e.StartTime,
                        Location = e.Location,
                        VenueDetails = e.VenueDetails,
                        OnlineUrl = e.OnlineUrl,
                        IsOnline = e.IsOnline,
                        MaxAttendees = e.MaxAttendees,
                        Category = e.Category,
                        Type = e.Type,
                        Accessibility = e.Accessibility,
                        ImageUrl = e.ImageUrl,
                        Price = e.Price,
                        IsActive = e.IsActive ?? true,
                        IsCancelled = e.IsCancelled,
                        CreatedAt = e.CreatedAt,
                        UpdatedAt = e.UpdatedAt,
                        CreatorId = e.CreatorId,
                        OrganizerId = e.OrganizerId,
                        Creator = e.Creator != null ? new ApplicationUser
                        {
                            Id = e.Creator.Id,
                            UserName = e.Creator.UserName,
                            Email = e.Creator.Email,
                            FirstName = e.Creator.FirstName,
                            LastName = e.Creator.LastName,
                            AvatarUrl = e.Creator.AvatarUrl
                        } : null,
                        Organizer = e.Organizer != null ? new ApplicationUser
                        {
                            Id = e.Organizer.Id,
                            UserName = e.Organizer.UserName,
                            Email = e.Organizer.Email,
                            FirstName = e.Organizer.FirstName,
                            LastName = e.Organizer.LastName,
                            AvatarUrl = e.Organizer.AvatarUrl
                        } : null,
                        CurrentParticipants = e.Registrations.Count,
                        IsFeatured = e.IsFeatured,
                        RequiresRegistration = e.RequiresRegistration,
                        HasAgeRestriction = e.HasAgeRestriction,
                        MinimumAge = e.MinimumAge,
                        HasEarlyBirdPrice = e.HasEarlyBirdPrice,
                        EarlyBirdPrice = e.EarlyBirdPrice,
                        EarlyBirdDeadline = e.EarlyBirdDeadline,
                        RefundPolicy = e.RefundPolicy ?? "Standard refund policy applies",
                        Requirements = e.Requirements ?? "No special requirements",
                        Schedule = e.Schedule ?? "Detailed schedule will be provided closer to the event",
                        Tags = e.Tags ?? new List<string>(),
                        AllowWaitlist = e.AllowWaitlist,
                        WaitlistCapacity = e.WaitlistCapacity
                    })
                    .ToListAsync();

                _logger.LogInformation("Found {Count} events matching query: {Query}", events.Count, query);

                return new SearchEventsResponse
                {
                    Events = events,
                    TotalResults = events.Count,
                    SearchQuery = query
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching events with query: {Query}", query);
                throw;
            }
        }

        public async Task<UpcomingEventsResponse> GetUpcomingEventsAsync(int count)
        {
            try
            {
                _logger.LogInformation("Getting {Count} upcoming events", count);

                var now = DateTime.UtcNow;
                var upcomingEvents = await _context.Events
                    .Include(e => e.Organizer)
                    .Include(e => e.Creator)
                    .Include(e => e.Registrations)
                    .Where(e => e.StartDate > now && !e.IsCancelled && (e.IsActive ?? true))
                    .OrderBy(e => e.StartDate)
                    .Select(e => new EventDto
                    {
                        Id = e.Id,
                        Title = e.Title,
                        Description = e.Description,
                        StartDate = e.StartDate,
                        EndDate = e.EndDate,
                        StartTime = e.StartTime,
                        Location = e.Location,
                        VenueDetails = e.VenueDetails,
                        OnlineUrl = e.OnlineUrl,
                        IsOnline = e.IsOnline,
                        MaxAttendees = e.MaxAttendees,
                        Category = e.Category,
                        Type = e.Type,
                        Accessibility = e.Accessibility,
                        ImageUrl = e.ImageUrl,
                        Price = e.Price,
                        IsActive = e.IsActive ?? true,
                        IsCancelled = e.IsCancelled,
                        CreatedAt = e.CreatedAt,
                        UpdatedAt = e.UpdatedAt,
                        CreatorId = e.CreatorId,
                        OrganizerId = e.OrganizerId,
                        Creator = e.Creator != null ? new ApplicationUser
                        {
                            Id = e.Creator.Id,
                            UserName = e.Creator.UserName,
                            Email = e.Creator.Email,
                            FirstName = e.Creator.FirstName,
                            LastName = e.Creator.LastName,
                            AvatarUrl = e.Creator.AvatarUrl
                        } : null,
                        Organizer = e.Organizer != null ? new ApplicationUser
                        {
                            Id = e.Organizer.Id,
                            UserName = e.Organizer.UserName,
                            Email = e.Organizer.Email,
                            FirstName = e.Organizer.FirstName,
                            LastName = e.Organizer.LastName,
                            AvatarUrl = e.Organizer.AvatarUrl
                        } : null,
                        CurrentParticipants = e.Registrations.Count,
                        IsFeatured = e.IsFeatured,
                        RequiresRegistration = e.RequiresRegistration,
                        HasAgeRestriction = e.HasAgeRestriction,
                        MinimumAge = e.MinimumAge,
                        HasEarlyBirdPrice = e.HasEarlyBirdPrice,
                        EarlyBirdPrice = e.EarlyBirdPrice,
                        EarlyBirdDeadline = e.EarlyBirdDeadline,
                        RefundPolicy = e.RefundPolicy ?? "Standard refund policy applies",
                        Requirements = e.Requirements ?? "No special requirements",
                        Schedule = e.Schedule ?? "Detailed schedule will be provided closer to the event",
                        Tags = e.Tags ?? new List<string>(),
                        AllowWaitlist = e.AllowWaitlist,
                        WaitlistCapacity = e.WaitlistCapacity
                    })
                    .Take(count)
                    .ToListAsync();

                var totalUpcoming = await _context.Events
                    .CountAsync(e => e.StartDate > now && !e.IsCancelled && (e.IsActive ?? true));

                _logger.LogInformation("Found {Count} upcoming events out of {Total} total upcoming events",
                    upcomingEvents.Count, totalUpcoming);

                return new UpcomingEventsResponse
                {
                    Events = upcomingEvents,
                    Count = upcomingEvents.Count,
                    TotalUpcoming = totalUpcoming
                };
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
                var @event = await _context.Events.FindAsync(eventId);
                if (@event is null)
                {
                    return false;
                }

                if (@event.CreatorId == userId || @event.OrganizerId == userId)
                {
                    return true;
                }

                var user = await _userManager.FindByIdAsync(userId);
                if (user is null)
                {
                    return false;
                }

                var userRoles = await _userManager.GetRolesAsync(user);
                return userRoles.Any(role => allowedRoles.Contains(role));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking user authorization for event {EventId}", eventId);
                throw;
            }
        }

        public async Task<IEnumerable<Event>> GetEventsByOrganizerAsync(string organizerId)
        {
            try
            {
                return await _context.Events
                    .Include(e => e.Creator)
                    .Include(e => e.Organizer)
                    .Where(e => e.OrganizerId == organizerId)
                    .OrderByDescending(e => e.CreatedAt)
                    .AsNoTracking()
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting events by organizer {OrganizerId}", organizerId);
                throw;
            }
        }

        public async Task<IEnumerable<string>> GetCategoriesAsync()
        {
            return await Task.FromResult(EventCategories.All);
        }

        public async Task<UserEventRegistration> RegisterUserForEventAsync(int eventId, string userId)
        {
            try
            {
                var @event = await _context.Events
                    .Include(e => e.Registrations)
                    .FirstOrDefaultAsync(e => e.Id == eventId);

                return @event is null 
                    ? throw new KeyNotFoundException($"Event with ID {eventId} not found")
                    : await RegisterUserForEventAsync(@event, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering user {UserId} for event {EventId}", userId, eventId);
                throw;
            }
        }

        private async Task<UserEventRegistration> RegisterUserForEventAsync(Event @event, string userId)
        {
            if (@event.IsCancelled)
            {
                throw new InvalidOperationException("Cannot register for a cancelled event");
            }

            if (@event.CurrentParticipants >= @event.MaxAttendees)
            {
                throw new InvalidOperationException("Event is already full");
            }

            var existingRegistration = await _context.UserEventRegistrations
                .FirstOrDefaultAsync(r => r.EventId == @event.Id && r.UserId == userId);

            if (existingRegistration is not null)
            {
                throw new InvalidOperationException("User is already registered for this event");
            }

            var registration = new UserEventRegistration
            {
                EventId = @event.Id,
                UserId = userId,
                Status = RegistrationStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            _context.UserEventRegistrations.Add(registration);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created registration for event {EventId} by user {UserId}", @event.Id, userId);

            return registration;
        }

        public async Task<RegisteredEventsResponse> GetRegisteredEventsAsync(string userId)
        {
            try
            {
                _logger.LogInformation("Getting registered events for user {UserId}", userId);

                var registeredEvents = await _context.Events
                    .Include(e => e.Organizer)
                    .Include(e => e.Creator)
                    .Include(e => e.Registrations)
                    .Where(e => e.Registrations.Any(r => r.UserId == userId))
                    .Select(e => new EventDto
                    {
                        Id = e.Id,
                        Title = e.Title,
                        Description = e.Description,
                        StartDate = e.StartDate,
                        EndDate = e.EndDate,
                        StartTime = e.StartTime,
                        Location = e.Location,
                        VenueDetails = e.VenueDetails,
                        OnlineUrl = e.OnlineUrl,
                        IsOnline = e.IsOnline,
                        MaxAttendees = e.MaxAttendees,
                        Category = e.Category,
                        Type = e.Type,
                        Accessibility = e.Accessibility,
                        ImageUrl = e.ImageUrl,
                        Price = e.Price,
                        IsActive = e.IsActive ?? true,
                        IsCancelled = e.IsCancelled,
                        CreatedAt = e.CreatedAt,
                        UpdatedAt = e.UpdatedAt,
                        CreatorId = e.CreatorId,
                        OrganizerId = e.OrganizerId,
                        Creator = e.Creator != null ? new ApplicationUser
                        {
                            Id = e.Creator.Id,
                            UserName = e.Creator.UserName,
                            Email = e.Creator.Email,
                            FirstName = e.Creator.FirstName,
                            LastName = e.Creator.LastName,
                            AvatarUrl = e.Creator.AvatarUrl
                        } : null,
                        Organizer = e.Organizer != null ? new ApplicationUser
                        {
                            Id = e.Organizer.Id,
                            UserName = e.Organizer.UserName,
                            Email = e.Organizer.Email,
                            FirstName = e.Organizer.FirstName,
                            LastName = e.Organizer.LastName,
                            AvatarUrl = e.Organizer.AvatarUrl
                        } : null,
                        CurrentParticipants = e.Registrations.Count,
                        IsFeatured = e.IsFeatured,
                        RequiresRegistration = e.RequiresRegistration,
                        HasAgeRestriction = e.HasAgeRestriction,
                        MinimumAge = e.MinimumAge,
                        HasEarlyBirdPrice = e.HasEarlyBirdPrice,
                        EarlyBirdPrice = e.EarlyBirdPrice,
                        EarlyBirdDeadline = e.EarlyBirdDeadline,
                        RefundPolicy = e.RefundPolicy ?? "Standard refund policy applies",
                        Requirements = e.Requirements ?? "No special requirements",
                        Schedule = e.Schedule ?? "Detailed schedule will be provided closer to the event",
                        Tags = e.Tags ?? new List<string>(),
                        AllowWaitlist = e.AllowWaitlist,
                        WaitlistCapacity = e.WaitlistCapacity
                    })
                    .OrderBy(e => e.StartDate)
                    .ToListAsync();

                _logger.LogInformation("Found {Count} registered events for user {UserId}", 
                    registeredEvents.Count, userId);

                return new RegisteredEventsResponse
                {
                    Events = registeredEvents,
                    TotalCount = registeredEvents.Count,
                    UserId = userId
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting registered events for user {UserId}", userId);
                throw;
            }
        }
    }
}
