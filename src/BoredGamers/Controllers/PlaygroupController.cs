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
 namespace BoredGamers.Controllers;

[Authorize]
public class PlaygroupController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<User> _userManager;

    public PlaygroupController(ApplicationDbContext db, UserManager<User> userManager)
    {
        _db = db;
        _userManager = userManager;
    }
    private string GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

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

        //private group still returns not found
        if (playgroup.IsPrivate && !playgroup.IsMember(userId))
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

        _db.PlaygroupMembers.Remove(membership);
        await _db.SaveChangesAsync();

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

        if (!playgroup.IsOwner(userId))
            return Forbid();

        // Get current user's profile to query friendships
        var userProfile = await _db.Set<UserProfile>()
            .FirstOrDefaultAsync(p => p.UserId == userId);

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
        var invitedProfile = await _db.Set<UserProfile>()
            .FirstOrDefaultAsync(p => p.UserId == invitedUserId);

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

}
