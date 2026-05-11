using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BoredGamers.Data;
using BoredGamers.Models;
using BoredGamers.Services.Flags;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace BoredGamers.Tests.Services.Flags;

[TestFixture]
public class PostFlagServiceTests
{
    private ApplicationDbContext _db = null!;
    private PostFlagService _service = null!;
    private User _reporter = null!;
    private UserProfile _reporterProfile = null!;
    private ProfilePost _post = null!;

    private static async Task<ApplicationDbContext> CreateSqliteDbAsync()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;
        var db = new BoredGamers.Tests.TestUtilities
            .TestApplicationDbContext(options);
        await db.Database.EnsureCreatedAsync();
        return db;
    }

    [SetUp]
    public async Task SetUp()
    {
        _db = await CreateSqliteDbAsync();
        _service = new PostFlagService(_db);

        // Seed a user + profile + post
        _reporter = new User
        {
            Id = "reporter-1",
            UserName = "Reporter",
            Email = "reporter@test.com",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.Users.Add(_reporter);
        await _db.SaveChangesAsync();

        _reporterProfile = new UserProfile
        {
            UserId = _reporter.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.Set<UserProfile>().Add(_reporterProfile);
        await _db.SaveChangesAsync();

        _post = new ProfilePost
        {
            UserProfileId = _reporterProfile.Id,
            Content = "Some post content",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.ProfilePosts.Add(_post);
        await _db.SaveChangesAsync();
    }

    [TearDown]
    public void TearDown() => _db?.Dispose();

    // T1 — Valid input creates a flag row
    [Test]
    public async Task FlagPost_ValidInput_CreatesRow()
    {
        var result = await _service.FlagPostAsync(
            _reporter.Id, _post.Id, CancellationToken.None);

        Assert.That(result.Success, Is.True);

        var flags = await _db.ProfilePostFlags.ToListAsync();
        Assert.That(flags.Count, Is.EqualTo(1));
        Assert.That(flags[0].ProfilePostId, Is.EqualTo(_post.Id));
        Assert.That(flags[0].ReporterId, Is.EqualTo(_reporter.Id));
        Assert.That(flags[0].ReportedAt,
            Is.EqualTo(DateTime.UtcNow).Within(TimeSpan.FromSeconds(5)));
    }

    // T2 — Same user, same post → second call fails, no second row
    [Test]
    public async Task FlagPost_Duplicate_FailsWithNoSecondRow()
    {
        // Seed an existing flag
        _db.ProfilePostFlags.Add(new ProfilePostFlag
        {
            ProfilePostId = _post.Id,
            ReporterId = _reporter.Id,
            ReportedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        var result = await _service.FlagPostAsync(
            _reporter.Id, _post.Id, CancellationToken.None);

        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage,
            Does.Contain("already"));

        var count = await _db.ProfilePostFlags.CountAsync();
        Assert.That(count, Is.EqualTo(1));
    }

    // T3 — Nonexistent post → fails gracefully
    [Test]
    public async Task FlagPost_NonexistentPost_FailsGracefully()
    {
        var result = await _service.FlagPostAsync(
            _reporter.Id, 99999, CancellationToken.None);

        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage,
            Does.Contain("not found"));

        var count = await _db.ProfilePostFlags.CountAsync();
        Assert.That(count, Is.EqualTo(0));
    }
}
