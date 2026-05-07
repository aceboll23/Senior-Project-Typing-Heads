using BoredGamers.Data;
using BoredGamers.Models;
using BoredGamers.Services.Collections;
using BoredGamers.Tests.TestUtilities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace BoredGamers.Tests.Services.Collections;

[TestFixture]
public class GetAllFriendsTradeableGamesTests
{
    private SqliteConnection _conn = null!;
    private ApplicationDbContext _db = null!;
    private UserCollectionService _svc = null!;

    private User _viewer = null!;
    private User _friend1 = null!;
    private User _friend2 = null!;
    private User _stranger = null!;
    private UserProfile _viewerProfile = null!;
    private UserProfile _friend1Profile = null!;
    private UserProfile _friend2Profile = null!;
    private UserProfile _strangerProfile = null!;

    [SetUp]
    public async Task SetUp()
    {
        _conn = new SqliteConnection("DataSource=:memory:");
        await _conn.OpenAsync();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_conn)
            .Options;
        _db = new TestApplicationDbContext(options);
        await _db.Database.EnsureCreatedAsync();

        _svc = new UserCollectionService(_db);

        _viewer = new User { Id = "viewer-1", UserName = "Viewer", Email = "viewer@test.com", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        _friend1 = new User { Id = "friend1-1", UserName = "Friend1", Email = "friend1@test.com", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        _friend2 = new User { Id = "friend2-1", UserName = "Friend2", Email = "friend2@test.com", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        _stranger = new User { Id = "stranger-1", UserName = "Stranger", Email = "stranger@test.com", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        _db.Users.AddRange(_viewer, _friend1, _friend2, _stranger);
        await _db.SaveChangesAsync();

        _viewerProfile = new UserProfile { UserId = _viewer.Id, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        _friend1Profile = new UserProfile { UserId = _friend1.Id, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        _friend2Profile = new UserProfile { UserId = _friend2.Id, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        _strangerProfile = new UserProfile { UserId = _stranger.Id, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        _db.Set<UserProfile>().AddRange(_viewerProfile, _friend1Profile, _friend2Profile, _strangerProfile);
        await _db.SaveChangesAsync();
    }

    [TearDown]
    public async Task TearDown()
    {
        await _db.DisposeAsync();
        await _conn.DisposeAsync();
    }

    private async Task MakeFriends(UserProfile a, UserProfile b)
    {
        _db.Set<Friendship>().Add(new Friendship
        {
            RequesterProfileId = a.Id,
            ReceiverProfileId = b.Id,
            Status = FriendshipStatus.Accepted,
            RequestedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
    }

    private async Task<Game> AddGameAsync(string name, int bggId)
    {
        var game = new Game { BggGameId = bggId, Name = name, LastSyncedAt = DateTime.UtcNow };
        _db.Games.Add(game);
        await _db.SaveChangesAsync();
        return game;
    }

    [Test]
    public async Task GetAllFriendsTradeableGamesAsync_ReturnsTradeableGamesFromAllFriends()
    {
        await MakeFriends(_viewerProfile, _friend1Profile);
        await MakeFriends(_viewerProfile, _friend2Profile);
        var game1 = await AddGameAsync("Friend1 Game", 101);
        var game2 = await AddGameAsync("Friend2 Game", 102);

        _db.UserGameCollections.AddRange(
            new UserGameCollection { UserId = _friend1.Id, GameId = game1.Id, DateAdded = DateTime.UtcNow, Status = CollectionStatus.Owned, IsAvailableForTrade = true },
            new UserGameCollection { UserId = _friend2.Id, GameId = game2.Id, DateAdded = DateTime.UtcNow, Status = CollectionStatus.Owned, IsAvailableForTrade = true }
        );
        await _db.SaveChangesAsync();

        var (items, totalCount) = await _svc.GetAllFriendsTradeableGamesAsync(_viewer.Id, 1, 20);

        Assert.That(totalCount, Is.EqualTo(2));
        Assert.That(items.Count, Is.EqualTo(2));
        Assert.That(items.Select(i => i.Game.Name), Does.Contain("Friend1 Game"));
        Assert.That(items.Select(i => i.Game.Name), Does.Contain("Friend2 Game"));
    }

    [Test]
    public async Task GetAllFriendsTradeableGamesAsync_ExcludesGamesFromNonFriends()
    {
        await MakeFriends(_viewerProfile, _friend1Profile);
        var friendGame = await AddGameAsync("Friend Game", 201);
        var strangerGame = await AddGameAsync("Stranger Game", 202);

        _db.UserGameCollections.AddRange(
            new UserGameCollection { UserId = _friend1.Id, GameId = friendGame.Id, DateAdded = DateTime.UtcNow, Status = CollectionStatus.Owned, IsAvailableForTrade = true },
            new UserGameCollection { UserId = _stranger.Id, GameId = strangerGame.Id, DateAdded = DateTime.UtcNow, Status = CollectionStatus.Owned, IsAvailableForTrade = true }
        );
        await _db.SaveChangesAsync();

        var (items, _) = await _svc.GetAllFriendsTradeableGamesAsync(_viewer.Id, 1, 20);

        Assert.That(items.Count, Is.EqualTo(1));
        Assert.That(items[0].Game.Name, Is.EqualTo("Friend Game"));
    }

    [Test]
    public async Task GetAllFriendsTradeableGamesAsync_ExcludesNonTradeableGames()
    {
        await MakeFriends(_viewerProfile, _friend1Profile);
        var tradeGame = await AddGameAsync("Trade Game", 301);
        var noTradeGame = await AddGameAsync("No Trade Game", 302);

        _db.UserGameCollections.AddRange(
            new UserGameCollection { UserId = _friend1.Id, GameId = tradeGame.Id, DateAdded = DateTime.UtcNow, Status = CollectionStatus.Owned, IsAvailableForTrade = true },
            new UserGameCollection { UserId = _friend1.Id, GameId = noTradeGame.Id, DateAdded = DateTime.UtcNow, Status = CollectionStatus.Owned, IsAvailableForTrade = false }
        );
        await _db.SaveChangesAsync();

        var (items, _) = await _svc.GetAllFriendsTradeableGamesAsync(_viewer.Id, 1, 20);

        Assert.That(items.Count, Is.EqualTo(1));
        Assert.That(items[0].Game.Name, Is.EqualTo("Trade Game"));
    }

    [Test]
    public async Task GetAllFriendsTradeableGamesAsync_ReturnsEmptyWhenNoFriends()
    {
        var game = await AddGameAsync("Some Game", 401);
        _db.UserGameCollections.Add(new UserGameCollection { UserId = _friend1.Id, GameId = game.Id, DateAdded = DateTime.UtcNow, Status = CollectionStatus.Owned, IsAvailableForTrade = true });
        await _db.SaveChangesAsync();

        var (items, totalCount) = await _svc.GetAllFriendsTradeableGamesAsync(_viewer.Id, 1, 20);

        Assert.That(totalCount, Is.EqualTo(0));
        Assert.That(items, Is.Empty);
    }

    [Test]
    public async Task GetAllFriendsTradeableGamesAsync_SortsByMostRecentlyAdded()
    {
        await MakeFriends(_viewerProfile, _friend1Profile);
        var olderGame = await AddGameAsync("Older Game", 501);
        var newerGame = await AddGameAsync("Newer Game", 502);

        _db.UserGameCollections.AddRange(
            new UserGameCollection { UserId = _friend1.Id, GameId = olderGame.Id, DateAdded = DateTime.UtcNow.AddDays(-2), Status = CollectionStatus.Owned, IsAvailableForTrade = true },
            new UserGameCollection { UserId = _friend1.Id, GameId = newerGame.Id, DateAdded = DateTime.UtcNow, Status = CollectionStatus.Owned, IsAvailableForTrade = true }
        );
        await _db.SaveChangesAsync();

        var (items, _) = await _svc.GetAllFriendsTradeableGamesAsync(_viewer.Id, 1, 20);

        Assert.That(items[0].Game.Name, Is.EqualTo("Newer Game"));
        Assert.That(items[1].Game.Name, Is.EqualTo("Older Game"));
    }

    [Test]
    public async Task GetAllFriendsTradeableGamesAsync_PaginatesCorrectly()
    {
        await MakeFriends(_viewerProfile, _friend1Profile);
        for (int i = 1; i <= 25; i++)
        {
            var g = await AddGameAsync($"Game {i:D2}", 600 + i);
            _db.UserGameCollections.Add(new UserGameCollection
            {
                UserId = _friend1.Id,
                GameId = g.Id,
                DateAdded = DateTime.UtcNow.AddMinutes(-i),
                Status = CollectionStatus.Owned,
                IsAvailableForTrade = true
            });
        }
        await _db.SaveChangesAsync();

        var (page1Items, total) = await _svc.GetAllFriendsTradeableGamesAsync(_viewer.Id, 1, 20);
        var (page2Items, _) = await _svc.GetAllFriendsTradeableGamesAsync(_viewer.Id, 2, 20);

        Assert.That(total, Is.EqualTo(25));
        Assert.That(page1Items.Count, Is.EqualTo(20));
        Assert.That(page2Items.Count, Is.EqualTo(5));
    }

    [Test]
    public async Task GetAllFriendsTradeableGamesAsync_ExcludesBlockedUserGames()
    {
        await MakeFriends(_viewerProfile, _friend1Profile);
        await MakeFriends(_viewerProfile, _friend2Profile);
        var game1 = await AddGameAsync("Friend1 Game", 701);
        var game2 = await AddGameAsync("Friend2 Blocked Game", 702);

        _db.UserGameCollections.AddRange(
            new UserGameCollection { UserId = _friend1.Id, GameId = game1.Id, DateAdded = DateTime.UtcNow, Status = CollectionStatus.Owned, IsAvailableForTrade = true },
            new UserGameCollection { UserId = _friend2.Id, GameId = game2.Id, DateAdded = DateTime.UtcNow, Status = CollectionStatus.Owned, IsAvailableForTrade = true }
        );

        _db.Set<BlockedUser>().Add(new BlockedUser
        {
            BlockerProfileId = _viewerProfile.Id,
            BlockedProfileId = _friend2Profile.Id,
            BlockedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        var (items, _) = await _svc.GetAllFriendsTradeableGamesAsync(_viewer.Id, 1, 20);

        Assert.That(items.Count, Is.EqualTo(1));
        Assert.That(items[0].Game.Name, Is.EqualTo("Friend1 Game"));
    }

    [Test]
    public async Task GetAllFriendsTradeableGamesAsync_SetsOwnerUsernameOnItems()
    {
        await MakeFriends(_viewerProfile, _friend1Profile);
        var game = await AddGameAsync("Trade Game", 801);
        _db.UserGameCollections.Add(new UserGameCollection { UserId = _friend1.Id, GameId = game.Id, DateAdded = DateTime.UtcNow, Status = CollectionStatus.Owned, IsAvailableForTrade = true });
        await _db.SaveChangesAsync();

        var (items, _) = await _svc.GetAllFriendsTradeableGamesAsync(_viewer.Id, 1, 20);

        Assert.That(items[0].OwnerUsername, Is.EqualTo(_friend1.UserName));
    }
}
