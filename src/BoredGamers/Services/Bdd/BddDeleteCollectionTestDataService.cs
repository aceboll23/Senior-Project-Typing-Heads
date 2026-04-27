using BoredGamers.Data;
using BoredGamers.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BoredGamers.Services.Bdd;

public class BddDeleteCollectionTestDataService
{
    private const string TestUserName = "bdd_delete_collection_user";
    private const string TestEmail = "bdd_delete_collection_user@local.test";
    private const string TestPassword = "BddDeleteCollection123!";
    private const int OwnedGameBggId = 900020;
    private const string OwnedGameName = "BDD Delete Collection Game";

    private readonly ApplicationDbContext _db;
    private readonly UserManager<User> _userManager;

    public BddDeleteCollectionTestDataService(ApplicationDbContext db, UserManager<User> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public async Task<BddDeleteCollectionSeedResult> ResetAndSeedDeleteCollectionTestDataAsync()
    {
        // 1. Remove existing BDD test user and their collection data
        var existingUser = await _db.Users
            .OfType<User>()
            .FirstOrDefaultAsync(u => u.UserName == TestUserName);

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

        // 2. Ensure the seeded game exists
        var ownedGame = await _db.Games.FirstOrDefaultAsync(g => g.BggGameId == OwnedGameBggId);
        if (ownedGame == null)
        {
            ownedGame = new Game
            {
                BggGameId = OwnedGameBggId,
                Name = OwnedGameName,
                YearPublished = 2024,
                Description = "Seeded game for BDD delete-from-collection scenario.",
                MinPlayers = 2,
                MaxPlayers = 4,
                PlayTime = 60,
                AverageRating = 7.50m,
                BggNumVoters = 100
            };
            _db.Games.Add(ownedGame);
        }
        else
        {
            ownedGame.Name = OwnedGameName;
        }

        await _db.SaveChangesAsync();

        // 3. Create fresh BDD test user
        var user = new User
        {
            UserName = TestUserName,
            Email = TestEmail,
            EmailConfirmed = true
        };

        var createUserResult = await _userManager.CreateAsync(user, TestPassword);
        if (!createUserResult.Succeeded)
        {
            var errors = string.Join("; ", createUserResult.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to create seeded BDD delete-collection user: {errors}");
        }

        // 4. Add the seeded game to the user's owned collection
        _db.UserGameCollections.Add(new UserGameCollection
        {
            UserId = user.Id,
            GameId = ownedGame.Id,
            DateAdded = DateTime.UtcNow,
            Status = CollectionStatus.Owned
        });
        await _db.SaveChangesAsync();

        return new BddDeleteCollectionSeedResult
        {
            Username = TestUserName,
            Password = TestPassword,
            OwnedGameId = ownedGame.Id,
            OwnedGameName = ownedGame.Name
        };
    }
}
