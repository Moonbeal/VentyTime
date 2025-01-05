using System.Net.Http;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Security.Claims;
using VentyTime.Shared.Models;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Authorization;

namespace VentyTime.Client.Services
{
    public class EventService : IEventService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILocalStorageService _localStorage;
        private readonly AuthenticationStateProvider _authStateProvider;

        public EventService(
            IHttpClientFactory httpClientFactory, 
            ILocalStorageService localStorage,
            AuthenticationStateProvider authStateProvider)
        {
            _httpClientFactory = httpClientFactory;
            _localStorage = localStorage;
            _authStateProvider = authStateProvider;
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

        public async Task<List<Event>> GetEventsAsync()
        {
            try
            {
                var client = await CreateClientAsync();
                var response = await client.GetAsync("api/events");
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
                var client = await CreateClientAsync();
                var response = await client.GetAsync($"api/events/{id}");
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
                var client = await CreateClientAsync();

                // Get the local time zone offset in minutes for the event's date
                var offsetMinutes = (int)TimeZoneInfo.Local.GetUtcOffset(eventItem.StartDate).TotalMinutes;
                Console.WriteLine($"Time zone offset for event date: {offsetMinutes} minutes");

                using var request = new HttpRequestMessage(HttpMethod.Put, $"api/events/{eventItem.Id}");
                request.Headers.Add("X-TimeZone-Offset", offsetMinutes.ToString());
                request.Content = JsonContent.Create(eventItem);

                var response = await client.SendAsync(request);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<Event>() ?? throw new Exception("Failed to update event");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating event: {ex.Message}");
                throw;
            }
        }

        public async Task<(bool success, string? error)> DeleteEventAsync(int id)
        {
            try
            {
                var client = await CreateClientAsync();
                var response = await client.DeleteAsync($"api/events/{id}");
                
                if (response.IsSuccessStatusCode)
                {
                    return (true, null);
                }
                
                var errorContent = await response.Content.ReadAsStringAsync();
                var error = response.StatusCode switch
                {
                    System.Net.HttpStatusCode.BadRequest => errorContent,
                    System.Net.HttpStatusCode.Unauthorized => "You are not authorized to delete this event",
                    System.Net.HttpStatusCode.Forbidden => "You do not have permission to delete this event",
                    System.Net.HttpStatusCode.NotFound => "Event not found",
                    _ => "Failed to delete event"
                };
                
                Console.WriteLine($"Error deleting event: {error}");
                return (false, error);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting event: {ex.Message}");
                return (false, "An unexpected error occurred while deleting the event");
            }
        }

        public async Task<List<string>> GetCategoriesAsync()
        {
            try
            {
                var client = await CreateClientAsync();
                var response = await client.GetAsync("api/categories");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<List<string>>() ?? new List<string>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting categories: {ex.Message}");
                return new List<string>();
            }
        }

        public async Task<List<Event>> GetEventsByOrganizerIdAsync(string organizerId)
        {
            try
            {
                var client = await CreateClientAsync();
                var response = await client.GetAsync($"api/events/organizer/{organizerId}");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<List<Event>>() ?? new List<Event>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting events for organizer {organizerId}: {ex.Message}");
                return new List<Event>();
            }
        }

        public async Task<(bool Success, string? ErrorMessage)> RegisterForEventAsync(int eventId)
        {
            try
            {
                var client = await CreateClientAsync();
                var response = await client.PostAsync($"api/events/{eventId}/register", null);
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadFromJsonAsync<ErrorResponse>();
                    return (false, errorContent?.Message ?? "Failed to register for the event");
                }
                return (true, null);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error registering for event {eventId}: {ex.Message}");
                return (false, "An unexpected error occurred");
            }
        }

        private class ErrorResponse
        {
            public string? Message { get; set; }
        }

        public async Task<List<Event>> GetRegisteredEventsAsync()
        {
            try
            {
                var client = await CreateClientAsync();
                return await client.GetFromJsonAsync<List<Event>>("api/events/registered") ?? new List<Event>();
            }
            catch (Exception)
            {
                return new List<Event>();
            }
        }

        public async Task<List<Event>> GetOrganizedEventsAsync()
        {
            try
            {
                var client = await CreateClientAsync();
                return await client.GetFromJsonAsync<List<Event>>("api/events/organized") ?? new List<Event>();
            }
            catch (Exception)
            {
                return new List<Event>();
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
                var response = await client.GetAsync($"api/events/{eventId}/registration-status");
                return response.IsSuccessStatusCode;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<List<Event>> GetUpcomingEventsAsync()
        {
            try
            {
                var client = await CreateClientAsync();
                var response = await client.GetAsync("api/events/upcoming");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<List<Event>>() ?? new List<Event>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting upcoming events: {ex.Message}");
                return new List<Event>();
            }
        }

        public async Task<List<Event>> GetPopularEventsAsync()
        {
            try
            {
                var client = await CreateClientAsync();
                var response = await client.GetAsync("api/events/popular");
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadFromJsonAsync<List<Event>>() ?? new List<Event>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting popular events: {ex.Message}");
                return new List<Event>();
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
                var response = await client.PostAsync("api/Upload", content);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Upload failed. Status: {response.StatusCode}, Error: {errorContent}");
                    throw new HttpRequestException($"Upload failed with status {response.StatusCode}: {errorContent}");
                }
                
                var result = await response.Content.ReadFromJsonAsync<ImageUploadResult>();
                if (result == null || string.IsNullOrEmpty(result.ImageUrl))
                {
                    throw new Exception("Server returned invalid image upload result");
                }
                return result.ImageUrl;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error uploading image: {ex.Message}");
                throw;
            }
        }

        private class ImageUploadResult
        {
            public string ImageUrl { get; set; } = string.Empty;
            public string ThumbnailUrl { get; set; } = string.Empty;
        }

        public async Task<List<Event>> SearchEventsAsync(string searchTerm)
        {
            try
            {
                var client = await CreateClientAsync();
                return await client.GetFromJsonAsync<List<Event>>($"api/events/search?query={Uri.EscapeDataString(searchTerm)}") ?? new List<Event>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error searching events: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> IsEventFullAsync(int eventId)
        {
            try
            {
                var client = await CreateClientAsync();
                var @event = await client.GetFromJsonAsync<Event>($"api/events/{eventId}");
                return @event?.IsFull ?? false;
            }
            catch (Exception)
            {
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
            catch (Exception)
            {
                return false;
            }
        }
    }
}
