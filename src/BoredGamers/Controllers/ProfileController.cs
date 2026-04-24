using BoredGamers.Data;
using BoredGamers.Models;
using BoredGamers.Services.Block;
using BoredGamers.Services.Posts;
using BoredGamers.Services.ProfilePicture;
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
    private readonly IProfilePostService _postService;
    private readonly IBlockService _blockService;
    private readonly IProfilePictureService _profilePictureService;

    public ProfileController(ApplicationDbContext db, UserManager<User> userManager, IProfilePostService postService, IBlockService blockService, IProfilePictureService profilePictureService)
    {
        _db = db;
        _userManager = userManager;
        _postService = postService;
        _blockService = blockService;
        _profilePictureService = profilePictureService;
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

        // If the profile owner has blocked the current viewer, show a generic not-available page
        if (!isOwnProfile && currentUser != null)
        {
            var isBlocked = await _blockService.IsBlockedByAsync(currentUser.Id, profileUser.Id);
            if (isBlocked)
            {
                ViewData["ProfileUsername"] = username;
                return View("ProfileNotAvailable");
            }
        }

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
                var friendship = await _db.Set<Friendship>().FirstOrDefaultAsync(f => (f.RequesterProfileId == currentProfile.Id && f.ReceiverProfileId == targetProfile.Id) ||
                        (f.RequesterProfileId == targetProfile.Id && f.ReceiverProfileId == currentProfile.Id));

                ViewData["FriendshipStatus"] = friendship?.Status.ToString();
                ViewData["FriendshipRequesterId"] = friendship?.RequesterProfileId;
                ViewData["CurrentUserProfileId"] = currentProfile.Id;

                // Check if current user has blocked the target
                var hasBlocked = await _db.Set<BlockedUser>().AnyAsync(b =>
                    b.BlockerProfileId == currentProfile.Id && b.BlockedProfileId == targetProfile.Id);
                ViewData["HasBlocked"] = hasBlocked;
            }
        }

        // Load pending incoming friend requests (only on own profile)
        /**
        if (isOwnProfile && currentUser != null)
        {
            var currentProfile = await _db.Set<UserProfile>().FirstOrDefaultAsync(p => p.UserId == currentUser.Id);

            if (currentProfile != null)
            {
                var pendingRequests = await _db.Set<Friendship>()
                    .Where(f => f.ReceiverProfileId == currentProfile.Id && f.Status == FriendshipStatus.Pending)
                    .Include(f => f.RequesterProfile)
                    .ToListAsync();

                // Load the User for each requester to get username and avatar
                var requestsWithUsers = new List<object>();
                foreach (var f in pendingRequests)
                {
                    var requesterUser = await _userManager.Users.FirstOrDefaultAsync(u => u.Profile != null && u.Profile.Id == f.RequesterProfileId);

                    requestsWithUsers.Add(new
                    {
                        FriendshipId = f.Id,
                        Username = requesterUser?.UserName,
                        AvatarUrl = f.RequesterProfile.AvatarUrl,
                        RequestedAt = f.RequestedAt.ToString("MMM dd, yyyy")
                    });
                }

                ViewData["PendingRequests"] = requestsWithUsers;
            }
        }
        */

        var posts = await _postService.GetPostsByUserIdAsync(profileUser.Id);
        ViewData["ProfilePosts"] = posts;

        var games = await _db.Games
            .AsNoTracking()
            .OrderBy(g => g.Name)
            .Select(g => new { g.Id, g.Name })
            .ToListAsync();
        ViewData["Games"] = games;

        return View();
    }

    [HttpPost]
    public async Task<IActionResult> UploadAvatar(IFormFile? avatar)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Challenge();

        if (avatar == null || avatar.Length == 0)
        {
            TempData["AvatarError"] = "Please select a file.";
            return RedirectToAction("Index", new { username = user.UserName });
        }

        var result = await _profilePictureService.UploadAvatarAsync(user.Id, avatar);

        if (!result.Success)
            TempData["AvatarError"] = result.ErrorMessage;

        return RedirectToAction("Index", new { username = user.UserName });
    }
}