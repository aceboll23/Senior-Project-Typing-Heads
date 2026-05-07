using BoredGamers.Data;
using BoredGamers.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BoredGamers.Services.Bdd;

public class BddAllFriendTradesTestDataService
{
    private const string ViewerUserName = "bdd_aft_viewer";
    private const string ViewerEmail = "bdd_aft_viewer@local.test";
    private const string ViewerPassword = "BddAftViewer123!";

    private const string Friend1UserName = "bdd_aft_friend1";
    private const string Friend1Email = "bdd_aft_friend1@local.test";
    private const string Friend1Password = "BddAftFriend1123!";

    private const string Friend2UserName = "bdd_aft_friend2";
    private const string Friend2Email = "bdd_aft_friend2@local.test";
    private const string Friend2Password = "BddAftFriend2123!";

    private const string StrangerUserName = "bdd_aft_stranger";
    private const string StrangerEmail = "bdd_aft_stranger@local.test";
    private const string StrangerPassword = "BddAftStranger123!";

    private readonly ApplicationDbContext _db;
    private readonly UserManager<User> _userManager;

    public BddAllFriendTradesTestDataService(ApplicationDbContext db, UserManager<User> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public async Task<BddAllFriendTradesSeedResult> ResetAndSeedAsync()
    {
        await CleanupAsync();

        var friend1TradeGame = await UpsertGameAsync(900070, "BDD AFT Friend1 Trade Game");
        var friend2TradeGame = await UpsertGameAsync(900071, "BDD AFT Friend2 Trade Game");
        var noTradeGame = await UpsertGameAsync(900072, "BDD AFT No Trade Game");

        var viewer = await CreateUserAsync(ViewerUserName, ViewerEmail, ViewerPassword);
        var friend1 = await CreateUserAsync(Friend1UserName, Friend1Email, Friend1Password);
        var friend2 = await CreateUserAsync(Friend2UserName, Friend2Email, Friend2Password);
        var stranger = await CreateUserAsync(StrangerUserName, StrangerEmail, StrangerPassword);

        var viewerProfile = new UserProfile { UserId = viewer.Id, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var friend1Profile = new UserProfile { UserId = friend1.Id, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var friend2Profile = new UserProfile { UserId = friend2.Id, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var strangerProfile = new UserProfile { UserId = stranger.Id, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };

        _db.Set<UserProfile>().AddRange(viewerProfile, friend1Profile, friend2Profile, strangerProfile);
        await _db.SaveChangesAsync();

        _db.Set<Friendship>().AddRange(
            new Friendship
            {
                RequesterProfileId = viewerProfile.Id,
                ReceiverProfileId = friend1Profile.Id,
                Status = FriendshipStatus.Accepted,
                RequestedAt = DateTime.UtcNow,
                RespondedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Friendship
            {
                RequesterProfileId = viewerProfile.Id,
                ReceiverProfileId = friend2Profile.Id,
                Status = FriendshipStatus.Accepted,
                RequestedAt = DateTime.UtcNow,
                RespondedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        );

        _db.UserGameCollections.AddRange(
            new UserGameCollection { UserId = friend1.Id, GameId = friend1TradeGame.Id, DateAdded = DateTime.UtcNow, Status = CollectionStatus.Owned, IsAvailableForTrade = true },
            new UserGameCollection { UserId = friend2.Id, GameId = friend2TradeGame.Id, DateAdded = DateTime.UtcNow.AddMinutes(-5), Status = CollectionStatus.Owned, IsAvailableForTrade = true },
            new UserGameCollection { UserId = friend1.Id, GameId = noTradeGame.Id, DateAdded = DateTime.UtcNow, Status = CollectionStatus.Owned, IsAvailableForTrade = false }
        );

        await _db.SaveChangesAsync();

        return new BddAllFriendTradesSeedResult
        {
            ViewerUsername = ViewerUserName,
            ViewerPassword = ViewerPassword,
            Friend1Username = Friend1UserName,
            Friend2Username = Friend2UserName,
            Friend1TradeGameName = friend1TradeGame.Name,
            Friend2TradeGameName = friend2TradeGame.Name,
            NoTradeGameName = noTradeGame.Name,
            StrangerUsername = StrangerUserName
        };
    }

    private async Task CleanupAsync()
    {
        foreach (var username in new[] { ViewerUserName, Friend1UserName, Friend2UserName, StrangerUserName })
        {
            var existing = await _db.Users.OfType<User>().FirstOrDefaultAsync(u => u.UserName == username);
            if (existing == null) continue;

            var collections = await _db.UserGameCollections.Where(c => c.UserId == existing.Id).ToListAsync();
            _db.UserGameCollections.RemoveRange(collections);

            var profile = await _db.Set<UserProfile>().FirstOrDefaultAsync(p => p.UserId == existing.Id);
            if (profile != null)
            {
                var friendships = await _db.Set<Friendship>()
                    .Where(f => f.RequesterProfileId == profile.Id || f.ReceiverProfileId == profile.Id)
                    .ToListAsync();
                _db.Set<Friendship>().RemoveRange(friendships);
                await _db.SaveChangesAsync();

                _db.Set<UserProfile>().Remove(profile);
                await _db.SaveChangesAsync();
            }
            else
            {
                await _db.SaveChangesAsync();
            }

            await _userManager.DeleteAsync(existing);
        }
    }

    private async Task<Game> UpsertGameAsync(int bggId, string name)
    {
        var game = await _db.Games.FirstOrDefaultAsync(g => g.BggGameId == bggId);
        if (game == null)
        {
            game = new Game { BggGameId = bggId, Name = name, YearPublished = 2024, AverageRating = 7.0m, BggNumVoters = 50, LastSyncedAt = DateTime.UtcNow };
            _db.Games.Add(game);
        }
        else
        {
            game.Name = name;
        }
        await _db.SaveChangesAsync();
        return game;
    }

    private async Task<User> CreateUserAsync(string username, string email, string password)
    {
        var user = new User { UserName = username, Email = email, EmailConfirmed = true };
        var result = await _userManager.CreateAsync(user, password);
        if (!result.Succeeded)
            throw new InvalidOperationException($"Failed to create {username}: {string.Join("; ", result.Errors.Select(e => e.Description))}");
        return user;
    }
}

public class BddAllFriendTradesSeedResult
{
    public string ViewerUsername { get; set; } = "";
    public string ViewerPassword { get; set; } = "";
    public string Friend1Username { get; set; } = "";
    public string Friend2Username { get; set; } = "";
    public string Friend1TradeGameName { get; set; } = "";
    public string Friend2TradeGameName { get; set; } = "";
    public string NoTradeGameName { get; set; } = "";
    public string StrangerUsername { get; set; } = "";
}
