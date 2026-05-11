using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
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

namespace BoredGamers.Tests.Controllers
{
    [TestFixture]
    public class FriendCollectionControllerTests
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
            string currentUserId)
        {
            var mockCollections = new Mock<IUserCollectionService>();
            var mockAiRecommendations = new Mock<IAiRecommendationService>();
            var mockTransfers = new Mock<BoredGamers.Services.Transfers.IGameTransferService>();
            mockTransfers.Setup(t => t.GetPendingTransfersForUserAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<BoredGamers.Models.ViewModels.PendingTransferViewModel>());
            var controller = new CollectionController(db, mockCollections.Object, mockUserManager.Object, mockAiRecommendations.Object, mockTransfers.Object);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, currentUserId)
            };
            var identity = new ClaimsIdentity(claims, "Test");
            var principal = new ClaimsPrincipal(identity);

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };

            return controller;
        }

        [Test]
        public async Task FriendCollection_UnknownUsername_ReturnsNotFound()
        {
            // Arrange
            await using var conn = new SqliteConnection("DataSource=:memory:");
            await conn.OpenAsync();
            await using var db = await CreateSqliteDbAsync(conn);

            var mockUserManager = CreateUserManagerMock();
            mockUserManager.Setup(x => x.FindByNameAsync("ghost")).ReturnsAsync((User)null!);

            var controller = CreateController(db, mockUserManager, "current-user-id");

            // Act
            var result = await controller.FriendCollection("ghost");

            // Assert
            Assert.That(result, Is.TypeOf<NotFoundResult>());
        }

        [Test]
        public async Task FriendCollection_CurrentUserViewingOwnUsername_RedirectsToIndex()
        {
            // Arrange
            await using var conn = new SqliteConnection("DataSource=:memory:");
            await conn.OpenAsync();
            await using var db = await CreateSqliteDbAsync(conn);

            var currentUser = new User { Id = "current-user-id", UserName = "self", Email = "self@test.com" };

            var mockUserManager = CreateUserManagerMock();
            mockUserManager.Setup(x => x.FindByNameAsync("self")).ReturnsAsync(currentUser);

            var controller = CreateController(db, mockUserManager, "current-user-id");

            // Act
            var result = await controller.FriendCollection("self");

            // Assert
            Assert.That(result, Is.TypeOf<RedirectToActionResult>());
            var redirect = (RedirectToActionResult)result;
            Assert.That(redirect.ActionName, Is.EqualTo("Index"));
        }

        [Test]
        public async Task FriendCollection_FriendWithNoGames_ReturnsViewWithEmptyModel()
        {
            // Arrange
            await using var conn = new SqliteConnection("DataSource=:memory:");
            await conn.OpenAsync();
            await using var db = await CreateSqliteDbAsync(conn);

            var friend = new User { Id = "friend-id", UserName = "friendnoob", Email = "friendnoob@test.com" };

            var mockUserManager = CreateUserManagerMock();
            mockUserManager.Setup(x => x.FindByNameAsync("friendnoob")).ReturnsAsync(friend);

            var controller = CreateController(db, mockUserManager, "current-user-id");

            // Act
            var result = await controller.FriendCollection("friendnoob");

            // Assert
            Assert.That(result, Is.TypeOf<ViewResult>());
            var model = ((ViewResult)result).Model as List<Game>;
            Assert.That(model, Is.Not.Null);
            Assert.That(model!.Count, Is.EqualTo(0));
        }

        [Test]
        public async Task FriendCollection_ReturnsOnlyOwnedGames_WishlistExcluded()
        {
            // Arrange
            await using var conn = new SqliteConnection("DataSource=:memory:");
            await conn.OpenAsync();
            await using var db = await CreateSqliteDbAsync(conn);

            var friend = new User { Id = "friend-id", UserName = "frienduser", Email = "frienduser@test.com" };
            db.Users.Add(friend);

            var ownedGame = new Game { BggGameId = 9001, Name = "Owned Game", LastSyncedAt = DateTime.UtcNow };
            var wishlistGame = new Game { BggGameId = 9002, Name = "Wishlist Game", LastSyncedAt = DateTime.UtcNow };
            db.Games.AddRange(ownedGame, wishlistGame);
            await db.SaveChangesAsync();

            db.UserGameCollections.AddRange(
                new UserGameCollection { UserId = friend.Id, GameId = ownedGame.Id, DateAdded = DateTime.UtcNow, Status = CollectionStatus.Owned },
                new UserGameCollection { UserId = friend.Id, GameId = wishlistGame.Id, DateAdded = DateTime.UtcNow, Status = CollectionStatus.Wishlist }
            );
            await db.SaveChangesAsync();

            var mockUserManager = CreateUserManagerMock();
            mockUserManager.Setup(x => x.FindByNameAsync("frienduser")).ReturnsAsync(friend);

            var controller = CreateController(db, mockUserManager, "current-user-id");

            // Act
            var result = await controller.FriendCollection("frienduser");

            // Assert
            Assert.That(result, Is.TypeOf<ViewResult>());
            var model = ((ViewResult)result).Model as List<Game>;
            Assert.That(model, Is.Not.Null);
            Assert.That(model!.Count, Is.EqualTo(1));
            Assert.That(model[0].Name, Is.EqualTo("Owned Game"));
        }

        [Test]
        public async Task FriendCollection_SetsCorrectViewData()
        {
            // Arrange
            await using var conn = new SqliteConnection("DataSource=:memory:");
            await conn.OpenAsync();
            await using var db = await CreateSqliteDbAsync(conn);

            var friend = new User { Id = "friend-id", UserName = "alice", Email = "alice@test.com" };
            db.Users.Add(friend);

            var game = new Game { BggGameId = 9003, Name = "Alice Game", LastSyncedAt = DateTime.UtcNow };
            db.Games.Add(game);
            await db.SaveChangesAsync();

            db.UserGameCollections.Add(new UserGameCollection
            {
                UserId = friend.Id,
                GameId = game.Id,
                DateAdded = DateTime.UtcNow,
                Status = CollectionStatus.Owned
            });
            await db.SaveChangesAsync();

            var mockUserManager = CreateUserManagerMock();
            mockUserManager.Setup(x => x.FindByNameAsync("alice")).ReturnsAsync(friend);

            var controller = CreateController(db, mockUserManager, "current-user-id");

            // Act
            var result = await controller.FriendCollection("alice");

            // Assert
            Assert.That(result, Is.TypeOf<ViewResult>());
            var view = (ViewResult)result;
            Assert.That(view.ViewData["FriendUsername"], Is.EqualTo("alice"));
            Assert.That(view.ViewData["TotalCount"], Is.EqualTo(1));
            Assert.That(view.ViewData["Page"], Is.EqualTo(1));
        }

        [Test]
        public async Task FriendCollection_MultipleOwnedGames_ReturnsAll()
        {
            // Arrange
            await using var conn = new SqliteConnection("DataSource=:memory:");
            await conn.OpenAsync();
            await using var db = await CreateSqliteDbAsync(conn);

            var friend = new User { Id = "friend-id", UserName = "collector", Email = "collector@test.com" };
            db.Users.Add(friend);

            var games = Enumerable.Range(1, 5).Select(i => new Game
            {
                BggGameId = 9010 + i,
                Name = $"Owned Game {i}",
                LastSyncedAt = DateTime.UtcNow
            }).ToList();
            db.Games.AddRange(games);
            await db.SaveChangesAsync();

            db.UserGameCollections.AddRange(games.Select(g => new UserGameCollection
            {
                UserId = friend.Id,
                GameId = g.Id,
                DateAdded = DateTime.UtcNow,
                Status = CollectionStatus.Owned
            }));
            await db.SaveChangesAsync();

            var mockUserManager = CreateUserManagerMock();
            mockUserManager.Setup(x => x.FindByNameAsync("collector")).ReturnsAsync(friend);

            var controller = CreateController(db, mockUserManager, "current-user-id");

            // Act
            var result = await controller.FriendCollection("collector");

            // Assert
            var model = ((ViewResult)result).Model as List<Game>;
            Assert.That(model!.Count, Is.EqualTo(5));
            Assert.That((int)((ViewResult)result).ViewData["TotalCount"]!, Is.EqualTo(5));
        }

        [Test]
        public async Task FriendCollection_OnlyViewerOwnedGames_FriendSeesNothing()
        {
            // Arrange — viewer has games but friend has none
            await using var conn = new SqliteConnection("DataSource=:memory:");
            await conn.OpenAsync();
            await using var db = await CreateSqliteDbAsync(conn);

            var viewer = new User { Id = "viewer-id", UserName = "viewer", Email = "viewer@test.com" };
            var friend = new User { Id = "friend-id", UserName = "empty", Email = "empty@test.com" };
            db.Users.AddRange(viewer, friend);

            var game = new Game { BggGameId = 9020, Name = "Viewer Game", LastSyncedAt = DateTime.UtcNow };
            db.Games.Add(game);
            await db.SaveChangesAsync();

            db.UserGameCollections.Add(new UserGameCollection
            {
                UserId = viewer.Id,
                GameId = game.Id,
                DateAdded = DateTime.UtcNow,
                Status = CollectionStatus.Owned
            });
            await db.SaveChangesAsync();

            var mockUserManager = CreateUserManagerMock();
            mockUserManager.Setup(x => x.FindByNameAsync("empty")).ReturnsAsync(friend);

            var controller = CreateController(db, mockUserManager, "viewer-id");

            // Act
            var result = await controller.FriendCollection("empty");

            // Assert — friend's collection is empty, not contaminated by viewer's games
            var model = ((ViewResult)result).Model as List<Game>;
            Assert.That(model!.Count, Is.EqualTo(0));
        }
    }
}
