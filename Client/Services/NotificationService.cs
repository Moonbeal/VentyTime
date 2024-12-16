using System;
using MudBlazor;
using VentyTime.Shared.Models;

namespace VentyTime.Client.Services
{
    public class NotificationService
    {
        private readonly ISnackbar _snackbar;
        private event Action<NotificationMessage>? NotificationReceived;

        public NotificationService(ISnackbar snackbar)
        {
            _snackbar = snackbar;
        }

        public IDisposable OnNotification(Action<NotificationMessage> action)
        {
            NotificationReceived += action;
            return new NotificationSubscription(() => NotificationReceived -= action);
        }

        public void Show(string message, NotificationType type = NotificationType.Info)
        {
            var notification = new NotificationMessage
            {
                Message = message,
                Type = type,
                Timestamp = DateTime.UtcNow
            };

            NotificationReceived?.Invoke(notification);

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

        private class NotificationSubscription : IDisposable
        {
            private readonly Action _unsubscribe;

            public NotificationSubscription(Action unsubscribe)
            {
                _unsubscribe = unsubscribe;
            }

            public void Dispose()
            {
                _unsubscribe();
            }
        }
    }
}
