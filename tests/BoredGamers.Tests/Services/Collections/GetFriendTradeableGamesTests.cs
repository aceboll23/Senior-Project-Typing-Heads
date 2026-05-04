using BoredGamers.Data;
using BoredGamers.Models;
using BoredGamers.Services.Collections;
using BoredGamers.Tests.TestUtilities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace BoredGamers.Tests.Services.Collections;

[TestFixture]
public class GetFriendTradeableGamesTests
{
    private SqliteConnection _conn = null!;
    private ApplicationDbContext _db = null!;
    private UserCollectionService _svc = null!;

    private User _viewer = null!;
    private User _owner = null!;
    private UserProfile _viewerProfile = null!;
    private UserProfile _ownerProfile = null!;

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
        _owner = new User { Id = "owner-1", UserName = "Owner", Email = "owner@test.com", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        _db.Users.AddRange(_viewer, _owner);
        await _db.SaveChangesAsync();

        _viewerProfile = new UserProfile { UserId = _viewer.Id, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        _ownerProfile = new UserProfile { UserId = _owner.Id, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        _db.Set<UserProfile>().AddRange(_viewerProfile, _ownerProfile);
        await _db.SaveChangesAsync();
    }

    [TearDown]
    public async Task TearDown()
    {
        await _db.DisposeAsync();
        await _conn.DisposeAsync();
    }

    private async Task MakeFriends()
    {
        _db.Set<Friendship>().Add(new Friendship
        {
            RequesterProfileId = _viewerProfile.Id,
            ReceiverProfileId = _ownerProfile.Id,
            Status = FriendshipStatus.Accepted,
            RequestedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
    }

    private async Task<Game> AddGameAsync(string name, int bggId = 1)
    {
        var game = new Game { BggGameId = bggId, Name = name, LastSyncedAt = DateTime.UtcNow };
        _db.Games.Add(game);
        await _db.SaveChangesAsync();
        return game;
    }

    [Test]
    public async Task GetFriendTradeableGamesAsync_ReturnsOnlyTradeableGames()
    {
        await MakeFriends();
        var tradeGame = await AddGameAsync("Trade Game", 101);
        var noTradeGame = await AddGameAsync("No Trade Game", 102);

        _db.UserGameCollections.AddRange(
            new UserGameCollection { UserId = _owner.Id, GameId = tradeGame.Id, DateAdded = DateTime.UtcNow, Status = CollectionStatus.Owned, IsAvailableForTrade = true },
            new UserGameCollection { UserId = _owner.Id, GameId = noTradeGame.Id, DateAdded = DateTime.UtcNow, Status = CollectionStatus.Owned, IsAvailableForTrade = false }
        );
        await _db.SaveChangesAsync();

        var result = await _svc.GetFriendTradeableGamesAsync(_viewer.Id, _owner.UserName!);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Count, Is.EqualTo(1));
        Assert.That(result[0].Name, Is.EqualTo("Trade Game"));
    }

    [Test]
    public async Task GetFriendTradeableGamesAsync_ReturnsNullWhenNotFriends()
    {
        var game = await AddGameAsync("Some Game", 201);
        _db.UserGameCollections.Add(new UserGameCollection { UserId = _owner.Id, GameId = game.Id, DateAdded = DateTime.UtcNow, Status = CollectionStatus.Owned, IsAvailableForTrade = true });
        await _db.SaveChangesAsync();

        var result = await _svc.GetFriendTradeableGamesAsync(_viewer.Id, _owner.UserName!);

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GetFriendTradeableGamesAsync_ReturnsEmptyListWhenFriendHasNoTradeableGames()
    {
        await MakeFriends();
        var game = await AddGameAsync("Owned But Not Tradeable", 301);
        _db.UserGameCollections.Add(new UserGameCollection { UserId = _owner.Id, GameId = game.Id, DateAdded = DateTime.UtcNow, Status = CollectionStatus.Owned, IsAvailableForTrade = false });
        await _db.SaveChangesAsync();

        var result = await _svc.GetFriendTradeableGamesAsync(_viewer.Id, _owner.UserName!);

        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.Empty);
    }

    [Test]
    public async Task GetFriendTradeableGamesAsync_ReturnsNullWhenOwnerNotFound()
    {
        var result = await _svc.GetFriendTradeableGamesAsync(_viewer.Id, "nonexistent-user");

        Assert.That(result, Is.Null);
    }
}
