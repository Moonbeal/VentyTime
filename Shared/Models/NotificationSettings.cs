namespace VentyTime.Shared.Models
{
    public class NotificationSettings
    {
        public bool EmailNotifications { get; set; }
        public bool PushNotifications { get; set; }
        public bool EventReminders { get; set; }
        public bool NewFollowerNotifications { get; set; }
        public bool NewLikeNotifications { get; set; }
        public bool NewCommentNotifications { get; set; }
    }
}
