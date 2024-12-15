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

        public RegistrationService(
            HttpClient httpClient,
            ISnackbar snackbar,
            ILocalStorageService localStorage)
        {
            _httpClient = httpClient;
            _snackbar = snackbar;
            _localStorage = localStorage;
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

                var response = await _httpClient.PostAsync($"api/registrations/event/{eventId}", null);
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

        public async Task<bool> CancelRegistrationAsync(int registrationId)
        {
            try
            {
                var token = await _localStorage.GetItemAsync<string>("authToken");
                if (string.IsNullOrEmpty(token))
                    throw new UnauthorizedAccessException();

                _httpClient.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                var response = await _httpClient.DeleteAsync($"api/registrations/{registrationId}");
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
                _snackbar.Add($"An error occurred: {ex.Message}", Severity.Error);
                return false;
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

                var registrations = await _httpClient.GetFromJsonAsync<List<Registration>>($"api/registrations/event/{eventId}");
                return registrations ?? new List<Registration>();
            }
            catch (Exception ex)
            {
                _snackbar.Add($"An error occurred: {ex.Message}", Severity.Error);
                return new List<Registration>();
            }
        }

        public async Task<List<Registration>> GetUserRegistrationsAsync(string userId)
        {
            try
            {
                var token = await _localStorage.GetItemAsync<string>("authToken");
                if (string.IsNullOrEmpty(token))
                    throw new UnauthorizedAccessException();

                _httpClient.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                var registrations = await _httpClient.GetFromJsonAsync<List<Registration>>($"api/registrations/user/{userId}");
                return registrations ?? new List<Registration>();
            }
            catch (Exception ex)
            {
                _snackbar.Add($"An error occurred: {ex.Message}", Severity.Error);
                return new List<Registration>();
            }
        }
    }
}
