using System;
using VentyTime.Shared.Models;

namespace VentyTime.Server.Models
{
    public class Registration
    {
        public int EventId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
        public bool IsCancelled { get; set; }
        public DateTime? CancelledAt { get; set; }

        // Navigation properties
        public virtual Event Event { get; set; } = null!;
        public virtual ApplicationUser User { get; set; } = null!;
    }
}
