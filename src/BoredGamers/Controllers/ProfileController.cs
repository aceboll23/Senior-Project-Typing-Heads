using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using BoredGamers.Models;

namespace BoredGamers.Controllers
{
    // Serves user profile pages at /Profile/{username}
    public class ProfileController : Controller
    {
        private readonly UserManager<User> _userManager;

        // Dependency injection - ASP.NET provides UserManager
        // which lets us look up users in the Identity database
        public ProfileController(UserManager<User> userManager)
        {
            _userManager = userManager;
        }

        // GET /Profile/{username}
        [Route("Profile/{username}")]
        public async Task<IActionResult> Index(string username)
        {
            // Look up the user by their username
            var profileUser = await _userManager.FindByNameAsync(username);

            if (profileUser == null)
            {
                return View("NotFound");
            }

            // Check if the logged-in user is viewing their own profile
            var currentUser = await _userManager.GetUserAsync(User);
            var isOwnProfile = currentUser != null && currentUser.Id == profileUser.Id;

            ViewData["IsOwnProfile"] = isOwnProfile;
            ViewData["ProfileUsername"] = profileUser.UserName;
            ViewData["MemberSince"] = profileUser.CreatedAt.ToString("MMMM yyyy");
            ViewData["Email"] = isOwnProfile ? profileUser.Email : null;
            ViewData["FirstName"] = profileUser.FirstName;
            ViewData["LastName"] = profileUser.LastName;

            return View(profileUser);
        }
    }
}
