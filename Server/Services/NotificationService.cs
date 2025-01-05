using Microsoft.EntityFrameworkCore;
using VentyTime.Server.Data;
using VentyTime.Shared.Models;

namespace VentyTime.Server.Services;

public class NotificationService : INotificationService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(ApplicationDbContext context, ILogger<NotificationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<NotificationSettingsModel?> GetNotificationSettingsAsync(string userId)
    {
        try
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == userId);

            if (user == null)
            {
                _logger.LogWarning("User not found: {UserId}", userId);
                return null;
            }

            return new NotificationSettingsModel
            {
                EmailNotifications = user.EmailNotifications,
                PushNotifications = user.PushNotifications,
                EventReminders = user.EventReminders,
                NewFollowerNotifications = user.NewFollowerNotifications,
                NewLikeNotifications = user.NewLikeNotifications,
                NewCommentNotifications = user.NewCommentNotifications
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting notification settings for user {UserId}", userId);
            return null;
        }
    }

    public async Task<bool> UpdateNotificationSettingsAsync(string userId, NotificationSettingsModel settings)
    {
        try
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == userId);

            if (user == null)
            {
                _logger.LogWarning("User not found: {UserId}", userId);
                return false;
            }

            user.EmailNotifications = settings.EmailNotifications;
            user.PushNotifications = settings.PushNotifications;
            user.EventReminders = settings.EventReminders;
            user.NewFollowerNotifications = settings.NewFollowerNotifications;
            user.NewLikeNotifications = settings.NewLikeNotifications;
            user.NewCommentNotifications = settings.NewCommentNotifications;

            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating notification settings for user {UserId}", userId);
            return false;
        }
    }
}
