using System;
using System.ComponentModel.DataAnnotations;

namespace VentyTime.Shared.Models
{
    public class EventRegistration
    {
        public int Id { get; set; }
        
        public int EventId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public DateTime RegistrationDate { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public RegistrationStatus Status { get; set; } = RegistrationStatus.Pending;

        public virtual Event? Event { get; set; }
        public virtual ApplicationUser? User { get; set; }

        public bool IsConfirmed => Status == RegistrationStatus.Confirmed;
        public bool IsCancelled => Status == RegistrationStatus.Cancelled;
        public bool IsPending => Status == RegistrationStatus.Pending;
    }
}
