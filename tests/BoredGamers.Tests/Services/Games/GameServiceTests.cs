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
      var results = await service.GetBrowseGamesAsync(2);

      //Assert
      Assert.That(results.Count, Is.EqualTo(2));
      Assert.That(results.Any(g => g.Name == "Alpha"), Is.True);
      Assert.That(results.Any(g => g.Name == "Beta") || results.Any(g => g.Name == "Gamma"), Is.True);
    }

  }
}