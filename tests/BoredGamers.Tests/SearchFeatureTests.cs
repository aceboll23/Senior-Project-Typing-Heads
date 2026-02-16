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
public class SearchFeatureTests
{
    // Helper method creating a fake DB in memory to decouple from real db.
    private static async Task<ApplicationDbContext> CreateSqliteInMemoryDbAsync()
    {
        // creates a SQLite database in RAM
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;

        var db = new ApplicationDbContext(options);
        await db.Database.EnsureCreatedAsync();  // Creates (empty) tables

        return db;
    }

    // fills the tables made by "EnsureCreatedAsync()" with fake game data to test against
    private static async Task SeedGamesAsync(ApplicationDbContext db)
    {
        db.Games.AddRange(
            new Game { BggGameId = 1, Name = "Catan", BggRank = 25, YearPublished = 1995, ThumbnailUrl = "t1" },
            new Game { BggGameId = 2, Name = "Gloomhaven", BggRank = 1, YearPublished = 2017, ThumbnailUrl = "t2" },
            new Game { BggGameId = 3, Name = "The Crew", BggRank = 50, YearPublished = 2019, ThumbnailUrl = "t3" },
            new Game { BggGameId = 4, Name = "Azul", BggRank = 30, YearPublished = 2017, ThumbnailUrl = "t4" },
            new Game { BggGameId = 5, Name = "Catan: Seafarers", BggRank = 200, YearPublished = 1997, ThumbnailUrl = "t5" },
            new Game { BggGameId = 6, Name = "Eternal Decks", BggRank = 15, YearPublished = 2024, ThumbnailUrl = "t6" },
            new Game { BggGameId = 7, Name = "Dewan", BggRank = 33, YearPublished = 2025, ThumbnailUrl = "t7" }
        );

        await db.SaveChangesAsync();
    }




    // =====================================================================
    // Search by name — core functionality
    // =====================================================================

    [Test]
    
    public async Task SearchGamesAsync_FindsGameByExactName()
    {
        // THe "baseline" test: if you enter the name of a normal game name, does it return the game?
        // Verify search finds a game when the full name is entered
        await using var db = await CreateSqliteInMemoryDbAsync();
        await SeedGamesAsync(db);
        var service = new GameService(db);

        var results = await service.SearchGamesAsync(query: "Dewan", limit: 10);

        Assert.That(results, Has.Count.EqualTo(1));
        Assert.That(results[0].Name, Is.EqualTo("Dewan"));
    }

    [Test]
    // Users might type "dewan", "DEWAN", or "Dewan" ... all should work
    public async Task SearchGamesAsync_IsCaseInsensitive()
    {
        
        await using var db = await CreateSqliteInMemoryDbAsync();
        await SeedGamesAsync(db);
        var service = new GameService(db);

        var lower = await service.SearchGamesAsync(query: "dewan", limit: 10);
        var upper = await service.SearchGamesAsync(query: "DEWAN", limit: 10);
        var mixed = await service.SearchGamesAsync(query: "DeWaN", limit: 10);

        Assert.That(lower, Has.Count.EqualTo(1));
        Assert.That(upper, Has.Count.EqualTo(1));
        Assert.That(mixed, Has.Count.EqualTo(1));
    }

    [Test]
    // Verify search works with spaces in the query
    public async Task SearchGamesAsync_FindsMultiWordGameName()
    {
        
        await using var db = await CreateSqliteInMemoryDbAsync();
        await SeedGamesAsync(db);
        var service = new GameService(db);

        var results = await service.SearchGamesAsync(query: "Eternal Decks", limit: 10);

        Assert.That(results, Has.Count.EqualTo(1));
        Assert.That(results[0].Name, Is.EqualTo("Eternal Decks"));
    }

    [Test]
    // can return multiple games if multiple games have the entered search term as part of their name
    // Searching "Catan" should find both "Catan" and "Catan: Seafarers"
    public async Task SearchGamesAsync_PartialNameMatchesMultipleGames()
    {
        
        await using var db = await CreateSqliteInMemoryDbAsync();
        await SeedGamesAsync(db);
        var service = new GameService(db);

        var results = await service.SearchGamesAsync(query: "Catan", limit: 10);

        Assert.That(results, Has.Count.EqualTo(2));
        Assert.That(results.Select(g => g.Name), Does.Contain("Catan"));
        Assert.That(results.Select(g => g.Name), Does.Contain("Catan: Seafarers"));
    }

    [Test]
    // Game names can have colons, e.g. "Catan: Seafarers"
    public async Task SearchGamesAsync_SpecialCharactersInName()
    {
        
        await using var db = await CreateSqliteInMemoryDbAsync();
        await SeedGamesAsync(db);
        var service = new GameService(db);

        var results = await service.SearchGamesAsync(query: "Catan: Seafarers", limit: 10);

        Assert.That(results, Has.Count.EqualTo(1));
        Assert.That(results[0].Name, Is.EqualTo("Catan: Seafarers"));
    }



    // =====================================================================
    // Results ordering — results should be sorted by BGG rank
    // =====================================================================

    [Test]
    // When multiple games match, they should come back ordered by rank (lowest = best)
    public async Task SearchGamesAsync_ResultsOrderedByBggRank()
    {
        await using var db = await CreateSqliteInMemoryDbAsync();
        await SeedGamesAsync(db);
        var service = new GameService(db);

        // "a" matches Catan (25), Azul (30), Catan: Seafarers (200), and others
        var results = await service.SearchGamesAsync(query: "a", limit: 50);

        Assert.That(results.Select(g => g.BggRank), Is.Ordered);
    }



    // =====================================================================
    // Empty / no results handling
    // =====================================================================

    [Test]
    // A search for a game that doesn't exist should return empty, not null
    public async Task SearchGamesAsync_NoMatchReturnsEmptyList()
    {
        await using var db = await CreateSqliteInMemoryDbAsync();
        await SeedGamesAsync(db);
        var service = new GameService(db);

        var results = await service.SearchGamesAsync(query: "NonexistentGame12345", limit: 10);

        Assert.That(results, Is.Empty);
        Assert.That(results, Is.Not.Null);
    }

    [Test]
    //If someone passes null as the search query, the service should return an empty list (instead of crashing).
    public async Task SearchGamesAsync_NullQueryReturnsEmptyList()
    {
        await using var db = await CreateSqliteInMemoryDbAsync();
        await SeedGamesAsync(db);
        var service = new GameService(db);

        var results = await service.SearchGamesAsync(query: null, limit: 10);

        Assert.That(results, Is.Empty);
    }

    [Test]
    // if the search is only spaces and tabs, it should be treated the same as it was empty.
    public async Task SearchGamesAsync_WhitespaceOnlyQueryReturnsEmptyList()
    {
        await using var db = await CreateSqliteInMemoryDbAsync();
        await SeedGamesAsync(db);
        var service = new GameService(db);

        var results = await service.SearchGamesAsync(query: "   ", limit: 10);

        Assert.That(results, Is.Empty);
    }

}
