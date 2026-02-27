using System.Threading;
using System.Threading.Tasks;
using BoredGamers.Services.Collections;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BoredGamers.Controllers
{
  [Authorize]
  [Route("collection")]
  public class CollectionController : Controller
  {
    private readonly IUserCollectionService _collections;

    public CollectionController(IUserCollectionService collections)
    {
      _collections = collections;
    }

    [HttpPost("add")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(int gameId, CancellationToken ct)
    {
      var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
      if (string.IsNullOrWhiteSpace(userId))
        return Unauthorized();

      var added = await _collections.AddToCollectionAsync(userId, gameId, ct);

      return RedirectToAction("Details", "GamesPage", new { id = gameId });
    }
  }
}