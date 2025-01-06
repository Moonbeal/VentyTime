using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace VentyTime.Shared.Models
{
    public class EventTag
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int EventId { get; set; }

        [Required]
        [StringLength(50)]
        public string Tag { get; set; } = string.Empty;

        [JsonIgnore]
        public virtual Event? Event { get; set; }
    }
}
