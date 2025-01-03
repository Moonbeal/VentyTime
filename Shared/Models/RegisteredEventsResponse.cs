using System.Collections.Generic;

namespace VentyTime.Shared.Models
{
    public class RegisteredEventsResponse
    {
        public IEnumerable<EventDto> Events { get; set; } = new List<EventDto>();
        public int TotalCount { get; set; }
        public string UserId { get; set; } = string.Empty;
    }
}
