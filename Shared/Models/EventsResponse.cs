using System.Collections.Generic;

namespace VentyTime.Shared.Models
{
    public class EventsResponse
    {
        public IEnumerable<EventDto> Events { get; set; } = new List<EventDto>();
        public int TotalCount { get; set; }
    }
}
