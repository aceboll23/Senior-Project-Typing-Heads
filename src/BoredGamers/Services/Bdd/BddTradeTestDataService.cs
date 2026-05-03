using BoredGamers.Data;
using BoredGamers.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BoredGamers.Services.Bdd;

public class BddTradeResult
{
    public string OwnerUsername { get; set; } = "";
    public string OwnerPassword { get; set; } = "";
    public int GameId { get; set; }
    public string GameName { get; set; } = "";
}

public class BddTradeTestDataService
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<User> _userManager;

    private const string OwnerUsername = "bdd_trade_owner";
    private const string Password = "Test1234!";

    public BddTradeTestDataService(ApplicationDbContext db, UserManager<User> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public async Task<BddTradeResult> ResetWithGameAsync()
    {
        await CleanupAsync();
        var owner = await CreateUserAsync(OwnerUsername);
        var game = await GetOrCreateGameAsync();

        _db.UserGameCollections.Add(new UserGameCollection
        {
            UserId = owner.Id,
            GameId = game.Id,
            DateAdded = DateTime.UtcNow,
            Status = CollectionStatus.Owned,
            IsAvailableForTrade = false
        });
        await _db.SaveChangesAsync();

        return new BddTradeResult
        {
            OwnerUsername = OwnerUsername,
            OwnerPassword = Password,
            GameId = game.Id,
            GameName = game.Name
        };
    }

    public async Task<BddTradeResult> ResetWithTradedGameAsync()
    {
        await CleanupAsync();
        var owner = await CreateUserAsync(OwnerUsername);
        var game = await GetOrCreateGameAsync();

        _db.UserGameCollections.Add(new UserGameCollection
        {
            UserId = owner.Id,
            GameId = game.Id,
            DateAdded = DateTime.UtcNow,
            Status = CollectionStatus.Owned,
            IsAvailableForTrade = true
        });
        await _db.SaveChangesAsync();

        return new BddTradeResult
        {
            OwnerUsername = OwnerUsername,
            OwnerPassword = Password,
            GameId = game.Id,
            GameName = game.Name
        };
    }

    private async Task<Game> GetOrCreateGameAsync()
    {
        var game = await _db.Games.FirstOrDefaultAsync();
        if (game != null)
            return game;

        game = new Game
        {
            BggGameId = 999999,
            Name = "BDD Trade Test Game",
            LastSyncedAt = DateTime.UtcNow
        };
        _db.Games.Add(game);
        await _db.SaveChangesAsync();
        return game;
    }

    private async Task CleanupAsync()
    {
        var user = await _userManager.FindByNameAsync(OwnerUsername);
        if (user == null) return;

        var collections = await _db.UserGameCollections.Where(c => c.UserId == user.Id).ToListAsync();
        _db.UserGameCollections.RemoveRange(collections);
        await _db.SaveChangesAsync();

        await _userManager.DeleteAsync(user);
    }

    private async Task<User> CreateUserAsync(string username)
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
            throw new InvalidOperationException($"Failed to create {username}: {string.Join(", ", result.Errors.Select(e => e.Description))}");

        return user;
    }
}
