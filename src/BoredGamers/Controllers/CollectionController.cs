using System;
using System.Threading;
using System.Threading.Tasks;
using BoredGamers.Services.Collections;
using BoredGamers.Services.Ai;
using BoredGamers.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Linq;
using BoredGamers.Models;
using BoredGamers.Models.ViewModels;

namespace BoredGamers.Controllers
{
  [Authorize]
  [Route("collection")]
  public class CollectionController : Controller
  {
    private readonly ApplicationDbContext _db;
    private readonly IUserCollectionService _collections;
    private readonly UserManager<User> _userManager;
    private readonly IAiRecommendationService _aiRecommendations;

    public CollectionController(
        ApplicationDbContext db,
        IUserCollectionService collections,
        UserManager<User> userManager,
        IAiRecommendationService aiRecommendations)
    {
      _db = db;
      _collections = collections;
      _userManager = userManager;
      _aiRecommendations = aiRecommendations;
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

    [HttpPost("toggle-trade")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleTrade(int gameId, CancellationToken ct)
    {
      var userId = GetUserId();
      var result = await _collections.ToggleTradeStatusAsync(userId, gameId, ct);
      if (result == null)
        return Forbid();
      return RedirectToAction("Index");
    }

    [HttpPost("remove-from-collection")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveFromCollection(int gameId, CancellationToken ct)
    {
      var userId = GetUserId();
      if (string.IsNullOrWhiteSpace(userId))
        return Unauthorized();

      await _collections.RemoveFromCollectionAsync(userId, gameId, ct);

      return RedirectToAction("Index");
    }

    // POST /collection/ai-recommendations
    // Returns JSON: either { message: "..." } when there's nothing to display
    // or { games: [ {Id, Name, ImageUrl, ThumbnailUrl, BggNumVoters, YearPublished, AverageRating}, ... ] }
    // Gated by AiAccessPolicy because each call costs real money against the
    // shared Anthropic API key.
    [HttpPost("ai-recommendations")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AiRecommendations(CancellationToken ct)
    {
      var currentUser = await _userManager.GetUserAsync(User);
      if (currentUser == null)
        return Unauthorized();

      if (!AiAccessPolicy.IsAllowed(currentUser.UserName))
        return Forbid();

      var ownedGameNames = await _db.UserGameCollections
          .Where(c => c.UserId == currentUser.Id && c.Status == CollectionStatus.Owned)
          .Include(c => c.Game)
          .Select(c => c.Game.Name)
          .ToListAsync(ct);

      if (ownedGameNames.Count == 0)
      {
        return Ok(new { message = "Add some games to your collection first to get personalized AI recommendations." });
      }

      var recommendedNames = await _aiRecommendations.GetRecommendationsAsync(ownedGameNames, ct);

      if (recommendedNames.Count == 0)
      {
        return Ok(new { message = "The AI didn't return any recommendations this time. Please try again." });
      }

      // Match recommended names against the local Games table. Default SQL Server
      // collation is case-insensitive, so a simple Contains lookup is enough for
      // the minimal version. Recommendations Claude makes that aren't in our
      // library are silently dropped here.
      var matchedGames = await _db.Games
          .Where(g => recommendedNames.Contains(g.Name))
          .Select(g => new
          {
              g.Id,
              g.Name,
              g.ImageUrl,
              g.ThumbnailUrl,
              g.BggNumVoters,
              g.YearPublished,
              g.AverageRating
          })
          .ToListAsync(ct);

      if (matchedGames.Count == 0)
      {
        return Ok(new { message = "The AI suggested games we don't have in our library yet. Try again — you may get a different set." });
      }

      return Ok(new { games = matchedGames });
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(int page = 1, CancellationToken ct = default)
    {
      var userId = GetUserId();
      if (string.IsNullOrWhiteSpace(userId))
        return Unauthorized();

      const int pageSize = 20;
      if (page < 1) page = 1;

      var ownedBaseQuery = _db.UserGameCollections
          .AsNoTracking()
          .Where(c => c.UserId == userId && c.Status == CollectionStatus.Owned);

      var totalCount = await ownedBaseQuery.CountAsync(ct);
      var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

      var ownedItems = await ownedBaseQuery
          .Include(c => c.Game)
          .OrderByDescending(c => c.DateAdded)
          .Skip((page - 1) * pageSize)
          .Take(pageSize)
          .Select(c => new CollectionItemViewModel { Game = c.Game, IsAvailableForTrade = c.IsAvailableForTrade })
          .ToListAsync(ct);

      var wishlistGames = await _db.UserGameCollections
          .AsNoTracking()
          .Where(c => c.UserId == userId && c.Status == CollectionStatus.Wishlist)
          .Include(c => c.Game)
          .OrderByDescending(c => c.DateAdded)
          .Select(c => c.Game)
          .ToListAsync(ct);

      ViewData["Page"] = page;
      ViewData["TotalPages"] = totalPages;
      ViewData["TotalCount"] = totalCount;
      ViewData["WishlistGames"] = wishlistGames;

      return View(ownedItems);
    }

    [HttpGet("trades/{username}")]
    public async Task<IActionResult> FriendTrades(string username, CancellationToken ct)
    {
      var viewerUserId = GetUserId();
      var games = await _collections.GetFriendTradeableGamesAsync(viewerUserId, username, ct);
      if (games == null)
        return Forbid();

      ViewData["FriendUsername"] = username;
      return View(games);
    }

    [HttpGet("{username}")]
    public async Task<IActionResult> FriendCollection(string username, int page = 1, CancellationToken ct = default)
    {
      var currentUserId = GetUserId();

      var targetUser = await _userManager.FindByNameAsync(username);
      if (targetUser == null)
        return NotFound();

      if (targetUser.Id == currentUserId)
        return RedirectToAction("Index");

      const int pageSize = 20;
      if (page < 1) page = 1;

      var ownedQuery = _db.UserGameCollections
          .AsNoTracking()
          .Where(c => c.UserId == targetUser.Id && c.Status == CollectionStatus.Owned)
          .Include(c => c.Game)
          .OrderByDescending(c => c.DateAdded)
          .Select(c => c.Game);

      var totalCount = await ownedQuery.CountAsync(ct);
      var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

      var ownedGames = await ownedQuery
          .Skip((page - 1) * pageSize)
          .Take(pageSize)
          .ToListAsync(ct);

      ViewData["FriendUsername"] = username;
      ViewData["Page"] = page;
      ViewData["TotalPages"] = totalPages;
      ViewData["TotalCount"] = totalCount;

      return View(ownedGames);
    }
  }
}