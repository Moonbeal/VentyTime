using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VentyTime.Server.Data;
using VentyTime.Shared.Models;

namespace VentyTime.Server.Services
{
    public interface IRegistrationService
    {
        Task<EventRegistration> CreateRegistrationAsync(int eventId, string userId);
        Task<EventRegistration> UpdateRegistrationStatusAsync(int eventId, string userId, RegistrationStatus newStatus);
        Task<EventRegistration?> GetRegistrationAsync(int eventId, string userId);
        Task<IEnumerable<EventRegistration>> GetRegistrationsByEventAsync(int eventId);
        Task<IEnumerable<EventRegistration>> GetRegistrationsByUserAsync(string userId);
        Task<bool> CancelRegistrationAsync(int eventId, string userId);
        Task<bool> ConfirmRegistrationAsync(int eventId, string userId);
        Task<bool> IsEventFullAsync(int eventId);
        Task<bool> UpdateRegistrationStatusAsync(int registrationId, RegistrationStatus status);
    }

    public class RegistrationService : IRegistrationService
    {
        private readonly ApplicationDbContext _context;

        public RegistrationService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<EventRegistration> CreateRegistrationAsync(int eventId, string userId)
        {
            var registration = new EventRegistration
            {
                EventId = eventId,
                UserId = userId,
                Status = RegistrationStatus.Pending
            };

            _context.EventRegistrations.Add(registration);
            await _context.SaveChangesAsync();

            return registration;
        }

        public async Task<EventRegistration> UpdateRegistrationStatusAsync(int eventId, string userId, RegistrationStatus newStatus)
        {
            var registration = await GetRegistrationAsync(eventId, userId) ?? 
                throw new KeyNotFoundException($"Registration not found for event {eventId} and user {userId}");

            registration.Status = newStatus;
            registration.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return registration;
        }

        public async Task<bool> UpdateRegistrationStatusAsync(int registrationId, RegistrationStatus status)
        {
            var registration = await _context.EventRegistrations.FindAsync(registrationId);
            if (registration == null)
            {
                return false;
            }

            registration.Status = status;
            registration.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<EventRegistration?> GetRegistrationAsync(int eventId, string userId)
        {
            return await _context.EventRegistrations
                .Include(r => r.Event)
                .Include(r => r.User)
                .FirstOrDefaultAsync(r => r.EventId == eventId && r.UserId == userId);
        }

        public async Task<IEnumerable<EventRegistration>> GetRegistrationsByEventAsync(int eventId)
        {
            return await _context.EventRegistrations
                .Include(r => r.User)
                .Where(r => r.EventId == eventId)
                .ToListAsync();
        }

        public async Task<IEnumerable<EventRegistration>> GetRegistrationsByUserAsync(string userId)
        {
            return await _context.EventRegistrations
                .Include(r => r.Event)
                .Where(r => r.UserId == userId)
                .ToListAsync();
        }

        public async Task<bool> CancelRegistrationAsync(int eventId, string userId)
        {
            var registration = await GetRegistrationAsync(eventId, userId);
            if (registration is null)
            {
                return false;
            }

            registration.Status = RegistrationStatus.Cancelled;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ConfirmRegistrationAsync(int eventId, string userId)
        {
            var registration = await GetRegistrationAsync(eventId, userId);
            if (registration is null)
            {
                return false;
            }

            registration.Status = RegistrationStatus.Confirmed;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> IsEventFullAsync(int eventId)
        {
            var @event = await _context.Events
                .Include(e => e.EventRegistrations)
                .FirstOrDefaultAsync(e => e.Id == eventId) ?? 
                throw new KeyNotFoundException($"Event with ID {eventId} not found");

            var confirmedRegistrations = @event.EventRegistrations?.Count(r => r.Status == RegistrationStatus.Confirmed) ?? 0;
            return @event.MaxAttendees > 0 && confirmedRegistrations >= @event.MaxAttendees;
        }
    }
}
