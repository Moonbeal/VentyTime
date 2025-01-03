using System.Net.Http;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Security.Claims;
using VentyTime.Shared.Models;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Logging;
using MudBlazor;

namespace VentyTime.Client.Services
{
    public class EventService : IEventService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILocalStorageService _localStorage;
        private readonly AuthenticationStateProvider _authStateProvider;
        private readonly ILogger<EventService> _logger;
        private readonly Dictionary<string, CachedEventData> _eventCache = new();
        private const int CacheExpirationMinutes = 5;

        public EventService(
            IHttpClientFactory httpClientFactory,
            ILocalStorageService localStorage,
            AuthenticationStateProvider authStateProvider,
            ILogger<EventService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _localStorage = localStorage;
            _authStateProvider = authStateProvider;
            _logger = logger;
        }

        private class CachedEventData
        {
            public (IEnumerable<EventDto> Events, int TotalCount) Data { get; set; }
            public DateTime ExpirationTime { get; set; }
        }

        private async Task<HttpClient> CreateClientAsync()
        {
            var client = _httpClientFactory.CreateClient("VentyTime.ServerAPI");
            var token = await _localStorage.GetItemAsync<string>("authToken");
            if (!string.IsNullOrEmpty(token))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            return client;
        }

        public async Task<(IEnumerable<EventDto> Events, int TotalCount)> GetEventsAsync(
            int page = 1,
            int? pageSize = null,
            string? category = null,
            string? searchQuery = null,
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            try
            {
                var client = await CreateClientAsync();
                var queryParams = new List<string>();

                // Only add page and pageSize if pageSize is specified
                if (pageSize.HasValue)
                {
                    queryParams.Add($"page={page}");
                    queryParams.Add($"pageSize={pageSize.Value}");
                }

                if (!string.IsNullOrEmpty(category))
                {
                    queryParams.Add($"category={Uri.EscapeDataString(category)}");
                }

                if (!string.IsNullOrEmpty(searchQuery))
                {
                    queryParams.Add($"searchQuery={Uri.EscapeDataString(searchQuery)}");
                }

                if (startDate.HasValue)
                {
                    queryParams.Add($"startDate={startDate.Value:yyyy-MM-dd}");
                }

                if (endDate.HasValue)
                {
                    queryParams.Add($"endDate={endDate.Value:yyyy-MM-dd}");
                }

                var url = $"api/events{(queryParams.Any() ? "?" + string.Join("&", queryParams) : "")}";
                
                var response = await client.GetFromJsonAsync<(IEnumerable<EventDto> Events, int TotalCount)>(url);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting events");
                throw;
            }
        }

        public async Task<EventDto?> GetEventByIdAsync(int id)
        {
            try
            {
                var client = await CreateClientAsync();
                var response = await client.GetAsync($"api/events/{id}");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<EventDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting event {EventId}", id);
                throw;
            }
        }

        public async Task<EventDto> CreateEventAsync(EventDto eventItem)
        {
            try
            {
                Console.WriteLine($"Creating event with date: {eventItem.StartDate}");
                var client = await CreateClientAsync();

                // Get the current user's ID
                var authState = await _authStateProvider.GetAuthenticationStateAsync();
                var user = authState.User;
                var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userId))
                {
                    throw new InvalidOperationException("User ID not found. Please make sure you are logged in.");
                }

                // Set the creator and organizer IDs
                eventItem.CreatorId = userId;
                eventItem.OrganizerId = userId;

                // Get the local time zone offset in minutes for the event's date
                var offsetMinutes = (int)TimeZoneInfo.Local.GetUtcOffset(eventItem.StartDate).TotalMinutes;
                Console.WriteLine($"Time zone offset for event date: {offsetMinutes} minutes");

                // Convert dates to UTC before sending
                eventItem.StartDate = DateTime.SpecifyKind(eventItem.StartDate, DateTimeKind.Local).ToUniversalTime();
                eventItem.EndDate = DateTime.SpecifyKind(eventItem.EndDate, DateTimeKind.Local).ToUniversalTime();
                eventItem.StartTime = eventItem.StartDate.TimeOfDay;

                Console.WriteLine($"Sending event with UTC dates - Start: {eventItem.StartDate}, End: {eventItem.EndDate}");

