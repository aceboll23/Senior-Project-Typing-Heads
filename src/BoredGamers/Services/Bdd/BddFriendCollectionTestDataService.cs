using BoredGamers.Data;
using BoredGamers.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BoredGamers.Services.Bdd;

public class BddFriendCollectionTestDataService
{
    private const string ViewerUserName = "bdd_fc_viewer";
    private const string ViewerEmail = "bdd_fc_viewer@local.test";
    private const string ViewerPassword = "BddFcViewer123!";

    private const string FriendWithGamesUserName = "bdd_fc_friend";
    private const string FriendWithGamesEmail = "bdd_fc_friend@local.test";
    private const string FriendWithGamesPassword = "BddFcFriend123!";

    private const string FriendEmptyUserName = "bdd_fc_empty_friend";
    private const string FriendEmptyEmail = "bdd_fc_empty_friend@local.test";
    private const string FriendEmptyPassword = "BddFcEmpty123!";

    private readonly ApplicationDbContext _db;
    private readonly UserManager<User> _userManager;

    public BddFriendCollectionTestDataService(ApplicationDbContext db, UserManager<User> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public async Task<BddFriendCollectionSeedResult> ResetAndSeedAsync()
    {
        foreach (var username in new[] { ViewerUserName, FriendWithGamesUserName, FriendEmptyUserName })
        {
            var existing = await _db.Users.OfType<User>().FirstOrDefaultAsync(u => u.UserName == username);
            if (existing != null)
            {
                var collections = await _db.UserGameCollections.Where(c => c.UserId == existing.Id).ToListAsync();
                if (collections.Count > 0)
                {
                    _db.UserGameCollections.RemoveRange(collections);
                    await _db.SaveChangesAsync();
                }
                await _userManager.DeleteAsync(existing);
            }
        }

        var ownedGame = await _db.Games.FirstOrDefaultAsync(g => g.BggGameId == 900050);
        if (ownedGame == null)
        {
            ownedGame = new Game
            {
                BggGameId = 900050,
                Name = "BDD FC Owned Game",
                YearPublished = 2024,
                Description = "Seeded owned game for BDD friend collection tests.",
                MinPlayers = 2,
                MaxPlayers = 4,
                PlayTime = 60,
                AverageRating = 7.5m,
                BggNumVoters = 100
            };
            _db.Games.Add(ownedGame);
        }
        else
        {
            ownedGame.Name = "BDD FC Owned Game";
        }

        var wishlistGame = await _db.Games.FirstOrDefaultAsync(g => g.BggGameId == 900051);
        if (wishlistGame == null)
        {
            wishlistGame = new Game
            {
                BggGameId = 900051,
                Name = "BDD FC Wishlist Game",
                YearPublished = 2024,
                Description = "Seeded wishlist-only game for BDD friend collection tests.",
                MinPlayers = 2,
                MaxPlayers = 4,
                PlayTime = 45,
                AverageRating = 6.8m,
                BggNumVoters = 75
            };
            _db.Games.Add(wishlistGame);
        }
        else
        {
            wishlistGame.Name = "BDD FC Wishlist Game";
        }

        await _db.SaveChangesAsync();

        var viewer = new User { UserName = ViewerUserName, Email = ViewerEmail, EmailConfirmed = true };
        var viewerResult = await _userManager.CreateAsync(viewer, ViewerPassword);
        if (!viewerResult.Succeeded)
            throw new InvalidOperationException($"Failed to create viewer: {string.Join("; ", viewerResult.Errors.Select(e => e.Description))}");

        var friendWithGames = new User { UserName = FriendWithGamesUserName, Email = FriendWithGamesEmail, EmailConfirmed = true };
        var friendResult = await _userManager.CreateAsync(friendWithGames, FriendWithGamesPassword);
        if (!friendResult.Succeeded)
            throw new InvalidOperationException($"Failed to create friend: {string.Join("; ", friendResult.Errors.Select(e => e.Description))}");

        var emptyFriend = new User { UserName = FriendEmptyUserName, Email = FriendEmptyEmail, EmailConfirmed = true };
        var emptyResult = await _userManager.CreateAsync(emptyFriend, FriendEmptyPassword);
        if (!emptyResult.Succeeded)
            throw new InvalidOperationException($"Failed to create empty friend: {string.Join("; ", emptyResult.Errors.Select(e => e.Description))}");

        _db.UserGameCollections.AddRange(
            new UserGameCollection { UserId = friendWithGames.Id, GameId = ownedGame.Id, DateAdded = DateTime.UtcNow, Status = CollectionStatus.Owned },
            new UserGameCollection { UserId = friendWithGames.Id, GameId = wishlistGame.Id, DateAdded = DateTime.UtcNow, Status = CollectionStatus.Wishlist }
        );
        await _db.SaveChangesAsync();

        return new BddFriendCollectionSeedResult
        {
            ViewerUsername = ViewerUserName,
            ViewerPassword = ViewerPassword,
            FriendWithGamesUsername = FriendWithGamesUserName,
            FriendEmptyUsername = FriendEmptyUserName,
            OwnedGameId = ownedGame.Id,
            OwnedGameName = ownedGame.Name,
            WishlistGameName = wishlistGame.Name
        };
    }
}

public class BddFriendCollectionSeedResult
{
    public string ViewerUsername { get; set; } = "";
    public string ViewerPassword { get; set; } = "";
    public string FriendWithGamesUsername { get; set; } = "";
    public string FriendEmptyUsername { get; set; } = "";
    public int OwnedGameId { get; set; }
    public string OwnedGameName { get; set; } = "";
    public string WishlistGameName { get; set; } = "";
}
