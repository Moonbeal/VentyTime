using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using System.Text.Json.Serialization;

namespace VentyTime.Shared.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        [MaxLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string LastName { get; set; } = string.Empty;

        public string FullName => $"{FirstName} {LastName}";

        [MaxLength(2000)]
        public string AvatarUrl { get; set; } = "/images/default-profile.png";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }

        // Notification settings
        public bool EmailNotifications { get; set; } = true;
        public bool PushNotifications { get; set; } = true;
        public bool EventReminders { get; set; } = true;
        public bool NewFollowerNotifications { get; set; } = true;
        public bool NewLikeNotifications { get; set; } = true;
        public bool NewCommentNotifications { get; set; } = true;

        [JsonIgnore]
        public virtual ICollection<Event> OrganizedEvents { get; set; } = new List<Event>();

        [JsonIgnore]
        public virtual ICollection<Registration> Registrations { get; set; } = new List<Registration>();

        [JsonIgnore]
        public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();

        public bool IsActive { get; set; } = true;
    }
}
