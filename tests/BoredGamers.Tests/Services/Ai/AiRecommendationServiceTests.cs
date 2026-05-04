using System;
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
}
