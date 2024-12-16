using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace VentyTime.Server.Hubs
{
    public class NotificationHub : Hub
    {
        public async Task SendNotification(string userId, string message, string type)
        {
            await Clients.User(userId).SendAsync("ReceiveNotification", message, type);
        }

        public async Task SendEventUpdate(string eventId, string message)
        {
            await Clients.Group($"event_{eventId}").SendAsync("ReceiveEventUpdate", message);
        }

        public async Task JoinEventGroup(string eventId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"event_{eventId}");
        }

        public async Task LeaveEventGroup(string eventId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"event_{eventId}");
        }
    }
}
