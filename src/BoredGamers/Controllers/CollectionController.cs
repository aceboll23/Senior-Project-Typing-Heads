using System;
using System.Threading;
using System.Threading.Tasks;
using BoredGamers.Services.Collections;
using BoredGamers.Services.Ai;
using BoredGamers.Services.Transfers;
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
    private readonly IGameTransferService _transfers;

    public CollectionController(
        ApplicationDbContext db,
        IUserCollectionService collections,
        UserManager<User> userManager,
        IAiRecommendationService aiRecommendations,
        IGameTransferService transfers)
    {
      _db = db;
      _collections = collections;
      _userManager = userManager;
      _aiRecommendations = aiRecommendations;
      _transfers = transfers;
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

      var hasOwnedGames = await _db.UserGameCollections
          .AnyAsync(c => c.UserId == currentUser.Id && c.Status == CollectionStatus.Owned, ct);

      if (!hasOwnedGames)
      {
        return Ok(new { message = "Add some games to your collection first to get personalized AI recommendations." });
      }

      var games = await _aiRecommendations.GetRecommendationsAsync(currentUser.Id, ct);

      if (games.Count == 0)
      {
        return Ok(new { message = "The AI didn't return any recommendations this time. Please try again." });
      }

      return Ok(new
      {
          games = games.Select(g => new
          {
              g.Id,
              g.Name,
              g.ImageUrl,
              g.ThumbnailUrl,
              g.BggNumVoters,
              g.YearPublished,
              g.AverageRating
          })
      });
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

      var pendingTransfers = await _transfers.GetPendingTransfersForUserAsync(userId, ct);

      ViewData["Page"] = page;
      ViewData["TotalPages"] = totalPages;
      ViewData["TotalCount"] = totalCount;
      ViewData["WishlistGames"] = wishlistGames;
      ViewData["PendingTransfers"] = pendingTransfers;

      return View(ownedItems);
    }

    // ── Transfer actions ──────────────────────────────────────────────────────

    [HttpGet("transfer/{gameId:int}")]
    public async Task<IActionResult> Transfer(int gameId, CancellationToken ct)
    {
      var userId = GetUserId();
      var entry = await _db.UserGameCollections
          .AsNoTracking()
          .Include(c => c.Game)
          .FirstOrDefaultAsync(c => c.UserId == userId && c.GameId == gameId && c.Status == CollectionStatus.Owned, ct);

      if (entry == null)
      {
        TempData["Error"] = "Game not found in your collection.";
        return RedirectToAction("Index");
      }

      var currentProfile = await _db.Set<UserProfile>().FirstOrDefaultAsync(p => p.UserId == userId, ct);
      if (currentProfile != null)
      {
        var friendships = await _db.Set<Friendship>()
            .Where(f => (f.RequesterProfileId == currentProfile.Id || f.ReceiverProfileId == currentProfile.Id)
                        && f.Status == FriendshipStatus.Accepted)
            .ToListAsync(ct);
        var friendProfileIds = friendships
            .Select(f => f.RequesterProfileId == currentProfile.Id ? f.ReceiverProfileId : f.RequesterProfileId)
            .ToList();
        var friends = await _userManager.Users
            .Include(u => u.Profile)
            .Where(u => u.Profile != null && friendProfileIds.Contains(u.Profile.Id) && !u.IsBanned && !u.IsDeactivated)
            .OrderBy(u => u.UserName)
            .ToListAsync(ct);
        ViewData["Friends"] = friends.Select(u => u.UserName).ToList();
      }
      else
      {
        ViewData["Friends"] = new List<string>();
      }

      return View(entry.Game);
    }

    [HttpPost("transfer/{gameId:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Transfer(int gameId, string toUsername, CancellationToken ct)
    {
      if (string.IsNullOrWhiteSpace(toUsername))
      {
        TempData["Error"] = "Please enter a username.";
        return RedirectToAction("Transfer", new { gameId });
      }
      var receiver = await _userManager.FindByNameAsync(toUsername);
      if (receiver == null)
      {
        TempData["Error"] = $"User '{toUsername}' not found.";
        return RedirectToAction("Transfer", new { gameId });
      }
      return RedirectToAction("TransferConfirm", new { gameId, to = toUsername });
    }

    [HttpGet("transfer/{gameId:int}/confirm")]
    public async Task<IActionResult> TransferConfirm(int gameId, string to, CancellationToken ct)
    {
      var userId = GetUserId();
      var entry = await _db.UserGameCollections
          .AsNoTracking()
          .Include(c => c.Game)
          .FirstOrDefaultAsync(c => c.UserId == userId && c.GameId == gameId && c.Status == CollectionStatus.Owned, ct);

      if (entry == null)
      {
        TempData["Error"] = "Game not found in your collection.";
        return RedirectToAction("Index");
      }
      ViewData["ToUsername"] = to;
      return View(entry.Game);
    }

    [HttpPost("transfer/{gameId:int}/confirm")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ExecuteTransfer(int gameId, string toUsername, CancellationToken ct)
    {
      var userId = GetUserId();
      var result = await _transfers.InitiateTransferAsync(userId, gameId, toUsername, ct);
      if (!result.Success)
      {
        TempData["Error"] = result.Error;
        return RedirectToAction("Index");
      }
      TempData["Success"] = $"Transfer initiated. {toUsername} will be notified to confirm they received the game.";
      return RedirectToAction("Index");
    }

    [HttpPost("transfer/accept/{transferId:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AcceptTransfer(int transferId, CancellationToken ct)
    {
      var userId = GetUserId();
      var result = await _transfers.AcceptTransferAsync(userId, transferId, ct);
      TempData[result.Success ? "Success" : "Error"] = result.Success
          ? "Game added to your collection!"
          : result.Error;
      return RedirectToAction("Index");
    }

    [HttpPost("transfer/decline/{transferId:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeclineTransfer(int transferId, CancellationToken ct)
    {
      var userId = GetUserId();
      var result = await _transfers.DeclineTransferAsync(userId, transferId, ct);
      TempData[result.Success ? "Success" : "Error"] = result.Success
          ? "Transfer declined."
          : result.Error;
      return RedirectToAction("Index");
    }

    // ── End Transfer actions ──────────────────────────────────────────────────

    [HttpGet("all-friend-trades")]
    public async Task<IActionResult> AllFriendTrades(int page = 1, CancellationToken ct = default)
    {
      const int pageSize = 20;
      if (page < 1) page = 1;

      var viewerUserId = GetUserId();
      var (items, totalCount) = await _collections.GetAllFriendsTradeableGamesAsync(viewerUserId, page, pageSize, ct);

      var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
      ViewData["Page"] = page;
      ViewData["TotalPages"] = totalPages;
      ViewData["TotalCount"] = totalCount;

      return View(items);
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