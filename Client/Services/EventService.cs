using System.Net.Http.Json;
using VentyTime.Client.Extensions;
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

        public async Task<(List<Event> Events, int TotalCount)> GetEventsAsync(int page = 1, int pageSize = 50, string? searchQuery = null)
        {
            try
            {
                var query = $"api/events?page={page}&pageSize={pageSize}";
                if (!string.IsNullOrWhiteSpace(searchQuery))
                {
                    query += $"&searchQuery={Uri.EscapeDataString(searchQuery)}";
                }
                var response = await _httpClient.GetAsync(query);
                if (!response.IsSuccessStatusCode)
                {
                    throw await HttpRequestExceptionExtensions.CreateFromResponseAsync(response);
                }
                
                var result = await response.Content.ReadFromJsonAsync<(List<Event> Events, int TotalCount)>();
                return result;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<Event> GetEventByIdAsync(int id)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/events/{id}");
                if (!response.IsSuccessStatusCode)
                {
                    throw await HttpRequestExceptionExtensions.CreateFromResponseAsync(response);
                }
                return await response.Content.ReadFromJsonAsync<Event>() ?? throw new Exception("Event not found");
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<List<Event>> GetEventsByOrganizerAsync(string organizerId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/events/organizer/{organizerId}");
                if (!response.IsSuccessStatusCode)
                {
                    throw await HttpRequestExceptionExtensions.CreateFromResponseAsync(response);
                }
                return await response.Content.ReadFromJsonAsync<List<Event>>() ?? new List<Event>();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<Event> CreateEventAsync(Event @event)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/events", @event);
                if (!response.IsSuccessStatusCode)
                {
                    throw await HttpRequestExceptionExtensions.CreateFromResponseAsync(response);
                }
                return await response.Content.ReadFromJsonAsync<Event>() ?? throw new Exception("Failed to create event");
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<Event> UpdateEventAsync(Event @event)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync($"api/events/{@event.Id}", @event);
                if (!response.IsSuccessStatusCode)
                {
                    throw await HttpRequestExceptionExtensions.CreateFromResponseAsync(response);
                }
                return await response.Content.ReadFromJsonAsync<Event>() ?? throw new Exception("Failed to update event");
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task DeleteEventAsync(int id)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"api/events/{id}");
                if (!response.IsSuccessStatusCode)
                {
                    throw await HttpRequestExceptionExtensions.CreateFromResponseAsync(response);
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<List<Event>> GetUpcomingEventsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("api/events/upcoming");
                if (!response.IsSuccessStatusCode)
                {
                    throw await HttpRequestExceptionExtensions.CreateFromResponseAsync(response);
                }
                return await response.Content.ReadFromJsonAsync<List<Event>>() ?? new List<Event>();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<List<Event>> GetPopularEventsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("api/events/popular");
                if (!response.IsSuccessStatusCode)
                {
                    throw await HttpRequestExceptionExtensions.CreateFromResponseAsync(response);
                }
                return await response.Content.ReadFromJsonAsync<List<Event>>() ?? new List<Event>();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<List<Event>> SearchEventsAsync(string query)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/events/search?q={Uri.EscapeDataString(query)}");
                if (!response.IsSuccessStatusCode)
                {
                    throw await HttpRequestExceptionExtensions.CreateFromResponseAsync(response);
                }
                return await response.Content.ReadFromJsonAsync<List<Event>>() ?? new List<Event>();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<List<string>> GetCategoriesAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("api/events/categories");
                if (!response.IsSuccessStatusCode)
                {
                    throw await HttpRequestExceptionExtensions.CreateFromResponseAsync(response);
                }
                return await response.Content.ReadFromJsonAsync<List<string>>() ?? new List<string>();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<List<Event>> GetRegisteredEventsAsync()
        {
            try
            {
                var response = await _httpClient.GetAsync("api/events/registered");
                if (!response.IsSuccessStatusCode)
                {
                    throw await HttpRequestExceptionExtensions.CreateFromResponseAsync(response);
                }
                return await response.Content.ReadFromJsonAsync<List<Event>>() ?? new List<Event>();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<bool> IsEventFullAsync(int eventId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/events/{eventId}/is-full");
                if (!response.IsSuccessStatusCode)
                {
                    throw await HttpRequestExceptionExtensions.CreateFromResponseAsync(response);
                }
                return await response.Content.ReadFromJsonAsync<bool>();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<(bool Success, string? ErrorMessage)> RegisterForEventAsync(int eventId)
        {
            try
            {
                var response = await _httpClient.PostAsync($"api/events/{eventId}/register", null);
                if (response.IsSuccessStatusCode)
                {
                    return (true, null);
                }
                var errorMessage = await response.Content.ReadAsStringAsync();
                return (false, errorMessage);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        public async Task<bool> UnregisterFromEventAsync(int eventId)
        {
            try
            {
                var response = await _httpClient.PostAsync($"api/events/{eventId}/unregister", null);
                if (!response.IsSuccessStatusCode)
                {
                    throw await HttpRequestExceptionExtensions.CreateFromResponseAsync(response);
                }
                return await response.Content.ReadFromJsonAsync<bool>();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<List<EventRegistration>> GetEventParticipantsAsync(int eventId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/events/{eventId}/participants");
                if (!response.IsSuccessStatusCode)
                {
                    throw await HttpRequestExceptionExtensions.CreateFromResponseAsync(response);
                }
                return await response.Content.ReadFromJsonAsync<List<EventRegistration>>() ?? new List<EventRegistration>();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<bool> RemoveParticipantAsync(int eventId, string userId)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"api/events/{eventId}/participants/{userId}");
                if (!response.IsSuccessStatusCode)
                {
                    throw await HttpRequestExceptionExtensions.CreateFromResponseAsync(response);
                }
                return await response.Content.ReadFromJsonAsync<bool>();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<bool> SendNotificationToParticipantAsync(int eventId, string userId, string message)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync($"api/events/{eventId}/notify/{userId}", message);
                if (!response.IsSuccessStatusCode)
                {
                    throw await HttpRequestExceptionExtensions.CreateFromResponseAsync(response);
                }
                return await response.Content.ReadFromJsonAsync<bool>();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<bool> SendNotificationToAllParticipantsAsync(int eventId, string message)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync($"api/events/{eventId}/notify-all", message);
                if (!response.IsSuccessStatusCode)
                {
                    throw await HttpRequestExceptionExtensions.CreateFromResponseAsync(response);
                }
                return await response.Content.ReadFromJsonAsync<bool>();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<bool> CancelEventAsync(int eventId)
        {
            try
            {
                var response = await _httpClient.PostAsync($"api/events/{eventId}/cancel", null);
                if (!response.IsSuccessStatusCode)
                {
                    throw await HttpRequestExceptionExtensions.CreateFromResponseAsync(response);
                }
                return await response.Content.ReadFromJsonAsync<bool>();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<bool> CheckRegistrationStatusAsync(int eventId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/events/{eventId}/registration-status");
                if (!response.IsSuccessStatusCode)
                {
                    throw await HttpRequestExceptionExtensions.CreateFromResponseAsync(response);
                }
                return await response.Content.ReadFromJsonAsync<bool>();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<byte[]> GenerateReportAsync(int eventId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/events/{eventId}/report");
                if (!response.IsSuccessStatusCode)
                {
                    throw await HttpRequestExceptionExtensions.CreateFromResponseAsync(response);
                }
                return await response.Content.ReadAsByteArrayAsync();
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<string> UploadEventImage(MultipartFormDataContent content)
        {
            try
            {
                var response = await _httpClient.PostAsync("api/events/upload-image", content);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
