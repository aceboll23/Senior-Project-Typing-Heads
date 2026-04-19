using BoredGamers.Data;
using BoredGamers.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BoredGamers.Controllers;

[Authorize]
public class MessagesController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<User> _userManager;

    public MessagesController(ApplicationDbContext db, UserManager<User> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    // GET /Messages (inbox for showing all conversations)
    public async Task<IActionResult> Index()
    {
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser == null)
        {
            return Challenge();
        }

        var currentProfile = await _db.Set<UserProfile>().FirstOrDefaultAsync(p => p.UserId == currentUser.Id);
        
        if(currentProfile == null)
        {
            return Challenge();
        }

        // Get all friend profile IDs
        var friendProfileIds = await _db.Set<Friendship>()
            .Where(f => (f.RequesterProfileId == currentProfile.Id || f.ReceiverProfileId == currentProfile.Id) && f.Status == FriendshipStatus.Accepted)
            .Select(f => f.RequesterProfileId == currentProfile.Id ? f.ReceiverProfileId : f.RequesterProfileId)
            .ToListAsync();            
            
        // Get latest message from conversation
        var allMessages = await _db.DirectMessages
            .Where(m => (m.RecipientProfileId == currentProfile.Id && !m.DeletedByRecipient) || (m.SenderProfileId == currentProfile.Id && !m.DeletedBySender))
            .Include(m => m.SenderProfile)
            .Include(m => m.RecipientProfile)
            .OrderByDescending(m => m.SentAt)
            .ToListAsync();

        // Group into conversations (one entry per other user)
        var conversations = allMessages
            .GroupBy(m => m.SenderProfileId == currentProfile.Id ? m.RecipientProfileId : m.SenderProfileId)
            .Select(g => g.First())
            .ToList();

        // Split into friend messages and message requests
        var friendConversations = conversations
            .Where(m =>
            {
                var otherId = m.SenderProfileId == currentProfile.Id ? m.RecipientProfileId : m.SenderProfileId;
                return friendProfileIds.Contains(otherId);
            })
            .ToList();

        var messageRequests = conversations
            .Where(m =>
            {
                var otherId = m.SenderProfileId == currentProfile.Id ? m.RecipientProfileId : m.SenderProfileId;
                return !friendProfileIds.Contains(otherId);
            })
            .ToList();

            // Load usernames for display
            //Bad for scaling: Warning this loads in all users. 
            var allProfiles = await _db.Set<UserProfile>().Include(p => p.User).ToListAsync();

            ViewData["CurrentProfileId"] = currentProfile.Id;
            ViewData["FriendConversations"] = friendConversations;
            ViewData["MessageRequests"] = messageRequests;
            ViewData["AllProfiles"] = allProfiles;

            return View();
    }

    // GET /Messages/Conversations/{username}
    public async Task<IActionResult> Conversation(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            return NotFound();
        }
        
        var currentUser = await _userManager.GetUserAsync(User);
        if(currentUser == null)
        {
            return Challenge();
        }

        var currentProfile = await _db.Set<UserProfile>().FirstOrDefaultAsync(p => p.UserId == currentUser.Id);
        if(currentProfile == null)
        {
            return Challenge();
        }

        var otherUser = await _userManager.Users.Include(u => u.Profile).FirstOrDefaultAsync(u => u.UserName == username && !u.IsBanned && !u.IsDeactivated);
        if(otherUser?.Profile == null)
        {
            return NotFound();
        }

        var otherProfile = otherUser.Profile;

        //Load full conversation in CHRONOLOGICAL order
        var messages = await _db.DirectMessages
            .Where(m =>
                (m.SenderProfileId == currentProfile.Id && m.RecipientProfileId == otherProfile.Id && !m.DeletedBySender) ||
                (m.SenderProfileId == otherProfile.Id && m.RecipientProfileId == currentProfile.Id && !m.DeletedByRecipient))
            .OrderBy(m => m.SentAt).ToListAsync();

        // Mark unread messages as delivered/read
        var unread = messages
            .Where(m => m.RecipientProfileId == currentProfile.Id && m.Status != MessageStatus.Read)
            .ToList();
        foreach (var msg in unread)
        {
            msg.Status = MessageStatus.Read;
            msg.ReadAt = DateTime.UtcNow;
        }

        if(unread.Any())
        {
            await _db.SaveChangesAsync();
        }

        //Check if friends
        var isFriend = await _db.Set<Friendship>()
            .AnyAsync(f => ((f.RequesterProfileId == currentProfile.Id && f.ReceiverProfileId == otherProfile.Id) ||
            (f.RequesterProfileId == otherProfile.Id && f.ReceiverProfileId == currentProfile.Id)) && f.Status == FriendshipStatus.Accepted);
        
        ViewData["CurrentProfileId"] = currentProfile.Id;
        ViewData["OtherUsername"] = otherUser.UserName;
        ViewData["OtherAvatarUrl"] = otherProfile.AvatarUrl;
        ViewData["OtherProfileId"] = otherProfile.Id;
        ViewData["IsFriend"] = isFriend;
        ViewData["Messages"] = messages;

        return View();
    }

    //POST /Messages/Send
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Send(string recipientUsername, string content)
    {
        if(string.IsNullOrWhiteSpace(content))
        {
            return Json(new {success = false, message = "Message cannot be empty"});
        }
        if(content.Length > 1000)
        {
            return Json(new {success = false, message = "Message cannot exceed 1000 characters"});
        }

        var currentUser = await _userManager.GetUserAsync(User);
        if(currentUser == null)
        {
            return Unauthorized();
        }

        var currentProfile = await _db.Set<UserProfile>().FirstOrDefaultAsync(p => p.UserId == currentUser.Id);
        if(currentProfile == null)
        {
            return BadRequest("Profile not found");
        }

        var recipient = await _userManager.Users.Include(u => u.Profile).FirstOrDefaultAsync(u => u.UserName == recipientUsername && !u.IsBanned && !u.IsDeactivated);
        if(recipient?.Profile == null)
        {
            return NotFound("User not found");
        }
        if(recipient.Id == currentUser.Id)
        {
            return BadRequest ("Cannot message yourself");
        }

        // Prevent messaging if a block exists in either direction
        var isBlocked = await _db.Set<BlockedUser>().AnyAsync(b =>
            (b.BlockerProfileId == currentProfile.Id && b.BlockedProfileId == recipient.Profile.Id) ||
            (b.BlockerProfileId == recipient.Profile.Id && b.BlockedProfileId == currentProfile.Id));
        if (isBlocked)
            return Json(new { success = false, message = "Unable to send message." });

        var message = new DirectMessage
        {
            SenderProfileId = currentProfile.Id,
            RecipientProfileId = recipient.Profile.Id,
            Content = content,
            Status = MessageStatus.Sent,
            SentAt = DateTime.UtcNow
        };

        _db.DirectMessages.Add(message);
        await _db.SaveChangesAsync();

        return Json(new
        {
            success = true,
            messageId = message.Id,
            sentAt = message.SentAt.ToString("MMM dd, h:mm tt"),
            content = message.Content,
            status = message.Status.ToString()
        });
    }
}