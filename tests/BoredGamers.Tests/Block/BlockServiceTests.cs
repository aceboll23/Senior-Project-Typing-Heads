using BoredGamers.Data;
using BoredGamers.Models;
using BoredGamers.Services.Block;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace BoredGamers.Tests.Block;

[TestFixture]
public class BlockServiceTests
{
    private ApplicationDbContext _db = null!;
    private BlockService _service = null!;

    private User _blocker = null!;
    private User _target = null!;
    private User _stranger = null!;
    private UserProfile _blockerProfile = null!;
    private UserProfile _targetProfile = null!;
    private UserProfile _strangerProfile = null!;

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

    [SetUp]
    public async Task SetUp()
    {
        _db = await CreateSqliteInMemoryDbAsync();
        _service = new BlockService(_db);

        _blocker = new User { Id = "blocker-1", UserName = "Blocker", Email = "blocker@test.com", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        _target = new User { Id = "target-1", UserName = "Target", Email = "target@test.com", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        _stranger = new User { Id = "stranger-1", UserName = "Stranger", Email = "stranger@test.com", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        _db.Users.AddRange(_blocker, _target, _stranger);
        await _db.SaveChangesAsync();

        _blockerProfile = new UserProfile { UserId = _blocker.Id, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        _targetProfile = new UserProfile { UserId = _target.Id, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        _strangerProfile = new UserProfile { UserId = _stranger.Id, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        _db.Set<UserProfile>().AddRange(_blockerProfile, _targetProfile, _strangerProfile);
        await _db.SaveChangesAsync();
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    // --- BlockUserAsync ---

    [Test]
    public async Task BlockUserAsync_CreatesBlockRecord()
    {
        var result = await _service.BlockUserAsync(_blocker.Id, _target.UserName!);

        Assert.That(result.Success, Is.True);
        var block = await _db.Set<BlockedUser>().FirstOrDefaultAsync(b =>
            b.BlockerProfileId == _blockerProfile.Id && b.BlockedProfileId == _targetProfile.Id);
        Assert.That(block, Is.Not.Null);
    }

    [Test]
    public async Task BlockUserAsync_RemovesExistingFriendship()
    {
        _db.Set<Friendship>().Add(new Friendship
        {
            RequesterProfileId = _blockerProfile.Id,
            ReceiverProfileId = _targetProfile.Id,
            Status = FriendshipStatus.Accepted,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        await _service.BlockUserAsync(_blocker.Id, _target.UserName!);

        var friendship = await _db.Set<Friendship>().FirstOrDefaultAsync(f =>
            (f.RequesterProfileId == _blockerProfile.Id && f.ReceiverProfileId == _targetProfile.Id) ||
            (f.RequesterProfileId == _targetProfile.Id && f.ReceiverProfileId == _blockerProfile.Id));
        Assert.That(friendship, Is.Null);
    }

    [Test]
    public async Task BlockUserAsync_IsIdempotentWhenAlreadyBlocked()
    {
        await _service.BlockUserAsync(_blocker.Id, _target.UserName!);
        var result = await _service.BlockUserAsync(_blocker.Id, _target.UserName!);

        Assert.That(result.Success, Is.True);
        var count = await _db.Set<BlockedUser>().CountAsync(b =>
            b.BlockerProfileId == _blockerProfile.Id && b.BlockedProfileId == _targetProfile.Id);
        Assert.That(count, Is.EqualTo(1));
    }

    [Test]
    public async Task BlockUserAsync_FailsWhenBlockingSelf()
    {
        var result = await _service.BlockUserAsync(_blocker.Id, _blocker.UserName!);

        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("yourself"));
    }

    [Test]
    public async Task BlockUserAsync_FailsWhenTargetUserNotFound()
    {
        var result = await _service.BlockUserAsync(_blocker.Id, "nonexistent_user");

        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("not found").IgnoreCase);
    }

    // --- UnblockUserAsync ---

    [Test]
    public async Task UnblockUserAsync_RemovesBlockRecord()
    {
        _db.Set<BlockedUser>().Add(new BlockedUser
        {
            BlockerProfileId = _blockerProfile.Id,
            BlockedProfileId = _targetProfile.Id,
            BlockedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        var result = await _service.UnblockUserAsync(_blocker.Id, _target.UserName!);

        Assert.That(result.Success, Is.True);
        var block = await _db.Set<BlockedUser>().FirstOrDefaultAsync(b =>
            b.BlockerProfileId == _blockerProfile.Id && b.BlockedProfileId == _targetProfile.Id);
        Assert.That(block, Is.Null);
    }

    [Test]
    public async Task UnblockUserAsync_FailsWhenUserNotBlocked()
    {
        var result = await _service.UnblockUserAsync(_blocker.Id, _target.UserName!);

        Assert.That(result.Success, Is.False);
    }

    // --- IsBlockedByAsync ---

    [Test]
    public async Task IsBlockedByAsync_ReturnsTrueWhenViewerIsBlocked()
    {
        _db.Set<BlockedUser>().Add(new BlockedUser
        {
            BlockerProfileId = _targetProfile.Id,
            BlockedProfileId = _blockerProfile.Id,
            BlockedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        // Blocker tries to view Target's profile — Target has blocked Blocker
        var isBlocked = await _service.IsBlockedByAsync(_blocker.Id, _target.Id);

        Assert.That(isBlocked, Is.True);
    }

    [Test]
    public async Task IsBlockedByAsync_ReturnsFalseWhenNotBlocked()
    {
        var isBlocked = await _service.IsBlockedByAsync(_blocker.Id, _target.Id);

        Assert.That(isBlocked, Is.False);
    }

    // --- GetBlockedUsersAsync ---

    [Test]
    public async Task GetBlockedUsersAsync_ReturnsBlockedUsers()
    {
        _db.Set<BlockedUser>().Add(new BlockedUser
        {
            BlockerProfileId = _blockerProfile.Id,
            BlockedProfileId = _targetProfile.Id,
            BlockedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        var blocked = await _service.GetBlockedUsersAsync(_blocker.Id);

        Assert.That(blocked.Count, Is.EqualTo(1));
        Assert.That(blocked[0].Username, Is.EqualTo(_target.UserName));
    }

    [Test]
    public async Task GetBlockedUsersAsync_ReturnsEmptyWhenNoneBlocked()
    {
        var blocked = await _service.GetBlockedUsersAsync(_blocker.Id);

        Assert.That(blocked, Is.Empty);
    }

    [Test]
    public async Task GetBlockedUsersAsync_DoesNotReturnUsersWhoBlockedMe()
    {
        // Stranger has blocked blocker — blocker's list should not include stranger
        _db.Set<BlockedUser>().Add(new BlockedUser
        {
            BlockerProfileId = _strangerProfile.Id,
            BlockedProfileId = _blockerProfile.Id,
            BlockedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        var blocked = await _service.GetBlockedUsersAsync(_blocker.Id);

        Assert.That(blocked, Is.Empty);
    }
}
