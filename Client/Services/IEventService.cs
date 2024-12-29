using System.Net.Http;
using VentyTime.Shared.Models;

namespace VentyTime.Client.Services
{
    public interface IEventService
    {
        Task<(List<Event> Events, int TotalCount)> GetEventsAsync(int page = 1, int pageSize = 50, string? searchQuery = null);
        Task<Event> GetEventByIdAsync(int id);
        Task<List<Event>> GetEventsByOrganizerAsync(string organizerId);
        Task<Event> CreateEventAsync(Event @event);
        Task<Event> UpdateEventAsync(Event @event);
        Task DeleteEventAsync(int id);
        Task<List<Event>> GetUpcomingEventsAsync();
        Task<List<Event>> GetPopularEventsAsync();
        Task<List<Event>> SearchEventsAsync(string query);
        Task<List<string>> GetCategoriesAsync();
        Task<List<Event>> GetRegisteredEventsAsync();
        Task<bool> IsEventFullAsync(int eventId);
        Task<(bool Success, string? ErrorMessage)> RegisterForEventAsync(int eventId);
        Task<bool> UnregisterFromEventAsync(int eventId);
        Task<List<EventRegistration>> GetEventParticipantsAsync(int eventId);
        Task<bool> RemoveParticipantAsync(int eventId, string userId);
        Task<bool> SendNotificationToParticipantAsync(int eventId, string userId, string message);
        Task<bool> SendNotificationToAllParticipantsAsync(int eventId, string message);
        Task<bool> CancelEventAsync(int eventId);
        Task<bool> CheckRegistrationStatusAsync(int eventId);
        Task<byte[]> GenerateReportAsync(int eventId);
        Task<string> UploadEventImage(MultipartFormDataContent content);
    }
}
