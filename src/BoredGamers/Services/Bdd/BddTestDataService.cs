using BoredGamers.Data;
using BoredGamers.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BoredGamers.Services.Bdd;

public class BddTestDataService
{
  private const string ReviewTestUserName = "bdd_review_user";
  private const string ReviewTestEmail = "bdd_review_user@local.test";
  private const string ReviewTestPassword = "BddReview123";

  private readonly ApplicationDbContext _db;
  private readonly UserManager<User> _userManager;

  public BddTestDataService(ApplicationDbContext db, UserManager<User> userManager)
  {
    _db = db;
    _userManager = userManager;
  }

  public async Task<BddReviewSeedResult> ResetAndSeedReviewTestDataAsync()
  {
    // 1. Remove any existing seeded review test user and all review data tied to it
    var existingUser = await _db.Users
        .OfType<User>()
        .Include(u => u.Profile)
        .FirstOrDefaultAsync(u => u.UserName == ReviewTestUserName);

    if (existingUser != null)
    {
      var existingReviews = await _db.Reviews
          .Where(r => r.UserId == existingUser.Id)
          .ToListAsync();

      if (existingReviews.Count > 0)
      {
        _db.Reviews.RemoveRange(existingReviews);
      }

      var existingCollections = await _db.UserGameCollections
          .Where(c => c.UserId == existingUser.Id)
          .ToListAsync();

      if (existingCollections.Count > 0)
      {
        _db.UserGameCollections.RemoveRange(existingCollections);
      }

      await _db.SaveChangesAsync();

      await _userManager.DeleteAsync(existingUser);
    }

    // 2. Ensure the two review test games exist
    var createGame = await _db.Games.FirstOrDefaultAsync(g => g.BggGameId == 900001);
    if (createGame == null)
    {
      createGame = new Game
      {
        BggGameId = 900001,
        Name = "BDD Create Review Game",
        YearPublished = 2024,
        Description = "Seeded game used for BDD create and invalid review scenarios.",
        MinPlayers = 2,
        MaxPlayers = 4,
        PlayTime = 60,
        AverageRating = 7.50m,
        BggNumVoters = 100
      };

      _db.Games.Add(createGame);
    }
    else
    {
      createGame.Name = "BDD Create Review Game";
      createGame.Description = "Seeded game used for BDD create and invalid review scenarios.";
    }

    var existingReviewGame = await _db.Games.FirstOrDefaultAsync(g => g.BggGameId == 900002);
    if (existingReviewGame == null)
    {
      existingReviewGame = new Game
      {
        BggGameId = 900002,
        Name = "BDD Existing Review Game",
        YearPublished = 2024,
        Description = "Seeded game used for BDD edit and delete review scenarios.",
        MinPlayers = 2,
        MaxPlayers = 5,
        PlayTime = 90,
        AverageRating = 8.10m,
        BggNumVoters = 150
      };

      _db.Games.Add(existingReviewGame);
    }
    else
    {
      existingReviewGame.Name = "BDD Existing Review Game";
      existingReviewGame.Description = "Seeded game used for BDD edit and delete review scenarios.";
    }

    await _db.SaveChangesAsync();

    // 3. Recreate the seeded BDD user
    var user = new User
    {
      UserName = ReviewTestUserName,
      Email = ReviewTestEmail,
      EmailConfirmed = true
    };

    var createUserResult = await _userManager.CreateAsync(user, ReviewTestPassword);
    if (!createUserResult.Succeeded)
    {
      var errors = string.Join("; ", createUserResult.Errors.Select(e => e.Description));
      throw new InvalidOperationException($"Failed to create seeded BDD user: {errors}");
    }

    // 4. Seed one existing review for edit/delete scenarios
    var seededReview = new Review
    {
      GameId = existingReviewGame.Id,
      UserId = user.Id,
      Rating = 8,
      Text = "BDD seeded review text",
      CreatedAt = DateTime.UtcNow
    };

    _db.Reviews.Add(seededReview);
    await _db.SaveChangesAsync();

    return new BddReviewSeedResult
    {
      Username = ReviewTestUserName,
      Password = ReviewTestPassword,
      CreateGameId = createGame.Id,
      ExistingReviewGameId = existingReviewGame.Id,
      SeededReviewText = seededReview.Text
    };
  }
}

public class BddReviewSeedResult
{
  public string Username { get; set; } = "";
  public string Password { get; set; } = "";
  public int CreateGameId { get; set; }
  public int ExistingReviewGameId { get; set; }
  public string SeededReviewText { get; set; } = "";
}