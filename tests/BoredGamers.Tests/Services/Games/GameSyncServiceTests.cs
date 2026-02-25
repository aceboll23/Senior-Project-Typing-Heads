using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BoredGamers.Data;
using BoredGamers.Models;
using BoredGamers.Services.Bgg;
using BoredGamers.Services.Games;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace BoredGamers.Tests.Services.Games
{
  [TestFixture]
  public class GameSyncServiceTests
  {
    private static ApplicationDbContext CreateDb()
    {
      var options = new DbContextOptionsBuilder<ApplicationDbContext>()
          .UseInMemoryDatabase(Guid.NewGuid().ToString())
          .Options;

      return new ApplicationDbContext(options);
    }

    [Test]
    public async Task SyncTopRankedAsync_InsertsNewGames_AndPersistsNewFields()
    {
      // Arrange
      await using var db = CreateDb();

      var bgg = new Mock<IBggClient>(MockBehavior.Strict);
      var logger = new Mock<ILogger<GameSyncService>>();

      var top = new List<BggTopGame>
            {
                new BggTopGame { BggGameId = 1, Name = "Game One" },
                new BggTopGame { BggGameId = 2, Name = "Game Two" }
            };

      var details = new Dictionary<int, BggGameDetails>
      {
        [1] = new BggGameDetails
        {
          Name = "Game One",
          Description = "Desc 1",
          MinPlayers = 1,
          MaxPlayers = 4,
          PlayTime = 60,
          YearPublished = 2001,
          ThumbnailUrl = "thumb1",
          ImageUrl = "img1",
          AverageRating = 8.25m,
          UsersRated = 1000
        },
        [2] = new BggGameDetails
        {
          Name = "Game Two",
          Description = "Desc 2",
          MinPlayers = 2,
          MaxPlayers = 5,
          PlayTime = 90,
          YearPublished = 2002,
          ThumbnailUrl = "thumb2",
          ImageUrl = "img2",
          AverageRating = 7.50m,
          UsersRated = 2000
        }
      };

      bgg.Setup(x => x.GetTopRankedGamesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
         .ReturnsAsync(top);

      bgg.Setup(x => x.GetGameDetailsAsync(It.IsAny<IEnumerable<int>>(), It.IsAny<CancellationToken>()))
         .ReturnsAsync(details);

      var svc = new GameSyncService(db, bgg.Object, logger.Object);

      // Act
      var changes = await svc.SyncTopRankedAsync(limit: 2);

      // Assert
      Assert.That(changes, Is.EqualTo(2));

      var saved = await db.Games
          .OrderBy(g => g.BggGameId)
          .ToListAsync();

      Assert.That(saved.Count, Is.EqualTo(2));

      var g1 = saved[0];
      Assert.That(g1.BggGameId, Is.EqualTo(1));
      Assert.That(g1.Name, Is.EqualTo("Game One"));
      Assert.That(g1.Description, Is.EqualTo("Desc 1"));
      Assert.That(g1.MinPlayers, Is.EqualTo(1));
      Assert.That(g1.MaxPlayers, Is.EqualTo(4));
      Assert.That(g1.PlayTime, Is.EqualTo(60));

      var g2 = saved[1];
      Assert.That(g2.BggGameId, Is.EqualTo(2));
      Assert.That(g2.Name, Is.EqualTo("Game Two"));
      Assert.That(g2.Description, Is.EqualTo("Desc 2"));
      Assert.That(g2.MinPlayers, Is.EqualTo(2));
      Assert.That(g2.MaxPlayers, Is.EqualTo(5));
      Assert.That(g2.PlayTime, Is.EqualTo(90));

      bgg.VerifyAll();
    }

    [Test]
    public async Task SyncTopRankedAsync_RunningTwice_UpdatesExistingGame_DoesNotDuplicate()
    {
      // Arrange
      await using var db = CreateDb();

      var bgg = new Mock<IBggClient>(MockBehavior.Strict);
      var logger = new Mock<ILogger<GameSyncService>>();

      var initialTop = new List<BggTopGame>
    {
        new BggTopGame { BggGameId = 10, Name = "Original Name" }
    };

      var initialDetails = new Dictionary<int, BggGameDetails>
      {
        [10] = new BggGameDetails
        {
          Name = "Original Name",
          Description = "Original Desc",
          MinPlayers = 2,
          MaxPlayers = 4,
          PlayTime = 60,
          UsersRated = 500
        }
      };

      var updatedTop = new List<BggTopGame>
    {
        new BggTopGame { BggGameId = 10, Name = "Updated Name" }
    };

      var updatedDetails = new Dictionary<int, BggGameDetails>
      {
        [10] = new BggGameDetails
        {
          Name = "Updated Name",
          Description = "Updated Desc",
          MinPlayers = 1,
          MaxPlayers = 5,
          PlayTime = 120,
          UsersRated = 1000
        }
      };

      // First run setup
      bgg.Setup(x => x.GetTopRankedGamesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
         .ReturnsAsync(initialTop);

      bgg.Setup(x => x.GetGameDetailsAsync(It.IsAny<IEnumerable<int>>(), It.IsAny<CancellationToken>()))
         .ReturnsAsync(initialDetails);

      var svc = new GameSyncService(db, bgg.Object, logger.Object);

      await svc.SyncTopRankedAsync(1);

      // Reset mock for second run
      bgg.Reset();

      bgg.Setup(x => x.GetTopRankedGamesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
         .ReturnsAsync(updatedTop);

      bgg.Setup(x => x.GetGameDetailsAsync(It.IsAny<IEnumerable<int>>(), It.IsAny<CancellationToken>()))
         .ReturnsAsync(updatedDetails);

      // Act - second run
      await svc.SyncTopRankedAsync(1);

      // Assert
      var games = await db.Games.ToListAsync();

      Assert.That(games.Count, Is.EqualTo(1), "Should not duplicate existing game.");

      var game = games.Single();

      Assert.That(game.Name, Is.EqualTo("Updated Name"));
      Assert.That(game.Description, Is.EqualTo("Updated Desc"));
      Assert.That(game.MinPlayers, Is.EqualTo(1));
      Assert.That(game.MaxPlayers, Is.EqualTo(5));
      Assert.That(game.PlayTime, Is.EqualTo(120));
      Assert.That(game.BggNumVoters, Is.EqualTo(1000));
    }

    [Test]
    public async Task SyncTopRankedAsync_WhenBggReturnsEmpty_DoesNotModifyDatabase()
    {
      // Arrange
      await using var db = CreateDb();

      // Seed an existing game manually
      db.Games.Add(new Game
      {
        BggGameId = 99,
        Name = "Existing Game",
        Description = "Existing Desc",
        MinPlayers = 2,
        MaxPlayers = 4,
        PlayTime = 60
      });

      await db.SaveChangesAsync();

      var bgg = new Mock<IBggClient>();
      var logger = new Mock<ILogger<GameSyncService>>();

      bgg.Setup(x => x.GetTopRankedGamesAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
         .ReturnsAsync(new List<BggTopGame>()); // EMPTY

      var svc = new GameSyncService(db, bgg.Object, logger.Object);

      // Act
      var result = await svc.SyncTopRankedAsync(100);

      // Assert
      Assert.That(result, Is.EqualTo(0));

      var games = await db.Games.ToListAsync();

      Assert.That(games.Count, Is.EqualTo(1));
      Assert.That(games[0].Name, Is.EqualTo("Existing Game"));
    }

    [Test]
    public async Task SyncByIdsAsync_WhenGivenOnlyInvalidIds_Returns0_AndDoesNotCallBgg()
    {
      // Arrange
      await using var db = CreateDb();

      var bgg = new Mock<IBggClient>(MockBehavior.Strict);
      var logger = new Mock<ILogger<GameSyncService>>();
      var svc = new GameSyncService(db, bgg.Object, logger.Object);

      // Act
      var result = await svc.SyncByIdsAsync(new[] { 0, -1, -50 });

      // Assert
      Assert.That(result, Is.EqualTo(0));
      Assert.That(await db.Games.CountAsync(), Is.EqualTo(0));

      // No BGG calls should happen
      bgg.VerifyNoOtherCalls();
    }
  }
}