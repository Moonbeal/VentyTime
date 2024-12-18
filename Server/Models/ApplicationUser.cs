using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;

namespace VentyTime.Server.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Role { get; set; } = "User";
        public DateTime LastLoginAt { get; set; } = DateTime.UtcNow;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public bool IsActive { get; set; } = true;
        public string? ProfilePictureUrl { get; set; }
        public DateTime DateJoined { get; set; } = DateTime.UtcNow;

        public string FullName => $"{FirstName} {LastName}".Trim();

        // Navigation properties
        public virtual ICollection<Event> OrganizedEvents { get; set; } = new List<Event>();
        public virtual ICollection<Registration> Registrations { get; set; } = new List<Registration>();
    }
}
