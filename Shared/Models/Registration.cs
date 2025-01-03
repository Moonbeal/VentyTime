using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace VentyTime.Shared.Models
{
    public class Registration
    {
        public int Id { get; set; }

        [Required]
        public int EventId { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        public RegistrationStatus Status { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public virtual Event? Event { get; set; }

        public virtual ApplicationUser? User { get; set; }
    }

    public enum RegistrationStatus
    {
        None = 0,
        Pending = 1,
        Confirmed = 2,
        Cancelled = 3,
        Rejected = 4
    }
}
