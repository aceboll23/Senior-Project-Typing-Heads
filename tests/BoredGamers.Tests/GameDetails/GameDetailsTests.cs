using System.Threading.Tasks;
using BoredGamers.Data;
using BoredGamers.Models;
using BoredGamers.Services.Games;
using BoredGamers.Tests.TestDoubles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using NUnit.Framework;

namespace BoredGamers.Tests.GameDetails;

[TestFixture]
public class GameDetailsTests
{
    // Same helper pattern as SearchFeatureTests — creates a throwaway SQLite DB in memory
    private static async Task<ApplicationDbContext> CreateSqliteInMemoryDbAsync()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;

        var db = new ApplicationDbContext(options);
        await db.Database.EnsureCreatedAsync();

        return db;
    }

    private static async Task SeedGamesAsync(ApplicationDbContext db)
    {
        // BggRank was removed by Ian — games now use AverageRating + BggNumVoters instead
        db.Games.AddRange(
            new Game
            {
                BggGameId = 1,
                Name = "Catan",
                YearPublished = 1995,
                ThumbnailUrl = "t1",
                ImageUrl = "img1",
                AverageRating = 7.2m,
                BggNumVoters = 5000,
                Description = "Trade and build settlements",
                MinPlayers = 3,
                MaxPlayers = 4,
                PlayTime = 60
            },
            new Game
            {
                BggGameId = 2,
                Name = "Gloomhaven",
                YearPublished = 2017,
                ThumbnailUrl = "t2",
                ImageUrl = "img2",
                AverageRating = 8.7m,
                BggNumVoters = 12000,
                Description = "Tactical combat dungeon crawler",
                MinPlayers = 1,
                MaxPlayers = 4,
                PlayTime = 120
            },
            new Game
            {
                BggGameId = 3,
                Name = "Dewan",
                YearPublished = 2025,
                ThumbnailUrl = "t3",
                ImageUrl = null,
                AverageRating = 6.5m,
                BggNumVoters = null,
                Description = null,
                MinPlayers = null,
                MaxPlayers = null,
                PlayTime = null
            }
        );
        await db.SaveChangesAsync();
    }

    // =====================================================================
    // GetGameByIdAsync — fetching a single game by its database Id
    // =====================================================================

    [Test]
    // Can we fetch a game that exists in the database?
    public async Task GetGameByIdAsync_ReturnsCorrectGame()
    {
        await using var db = await CreateSqliteInMemoryDbAsync();
        await SeedGamesAsync(db);
        var service = new GameService(db, new FakeBggClient());

        // Get the first game's Id (assigned by the database)
        var allGames = await db.Games.ToListAsync();
        var expectedGame = allGames[0]; // Catan

        var result = await service.GetGameByIdAsync(expectedGame.Id);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Name, Is.EqualTo("Catan"));
        Assert.That(result.AverageRating, Is.EqualTo(7.2m));
    }

    [Test]
    // If the Id doesn't exist, should return null (not crash)
    public async Task GetGameByIdAsync_ReturnsNullForNonExistentId()
    {
        await using var db = await CreateSqliteInMemoryDbAsync();
        await SeedGamesAsync(db);
        var service = new GameService(db, new FakeBggClient());

        var result = await service.GetGameByIdAsync(99999);

        Assert.That(result, Is.Null);
    }

    [Test]
    // Make sure all the game fields come back correctly (including new fields Ian added)
    public async Task GetGameByIdAsync_ReturnsAllGameFields()
    {
        await using var db = await CreateSqliteInMemoryDbAsync();
        await SeedGamesAsync(db);
        var service = new GameService(db, new FakeBggClient());

        var allGames = await db.Games.ToListAsync();
        var gloomhaven = allGames[1]; // Gloomhaven

        var result = await service.GetGameByIdAsync(gloomhaven.Id);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Name, Is.EqualTo("Gloomhaven"));
        Assert.That(result.BggGameId, Is.EqualTo(2));
        Assert.That(result.YearPublished, Is.EqualTo(2017));
        Assert.That(result.ThumbnailUrl, Is.EqualTo("t2"));
        Assert.That(result.ImageUrl, Is.EqualTo("img2"));
        Assert.That(result.AverageRating, Is.EqualTo(8.7m));
        Assert.That(result.BggNumVoters, Is.EqualTo(12000));
        Assert.That(result.Description, Is.EqualTo("Tactical combat dungeon crawler"));
        Assert.That(result.MinPlayers, Is.EqualTo(1));
        Assert.That(result.MaxPlayers, Is.EqualTo(4));
        Assert.That(result.PlayTime, Is.EqualTo(120));
    }

    [Test]
    // Game with null optional fields should still return correctly
    public async Task GetGameByIdAsync_HandlesNullFields()
    {
        await using var db = await CreateSqliteInMemoryDbAsync();
        await SeedGamesAsync(db);
        var service = new GameService(db, new FakeBggClient());

        var allGames = await db.Games.ToListAsync();
        var dewan = allGames[2]; // Dewan has several null fields

        var result = await service.GetGameByIdAsync(dewan.Id);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Name, Is.EqualTo("Dewan"));
        Assert.That(result.ImageUrl, Is.Null);
        Assert.That(result.BggNumVoters, Is.Null);
        Assert.That(result.Description, Is.Null);
        Assert.That(result.MinPlayers, Is.Null);
        Assert.That(result.MaxPlayers, Is.Null);
        Assert.That(result.PlayTime, Is.Null);
    }
}
