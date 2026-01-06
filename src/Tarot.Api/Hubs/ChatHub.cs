using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Tarot.Api.Hubs;

[Authorize]
public class ChatHub : Hub
{
    public async Task JoinGroup(string appointmentId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, appointmentId);
    }

    public async Task SendMessage(string appointmentId, string message)
    {
        var username = Context.User?.Identity?.Name ?? "Unknown";
        // Broadcast to group
        await Clients.Group(appointmentId).SendAsync("ReceiveMessage", username, message, DateTimeOffset.UtcNow);
    }
}
