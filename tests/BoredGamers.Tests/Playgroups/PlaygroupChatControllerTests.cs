using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using BoredGamers.Controllers;
using BoredGamers.Data;
using BoredGamers.Hubs;
using BoredGamers.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;

namespace BoredGamers.Tests.Playgroups
{
    [TestFixture]
    public class PlaygroupChatControllerTests
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

        private static JsonElement ParseJson(JsonResult result)
        {
            var json = JsonSerializer.Serialize(result.Value);
            return JsonDocument.Parse(json).RootElement;
        }

        private static Mock<IHubContext<PlaygroupChatHub>> CreateHubContextMock()
        {
            var mockProxy = new Mock<IClientProxy>();
            mockProxy
                .Setup(p => p.SendCoreAsync(It.IsAny<string>(), It.IsAny<object?[]>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var mockClients = new Mock<IHubClients>();
            mockClients.Setup(c => c.Group(It.IsAny<string>())).Returns(mockProxy.Object);

            var mockHub = new Mock<IHubContext<PlaygroupChatHub>>();
            mockHub.Setup(h => h.Clients).Returns(mockClients.Object);
            return mockHub;
        }

        private static PlaygroupController CreateController(
            ApplicationDbContext db,
            Mock<UserManager<User>> mockUserManager,
            string currentUserId)
        {
            var controller = new PlaygroupController(db, mockUserManager.Object, CreateHubContextMock().Object);
            var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, currentUserId) };
            var identity = new ClaimsIdentity(claims, "Test");
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            };
            return controller;
        }

        private static async Task<(User owner, User member, Playgroup playgroup, UserProfile ownerProfile, UserProfile memberProfile)>
            SeedPlaygroupWithTwoMembersAsync(ApplicationDbContext db)
        {
            var owner = new User { Id = "owner-id", UserName = "owner", Email = "owner@test.com" };
            var member = new User { Id = "member-id", UserName = "member", Email = "member@test.com" };
            db.Users.AddRange(owner, member);
            await db.SaveChangesAsync();

            var ownerProfile = new UserProfile { UserId = owner.Id, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
            var memberProfile = new UserProfile { UserId = member.Id, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
            db.Set<UserProfile>().AddRange(ownerProfile, memberProfile);
            await db.SaveChangesAsync();

            var playgroup = new Playgroup
            {
                Name = "Test Group",
                CreatedByUserId = owner.Id,
                IsPrivate = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            db.Playgroups.Add(playgroup);
            await db.SaveChangesAsync();

            db.PlaygroupMembers.AddRange(
                new PlaygroupMember { PlaygroupId = playgroup.Id, UserId = owner.Id, Role = PlaygroupRole.Owner, JoinedAt = DateTime.UtcNow },
                new PlaygroupMember { PlaygroupId = playgroup.Id, UserId = member.Id, Role = PlaygroupRole.Member, JoinedAt = DateTime.UtcNow }
            );
            await db.SaveChangesAsync();

            return (owner, member, playgroup, ownerProfile, memberProfile);
        }

        // ── Chat GET ──────────────────────────────────────────────────────────

        [Test]
        public async Task Chat_NonMember_ReturnsNotFound()
        {
            await using var conn = new SqliteConnection("DataSource=:memory:");
            await conn.OpenAsync();
            await using var db = await CreateSqliteDbAsync(conn);

            var (_, _, playgroup, _, _) = await SeedPlaygroupWithTwoMembersAsync(db);
            var mockUM = CreateUserManagerMock();
            var controller = CreateController(db, mockUM, "outsider-id");

            var result = await controller.Chat(playgroup.Id);

            Assert.That(result, Is.TypeOf<NotFoundResult>());
        }

        [Test]
        public async Task Chat_Member_ReturnsView()
        {
            await using var conn = new SqliteConnection("DataSource=:memory:");
            await conn.OpenAsync();
            await using var db = await CreateSqliteDbAsync(conn);

            var (_, member, playgroup, _, _) = await SeedPlaygroupWithTwoMembersAsync(db);
            var mockUM = CreateUserManagerMock();
            var controller = CreateController(db, mockUM, member.Id);

            var result = await controller.Chat(playgroup.Id);

            Assert.That(result, Is.TypeOf<ViewResult>());
        }

        [Test]
        public async Task Chat_ReturnsMessagesOrderedChronologically()
        {
            await using var conn = new SqliteConnection("DataSource=:memory:");
            await conn.OpenAsync();
            await using var db = await CreateSqliteDbAsync(conn);

            var (owner, member, playgroup, ownerProfile, _) = await SeedPlaygroupWithTwoMembersAsync(db);

            db.PlaygroupMessages.AddRange(
                new PlaygroupMessage { PlaygroupId = playgroup.Id, SenderProfileId = ownerProfile.Id, Content = "First", SentAt = DateTime.UtcNow.AddMinutes(-5) },
                new PlaygroupMessage { PlaygroupId = playgroup.Id, SenderProfileId = ownerProfile.Id, Content = "Second", SentAt = DateTime.UtcNow }
            );
            await db.SaveChangesAsync();

            var mockUM = CreateUserManagerMock();
            var controller = CreateController(db, mockUM, member.Id);

            var result = await controller.Chat(playgroup.Id);
            var messages = ((ViewResult)result).Model as List<PlaygroupMessage>;

            Assert.That(messages, Is.Not.Null);
            Assert.That(messages![0].Content, Is.EqualTo("First"));
            Assert.That(messages[1].Content, Is.EqualTo("Second"));
        }

        [Test]
        public async Task Chat_NonExistentPlaygroup_ReturnsNotFound()
        {
            await using var conn = new SqliteConnection("DataSource=:memory:");
            await conn.OpenAsync();
            await using var db = await CreateSqliteDbAsync(conn);

            var mockUM = CreateUserManagerMock();
            var controller = CreateController(db, mockUM, "any-user-id");

            var result = await controller.Chat(9999);

            Assert.That(result, Is.TypeOf<NotFoundResult>());
        }

        // ── SendMessage ───────────────────────────────────────────────────────

        [Test]
        public async Task SendMessage_EmptyContent_ReturnsJsonError()
        {
            await using var conn = new SqliteConnection("DataSource=:memory:");
            await conn.OpenAsync();
            await using var db = await CreateSqliteDbAsync(conn);

            var (_, member, playgroup, _, _) = await SeedPlaygroupWithTwoMembersAsync(db);
            var mockUM = CreateUserManagerMock();
            var controller = CreateController(db, mockUM, member.Id);

            var result = await controller.SendMessage(playgroup.Id, "   ") as JsonResult;

            Assert.That(result, Is.Not.Null);
            var data = ParseJson(result!);
            Assert.That(data.GetProperty("success").GetBoolean(), Is.False);
        }

        [Test]
        public async Task SendMessage_ContentOver1000Chars_ReturnsJsonError()
        {
            await using var conn = new SqliteConnection("DataSource=:memory:");
            await conn.OpenAsync();
            await using var db = await CreateSqliteDbAsync(conn);

            var (_, member, playgroup, _, memberProfile) = await SeedPlaygroupWithTwoMembersAsync(db);
            var mockUM = CreateUserManagerMock();
            mockUM.Setup(x => x.FindByIdAsync(member.Id)).ReturnsAsync(member);
            var controller = CreateController(db, mockUM, member.Id);

            var result = await controller.SendMessage(playgroup.Id, new string('x', 1001)) as JsonResult;

            Assert.That(result, Is.Not.Null);
            var data = ParseJson(result!);
            Assert.That(data.GetProperty("success").GetBoolean(), Is.False);
        }

        [Test]
        public async Task SendMessage_ByNonMember_ReturnsJsonAccessDenied()
        {
            await using var conn = new SqliteConnection("DataSource=:memory:");
            await conn.OpenAsync();
            await using var db = await CreateSqliteDbAsync(conn);

            var (_, _, playgroup, _, _) = await SeedPlaygroupWithTwoMembersAsync(db);
            var mockUM = CreateUserManagerMock();
            var controller = CreateController(db, mockUM, "outsider-id");

            var result = await controller.SendMessage(playgroup.Id, "Hello") as JsonResult;

            Assert.That(result, Is.Not.Null);
            var data = ParseJson(result!);
            Assert.That(data.GetProperty("success").GetBoolean(), Is.False);
        }

        [Test]
        public async Task SendMessage_ValidMessage_PersistsToDatabase()
        {
            await using var conn = new SqliteConnection("DataSource=:memory:");
            await conn.OpenAsync();
            await using var db = await CreateSqliteDbAsync(conn);

            var (_, member, playgroup, _, _) = await SeedPlaygroupWithTwoMembersAsync(db);
            var mockUM = CreateUserManagerMock();
            mockUM.Setup(x => x.FindByIdAsync(member.Id)).ReturnsAsync(member);
            var controller = CreateController(db, mockUM, member.Id);

            var result = await controller.SendMessage(playgroup.Id, "Hello group!") as JsonResult;
            var data = ParseJson(result!);

            Assert.That(data.GetProperty("success").GetBoolean(), Is.True);
            var saved = await db.PlaygroupMessages.FirstOrDefaultAsync(m => m.Content == "Hello group!");
            Assert.That(saved, Is.Not.Null);
            Assert.That(saved!.IsSystemMessage, Is.False);
        }

        [Test]
        public async Task SendMessage_ValidMessage_ReturnsJsonWithSenderInfo()
        {
            await using var conn = new SqliteConnection("DataSource=:memory:");
            await conn.OpenAsync();
            await using var db = await CreateSqliteDbAsync(conn);

            var (_, member, playgroup, _, _) = await SeedPlaygroupWithTwoMembersAsync(db);
            var mockUM = CreateUserManagerMock();
            mockUM.Setup(x => x.FindByIdAsync(member.Id)).ReturnsAsync(member);
            var controller = CreateController(db, mockUM, member.Id);

            var result = await controller.SendMessage(playgroup.Id, "Test message") as JsonResult;
            var data = ParseJson(result!);

            Assert.That(data.GetProperty("success").GetBoolean(), Is.True);
            Assert.That(data.GetProperty("senderName").GetString(), Is.EqualTo(member.UserName));
            Assert.That(data.GetProperty("content").GetString(), Is.EqualTo("Test message"));
        }

        [Test]
        public async Task SendMessage_CreatesNotificationForOtherMembers()
        {
            await using var conn = new SqliteConnection("DataSource=:memory:");
            await conn.OpenAsync();
            await using var db = await CreateSqliteDbAsync(conn);

            var (owner, member, playgroup, ownerProfile, _) = await SeedPlaygroupWithTwoMembersAsync(db);
            var mockUM = CreateUserManagerMock();
            mockUM.Setup(x => x.FindByIdAsync(member.Id)).ReturnsAsync(member);
            var controller = CreateController(db, mockUM, member.Id);

            await controller.SendMessage(playgroup.Id, "Notify owner!");

            var notifications = await db.Set<Notification>()
                .Where(n => n.UserProfileId == ownerProfile.Id)
                .ToListAsync();
            Assert.That(notifications.Count, Is.EqualTo(1));
        }

        [Test]
        public async Task SendMessage_DoesNotCreateNotificationForSender()
        {
            await using var conn = new SqliteConnection("DataSource=:memory:");
            await conn.OpenAsync();
            await using var db = await CreateSqliteDbAsync(conn);

            var (_, member, playgroup, _, memberProfile) = await SeedPlaygroupWithTwoMembersAsync(db);
            var mockUM = CreateUserManagerMock();
            mockUM.Setup(x => x.FindByIdAsync(member.Id)).ReturnsAsync(member);
            var controller = CreateController(db, mockUM, member.Id);

            await controller.SendMessage(playgroup.Id, "No self-notify");

            var selfNotifications = await db.Set<Notification>()
                .Where(n => n.UserProfileId == memberProfile.Id)
                .ToListAsync();
            Assert.That(selfNotifications.Count, Is.EqualTo(0));
        }

        // ── RemoveMember ──────────────────────────────────────────────────────

        [Test]
        public async Task RemoveMember_ByNonOwner_ReturnsForbid()
        {
            await using var conn = new SqliteConnection("DataSource=:memory:");
            await conn.OpenAsync();
            await using var db = await CreateSqliteDbAsync(conn);

            var (owner, member, playgroup, _, _) = await SeedPlaygroupWithTwoMembersAsync(db);
            var mockUM = CreateUserManagerMock();
            var controller = CreateController(db, mockUM, member.Id);

            var result = await controller.RemoveMember(playgroup.Id, owner.Id);

            Assert.That(result, Is.TypeOf<ForbidResult>());
        }

        [Test]
        public async Task RemoveMember_OwnerRemovesMember_MembershipDeleted()
        {
            await using var conn = new SqliteConnection("DataSource=:memory:");
            await conn.OpenAsync();
            await using var db = await CreateSqliteDbAsync(conn);

            var (owner, member, playgroup, _, _) = await SeedPlaygroupWithTwoMembersAsync(db);
            var mockUM = CreateUserManagerMock();
            mockUM.Setup(x => x.FindByIdAsync(member.Id)).ReturnsAsync(member);
            var controller = CreateController(db, mockUM, owner.Id);

            await controller.RemoveMember(playgroup.Id, member.Id);

            var membership = await db.PlaygroupMembers
                .FirstOrDefaultAsync(m => m.PlaygroupId == playgroup.Id && m.UserId == member.Id);
            Assert.That(membership, Is.Null);
        }

        [Test]
        public async Task RemoveMember_PostsSystemMessage()
        {
            await using var conn = new SqliteConnection("DataSource=:memory:");
            await conn.OpenAsync();
            await using var db = await CreateSqliteDbAsync(conn);

            var (owner, member, playgroup, _, _) = await SeedPlaygroupWithTwoMembersAsync(db);
            var mockUM = CreateUserManagerMock();
            mockUM.Setup(x => x.FindByIdAsync(member.Id)).ReturnsAsync(member);
            var controller = CreateController(db, mockUM, owner.Id);

            await controller.RemoveMember(playgroup.Id, member.Id);

            var systemMsg = await db.PlaygroupMessages
                .FirstOrDefaultAsync(m => m.PlaygroupId == playgroup.Id && m.IsSystemMessage);
            Assert.That(systemMsg, Is.Not.Null);
            Assert.That(systemMsg!.Content, Does.Contain("removed"));
        }

        [Test]
        public async Task RemoveMember_OwnerCannotRemoveSelf_Redirects()
        {
            await using var conn = new SqliteConnection("DataSource=:memory:");
            await conn.OpenAsync();
            await using var db = await CreateSqliteDbAsync(conn);

            var (owner, _, playgroup, _, _) = await SeedPlaygroupWithTwoMembersAsync(db);
            var mockUM = CreateUserManagerMock();
            var controller = CreateController(db, mockUM, owner.Id);

            var result = await controller.RemoveMember(playgroup.Id, owner.Id);

            Assert.That(result, Is.TypeOf<RedirectToActionResult>());
            var membership = await db.PlaygroupMembers
                .FirstOrDefaultAsync(m => m.PlaygroupId == playgroup.Id && m.UserId == owner.Id);
            Assert.That(membership, Is.Not.Null);
        }

        // ── System Messages ───────────────────────────────────────────────────

        [Test]
        public async Task LeavePlaygroup_PostsSystemMessage()
        {
            await using var conn = new SqliteConnection("DataSource=:memory:");
            await conn.OpenAsync();
            await using var db = await CreateSqliteDbAsync(conn);

            var (_, member, playgroup, _, _) = await SeedPlaygroupWithTwoMembersAsync(db);
            var mockUM = CreateUserManagerMock();
            mockUM.Setup(x => x.FindByIdAsync(member.Id)).ReturnsAsync(member);
            var controller = CreateController(db, mockUM, member.Id);

            await controller.LeavePlaygroup(playgroup.Id);

            var systemMsg = await db.PlaygroupMessages
                .FirstOrDefaultAsync(m => m.PlaygroupId == playgroup.Id && m.IsSystemMessage);
            Assert.That(systemMsg, Is.Not.Null);
            Assert.That(systemMsg!.Content, Does.Contain("left").IgnoreCase);
        }
    }
}
