using System;
using System.Threading;
using System.Threading.Tasks;
using BoredGamers.Services.Collections;
using BoredGamers.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Linq;
using BoredGamers.Models;

namespace BoredGamers.Controllers
{
  [Authorize]
  [Route("collection")]
  public class CollectionController : Controller
  {
    private readonly ApplicationDbContext _db;
    private readonly IUserCollectionService _collections;

    public CollectionController(ApplicationDbContext db, IUserCollectionService collections)
    {
      _db = db;
      _collections = collections;
    }

    private string GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpPost("add")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(int gameId, CancellationToken ct)
    {
      var userId = GetUserId();
      if (string.IsNullOrWhiteSpace(userId))
        return Unauthorized();

      var added = await _collections.AddToCollectionAsync(userId, gameId, ct);

      return RedirectToAction("Details", "GamesPage", new { id = gameId });
    }

    [HttpPost("add-to-wishlist")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddToWishlist(int gameId, CancellationToken ct)
    {
      var userId = GetUserId();
      if (string.IsNullOrWhiteSpace(userId))
        return Unauthorized();

      await _collections.AddToWishlistAsync(userId, gameId, ct);

      return RedirectToAction("Details", "GamesPage", new { id = gameId });
    }

    [HttpPost("remove-from-wishlist")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveFromWishlist(int gameId, CancellationToken ct)
    {
      var userId = GetUserId();
      if (string.IsNullOrWhiteSpace(userId))
        return Unauthorized();

      await _collections.RemoveFromWishlistAsync(userId, gameId, ct);

      return RedirectToAction("Index");
    }
    
    [HttpGet("")]
    public async Task<IActionResult> Index(int page = 1, CancellationToken ct = default)
    {
      var userId = GetUserId();
      if (string.IsNullOrWhiteSpace(userId))
        return Unauthorized();

      const int pageSize = 20;
      if (page < 1) page = 1;

      var ownedQuery = _db.UserGameCollections
          .AsNoTracking()
          .Where(c => c.UserId == userId && c.Status == CollectionStatus.Owned)
          .Include(c => c.Game)
          .OrderByDescending(c => c.DateAdded)
          .Select(c => c.Game);

      var wishlistQuery = _db.UserGameCollections
          .AsNoTracking()
          .Where(c => c.UserId == userId && c.Status == CollectionStatus.Wishlist)
          .Include(c => c.Game)
          .OrderByDescending(c => c.DateAdded)
          .Select(c => c.Game);

      var totalCount = await ownedQuery.CountAsync(ct);
      var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

      var ownedGames = await ownedQuery
          .Skip((page - 1) * pageSize)
          .Take(pageSize)
          .ToListAsync(ct);

      var wishlistGames = await wishlistQuery.ToListAsync(ct);

      ViewData["Page"] = page;
      ViewData["TotalPages"] = totalPages;
      ViewData["TotalCount"] = totalCount;
      ViewData["WishlistGames"] = wishlistGames;

      return View(ownedGames);
    }
  }
}