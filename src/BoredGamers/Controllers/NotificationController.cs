using System.Security.Claims;
using System.Threading.Tasks;
using BoredGamers.Data;
using BoredGamers.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BoredGamers.Controllers;

[Authorize]
public class NotificationController : Controller
{
    private readonly ApplicationDbContext _db;

    public NotificationController(ApplicationDbContext db)
    {
        _db = db;
    }

    private string GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    private async Task<UserProfile?> GetUserProfileAsync(string userId) =>
        await _db.Set<UserProfile>().FirstOrDefaultAsync(p => p.UserId == userId);

    // GET /Notification/Notifications
    public async Task<IActionResult> Notifications()
    {
        var profile = await GetUserProfileAsync(GetUserId());
        if (profile == null)
            return RedirectToAction("Index", "Playgroup");

        var notifications = await _db.Set<Notification>()
            .Where(n => n.UserProfileId == profile.Id)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();

        return View(notifications);
    }

    // POST /Notification/MarkAllNotificationsRead
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllNotificationsRead()
    {
        var profile = await GetUserProfileAsync(GetUserId());
        if (profile == null)
            return RedirectToAction("Index", "Playgroup");

        var unread = await _db.Set<Notification>()
            .Where(n => n.UserProfileId == profile.Id && !n.IsRead)
            .ToListAsync();

        foreach (var n in unread)
        {
            n.IsRead = true;
            n.ReadAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        return RedirectToAction("Notifications");
    }

    // POST /Notification/MarkNotificationRead/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkNotificationRead(int id)
    {
        var profile = await GetUserProfileAsync(GetUserId());
        if (profile == null)
            return RedirectToAction("Notifications");

        var notification = await _db.Set<Notification>()
            .FirstOrDefaultAsync(n => n.Id == id && n.UserProfileId == profile.Id);

        if (notification != null && !notification.IsRead)
        {
            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        return RedirectToAction("Notifications");
    }

    // POST /Notification/ViewNotification/5
    // Marks as read then redirects to the notification's ActionUrl
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ViewNotification(int id)
    {
        var profile = await GetUserProfileAsync(GetUserId());
        if (profile == null)
            return RedirectToAction("Notifications");

        var notification = await _db.Set<Notification>()
            .FirstOrDefaultAsync(n => n.Id == id && n.UserProfileId == profile.Id);

        if (notification == null)
            return RedirectToAction("Notifications");

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }

        if (!string.IsNullOrEmpty(notification.ActionUrl) && notification.ActionUrl.StartsWith("/"))
            return Redirect(notification.ActionUrl);

        return RedirectToAction("Notifications");
    }

    // POST /Notification/DeleteNotification/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteNotification(int id)
    {
        var profile = await GetUserProfileAsync(GetUserId());
        if (profile == null)
            return RedirectToAction("Notifications");

        var notification = await _db.Set<Notification>()
            .FirstOrDefaultAsync(n => n.Id == id && n.UserProfileId == profile.Id);

        if (notification != null)
        {
            _db.Set<Notification>().Remove(notification);
            await _db.SaveChangesAsync();
        }

        return RedirectToAction("Notifications");
    }
}
