using BoredGamers.Data;
using BoredGamers.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BoredGamers.Controllers;

[Authorize] // Only authenticated users can search
public class UserSearchController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<User> _userManager;

    public UserSearchController(ApplicationDbContext db, UserManager<User> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    // GET /UserSearch/Search?q=partial
    [HttpGet]
    public async Task<IActionResult> Search(string q)
    {
        // Need at least 2 characters
        if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
            return Json(new List<object>());

        var results = await _userManager.Users
            .Include(u => u.Profile)
            .Where(u =>
                u.UserName!.Contains(q) &&
                !u.IsBanned &&
                !u.IsDeactivated)
            .OrderBy(u => u.UserName)
            .Take(10)
            .Select(u => new
            {
                username = u.UserName,
                avatarUrl = u.Profile != null ? u.Profile.AvatarUrl : null,
                isPublic = u.Profile == null || u.Profile.IsProfilePublic
            })
            .ToListAsync();

        return Json(results);
    }

    // GET /UserSearch
    public async Task<IActionResult> Index(string? q)
    {
        var results = new List<object>();

        if (!string.IsNullOrWhiteSpace(q) && q.Length >= 2)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var currentProfile = currentUser != null
                ? await _db.Set<UserProfile>().FirstOrDefaultAsync(p => p.UserId == currentUser.Id)
                : null;

            var users = await _userManager.Users
                .Include(u => u.Profile)
                .Where(u =>
                    u.UserName!.Contains(q) &&
                    !u.IsBanned &&
                    !u.IsDeactivated)
                .OrderBy(u => u.UserName)
                .Take(20)
                .ToListAsync();

            foreach (var u in users)
            {
                // Check friendship status
                string? friendshipStatus = null;
                bool isSender = false;

                if (currentProfile != null && u.Profile != null)
                {
                    var friendship = await _db.Set<Friendship>().FirstOrDefaultAsync(f =>
                        (f.RequesterProfileId == currentProfile.Id && f.ReceiverProfileId == u.Profile.Id) ||
                        (f.RequesterProfileId == u.Profile.Id && f.ReceiverProfileId == currentProfile.Id));

                    if (friendship != null)
                    {
                        friendshipStatus = friendship.Status.ToString();
                        isSender = friendship.RequesterProfileId == currentProfile.Id;
                    }
                }

                results.Add(new
                {
                    username = u.UserName,
                    avatarUrl = u.Profile?.AvatarUrl,
                    isPublic = u.Profile == null || u.Profile.IsProfilePublic,
                    friendshipStatus,
                    isSender
                });
            }
        }

        ViewData["Query"] = q ?? string.Empty;
        ViewData["Results"] = results;
        return View();
    }
}