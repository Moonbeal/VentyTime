using VentyTime.Shared.Models;

namespace VentyTime.Client.Services
{
    public interface IRegistrationService
    {
        Task<bool> RegisterForEventAsync(int eventId);
        Task<bool> UnregisterFromEventAsync(int eventId);
        Task<bool> CancelRegistrationAsync(int eventId);
        Task<List<Registration>> GetUserRegistrationsAsync();
        Task<List<Registration>> GetEventRegistrationsAsync(int eventId);
    }
}
