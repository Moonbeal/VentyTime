using System.Collections.Generic;

namespace VentyTime.Shared.Models
{
    public class UpcomingEventsResponse
    {
        public IEnumerable<EventDto> Events { get; set; } = new List<EventDto>();
        public int Count { get; set; }
        public int TotalUpcoming { get; set; }
    }
}
