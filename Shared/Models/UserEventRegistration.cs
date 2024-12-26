using System;

namespace VentyTime.Shared.Models
{
    public class UserEventRegistration
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public int EventId { get; set; }
        public DateTime RegisteredAt { get; set; }
        public ApplicationUser? User { get; set; }
        public Event? Event { get; set; }
        public RegistrationStatus Status { get; set; }
    }
}
