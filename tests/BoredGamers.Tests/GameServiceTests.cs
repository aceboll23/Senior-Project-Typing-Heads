using System;
using System.Linq;
using System.Threading.Tasks;
using BoredGamers.Data;
using BoredGamers.Models;
using BoredGamers.Services.Games;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using NUnit.Framework;

namespace BoredGamers.Tests;

[TestFixture]
public class GameServiceTests
{
    private static async Task<ApplicationDbContext> CreateSqliteInMemoryDbAsync()
    {
        //SQLite in-memory DB exists only while the connection is open
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
        db.Games.AddRange(
            new Game { BggGameId = 1, Name = "Catan", BggRank = 25, YearPublished = 1995, ThumbnailUrl = "t1" },
            new Game { BggGameId = 2, Name = "Gloomhaven", BggRank = 1, YearPublished = 2017, ThumbnailUrl = "t2" },
            new Game { BggGameId = 3, Name = "The Crew", BggRank = 50, YearPublished = 2019, ThumbnailUrl = "t3" },
            new Game { BggGameId = 4, Name = "Azul", BggRank = 30, YearPublished = 2017, ThumbnailUrl = "t4" },
            new Game { BggGameId = 5, Name = "Catan: Seafarers", BggRank = 200, YearPublished = 1997, ThumbnailUrl = "t5" }
        );

        await db.SaveChangesAsync();
    }

    [Test]
    public async Task GetTopGamesAsync_ReturnsOrderdByRank_AndRespectsLimit()
    {
        await using var db = await CreateSqliteInMemoryDbAsync();
        await SeedGamesAsync(db);

        var service = new GameService(db);

        var top = await service.GetTopGamesAsync(limit: 3);

        Assert.That(top, Has.Count.EqualTo(3));
        Assert.That(top.Select(g => g.BggRank), Is.EqualTo(new[] { 1, 25, 30 }));
    }

    [Test]
    public async Task SearchGamesAsync_WhenQueryBlank_ReturnsEmptyList()
    {
        await using var db = await CreateSqliteInMemoryDbAsync();
        await SeedGamesAsync(db);

        var service = new GameService(db);

        var results = await service.SearchGamesAsync(query: "  ", limit: 10);

        Assert.That(results, Is.Empty);
    }

    [Test]
    public async Task SearchGamesAsync_FiltersByName_AndRespectsLimit()
    {
        await using var db = await CreateSqliteInMemoryDbAsync();
        await SeedGamesAsync(db);

        var service = new GameService(db);

        var results = await service.SearchGamesAsync(query: "catan", limit: 10);

        //Should return both Catan and Catan: Seafarers, ordered by rank
        Assert.That(results.Select(g => g.Name), Does.Contain("Catan"));
        Assert.That(results.Select(g => g.Name), Does.Contain("Catan: Seafarers"));
        Assert.That(results.Select(g => g.BggRank), Is.Ordered);
    }

    [Test]
    public async Task SearchGamesAsync_RespectsLimit()
    {
        await using var db = await CreateSqliteInMemoryDbAsync();
        await SeedGamesAsync(db);

        var service = new GameService(db);

        var results = await service.SearchGamesAsync(query: "a", limit: 2); //Matches multiple

        Assert.That(results, Has.Count.EqualTo(2));
    }
}
