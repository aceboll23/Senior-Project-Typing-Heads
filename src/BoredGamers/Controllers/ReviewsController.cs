using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BoredGamers.Services;

namespace BoredGamers.Controllers
{
  [Authorize]
  public class ReviewsController : Controller
  {
    private readonly ReviewService _reviewService;

    public ReviewsController(ReviewService reviewService)
    {
      _reviewService = reviewService;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int gameId, int rating, string text)
    {
      var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
      if (string.IsNullOrWhiteSpace(userId))
        return Forbid();

      var result = await _reviewService.CreateReviewAsync(userId, gameId, rating, text);

      if (!result.Success)
      {
        TempData["ReviewError"] = result.ErrorMessage;
        return RedirectToAction("Details", "Games", new { id = gameId });
      }

      TempData["ReviewSuccess"] = "Review submitted!";
      return RedirectToAction("Details", "Games", new { id = gameId });
    }
  }
}