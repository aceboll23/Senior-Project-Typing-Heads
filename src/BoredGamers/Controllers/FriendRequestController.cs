using BoredGamers.Data;
using BoredGamers.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BoredGamers.Controllers;

[Authorize]
public class FriendRequestController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<User> _userManager;
    private const int DailyRequestLimit = 10;
    private const int DeclineCooldownDays = 30;

    public FriendRequestController(ApplicationDbContext db, UserManager<User> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    // POST /FriendRequest/Send
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Send(string recipientUsername)
    {
        var sender = await _userManager.GetUserAsync(User);
        if (sender == null)
        { 
            return Unauthorized();
        }

        var senderProfile = await _db.Set<UserProfile>()
            .FirstOrDefaultAsync(p => p.UserId == sender.Id);
        if (senderProfile == null)
        { 
            return BadRequest("Sender profile not found.");
        }
        var recipient = await _userManager.Users.Include(u => u.Profile).FirstOrDefaultAsync(u => u.UserName == recipientUsername && !u.IsBanned && !u.IsDeactivated);

        if (recipient == null)
        { 
            return NotFound("user not found");
        }
        if(recipient.Id == sender.Id)
        {
             return NotFound("cannot send a request to yourself");
        }
        var recipientProfile = recipient.Profile;
        if  (recipientProfile == null)
        { 
            return BadRequest("Recipient profile not found");
        }

        //Check for any friendship record between the two users
        var existing = await _db.Set<Friendship>().FirstOrDefaultAsync( f =>(f.RequesterProfileId == senderProfile.Id && f.ReceiverProfileId == recipientProfile.Id) || 
            (f.RequesterProfileId == recipientProfile.Id && f.ReceiverProfileId == senderProfile.Id));

        if (existing != null)
        {
            if(existing.Status == FriendshipStatus.Accepted)
            {
                return Json(new {success = false, message = "Already friends"});
            }
            if(existing.Status == FriendshipStatus.Pending) 
            {
                return Json(new { success = false, message = "Request still pending"});
            }

            // Friend request cooldown
            if (existing.Status == FriendshipStatus.Declined)
            {
                var cooldownExpiry = existing.LastDeclinedAt?.AddDays(DeclineCooldownDays);
                if (cooldownExpiry.HasValue && DateTime.UtcNow < cooldownExpiry.Value)
                {
                    var daysLeft = (int)Math.Ceiling((cooldownExpiry.Value - DateTime.UtcNow).TotalDays);
                    return Json(new { success = false, message = $"You must wait {daysLeft} more day(s) before sending another request to this user." });
                }

                // Cooldown has passed — reuse the existing record and reset to pending
                existing.Status = FriendshipStatus.Pending;
                existing.RequesterProfileId = senderProfile.Id;
                existing.ReceiverProfileId = recipientProfile.Id;
                existing.RequestedAt = DateTime.UtcNow;
                existing.RespondedAt = null;
                existing.UpdatedAt = DateTime.UtcNow;

                _db.Set<FriendRequestRateLimit>().Add(new FriendRequestRateLimit
                {
                    UserProfileId = senderProfile.Id,
                    RequestSentAt = DateTime.UtcNow,
                    RequestDate = DateOnly.FromDateTime(DateTime.UtcNow)
                });

                await _db.SaveChangesAsync();
                return Json(new { success = true, status = "sent" });
            }

            if (existing.Status == FriendshipStatus.Cancelled)
            {
                // Cancelled requests can be resent freely — reuse the record
                existing.Status = FriendshipStatus.Pending;
                existing.RequesterProfileId = senderProfile.Id;
                existing.ReceiverProfileId = recipientProfile.Id;
                existing.RequestedAt = DateTime.UtcNow;
                existing.RespondedAt = null;
                existing.UpdatedAt = DateTime.UtcNow;

                _db.Set<FriendRequestRateLimit>().Add(new FriendRequestRateLimit
                {
                    UserProfileId = senderProfile.Id,
                    RequestSentAt = DateTime.UtcNow,
                    RequestDate = DateOnly.FromDateTime(DateTime.UtcNow)
                });

                await _db.SaveChangesAsync();
                return Json(new { success = true, status = "sent" });
            }
        }
        // Check daily rate limit
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var todayCount = await _db.Set<FriendRequestRateLimit>().CountAsync(r => r.UserProfileId == senderProfile.Id && r.RequestDate == today);

        if (todayCount >= DailyRequestLimit){
            return Json(new { success = false, message = "You have reached the daily limit of 10 friend requests." });
        }

        //Create new friendship record
        var friendship = new Friendship
        {
            RequesterProfileId = senderProfile.Id,
            ReceiverProfileId = recipientProfile.Id,
            Status = FriendshipStatus.Pending,
            RequestedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Set<Friendship>().Add(friendship);

        // Record the rate limit entry
        _db.Set<FriendRequestRateLimit>().Add(new FriendRequestRateLimit
        {
            UserProfileId = senderProfile.Id,
            RequestSentAt = DateTime.UtcNow,
            RequestDate = DateOnly.FromDateTime(DateTime.UtcNow)
        });

        await _db.SaveChangesAsync();

        return Json(new { success = true, status = "sent"});
    }

    //Post /FriendRequest/Cancel
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(string recipientUsername)
    {
        var sender = await _userManager.GetUserAsync(User);
        if (sender == null)
        {
            return Unauthorized();
        }

        var senderProfile = await _db.Set<UserProfile>().FirstOrDefaultAsync(p => p.UserId == sender.Id);
        if(senderProfile == null)
        {
            return BadRequest();
        }

        var recipient = await _userManager.Users.Include(u => u.Profile).FirstOrDefaultAsync(u => u.UserName == recipientUsername);
        if (recipient?.Profile == null)
        {
            return NotFound();
        }

        var friendship = await _db.Set<Friendship>().FirstOrDefaultAsync( f => f.RequesterProfileId == senderProfile.Id && f.ReceiverProfileId == recipient.Profile.Id && f.Status == FriendshipStatus.Pending);

        if(friendship == null)
        {
            return Json(new {success = false, message = "No pending request"});
        }

        friendship.Status = FriendshipStatus.Cancelled;
        friendship.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Json(new {success = true, status = "cancelled"});
    }

    // POST /FriendRequest/Accept
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Accept(int friendshipId)
    {
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser == null) return Unauthorized();

        var currentProfile = await _db.Set<UserProfile>()
            .FirstOrDefaultAsync(p => p.UserId == currentUser.Id);
        if (currentProfile == null)
        { 
            return BadRequest("Profile not found.");
        }
        // Makes sure the current user is the receiver and not the sender
        var friendship = await _db.Set<Friendship>().FirstOrDefaultAsync(f => f.Id == friendshipId &&
                f.ReceiverProfileId == currentProfile.Id &&
                f.Status == FriendshipStatus.Pending);

        if (friendship == null) {
            return Json(new { success = false, message = "Request not found." });
        }

        friendship.Status = FriendshipStatus.Accepted;
        friendship.RespondedAt = DateTime.UtcNow;
        friendship.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Json(new { success = true });
    }

    // POST /FriendRequest/Decline
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Decline(int friendshipId)
    {
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser == null){ 
            return Unauthorized();
        }
        var currentProfile = await _db.Set<UserProfile>().FirstOrDefaultAsync(p => p.UserId == currentUser.Id);
        if (currentProfile == null) return BadRequest("Profile not found.");

        // Makes sure the current user is the receiver and not the sender
        var friendship = await _db.Set<Friendship>().FirstOrDefaultAsync(f => f.Id == friendshipId &&
                f.ReceiverProfileId == currentProfile.Id &&
                f.Status == FriendshipStatus.Pending);

        if (friendship == null) {
            return Json(new { success = false, message = "Request not found." });
        }

        friendship.Status = FriendshipStatus.Declined;
        friendship.RespondedAt = DateTime.UtcNow;
        friendship.LastDeclinedAt = DateTime.UtcNow;
        friendship.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Json(new { success = true });
    }
}   