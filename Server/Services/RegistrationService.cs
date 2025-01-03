using Microsoft.EntityFrameworkCore;
using VentyTime.Server.Data;
using VentyTime.Shared.Models;

namespace VentyTime.Server.Services
{
    public interface IRegistrationService
    {
        Task<UserEventRegistration> CreateRegistrationAsync(UserEventRegistration registration, string userId);
        Task<UserEventRegistration> UpdateRegistrationAsync(UserEventRegistration registration, RegistrationStatus newStatus, string userId);
        Task<UserEventRegistration?> GetRegistrationByIdAsync(int id);
        Task<IEnumerable<UserEventRegistration>> GetRegistrationsByEventAsync(int eventId);
        Task<IEnumerable<UserEventRegistration>> GetRegistrationsByUserAsync(string userId);
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

        public async Task<UserEventRegistration> CreateRegistrationAsync(UserEventRegistration registration, string userId)
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
                registration.UpdatedAt = null;

                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    _context.UserEventRegistrations.Add(registration);
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

        public async Task<UserEventRegistration> UpdateRegistrationAsync(UserEventRegistration registration, RegistrationStatus newStatus, string userId)
        {
            try
            {
                var existingRegistration = await _context.UserEventRegistrations
                    .Include(r => r.Event)
                    .FirstOrDefaultAsync(r => r.EventId == registration.EventId && r.UserId == userId)
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

                    existingRegistration.Status = newStatus;
                    existingRegistration.UpdatedAt = DateTime.UtcNow;
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

        public async Task<UserEventRegistration?> GetRegistrationByIdAsync(int id)
        {
            return await _context.UserEventRegistrations
                .Include(r => r.Event)
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task<IEnumerable<UserEventRegistration>> GetRegistrationsByEventAsync(int eventId)
        {
            return await _context.UserEventRegistrations
                .Include(r => r.User)
                .Where(r => r.EventId == eventId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<UserEventRegistration>> GetRegistrationsByUserAsync(string userId)
        {
            return await _context.UserEventRegistrations
                .Include(r => r.Event)
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<bool> CancelRegistrationAsync(int id, string userId)
        {
            try
            {
                var registration = await _context.UserEventRegistrations
                    .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId)
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
                var registration = await _context.UserEventRegistrations
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

            var registrations = await _context.UserEventRegistrations
                .CountAsync(r => r.EventId == eventId && r.Status == RegistrationStatus.Confirmed);

            return registrations >= @event.MaxAttendees;
        }
    }
}
