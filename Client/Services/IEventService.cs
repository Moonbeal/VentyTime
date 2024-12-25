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
        Task<List<Event>> SearchEventsAsync(string searchTerm);
        Task<List<Event>> GetUpcomingEventsAsync(int count = 10);
        Task<List<Event>> GetPopularEventsAsync(int count = 5);
        Task<bool> IsEventFullAsync(int id);
        Task<bool> CancelEventAsync(int id);
        Task<ApiResponse<string>> UploadEventImage(MultipartFormDataContent content);
        Task<byte[]> GenerateReportAsync(ReportPeriod period);
        Task<List<string>> GetCategoriesAsync();
    }
}