                using var request = new HttpRequestMessage(HttpMethod.Post, "api/events");
                request.Headers.Add("X-TimeZone-Offset", offsetMinutes.ToString());
                request.Content = JsonContent.Create(eventItem);

                var response = await client.SendAsync(request);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Server error: {errorContent}");
                    throw new HttpRequestException($"Server error: {errorContent}", null, response.StatusCode);
                }

                var createdEvent = await response.Content.ReadFromJsonAsync<EventDto>();
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

        public async Task<EventDto> UpdateEventAsync(int id, EventDto eventItem)
        {
            try
            {
                var client = await CreateClientAsync();

                // Get the current user's ID
                var authState = await _authStateProvider.GetAuthenticationStateAsync();
                var user = authState.User;
                var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userId))
                {
                    throw new InvalidOperationException("User ID not found. Please make sure you are logged in.");
                }

                // Check if the user is the organizer of the event
                var existingEvent = await GetEventAsync(id) 
                    ?? throw new InvalidOperationException("Event not found.");

                if (existingEvent.OrganizerId != userId)
                {
                    throw new InvalidOperationException("You don't have permission to update this event.");
                }

                // Get the local time zone offset in minutes for the event's date
                var offsetMinutes = (int)TimeZoneInfo.Local.GetUtcOffset(eventItem.StartDate).TotalMinutes;
                Console.WriteLine($"Time zone offset for event date: {offsetMinutes} minutes");

                // Convert dates to UTC before sending
                eventItem.StartDate = DateTime.SpecifyKind(eventItem.StartDate, DateTimeKind.Local).ToUniversalTime();
                if (eventItem.EndDate != default)
                {
                    eventItem.EndDate = DateTime.SpecifyKind(eventItem.EndDate, DateTimeKind.Local).ToUniversalTime();
                }
                eventItem.StartTime = eventItem.StartDate.TimeOfDay;

                using var request = new HttpRequestMessage(HttpMethod.Put, $"api/events/{id}");
                request.Headers.Add("X-TimeZone-Offset", offsetMinutes.ToString());
                request.Content = JsonContent.Create(eventItem);

                var response = await client.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Server error: {errorContent}");
                    throw new HttpRequestException($"Server error: {errorContent}", null, response.StatusCode);
                }

                var updatedEvent = await response.Content.ReadFromJsonAsync<EventDto>();
                return updatedEvent ?? throw new Exception("Failed to update event");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating event: {ex.Message}");
                throw;
            }
        }

        public async Task<EventDto> UpdateEventAsync(EventDto eventToUpdate)
        {
            return await UpdateEventAsync(eventToUpdate.Id, eventToUpdate);
        }

        public async Task<EventDto?> GetEventAsync(int id)
        {
            try
            {
                var client = await CreateClientAsync();
                return await client.GetFromJsonAsync<EventDto>($"api/events/{id}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting event {id}: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> DeleteEventAsync(int id)
        {
            try
            {
                var client = await CreateClientAsync();
                var response = await client.DeleteAsync($"api/events/{id}");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting event {id}: {ex.Message}");
                return false;
            }
        }

        public async Task<List<string>> GetCategoriesAsync()
        {
            try
            {
                var client = await CreateClientAsync();
                return await client.GetFromJsonAsync<List<string>>("api/events/categories") ?? new List<string>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting categories: {ex.Message}");
                return new List<string>();
            }
        }

        public async Task<List<EventDto>> GetEventsByOrganizerIdAsync(string organizerId)
        {
            try
            {
                var client = await CreateClientAsync();
                var response = await client.GetAsync($"api/events/organizer/{organizerId}");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<List<EventDto>>() ?? new List<EventDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting events for organizer {OrganizerId}", organizerId);
                throw;
            }
        }

        public async Task<(bool Success, string? ErrorMessage)> RegisterForEventAsync(int eventId)
        {
            try
            {
                var client = await CreateClientAsync();
                var response = await client.PostAsync($"api/events/{eventId}/register", null);
                
                if (response.IsSuccessStatusCode)
                {
                    return (true, null);
                }

                var error = await response.Content.ReadFromJsonAsync<ApiResponse<string>>();
                return (false, error?.Message ?? "Failed to register for the event");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering for event");
                return (false, "An error occurred while registering for the event");
            }
        }

        public async Task<List<EventDto>> GetRegisteredEventsAsync()
        {
            try
            {
                var client = await CreateClientAsync();
                var response = await client.GetAsync("api/events/registered");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<List<EventDto>>() ?? new List<EventDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting registered events");
                throw;
            }
        }

        public async Task<bool> UnregisterFromEventAsync(int eventId)
        {
            try
            {
                var client = await CreateClientAsync();
                var response = await client.DeleteAsync($"api/events/{eventId}/register");
                return response.IsSuccessStatusCode;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> CheckRegistrationStatusAsync(int eventId)
        {
            try
            {
                var client = await CreateClientAsync();
                var response = await client.GetAsync($"api/registrations/event/{eventId}/status");
                return response.IsSuccessStatusCode;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<List<EventDto>> GetUpcomingEventsAsync()
        {
            try
            {
                var client = await CreateClientAsync();
                var response = await client.GetAsync("api/events/upcoming");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<List<EventDto>>() ?? new List<EventDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting upcoming events");
                throw;
            }
        }

        public async Task<List<EventDto>> GetPopularEventsAsync()
        {
            try
            {
                var client = await CreateClientAsync();
                var response = await client.GetAsync("api/events/popular");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<List<EventDto>>() ?? new List<EventDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting popular events");
                throw;
            }
        }

        public async Task<byte[]> GenerateReportAsync(int eventId)
        {
            try
            {
                var client = await CreateClientAsync();
                var response = await client.GetAsync($"api/events/{eventId}/report");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsByteArrayAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating report: {ex.Message}");
                throw;
            }
        }

        public async Task<string> UploadEventImage(MultipartFormDataContent content)
        {
            try
            {
                var client = await CreateClientAsync();
                var response = await client.PostAsync("api/events/upload-image", content);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error uploading image: {ex.Message}");
                throw;
            }
        }

        public async Task<List<EventDto>> SearchEventsAsync(string searchQuery)
        {
            try
            {
                var client = await CreateClientAsync();
                var response = await client.GetAsync($"api/events/search?q={Uri.EscapeDataString(searchQuery)}");
                response.EnsureSuccessStatusCode();

                var wrapper = await response.Content.ReadFromJsonAsync<ApiResponse<List<EventDto>>>();
                return wrapper?.Data ?? new List<EventDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching events");
                return new List<EventDto>();
            }
        }

        public async Task<(IEnumerable<EventDto> Events, int TotalCount)> GetEventsAsync(int? pageSize = null, int? pageNumber = null)
        {
            var cacheKey = $"events_p{pageNumber}_s{pageSize}";

            // Try to get from cache first
            if (_eventCache.TryGetValue(cacheKey, out var cachedData) && 
                cachedData.ExpirationTime > DateTime.UtcNow)
            {
                return cachedData.Data;
            }

            try
            {
                var client = await CreateClientAsync();
                var query = new List<string>();
                if (pageSize.HasValue) query.Add($"pageSize={pageSize}");
                if (pageNumber.HasValue) query.Add($"pageNumber={pageNumber}");
                
                var url = $"api/events{(query.Any() ? "?" + string.Join("&", query) : "")}";
                var response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();
                
                var wrapper = await response.Content.ReadFromJsonAsync<ApiResponse<EventsResponse>>();
                if (wrapper?.Data == null)
                {
                    _logger.LogError("Failed to deserialize events response");
                    return (Array.Empty<EventDto>(), 0);
                }

                // Cache the response
                _eventCache[cacheKey] = new CachedEventData
                {
                    Data = (wrapper.Data.Events, wrapper.Data.TotalCount),
                    ExpirationTime = DateTime.UtcNow.AddMinutes(CacheExpirationMinutes)
                };
                return (wrapper.Data.Events, wrapper.Data.TotalCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading events");
                throw;
            }
        }

        public async Task<bool> IsEventFullAsync(int eventId)
        {
            try
            {
                var client = await CreateClientAsync();
                var response = await client.GetAsync($"api/events/{eventId}/is-full");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<bool>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking if event is full: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> CancelEventAsync(int eventId)
        {
            try
            {
                var client = await CreateClientAsync();
                var response = await client.PostAsync($"api/events/{eventId}/cancel", null);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error cancelling event: {ex.Message}");
                return false;
            }
        }
    }
}
