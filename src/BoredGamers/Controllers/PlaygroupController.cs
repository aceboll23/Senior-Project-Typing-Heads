using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using BoredGamers.Data;
using BoredGamers.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
 namespace BoredGamers.Controllers;

[Authorize]
public class PlaygroupController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<User> _userManager;

    public PlaygroupController(ApplicationDbContext db, UserManager<User> userManager)
    {
        _db = db;
        _userManager = userManager;
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

        //private group still returns not found
        if (playgroup.IsPrivate && !playgroup.IsMember(userId))
            return NotFound();

        ViewData["UserId"] = userId;
        ViewData["Status"] = status;

         // Build a dictionary of UserId → UserName for display
        var memberIds = playgroup.Members.Select(m => m.UserId).ToList();
        var users = await _userManager.Users
            .Where(u => memberIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.UserName ?? "Unknown");
        ViewData["MemberNames"] = users;
        
        return View(playgroup);
    }
}
