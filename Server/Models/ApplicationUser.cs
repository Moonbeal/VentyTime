using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using VentyTime.Shared.Models;

namespace VentyTime.Server.Models
{
    // This file is deprecated. Please use Shared.Models.ApplicationUser instead.
    // This file will be removed in a future update.
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public UserRole Role { get; set; } = UserRole.User;
        public DateTime? LastLoginAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public bool IsActive { get; set; } = true;
        public string? ProfilePictureUrl { get; set; }

        // Profile fields
        public string AvatarUrl { get; set; } = "/images/default-profile.png";
        public string Bio { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string Website { get; set; } = string.Empty;

        public string FullName => $"{FirstName} {LastName}".Trim();

        // Navigation properties
        public virtual ICollection<Event> OrganizedEvents { get; set; } = new List<Event>();
        public virtual ICollection<Registration> Registrations { get; set; } = new List<Registration>();
    }
}
