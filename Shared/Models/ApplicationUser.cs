using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

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

        public string ProfilePictureUrl { get; set; } = string.Empty;

        public DateTime DateJoined { get; set; } = DateTime.Now;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public virtual ICollection<Registration> Registrations { get; set; } = new List<Registration>();
        public virtual ICollection<Event> OrganizedEvents { get; set; } = new List<Event>();
    }
}
