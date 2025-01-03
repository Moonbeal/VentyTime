using VentyTime.Shared.Models;

namespace VentyTime.Client.Services
{
    public interface IRegistrationService
    {
        Task<RegistrationResponse> RegisterForEventAsync(int eventId, string userId);
        Task<RegistrationResponse> UnregisterFromEventAsync(int eventId, string userId);
        Task<EventRegistration?> GetRegistrationAsync(int eventId, string userId);
        Task<bool> IsRegisteredForEventAsync(int eventId);
        Task<bool> HasPendingRegistrationAsync(int eventId);
        Task<bool> CancelRegistrationAsync(int eventId);
        Task<List<UserEventRegistration>> GetUserRegistrationsAsync();
    }
}
