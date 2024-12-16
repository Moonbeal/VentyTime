using System;
using System.ComponentModel.DataAnnotations;

namespace VentyTime.Shared.Models
{
    public class Registration
    {
        public int Id { get; set; }

        [Required]
        public int EventId { get; set; }
        public virtual Event? Event { get; set; }

        [Required]
        public string UserId { get; set; } = "";
        public virtual ApplicationUser? User { get; set; }

        public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
        public DateTime? CancelledAt { get; set; }
        public bool IsCancelled => CancelledAt.HasValue;
        
        public DateTime RegistrationDate { get; set; } = DateTime.UtcNow;

        public RegistrationStatus Status { get; set; } = RegistrationStatus.Pending;
    }

    public enum RegistrationStatus
    {
        Pending,
        Confirmed,
        Cancelled
    }
}
