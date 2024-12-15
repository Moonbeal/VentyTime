using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace VentyTime.Shared.Models
{
    public class Event
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Title is required")]
        [StringLength(100, ErrorMessage = "Title cannot be longer than 100 characters")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Start date is required")]
        public DateTime StartDate { get; set; }

        [Required(ErrorMessage = "Start time is required")]
        public TimeSpan StartTime { get; set; }

        [Required(ErrorMessage = "Location is required")]
        public string Location { get; set; } = string.Empty;

        public string ImageUrl { get; set; } = string.Empty;

        [Required]
        public string OrganizerId { get; set; } = string.Empty;

        public virtual ApplicationUser? Organizer { get; set; }

        public virtual ICollection<Registration> Registrations { get; set; } = new List<Registration>();

        [Required(ErrorMessage = "Price is required")]
        [Range(0, double.MaxValue, ErrorMessage = "Price must be greater than or equal to 0")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Maximum attendees is required")]
        [Range(0, int.MaxValue, ErrorMessage = "Maximum attendees must be greater than or equal to 0")]
        public int MaxAttendees { get; set; }

        public string OrganizerName => Organizer?.UserName ?? "Unknown";

        public bool IsRegistrationOpen => MaxAttendees == 0 || Registrations.Count < MaxAttendees;

        public DateTime GetDateTime() => StartDate.Date.Add(StartTime);

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // For backward compatibility
        public string Name => Title;
    }
}
