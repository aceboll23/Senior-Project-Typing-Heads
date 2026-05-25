using BoredGamers.Data;
using BoredGamers.Models;
using BoredGamers.Services.Posts;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace BoredGamers.Tests.Social;

[TestFixture]
public class PostLikeServiceTests
{
    private ApplicationDbContext _db = null!;
    private PostLikeService _service = null!;
    private User _user1 = null!;
    private User _user2 = null!;
    private ProfilePost _post = null!;

    private static async Task<ApplicationDbContext> CreateDbAsync()
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
        _db = await CreateDbAsync();
        _service = new PostLikeService(_db);

        _user1 = new User { Id = "user-1", UserName = "alice", Email = "a@test.com", EmailConfirmed = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        _user2 = new User { Id = "user-2", UserName = "bob", Email = "b@test.com", EmailConfirmed = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        _db.Users.AddRange(_user1, _user2);

        var profile = new UserProfile { UserId = _user1.Id, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        _db.Set<UserProfile>().Add(profile);
        await _db.SaveChangesAsync();

        _post = new ProfilePost
        {
            UserProfileId = profile.Id,
            Content = "Test post",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.ProfilePosts.Add(_post);
        await _db.SaveChangesAsync();
    }

    [TearDown]
    public void TearDown() => _db.Dispose();

    [Test]
    public async Task ToggleLikeAsync_FirstLike_ReturnsIsLikedTrueAndCountOne()
    {
        var (isLiked, count) = await _service.ToggleLikeAsync(_post.Id, _user2.Id);
        Assert.That(isLiked, Is.True);
        Assert.That(count, Is.EqualTo(1));
    }

    [Test]
    public async Task ToggleLikeAsync_UnlikeAfterLike_ReturnsIsLikedFalseAndCountZero()
    {
        await _service.ToggleLikeAsync(_post.Id, _user2.Id);
        var (isLiked, count) = await _service.ToggleLikeAsync(_post.Id, _user2.Id);
        Assert.That(isLiked, Is.False);
        Assert.That(count, Is.EqualTo(0));
    }

    [Test]
    public async Task ToggleLikeAsync_TwoUsersLike_CountIsTwo()
    {
        await _service.ToggleLikeAsync(_post.Id, _user1.Id);
        var (_, count) = await _service.ToggleLikeAsync(_post.Id, _user2.Id);
        Assert.That(count, Is.EqualTo(2));
    }

    [Test]
    public async Task GetLikeCountsAsync_ReturnCorrectCounts()
    {
        await _service.ToggleLikeAsync(_post.Id, _user1.Id);
        await _service.ToggleLikeAsync(_post.Id, _user2.Id);
        var counts = await _service.GetLikeCountsAsync(new[] { _post.Id });
        Assert.That(counts[_post.Id], Is.EqualTo(2));
    }

    [Test]
    public async Task GetLikeCountsAsync_PostWithNoLikes_NotInDictionary()
    {
        var counts = await _service.GetLikeCountsAsync(new[] { _post.Id });
        Assert.That(counts.ContainsKey(_post.Id), Is.False);
    }

    [Test]
    public async Task GetLikedPostIdsAsync_ReturnsOnlyLikedByUser()
    {
        await _service.ToggleLikeAsync(_post.Id, _user1.Id);
        var liked = await _service.GetLikedPostIdsAsync(_user1.Id, new[] { _post.Id });
        Assert.That(liked.Contains(_post.Id), Is.True);
    }

    [Test]
    public async Task GetLikedPostIdsAsync_OtherUsersLike_NotReturnedForCurrentUser()
    {
        await _service.ToggleLikeAsync(_post.Id, _user2.Id);
        var liked = await _service.GetLikedPostIdsAsync(_user1.Id, new[] { _post.Id });
        Assert.That(liked.Contains(_post.Id), Is.False);
    }
}
