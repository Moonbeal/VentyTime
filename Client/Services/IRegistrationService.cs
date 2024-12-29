using VentyTime.Shared.Models;

namespace VentyTime.Client.Services
{
    public interface IRegistrationService
    {
        Task<RegistrationResponse> RegisterForEventAsync(int eventId);
        Task<RegistrationResponse> UnregisterFromEventAsync(int eventId);
        Task<bool> IsRegisteredForEventAsync(int eventId);
        Task<int> GetRegisteredUsersCountAsync(int eventId);
        Task<List<Registration>> GetUserRegistrationsAsync();
        Task<bool> CancelRegistrationAsync(int eventId);
        Task<List<EventRegistration>> GetEventRegistrationsAsync(int eventId);
        Task<bool> UpdateRegistrationStatusAsync(int registrationId, RegistrationStatus status);
    }
}
