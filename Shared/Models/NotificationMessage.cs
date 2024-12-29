using System;

namespace VentyTime.Shared.Models
{
    public class NotificationMessage
    {
        public int Id { get; set; }
        public int? EventId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public NotificationType Type { get; set; }
        public bool IsDismissed { get; set; }
        public bool IsRead { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public virtual Event? Event { get; set; }
    }

    public enum NotificationType
    {
        Info,
        Success,
        Warning,
        Error,
        EventUpdate,
        CustomMessage
    }
}
