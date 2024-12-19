using VentyTime.Shared.Models;
using System.Net.Http;

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
        Task<List<Event>> SearchEventsAsync(string searchTerm);
        Task<List<Event>> GetUpcomingEventsAsync();
        Task<List<Event>> GetPopularEventsAsync(int count = 5);
        Task<bool> IsEventFullAsync(int id);
        Task<bool> CancelEventAsync(int id);
        Task<byte[]> GenerateReportAsync(ReportPeriod period);
        Task<ApiResponse<string>> UploadEventImage(MultipartFormDataContent content);
    }
}
