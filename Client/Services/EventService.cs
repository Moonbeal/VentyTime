using System.Net.Http;
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
            try
            {
                var response = await _httpClient.GetAsync("api/events");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<List<Event>>() ?? new List<Event>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting events: {ex.Message}");
                return new List<Event>();
            }
        }

        public async Task<Event?> GetEventByIdAsync(int id)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/events/{id}");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<Event>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting event {id}: {ex.Message}");
                return null;
            }
        }

        public async Task<Event> CreateEventAsync(Event eventItem)
        {
            try
            {
                Console.WriteLine($"Creating event with date: {eventItem.StartDate}");
                
                // Get the local time zone offset in minutes for the event's date
                var offsetMinutes = (int)TimeZoneInfo.Local.GetUtcOffset(eventItem.StartDate).TotalMinutes;
                Console.WriteLine($"Time zone offset for event date: {offsetMinutes} minutes");
                
                using var request = new HttpRequestMessage(HttpMethod.Post, "api/events");
                request.Headers.Add("X-TimeZone-Offset", offsetMinutes.ToString());
                request.Content = JsonContent.Create(eventItem);

                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();
                
                var createdEvent = await response.Content.ReadFromJsonAsync<Event>();
                return createdEvent ?? throw new Exception("Created event is null");
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"HTTP error creating event: {ex.Message}");
                if (ex.StatusCode != null)
                {
                    Console.WriteLine($"Status code: {ex.StatusCode}");
                }
                throw;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating event: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        public async Task<Event> UpdateEventAsync(Event eventItem)
        {
            try
            {
                // Get the local time zone offset in minutes for the event's date
                var offsetMinutes = (int)TimeZoneInfo.Local.GetUtcOffset(eventItem.StartDate).TotalMinutes;
                Console.WriteLine($"Time zone offset for event date: {offsetMinutes} minutes");
                
                using var request = new HttpRequestMessage(HttpMethod.Put, $"api/events/{eventItem.Id}");
                request.Headers.Add("X-TimeZone-Offset", offsetMinutes.ToString());
                request.Content = JsonContent.Create(eventItem);

                var response = await _httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<Event>() ?? throw new Exception("Failed to update event");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating event: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> DeleteEventAsync(int id)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"api/events/{id}");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting event: {ex.Message}");
                return false;
            }
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

        public async Task<bool> IsEventFullAsync(int id)
        {
            var @event = await GetEventByIdAsync(id);
            return @event?.IsFull ?? false;
        }

        public async Task<bool> CancelEventAsync(int id)
        {
            var response = await _httpClient.PostAsync($"api/events/{id}/cancel", null);
            return response.IsSuccessStatusCode;
        }

        public async Task<byte[]> GenerateReportAsync(ReportPeriod period)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/events/report/{period}");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsByteArrayAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating report: {ex.Message}");
                throw;
            }
        }

        public async Task<ApiResponse<string>> UploadEventImage(MultipartFormDataContent content)
        {
            try
            {
                var response = await _httpClient.PostAsync("api/events/upload-image", content);
                response.EnsureSuccessStatusCode();
                var url = await response.Content.ReadAsStringAsync();
                return new ApiResponse<string> { IsSuccessful = true, Data = url.Trim('"') };
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"HTTP error uploading image: {ex.Message}");
                return new ApiResponse<string> { IsSuccessful = false, Message = ex.Message };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error uploading image: {ex.Message}");
                return new ApiResponse<string> { IsSuccessful = false, Message = ex.Message };
            }
        }
    }
}
