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

        [Required(ErrorMessage = "Date is required")]
        public DateTime Date { get; set; }

        [Required(ErrorMessage = "Time is required")]
        public string Time { get; set; } = string.Empty;

        [Required(ErrorMessage = "Price is required")]
        [Range(0, double.MaxValue, ErrorMessage = "Price must be greater than or equal to 0")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Location is required")]
        public string Location { get; set; } = string.Empty;

        public string? ImageUrl { get; set; }

        [Required]
        public string OrganizerId { get; set; } = string.Empty;

        public string OrganizerName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Maximum attendees is required")]
        [Range(1, 1000, ErrorMessage = "Maximum attendees must be between 1 and 1000")]
        public int MaxAttendees { get; set; }

        public virtual ICollection<Registration> Registrations { get; set; } = new List<Registration>();

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public bool IsRegistrationOpen => MaxAttendees == 0 || Registrations.Count < MaxAttendees;

        public DateTime GetDateTime() => Date.Date.Add(TimeSpan.Parse(Time));

        // For backward compatibility
        public string Name => Title;
    }
}
