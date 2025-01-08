using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor;
using VentyTime.Client.Services;
using VentyTime.Shared.Models;
using Blazored.LocalStorage;
using Microsoft.Extensions.Logging;

namespace VentyTime.Client.Services
{
    public class NotificationService : INotificationService, IDisposable
    {
        private readonly ISnackbar _snackbar;
        private readonly IEventService _eventService;
        private readonly AuthenticationStateProvider _authStateProvider;
        private readonly ILocalStorageService _localStorage;
        private readonly List<Notification> _notifications = new();
        private readonly CancellationTokenSource _cancellationTokenSource = new();
        private readonly ILogger<NotificationService> _logger;
        private readonly DateTimeOffset _currentTime = DateTimeOffset.Parse("2025-01-07T15:06:11+02:00");
        public event Action? OnNotificationsChanged;

        public NotificationService(
            ISnackbar snackbar,
            IEventService eventService,
            AuthenticationStateProvider authStateProvider,
            ILocalStorageService localStorage,
            ILogger<NotificationService> logger)
        {
            _snackbar = snackbar;
            _eventService = eventService;
            _authStateProvider = authStateProvider;
            _localStorage = localStorage;
            _logger = logger;

            // Start initialization immediately
            InitializeAsync().ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    _logger.LogError(t.Exception, "Error during service initialization");
                }
                else
                {
                    StartPeriodicCheck();
                }
            });
        }

        private async Task InitializeAsync()
        {
            try
            {
                _logger.LogInformation("Initializing NotificationService");
                var authState = await _authStateProvider.GetAuthenticationStateAsync();
                var userId = authState.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogInformation("User not authenticated, skipping notification load");
                    return;
                }

                var key = $"notifications_{userId}";
                var storedNotifications = await _localStorage.GetItemAsync<List<Notification>>(key);
                if (storedNotifications != null)
                {
                    // Clean up old notifications (older than 7 days)
                    var cutoffDate = _currentTime.DateTime.AddDays(-7);
                    storedNotifications = storedNotifications.Where(n => n.CreatedAt >= cutoffDate).ToList();
                    
                    _notifications.Clear();
                    _notifications.AddRange(storedNotifications);
                    _logger.LogInformation("Loaded {Count} notifications from storage for user {UserId}", _notifications.Count, userId);
                    
                    // Save back the cleaned notifications
                    await _localStorage.SetItemAsync(key, _notifications);
                }
                else
                {
                    _logger.LogInformation("No stored notifications found for user {UserId}", userId);
                }
                OnNotificationsChanged?.Invoke();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing service");
                _snackbar.Add("Error loading notifications", Severity.Error);
            }
        }

        private void StartPeriodicCheck()
        {
            _logger.LogInformation("Starting periodic notification check");
            Task.Run(async () =>
            {
                try
                {
                    // Run initial check immediately
                    await CheckForEventNotifications();

                    while (!_cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        await Task.Delay(TimeSpan.FromMinutes(1), _cancellationTokenSource.Token);
                        await CheckForEventNotifications();
                    }
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Periodic notification check cancelled");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in periodic notification check");
                }
            }, _cancellationTokenSource.Token);
        }

        public async Task<List<Notification>> GetNotificationsAsync()
        {
            try
            {
                _logger.LogInformation("Getting notifications");
                
                var authState = await _authStateProvider.GetAuthenticationStateAsync();
                var userId = authState.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogInformation("User not authenticated, returning empty list");
                    return new List<Notification>();
                }

                var key = $"notifications_{userId}";
                var storedNotifications = await _localStorage.GetItemAsync<List<Notification>>(key);
                
                if (storedNotifications != null)
                {
                    _notifications.Clear();
                    _notifications.AddRange(storedNotifications);
                }

                _logger.LogInformation("Current notification count for user {UserId}: {Count}", userId, _notifications.Count);
                foreach (var notification in _notifications)
                {
                    _logger.LogInformation("Notification: {Id} - {Title} - {Message} - Created: {CreatedAt}", 
                        notification.Id, notification.Title, notification.Message, notification.CreatedAt);
                }
                
                return _notifications.OrderByDescending(n => n.CreatedAt).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting notifications");
                return new List<Notification>();
            }
        }

        public async Task<int> GetUnreadCountAsync()
        {
            try
            {
                var authState = await _authStateProvider.GetAuthenticationStateAsync();
                var userId = authState.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogInformation("User not authenticated, returning 0 unread count");
                    return 0;
                }

                var key = $"notifications_{userId}";
                var storedNotifications = await _localStorage.GetItemAsync<List<Notification>>(key);
                
                if (storedNotifications != null)
                {
                    var count = storedNotifications.Count(n => !n.IsRead);
                    _logger.LogInformation("Unread count for user {UserId}: {Count}", userId, count);
                    return count;
                }
                
                return 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting unread count");
                return 0;
            }
        }

        public async Task MarkAsReadAsync(int notificationId)
        {
            try
            {
                _logger.LogInformation("Marking notification {Id} as read", notificationId);
                var notification = _notifications.FirstOrDefault(n => n.Id == notificationId);
                if (notification != null)
                {
                    notification.IsRead = true;
                    await SaveNotificationsAsync();
                    OnNotificationsChanged?.Invoke();
                    _logger.LogInformation("Notification {Id} marked as read", notificationId);
                }
                else
                {
                    _logger.LogWarning("Notification {Id} not found", notificationId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking notification as read");
            }
        }

        public async Task MarkAllAsReadAsync()
        {
            try
            {
                _logger.LogInformation("Marking all notifications as read");
                foreach (var notification in _notifications)
                {
                    notification.IsRead = true;
                }
                await SaveNotificationsAsync();
                OnNotificationsChanged?.Invoke();
                _logger.LogInformation("All notifications marked as read");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking all notifications as read");
            }
        }

        private async Task SaveNotificationsAsync()
        {
            try
            {
                var authState = await _authStateProvider.GetAuthenticationStateAsync();
                var userId = authState.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("Cannot save notifications: user not authenticated");
                    return;
                }

                var key = $"notifications_{userId}";
                _logger.LogInformation("Saving {Count} notifications to storage for user {UserId}", _notifications.Count, userId);
                
                await _localStorage.SetItemAsync(key, _notifications);
                _logger.LogInformation("Notifications saved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving notifications");
                _snackbar.Add("Error saving notifications", Severity.Error);
            }
        }

        private async Task SaveNotificationsAsync(Notification notification)
        {
            try
            {
                var authState = await _authStateProvider.GetAuthenticationStateAsync();
                var userId = authState.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("Cannot save notifications: user not authenticated");
                    return;
                }

                var key = $"notifications_{userId}";
                _logger.LogInformation("Saving notification to storage for user {UserId}", userId);
                
                var storedNotifications = (await _localStorage.GetItemAsync<List<Notification>>(key)) ?? new List<Notification>();
                storedNotifications.Add(notification);
                await _localStorage.SetItemAsync(key, storedNotifications);
                _logger.LogInformation("Notification saved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving notifications");
                _snackbar.Add("Error saving notifications", Severity.Error);
            }
        }

        public async Task CheckForEventNotifications()
        {
            try
            {
                _logger.LogInformation("Starting CheckForEventNotifications");
                _logger.LogInformation("Current time (UTC): {CurrentTimeUtc}", _currentTime.UtcDateTime.ToString("yyyy-MM-dd HH:mm:ss"));
                _logger.LogInformation("Current time (Local): {CurrentTimeLocal}", _currentTime.LocalDateTime.ToString("yyyy-MM-dd HH:mm:ss zzz"));
                
                var authState = await _authStateProvider.GetAuthenticationStateAsync();
                var user = authState.User;
                
                if (!user.Identity?.IsAuthenticated ?? true)
                {
                    _logger.LogInformation("User not authenticated");
                    return;
                }

                var userId = user.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogInformation("User ID not found");
                    _logger.LogInformation("Available claims: {Claims}", 
                        string.Join(", ", user.Claims.Select(c => $"{c.Type}: {c.Value}")));
                    return;
                }

                _logger.LogInformation("Checking notifications for user: {UserId}", userId);
                var userEvents = await _eventService.GetRegisteredEventsAsync();
                _logger.LogInformation("Found {Count} registered events", userEvents.Count);

                // Get tomorrow's date in local time
                var currentTimeLocal = _currentTime.LocalDateTime;
                var tomorrowDate = currentTimeLocal.Date.AddDays(1);
                _logger.LogInformation("Tomorrow date: {TomorrowDate}", tomorrowDate.ToString("yyyy-MM-dd"));

                foreach (var evt in userEvents)
                {
                    _logger.LogInformation("Checking event: {EventTitle} (ID: {EventId})", evt.Title, evt.Id);
                    _logger.LogInformation("Event start date (UTC): {EventStartDate}", evt.StartDate.ToString("yyyy-MM-dd HH:mm:ss"));
                    _logger.LogInformation("Event start time: {EventStartTime}", evt.StartTime.ToString());
                    
                    // Combine date and time to get the actual event start time
                    var eventStartUtc = evt.StartDate.Date.Add(evt.StartTime);
                    _logger.LogInformation("Combined event start (UTC): {EventStartUtc}", eventStartUtc.ToString("yyyy-MM-dd HH:mm:ss"));
                    
                    // Convert event time to local time for comparison
                    var eventStartLocal = DateTime.SpecifyKind(eventStartUtc, DateTimeKind.Utc).ToLocalTime();
                    _logger.LogInformation("Event start (Local): {EventStartLocal}", eventStartLocal.ToString("yyyy-MM-dd HH:mm:ss zzz"));
                    
                    // Check if the event is tomorrow
                    var eventDate = eventStartLocal.Date;
                    _logger.LogInformation("Event date: {EventDate}", eventDate.ToString("yyyy-MM-dd"));
                    _logger.LogInformation("Current date: {CurrentDate}", currentTimeLocal.Date.ToString("yyyy-MM-dd"));
                    _logger.LogInformation("Tomorrow date: {TomorrowDate}", tomorrowDate.ToString("yyyy-MM-dd"));
                    _logger.LogInformation("Is event tomorrow: {IsEventTomorrow}", eventDate == tomorrowDate);
                    
                    if (eventDate == tomorrowDate)
                    {
                        _logger.LogInformation("Found event tomorrow: {EventTitle}", evt.Title);
                        
                        // Check if we haven't already notified about this event today
                        var existingNotification = _notifications.Any(n => 
                            n.EventId == evt.Id && 
                            n.CreatedAt.Date == currentTimeLocal.Date);
                        
                        _logger.LogInformation("Existing notifications count: {Count}", _notifications.Count);
                        _logger.LogInformation("Has existing notification for today: {HasExistingNotification}", existingNotification);
                        
                        if (existingNotification)
                        {
                            var notification = _notifications.First(n => n.EventId == evt.Id && n.CreatedAt.Date == currentTimeLocal.Date);
                            _logger.LogInformation("Found existing notification: ID {Id}, Created {CreatedAt}", 
                                notification.Id, notification.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss zzz"));
                        }

                        if (!existingNotification)
                        {
                            _logger.LogInformation("Creating new notification for: {EventTitle}", evt.Title);
                            var notification = new Notification
                            {
                                Id = _notifications.Count + 1,
                                UserId = userId,
                                Title = "Event Tomorrow",
                                Message = $"The event '{evt.Title}' starts tomorrow at {eventStartLocal.ToShortTimeString()}",
                                EventId = evt.Id,
                                Link = $"/event/{evt.Id}",
                                CreatedAt = currentTimeLocal,
                                IsRead = false
                            };

                            _logger.LogInformation("New notification: {Title} - {Message} - Created: {CreatedAt}", 
                                notification.Title, notification.Message, notification.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss zzz"));
                            
                            _notifications.Add(notification);
                            _logger.LogInformation("Notification added to list. Total count: {Count}", _notifications.Count);
                            
                            await SaveNotificationsAsync();
                            _logger.LogInformation("Notification saved to storage");
                            
                            OnNotificationsChanged?.Invoke();
                            _logger.LogInformation("NotificationsChanged event invoked");
                            
                            _snackbar.Add(notification.Message, Severity.Info);
                            _logger.LogInformation("Snackbar notification shown");
                        }
                    }
                }
                _logger.LogInformation("Finished CheckForEventNotifications");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking for event notifications");
                _snackbar.Add("Error checking for notifications", Severity.Error);
            }
        }

        public async Task SendEventNotificationAsync(Notification notification)
        {
            try
            {
                var registrations = await _eventService.GetEventRegistrationsAsync(notification.EventId ?? 0);
                foreach (var registration in registrations)
                {
                    var userNotification = new Notification
                    {
                        UserId = registration.UserId,
                        Title = notification.Title,
                        Message = notification.Message,
                        EventId = notification.EventId,
                        Link = notification.Link,
                        CreatedAt = DateTime.Now,
                        IsRead = false
                    };
                    await SaveNotificationsAsync(userNotification);
                }
                OnNotificationsChanged?.Invoke();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending event notifications");
                throw;
            }
        }

        public async Task ClearNotificationsAsync()
        {
            try
            {
                _notifications.Clear();
                OnNotificationsChanged?.Invoke();
                
                var authState = await _authStateProvider.GetAuthenticationStateAsync();
                var userId = authState.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                
                if (!string.IsNullOrEmpty(userId))
                {
                    var key = $"notifications_{userId}";
                    await _localStorage.RemoveItemAsync(key);
                }
                
                _logger.LogInformation("Notifications cleared successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing notifications");
                _snackbar.Add("Error clearing notifications", Severity.Error);
            }
        }

        public void Show(string message, NotificationType type = NotificationType.Info)
        {
            switch (type)
            {
                case NotificationType.Success:
                    _snackbar.Add(message, Severity.Success);
                    break;
                case NotificationType.Error:
                    _snackbar.Add(message, Severity.Error);
                    break;
                case NotificationType.Warning:
                    _snackbar.Add(message, Severity.Warning);
                    break;
                case NotificationType.Info:
                    _snackbar.Add(message, Severity.Info);
                    break;
            }
        }

        public void Dispose()
        {
            // Dispose of unmanaged resources.
            Dispose(true);
            // Suppress finalization.
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource.Dispose();
            }
        }
    }
}
