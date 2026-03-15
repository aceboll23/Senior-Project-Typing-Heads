using System;
using System.Linq;
using System.Threading.Tasks;
using BoredGamers.Data;
using BoredGamers.Models;
using BoredGamers.Services.GameNightEvents;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using NUnit.Framework;

namespace BoredGamers.Tests.Events;

[TestFixture]
public class EventResponseTests
{
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

    private static async Task CreateTestUserAsync(ApplicationDbContext db, string userId, string userName)
    {
        db.Users.Add(new User
        {
            Id = userId,
            UserName = userName,
            NormalizedUserName = userName.ToUpper(),
            Email = $"{userName}@test.com",
            NormalizedEmail = $"{userName.ToUpper()}@TEST.COM",
            EmailConfirmed = true,
            SecurityStamp = Guid.NewGuid().ToString(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
    }

    private static async Task<(ApplicationDbContext db, GameNightEvent evt)> SetupEventWithMemberAsync()
    {
        var db = await CreateSqliteInMemoryDbAsync();

        await CreateTestUserAsync(db, "user1", "TestUser1");
        await CreateTestUserAsync(db, "user2", "TestUser2");

        var playgroup = new Playgroup
        {
            Name = "Test Group",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.Playgroups.Add(playgroup);
        await db.SaveChangesAsync();

        db.PlaygroupMembers.Add(new PlaygroupMember
        {
            PlaygroupId = playgroup.Id,
            UserId = "user1",
            Role = PlaygroupRole.Owner,
            JoinedAt = DateTime.UtcNow
        });
        db.PlaygroupMembers.Add(new PlaygroupMember
        {
            PlaygroupId = playgroup.Id,
            UserId = "user2",
            Role = PlaygroupRole.Member,
            JoinedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var evt = new GameNightEvent
        {
            PlaygroupId = playgroup.Id,
            CreatedByUserId = "user1",
            Title = "Test Event",
            EventDateTime = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow
        };
        db.GameNightEvents.Add(evt);
        await db.SaveChangesAsync();

        return (db, evt);
    }

    // =====================================================================
    // EventResponse Model Tests
    // =====================================================================

    [Test]
    public async Task EventResponse_CanBeCreatedAndSaved()
    {
        var db = await CreateSqliteInMemoryDbAsync();

        await CreateTestUserAsync(db, "user1", "TestUser1");

        var playgroup = new Playgroup { Name = "Test Group", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        db.Playgroups.Add(playgroup);
        await db.SaveChangesAsync();

        var evt = new GameNightEvent
        {
            PlaygroupId = playgroup.Id,
            CreatedByUserId = "user1",
            Title = "Game Night",
            EventDateTime = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow
        };
        db.GameNightEvents.Add(evt);
        await db.SaveChangesAsync();

        var response = new EventResponse
        {
            GameNightEventId = evt.Id,
            UserId = "user1",
            Status = ResponseStatus.Going,
            RespondedAt = DateTime.UtcNow
        };
        db.EventResponses.Add(response);
        await db.SaveChangesAsync();

        var saved = await db.EventResponses.FirstOrDefaultAsync();
        Assert.That(saved, Is.Not.Null);
        Assert.That(saved!.Status, Is.EqualTo(ResponseStatus.Going));
    }

    [Test]
    public async Task EventResponse_UniquePerUserPerEvent()
    {
        var db = await CreateSqliteInMemoryDbAsync();

        await CreateTestUserAsync(db, "user1", "TestUser1");

        var playgroup = new Playgroup { Name = "Test Group", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        db.Playgroups.Add(playgroup);
        await db.SaveChangesAsync();

        var evt = new GameNightEvent
        {
            PlaygroupId = playgroup.Id,
            CreatedByUserId = "user1",
            Title = "Game Night",
            EventDateTime = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow
        };
        db.GameNightEvents.Add(evt);
        await db.SaveChangesAsync();

        db.EventResponses.Add(new EventResponse
        {
            GameNightEventId = evt.Id,
            UserId = "user1",
            Status = ResponseStatus.Going,
            RespondedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        db.EventResponses.Add(new EventResponse
        {
            GameNightEventId = evt.Id,
            UserId = "user1",
            Status = ResponseStatus.Maybe,
            RespondedAt = DateTime.UtcNow
        });

        Assert.ThrowsAsync<DbUpdateException>(async () => await db.SaveChangesAsync());
    }

    [Test]
    public async Task EventResponse_CascadeDeletesWithEvent()
    {
        var db = await CreateSqliteInMemoryDbAsync();

        await CreateTestUserAsync(db, "user1", "TestUser1");

        var playgroup = new Playgroup { Name = "Test Group", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        db.Playgroups.Add(playgroup);
        await db.SaveChangesAsync();

        var evt = new GameNightEvent
        {
            PlaygroupId = playgroup.Id,
            CreatedByUserId = "user1",
            Title = "Game Night",
            EventDateTime = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow
        };
        db.GameNightEvents.Add(evt);
        await db.SaveChangesAsync();

        db.EventResponses.Add(new EventResponse
        {
            GameNightEventId = evt.Id,
            UserId = "user1",
            Status = ResponseStatus.Going,
            RespondedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        db.GameNightEvents.Remove(evt);
        await db.SaveChangesAsync();

        var responses = await db.EventResponses.CountAsync();
        Assert.That(responses, Is.EqualTo(0));
    }

    // =====================================================================
    // Service Tests (RespondToEvent)
    // =====================================================================

    [Test]
    public async Task RespondToEvent_MemberCanRespond()
    {
        var (db, evt) = await SetupEventWithMemberAsync();
        var service = new GameNightEventService(db);

        var result = await service.RespondToEventAsync(evt.Id, "user2", ResponseStatus.Going);

        Assert.That(result, Is.True);
        var response = await db.EventResponses.FirstOrDefaultAsync(r => r.UserId == "user2");
        Assert.That(response, Is.Not.Null);
        Assert.That(response!.Status, Is.EqualTo(ResponseStatus.Going));
    }

    [Test]
    public async Task RespondToEvent_NonMemberCannotRespond()
    {
        var (db, evt) = await SetupEventWithMemberAsync();
        var service = new GameNightEventService(db);

        var result = await service.RespondToEventAsync(evt.Id, "nonmember", ResponseStatus.Going);

        Assert.That(result, Is.False);
    }

    [Test]
    public async Task RespondToEvent_CanChangeResponse()
    {
        var (db, evt) = await SetupEventWithMemberAsync();
        var service = new GameNightEventService(db);

        await service.RespondToEventAsync(evt.Id, "user2", ResponseStatus.Going);
        await service.RespondToEventAsync(evt.Id, "user2", ResponseStatus.NotGoing);

        var response = await db.EventResponses.FirstOrDefaultAsync(r => r.UserId == "user2");
        Assert.That(response!.Status, Is.EqualTo(ResponseStatus.NotGoing));
    }

    [Test]
    public async Task GetEventResponses_ReturnsAllResponses()
    {
        var (db, evt) = await SetupEventWithMemberAsync();
        var service = new GameNightEventService(db);

        await service.RespondToEventAsync(evt.Id, "user1", ResponseStatus.Going);
        await service.RespondToEventAsync(evt.Id, "user2", ResponseStatus.Maybe);

        var responses = await service.GetEventResponsesAsync(evt.Id);
        Assert.That(responses.Count, Is.EqualTo(2));
    }

    [Test]
    public async Task GetUserResponse_ReturnsCorrectResponse()
    {
        var (db, evt) = await SetupEventWithMemberAsync();
        var service = new GameNightEventService(db);

        await service.RespondToEventAsync(evt.Id, "user1", ResponseStatus.Going);

        var response = await service.GetUserResponseAsync(evt.Id, "user1");
        Assert.That(response, Is.Not.Null);
        Assert.That(response!.Status, Is.EqualTo(ResponseStatus.Going));
    }

    [Test]
    public async Task GetUserResponse_ReturnsNullIfNoResponse()
    {
        var (db, evt) = await SetupEventWithMemberAsync();
        var service = new GameNightEventService(db);

        var response = await service.GetUserResponseAsync(evt.Id, "user2");
        Assert.That(response, Is.Null);
    }
}