using System;

namespace VentyTime.Shared.Models
{
    public class Notification
    {
        public int Id { get; set; }
        public string? UserId { get; set; }
        public string? Title { get; set; }
        public string? Message { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsRead { get; set; }
        public int? EventId { get; set; }
        public string? Link { get; set; }

        public Notification()
        {
            CreatedAt = DateTime.Now;
            IsRead = false;
        }
    }
}
