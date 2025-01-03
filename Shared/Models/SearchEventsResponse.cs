using System.Collections.Generic;

namespace VentyTime.Shared.Models
{
    public class SearchEventsResponse
    {
        public IEnumerable<EventDto> Events { get; set; } = new List<EventDto>();
        public int TotalResults { get; set; }
        public string SearchQuery { get; set; } = string.Empty;
    }
}
