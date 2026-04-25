using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace BoredGamers.Hubs;

[Authorize]
public class DirectMessageHub : Hub
{
    public async Task JoinConversation(string conversationKey)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, conversationKey);
    }

    public async Task LeaveConversation(string conversationKey)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, conversationKey);
    }

    // Conversation key is always sorted so both users end up in the same group
    // regardless of who initiates
    public static string ConversationKey(int profileIdA, int profileIdB)
    {
        var sorted = new[] { profileIdA, profileIdB }.OrderBy(x => x).ToArray();
        return $"dm-{sorted[0]}-{sorted[1]}";
    }
}