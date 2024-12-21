using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace VentyTime.Shared.Models
{
    public class Event
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Title is required")]
        [StringLength(100, ErrorMessage = "Title must be between 3 and 100 characters", MinimumLength = 3)]
        public string Title { get; set; } = "";

        [Required(ErrorMessage = "Description is required")]
        [StringLength(500, ErrorMessage = "Description must be between 10 and 500 characters", MinimumLength = 10)]
        public string Description { get; set; } = "";

        [Required(ErrorMessage = "Category is required")]
        public string Category { get; set; } = "";

        [Required(ErrorMessage = "Location is required")]
        public string Location { get; set; } = "";

        [Required(ErrorMessage = "Start date is required")]
        public DateTime StartDate { get; set; }

        public DateTime GetStartDateTime()
        {
            return StartDate;
        }

        public bool HasStarted()
        {
            return GetStartDateTime() <= DateTime.UtcNow;
        }

        public bool IsFinished()
        {
            // Events are considered finished 24 hours after their start time
            return GetStartDateTime().AddHours(24) <= DateTime.UtcNow;
        }

        [Required(ErrorMessage = "Price is required")]
        [Range(0, double.MaxValue, ErrorMessage = "Price must be 0 or greater")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Capacity is required")]
        [Range(0, int.MaxValue, ErrorMessage = "Capacity must be 0 or greater")]
        public int MaxAttendees { get; set; }

        public string? ImageUrl { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        [Required]
        public string OrganizerId { get; set; } = "";

        [JsonIgnore]
        public ApplicationUser? Organizer { get; set; }

        [JsonIgnore]
        public virtual ICollection<Registration> Registrations { get; set; } = new List<Registration>();

        public bool IsFull => MaxAttendees > 0 && Registrations.Count >= MaxAttendees;
        
        public int CurrentParticipants => Registrations.Count;

        public string OrganizerName => Organizer?.UserName ?? "Unknown";

        public bool IsRegistrationOpen => MaxAttendees == 0 || Registrations.Count < MaxAttendees;

        // For backward compatibility
        public string Name => Title;
    }
}
