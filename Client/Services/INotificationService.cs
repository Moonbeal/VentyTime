using VentyTime.Shared.Models;

namespace VentyTime.Client.Services
{
    public interface INotificationService
    {
        Task<List<NotificationMessage>> GetUserNotificationsAsync();
        Task<bool> MarkAsReadAsync(int id);
        Task<bool> DeleteNotificationAsync(int id);
        Task ShowAsync(string message, NotificationType type = NotificationType.Info);
    }
}
