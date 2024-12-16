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

        public async Task<bool> RegisterForEventAsync(int eventId)
        {
            try
            {
                var token = await _localStorage.GetItemAsync<string>("authToken");
                if (string.IsNullOrEmpty(token))
                    throw new UnauthorizedAccessException();

                _httpClient.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                var response = await _httpClient.PostAsync($"api/registrations/{eventId}/register", null);
                if (response.IsSuccessStatusCode)
                {
                    _snackbar.Add("Successfully registered for the event!", Severity.Success);
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
                _snackbar.Add($"An error occurred: {ex.Message}", Severity.Error);
                return false;
            }
        }

        public async Task<bool> UnregisterFromEventAsync(int eventId)
        {
            try
            {
                var token = await _localStorage.GetItemAsync<string>("authToken");
                if (string.IsNullOrEmpty(token))
                    throw new UnauthorizedAccessException();

                _httpClient.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                var response = await _httpClient.PostAsync($"api/registrations/{eventId}/unregister", null);
                if (response.IsSuccessStatusCode)
                {
                    _snackbar.Add("Successfully unregistered from the event!", Severity.Success);
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
                _snackbar.Add($"An error occurred: {ex.Message}", Severity.Error);
                return false;
            }
        }

        public async Task<bool> CancelRegistrationAsync(int eventId)
        {
            try
            {
                var token = await _localStorage.GetItemAsync<string>("authToken");
                if (string.IsNullOrEmpty(token))
                    throw new UnauthorizedAccessException();

                _httpClient.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                var response = await _httpClient.PostAsync($"api/registrations/{eventId}/cancel", null);
                if (response.IsSuccessStatusCode)
                {
                    _snackbar.Add("Successfully cancelled registration for the event!", Severity.Success);
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
                _snackbar.Add($"An error occurred: {ex.Message}", Severity.Error);
                return false;
            }
        }

        public async Task<List<Registration>> GetUserRegistrationsAsync()
        {
            try
            {
                var userId = await _authService.GetUserId();
                if (string.IsNullOrEmpty(userId)) return new List<Registration>();

                var token = await _localStorage.GetItemAsync<string>("authToken");
                if (string.IsNullOrEmpty(token))
                    throw new UnauthorizedAccessException();

                _httpClient.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                var response = await _httpClient.GetFromJsonAsync<List<Registration>>($"api/registrations/user");
                return response ?? new List<Registration>();
            }
            catch (Exception ex)
            {
                _snackbar.Add($"An error occurred: {ex.Message}", Severity.Error);
                return new List<Registration>();
            }
        }

        public async Task<List<Registration>> GetEventRegistrationsAsync(int eventId)
        {
            try
            {
                var token = await _localStorage.GetItemAsync<string>("authToken");
                if (string.IsNullOrEmpty(token))
                    throw new UnauthorizedAccessException();

                _httpClient.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                var response = await _httpClient.GetFromJsonAsync<List<Registration>>($"api/registrations/event/{eventId}");
                return response ?? new List<Registration>();
            }
            catch (Exception ex)
            {
                _snackbar.Add($"An error occurred: {ex.Message}", Severity.Error);
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

        public async Task<bool> IsUserRegisteredAsync(int eventId)
        {
            try
            {
                var userId = await _authService.GetUserId();
                var registration = await GetRegistrationAsync(eventId, userId);
                return registration != null;
            }
            catch (Exception ex)
            {
                _snackbar.Add($"An error occurred: {ex.Message}", Severity.Error);
                return false;
            }
        }
    }
}
