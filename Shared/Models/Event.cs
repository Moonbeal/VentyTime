using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace VentyTime.Shared.Models
{
    public enum EventType
    {
        Conference,
        Workshop,
        Meetup,
        Webinar,
        Social,
        Other
    }

    public static class EventCategories
    {
        public const string Technology = "Technology";
        public const string Music = "Music";
        public const string Sports = "Sports";
        public const string FoodAndDrink = "Food & Drink";
        public const string ArtsAndCulture = "Arts & Culture";
        public const string Business = "Business";

        public static readonly string[] All = new[]
        {
            Technology,
            Music,
            Sports,
            FoodAndDrink,
            ArtsAndCulture,
            Business
        };
    }

    public class Event
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Title is required")]
        [StringLength(100, ErrorMessage = "Title must be between 3 and 100 characters", MinimumLength = 3)]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required")]
        [StringLength(1000, ErrorMessage = "Description must be between 10 and 1000 characters", MinimumLength = 10)]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Start date is required")]
        [DataType(DataType.DateTime)]
        public DateTime StartDate { get; set; }

        [Required(ErrorMessage = "End date is required")]
        [DataType(DataType.DateTime)]
        public DateTime EndDate { get; set; }

        [Required(ErrorMessage = "Start time is required")]
        [DataType(DataType.Time)]
        public TimeSpan StartTime { get; set; }

        [Required(ErrorMessage = "Location is required")]
        [StringLength(200, ErrorMessage = "Location must be between 3 and 200 characters", MinimumLength = 3)]
        public string Location { get; set; } = string.Empty;

        [Required(ErrorMessage = "Maximum attendees is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Maximum attendees must be greater than 0")]
        public int MaxAttendees { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Current capacity must be 0 or greater")]
        public int CurrentCapacity { get; set; }

        [NotMapped]
        public int AvailableSpots => MaxAttendees - CurrentCapacity;

        [Required(ErrorMessage = "Category is required")]
        [StringLength(50, ErrorMessage = "Category must be between 3 and 50 characters", MinimumLength = 3)]
        public string Category { get; set; } = string.Empty;

        [Required(ErrorMessage = "Event type is required")]
        public EventType Type { get; set; } = EventType.Other;

        [Range(0, double.MaxValue, ErrorMessage = "Price must be 0 or greater")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        [StringLength(2000)]
        public string? ImageUrl { get; set; }

        public bool IsActive { get; set; } = true;

        public bool IsFeatured { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [DataType(DataType.DateTime)]
        public DateTime? UpdatedAt { get; set; }

        [Required(ErrorMessage = "Creator ID is required")]
        public string CreatorId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Organizer ID is required")]
        public string OrganizerId { get; set; } = string.Empty;

        // Navigation properties
        [JsonIgnore]
        public virtual ApplicationUser? Creator { get; set; }

        [JsonIgnore]
        public virtual ApplicationUser? Organizer { get; set; }

        [JsonIgnore]
        public virtual ICollection<Registration>? Registrations { get; set; }

        [JsonIgnore]
        public virtual ICollection<Comment>? Comments { get; set; }

        // Computed properties
        public bool IsFull => MaxAttendees > 0 && CurrentParticipants >= MaxAttendees;
        public int CurrentParticipants => Registrations?.Count(r => r.Status == RegistrationStatus.Confirmed) ?? 0;
        public string OrganizerName => Organizer?.UserName ?? "Unknown";
        public bool IsRegistrationOpen => MaxAttendees == 0 || CurrentParticipants < MaxAttendees;

        // Helper methods
        public DateTime GetStartDateTime() => StartDate.Add(StartTime);
        public bool HasStarted() => GetStartDateTime() <= DateTime.UtcNow;
        public bool IsFinished() => GetStartDateTime().AddHours(24) <= DateTime.UtcNow;
        public TimeSpan TimeUntilStart() => GetStartDateTime() - DateTime.UtcNow;
        public TimeSpan TimeSinceStart() => DateTime.UtcNow - GetStartDateTime();
        public TimeSpan TimeUntilEnd() => EndDate.Add(StartTime) - DateTime.UtcNow;
        public TimeSpan TimeSinceEnd() => DateTime.UtcNow - EndDate.Add(StartTime);
    }
}
