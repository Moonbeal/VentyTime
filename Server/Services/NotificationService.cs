using Microsoft.EntityFrameworkCore;
using VentyTime.Server.Data;
using VentyTime.Shared.Models;

namespace VentyTime.Server.Services
{
    public interface INotificationService
    {
        Task<List<NotificationMessage>> GetNotificationsForUserAsync(string userId);
        Task<NotificationMessage> CreateNotificationAsync(NotificationMessage notification);
        Task<List<NotificationMessage>> CreateNotificationsForEventParticipantsAsync(int eventId, NotificationMessage notification);
        Task<NotificationMessage?> GetNotificationByIdAsync(int id);
        Task<bool> DeleteNotificationAsync(int id);
        Task<bool> DismissNotificationAsync(int id);
        Task<bool> DismissAllNotificationsAsync(string userId);
    }

    public class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext _context;

        public NotificationService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<NotificationMessage>> GetNotificationsForUserAsync(string userId)
        {
            return await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsDismissed)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();
        }

        public async Task<NotificationMessage> CreateNotificationAsync(NotificationMessage notification)
        {
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
            return notification;
        }

        public async Task<List<NotificationMessage>> CreateNotificationsForEventParticipantsAsync(int eventId, NotificationMessage notification)
        {
            var participants = await _context.EventRegistrations
                .Where(r => r.EventId == eventId)
                .Select(r => r.UserId)
                .ToListAsync();

            var notifications = new List<NotificationMessage>();
            foreach (var userId in participants)
            {
                var newNotification = new NotificationMessage
                {
                    EventId = eventId,
                    UserId = userId,
                    Title = notification.Title,
                    Message = notification.Message,
                    Type = notification.Type,
                    CreatedAt = DateTime.UtcNow
                };
                notifications.Add(newNotification);
            }

            _context.Notifications.AddRange(notifications);
            await _context.SaveChangesAsync();
            return notifications;
        }

        public async Task<NotificationMessage?> GetNotificationByIdAsync(int id)
        {
            return await _context.Notifications.FindAsync(id);
        }

        public async Task<bool> DeleteNotificationAsync(int id)
        {
            var notification = await _context.Notifications.FindAsync(id);
            if (notification is null) return false;

            _context.Notifications.Remove(notification);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DismissNotificationAsync(int id)
        {
            var notification = await _context.Notifications.FindAsync(id);
            if (notification is null) return false;

            notification.IsDismissed = true;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DismissAllNotificationsAsync(string userId)
        {
            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsDismissed)
                .ToListAsync();

            foreach (var notification in notifications)
            {
                notification.IsDismissed = true;
            }

            await _context.SaveChangesAsync();
            return true;
        }
    }
}
