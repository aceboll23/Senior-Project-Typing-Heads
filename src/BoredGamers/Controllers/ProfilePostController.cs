using System.Security.Claims;
using System.Threading;
using BoredGamers.Models;
using BoredGamers.Services.Flags;
using BoredGamers.Services.Posts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BoredGamers.Controllers;

[Authorize]
public class ProfilePostController : Controller
{
    private readonly IProfilePostService _postService;
    private readonly IPostFlagService _flagService;

    public ProfilePostController(IProfilePostService postService, IPostFlagService flagService)
    {
        _postService = postService;
        _flagService = flagService;
    }

    private string GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        string content,
        string returnUsername,
        IFormFile? postImage,
        PostCategory category,
        int? gameId)
    {
        var result = await _postService.CreatePostAsync(GetUserId(), content, postImage, category, gameId);

        if (!result.Success)
            TempData["PostError"] = result.ErrorMessage;

        return RedirectToAction("Index", "Profile", new { username = returnUsername });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int postId, string returnUsername)
    {
        var result = await _postService.DeletePostAsync(postId, GetUserId());

        if (!result.Success)
            TempData["PostError"] = result.ErrorMessage;

        return RedirectToAction("Index", "Profile", new { username = returnUsername });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int postId, string content, string returnUsername)
    {
        var result = await _postService.EditPostAsync(postId, GetUserId(), content);

        if (!result.Success)
            TempData["PostError"] = result.ErrorMessage;

        return RedirectToAction("Index", "Profile", new { username = returnUsername });
    }

    [HttpPost("profile-post/{id}/flag")]
    public async Task<IActionResult> Flag(int id, CancellationToken ct)
    {
        var userId = GetUserId();
        var result = await _flagService.FlagPostAsync(userId, id, ct);

        return Json(new { success = result.Success, message = result.Success ? null : result.ErrorMessage });
    }
}
