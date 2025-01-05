using Microsoft.EntityFrameworkCore;
using VentyTime.Server.Data;
using VentyTime.Shared.Models;

namespace VentyTime.Server.Services
{
    public interface IRegistrationService
    {
        Task<Registration> CreateRegistrationAsync(Registration registration, string userId);
        Task<Registration> UpdateRegistrationAsync(Registration registration, RegistrationStatus newStatus, string userId);
        Task<Registration?> GetRegistrationByIdAsync(int id);
        Task<IEnumerable<Registration>> GetRegistrationsByEventAsync(int eventId);
        Task<IEnumerable<Registration>> GetRegistrationsByUserAsync(string userId);
        Task<bool> CancelRegistrationAsync(int id, string userId);
        Task<bool> ConfirmRegistrationAsync(int id, string userId);
        Task<bool> IsEventFullAsync(int eventId);
    }

    public class RegistrationService : IRegistrationService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<RegistrationService> _logger;
        private readonly IEventService _eventService;

        public RegistrationService(
            ApplicationDbContext context,
            ILogger<RegistrationService> logger,
            IEventService eventService)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _eventService = eventService ?? throw new ArgumentNullException(nameof(eventService));
        }

        public async Task<Registration> CreateRegistrationAsync(Registration registration, string userId)
        {
            try
            {
                _logger.LogInformation("Creating registration for event {EventId} by user {UserId}", 
                    registration.EventId, userId);

                var @event = await _eventService.GetEventByIdAsync(registration.EventId) 
                    ?? throw new KeyNotFoundException($"Event with ID {registration.EventId} not found");

                if (await IsEventFullAsync(registration.EventId))
                {
                    throw new InvalidOperationException("Event is already full");
                }

                registration.UserId = userId;
                registration.Status = RegistrationStatus.Pending;
                registration.CreatedAt = DateTime.UtcNow;

                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    _context.Registrations.Add(registration);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    _logger.LogInformation("Successfully created registration {RegistrationId}", registration.Id);

                    return registration;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Error saving registration to database");
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating registration");
                throw;
            }
        }

        public async Task<Registration> UpdateRegistrationAsync(Registration registration, RegistrationStatus newStatus, string userId)
        {
            try
            {
                var existingRegistration = await _context.Registrations
                    .Include(r => r.Event)
                    .FirstOrDefaultAsync(r => r.Id == registration.Id)
                    ?? throw new KeyNotFoundException($"Registration with ID {registration.Id} not found");

                if (existingRegistration.UserId != userId)
                {
                    throw new UnauthorizedAccessException("User is not authorized to update this registration");
                }

                if (existingRegistration.Status != RegistrationStatus.Pending)
                {
                    throw new InvalidOperationException($"Cannot update registration with status {existingRegistration.Status}");
                }

                existingRegistration.Status = newStatus;
                existingRegistration.UpdatedAt = DateTime.UtcNow;

                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    _logger.LogInformation("Successfully updated registration {RegistrationId}", registration.Id);

                    return existingRegistration;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Error updating registration in database");
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating registration");
                throw;
            }
        }

        public async Task<Registration?> GetRegistrationByIdAsync(int id)
        {
            return await _context.Registrations
                .Include(r => r.Event)
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task<IEnumerable<Registration>> GetRegistrationsByEventAsync(int eventId)
        {
            return await _context.Registrations
                .Include(r => r.User)
                .Where(r => r.EventId == eventId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Registration>> GetRegistrationsByUserAsync(string userId)
        {
            return await _context.Registrations
                .Include(r => r.Event!)
                .ThenInclude(e => e.Organizer!)
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<bool> CancelRegistrationAsync(int id, string userId)
        {
            try
            {
                var registration = await _context.Registrations
                    .Include(r => r.Event)
                    .FirstOrDefaultAsync(r => r.Id == id)
                    ?? throw new KeyNotFoundException($"Registration with ID {id} not found");

                if (registration.UserId != userId)
                {
                    throw new UnauthorizedAccessException("User is not authorized to cancel this registration");
                }

                registration.Status = RegistrationStatus.Cancelled;
                registration.UpdatedAt = DateTime.UtcNow;

                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    _logger.LogInformation("Successfully cancelled registration {RegistrationId}", id);

                    return true;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Error cancelling registration in database");
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling registration");
                throw;
            }
        }

        public async Task<bool> ConfirmRegistrationAsync(int id, string userId)
        {
            try
            {
                var registration = await _context.Registrations
                    .Include(r => r.Event)
                    .FirstOrDefaultAsync(r => r.Id == id)
                    ?? throw new KeyNotFoundException($"Registration with ID {id} not found");

                if (!await _eventService.IsUserAuthorizedForEvent(registration.EventId, userId, new[] { "Admin", "Organizer" }))
                {
                    throw new UnauthorizedAccessException("User is not authorized to confirm registrations");
                }

                registration.Status = RegistrationStatus.Confirmed;
                registration.UpdatedAt = DateTime.UtcNow;

                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    _logger.LogInformation("Successfully confirmed registration {RegistrationId}", id);

                    return true;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Error confirming registration in database");
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error confirming registration");
                throw;
            }
        }

        public async Task<bool> IsEventFullAsync(int eventId)
        {
            var @event = await _context.Events
                .Include(e => e.Registrations)
                .FirstOrDefaultAsync(e => e.Id == eventId)
                ?? throw new KeyNotFoundException($"Event with ID {eventId} not found");

            var registrations = @event.Registrations ?? new List<Registration>();
            return @event.MaxAttendees > 0 && 
                   registrations.Count(r => r.Status != RegistrationStatus.Cancelled) >= @event.MaxAttendees;
        }
    }
}
