using System.Net.Http.Json;
using VentyTime.Shared.Models;

namespace VentyTime.Client.Services;

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
            var events = await _httpClient.GetFromJsonAsync<List<Event>>("api/events");
            return events ?? new List<Event>();
        }
        catch
        {
            return new List<Event>();
        }
    }

    public async Task<Event?> GetEventByIdAsync(int id)
    {
        try
        {
            return await _httpClient.GetFromJsonAsync<Event>($"api/events/{id}");
        }
        catch
        {
            return null;
        }
    }

    public async Task<Event?> CreateEventAsync(Event eventModel)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("api/events", eventModel);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<Event>();
            }
            return null;
        }
        catch
        {
            return null;
        }
    }

    public async Task<Event?> UpdateEventAsync(Event eventModel)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"api/events/{eventModel.Id}", eventModel);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<Event>();
            }
            return null;
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> DeleteEventAsync(int id)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"api/events/{id}");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
