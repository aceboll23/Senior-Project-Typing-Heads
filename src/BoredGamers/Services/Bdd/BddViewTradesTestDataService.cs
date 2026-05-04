using BoredGamers.Data;
using BoredGamers.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BoredGamers.Services.Bdd;

public class BddViewTradesTestDataService
{
    private const string ViewerUserName = "bdd_vt_viewer";
    private const string ViewerEmail = "bdd_vt_viewer@local.test";
    private const string ViewerPassword = "BddVtViewer123!";

    private const string FriendUserName = "bdd_vt_friend";
    private const string FriendEmail = "bdd_vt_friend@local.test";
    private const string FriendPassword = "BddVtFriend123!";

    private const string EmptyFriendUserName = "bdd_vt_empty_friend";
    private const string EmptyFriendEmail = "bdd_vt_empty_friend@local.test";
    private const string EmptyFriendPassword = "BddVtEmpty123!";

    private const string StrangerUserName = "bdd_vt_stranger";
    private const string StrangerEmail = "bdd_vt_stranger@local.test";
    private const string StrangerPassword = "BddVtStranger123!";

    private readonly ApplicationDbContext _db;
    private readonly UserManager<User> _userManager;

    public BddViewTradesTestDataService(ApplicationDbContext db, UserManager<User> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public async Task<BddViewTradesSeedResult> ResetAndSeedAsync()
    {
        await CleanupAsync();

        var tradeGame = await UpsertGameAsync(900060, "BDD VT Trade Game");
        var nonTradeGame = await UpsertGameAsync(900061, "BDD VT Non-Trade Game");
        var emptyFriendGame = await UpsertGameAsync(900062, "BDD VT Empty Friend Game");

        var viewer = await CreateUserAsync(ViewerUserName, ViewerEmail, ViewerPassword);
        var friend = await CreateUserAsync(FriendUserName, FriendEmail, FriendPassword);
        var emptyFriend = await CreateUserAsync(EmptyFriendUserName, EmptyFriendEmail, EmptyFriendPassword);
        var stranger = await CreateUserAsync(StrangerUserName, StrangerEmail, StrangerPassword);

        var viewerProfile = new UserProfile { UserId = viewer.Id, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var friendProfile = new UserProfile { UserId = friend.Id, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var emptyFriendProfile = new UserProfile { UserId = emptyFriend.Id, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var strangerProfile = new UserProfile { UserId = stranger.Id, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };

        _db.Set<UserProfile>().AddRange(viewerProfile, friendProfile, emptyFriendProfile, strangerProfile);
        await _db.SaveChangesAsync();

        _db.Set<Friendship>().AddRange(
            new Friendship
            {
                RequesterProfileId = viewerProfile.Id,
                ReceiverProfileId = friendProfile.Id,
                Status = FriendshipStatus.Accepted,
                RequestedAt = DateTime.UtcNow,
                RespondedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new Friendship
            {
                RequesterProfileId = viewerProfile.Id,
                ReceiverProfileId = emptyFriendProfile.Id,
                Status = FriendshipStatus.Accepted,
                RequestedAt = DateTime.UtcNow,
                RespondedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        );

        _db.UserGameCollections.AddRange(
            new UserGameCollection { UserId = friend.Id, GameId = tradeGame.Id, DateAdded = DateTime.UtcNow, Status = CollectionStatus.Owned, IsAvailableForTrade = true },
            new UserGameCollection { UserId = friend.Id, GameId = nonTradeGame.Id, DateAdded = DateTime.UtcNow, Status = CollectionStatus.Owned, IsAvailableForTrade = false },
            new UserGameCollection { UserId = emptyFriend.Id, GameId = emptyFriendGame.Id, DateAdded = DateTime.UtcNow, Status = CollectionStatus.Owned, IsAvailableForTrade = false }
        );

        await _db.SaveChangesAsync();

        return new BddViewTradesSeedResult
        {
            ViewerUsername = ViewerUserName,
            ViewerPassword = ViewerPassword,
            FriendWithTradesUsername = FriendUserName,
            FriendNoTradesUsername = EmptyFriendUserName,
            StrangerUsername = StrangerUserName,
            TradeGameName = tradeGame.Name,
            NonTradeGameName = nonTradeGame.Name
        };
    }

    private async Task CleanupAsync()
    {
        foreach (var username in new[] { ViewerUserName, FriendUserName, EmptyFriendUserName, StrangerUserName })
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

public class BddViewTradesSeedResult
{
    public string ViewerUsername { get; set; } = "";
    public string ViewerPassword { get; set; } = "";
    public string FriendWithTradesUsername { get; set; } = "";
    public string FriendNoTradesUsername { get; set; } = "";
    public string StrangerUsername { get; set; } = "";
    public string TradeGameName { get; set; } = "";
    public string NonTradeGameName { get; set; } = "";
}
