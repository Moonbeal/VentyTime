using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace VentyTime.Shared.Models
{
    public class Event
    {
        public int Id { get; set; }

        [Required]
        public string Title { get; set; } = "";

        [Required]
        public string Description { get; set; } = "";

        [Required]
        public string Category { get; set; } = "";

        [Required]
        public decimal Price { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public TimeSpan? StartTime { get; set; }

        [Required]
        public string Location { get; set; } = "";

        [Required]
        public int MaxAttendees { get; set; }

        public string? ImageUrl { get; set; }

        [Required]
        public string OrganizerId { get; set; } = "";

        public virtual ApplicationUser? Organizer { get; set; }

        public virtual ICollection<Registration> Registrations { get; set; } = new List<Registration>();

        public bool IsFull => MaxAttendees > 0 && Registrations.Count >= MaxAttendees;
        public bool HasStarted => DateTime.Now > StartDate.Add(StartTime ?? TimeSpan.Zero);
        public bool IsFinished => HasStarted && DateTime.Now > StartDate.Add(StartTime ?? TimeSpan.Zero).AddHours(4); // Assuming events last 4 hours
        public int CurrentParticipants => Registrations.Count;

        public string OrganizerName => Organizer?.UserName ?? "Unknown";

        public bool IsRegistrationOpen => MaxAttendees == 0 || Registrations.Count < MaxAttendees;

        public DateTime GetDateTime() => StartDate.Date.Add(StartTime ?? TimeSpan.Zero);

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // For backward compatibility
        public string Name => Title;
    }
}
