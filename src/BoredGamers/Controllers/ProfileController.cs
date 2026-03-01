using BoredGamers.Data;
using BoredGamers.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BoredGamers.Controllers;

[Authorize]
public class ProfileController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<User> _userManager;

    public ProfileController(ApplicationDbContext db, UserManager<User> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    // GET /Profile/{username}
    public async Task<IActionResult> Index(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            var me = await _userManager.GetUserAsync(User);
            if (me == null) return Challenge();
            username = me.UserName;
        }
        //           return NotFound();

        var profileUser = await _userManager.Users
            .Include(u => u.Profile)
            .FirstOrDefaultAsync(u => u.UserName == username && !u.IsBanned && !u.IsDeactivated);

        if (profileUser == null)
            return NotFound();

        var currentUser = await _userManager.GetUserAsync(User);
        var isOwnProfile = currentUser?.Id == profileUser.Id;

        // Block access to private profiles (currently commented out because there is no setting to turn on/off private profile)
        /**
        if (!isOwnProfile && (profileUser.Profile == null || !profileUser.Profile.IsProfilePublic))
        {
            ViewData["Username"] = profileUser.UserName;
            //returns to private view
            return View("PrivateProfile");
        }*/

        ViewData["IsOwnProfile"] = isOwnProfile;
        ViewData["ProfileUsername"] = profileUser.UserName;
        ViewData["Email"] = profileUser.Email;
        ViewData["FirstName"] = profileUser.FirstName;
        ViewData["LastName"] = profileUser.LastName;
        ViewData["MemberSince"] = profileUser.CreatedAt.ToString("MMMM yyyy");
        ViewData["AvatarUrl"] = profileUser.Profile?.AvatarUrl;
        ViewData["ShowEmail"] = profileUser.Profile?.ShowEmail ?? false;
        ViewData["ShowRealName"] = profileUser.Profile?.ShowRealName ?? true;

        var ownerUserId = isOwnProfile
            ? currentUser!.Id
            : profileUser.Id;

        //Load user's game collection (basic list)
        var collection = await _db.UserGameCollections
            .AsNoTracking()
            .Where(c => c.UserId == profileUser.Id)
            .Include(c => c.Game)
            .OrderByDescending(c => c.DateAdded)
            .Select(c => c.Game)
            .ToListAsync();

        ViewData["Collection"] = collection;

        if (!isOwnProfile && currentUser != null)
        {
            var currentProfile = await _db.Set<UserProfile>().FirstOrDefaultAsync(p => p.UserId == currentUser.Id);
            var targetProfile = profileUser.Profile;

            if (currentProfile != null && targetProfile != null)
            {
                var friendship = await _db.Set<Friendship>()
                    .FirstOrDefaultAsync(f => (f.RequesterProfileId == currentProfile.Id && f.ReceiverProfileId == targetProfile.Id) ||
                        (f.RequesterProfileId == targetProfile.Id && f.ReceiverProfileId == currentProfile.Id));

                ViewData["FriendshipStatus"] = friendship?.Status.ToString();
                ViewData["FriendshipRequesterId"] = friendship?.RequesterProfileId;
                ViewData["CurrentUserProfileId"] = currentProfile.Id;
            }
        }

        return View();
    }
}