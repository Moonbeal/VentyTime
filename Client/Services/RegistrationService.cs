using System.Net.Http.Json;
using VentyTime.Shared.Models;
using MudBlazor;
using Blazored.LocalStorage;
using VentyTime.Client.Extensions;

namespace VentyTime.Client.Services
{
    public class RegistrationService : IRegistrationService
    {
        private readonly HttpClient _httpClient;
        private readonly ISnackbar _snackbar;
        private readonly ILocalStorageService _localStorage;

        public RegistrationService(
            IHttpClientFactory httpClientFactory,
            ISnackbar snackbar,
            ILocalStorageService localStorage)
        {
            _httpClient = httpClientFactory.CreateClient("VentyTime.ServerAPI");
            _snackbar = snackbar;
            _localStorage = localStorage;
        }

        public async Task<List<EventRegistration>> GetEventRegistrationsAsync(int eventId)
        {
            try
            {
                var token = await _localStorage.GetItemAsync<string>("authToken");
                if (string.IsNullOrEmpty(token))
                    return new List<EventRegistration>();

                _httpClient.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                var response = await _httpClient.GetAsync($"api/registrations/event/{eventId}");
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<List<EventRegistration>>() ?? new List<EventRegistration>();
                }
                throw await HttpRequestExceptionExtensions.CreateFromResponseAsync(response);
            }
            catch (Exception ex)
            {
                _snackbar.Add($"Error: {ex.Message}", Severity.Error);
                return new List<EventRegistration>();
            }
        }

        public async Task<EventRegistration> GetRegistrationAsync(int registrationId)
        {
            try
            {
                var token = await _localStorage.GetItemAsync<string>("authToken");
                if (string.IsNullOrEmpty(token))
                    throw new UnauthorizedAccessException();

                _httpClient.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                var response = await _httpClient.GetAsync($"api/registrations/{registrationId}");
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<EventRegistration>() ?? throw new InvalidOperationException("Registration not found");
                }
                throw await HttpRequestExceptionExtensions.CreateFromResponseAsync(response);
            }
            catch (Exception ex)
            {
                _snackbar.Add($"Error: {ex.Message}", Severity.Error);
                throw;
            }
        }

        public async Task<RegistrationResponse> RegisterForEventAsync(int eventId)
        {
            try
            {
                var token = await _localStorage.GetItemAsync<string>("authToken");
                if (string.IsNullOrEmpty(token))
                {
                    _snackbar.Add("You must be logged in to register for events", Severity.Warning);
                    return new RegistrationResponse(false, "You must be logged in to register for events");
                }

                _httpClient.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                var response = await _httpClient.PostAsync($"api/registrations/event/{eventId}", null);
                if (response.IsSuccessStatusCode)
                {
                    _snackbar.Add("Successfully registered for the event!", Severity.Success);
                    return new RegistrationResponse(true, "Successfully registered for the event!");
                }

                var error = await response.Content.ReadAsStringAsync();
                _snackbar.Add(error, Severity.Error);
                return new RegistrationResponse(false, error);
            }
            catch (Exception ex)
            {
                _snackbar.Add($"Error: {ex.Message}", Severity.Error);
                return new RegistrationResponse(false, ex.Message);
            }
        }

        public async Task<bool> UpdateRegistrationStatusAsync(int registrationId, RegistrationStatus status)
        {
            try
            {
                var token = await _localStorage.GetItemAsync<string>("authToken");
                if (string.IsNullOrEmpty(token))
                    throw new UnauthorizedAccessException();

                _httpClient.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                var response = await _httpClient.PutAsJsonAsync($"api/registrations/{registrationId}/status", status);
                if (response.IsSuccessStatusCode)
                {
                    return true;
                }
                throw await HttpRequestExceptionExtensions.CreateFromResponseAsync(response);
            }
            catch (Exception ex)
            {
                _snackbar.Add($"Error: {ex.Message}", Severity.Error);
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
                    return true;
                }
                throw await HttpRequestExceptionExtensions.CreateFromResponseAsync(response);
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

                var response = await _httpClient.GetAsync("api/registrations/user");
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<List<Registration>>() ?? new List<Registration>();
                }
                throw await HttpRequestExceptionExtensions.CreateFromResponseAsync(response);
            }
            catch (Exception ex)
            {
                _snackbar.Add($"Error: {ex.Message}", Severity.Error);
                return new List<Registration>();
            }
        }

        public async Task<RegistrationResponse> UnregisterFromEventAsync(int eventId)
        {
            try
            {
                var token = await _localStorage.GetItemAsync<string>("authToken");
                if (string.IsNullOrEmpty(token))
                {
                    return new RegistrationResponse(false, "You must be logged in to unregister from events");
                }

                _httpClient.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                var response = await _httpClient.DeleteAsync($"api/registrations/event/{eventId}");
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

        public async Task<bool> IsRegisteredForEventAsync(int eventId)
        {
            try
            {
                var token = await _localStorage.GetItemAsync<string>("authToken");
                if (string.IsNullOrEmpty(token))
                    return false;

                _httpClient.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                var response = await _httpClient.GetAsync($"api/registrations/event/{eventId}/status");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _snackbar.Add($"Error: {ex.Message}", Severity.Error);
                return false;
            }
        }

        public async Task<int> GetRegisteredUsersCountAsync(int eventId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"api/registrations/event/{eventId}/count");
                if (response.IsSuccessStatusCode)
                {
                    var count = await response.Content.ReadFromJsonAsync<int>();
                    return count;
                }
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
