using System.Net.Http;
using VentyTime.Shared.Models;

namespace VentyTime.Client.Services
{
    public interface IEventService
    {
        Task<List<Event>> GetEventsAsync();
        Task<Event?> GetEventByIdAsync(int id);
        Task<Event> CreateEventAsync(Event eventItem);
        Task<Event> UpdateEventAsync(Event eventItem);
        Task<bool> DeleteEventAsync(int id);
        Task<List<Event>> GetEventsByOrganizerIdAsync(string organizerId);
        Task<List<Event>> SearchEventsAsync(string query);
        Task<List<Event>> GetUpcomingEventsAsync();
        Task<List<Event>> GetPopularEventsAsync();
        Task<bool> IsEventFullAsync(int eventId);
        Task<bool> CancelEventAsync(int eventId);
        Task<byte[]> GenerateReportAsync(int eventId);
        Task<string> UploadEventImage(MultipartFormDataContent content);
        Task<List<string>> GetCategoriesAsync();
        Task<(bool Success, string? ErrorMessage)> RegisterForEventAsync(int eventId);
        Task<List<Event>> GetRegisteredEventsAsync();
        Task<bool> UnregisterFromEventAsync(int eventId);
        Task<bool> CheckRegistrationStatusAsync(int eventId);
    }
}
