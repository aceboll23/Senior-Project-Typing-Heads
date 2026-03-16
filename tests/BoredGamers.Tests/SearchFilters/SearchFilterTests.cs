using System.Threading.Tasks;
using BoredGamers.Data;
using BoredGamers.Models;
using BoredGamers.Services.Games;
using BoredGamers.Tests.TestDoubles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using NUnit.Framework;

namespace BoredGamers.Tests.SearchFilters;

[TestFixture]
public class SearchFilterTests
{
    // Same SQLite in-memory helper used in other test files
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

    // Seed data designed to test each filter type
    // Games have a spread of play times, player counts, and ratings
    private static async Task SeedGamesAsync(ApplicationDbContext db)
    {
        db.Games.AddRange(
            new Game
            {
                BggGameId = 1,
                Name = "Quick Card Game",
                AverageRating = 6.5m,
                MinPlayers = 2,
                MaxPlayers = 4,
                PlayTime = 20,       // Under 30 min
                BggNumVoters = 500
            },
            new Game
            {
                BggGameId = 2,
                Name = "Catan",
                AverageRating = 7.2m,
                MinPlayers = 3,
                MaxPlayers = 4,
                PlayTime = 60,       // 30-60 min
                BggNumVoters = 5000
            },
            new Game
            {
                BggGameId = 3,
                Name = "Ticket to Ride",
                AverageRating = 7.5m,
                MinPlayers = 2,
                MaxPlayers = 5,
                PlayTime = 45,       // 30-60 min
                BggNumVoters = 8000
            },
            new Game
            {
                BggGameId = 4,
                Name = "Gloomhaven",
                AverageRating = 8.7m,
                MinPlayers = 1,
                MaxPlayers = 4,
                PlayTime = 120,      // 1-2 hours
                BggNumVoters = 12000
            },
            new Game
            {
                BggGameId = 5,
                Name = "Twilight Imperium",
                AverageRating = 8.5m,
                MinPlayers = 3,
                MaxPlayers = 6,
                PlayTime = 240,      // 2+ hours
                BggNumVoters = 3000
            },
            new Game
            {
                BggGameId = 6,
                Name = "Solo Quest",
                AverageRating = 5.0m,
                MinPlayers = 1,
                MaxPlayers = 1,      // Solo only
                PlayTime = 30,
                BggNumVoters = 200
            },
            new Game
            {
                BggGameId = 7,
                Name = "Party Blitz",
                AverageRating = 6.0m,
                MinPlayers = 4,
                MaxPlayers = 10,     // Big group game
                PlayTime = 15,       // Under 30 min
                BggNumVoters = 1000
            },
            new Game
            {
                BggGameId = 8,
                Name = "Unknown Playtime",
                AverageRating = 7.0m,
                MinPlayers = 2,
                MaxPlayers = 4,
                PlayTime = null,     // No play time data
                BggNumVoters = 100
            }
        );
        await db.SaveChangesAsync();
    }

    // =====================================================================
    // Play Time Filter
    // =====================================================================

    [Test]
    // Under 30 min should return Quick Card Game, Party Blitz
    public async Task Filter_PlayTimeUnder30_ReturnsShortGames()
    {
        await using var db = await CreateSqliteInMemoryDbAsync();
        await SeedGamesAsync(db);
        var service = new GameService(db, new FakeBggClient());

        var results = await service.SearchGamesFilteredAsync(
            query: null, maxPlayTime: 29, minPlayTime: null,
            playerCount: null, minRating: null, limit: 50);

        Assert.That(results, Has.Count.EqualTo(2));
        Assert.That(results.Select(g => g.Name),
            Is.EquivalentTo(new[] { "Quick Card Game", "Party Blitz" }));
    }

    [Test]
    // 30-60 min should return Catan, Ticket to Ride, Solo Quest
    public async Task Filter_PlayTime30To60_ReturnsMediumGames()
    {
        await using var db = await CreateSqliteInMemoryDbAsync();
        await SeedGamesAsync(db);
        var service = new GameService(db, new FakeBggClient ());

        var results = await service.SearchGamesFilteredAsync(
            query: null, minPlayTime: 30, maxPlayTime: 60,
            playerCount: null, minRating: null, limit: 50);

        Assert.That(results, Has.Count.EqualTo(3));
        Assert.That(results.Select(g => g.Name),
            Has.Member("Catan")
            .And.Member("Ticket to Ride")
            .And.Member("Solo Quest"));
    }

