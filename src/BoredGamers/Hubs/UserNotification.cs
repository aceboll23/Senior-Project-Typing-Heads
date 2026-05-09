using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace BoredGamers.Hubs;

[Authorize]
public class UserNotificationHub : Hub
{
    // Each user joins a personal group based on their user ID so we can
    // broadcast to just them regardless of which page they're on
    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier;
        Console.WriteLine($"[SignalR] User connected. UserIdentifier: '{userId ?? "NULL"}', ConnectionId: {Context.ConnectionId}");
        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(userId));
            Console.WriteLine($"[SignalR] Added to group: {GroupName(userId)}");
        }
        await base.OnConnectedAsync();
    }

    public static string GroupName(string userId) => $"user-{userId}";
}