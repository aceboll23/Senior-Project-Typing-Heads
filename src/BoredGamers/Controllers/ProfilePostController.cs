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
    private readonly IPostReplyService _replyService;
    private readonly IPostLikeService _likeService;

    public ProfilePostController(IProfilePostService postService, IPostFlagService flagService, IPostReplyService replyService, IPostLikeService likeService)
    {
        _postService = postService;
        _flagService = flagService;
        _replyService = replyService;
        _likeService = likeService;
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reply(int postId, string content, string? returnUsername, string? returnFeed, CancellationToken ct)
    {
        var result = await _replyService.CreateReplyAsync(postId, GetUserId(), content, ct);

        if (!result.Success)
            TempData["ReplyError"] = result.ErrorMessage;

        if (!string.IsNullOrEmpty(returnUsername))
            return RedirectToAction("Index", "Profile", new { username = returnUsername });

        return RedirectToAction("Index", "SocialFeed");
    }

    [HttpPost]
    public async Task<IActionResult> Like(int postId, CancellationToken ct)
    {
        var (isLiked, likeCount) = await _likeService.ToggleLikeAsync(postId, GetUserId(), ct);
        return Json(new { isLiked, likeCount });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditReply(int replyId, string content, string? returnUsername, CancellationToken ct)
    {
        var result = await _replyService.EditReplyAsync(replyId, GetUserId(), content, ct);

        if (!result.Success)
            TempData["ReplyError"] = result.ErrorMessage;

        if (!string.IsNullOrEmpty(returnUsername))
            return RedirectToAction("Index", "Profile", new { username = returnUsername });

        return RedirectToAction("Index", "SocialFeed");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteReply(int replyId, string? returnUsername, CancellationToken ct)
    {
        var result = await _replyService.DeleteReplyAsync(replyId, GetUserId(), ct);

        if (!result.Success)
            TempData["ReplyError"] = result.ErrorMessage;

        if (!string.IsNullOrEmpty(returnUsername))
            return RedirectToAction("Index", "Profile", new { username = returnUsername });

        return RedirectToAction("Index", "SocialFeed");
    }

    [HttpPost("profile-post/{id}/flag")]
    public async Task<IActionResult> Flag(int id, CancellationToken ct)
    {
        var userId = GetUserId();
        var result = await _flagService.FlagPostAsync(userId, id, ct);

        return Json(new { success = result.Success, message = result.Success ? null : result.ErrorMessage });
    }
}
