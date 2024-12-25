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
        Networking,
        Hackathon,
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

        public EventType Type { get; set; } = EventType.Other;

        [Required(ErrorMessage = "Category is required")]
        public string Category { get; set; } = EventCategories.Technology;

        [Required(ErrorMessage = "Location is required")]
        public string Location { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required")]
        [StringLength(500, ErrorMessage = "Description must be between 10 and 500 characters", MinimumLength = 10)]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Start date is required")]
        public DateTime StartDate { get; set; }

        [Required(ErrorMessage = "End date is required")]
        public DateTime EndDate { get; set; }

        [Required(ErrorMessage = "Start time is required")]
        public TimeSpan StartTime { get; set; }

        public bool IsFeatured { get; set; }

        public string CreatorId { get; set; } = string.Empty;

        public DateTime GetStartDateTime()
        {
            return StartDate.Add(StartTime);
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

        public TimeSpan TimeUntilStart()
        {
            return GetStartDateTime() - DateTime.UtcNow;
        }

        public TimeSpan TimeSinceStart()
        {
            return DateTime.UtcNow - GetStartDateTime();
        }

        public TimeSpan TimeUntilEnd()
        {
            return EndDate.Add(StartTime) - DateTime.UtcNow;
        }

        public TimeSpan TimeSinceEnd()
        {
            return DateTime.UtcNow - EndDate.Add(StartTime);
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
        public string OrganizerId { get; set; } = string.Empty;

        [JsonIgnore]
        public virtual ICollection<Registration>? Registrations { get; set; }

        [JsonIgnore]
        public virtual ICollection<Comment>? Comments { get; set; }

        [JsonIgnore]
        public virtual ApplicationUser? Organizer { get; set; }

        public bool IsFull => MaxAttendees > 0 && Registrations?.Count >= MaxAttendees;

        public int CurrentParticipants => Registrations?.Count ?? 0;

        public string OrganizerName => Organizer?.UserName ?? "Unknown";

        public bool IsRegistrationOpen => MaxAttendees == 0 || Registrations?.Count < MaxAttendees;

        // For backward compatibility
        public string Name => Title;
    }
}
