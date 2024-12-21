using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using VentyTime.Shared.Models;
using MudBlazor;
using Blazored.LocalStorage;

namespace VentyTime.Client.Services
{
    public class RegistrationService : IRegistrationService
    {
        private readonly HttpClient _httpClient;
        private readonly ISnackbar _snackbar;
        private readonly ILocalStorageService _localStorage;
        private readonly IAuthService _authService;

        public RegistrationService(
            HttpClient httpClient,
            ISnackbar snackbar,
            ILocalStorageService localStorage,
            IAuthService authService)
        {
            _httpClient = httpClient;
            _snackbar = snackbar;
            _localStorage = localStorage;
            _authService = authService;
        }

        public async Task<RegistrationResponse> RegisterForEventAsync(int eventId)
        {
            try
            {
                var token = await _localStorage.GetItemAsync<string>("authToken");
                if (string.IsNullOrEmpty(token))
                    return new RegistrationResponse(false, "User not authenticated");

                _httpClient.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                var response = await _httpClient.PostAsync($"api/registrations/{eventId}/register", null);
                if (response.IsSuccessStatusCode)
                {
                    _snackbar.Add("Successfully registered for the event!", Severity.Success);
                    return new RegistrationResponse(true, "Successfully registered for the event!");
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _snackbar.Add(error, Severity.Error);
                    return new RegistrationResponse(false, error);
                }
            }
            catch (Exception ex)
            {
                _snackbar.Add($"Error: {ex.Message}", Severity.Error);
                return new RegistrationResponse(false, ex.Message);
            }
        }

        public async Task<RegistrationResponse> UnregisterFromEventAsync(int eventId)
        {
            try
            {
                var token = await _localStorage.GetItemAsync<string>("authToken");
                if (string.IsNullOrEmpty(token))
                    return new RegistrationResponse(false, "User not authenticated");

                _httpClient.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                var response = await _httpClient.PostAsync($"api/registrations/{eventId}/unregister", null);
                if (response.IsSuccessStatusCode)
                {
                    _snackbar.Add("Successfully unregistered from the event!", Severity.Success);
                    return new RegistrationResponse(true, "Successfully unregistered from the event!");
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _snackbar.Add(error, Severity.Error);
                    return new RegistrationResponse(false, error);
                }
            }
            catch (Exception ex)
            {
                _snackbar.Add($"Error: {ex.Message}", Severity.Error);
                return new RegistrationResponse(false, ex.Message);
            }
        }

        public async Task<bool> CancelRegistrationAsync(int eventId)
        {
            try
            {
                var token = await _localStorage.GetItemAsync<string>("authToken");
                if (string.IsNullOrEmpty(token))
                    return false;

                _httpClient.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                var response = await _httpClient.PostAsync($"api/registrations/{eventId}/cancel", null);
                if (response.IsSuccessStatusCode)
                {
                    _snackbar.Add("Successfully cancelled registration!", Severity.Success);
                    return true;
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _snackbar.Add(error, Severity.Error);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _snackbar.Add($"Error: {ex.Message}", Severity.Error);
                return false;
            }
        }

        public async Task<List<Registration>> GetUserRegistrationsAsync()
        {
            try
            {
                var token = await _localStorage.GetItemAsync<string>("authToken");
                if (string.IsNullOrEmpty(token))
                    return new List<Registration>();

                _httpClient.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                var registrations = await _httpClient.GetFromJsonAsync<List<Registration>>("api/registrations/user");
                return registrations ?? new List<Registration>();
            }
            catch (Exception ex)
            {
                _snackbar.Add($"Error: {ex.Message}", Severity.Error);
                return new List<Registration>();
            }
        }

        public async Task<List<Registration>> GetEventRegistrationsAsync(int eventId)
        {
            try
            {
                var token = await _localStorage.GetItemAsync<string>("authToken");
                if (string.IsNullOrEmpty(token))
                    return new List<Registration>();

                _httpClient.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                var registrations = await _httpClient.GetFromJsonAsync<List<Registration>>($"api/registrations/event/{eventId}");
                return registrations ?? new List<Registration>();
            }
            catch (Exception ex)
            {
                _snackbar.Add($"Error: {ex.Message}", Severity.Error);
                return new List<Registration>();
            }
        }

        public async Task<Registration?> GetRegistrationAsync(int eventId, string userId)
        {
            try
            {
                var token = await _localStorage.GetItemAsync<string>("authToken");
                if (string.IsNullOrEmpty(token))
                    throw new UnauthorizedAccessException();

                _httpClient.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                return await _httpClient.GetFromJsonAsync<Registration>($"api/registrations/{eventId}/user/{userId}");
            }
            catch (Exception ex)
            {
                _snackbar.Add($"An error occurred: {ex.Message}", Severity.Error);
                return null;
            }
        }

        public async Task<RegistrationResponse> IsUserRegisteredAsync(int eventId)
        {
            try
            {
                var userId = await _authService.GetUserId();
                if (string.IsNullOrEmpty(userId))
                {
                    _snackbar.Add("User ID not found. Please log in again.", Severity.Error);
                    return new RegistrationResponse(false, "User not authenticated");
                }
                var registration = await GetRegistrationAsync(eventId, userId);
                if (registration != null)
                {
                    return new RegistrationResponse(true, "User is registered for the event!");
                }
                else
                {
                    return new RegistrationResponse(false, "User is not registered for the event!");
                }
            }
            catch (Exception ex)
            {
                _snackbar.Add($"Error: {ex.Message}", Severity.Error);
                return new RegistrationResponse(false, ex.Message);
            }
        }

        public async Task<bool> IsRegisteredForEventAsync(int eventId)
        {
            try
            {
                var token = await _localStorage.GetItemAsync<string>("authToken");
                if (string.IsNullOrEmpty(token))
                    return false;

                _httpClient.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                var response = await _httpClient.GetAsync($"api/registrations/{eventId}/isregistered");
                return response.IsSuccessStatusCode && await response.Content.ReadFromJsonAsync<bool>();
            }
            catch (Exception ex)
            {
                _snackbar.Add($"Error checking registration status: {ex.Message}", Severity.Error);
                return false;
            }
        }

        public async Task<int> GetRegisteredUsersCountAsync(int eventId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/registrations/{eventId}/count");
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<int>();
                }
                
                _snackbar.Add("Error getting registered users count", Severity.Error);
                return 0;
            }
            catch (Exception ex)
            {
                _snackbar.Add($"Error: {ex.Message}", Severity.Error);
                return 0;
            }
        }
    }
}
