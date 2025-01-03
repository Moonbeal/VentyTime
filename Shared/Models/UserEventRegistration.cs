using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace VentyTime.Shared.Models
{
    public class UserEventRegistration
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        public int EventId { get; set; }

        public RegistrationStatus Status { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }

        public DateTime RegisteredAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        [JsonIgnore]
        public virtual Event? Event { get; set; }

        [JsonIgnore]
        public virtual ApplicationUser? User { get; set; }
    }
}
