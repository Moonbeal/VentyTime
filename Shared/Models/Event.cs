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
        Concert,
        Exhibition,
        SportEvent,
        Networking,
        Charity,
        Other
    }

    public enum EventAccessibility
    {
        Public,
        Private,
        InviteOnly
    }

    public class Event
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Title is required")]
        [StringLength(100, ErrorMessage = "Title must be between 3 and 100 characters", MinimumLength = 3)]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required")]
        [StringLength(2000, ErrorMessage = "Description must be between 10 and 2000 characters", MinimumLength = 10)]
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

        [StringLength(200)]
        public string? VenueDetails { get; set; }

        [StringLength(200)]
        public string? OnlineUrl { get; set; }

        public bool IsOnline { get; set; }

        public string? OnlineMeetingUrl { get; set; }

        public string? OnlineMeetingId { get; set; }

        public string? OnlineMeetingPassword { get; set; }

        [Required(ErrorMessage = "Maximum attendees is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Maximum attendees must be greater than 0")]
        public int MaxAttendees { get; set; }

        [Required]
        public string Category { get; set; } = string.Empty;

        public EventType Type { get; set; } = EventType.Other;

        public EventAccessibility Accessibility { get; set; } = EventAccessibility.Public;

        [StringLength(2000)]
        public string? ImageUrl { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        public bool? IsActive { get; set; } = true;

        public bool IsCancelled { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        [Required]
        public string CreatorId { get; set; } = string.Empty;

        [Required]
        public string OrganizerId { get; set; } = string.Empty;

        // Navigation properties
        [JsonIgnore]
        public virtual ApplicationUser? Creator { get; set; }

        [JsonIgnore]
        public virtual ApplicationUser? Organizer { get; set; }

        [JsonIgnore]
        public virtual ICollection<UserEventRegistration> Registrations { get; set; } = new List<UserEventRegistration>();

        [JsonIgnore]
        public virtual ICollection<EventComment> EventComments { get; set; } = new List<EventComment>();

        [NotMapped]
        public int CurrentParticipants { get; private set; }

        // Additional properties for event management
        public bool IsFeatured { get; set; }
        public bool RequiresRegistration { get; set; } = true;
        public bool HasAgeRestriction { get; set; }
        public int? MinimumAge { get; set; }
        public bool HasEarlyBirdPrice { get; set; }
        public decimal? EarlyBirdPrice { get; set; }
        public DateTime? EarlyBirdDeadline { get; set; }
        public string? RefundPolicy { get; set; }
        public string? Requirements { get; set; }
        public string? Schedule { get; set; }
        public List<string>? Tags { get; set; }

        // Waitlist properties
        public bool AllowWaitlist { get; set; }
        public int? WaitlistCapacity { get; set; }

        // Computed properties
        [NotMapped]
        public int CurrentCapacity => CurrentParticipants;

        [NotMapped]
        public bool IsFull => CurrentParticipants >= MaxAttendees;

        [NotMapped]
        public bool HasStarted => DateTime.UtcNow >= StartDate;

        [NotMapped]
        public bool IsFinished => DateTime.UtcNow > EndDate;

        [NotMapped]
        public bool IsRegistrationOpen => !IsFull && !IsCancelled && !HasStarted && IsActive.HasValue && IsActive.Value;

        [NotMapped]
        public int AvailableSpots => MaxAttendees - CurrentParticipants;
    }
}
