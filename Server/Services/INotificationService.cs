using VentyTime.Shared.Models;

namespace VentyTime.Server.Services
{
    public interface INotificationService
    {
        Task CreateNotificationAsync(
            string userId,
            string title,
            string message,
            NotificationType type,
            string? relatedEntityType = null,
            int? relatedEntityId = null);

        Task<List<NotificationMessage>> GetUserNotificationsAsync(string userId);
        Task<bool> MarkAsReadAsync(int notificationId, string userId);
        Task<bool> DeleteNotificationAsync(int notificationId, string userId);
    }
}
