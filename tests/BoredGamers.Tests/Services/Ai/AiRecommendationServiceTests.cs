using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BoredGamers.Data;
using BoredGamers.Models;
using BoredGamers.Services.Ai;
using BoredGamers.Services.Bgg;
using BoredGamers.Services.Games;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;

namespace BoredGamers.Tests.Services.Ai;

[TestFixture]
public class AiRecommendationServiceTests
{
    private const string TestUserId = "test-user-1";

    private static ApplicationDbContext NewInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private static AiRecommendationService BuildSut(
        ApplicationDbContext db,
        Mock<IAiClient>? aiClient = null,
        Mock<IBggClient>? bgg = null,
        Mock<IGameService>? games = null)
    {
        aiClient ??= new Mock<IAiClient>();
        bgg ??= new Mock<IBggClient>();
        games ??= new Mock<IGameService>();
        return new AiRecommendationService(aiClient.Object, db, bgg.Object, games.Object);
    }

    private static async Task SeedOwnedGameAsync(
        ApplicationDbContext db,
        Game game,
        string userId = TestUserId)
    {
        db.Games.Add(game);
        db.UserGameCollections.Add(new UserGameCollection
        {
            UserId = userId,
            GameId = game.Id,
            Status = CollectionStatus.Owned,
            DateAdded = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
    }

    // =====================================================================
    // Legacy method — to be removed once the controller switches to the
    // new (string userId) overload. These tests cover the existing button.
    // =====================================================================

    [Test]
    public async Task GetRecommendationsAsync_Legacy_PassesOwnedGameNamesIntoUserPrompt()
    {
        using var db = NewInMemoryDb();
        var capturedUserPrompt = string.Empty;
        var aiClient = new Mock<IAiClient>();
        aiClient
            .Setup(c => c.GetCompletionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, CancellationToken>((_, user, _) => capturedUserPrompt = user)
            .ReturnsAsync("");

        var sut = BuildSut(db, aiClient: aiClient);

        await sut.GetRecommendationsAsync(new[] { "Catan", "Wingspan" });

        Assert.That(capturedUserPrompt, Does.Contain("Catan"));
        Assert.That(capturedUserPrompt, Does.Contain("Wingspan"));
    }

    [Test]
    public async Task GetRecommendationsAsync_Legacy_ParsesNewlineSeparatedResponseIntoList()
    {
        using var db = NewInMemoryDb();
        var aiClient = new Mock<IAiClient>();
        aiClient
            .Setup(c => c.GetCompletionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Game A\nGame B\nGame C");

        var sut = BuildSut(db, aiClient: aiClient);

        var result = await sut.GetRecommendationsAsync(new[] { "Owned Game" });

        Assert.That(result, Is.EqualTo(new[] { "Game A", "Game B", "Game C" }));
    }

    [Test]
    public async Task GetRecommendationsAsync_Legacy_TrimsWhitespaceAndDropsBlankLines()
    {
        using var db = NewInMemoryDb();
        var aiClient = new Mock<IAiClient>();
        aiClient
            .Setup(c => c.GetCompletionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("  Game A  \n\n   \nGame B\n");

        var sut = BuildSut(db, aiClient: aiClient);

        var result = await sut.GetRecommendationsAsync(new[] { "Owned" });

        Assert.That(result, Is.EqualTo(new[] { "Game A", "Game B" }));
    }

    // =====================================================================
    // TYP-245 v2 — new (string userId) overload.
    // =====================================================================

    // T4 — Prompt includes name, min/max players, play time, and (truncated) description.
    [Test]
    public async Task GetRecommendationsAsync_FullyPopulatedGame_PromptIncludesAllFields()
    {
        using var db = NewInMemoryDb();
        await SeedOwnedGameAsync(db, new Game
        {
            Id = 1,
            BggGameId = 1,
            Name = "TestGameAlpha",
            MinPlayers = 2,
            MaxPlayers = 6,
            PlayTime = 45,
            Description = "TestUniqueDescriptionXyz123 is a fascinating bird-collection card game.",
            LastSyncedAt = DateTime.UtcNow
        });

        var capturedUserPrompt = string.Empty;
        var aiClient = new Mock<IAiClient>();
        aiClient
            .Setup(x => x.GetCompletionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, CancellationToken>((sys, user, ct) => capturedUserPrompt = user)
            .ReturnsAsync(string.Empty);

        var sut = BuildSut(db, aiClient: aiClient);

        await sut.GetRecommendationsAsync(TestUserId);

        Assert.That(capturedUserPrompt, Does.Contain("TestGameAlpha"));
        Assert.That(capturedUserPrompt, Does.Contain("2-6"));
        Assert.That(capturedUserPrompt, Does.Contain("45"));
        Assert.That(capturedUserPrompt, Does.Contain("TestUniqueDescriptionXyz123"));
    }

    // T5 — Game with all-null context fields produces a clean prompt: just the
    // name, no literal "null", no empty parens, no orphan punctuation.
    [Test]
    public async Task GetRecommendationsAsync_GameWithNullFields_PromptOmitsThemGracefully()
    {
        using var db = NewInMemoryDb();
        await SeedOwnedGameAsync(db, new Game
        {
            Id = 1,
            BggGameId = 1,
            Name = "MinimalGame",
            // MinPlayers, MaxPlayers, PlayTime, Description left null.
            LastSyncedAt = DateTime.UtcNow
        });

        var capturedUserPrompt = string.Empty;
        var aiClient = new Mock<IAiClient>();
        aiClient
            .Setup(x => x.GetCompletionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, CancellationToken>((sys, user, ct) => capturedUserPrompt = user)
            .ReturnsAsync(string.Empty);

        var sut = BuildSut(db, aiClient: aiClient);

        await sut.GetRecommendationsAsync(TestUserId);

        Assert.That(capturedUserPrompt, Does.Contain("MinimalGame"));
        Assert.That(capturedUserPrompt, Does.Not.Contain("null").IgnoreCase);
        Assert.That(capturedUserPrompt, Does.Not.Contain("()"));
    }

    // T6 — Top 10 most recent owned games are sent in the prompt; older owned
    // games and wishlist items are excluded.
    [Test]
    public async Task GetRecommendationsAsync_TakesTopTenMostRecentOwnedGamesAndIgnoresWishlist()
    {
        using var db = NewInMemoryDb();
        var baseTime = DateTime.UtcNow;

        // 12 owned games — game01 oldest, game12 most recent.
        for (int i = 1; i <= 12; i++)
        {
            db.Games.Add(new Game
            {
                Id = i,
                BggGameId = i,
                Name = $"Owned-{i:D2}",
                LastSyncedAt = baseTime
            });
            db.UserGameCollections.Add(new UserGameCollection
            {
                UserId = TestUserId,
                GameId = i,
                Status = CollectionStatus.Owned,
                DateAdded = baseTime.AddDays(-12 + i)
            });
        }

        // 2 wishlist items — should be excluded entirely.
        db.Games.Add(new Game { Id = 100, BggGameId = 100, Name = "Wishlist-A", LastSyncedAt = baseTime });
        db.Games.Add(new Game { Id = 101, BggGameId = 101, Name = "Wishlist-B", LastSyncedAt = baseTime });
        db.UserGameCollections.Add(new UserGameCollection
        {
            UserId = TestUserId, GameId = 100,
            Status = CollectionStatus.Wishlist, DateAdded = baseTime
        });
        db.UserGameCollections.Add(new UserGameCollection
        {
            UserId = TestUserId, GameId = 101,
            Status = CollectionStatus.Wishlist, DateAdded = baseTime
        });

        await db.SaveChangesAsync();

        var capturedPrompt = string.Empty;
        var aiClient = new Mock<IAiClient>();
        aiClient
            .Setup(x => x.GetCompletionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, CancellationToken>((sys, user, ct) => capturedPrompt = user)
            .ReturnsAsync(string.Empty);

        var sut = BuildSut(db, aiClient: aiClient);

        await sut.GetRecommendationsAsync(TestUserId);

        // 10 most recent owned: game03 through game12.
        for (int i = 3; i <= 12; i++)
            Assert.That(capturedPrompt, Does.Contain($"Owned-{i:D2}"));

        // Older two owned + both wishlist items must be absent.
        Assert.That(capturedPrompt, Does.Not.Contain("Owned-01"));
        Assert.That(capturedPrompt, Does.Not.Contain("Owned-02"));
        Assert.That(capturedPrompt, Does.Not.Contain("Wishlist-A"));
        Assert.That(capturedPrompt, Does.Not.Contain("Wishlist-B"));
    }

    // T7 — Games the user already owns are filtered out of the result set even
    // if Claude includes them in its response. Fixes the existing silent bug.
    [Test]
    public async Task GetRecommendationsAsync_ExcludesGamesUserAlreadyOwns()
    {
        using var db = NewInMemoryDb();
        var baseTime = DateTime.UtcNow;

        var catan = new Game { Id = 1, BggGameId = 1, Name = "Catan", LastSyncedAt = baseTime };
        var wingspan = new Game { Id = 2, BggGameId = 2, Name = "Wingspan", LastSyncedAt = baseTime };
        db.Games.AddRange(catan, wingspan);
        db.UserGameCollections.Add(new UserGameCollection
        {
            UserId = TestUserId, GameId = catan.Id,
            Status = CollectionStatus.Owned, DateAdded = baseTime
        });
        await db.SaveChangesAsync();

        var aiClient = new Mock<IAiClient>();
        aiClient
            .Setup(x => x.GetCompletionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Catan\nWingspan");

        var sut = BuildSut(db, aiClient: aiClient);

        var result = await sut.GetRecommendationsAsync(TestUserId);

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].Name, Is.EqualTo("Wingspan"));
    }

    // T8 — When Claude returns more unmatched names than the cap, BGG search +
    // promotion happen at most MaxBggPromotions times. Cost / latency control.
    [Test]
    public async Task GetRecommendationsAsync_CapsBggPromotionsAtThree()
    {
        using var db = NewInMemoryDb();
        var baseTime = DateTime.UtcNow;

        // One owned game so the prompt isn't empty (and orchestration runs).
        db.Games.Add(new Game { Id = 1, BggGameId = 1, Name = "OwnedSeed", LastSyncedAt = baseTime });
        db.UserGameCollections.Add(new UserGameCollection
        {
            UserId = TestUserId, GameId = 1,
            Status = CollectionStatus.Owned, DateAdded = baseTime
        });
        await db.SaveChangesAsync();

        // Claude returns 8 names — none in local DB, so all 8 are candidates
        // for BGG promotion. The cap should keep us to 3.
        var aiClient = new Mock<IAiClient>();
        aiClient
            .Setup(x => x.GetCompletionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Promote-1\nPromote-2\nPromote-3\nPromote-4\nPromote-5\nPromote-6\nPromote-7\nPromote-8");

        var bgg = new Mock<IBggClient>();
        bgg
            .Setup(x => x.SearchGamesAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<BggGameDetails> { new() { BggGameId = 999, Name = "PromotedFake" } });

        var games = new Mock<IGameService>();
        games
            .Setup(x => x.SaveGameFromBggAsync(It.IsAny<int>()))
            .ReturnsAsync(new Game { Id = 999, BggGameId = 999, Name = "PromotedFake", LastSyncedAt = baseTime });

        var sut = BuildSut(db, aiClient: aiClient, bgg: bgg, games: games);

        await sut.GetRecommendationsAsync(TestUserId);

        bgg.Verify(
            x => x.SearchGamesAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Exactly(3));
        games.Verify(
            x => x.SaveGameFromBggAsync(It.IsAny<int>()),
            Times.Exactly(3));
    }

    // T9 — Names with no local OR BGG hit are silently dropped, and the
    // surviving entries appear in the order Claude proposed them.
    [Test]
    public async Task GetRecommendationsAsync_DropsUnresolvableNamesAndPreservesOrder()
    {
        using var db = NewInMemoryDb();
        var baseTime = DateTime.UtcNow;

        var ownedSeed = new Game { Id = 1, BggGameId = 1, Name = "OwnedSeed", LastSyncedAt = baseTime };
        var wingspan = new Game { Id = 2, BggGameId = 2, Name = "Wingspan", LastSyncedAt = baseTime };
        db.Games.AddRange(ownedSeed, wingspan);
        db.UserGameCollections.Add(new UserGameCollection
        {
            UserId = TestUserId, GameId = ownedSeed.Id,
            Status = CollectionStatus.Owned, DateAdded = baseTime
        });
        await db.SaveChangesAsync();

        var aiClient = new Mock<IAiClient>();
        aiClient
            .Setup(x => x.GetCompletionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Wingspan\nFakeGameXYZ\nBrass: Birmingham");

        var bgg = new Mock<IBggClient>();
        bgg
            .Setup(x => x.SearchGamesAsync("FakeGameXYZ", It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<BggGameDetails>());
        bgg
            .Setup(x => x.SearchGamesAsync("Brass: Birmingham", It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<BggGameDetails> { new() { BggGameId = 224517, Name = "Brass: Birmingham" } });

        var games = new Mock<IGameService>();
        games
            .Setup(x => x.SaveGameFromBggAsync(224517))
            .ReturnsAsync(new Game { Id = 3, BggGameId = 224517, Name = "Brass: Birmingham", LastSyncedAt = baseTime });

        var sut = BuildSut(db, aiClient: aiClient, bgg: bgg, games: games);

        var result = await sut.GetRecommendationsAsync(TestUserId);

        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(result[0].Name, Is.EqualTo("Wingspan"));
        Assert.That(result[1].Name, Is.EqualTo("Brass: Birmingham"));
    }
}
