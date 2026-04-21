using BoredGamers.Data;
using BoredGamers.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BoredGamers.Services.Bdd;

public class BddAverageRatingTestDataService
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<User> _userManager;

    private const string ViewerUsername = "bdd_avg_rating_viewer";
    private const string ReviewerOneUsername = "bdd_avg_rating_reviewer_one";
    private const string ReviewerTwoUsername = "bdd_avg_rating_reviewer_two";
    private const string Password = "BddAvgRating123!";
    private const string GameName = "BDD Average Rating Test Game";
    private const int BggGameId = 987654;

    public BddAverageRatingTestDataService(ApplicationDbContext db, UserManager<User> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public async Task<BddAverageRatingSeedResult> ResetAndSeedAverageRatingTestDataAsync()
    {
        await CleanupExistingDataAsync();

        var viewer = await CreateUserWithProfileAsync(ViewerUsername);
        var reviewerOne = await CreateUserWithProfileAsync(ReviewerOneUsername);
        var reviewerTwo = await CreateUserWithProfileAsync(ReviewerTwoUsername);

        var game = new Game
        {
            BggGameId = BggGameId,
            Name = GameName,
            LastSyncedAt = DateTime.UtcNow
        };
        _db.Games.Add(game);
        await _db.SaveChangesAsync();

        _db.Reviews.AddRange(
            new Review { GameId = game.Id, UserId = reviewerOne.Id, Rating = 4, Text = "Not for me.", CreatedAt = DateTime.UtcNow },
            new Review { GameId = game.Id, UserId = reviewerTwo.Id, Rating = 8, Text = "Really enjoyed it.", CreatedAt = DateTime.UtcNow }
        );
        await _db.SaveChangesAsync();

        return new BddAverageRatingSeedResult
        {
            Username = ViewerUsername,
            Password = Password,
            GameId = game.Id,
            ExpectedAverage = 6.0m
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

        foreach (var username in new[] { ViewerUsername, ReviewerOneUsername, ReviewerTwoUsername })
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
