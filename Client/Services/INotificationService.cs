using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VentyTime.Shared.Models;

namespace VentyTime.Client.Services
{
    public interface INotificationService : IDisposable
    {
        event Action OnNotificationsChanged;
        Task<List<Notification>> GetNotificationsAsync();
        Task<int> GetUnreadCountAsync();
        Task MarkAsReadAsync(int notificationId);
        Task MarkAllAsReadAsync();
        Task CheckForEventNotifications();
        Task SendEventNotificationAsync(Notification notification);
    }
}
