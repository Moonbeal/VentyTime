using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using VentyTime.Shared.Models;
using MudBlazor;
using Blazored.LocalStorage;
using Microsoft.Extensions.Logging;

namespace VentyTime.Client.Services
{
    public class RegistrationService : IRegistrationService
    {
        private readonly HttpClient _httpClient;
        private readonly ISnackbar _snackbar;
        private readonly ILocalStorageService _localStorage;
        private readonly ILogger<RegistrationService> _logger;

        public RegistrationService(
            HttpClient httpClient,
            ISnackbar snackbar,
            ILocalStorageService localStorage,
            ILogger<RegistrationService> logger)
        {
            _httpClient = httpClient;
            _snackbar = snackbar;
            _localStorage = localStorage;
            _logger = logger;
        }

        public async Task<RegistrationResponse> RegisterForEventAsync(int eventId)
        {
            try
            {
                var token = await _localStorage.GetItemAsync<string>("authToken");
                if (string.IsNullOrEmpty(token))
                {
                    return new RegistrationResponse { Success = false, Message = "You must be logged in to register for an event." };
                }

                var registration = new EventRegistration
                {
                    EventId = eventId,
                    RegistrationDate = DateTime.UtcNow
                };

                var response = await _httpClient.PostAsJsonAsync($"api/registrations", registration);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<EventRegistration>();
                    return new RegistrationResponse { Success = true, Registration = result };
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to register for event. Status: {StatusCode}, Error: {Error}", response.StatusCode, error);
                    return new RegistrationResponse { Success = false, Message = error };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering for event {EventId}", eventId);
                return new RegistrationResponse { Success = false, Message = "An error occurred while registering for the event" };
            }
        }

        public async Task<bool> IsRegisteredForEventAsync(int eventId)
        {
            try
            {
                var token = await _localStorage.GetItemAsync<string>("authToken");
                if (string.IsNullOrEmpty(token))
                {
                    return false;
                }

                var response = await _httpClient.GetAsync($"api/registrations/{eventId}/status");
                return response.IsSuccessStatusCode && await response.Content.ReadFromJsonAsync<bool>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking registration status for event {EventId}", eventId);
                return false;
            }
        }

        public async Task<bool> HasPendingRegistrationAsync(int eventId)
        {
            try
            {
                var token = await _localStorage.GetItemAsync<string>("authToken");
                if (string.IsNullOrEmpty(token))
                {
                    return false;
                }

                var response = await _httpClient.GetAsync($"api/registrations/{eventId}/pending");
                return response.IsSuccessStatusCode && await response.Content.ReadFromJsonAsync<bool>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking pending registration status for event {EventId}", eventId);
                return false;
            }
        }

        public async Task<bool> CancelRegistrationAsync(int eventId)
        {
            try
            {
                var token = await _localStorage.GetItemAsync<string>("authToken");
                if (string.IsNullOrEmpty(token))
                {
                    return false;
                }

                var response = await _httpClient.DeleteAsync($"api/registrations/{eventId}");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error canceling registration for event {EventId}", eventId);
                return false;
            }
        }

        public async Task<RegistrationResponse> RegisterForEventAsync(int eventId, string userId)
        {
            try
            {
                var registration = new EventRegistration
                {
                    EventId = eventId,
                    UserId = userId,
                    RegistrationDate = DateTime.UtcNow
                };

                var response = await _httpClient.PostAsJsonAsync($"api/registrations", registration);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<EventRegistration>();
                    return new RegistrationResponse { Success = true, Registration = result };
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to register for event. Status: {StatusCode}, Error: {Error}", response.StatusCode, error);
                    return new RegistrationResponse { Success = false, Message = error };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error registering for event {EventId}", eventId);
                return new RegistrationResponse { Success = false, Message = "An error occurred while registering for the event" };
            }
        }

        public async Task<RegistrationResponse> UnregisterFromEventAsync(int eventId, string userId)
        {
            try
            {
                var response = await _httpClient.DeleteAsync($"api/registrations/{eventId}/{userId}");
                if (response.IsSuccessStatusCode)
                {
                    return new RegistrationResponse { Success = true };
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to unregister from event. Status: {StatusCode}, Error: {Error}", response.StatusCode, error);
                    return new RegistrationResponse { Success = false, Message = error };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unregistering from event {EventId}", eventId);
                return new RegistrationResponse { Success = false, Message = "An error occurred while unregistering from the event" };
            }
        }

        public async Task<EventRegistration?> GetRegistrationAsync(int eventId, string userId)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<EventRegistration>($"api/registrations/{eventId}/{userId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting registration for event {EventId} and user {UserId}", eventId, userId);
                return null;
            }
        }

        public async Task<bool> IsRegisteredForEventAsync(int eventId, string userId)
        {
            try
            {
                var registration = await GetRegistrationAsync(eventId, userId);
                return registration != null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking registration status for event {EventId}", eventId);
                return false;
            }
        }

        public async Task<bool> HasPendingRegistrationAsync(int eventId, string userId)
        {
            try
            {
                var registration = await GetRegistrationAsync(eventId, userId);
                return registration != null && !registration.IsConfirmed;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking pending registration status for event {EventId}", eventId);
                return false;
            }
        }

        public async Task<bool> CancelRegistrationAsync(int eventId, string userId)
        {
            try
            {
                var response = await UnregisterFromEventAsync(eventId, userId);
                return response.Success;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error canceling registration for event {EventId}", eventId);
                return false;
            }
        }

        public async Task<List<UserEventRegistration>> GetUserRegistrationsAsync()
        {
            try
            {
                var token = await _localStorage.GetItemAsync<string>("authToken");
                if (string.IsNullOrEmpty(token))
                    return new List<UserEventRegistration>();

                _httpClient.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                return await _httpClient.GetFromJsonAsync<List<UserEventRegistration>>("api/registrations/user") ?? new List<UserEventRegistration>();
            }
            catch (Exception ex)
            {
                _snackbar.Add("Error fetching user registrations", Severity.Error);
                Console.WriteLine($"Error fetching user registrations: {ex}");
                return new List<UserEventRegistration>();
            }
        }

        private class ApiErrorResponse
        {
            public string? Message { get; set; }
        }

        private class StatusResponse
        {
            public RegistrationStatus Status { get; set; }
        }
    }
}
