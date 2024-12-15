using VentyTime.Shared.Models;

namespace VentyTime.Client.Services;

public interface IEventService
{
    Task<List<Event>> GetEventsAsync();
    Task<Event?> GetEventByIdAsync(int id);
    Task<Event?> CreateEventAsync(Event eventModel);
    Task<Event?> UpdateEventAsync(Event eventModel);
    Task<bool> DeleteEventAsync(int id);
    Task<List<Event>> GetEventsByOrganizerIdAsync(string organizerId);
}
