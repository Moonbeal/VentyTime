using System;

namespace VentyTime.Shared.Models
{
    public class NotificationMessage
    {
        public int Id { get; set; } = 0;
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public NotificationType Type { get; set; } = NotificationType.Info;
        public bool IsRead { get; set; } = false;
        public bool IsDismissed { get; set; } = false;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string UserId { get; set; } = string.Empty;
    }

    public enum NotificationType
    {
        Info,
        Success,
        Warning,
        Error
    }
}
