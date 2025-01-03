using System.Net.Http;
using System.Collections.Generic;
using VentyTime.Shared.Models;

namespace VentyTime.Client.Services
{
    public interface IEventService
    {
        Task<(IEnumerable<EventDto> Events, int TotalCount)> GetEventsAsync(int page = 1, int? pageSize = null, string? category = null, string? searchQuery = null, DateTime? startDate = null, DateTime? endDate = null);
        Task<EventDto?> GetEventByIdAsync(int id);
        Task<EventDto> CreateEventAsync(EventDto eventItem);
        Task<EventDto> UpdateEventAsync(EventDto eventItem);
        Task<bool> DeleteEventAsync(int id);
        Task<List<EventDto>> GetEventsByOrganizerIdAsync(string organizerId);
        Task<List<EventDto>> SearchEventsAsync(string query);
        Task<List<EventDto>> GetUpcomingEventsAsync();
        Task<List<EventDto>> GetPopularEventsAsync();
        Task<bool> IsEventFullAsync(int eventId);
        Task<bool> CancelEventAsync(int eventId);
        Task<byte[]> GenerateReportAsync(int eventId);
        Task<string> UploadEventImage(MultipartFormDataContent content);
        Task<List<string>> GetCategoriesAsync();
        Task<(bool Success, string? ErrorMessage)> RegisterForEventAsync(int eventId);
        Task<List<EventDto>> GetRegisteredEventsAsync();
        Task<bool> UnregisterFromEventAsync(int eventId);
        Task<bool> CheckRegistrationStatusAsync(int eventId);
    }
}
