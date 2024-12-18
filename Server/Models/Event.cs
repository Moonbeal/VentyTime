using System;
using System.Collections.Generic;

namespace VentyTime.Server.Models
{
    public class Event
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public decimal Price { get; set; }
        public int MaxAttendees { get; set; }
        public string Category { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public string? ImageUrl { get; set; }

        // Foreign key for organizer
        public string OrganizerId { get; set; } = string.Empty;

        // Navigation properties
        public virtual ApplicationUser Organizer { get; set; } = null!;
        public virtual ICollection<Registration> Registrations { get; set; } = new List<Registration>();
    }
}
