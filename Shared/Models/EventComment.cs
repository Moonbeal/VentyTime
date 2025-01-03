using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace VentyTime.Shared.Models
{
    public class EventComment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int EventId { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [StringLength(1000, ErrorMessage = "Comment must be between 1 and 1000 characters", MinimumLength = 1)]
        public string Content { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public bool IsDeleted { get; set; }

        // Navigation properties
        [JsonIgnore]
        public virtual Event? Event { get; set; }

        [JsonIgnore]
        public virtual ApplicationUser? User { get; set; }
    }
}
