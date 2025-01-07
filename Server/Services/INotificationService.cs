using VentyTime.Shared.Models;

namespace VentyTime.Server.Services;

public interface INotificationService
{
    Task<NotificationSettingsModel?> GetNotificationSettingsAsync(string userId);
    Task<bool> UpdateNotificationSettingsAsync(string userId, NotificationSettingsModel settings);
    Task<bool> ClearNotificationsAsync(string userId);
}
