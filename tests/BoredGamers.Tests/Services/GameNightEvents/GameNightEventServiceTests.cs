using System;
using System.Threading.Tasks;
using BoredGamers.Data;
using BoredGamers.Models;
using BoredGamers.Services.GameNightEvents;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace BoredGamers.Tests.Services.GameNightEvents
{
  [TestFixture]
  public class GameNightEventServiceTests
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
    public async Task AddGameToEvent_UserOwnsGame_AndGameNotAlreadyAdded_ReturnsTrue()
    {
      // Arrange
      await using var conn = new SqliteConnection("DataSource=:memory:");
      await conn.OpenAsync();
      await using var db = await CreateSqliteDbAsync(conn);

      var user = new User
      {
        Id = "user-1",
        UserName = "user1@test.com",
        Email = "user1@test.com"
      };
      db.Users.Add(user);

      var playgroup = new Playgroup
      {
        Name = "Test Group",
        CreatedByUserId = user.Id,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
      };
      db.Playgroups.Add(playgroup);
      await db.SaveChangesAsync();

      db.PlaygroupMembers.Add(new PlaygroupMember
      {
        PlaygroupId = playgroup.Id,
        UserId = user.Id,
        Role = PlaygroupRole.Owner,
        JoinedAt = DateTime.UtcNow
      });

      var game = new Game
      {
        BggGameId = 101,
        Name = "Catan",
        LastSyncedAt = DateTime.UtcNow
      };
      db.Games.Add(game);
      await db.SaveChangesAsync();

      db.UserGameCollections.Add(new UserGameCollection
      {
        UserId = user.Id,
        GameId = game.Id,
        DateAdded = DateTime.UtcNow
      });

      var gameNightEvent = new GameNightEvent
      {
        PlaygroupId = playgroup.Id,
        CreatedByUserId = user.Id,
        Title = "Friday Night",
        EventDateTime = DateTime.UtcNow.AddDays(1),
        Description = "Bring games",
        CreatedAt = DateTime.UtcNow
      };
      db.GameNightEvents.Add(gameNightEvent);
      await db.SaveChangesAsync();

      var svc = new GameNightEventService(db);

      // Act
      var added = await svc.AddGameToEventAsync(gameNightEvent.Id, game.Id, user.Id);

      // Assert
      Assert.That(added, Is.True);
      var count = await db.GameNightEventGames.CountAsync();
      Assert.That(count, Is.EqualTo(1));
    }

    [Test]
    public async Task AddGameToEvent_SameGameAlreadyAddedToEvent_ReturnsFalse_AndDoesNotDuplicate()
    {
      // Arrange
      await using var conn = new SqliteConnection("DataSource=:memory:");
      await conn.OpenAsync();
      await using var db = await CreateSqliteDbAsync(conn);

      var creator = new User
      {
        Id = "user-1",
        UserName = "creator@test.com",
        Email = "creator@test.com"
      };

      var secondUser = new User
      {
        Id = "user-2",
        UserName = "member@test.com",
        Email = "member@test.com"
      };

      db.Users.Add(creator);
      db.Users.Add(secondUser);

      var playgroup = new Playgroup
      {
        Name = "Test Group",
        CreatedByUserId = creator.Id,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
      };
      db.Playgroups.Add(playgroup);
      await db.SaveChangesAsync();

      db.PlaygroupMembers.Add(new PlaygroupMember
      {
        PlaygroupId = playgroup.Id,
        UserId = creator.Id,
        Role = PlaygroupRole.Owner,
        JoinedAt = DateTime.UtcNow
      });

      db.PlaygroupMembers.Add(new PlaygroupMember
      {
        PlaygroupId = playgroup.Id,
        UserId = secondUser.Id,
        Role = PlaygroupRole.Member,
        JoinedAt = DateTime.UtcNow
      });

      var game = new Game
      {
        BggGameId = 202,
        Name = "Azul",
        LastSyncedAt = DateTime.UtcNow
      };
      db.Games.Add(game);
      await db.SaveChangesAsync();

      db.UserGameCollections.Add(new UserGameCollection
      {
        UserId = creator.Id,
        GameId = game.Id,
        DateAdded = DateTime.UtcNow
      });

      db.UserGameCollections.Add(new UserGameCollection
      {
        UserId = secondUser.Id,
        GameId = game.Id,
        DateAdded = DateTime.UtcNow
      });

      var gameNightEvent = new GameNightEvent
      {
        PlaygroupId = playgroup.Id,
        CreatedByUserId = creator.Id,
        Title = "Saturday Night",
        EventDateTime = DateTime.UtcNow.AddDays(2),
        Description = "Board games",
        CreatedAt = DateTime.UtcNow
      };
      db.GameNightEvents.Add(gameNightEvent);
      await db.SaveChangesAsync();

      db.GameNightEventGames.Add(new GameNightEventGame
      {
        GameNightEventId = gameNightEvent.Id,
        GameId = game.Id,
        UserId = creator.Id
      });
      await db.SaveChangesAsync();

      var svc = new GameNightEventService(db);

      // Act
      var added = await svc.AddGameToEventAsync(gameNightEvent.Id, game.Id, secondUser.Id);

      // Assert
      Assert.That(added, Is.False);
      var count = await db.GameNightEventGames.CountAsync();
      Assert.That(count, Is.EqualTo(1));
    }

    [Test]
    public async Task RemoveGameFromEvent_WhenUserOwnsThatEventGame_ReturnsTrue_AndRemovesRow()
    {
      // Arrange
      await using var conn = new SqliteConnection("DataSource=:memory:");
      await conn.OpenAsync();
      await using var db = await CreateSqliteDbAsync(conn);

      var user = new User
      {
        Id = "user-1",
        UserName = "user1@test.com",
        Email = "user1@test.com"
      };
      db.Users.Add(user);

      var playgroup = new Playgroup
      {
        Name = "Test Group",
        CreatedByUserId = user.Id,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
      };
      db.Playgroups.Add(playgroup);
      await db.SaveChangesAsync();

      db.PlaygroupMembers.Add(new PlaygroupMember
      {
        PlaygroupId = playgroup.Id,
        UserId = user.Id,
        Role = PlaygroupRole.Owner,
        JoinedAt = DateTime.UtcNow
      });

      var game = new Game
      {
        BggGameId = 303,
        Name = "Wingspan",
        LastSyncedAt = DateTime.UtcNow
      };
      db.Games.Add(game);
      await db.SaveChangesAsync();

      var gameNightEvent = new GameNightEvent
      {
        PlaygroupId = playgroup.Id,
        CreatedByUserId = user.Id,
        Title = "Game Night",
        EventDateTime = DateTime.UtcNow.AddDays(1),
        Description = "Test",
        CreatedAt = DateTime.UtcNow
      };
      db.GameNightEvents.Add(gameNightEvent);
      await db.SaveChangesAsync();

      var eventGame = new GameNightEventGame
      {
        GameNightEventId = gameNightEvent.Id,
        GameId = game.Id,
        UserId = user.Id
      };
      db.GameNightEventGames.Add(eventGame);
      await db.SaveChangesAsync();

      var svc = new GameNightEventService(db);

      // Act
      var removed = await svc.RemoveGameFromEventAsync(eventGame.Id, user.Id);

      // Assert
      Assert.That(removed, Is.True);
      var count = await db.GameNightEventGames.CountAsync();
      Assert.That(count, Is.EqualTo(0));
    }

    [Test]
    public async Task RemoveGameFromEvent_WhenDifferentUserTriesToRemove_ReturnsFalse()
    {
      // Arrange
      await using var conn = new SqliteConnection("DataSource=:memory:");
      await conn.OpenAsync();
      await using var db = await CreateSqliteDbAsync(conn);

      var ownerUser = new User
      {
        Id = "user-1",
        UserName = "owner@test.com",
        Email = "owner@test.com"
      };

      var otherUser = new User
      {
        Id = "user-2",
        UserName = "other@test.com",
        Email = "other@test.com"
      };

      db.Users.Add(ownerUser);
      db.Users.Add(otherUser);

      var playgroup = new Playgroup
      {
        Name = "Test Group",
        CreatedByUserId = ownerUser.Id,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
      };
      db.Playgroups.Add(playgroup);
      await db.SaveChangesAsync();

      var game = new Game
      {
        BggGameId = 404,
        Name = "Heat",
        LastSyncedAt = DateTime.UtcNow
      };
      db.Games.Add(game);
      await db.SaveChangesAsync();

      var gameNightEvent = new GameNightEvent
      {
        PlaygroupId = playgroup.Id,
        CreatedByUserId = ownerUser.Id,
        Title = "Sunday Event",
        EventDateTime = DateTime.UtcNow.AddDays(1),
        Description = "Test",
        CreatedAt = DateTime.UtcNow
      };
      db.GameNightEvents.Add(gameNightEvent);
      await db.SaveChangesAsync();

      var eventGame = new GameNightEventGame
      {
        GameNightEventId = gameNightEvent.Id,
        GameId = game.Id,
        UserId = ownerUser.Id
      };
      db.GameNightEventGames.Add(eventGame);
      await db.SaveChangesAsync();

      var svc = new GameNightEventService(db);

      // Act
      var removed = await svc.RemoveGameFromEventAsync(eventGame.Id, otherUser.Id);

      // Assert
      Assert.That(removed, Is.False);
      var count = await db.GameNightEventGames.CountAsync();
      Assert.That(count, Is.EqualTo(1));
    }

    [Test]
    public async Task PlaygroupHasEventOnDateAsync_WhenSameDayEventExists_ReturnsTrue()
    {
      // Arrange
      await using var conn = new SqliteConnection("DataSource=:memory:");
      await conn.OpenAsync();
      await using var db = await CreateSqliteDbAsync(conn);

      var user = new User
      {
        Id = "user-1",
        UserName = "user1@test.com",
        Email = "user1@test.com"
      };
      db.Users.Add(user);

      var playgroup = new Playgroup
      {
        Name = "Test Group",
        CreatedByUserId = user.Id,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
      };
      db.Playgroups.Add(playgroup);
      await db.SaveChangesAsync();

      var existingEvent = new GameNightEvent
      {
        PlaygroupId = playgroup.Id,
        CreatedByUserId = user.Id,
        Title = "Existing Event",
        EventDateTime = new DateTime(2026, 3, 20, 18, 0, 0),
        Description = "Test",
        CreatedAt = DateTime.UtcNow
      };
      db.GameNightEvents.Add(existingEvent);
      await db.SaveChangesAsync();

      var svc = new GameNightEventService(db);

      // Act
      var hasSameDay = await svc.PlaygroupHasEventOnDateAsync(
        playgroup.Id,
        new DateTime(2026, 3, 20, 20, 30, 0));

      // Assert
      Assert.That(hasSameDay, Is.True);
    }

    [Test]
    public async Task CancelEventAsync_WhenCreatorCancels_RemovesEventAndRelatedEventGames()
    {
      // Arrange
      await using var conn = new SqliteConnection("DataSource=:memory:");
      await conn.OpenAsync();
      await using var db = await CreateSqliteDbAsync(conn);

      var user = new User
      {
        Id = "user-1",
        UserName = "user1@test.com",
        Email = "user1@test.com"
      };
      db.Users.Add(user);

      var playgroup = new Playgroup
      {
        Name = "Test Group",
        CreatedByUserId = user.Id,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
      };
      db.Playgroups.Add(playgroup);

      var game = new Game
      {
        BggGameId = 505,
        Name = "Terraforming Mars",
        LastSyncedAt = DateTime.UtcNow
      };
      db.Games.Add(game);
      await db.SaveChangesAsync();

      var gameNightEvent = new GameNightEvent
      {
        PlaygroupId = playgroup.Id,
        CreatedByUserId = user.Id,
        Title = "Cancelable Event",
        EventDateTime = DateTime.UtcNow.AddDays(2),
        Description = "Test",
        CreatedAt = DateTime.UtcNow
      };
      db.GameNightEvents.Add(gameNightEvent);
      await db.SaveChangesAsync();

      db.GameNightEventGames.Add(new GameNightEventGame
      {
        GameNightEventId = gameNightEvent.Id,
        GameId = game.Id,
        UserId = user.Id
      });
      await db.SaveChangesAsync();

      var svc = new GameNightEventService(db);

      // Act
      var cancelled = await svc.CancelEventAsync(gameNightEvent.Id, user.Id);

      // Assert
      Assert.That(cancelled, Is.True);
      Assert.That(await db.GameNightEvents.CountAsync(), Is.EqualTo(0));
      Assert.That(await db.GameNightEventGames.CountAsync(), Is.EqualTo(0));
    }
  }
}