using BoredGamers.Data;
using BoredGamers.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BoredGamers.ViewComponents;

public class UserAvatarViewComponent : ViewComponent
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<User> _userManager;

    public UserAvatarViewComponent(ApplicationDbContext db, UserManager<User> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        if (UserClaimsPrincipal?.Identity?.IsAuthenticated != true)
            return Content(string.Empty);

        var user = await _userManager.GetUserAsync(UserClaimsPrincipal);
        if (user == null) return Content(string.Empty);

        var profile = await _db.Set<UserProfile>().FirstOrDefaultAsync(p => p.UserId == user.Id);
        var avatarUrl = profile?.AvatarUrl;

        return View(model: avatarUrl);
    }
}
