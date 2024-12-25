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

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        [JsonIgnore]
        public virtual Event? Event { get; set; }

        [JsonIgnore]
        public virtual ApplicationUser? User { get; set; }

        // This is used to track when the registration was made
        public DateTime RegistrationDate { get; set; }
    }

    public enum RegistrationStatus
    {
        Pending,
        Confirmed,
        Cancelled
    }
}
