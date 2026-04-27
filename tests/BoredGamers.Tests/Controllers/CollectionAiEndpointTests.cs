using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using BoredGamers.Controllers;
using BoredGamers.Data;
using BoredGamers.Models;
using BoredGamers.Services.Ai;
using BoredGamers.Services.Collections;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;

namespace BoredGamers.Tests.Controllers;

[TestFixture]
public class CollectionAiEndpointTests
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

    private static Mock<UserManager<User>> CreateUserManagerMock()
    {
        var store = new Mock<IUserStore<User>>();
        return new Mock<UserManager<User>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
    }

    private static CollectionController CreateController(
        ApplicationDbContext db,
        Mock<UserManager<User>> mockUserManager,
        Mock<IAiRecommendationService> mockAi,
        string currentUserId)
    {
        var mockCollections = new Mock<IUserCollectionService>();
        var controller = new CollectionController(db, mockCollections.Object, mockUserManager.Object, mockAi.Object);

        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, currentUserId) };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };

        return controller;
    }

    // Helper to read a property by name off an anonymous JSON-shape result
    private static object? GetProp(object? value, string name) =>
        value?.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance)?.GetValue(value);

    [Test]
    public async Task AiRecommendations_UserNotOnAllowlist_ReturnsForbid()
    {
        // Arrange — user exists but their username is NOT in AiAccessPolicy
        await using var conn = new SqliteConnection("DataSource=:memory:");
        await conn.OpenAsync();
        await using var db = await CreateSqliteDbAsync(conn);

        var user = new User { Id = "user-x", UserName = "RandomNonAllowlistedUser", Email = "x@test.com" };

        var mockUserManager = CreateUserManagerMock();
        mockUserManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);

        var mockAi = new Mock<IAiRecommendationService>();
        var controller = CreateController(db, mockUserManager, mockAi, "user-x");

        // Act
        var result = await controller.AiRecommendations(CancellationToken.None);

        // Assert — no AI call should have been made
        Assert.That(result, Is.TypeOf<ForbidResult>());
        mockAi.Verify(
            a => a.GetRecommendationsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task AiRecommendations_AllowlistedUserWithEmptyCollection_ReturnsMessageWithoutCallingAi()
    {
        // Arrange — PersonThree is on the allowlist, no owned games seeded
        await using var conn = new SqliteConnection("DataSource=:memory:");
        await conn.OpenAsync();
        await using var db = await CreateSqliteDbAsync(conn);

        var user = new User { Id = "user-1", UserName = "PersonThree", Email = "p3@test.com" };

        var mockUserManager = CreateUserManagerMock();
        mockUserManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);

        var mockAi = new Mock<IAiRecommendationService>();
        var controller = CreateController(db, mockUserManager, mockAi, "user-1");

        // Act
        var result = await controller.AiRecommendations(CancellationToken.None);

        // Assert — JSON message returned, AI never called
        Assert.That(result, Is.TypeOf<OkObjectResult>());
        var ok = (OkObjectResult)result;
        var message = GetProp(ok.Value, "message") as string;
        Assert.That(message, Is.Not.Null.And.Not.Empty);

        mockAi.Verify(
            a => a.GetRecommendationsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task AiRecommendations_AllowlistedUserWithGames_ReturnsMatchedGames()
    {
        // Arrange — PersonThree owns one game; AI recommends two names, only one matches the local DB
        await using var conn = new SqliteConnection("DataSource=:memory:");
        await conn.OpenAsync();
        await using var db = await CreateSqliteDbAsync(conn);

        var user = new User { Id = "user-1", UserName = "PersonThree", Email = "p3@test.com" };
        db.Users.Add(user);

        var ownedGame = new Game { BggGameId = 5001, Name = "Catan", LastSyncedAt = DateTime.UtcNow };
        var matchingRecommendation = new Game { BggGameId = 5002, Name = "Wingspan", LastSyncedAt = DateTime.UtcNow };
        var unrelatedGame = new Game { BggGameId = 5003, Name = "Some Other Game", LastSyncedAt = DateTime.UtcNow };
        db.Games.AddRange(ownedGame, matchingRecommendation, unrelatedGame);
        await db.SaveChangesAsync();

        db.UserGameCollections.Add(new UserGameCollection
        {
            UserId = user.Id,
            GameId = ownedGame.Id,
            DateAdded = DateTime.UtcNow,
            Status = CollectionStatus.Owned
        });
        await db.SaveChangesAsync();

        var mockUserManager = CreateUserManagerMock();
        mockUserManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);

        var mockAi = new Mock<IAiRecommendationService>();
        // AI returns two names: one is in our DB (Wingspan), one isn't (Bohnanza)
        mockAi
            .Setup(a => a.GetRecommendationsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { "Wingspan", "Bohnanza" });

        var controller = CreateController(db, mockUserManager, mockAi, "user-1");

        // Act
        var result = await controller.AiRecommendations(CancellationToken.None);

        // Assert — returns a games list containing only the matched game
        Assert.That(result, Is.TypeOf<OkObjectResult>());
        var ok = (OkObjectResult)result;

        var games = GetProp(ok.Value, "games") as System.Collections.IEnumerable;
        Assert.That(games, Is.Not.Null);

        var gameList = games!.Cast<object>().ToList();
        Assert.That(gameList.Count, Is.EqualTo(1));
        Assert.That(GetProp(gameList[0], "Name"), Is.EqualTo("Wingspan"));
    }

    [Test]
    public async Task AiRecommendations_AllowlistedUserButNoLocalDbMatches_ReturnsMessage()
    {
        // Arrange — user owns a game, AI recommends names, but none are in the local DB
        await using var conn = new SqliteConnection("DataSource=:memory:");
        await conn.OpenAsync();
        await using var db = await CreateSqliteDbAsync(conn);

        var user = new User { Id = "user-1", UserName = "PersonThree", Email = "p3@test.com" };
        db.Users.Add(user);

        var ownedGame = new Game { BggGameId = 6001, Name = "Catan", LastSyncedAt = DateTime.UtcNow };
        db.Games.Add(ownedGame);
        await db.SaveChangesAsync();

        db.UserGameCollections.Add(new UserGameCollection
        {
            UserId = user.Id,
            GameId = ownedGame.Id,
            DateAdded = DateTime.UtcNow,
            Status = CollectionStatus.Owned
        });
        await db.SaveChangesAsync();

        var mockUserManager = CreateUserManagerMock();
        mockUserManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);

        var mockAi = new Mock<IAiRecommendationService>();
        mockAi
            .Setup(a => a.GetRecommendationsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { "Game Not In Our DB", "Another Game Not In Our DB" });

        var controller = CreateController(db, mockUserManager, mockAi, "user-1");

        // Act
        var result = await controller.AiRecommendations(CancellationToken.None);

        // Assert — friendly message instead of an empty cards array
        Assert.That(result, Is.TypeOf<OkObjectResult>());
        var ok = (OkObjectResult)result;
        var message = GetProp(ok.Value, "message") as string;
        Assert.That(message, Is.Not.Null.And.Not.Empty);
    }
}