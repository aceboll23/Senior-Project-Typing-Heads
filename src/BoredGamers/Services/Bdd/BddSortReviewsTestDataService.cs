using BoredGamers.Data;
using BoredGamers.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BoredGamers.Services.Bdd;

public class BddSortReviewsTestDataService
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<User> _userManager;

    private const string ViewerUsername = "bdd_sort_reviews_viewer";
    private const string ReviewerLowUsername = "bdd_sort_reviews_reviewer_low";
    private const string ReviewerHighUsername = "bdd_sort_reviews_reviewer_high";
    private const string ReviewerMidUsername = "bdd_sort_reviews_reviewer_mid";
    private const string Password = "BddSortReviews123!";
    private const string GameName = "BDD Sort Reviews Test Game";
    private const int BggGameId = 987655;

    public BddSortReviewsTestDataService(ApplicationDbContext db, UserManager<User> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public async Task<BddSortReviewsSeedResult> ResetAndSeedSortReviewsTestDataAsync()
    {
        await CleanupExistingDataAsync();

        var viewer = await CreateUserWithProfileAsync(ViewerUsername);
        var reviewerLow = await CreateUserWithProfileAsync(ReviewerLowUsername);
        var reviewerHigh = await CreateUserWithProfileAsync(ReviewerHighUsername);
        var reviewerMid = await CreateUserWithProfileAsync(ReviewerMidUsername);

        var game = new Game
        {
            BggGameId = BggGameId,
            Name = GameName,
            LastSyncedAt = DateTime.UtcNow
        };
        _db.Games.Add(game);
        await _db.SaveChangesAsync();

        _db.Reviews.AddRange(
            new Review { GameId = game.Id, UserId = reviewerLow.Id, Rating = 2, Text = "Not a fan.", CreatedAt = DateTime.UtcNow },
            new Review { GameId = game.Id, UserId = reviewerHigh.Id, Rating = 9, Text = "Loved it.", CreatedAt = DateTime.UtcNow },
            new Review { GameId = game.Id, UserId = reviewerMid.Id, Rating = 5, Text = "It was okay.", CreatedAt = DateTime.UtcNow }
        );
        await _db.SaveChangesAsync();

        return new BddSortReviewsSeedResult
        {
            Username = ViewerUsername,
            Password = Password,
            GameId = game.Id
        };
    }

    private async Task CleanupExistingDataAsync()
    {
        var existingGames = await _db.Games.Where(g => g.BggGameId == BggGameId).ToListAsync();
        foreach (var existingGame in existingGames)
        {
            var reviews = await _db.Reviews.Where(r => r.GameId == existingGame.Id).ToListAsync();
            _db.Reviews.RemoveRange(reviews);
            _db.Games.Remove(existingGame);
        }
        await _db.SaveChangesAsync();

        foreach (var username in new[] { ViewerUsername, ReviewerLowUsername, ReviewerHighUsername, ReviewerMidUsername })
        {
            var user = await _userManager.FindByNameAsync(username);
            if (user == null) continue;

            var profile = await _db.Set<UserProfile>().FirstOrDefaultAsync(p => p.UserId == user.Id);
            if (profile != null)
            {
                _db.Set<UserProfile>().Remove(profile);
                await _db.SaveChangesAsync();
            }

            await _userManager.DeleteAsync(user);
        }
    }

    private async Task<User> CreateUserWithProfileAsync(string username)
    {
        var user = new User
        {
            UserName = username,
            Email = $"{username}@bddtest.com",
            EmailConfirmed = true,
            UpdatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, Password);
        if (!result.Succeeded)
            throw new InvalidOperationException($"Failed to create user {username}: {string.Join(", ", result.Errors.Select(e => e.Description))}");

        var profile = new UserProfile
        {
            UserId = user.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.Set<UserProfile>().Add(profile);
        await _db.SaveChangesAsync();

        return user;
    }
}
