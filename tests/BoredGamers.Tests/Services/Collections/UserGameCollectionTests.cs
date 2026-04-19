using System;
using System.Linq;
using System.Threading.Tasks;
using BoredGamers.Data;
using BoredGamers.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace BoredGamers.Tests.Services.Collections
{
  [TestFixture]
  public class UserGameCollectionTests
  {
    private static async Task<ApplicationDbContext> CreateSqliteDbAsync(SqliteConnection conn)
    {
      var options = new DbContextOptionsBuilder<ApplicationDbContext>()
          .UseSqlite(conn)
          .Options;

      var db = new BoredGamers.Tests.TestUtilities.TestApplicationDbContext(options);

      await db.Database.EnsureCreatedAsync();
      return db;
    }

    [Test]
    public async Task AddToCollection_FirstTime_CreatesJoinRecord()
    {
      // Arrange (SQLite in-memory so unique/FKs behave like a real DB)
      await using var conn = new SqliteConnection("DataSource=:memory:");
      await conn.OpenAsync();

      await using var db = await CreateSqliteDbAsync(conn);

      // Seed a user (Identity)
      var user = new IdentityUser
      {
        Id = "user-1",
        UserName = "user1@test.com",
        Email = "user1@test.com"
      };
      db.Users.Add(user);

      // Seed a game 
      var game = new Game
      {
        BggGameId = 123,
        Name = "Test Game",
        LastSyncedAt = DateTime.UtcNow
      };
      db.Games.Add(game);

      await db.SaveChangesAsync();

      // Act
      db.UserGameCollections.Add(new UserGameCollection
      {
        UserId = user.Id,
        GameId = game.Id,
        DateAdded = DateTime.UtcNow
      });

      await db.SaveChangesAsync();

      // Assert
      var count = await db.UserGameCollections.CountAsync();
      Assert.That(count, Is.EqualTo(1));
    }

    [Test]
    public async Task AddToCollection_SameUserSameGame_Twice_DoesNotCreateDuplicate_AndDoesNotCrash()
    {
      // Arrange
      await using var conn = new SqliteConnection("DataSource=:memory:");
      await conn.OpenAsync();

      await using var db = await CreateSqliteDbAsync(conn);

      var user = new IdentityUser { Id = "user-1", UserName = "user1@test.com", Email = "user1@test.com" };
      db.Users.Add(user);

      var game = new Game { BggGameId = 999, Name = "Test Game", LastSyncedAt = DateTime.UtcNow };
      db.Games.Add(game);

      await db.SaveChangesAsync();

      // Act (simulate same request twice)
      var svc = new BoredGamers.Services.Collections.UserCollectionService(db);

      var added1 = await svc.AddToCollectionAsync(user.Id, game.Id);
      var added2 = await svc.AddToCollectionAsync(user.Id, game.Id);

      // Assert: should not crash AND should still be 1 record
      Assert.That(added1, Is.True);
      Assert.That(added2, Is.False);

      var count = await db.UserGameCollections.CountAsync();
      Assert.That(count, Is.EqualTo(1));
    }

    // --- TYP-49 Wishlist Tests ---

    [Test]
    public async Task NewCollectionRecord_DefaultsToOwned()
    {
      // Arrange
      await using var conn = new SqliteConnection("DataSource=:memory:");
      await conn.OpenAsync();
      await using var db = await CreateSqliteDbAsync(conn);

      var user = new IdentityUser { Id = "user-1", UserName = "user1@test.com", Email = "user1@test.com" };
      db.Users.Add(user);
      var game = new Game { BggGameId = 1001, Name = "Default Status Game", LastSyncedAt = DateTime.UtcNow };
      db.Games.Add(game);
      await db.SaveChangesAsync();

      // Act — create record without setting Status
      db.UserGameCollections.Add(new UserGameCollection
      {
        UserId = user.Id,
        GameId = game.Id,
        DateAdded = DateTime.UtcNow
      });
      await db.SaveChangesAsync();

      // Assert — Status should default to Owned
      var record = await db.UserGameCollections.FirstAsync();
      Assert.That(record.Status, Is.EqualTo(CollectionStatus.Owned));
    }

    [Test]
    public async Task AddToWishlist_CreatesRecordWithWishlistStatus()
    {
      // Arrange
      await using var conn = new SqliteConnection("DataSource=:memory:");
      await conn.OpenAsync();
      await using var db = await CreateSqliteDbAsync(conn);

      var user = new IdentityUser { Id = "user-1", UserName = "user1@test.com", Email = "user1@test.com" };
      db.Users.Add(user);
      var game = new Game { BggGameId = 1002, Name = "Wishlist Game", LastSyncedAt = DateTime.UtcNow };
      db.Games.Add(game);
      await db.SaveChangesAsync();

      var svc = new BoredGamers.Services.Collections.UserCollectionService(db);

      // Act
      var result = await svc.AddToWishlistAsync(user.Id, game.Id);

      // Assert
      Assert.That(result, Is.True);
      var record = await db.UserGameCollections.FirstAsync();
      Assert.That(record.Status, Is.EqualTo(CollectionStatus.Wishlist));
    }

    [Test]
    public async Task AddToWishlist_SameGameTwice_ReturnsFalse_AndNoDuplicate()
    {
      // Arrange
      await using var conn = new SqliteConnection("DataSource=:memory:");
      await conn.OpenAsync();
      await using var db = await CreateSqliteDbAsync(conn);

      var user = new IdentityUser { Id = "user-1", UserName = "user1@test.com", Email = "user1@test.com" };
      db.Users.Add(user);
      var game = new Game { BggGameId = 1003, Name = "Duplicate Wishlist Game", LastSyncedAt = DateTime.UtcNow };
      db.Games.Add(game);
      await db.SaveChangesAsync();

      var svc = new BoredGamers.Services.Collections.UserCollectionService(db);

      // Act
      var first = await svc.AddToWishlistAsync(user.Id, game.Id);
      var second = await svc.AddToWishlistAsync(user.Id, game.Id);

      // Assert
      Assert.That(first, Is.True);
      Assert.That(second, Is.False);
      var count = await db.UserGameCollections.CountAsync();
      Assert.That(count, Is.EqualTo(1));
    }

    [Test]
    public async Task AddToWishlist_GameAlreadyOwned_ReturnsFalse()
    {
      // Arrange
      await using var conn = new SqliteConnection("DataSource=:memory:");
      await conn.OpenAsync();
      await using var db = await CreateSqliteDbAsync(conn);

      var user = new IdentityUser { Id = "user-1", UserName = "user1@test.com", Email = "user1@test.com" };
      db.Users.Add(user);
      var game = new Game { BggGameId = 1004, Name = "Already Owned Game", LastSyncedAt = DateTime.UtcNow };
      db.Games.Add(game);
      await db.SaveChangesAsync();

      var svc = new BoredGamers.Services.Collections.UserCollectionService(db);
      await svc.AddToCollectionAsync(user.Id, game.Id);

      // Act — try to wishlist a game you already own
      var result = await svc.AddToWishlistAsync(user.Id, game.Id);

      // Assert
      Assert.That(result, Is.False);
    }

    [Test]
    public async Task AddToCollection_WhenGameIsWishlisted_PromotesToOwned()
    {
      // Arrange
      await using var conn = new SqliteConnection("DataSource=:memory:");
      await conn.OpenAsync();
      await using var db = await CreateSqliteDbAsync(conn);

      var user = new IdentityUser { Id = "user-1", UserName = "user1@test.com", Email = "user1@test.com" };
      db.Users.Add(user);
      var game = new Game { BggGameId = 1005, Name = "Promote To Owned Game", LastSyncedAt = DateTime.UtcNow };
      db.Games.Add(game);
      await db.SaveChangesAsync();

      var svc = new BoredGamers.Services.Collections.UserCollectionService(db);
      await svc.AddToWishlistAsync(user.Id, game.Id);

      // Act — add to owned collection while already wishlisted
      var result = await svc.AddToCollectionAsync(user.Id, game.Id);

      // Assert — promoted to Owned, still only one record
      Assert.That(result, Is.True);
      var count = await db.UserGameCollections.CountAsync();
      Assert.That(count, Is.EqualTo(1));
      var record = await db.UserGameCollections.FirstAsync();
      Assert.That(record.Status, Is.EqualTo(CollectionStatus.Owned));
    }

    [Test]
    public async Task IsOnWishlistAsync_WhenGameIsWishlisted_ReturnsTrue()
    {
      // Arrange
      await using var conn = new SqliteConnection("DataSource=:memory:");
      await conn.OpenAsync();
      await using var db = await CreateSqliteDbAsync(conn);

      var user = new IdentityUser { Id = "user-1", UserName = "user1@test.com", Email = "user1@test.com" };
      db.Users.Add(user);
      var game = new Game { BggGameId = 1006, Name = "Wishlist Check Game", LastSyncedAt = DateTime.UtcNow };
      db.Games.Add(game);
      await db.SaveChangesAsync();

      var svc = new BoredGamers.Services.Collections.UserCollectionService(db);
      await svc.AddToWishlistAsync(user.Id, game.Id);

      // Act
      var result = await svc.IsOnWishlistAsync(user.Id, game.Id);

      // Assert
      Assert.That(result, Is.True);
    }

    [Test]
    public async Task IsOnWishlistAsync_WhenGameIsOwned_ReturnsFalse()
    {
      // Arrange
      await using var conn = new SqliteConnection("DataSource=:memory:");
      await conn.OpenAsync();
      await using var db = await CreateSqliteDbAsync(conn);

      var user = new IdentityUser { Id = "user-1", UserName = "user1@test.com", Email = "user1@test.com" };
      db.Users.Add(user);
      var game = new Game { BggGameId = 1007, Name = "Owned Not Wishlist Game", LastSyncedAt = DateTime.UtcNow };
      db.Games.Add(game);
      await db.SaveChangesAsync();

      var svc = new BoredGamers.Services.Collections.UserCollectionService(db);
      await svc.AddToCollectionAsync(user.Id, game.Id);

      // Act
      var result = await svc.IsOnWishlistAsync(user.Id, game.Id);

      // Assert
      Assert.That(result, Is.False);
    }

    [Test]
    public async Task IsInCollectionAsync_WhenGameIsOwned_ReturnsTrue()
    {
      // Arrange
      await using var conn = new SqliteConnection("DataSource=:memory:");
      await conn.OpenAsync();
      await using var db = await CreateSqliteDbAsync(conn);

      var user = new IdentityUser { Id = "user-1", UserName = "user1@test.com", Email = "user1@test.com" };
      db.Users.Add(user);
      var game = new Game { BggGameId = 1008, Name = "Owned Game", LastSyncedAt = DateTime.UtcNow };
      db.Games.Add(game);
      await db.SaveChangesAsync();

      var svc = new BoredGamers.Services.Collections.UserCollectionService(db);
      await svc.AddToCollectionAsync(user.Id, game.Id);

      // Act
      var result = await svc.IsInCollectionAsync(user.Id, game.Id);

      // Assert
      Assert.That(result, Is.True);
    }

    // --- TYP-50 Remove Wishlist Tests ---

    [Test]
    public async Task RemoveFromWishlist_WhenGameIsWishlisted_ReturnsTrue_AndDeletesRecord()
    {
        await using var conn = new SqliteConnection("DataSource=:memory:");
        await conn.OpenAsync();
        await using var db = await CreateSqliteDbAsync(conn);

        var user = new IdentityUser { Id = "user-1", UserName = "user1@test.com", Email = "user1@test.com" };
        db.Users.Add(user);
        var game = new Game { BggGameId = 1011, Name = "Remove Wishlist Game", LastSyncedAt = DateTime.UtcNow };
        db.Games.Add(game);
        await db.SaveChangesAsync();

        var svc = new BoredGamers.Services.Collections.UserCollectionService(db);
        await svc.AddToWishlistAsync(user.Id, game.Id);

        var result = await svc.RemoveFromWishlistAsync(user.Id, game.Id);

        Assert.That(result, Is.True);
        var count = await db.UserGameCollections.CountAsync();
        Assert.That(count, Is.EqualTo(0));
    }

    [Test]
    public async Task RemoveFromWishlist_WhenGameNotOnWishlist_ReturnsFalse()
    {
        await using var conn = new SqliteConnection("DataSource=:memory:");
        await conn.OpenAsync();
        await using var db = await CreateSqliteDbAsync(conn);

        var user = new IdentityUser { Id = "user-1", UserName = "user1@test.com", Email = "user1@test.com" };
        db.Users.Add(user);
        var game = new Game { BggGameId = 1012, Name = "Never On Wishlist Game", LastSyncedAt = DateTime.UtcNow };
        db.Games.Add(game);
        await db.SaveChangesAsync();

        var svc = new BoredGamers.Services.Collections.UserCollectionService(db);

        var result = await svc.RemoveFromWishlistAsync(user.Id, game.Id);

        Assert.That(result, Is.False);
        var count = await db.UserGameCollections.CountAsync();
        Assert.That(count, Is.EqualTo(0));
    }

    [Test]
    public async Task RemoveFromWishlist_WhenGameIsOwned_ReturnsFalse_AndKeepsOwnedRecord()
    {
        await using var conn = new SqliteConnection("DataSource=:memory:");
        await conn.OpenAsync();
        await using var db = await CreateSqliteDbAsync(conn);

        var user = new IdentityUser { Id = "user-1", UserName = "user1@test.com", Email = "user1@test.com" };
        db.Users.Add(user);
        var game = new Game { BggGameId = 1013, Name = "Owned Not Wishlist Remove Game", LastSyncedAt = DateTime.UtcNow };
        db.Games.Add(game);
        await db.SaveChangesAsync();

        var svc = new BoredGamers.Services.Collections.UserCollectionService(db);
        await svc.AddToCollectionAsync(user.Id, game.Id);

        var result = await svc.RemoveFromWishlistAsync(user.Id, game.Id);

        Assert.That(result, Is.False);
        var record = await db.UserGameCollections.FirstAsync();
        Assert.That(record.Status, Is.EqualTo(CollectionStatus.Owned));
    }

    [Test]
    public async Task GetOwnedGames_ReturnsOnlyOwned_NotWishlisted()
    {
      // Arrange
      await using var conn = new SqliteConnection("DataSource=:memory:");
      await conn.OpenAsync();
      await using var db = await CreateSqliteDbAsync(conn);

      var user = new IdentityUser { Id = "user-1", UserName = "user1@test.com", Email = "user1@test.com" };
      db.Users.Add(user);

      var ownedGame = new Game { BggGameId = 1009, Name = "Owned Game", LastSyncedAt = DateTime.UtcNow };
      var wishlistGame = new Game { BggGameId = 1010, Name = "Wishlist Game", LastSyncedAt = DateTime.UtcNow };
      db.Games.AddRange(ownedGame, wishlistGame);
      await db.SaveChangesAsync();

      var svc = new BoredGamers.Services.Collections.UserCollectionService(db);
      await svc.AddToCollectionAsync(user.Id, ownedGame.Id);
      await svc.AddToWishlistAsync(user.Id, wishlistGame.Id);

      // Act — query only owned games
      var ownedOnly = await db.UserGameCollections
          .Where(c => c.UserId == user.Id && c.Status == CollectionStatus.Owned)
          .ToListAsync();

      // Assert — only the owned game comes back
      Assert.That(ownedOnly.Count, Is.EqualTo(1));
      Assert.That(ownedOnly[0].GameId, Is.EqualTo(ownedGame.Id));
    }
  }
}