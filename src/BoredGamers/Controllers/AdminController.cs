using System.Threading;
using System.Threading.Tasks;
using BoredGamers.Data;
using BoredGamers.Services.Ai;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BoredGamers.Controllers;

[Authorize]
public class AdminController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<Models.User> _userManager;

    public AdminController(ApplicationDbContext db, UserManager<Models.User> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    [HttpGet("admin/reports")]
    public async Task<IActionResult> Reports(CancellationToken ct)
    {
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser == null)
            return Unauthorized();

        if (!AiAccessPolicy.IsAllowed(currentUser.UserName))
            return Forbid();

        var reports = await _db.ProfilePostFlags
            .Include(f => f.ProfilePost)
            .OrderByDescending(f => f.ReportedAt)
            .Select(f => new
            {
                f.Id,
                PostContent = f.ProfilePost.Content,
                ReporterId = f.ReporterId,
                f.ReportedAt,
                f.ProfilePostId
            })
            .ToListAsync(ct);

        return Ok(reports);
    }
}
