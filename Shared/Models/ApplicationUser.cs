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

        [MaxLength(500)]
        public string Bio { get; set; } = string.Empty;

        [MaxLength(100)]
        public string Location { get; set; } = string.Empty;

        [MaxLength(200)]
        public string Website { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }

        [JsonIgnore]
        public virtual ICollection<Event> OrganizedEvents { get; set; } = new List<Event>();

        [JsonIgnore]
        public virtual ICollection<Event> CreatedEvents { get; set; } = new List<Event>();

        [JsonIgnore]
        public virtual ICollection<UserEventRegistration> Registrations { get; set; } = new List<UserEventRegistration>();

        [JsonIgnore]
        public virtual ICollection<EventComment> EventComments { get; set; } = new List<EventComment>();

        public bool IsActive { get; set; } = true;
    }
}
