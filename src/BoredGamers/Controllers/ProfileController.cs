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
            return NotFound();

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

        return View();
    }
}