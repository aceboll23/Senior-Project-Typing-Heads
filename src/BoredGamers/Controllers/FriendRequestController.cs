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
}   