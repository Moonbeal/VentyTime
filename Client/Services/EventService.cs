using System.Net.Http.Json;
using VentyTime.Shared.Models;

namespace VentyTime.Client.Services
{
    public class EventService : IEventService
    {
        private readonly HttpClient _httpClient;

        public EventService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<Event>> GetEventsAsync()
        {
            return await _httpClient.GetFromJsonAsync<List<Event>>("api/events") ?? new List<Event>();
        }

        public async Task<Event?> GetEventByIdAsync(int id)
        {
            return await _httpClient.GetFromJsonAsync<Event>($"api/events/{id}");
        }

        public async Task<Event> CreateEventAsync(Event eventItem)
        {
            var response = await _httpClient.PostAsJsonAsync("api/events", eventItem);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<Event>() ?? throw new Exception("Failed to create event");
        }

        public async Task<Event> UpdateEventAsync(Event eventItem)
        {
            var response = await _httpClient.PutAsJsonAsync($"api/events/{eventItem.Id}", eventItem);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<Event>() ?? throw new Exception("Failed to update event");
        }

        public async Task<bool> DeleteEventAsync(int id)
        {
            var response = await _httpClient.DeleteAsync($"api/events/{id}");
            return response.IsSuccessStatusCode;
        }

        public async Task<List<Event>> GetEventsByOrganizerIdAsync(string organizerId)
        {
            return await _httpClient.GetFromJsonAsync<List<Event>>($"api/events/organizer/{organizerId}") ?? new List<Event>();
        }

        public async Task<List<Event>> SearchEventsAsync(string searchTerm)
        {
            return await _httpClient.GetFromJsonAsync<List<Event>>($"api/events/search?q={Uri.EscapeDataString(searchTerm)}") ?? new List<Event>();
        }

        public async Task<List<Event>> GetUpcomingEventsAsync()
        {
            return await _httpClient.GetFromJsonAsync<List<Event>>("api/events/upcoming") ?? new List<Event>();
        }

        public async Task<List<Event>> GetPopularEventsAsync(int count = 5)
        {
            return await _httpClient.GetFromJsonAsync<List<Event>>($"api/events/popular?count={count}") ?? new List<Event>();
        }

        public async Task<bool> IsEventFullAsync(int eventId)
        {
            var @event = await GetEventByIdAsync(eventId);
            return @event?.IsFull ?? false;
        }
    }
}
