using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace BoredGamers.Hubs;

[Authorize]
public class PlaygroupChatHub : Hub
{
    public async Task JoinGroup(int playgroupId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(playgroupId));
    }

    public async Task LeaveGroup(int playgroupId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(playgroupId));
    }

    public static string GroupName(int playgroupId) => $"playgroup-{playgroupId}";
}
