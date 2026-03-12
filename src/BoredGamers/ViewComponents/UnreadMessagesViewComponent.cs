using BoredGamers.Data;
using BoredGamers.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BoredGamers.ViewComponents;

public class UnreadMessagesViewComponent : ViewComponent
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<User> _userManager;

    public UnreadMessagesViewComponent(ApplicationDbContext db, UserManager<User> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        if (UserClaimsPrincipal?.Identity?.IsAuthenticated != true)
            return Content(string.Empty);

        var currentUser = await _userManager.GetUserAsync(UserClaimsPrincipal);
        if (currentUser == null)
            return Content(string.Empty);

        var currentProfile = await _db.Set<UserProfile>()
            .FirstOrDefaultAsync(p => p.UserId == currentUser.Id);
        if (currentProfile == null)
            return Content(string.Empty);

        var unreadCount = await _db.DirectMessages
            .Where(m =>
                m.RecipientProfileId == currentProfile.Id &&
                m.Status != MessageStatus.Read &&
                !m.DeletedByRecipient)
            .Select(m => m.SenderProfileId)
            .Distinct()
            .CountAsync();

        if (unreadCount == 0)
            return Content(string.Empty);

        return View(unreadCount);
    }
}