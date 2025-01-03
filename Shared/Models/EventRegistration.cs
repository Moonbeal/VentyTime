using System;

namespace VentyTime.Shared.Models
{
    public class EventRegistration
    {
        public int Id { get; set; }
        public int EventId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public DateTime RegistrationDate { get; set; }
        public bool IsConfirmed { get; set; }
        public bool IsCancelled { get; set; }
        public bool IsWaitlisted { get; set; }
        public int? WaitlistPosition { get; set; }
        public string? CancellationReason { get; set; }
        public DateTime? CancellationDate { get; set; }

        // Navigation properties
        public EventDto? Event { get; set; }
        public User? User { get; set; }
    }
}
