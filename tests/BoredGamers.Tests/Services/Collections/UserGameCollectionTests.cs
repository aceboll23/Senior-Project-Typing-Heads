using System;
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

      var join = new UserGameCollection
      {
        UserId = user.Id,
        GameId = game.Id,
        DateAdded = DateTime.UtcNow
      };

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
  }
}