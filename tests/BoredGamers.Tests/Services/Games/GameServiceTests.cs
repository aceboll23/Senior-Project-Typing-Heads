using System;
using System.Threading.Tasks;
using BoredGamers.Data;
using BoredGamers.Services.Bgg;
using BoredGamers.Services.Games;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;

namespace BoredGamers.Tests.Services.Games
{
  [TestFixture]
  public class GameServiceTests
  {
    private static ApplicationDbContext CreateDb()
    {
      var options = new DbContextOptionsBuilder<ApplicationDbContext>()
          .UseInMemoryDatabase(Guid.NewGuid().ToString())
          .Options;

      return new ApplicationDbContext(options);
    }

    [Test]
    public async Task GetBrowseGamesAsync_ReturnsGamesFromDatabase_UpToLimit()
    {
      //Arrange
      await using var db = CreateDb();

      db.Games.Add(new BoredGamers.Models.Game { BggGameId = 1, Name = "Alpha" });
      db.Games.Add(new BoredGamers.Models.Game { BggGameId = 2, Name = "Beta" });
      db.Games.Add(new BoredGamers.Models.Game { BggGameId = 3, Name = "Gamma" });

      await db.SaveChangesAsync();

      var bgg = new Mock<IBggClient>();
      var service = new GameService(db, bgg.Object);

      //Act
      var results = await service.GetBrowseGamesAsync(1, 2);

      //Assert
      Assert.That(results.Count, Is.EqualTo(2));
      Assert.That(results.Any(g => g.Name == "Alpha"), Is.True);
      Assert.That(results.Any(g => g.Name == "Beta") || results.Any(g => g.Name == "Gamma"), Is.True);
    }

    [Test]
    public async Task GetBrowseGamesFilteredAsync_FiltersByMinimumRating()
    {
      //Arrange
      await using var db = CreateDb();

      db.Games.Add(new BoredGamers.Models.Game
      {
        BggGameId = 1,
        Name = "Low Rated Game",
        AverageRating = 5.5m
      });

      db.Games.Add(new BoredGamers.Models.Game
      {
        BggGameId = 12,
        Name = "High Rated Game",
        AverageRating = 8.2m
      });

      await db.SaveChangesAsync();

      var bgg = new Mock<IBggClient>();
      var service = new GameService(db, bgg.Object);

      //Act
      var results = await service.GetBrowseGamesFilteredAsync(
        page: 1,
        pageSize: 20,
        minPlayTime: null,
        maxPlayTime: null,
        playerCount: null,
        minRating: 7.0m);

        //Assert
        Assert.That(results.Count, Is.EqualTo(1));
        Assert.That(results[0].Name, Is.EqualTo("High Rated Game"));
    }

  }
}