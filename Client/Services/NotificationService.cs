using System;
using System.Net.Http.Json;
using MudBlazor;
using VentyTime.Shared.Models;
using VentyTime.Client.Extensions;

namespace VentyTime.Client.Services
{
    public interface INotificationService
    {
        Task<List<NotificationMessage>> GetUserNotificationsAsync();
        Task<NotificationMessage> CreateNotificationAsync(NotificationMessage notification);
        Task<List<NotificationMessage>> CreateNotificationsForEventParticipantsAsync(int eventId, NotificationMessage notification);
        Task<bool> MarkAsReadAsync(int notificationId);
        Task<bool> DeleteNotificationAsync(int notificationId);
        Task ShowAsync(string message, NotificationType type = NotificationType.Info);
    }

    public class NotificationService : INotificationService
    {
        private readonly ISnackbar _snackbar;
        private readonly HttpClient _httpClient;

        public NotificationService(ISnackbar snackbar, IHttpClientFactory httpClientFactory)
        {
            _snackbar = snackbar;
            _httpClient = httpClientFactory.CreateClient("VentyTime.ServerAPI");
        }

        public async Task<List<NotificationMessage>> GetUserNotificationsAsync()
        {
            var response = await _httpClient.GetAsync("api/notifications");
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<List<NotificationMessage>>() ?? new List<NotificationMessage>();
            }
            throw await HttpRequestExceptionExtensions.CreateFromResponseAsync(response);
        }

        public async Task<NotificationMessage> CreateNotificationAsync(NotificationMessage notification)
        {
            var response = await _httpClient.PostAsJsonAsync("api/notifications", notification);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<NotificationMessage>() ?? throw new InvalidOperationException("Failed to create notification");
            }
            throw await HttpRequestExceptionExtensions.CreateFromResponseAsync(response);
        }

        public async Task<List<NotificationMessage>> CreateNotificationsForEventParticipantsAsync(int eventId, NotificationMessage notification)
        {
            var response = await _httpClient.PostAsJsonAsync($"api/notifications/events/{eventId}/participants", notification);
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<List<NotificationMessage>>() ?? new List<NotificationMessage>();
            }
            throw await HttpRequestExceptionExtensions.CreateFromResponseAsync(response);
        }

        public async Task<bool> MarkAsReadAsync(int notificationId)
        {
            var response = await _httpClient.PutAsync($"api/notifications/{notificationId}/read", null);
            if (response.IsSuccessStatusCode)
            {
                return true;
            }
            throw await HttpRequestExceptionExtensions.CreateFromResponseAsync(response);
        }

        public async Task<bool> DeleteNotificationAsync(int notificationId)
        {
            var response = await _httpClient.DeleteAsync($"api/notifications/{notificationId}");
            if (response.IsSuccessStatusCode)
            {
                return true;
            }
            throw await HttpRequestExceptionExtensions.CreateFromResponseAsync(response);
        }

        public async Task ShowAsync(string message, NotificationType type = NotificationType.Info)
        {
            var severity = type switch
            {
                NotificationType.Success => Severity.Success,
                NotificationType.Error => Severity.Error,
                NotificationType.Warning => Severity.Warning,
                _ => Severity.Info
            };

            _snackbar.Add(message, severity);
            await Task.CompletedTask;
        }
    }
}
