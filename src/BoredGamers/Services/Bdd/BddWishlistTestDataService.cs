using BoredGamers.Data;
using BoredGamers.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BoredGamers.Services.Bdd;

public class BddWishlistTestDataService
{
  private const string WishlistTestUserName = "bdd_wishlist_user";
  private const string WishlistTestEmail = "bdd_wishlist_user@local.test";
  private const string WishlistTestPassword = "BddWishlist123!";

  private readonly ApplicationDbContext _db;
  private readonly UserManager<User> _userManager;

  public BddWishlistTestDataService(ApplicationDbContext db, UserManager<User> userManager)
  {
    _db = db;
    _userManager = userManager;
  }

  public async Task<BddWishlistSeedResult> ResetAndSeedWishlistTestDataAsync()
  {
    // 1. Remove existing BDD wishlist test user and their collection data
    var existingUser = await _db.Users
        .OfType<User>()
        .FirstOrDefaultAsync(u => u.UserName == WishlistTestUserName);

    if (existingUser != null)
    {
      var existingCollections = await _db.UserGameCollections
          .Where(c => c.UserId == existingUser.Id)
          .ToListAsync();

      if (existingCollections.Count > 0)
      {
        _db.UserGameCollections.RemoveRange(existingCollections);
        await _db.SaveChangesAsync();
      }

      await _userManager.DeleteAsync(existingUser);
    }

    // 2. Ensure the two wishlist test games exist
    var gameNotOnWishlist = await _db.Games.FirstOrDefaultAsync(g => g.BggGameId == 900010);
    if (gameNotOnWishlist == null)
    {
      gameNotOnWishlist = new Game
      {
        BggGameId = 900010,
        Name = "BDD Wishlist Add Game",
        YearPublished = 2024,
        Description = "Seeded game for BDD add-to-wishlist scenario.",
        MinPlayers = 2,
        MaxPlayers = 4,
        PlayTime = 60,
        AverageRating = 7.50m,
        BggNumVoters = 100
      };
      _db.Games.Add(gameNotOnWishlist);
    }
    else
    {
      gameNotOnWishlist.Name = "BDD Wishlist Add Game";
    }

    var gameAlreadyOnWishlist = await _db.Games.FirstOrDefaultAsync(g => g.BggGameId == 900011);
    if (gameAlreadyOnWishlist == null)
    {
      gameAlreadyOnWishlist = new Game
      {
        BggGameId = 900011,
        Name = "BDD Wishlist Duplicate Game",
        YearPublished = 2024,
        Description = "Seeded game for BDD duplicate-add scenario.",
        MinPlayers = 2,
        MaxPlayers = 5,
        PlayTime = 90,
        AverageRating = 8.10m,
        BggNumVoters = 150
      };
      _db.Games.Add(gameAlreadyOnWishlist);
    }
    else
    {
      gameAlreadyOnWishlist.Name = "BDD Wishlist Duplicate Game";
    }

    await _db.SaveChangesAsync();

    // 3. Create fresh BDD wishlist test user
    var user = new User
    {
      UserName = WishlistTestUserName,
      Email = WishlistTestEmail,
      EmailConfirmed = true
    };

    var createUserResult = await _userManager.CreateAsync(user, WishlistTestPassword);
    if (!createUserResult.Succeeded)
    {
      var errors = string.Join("; ", createUserResult.Errors.Select(e => e.Description));
      throw new InvalidOperationException($"Failed to create seeded BDD wishlist user: {errors}");
    }

    // 4. Pre-add one game to the user's wishlist for the duplicate scenario
    var preAddedEntry = new UserGameCollection
    {
      UserId = user.Id,
      GameId = gameAlreadyOnWishlist.Id,
      DateAdded = DateTime.UtcNow,
      Status = CollectionStatus.Wishlist
    };

    _db.UserGameCollections.Add(preAddedEntry);
    await _db.SaveChangesAsync();

    return new BddWishlistSeedResult
    {
      Username = WishlistTestUserName,
      Password = WishlistTestPassword,
      GameNotOnWishlistId = gameNotOnWishlist.Id,
      GameAlreadyOnWishlistId = gameAlreadyOnWishlist.Id,
      GameNotOnWishlistName = gameNotOnWishlist.Name,
      GameAlreadyOnWishlistName = gameAlreadyOnWishlist.Name
    };
  }
}