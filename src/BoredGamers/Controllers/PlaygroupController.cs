using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using BoredGamers.Data;
using BoredGamers.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BoredGamers.Controllers;

[Authorize]
public class PlaygroupController : Controller
{
    private readonly ApplicationDbContext _db;

    public PlaygroupController(ApplicationDbContext db)
    {
        _db = db;
    }

    private string GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    // GET /Playgroup — "My Playgroups" list
    public async Task<IActionResult> Index()
    {
        var userId = GetUserId();

        var myGroups = await _db.PlaygroupMembers
            .Where(m => m.UserId == userId)
            .Include(m => m.Playgroup)
            .Select(m => m.Playgroup)
            .OrderByDescending(g => g.CreatedAt)
            .ToListAsync();

        return View(myGroups);
    }

    // GET /Playgroup/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST /Playgroup/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string name, string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            ModelState.AddModelError("Name", "Playgroup name is required.");
            return View();
        }

        if (name.Length > 100)
        {
            ModelState.AddModelError("Name", "Playgroup name cannot exceed 100 characters.");
            return View();
        }

        var userId = GetUserId();

        var playgroup = new Playgroup
        {
            Name = name.Trim(),
            Description = description?.Trim(),
            CreatedByUserId = userId,
            IsPrivate = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Playgroups.Add(playgroup);
        await _db.SaveChangesAsync();

        var membership = new PlaygroupMember
        {
            PlaygroupId = playgroup.Id,
            UserId = userId,
            Role = PlaygroupRole.Owner,
            JoinedAt = DateTime.UtcNow
        };

        _db.PlaygroupMembers.Add(membership);
        await _db.SaveChangesAsync();

        return RedirectToAction("Details", new { id = playgroup.Id });
    }

    // GET /Playgroup/Details/5
    public async Task<IActionResult> Details(int id, string? status)
    {
        var userId = GetUserId();

        var playgroup = await _db.Playgroups
            .Include(g => g.Members)
            .FirstOrDefaultAsync(g => g.Id == id);

        if (playgroup == null)
            return NotFound();

        var isMember = playgroup.Members.Any(m => m.UserId == userId);
        if (playgroup.IsPrivate && !isMember)
            return NotFound();

        ViewData["IsMember"] = isMember;
        ViewData["IsOwner"] = playgroup.Members.Any(m => m.UserId == userId && m.Role == PlaygroupRole.Owner);
        ViewData["MemberCount"] = playgroup.Members.Count;
        ViewData["Status"] = status;

        return View(playgroup);
    }
}