    [Test]
    // 1-2 hours should return Gloomhaven (120 min)
    public async Task Filter_PlayTime60To120_ReturnsLongGames()
    {
        await using var db = await CreateSqliteInMemoryDbAsync();
        await SeedGamesAsync(db);
        var service = new GameService(db, new FakeBggClient ());

        var results = await service.SearchGamesFilteredAsync(
            query: null, minPlayTime: 61, maxPlayTime: 120,
            playerCount: null, minRating: null, limit: 50);

        Assert.That(results, Has.Count.EqualTo(1));
        Assert.That(results[0].Name, Is.EqualTo("Gloomhaven"));
    }

    [Test]
    // 2+ hours should return Twilight Imperium (240 min)
    public async Task Filter_PlayTimeOver120_ReturnsVeryLongGames()
    {
        await using var db = await CreateSqliteInMemoryDbAsync();
        await SeedGamesAsync(db);
        var service = new GameService(db, new FakeBggClient());

        var results = await service.SearchGamesFilteredAsync(
            query: null, minPlayTime: 121, maxPlayTime: null,
            playerCount: null, minRating: null, limit: 50);

        Assert.That(results, Has.Count.EqualTo(1));
        Assert.That(results[0].Name, Is.EqualTo("Twilight Imperium"));
    }

    // =====================================================================
    // Player Count Filter
    // =====================================================================

    [Test]
    // Filtering for 1 player should return games where MinPlayers <= 1 <= MaxPlayers
    public async Task Filter_PlayerCount1_ReturnsSoloGames()
    {
        await using var db = await CreateSqliteInMemoryDbAsync();
        await SeedGamesAsync(db);
        var service = new GameService(db, new FakeBggClient());

        var results = await service.SearchGamesFilteredAsync(
            query: null, minPlayTime: null, maxPlayTime: null,
            playerCount: 1, minRating: null, limit: 50);

        Assert.That(results, Has.Count.EqualTo(2));
        Assert.That(results.Select(g => g.Name),
            Is.EquivalentTo(new[] { "Gloomhaven", "Solo Quest" }));
    }

    [Test]
    // Filtering for 6 players should return games that support 6
    public async Task Filter_PlayerCount6_ReturnsLargeGroupGames()
    {
        await using var db = await CreateSqliteInMemoryDbAsync();
        await SeedGamesAsync(db);
        var service = new GameService(db, new FakeBggClient());

        var results = await service.SearchGamesFilteredAsync(
            query: null, minPlayTime: null, maxPlayTime: null,
            playerCount: 6, minRating: null, limit: 50);

        Assert.That(results, Has.Count.EqualTo(2));
        Assert.That(results.Select(g => g.Name),
            Is.EquivalentTo(new[] { "Twilight Imperium", "Party Blitz" }));
    }

    [Test]
    // Filtering for 10 players should only return Party Blitz
    public async Task Filter_PlayerCount10_ReturnsOnlyBigPartyGames()
    {
        await using var db = await CreateSqliteInMemoryDbAsync();
        await SeedGamesAsync(db);
        var service = new GameService(db, new FakeBggClient());

        var results = await service.SearchGamesFilteredAsync(
            query: null, minPlayTime: null, maxPlayTime: null,
            playerCount: 10, minRating: null, limit: 50);

        Assert.That(results, Has.Count.EqualTo(1));
        Assert.That(results[0].Name, Is.EqualTo("Party Blitz"));
    }

    // =====================================================================
    // Rating Filter
    // =====================================================================

    [Test]
    // Min rating 8.0 should return Gloomhaven (8.7) and Twilight Imperium (8.5)
    public async Task Filter_MinRating8_ReturnsHighRatedGames()
    {
        await using var db = await CreateSqliteInMemoryDbAsync();
        await SeedGamesAsync(db);
        var service = new GameService(db, new FakeBggClient());

        var results = await service.SearchGamesFilteredAsync(
            query: null, minPlayTime: null, maxPlayTime: null,
            playerCount: null, minRating: 8.0m, limit: 50);

        Assert.That(results, Has.Count.EqualTo(2));
        Assert.That(results.Select(g => g.Name),
            Is.EquivalentTo(new[] { "Gloomhaven", "Twilight Imperium" }));
    }

    [Test]
    // Min rating 7.0 should return 4 games (7.0, 7.2, 7.5, 8.5, 8.7)
    public async Task Filter_MinRating7_ReturnsFiveGames()
    {
        await using var db = await CreateSqliteInMemoryDbAsync();
        await SeedGamesAsync(db);
        var service = new GameService(db, new FakeBggClient());

        var results = await service.SearchGamesFilteredAsync(
            query: null, minPlayTime: null, maxPlayTime: null,
            playerCount: null, minRating: 7.0m, limit: 50);

        Assert.That(results, Has.Count.EqualTo(5));
    }

