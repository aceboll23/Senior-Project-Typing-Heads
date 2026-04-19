using BoredGamers.Data;
using BoredGamers.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BoredGamers.Controllers;

[Authorize]
public class FriendsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<User> _userManager;

    public FriendsController(ApplicationDbContext db, UserManager<User> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveFriend(int friendProfileId)
    {
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser == null)
            return Challenge();

        var currentProfile = await _db.Set<UserProfile>().FirstOrDefaultAsync(p => p.UserId == currentUser.Id);
        if (currentProfile == null)
            return Challenge();

        var friendship = await _db.Set<Friendship>().FirstOrDefaultAsync(f =>
            f.Status == FriendshipStatus.Accepted &&
            ((f.RequesterProfileId == currentProfile.Id && f.ReceiverProfileId == friendProfileId) ||
             (f.ReceiverProfileId == currentProfile.Id && f.RequesterProfileId == friendProfileId)));

        if (friendship != null)
        {
            _db.Set<Friendship>().Remove(friendship);
            await _db.SaveChangesAsync();
        }

        return RedirectToAction("Index");
    }

    // GET /Friends
    public async Task<IActionResult> Index()
    {
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

        //gets all accepted friendships for the current user
        var friendships = await _db.Set<Friendship>().Where(f => (f.RequesterProfileId == currentProfile.Id || f.ReceiverProfileId == currentProfile.Id) && f.Status == FriendshipStatus.Accepted).ToListAsync();

        //gets the profile id of the other user in each friendship
        var friendProfileIds = friendships.Select(f => f.RequesterProfileId == currentProfile.Id ? f.ReceiverProfileId : f.RequesterProfileId).ToList();

        //load each friend's user and profile
        var friends = await _userManager.Users.Include(u => u.Profile)
            .Where(u => u.Profile != null && friendProfileIds.Contains(u.Profile.Id) && !u.IsBanned && !u.IsDeactivated).OrderBy(u => u.UserName).ToListAsync();
        
        return View(friends);
    }
}