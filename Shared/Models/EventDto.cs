using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace VentyTime.Shared.Models
{
    public class EventDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public string Location { get; set; } = string.Empty;
        public string? VenueDetails { get; set; }
        public string? OnlineUrl { get; set; }
        public bool IsOnline { get; set; }
        public int MaxAttendees { get; set; }
        public string Category { get; set; } = string.Empty;
        public EventType Type { get; set; }
        public EventAccessibility Accessibility { get; set; }
        public string? ImageUrl { get; set; }
        public decimal Price { get; set; }
        public bool? IsActive { get; set; }
        public bool IsCancelled { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string CreatorId { get; set; } = string.Empty;
        public string OrganizerId { get; set; } = string.Empty;
        public ApplicationUser? Creator { get; set; }
        public bool IsImageLoaded { get; set; }
        public ApplicationUser? Organizer { get; set; }
        public int CurrentParticipants { get; set; }
        public bool IsFeatured { get; set; }
        public bool RequiresRegistration { get; set; }
        public bool HasAgeRestriction { get; set; }
        public int? MinimumAge { get; set; }
        public bool HasEarlyBirdPrice { get; set; }
        public decimal? EarlyBirdPrice { get; set; }
        public DateTime? EarlyBirdDeadline { get; set; }
        public string? RefundPolicy { get; set; }
        public string? Requirements { get; set; }
        public string? Schedule { get; set; }
        public List<string>? Tags { get; set; }
        public bool AllowWaitlist { get; set; }
        public int? WaitlistCapacity { get; set; }

        public bool HasStarted => DateTime.Now >= StartDate;
        public bool HasEnded => DateTime.Now > EndDate;
        public bool IsUpcoming => !HasStarted && !IsCancelled;
        public bool IsOngoing => HasStarted && !HasEnded && !IsCancelled;
        public bool IsPast => HasEnded || IsCancelled;
        public bool IsAvailableForRegistration => !HasStarted && !IsCancelled && (CurrentParticipants < MaxAttendees);
        public bool IsFullyBooked => CurrentParticipants >= MaxAttendees;
        public bool IsRegistrationOpen => IsAvailableForRegistration && !IsFullyBooked;
        public bool IsFull => CurrentParticipants >= MaxAttendees;
        public bool IsFinished => HasEnded || IsCancelled;
    }
}
