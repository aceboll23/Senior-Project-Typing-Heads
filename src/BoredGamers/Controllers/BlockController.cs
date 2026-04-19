using BoredGamers.Services.Block;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using BoredGamers.Models;

namespace BoredGamers.Controllers;

[Authorize]
public class BlockController : Controller
{
    private readonly IBlockService _blockService;
    private readonly UserManager<User> _userManager;

    public BlockController(IBlockService blockService, UserManager<User> userManager)
    {
        _blockService = blockService;
        _userManager = userManager;
    }

    // POST /Block/Block
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Block(string targetUsername)
    {
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser == null) return Unauthorized();

        var result = await _blockService.BlockUserAsync(currentUser.Id, targetUsername);

        if (result.Success)
            TempData["BlockSuccess"] = $"{targetUsername} has been blocked.";
        else
            TempData["BlockError"] = result.ErrorMessage;

        return RedirectToAction("Index", "Profile", new { username = targetUsername });
    }

    // POST /Block/Unblock
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Unblock(string targetUsername)
    {
        var currentUser = await _userManager.GetUserAsync(User);
        if (currentUser == null) return Unauthorized();

        await _blockService.UnblockUserAsync(currentUser.Id, targetUsername);

        return RedirectToAction("BlockedUsers", "Settings");
    }
}
