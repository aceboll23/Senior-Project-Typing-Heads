using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using BoredGamers.Data;
using BoredGamers.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using BoredGamers.Hubs;
 namespace BoredGamers.Controllers;

[Authorize]
public class PlaygroupController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<User> _userManager;
    private readonly IHubContext<PlaygroupChatHub> _chatHub;

    public PlaygroupController(ApplicationDbContext db, UserManager<User> userManager, IHubContext<PlaygroupChatHub> chatHub)
    {
        _db = db;
        _userManager = userManager;
        _chatHub = chatHub;
    }
    private string GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    private async Task<UserProfile?> GetUserProfileAsync(string userId) =>
        await _db.Set<UserProfile>().FirstOrDefaultAsync(p => p.UserId == userId);

    // GET /Playgroup — "My Playgroups" list
    public async Task<IActionResult> Index()
    {
        var userId = GetUserId();

        var myGroups = await _db.PlaygroupMembers
            .Where(m => m.UserId == userId)
            .Include(m => m.Playgroup)
            .Select(m => m.Playgroup)
            .OrderByDescending(g => g.CreatedAt)
            .ToListAsync();
        
        // Count pending invites for the button
        var pendingInviteCount = await _db.PlaygroupInvites
            .CountAsync(i => i.InvitedUserId == userId && i.Status == InviteStatus.Pending);
        ViewData["PendingInviteCount"] = pendingInviteCount;

        return View(myGroups);
    }

    // GET /Playgroup/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST /Playgroup/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string name, string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            ModelState.AddModelError("Name", "Playgroup name is required.");
            return View();
        }

        if (name.Length > 100)
        {
            ModelState.AddModelError("Name", "Playgroup name cannot exceed 100 characters.");
            return View();
        }

        var userId = GetUserId();

        var playgroup = new Playgroup
        {
            Name = name.Trim(),
            Description = description?.Trim(),
            CreatedByUserId = userId,
            IsPrivate = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Playgroups.Add(playgroup);
        await _db.SaveChangesAsync();

        var membership = new PlaygroupMember
        {
            PlaygroupId = playgroup.Id,
            UserId = userId,
            Role = PlaygroupRole.Owner,
            JoinedAt = DateTime.UtcNow
        };

        _db.PlaygroupMembers.Add(membership);
        await _db.SaveChangesAsync();

        return RedirectToAction("Details", new { id = playgroup.Id });
    }

    // GET /Playgroup/Details/5
    public async Task<IActionResult> Details(int id, string? status)
    {
        var userId = GetUserId();

        var playgroup = await _db.Playgroups
            .Include(g => g.Members)
            .Include(g => g.GameNightEvents)
            .FirstOrDefaultAsync(g => g.Id == id);

        if (playgroup == null)
            return NotFound();

        if (!playgroup.IsMember(userId))
            return NotFound();

        ViewData["UserId"] = userId;
        ViewData["Status"] = status;

         // Build a dictionary of UserId → UserName for display
        var memberIds = playgroup.Members.Select(m => m.UserId).ToList();
        var users = await _userManager.Users
            .Where(u => memberIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.UserName ?? "Unknown");
        ViewData["MemberNames"] = users;
        
        return View(playgroup);
    }

    // POST /Playgroup/LeavePlaygroup/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> LeavePlaygroup(int id)
    {
        var userId = GetUserId();

        var membership = await _db.PlaygroupMembers
            .FirstOrDefaultAsync(m => m.PlaygroupId == id && m.UserId == userId);

        if (membership == null)
            return NotFound();

        if (membership.Role == PlaygroupRole.Owner)
            return RedirectToAction("Details", new { id, status = "cannot-leave-owner" });

        var leavingUser = await _userManager.FindByIdAsync(userId);
        _db.PlaygroupMembers.Remove(membership);
        await _db.SaveChangesAsync();

        await PostSystemMessageAsync(id, $"{leavingUser?.UserName ?? "A member"} left the playgroup.");

        return RedirectToAction("Index");
    }

        // GET /Playgroup/InviteFriends/5
    public async Task<IActionResult> InviteFriends(int id)
    {
        var userId = GetUserId();

        var playgroup = await _db.Playgroups
            .Include(g => g.Members)
            .FirstOrDefaultAsync(g => g.Id == id);

        if (playgroup == null)
            return NotFound();

        if (!playgroup.IsMember(userId))
            return NotFound();

        if (!playgroup.IsOwner(userId))
            return Forbid();

        // Get current user's profile to query friendships
        var userProfile = await GetUserProfileAsync(userId);

        if (userProfile == null)
            return NotFound();

        // Get accepted friendships
        var friendships = await _db.Set<Friendship>()
            .Include(f => f.RequesterProfile).ThenInclude(p => p.User)
            .Include(f => f.ReceiverProfile).ThenInclude(p => p.User)
            .Where(f => f.Status == FriendshipStatus.Accepted &&
                       (f.RequesterProfileId == userProfile.Id || f.ReceiverProfileId == userProfile.Id))
            .ToListAsync();

        // Get friend UserIds
        var friendUserIds = friendships.Select(f =>
            f.RequesterProfileId == userProfile.Id
                ? f.ReceiverProfile.UserId
                : f.RequesterProfile.UserId
        ).ToList();

        // Exclude users who are already members
        var memberUserIds = playgroup.Members.Select(m => m.UserId).ToHashSet();

        // Exclude users who already have a pending invite
        var pendingInviteUserIds = await _db.PlaygroupInvites
            .Where(i => i.PlaygroupId == id && i.Status == InviteStatus.Pending)
            .Select(i => i.InvitedUserId)
            .ToListAsync();

        var excludeIds = memberUserIds.Union(pendingInviteUserIds).ToHashSet();

        // Build list of invitable friends
        var invitableFriends = new List<User>();
        foreach (var friendUserId in friendUserIds)
        {
            if (!excludeIds.Contains(friendUserId))
            {
                var friendUser = await _userManager.FindByIdAsync(friendUserId);
                if (friendUser != null)
                    invitableFriends.Add(friendUser);
            }
        }

        ViewData["PlaygroupId"] = id;
        ViewData["PlaygroupName"] = playgroup.Name;
        return View(invitableFriends);
    }

        // POST /Playgroup/SendInvite
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendInvite(int playgroupId, string invitedUserId)
    {
        var userId = GetUserId();

        var playgroup = await _db.Playgroups
            .Include(g => g.Members)
            .FirstOrDefaultAsync(g => g.Id == playgroupId);

        if (playgroup == null)
            return NotFound();

        if (!playgroup.IsOwner(userId))
            return Forbid();

        // Check not already a member
        if (playgroup.IsMember(invitedUserId))
            return RedirectToAction("InviteFriends", new { id = playgroupId });
        
        // Remove any old invite (accepted/declined) so we can re-invite
        var existingInvite = await _db.PlaygroupInvites
            .FirstOrDefaultAsync(i => i.PlaygroupId == playgroupId
                                   && i.InvitedUserId == invitedUserId);
        if (existingInvite != null)
        {
            if (existingInvite.Status == InviteStatus.Pending)
                return RedirectToAction("InviteFriends", new { id = playgroupId });
            
            _db.PlaygroupInvites.Remove(existingInvite);
            await _db.SaveChangesAsync();
        }
        
        // Create the invite
        var invite = new PlaygroupInvite
        {
            PlaygroupId = playgroupId,
            InvitedUserId = invitedUserId,
            InvitedByUserId = userId,
            Status = InviteStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        _db.PlaygroupInvites.Add(invite);

        // Create a notification for the invited user
        var invitedProfile = await GetUserProfileAsync(invitedUserId);

        if (invitedProfile != null)
        {
            var notification = new Notification
            {
                UserProfileId = invitedProfile.Id,
                Type = "PlaygroupInvite",
                Title = "Playgroup Invitation",
                Message = $"You've been invited to join {playgroup.Name}!",
                ActionUrl = "/Playgroup/PendingInvites",
                RelatedEntityId = invite.Id,
                CreatedAt = DateTime.UtcNow
            };
            _db.Set<Notification>().Add(notification);
        }

        await _db.SaveChangesAsync();

        return RedirectToAction("InviteFriends", new { id = playgroupId });
    }

        // GET /Playgroup/PendingInvites
    public async Task<IActionResult> PendingInvites()
    {
        var userId = GetUserId();

        var invites = await _db.PlaygroupInvites
            .Include(i => i.Playgroup)
            .Where(i => i.InvitedUserId == userId && i.Status == InviteStatus.Pending)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync();

        // Get inviter usernames
        var inviterIds = invites.Select(i => i.InvitedByUserId).Distinct().ToList();
        var inviterNames = await _userManager.Users
            .Where(u => inviterIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.UserName ?? "Unknown");
        ViewData["InviterNames"] = inviterNames;

        return View(invites);
    }

        // POST /Playgroup/AcceptInvite/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AcceptInvite(int id)
    {
        var userId = GetUserId();

        var invite = await _db.PlaygroupInvites
            .FirstOrDefaultAsync(i => i.Id == id && i.InvitedUserId == userId && i.Status == InviteStatus.Pending);

        if (invite == null)
            return NotFound();

        invite.Status = InviteStatus.Accepted;
        invite.RespondedAt = DateTime.UtcNow;

        // Add as member
        var membership = new PlaygroupMember
        {
            PlaygroupId = invite.PlaygroupId,
            UserId = userId,
            Role = PlaygroupRole.Member,
            JoinedAt = DateTime.UtcNow
        };

        _db.PlaygroupMembers.Add(membership);
        await _db.SaveChangesAsync();

        var joinedUser = await _userManager.FindByIdAsync(userId);
        await PostSystemMessageAsync(invite.PlaygroupId, $"{joinedUser?.UserName ?? "A member"} joined the playgroup.");

        return RedirectToAction("Details", new { id = invite.PlaygroupId });
    }

        // POST /Playgroup/DeclineInvite/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeclineInvite(int id)
    {
        var userId = GetUserId();

        var invite = await _db.PlaygroupInvites
            .FirstOrDefaultAsync(i => i.Id == id && i.InvitedUserId == userId && i.Status == InviteStatus.Pending);

        if (invite == null)
            return NotFound();

        invite.Status = InviteStatus.Declined;
        invite.RespondedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return RedirectToAction("PendingInvites");
    }

        // GET /Playgroup/DeletePlaygroup/5
    public async Task<IActionResult> DeletePlaygroup(int id)
    {
        var userId = GetUserId();

        var playgroup = await _db.Playgroups
            .Include(g => g.Members)
            .FirstOrDefaultAsync(g => g.Id == id);

        if (playgroup == null)
            return NotFound();

        if (!playgroup.IsMember(userId))
            return NotFound();

        if (!playgroup.IsOwner(userId))
            return Forbid();

        return View(playgroup);
    }

    // POST /Playgroup/ConfirmDelete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmDelete(int id, string confirmation)
    {
        var userId = GetUserId();

        var playgroup = await _db.Playgroups
            .Include(g => g.Members)
            .FirstOrDefaultAsync(g => g.Id == id);

        if (playgroup == null)
            return NotFound();

        if (!playgroup.IsOwner(userId))
            return Forbid();

        if (confirmation != "DELETE")
            return RedirectToAction("DeletePlaygroup", new { id, status = "invalid" });

        _db.Playgroups.Remove(playgroup);
        await _db.SaveChangesAsync();

        return RedirectToAction("Index");
    }

    private async Task PostSystemMessageAsync(int playgroupId, string text)
    {
        _db.PlaygroupMessages.Add(new PlaygroupMessage
        {
            PlaygroupId = playgroupId,
            SenderProfileId = null,
            Content = text,
            IsSystemMessage = true,
            SentAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
    }

    // GET /Playgroup/Chat/5
    public async Task<IActionResult> Chat(int id)
    {
        var userId = GetUserId();

        var playgroup = await _db.Playgroups
            .Include(g => g.Members)
            .FirstOrDefaultAsync(g => g.Id == id);

        if (playgroup == null || !playgroup.IsMember(userId))
            return NotFound();

        var messages = await _db.PlaygroupMessages
            .Where(m => m.PlaygroupId == id)
            .Include(m => m.SenderProfile)
                .ThenInclude(p => p!.User)
            .OrderBy(m => m.SentAt)
            .ToListAsync();

        ViewData["PlaygroupId"] = id;
        ViewData["PlaygroupName"] = playgroup.Name;
        return View(messages);
    }

    // POST /Playgroup/SendMessage
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendMessage(int playgroupId, string content)
    {
        var userId = GetUserId();

        var playgroup = await _db.Playgroups
            .Include(g => g.Members)
            .FirstOrDefaultAsync(g => g.Id == playgroupId);

        if (playgroup == null || !playgroup.IsMember(userId))
            return Json(new { success = false, error = "Access denied." });

        if (string.IsNullOrWhiteSpace(content))
            return Json(new { success = false, error = "Message cannot be empty." });

        if (content.Length > 1000)
            return Json(new { success = false, error = "Message cannot exceed 1000 characters." });

        var senderProfile = await GetUserProfileAsync(userId);
        if (senderProfile == null)
            return Json(new { success = false, error = "Profile not found." });

        var message = new PlaygroupMessage
        {
            PlaygroupId = playgroupId,
            SenderProfileId = senderProfile.Id,
            Content = content.Trim(),
            IsSystemMessage = false,
            SentAt = DateTime.UtcNow
        };

        _db.PlaygroupMessages.Add(message);
        await _db.SaveChangesAsync();

        var senderUser = await _userManager.FindByIdAsync(userId);
        var senderName = senderUser?.UserName ?? "Someone";

        await _chatHub.Clients
            .Group(PlaygroupChatHub.GroupName(playgroupId))
            .SendAsync("ReceiveMessage", new
            {
                senderName,
                avatarUrl = senderProfile.AvatarUrl,
                content = message.Content,
                sentAt = message.SentAt.ToString("MMM d, yyyy · h:mm tt")
            });

        // Notify all other members
        var otherMemberUserIds = playgroup.Members
            .Where(m => m.UserId != userId)
            .Select(m => m.UserId)
            .ToList();

        foreach (var memberId in otherMemberUserIds)
        {
            var profile = await GetUserProfileAsync(memberId);
            if (profile == null) continue;
            _db.Set<Notification>().Add(new Notification
            {
                UserProfileId = profile.Id,
                Type = "PlaygroupMessage",
                Title = $"New message in {playgroup.Name}",
                Message = $"{senderName}: {content.Trim().Substring(0, Math.Min(80, content.Trim().Length))}",
                ActionUrl = $"/Playgroup/Chat/{playgroupId}",
                RelatedEntityId = message.Id,
                CreatedAt = DateTime.UtcNow
            });
        }

        await _db.SaveChangesAsync();

        return Json(new
        {
            success = true,
            messageId = message.Id,
            senderName,
            avatarUrl = senderProfile.AvatarUrl,
            content = message.Content,
            sentAt = message.SentAt.ToString("MMM d, yyyy · h:mm tt")
        });
    }

    // POST /Playgroup/RemoveMember
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveMember(int playgroupId, string targetUserId)
    {
        var userId = GetUserId();

        var playgroup = await _db.Playgroups
            .Include(g => g.Members)
            .FirstOrDefaultAsync(g => g.Id == playgroupId);

        if (playgroup == null)
            return NotFound();

        if (!playgroup.IsOwner(userId))
            return Forbid();

        if (targetUserId == userId)
            return RedirectToAction("Details", new { id = playgroupId });

        var membership = await _db.PlaygroupMembers
            .FirstOrDefaultAsync(m => m.PlaygroupId == playgroupId && m.UserId == targetUserId);

        if (membership == null)
            return NotFound();

        var removedUser = await _userManager.FindByIdAsync(targetUserId);
        _db.PlaygroupMembers.Remove(membership);
        await _db.SaveChangesAsync();

        await PostSystemMessageAsync(playgroupId, $"{removedUser?.UserName ?? "A member"} was removed from the playgroup.");

        return RedirectToAction("Details", new { id = playgroupId });
    }

    // GET /Playgroup/Collection/5
    public async Task<IActionResult> Collection(int id)
    {
        var userId = GetUserId();

        var playgroup = await _db.Playgroups
            .Include(g => g.Members)
            .FirstOrDefaultAsync(g => g.Id == id);

        if (playgroup == null)
            return NotFound();

        //nonmembers cannot access the collection page
        if (!playgroup.IsMember(userId))
            return NotFound();

        //get all member user IDs
        var memberUserIds = playgroup.Members.Select(m => m.UserId).ToList();

        //load all game collection entries for all members, including the game details
        var collectionEntries = await _db.UserGameCollections
            .Where(c => memberUserIds.Contains(c.UserId))
            .Include(c => c.Game)
            .ToListAsync();

        //get usernames for display
        var memberNames = await _userManager.Users
            .Where(u => memberUserIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.UserName ?? "Unknown");

        //group by game, collecting which members own each one
        var combinedCollection = collectionEntries
            .GroupBy(c => c.GameId)
            .Select(g => new
            {
                Game = g.First().Game,
                Owners = g.Select(c => memberNames.GetValueOrDefault(c.UserId, "Unknown"))
                        .OrderBy(name => name)
                        .ToList()
            })
            .OrderBy(x => x.Game.Name)
            .ToList();

        ViewData["PlaygroupId"] = id;
        ViewData["PlaygroupName"] = playgroup.Name;
        ViewData["CombinedCollection"] = combinedCollection;

        return View();
    }

}
