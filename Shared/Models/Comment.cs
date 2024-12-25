using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace VentyTime.Shared.Models
{
    public class Comment
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Event ID is required")]
        public int EventId { get; set; }

        [Required(ErrorMessage = "User ID is required")]
        public string UserId { get; set; } = string.Empty;

        [Required(ErrorMessage = "Comment content is required")]
        [StringLength(1000, MinimumLength = 1, ErrorMessage = "Comment must be between 1 and 1000 characters")]
        public string Content { get; set; } = string.Empty;

        public bool IsEdited { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        [JsonIgnore]
        public virtual Event? Event { get; set; }

        [JsonIgnore]
        public virtual ApplicationUser? User { get; set; }
    }
}