    // =====================================================================
    // Combined Filters
    // =====================================================================

    [Test]
    // 4 players + under 60 min should return Quick Card Game, Catan, Ticket to Ride
    public async Task Filter_Combined_PlayerCountAndPlayTime()
    {
        await using var db = await CreateSqliteInMemoryDbAsync();
        await SeedGamesAsync(db);
        var service = new GameService(db, new FakeBggClient());

        var results = await service.SearchGamesFilteredAsync(
            query: null, minPlayTime: null, maxPlayTime: 60,
            playerCount: 4, minRating: null, limit: 50);

        Assert.That(results, Has.Count.EqualTo(4));
Assert.That(results.Select(g => g.Name),
    Is.EquivalentTo(new[] { "Quick Card Game", "Catan", "Ticket to Ride", "Party Blitz" }));
    }

    [Test]
    // 4 players + rating >= 7.0 + under 120 min = Catan, Ticket to Ride
    public async Task Filter_Combined_AllThreeFilters()
    {
        await using var db = await CreateSqliteInMemoryDbAsync();
        await SeedGamesAsync(db);
        var service = new GameService(db, new FakeBggClient());

        var results = await service.SearchGamesFilteredAsync(
            query: null, minPlayTime: null, maxPlayTime: 120,
            playerCount: 4, minRating: 7.0m, limit: 50);

        Assert.That(results, Has.Count.EqualTo(3));
        Assert.That(results.Select(g => g.Name),
            Is.EquivalentTo(new[] { "Catan", "Ticket to Ride", "Gloomhaven" }));
    }

    // =====================================================================
    // Filters + Search Query Combined
    // =====================================================================

    [Test]
    // Search for "t" + min rating 7 should return Ticket to Ride and Twilight Imperium
    public async Task Filter_WithSearchQuery_CombinesNameAndFilters()
    {
        await using var db = await CreateSqliteInMemoryDbAsync();
        await SeedGamesAsync(db);
        var service = new GameService(db, new FakeBggClient());

        var results = await service.SearchGamesFilteredAsync(
            query: "t", minPlayTime: null, maxPlayTime: null,
            playerCount: null, minRating: 7.0m, limit: 50);

        Assert.That(results, Has.Count.EqualTo(4));
        Assert.That(results.Select(g => g.Name),
            Is.EquivalentTo(new[] { "Ticket to Ride", "Twilight Imperium", "Catan", "Unknown Playtime" }));

    }

    // =====================================================================
    // No Filters — should return all games (same as regular search)
    // =====================================================================

    [Test]
    // No filters and no query should return all games
    public async Task Filter_NoFilters_ReturnsAllGames()
    {
        await using var db = await CreateSqliteInMemoryDbAsync();
        await SeedGamesAsync(db);
        var service = new GameService(db, new FakeBggClient());

        var results = await service.SearchGamesFilteredAsync(
            query: null, minPlayTime: null, maxPlayTime: null,
            playerCount: null, minRating: null, limit: 50);

        Assert.That(results, Has.Count.EqualTo(8));
    }

    // =====================================================================
    // No Matches
    // =====================================================================

    [Test]
    // Impossible filter combination should return empty list
    public async Task Filter_NoMatches_ReturnsEmptyList()
    {
        await using var db = await CreateSqliteInMemoryDbAsync();
        await SeedGamesAsync(db);
        var service = new GameService(db, new FakeBggClient());

        // 10 players + rating >= 9 — no game matches both
        var results = await service.SearchGamesFilteredAsync(
            query: null, minPlayTime: null, maxPlayTime: null,
            playerCount: 10, minRating: 9.0m, limit: 50);

        Assert.That(results, Is.Empty);
    }

    // =====================================================================
    // Games with null fields should be excluded by filters, not crash
    // =====================================================================

    [Test]
    // "Unknown Playtime" has null PlayTime — should NOT appear in play time filtered results
    public async Task Filter_NullPlayTime_ExcludedFromPlayTimeFilter()
    {
        await using var db = await CreateSqliteInMemoryDbAsync();
        await SeedGamesAsync(db);
        var service = new GameService(db, new FakeBggClient());

        // Get all games with any play time filter — null PlayTime games should be excluded
        var results = await service.SearchGamesFilteredAsync(
            query: null, minPlayTime: 1, maxPlayTime: 999,
            playerCount: null, minRating: null, limit: 50);

        Assert.That(results.Select(g => g.Name), Has.No.Member("Unknown Playtime"));
    }
}