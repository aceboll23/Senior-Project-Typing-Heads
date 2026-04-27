using BoredGamers.Data;
using BoredGamers.Models;
using BoredGamers.Services.Block;
using BoredGamers.Services.Email;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BoredGamers.Controllers;

[Authorize]
public class SettingsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly IEmailService _emailService;
    private readonly IBlockService _blockService;

    public SettingsController(ApplicationDbContext db, UserManager<User> userManager, SignInManager<User> signInManager, IEmailService emailService, IBlockService blockService)
    {
        _db = db;
        _userManager = userManager;
        _signInManager = signInManager;
        _emailService = emailService;
        _blockService = blockService;
    }

    // GET /Settings
    public async Task<IActionResult> Index()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null){ 
            return Challenge();
        }
        var model = new SettingsViewModel
        {
            Username = user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty
        };

        // Shows pending email notice if they have one awaiting verification
        if (!string.IsNullOrEmpty(user.PendingEmail))
        { 
            ViewData["PendingEmail"] = user.PendingEmail;
        }
        return View(model);
    }

    // GET /Settings/DeleteAccount
    [HttpGet]
    public IActionResult DeleteAccount()
    {
        return View();
    }

    // POST/Settings/DeleteAccount
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteAccount(DeleteAccountViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return Challenge();
        }

        // Verifies password before deleting
        var passwordValid = await _userManager.CheckPasswordAsync(user, model.CurrentPassword);
        if (!passwordValid)
        {
            ModelState.AddModelError(nameof(model.CurrentPassword), "Password is incorrect");
            return View(model);
        }

        // Find the user's profile
        var profile = await _db.Set<UserProfile>().FirstOrDefaultAsync(p => p.UserId == user.Id);

        if (profile != null)
        {
            // Delete friendships in BOTH directions (where user is requester OR receiver)
            var friendships = await _db.Set<Friendship>()
                .Where(f => f.RequesterProfileId == profile.Id || f.ReceiverProfileId == profile.Id)
                .ToListAsync();
            if (friendships.Count > 0)
            {
                _db.Set<Friendship>().RemoveRange(friendships);
            }

            // Delete blocks in both directions
            var blocks = await _db.Set<BlockedUser>()
                .Where(b => b.BlockerProfileId == profile.Id || b.BlockedProfileId == profile.Id)
                .ToListAsync();
            if (blocks.Count > 0)
            {
                _db.Set<BlockedUser>().RemoveRange(blocks);
            }

            // Delete direct messages in both directions
            var messages = await _db.DirectMessages
                .Where(m => m.SenderProfileId == profile.Id || m.RecipientProfileId == profile.Id)
                .ToListAsync();
            if (messages.Count > 0)
            {
                _db.DirectMessages.RemoveRange(messages);
            }

            // Delete profile posts
            var posts = await _db.ProfilePosts
                .Where(p => p.UserProfileId == profile.Id)
                .ToListAsync();
            if (posts.Count > 0)
            {
                _db.ProfilePosts.RemoveRange(posts);
            }

            // Delete playgroup messages sent by this user
            var playgroupMessages = await _db.PlaygroupMessages
                .Where(m => m.SenderProfileId == profile.Id)
                .ToListAsync();
            if (playgroupMessages.Count > 0)
            {
                _db.PlaygroupMessages.RemoveRange(playgroupMessages);
            }

            await _db.SaveChangesAsync();
        }

        // Delete game votes by this user
        var votes = await _db.GameVotes
            .Where(v => v.UserId == user.Id)
            .ToListAsync();
        if (votes.Count > 0)
        {
            _db.GameVotes.RemoveRange(votes);
            await _db.SaveChangesAsync();
        }

        // Delete event responses
        var responses = await _db.EventResponses
            .Where(r => r.UserId == user.Id)
            .ToListAsync();
        if (responses.Count > 0)
        {
            _db.EventResponses.RemoveRange(responses);
            await _db.SaveChangesAsync();
        }

        // Delete event games this user added
        var eventGames = await _db.GameNightEventGames
            .Where(eg => eg.UserId == user.Id)
            .ToListAsync();
        if (eventGames.Count > 0)
        {
            _db.GameNightEventGames.RemoveRange(eventGames);
            await _db.SaveChangesAsync();
        }

        // Delete game night events created by this user
        var events = await _db.GameNightEvents
            .Where(e => e.CreatedByUserId == user.Id)
            .ToListAsync();
        if (events.Count > 0)
        {
            _db.GameNightEvents.RemoveRange(events);
            await _db.SaveChangesAsync();
        }

        // Delete playgroup memberships
        var memberships = await _db.PlaygroupMembers
            .Where(m => m.UserId == user.Id)
            .ToListAsync();
        if (memberships.Count > 0)
        {
            _db.PlaygroupMembers.RemoveRange(memberships);
            await _db.SaveChangesAsync();
        }

        // Delete game collection
        var collection = await _db.UserGameCollections
            .Where(c => c.UserId == user.Id)
            .ToListAsync();
        if (collection.Count > 0)
        {
            _db.UserGameCollections.RemoveRange(collection);
            await _db.SaveChangesAsync();
        }

        // Delete reviews
        var reviews = await _db.Reviews
            .Where(r => r.UserId == user.Id)
            .ToListAsync();
        if (reviews.Count > 0)
        {
            _db.Reviews.RemoveRange(reviews);
            await _db.SaveChangesAsync();
        }

        // Signs out before deleting so the session is cleared immediately
        await _signInManager.SignOutAsync();

        var result = await _userManager.DeleteAsync(user);
        if (!result.Succeeded)
        {
            // Signs back in if deletion failed so the user isn't stranded
            await _signInManager.SignInAsync(user, isPersistent: false);
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return View(model);
        }
        return RedirectToAction("Index", "Home");
    }

    // POST /Settings
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(SettingsViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }
        var user = await _userManager.GetUserAsync(User);
        if (user == null){
             return Challenge();
        }
        // Verifies current password before making any changes
        var passwordValid = await _userManager.CheckPasswordAsync(user, model.CurrentPassword);
        if (!passwordValid)
        {
            ModelState.AddModelError(nameof(model.CurrentPassword), "Current password is incorrect.");
            return View(model);
        }

        var usernameChanged = !string.Equals(user.UserName, model.Username, StringComparison.OrdinalIgnoreCase);
        var emailChanged = !string.Equals(user.Email, model.Email, StringComparison.OrdinalIgnoreCase);

        // Checks username uniqueness if changed
        if (usernameChanged)
        {
            var existingUser = await _userManager.FindByNameAsync(model.Username);
            if (existingUser != null && existingUser.Id != user.Id)
            {
                ModelState.AddModelError(nameof(model.Username), "That username is already taken.");
                return View(model);
            }

            var setUsernameResult = await _userManager.SetUserNameAsync(user, model.Username);
            if (!setUsernameResult.Succeeded)
            {
                foreach (var error in setUsernameResult.Errors){
                    ModelState.AddModelError(nameof(model.Username), error.Description);
                }
                return View(model);
            }
        }

        // Handles email change — store as pending and send verification
        if (emailChanged)
        {
            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null && existingUser.Id != user.Id)
            {
                ModelState.AddModelError(nameof(model.Email), "That email is already in use.");
                return View(model);
            }

            // Stores the new email as pending until verified
            var token = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));

            user.PendingEmail = model.Email;
            user.EmailVerificationToken = token;
            user.EmailVerificationTokenExpiry = DateTime.UtcNow.AddHours(24);
            user.UpdatedAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            // Builds verification link
            var verifyLink = Url.Action(
                "VerifyEmail",
                "Settings",
                new { token },
                Request.Scheme);

            var subject = "BoredGamers — Verify Your New Email";
            var body = $@"
                <p>Hi {user.UserName},</p>
                <p>You requested to change your email address on BoredGamers.</p>
                <p><a href='{verifyLink}'>Click here to confirm your new email address</a></p>
                <p>This link expires in 24 hours.</p>
                <p>If you did not request this, you can ignore this email. Your current email will remain unchanged.</p>";

            await _emailService.SendEmailAsync(model.Email, subject, body);

            ViewData["PendingEmail"] = model.Email;
            ViewData["SuccessMessage"] = usernameChanged
                ? "Username updated. A verification email has been sent to your new address."
                : "A verification email has been sent to your new address.";

            return View(model);
        }

        user.UpdatedAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        // Refreshes the auth cookie so the new username shows in the nav immediately
        await _signInManager.RefreshSignInAsync(user);

        ViewData["SuccessMessage"] = "Your settings have been updated.";
        return View(model);
    }

    // GET /Settings/VerifyEmail?token=xxx
    [HttpGet]
    public async Task<IActionResult> VerifyEmail(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return RedirectToAction("Index");

        var user = await _db.Users.OfType<User>()
            .FirstOrDefaultAsync(u => u.EmailVerificationToken == token && u.EmailVerificationTokenExpiry > DateTime.UtcNow);

        if (user == null)
        {
            ViewData["ErrorMessage"] = "This verification link is invalid or has expired.";
            return View("VerifyEmailError");
        }

        // Applies the pending email
        var setEmailResult = await _userManager.SetEmailAsync(user, user.PendingEmail);
        if (!setEmailResult.Succeeded)
        {
            ViewData["ErrorMessage"] = "Could not update your email address. It may already be in use.";
            return View("VerifyEmailError");
        }

        // Clears pending email fields
        user.PendingEmail = null;
        user.EmailVerificationToken = null;
        user.EmailVerificationTokenExpiry = null;
        user.UpdatedAt = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        await _signInManager.RefreshSignInAsync(user);

        return View("VerifyEmailSuccess");
    }

    // GET /Settings/BlockedUsers
    public async Task<IActionResult> BlockedUsers()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        var blockedUsers = await _blockService.GetBlockedUsersAsync(user.Id);
        return View(blockedUsers);
    }
}