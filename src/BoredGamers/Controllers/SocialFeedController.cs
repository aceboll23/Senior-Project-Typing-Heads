using System.Security.Claims;
using BoredGamers.Services.SocialFeed;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BoredGamers.Controllers;

[Authorize]
public class SocialFeedController : Controller
{
    private readonly ISocialFeedService _feedService;

    public SocialFeedController(ISocialFeedService feedService)
    {
        _feedService = feedService;
    }

    private string GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    public async Task<IActionResult> Index()
    {
        var posts = await _feedService.GetFeedForUserAsync(GetUserId());
        return View(posts);
    }
}
