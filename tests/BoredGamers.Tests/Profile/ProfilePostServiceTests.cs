using BoredGamers.Data;
using BoredGamers.Models;
using BoredGamers.Services.Posts;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace BoredGamers.Tests.Profile;

[TestFixture]
public class ProfilePostServiceTests
{
    private ApplicationDbContext _db = null!;
    private ProfilePostService _service = null!;

    private User _owner = null!;
    private User _other = null!;
    private UserProfile _ownerProfile = null!;

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
        _service = new ProfilePostService(_db);

        _owner = new User
        {
            Id = "owner-1",
            UserName = "PostOwner",
            Email = "owner@test.com",
            CreatedAt = DateTime.UtcNow
        };
        _other = new User
        {
            Id = "other-1",
            UserName = "OtherUser",
            Email = "other@test.com",
            CreatedAt = DateTime.UtcNow
        };
        _db.Users.AddRange(_owner, _other);
        await _db.SaveChangesAsync();

        _ownerProfile = new UserProfile
        {
            UserId = _owner.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.Set<UserProfile>().Add(_ownerProfile);
        await _db.SaveChangesAsync();
    }

    [TearDown]
    public void TearDown() => _db?.Dispose();

    // =====================================================================
    // CreatePostAsync
    // =====================================================================

    [Test]
    public async Task CreatePostAsync_ValidContent_ReturnsSuccess()
    {
        var result = await _service.CreatePostAsync(_owner.Id, "Great game!");
        Assert.That(result.Success, Is.True);
    }

    [Test]
    public async Task CreatePostAsync_ValidContent_SavesPostToDatabase()
    {
        await _service.CreatePostAsync(_owner.Id, "Great game!");
        var post = await _db.ProfilePosts.FirstOrDefaultAsync(p => p.UserProfileId == _ownerProfile.Id);
        Assert.That(post, Is.Not.Null);
        Assert.That(post!.Content, Is.EqualTo("Great game!"));
    }

    [Test]
    public async Task CreatePostAsync_EmptyContent_ReturnsFail()
    {
        var result = await _service.CreatePostAsync(_owner.Id, "");
        Assert.That(result.Success, Is.False);
    }

    [Test]
    public async Task CreatePostAsync_WhitespaceContent_ReturnsFail()
    {
        var result = await _service.CreatePostAsync(_owner.Id, "   ");
        Assert.That(result.Success, Is.False);
    }

    [Test]
    public async Task CreatePostAsync_ContentExceeds500Chars_ReturnsFail()
    {
        var tooLong = new string('x', 501);
        var result = await _service.CreatePostAsync(_owner.Id, tooLong);
        Assert.That(result.Success, Is.False);
    }

    [Test]
    public async Task CreatePostAsync_UserWithNoProfile_ReturnsFail()
    {
        var result = await _service.CreatePostAsync("nonexistent-user", "Hello");
        Assert.That(result.Success, Is.False);
    }

    // =====================================================================
    // GetPostsByUserIdAsync
    // =====================================================================

    [Test]
    public async Task GetPostsByUserIdAsync_ReturnsPosts()
    {
        _db.ProfilePosts.Add(new ProfilePost
        {
            UserProfileId = _ownerProfile.Id,
            Content = "Post A",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        var posts = await _service.GetPostsByUserIdAsync(_owner.Id);
        Assert.That(posts.Count, Is.EqualTo(1));
        Assert.That(posts[0].Content, Is.EqualTo("Post A"));
    }

    [Test]
    public async Task GetPostsByUserIdAsync_ReturnsPostsOrderedByMostRecent()
    {
        _db.ProfilePosts.AddRange(
            new ProfilePost
            {
                UserProfileId = _ownerProfile.Id,
                Content = "Older post",
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                UpdatedAt = DateTime.UtcNow
            },
            new ProfilePost
            {
                UserProfileId = _ownerProfile.Id,
                Content = "Newer post",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        );
        await _db.SaveChangesAsync();

        var posts = await _service.GetPostsByUserIdAsync(_owner.Id);
        Assert.That(posts[0].Content, Is.EqualTo("Newer post"));
        Assert.That(posts[1].Content, Is.EqualTo("Older post"));
    }

    [Test]
    public async Task GetPostsByUserIdAsync_NoProfile_ReturnsEmpty()
    {
        var posts = await _service.GetPostsByUserIdAsync("nonexistent-user");
        Assert.That(posts, Is.Empty);
    }

    // =====================================================================
    // DeletePostAsync
    // =====================================================================

    [Test]
    public async Task DeletePostAsync_ByOwner_ReturnsSuccess()
    {
        var post = new ProfilePost
        {
            UserProfileId = _ownerProfile.Id,
            Content = "To be deleted",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.ProfilePosts.Add(post);
        await _db.SaveChangesAsync();

        var result = await _service.DeletePostAsync(post.Id, _owner.Id);
        Assert.That(result.Success, Is.True);
    }

    [Test]
    public async Task DeletePostAsync_ByOwner_RemovesPostFromDatabase()
    {
        var post = new ProfilePost
        {
            UserProfileId = _ownerProfile.Id,
            Content = "To be deleted",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.ProfilePosts.Add(post);
        await _db.SaveChangesAsync();

        await _service.DeletePostAsync(post.Id, _owner.Id);
        var exists = await _db.ProfilePosts.AnyAsync(p => p.Id == post.Id);
        Assert.That(exists, Is.False);
    }

    [Test]
    public async Task DeletePostAsync_ByNonOwner_ReturnsFail()
    {
        var otherProfile = new UserProfile
        {
            UserId = _other.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.Set<UserProfile>().Add(otherProfile);
        var post = new ProfilePost
        {
            UserProfileId = _ownerProfile.Id,
            Content = "Owner's post",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.ProfilePosts.Add(post);
        await _db.SaveChangesAsync();

        var result = await _service.DeletePostAsync(post.Id, _other.Id);
        Assert.That(result.Success, Is.False);
    }

    [Test]
    public async Task DeletePostAsync_NonExistentPost_ReturnsFail()
    {
        var result = await _service.DeletePostAsync(9999, _owner.Id);
        Assert.That(result.Success, Is.False);
    }

    // =====================================================================
    // EditPostAsync
    // =====================================================================

    [Test]
    public async Task EditPostAsync_ByOwner_ReturnsSuccess()
    {
        var post = new ProfilePost
        {
            UserProfileId = _ownerProfile.Id,
            Content = "Original",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.ProfilePosts.Add(post);
        await _db.SaveChangesAsync();

        var result = await _service.EditPostAsync(post.Id, _owner.Id, "Updated");
        Assert.That(result.Success, Is.True);
    }

    [Test]
    public async Task EditPostAsync_ByOwner_UpdatesContent()
    {
        var post = new ProfilePost
        {
            UserProfileId = _ownerProfile.Id,
            Content = "Original",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.ProfilePosts.Add(post);
        await _db.SaveChangesAsync();

        await _service.EditPostAsync(post.Id, _owner.Id, "Updated content");
        var updated = await _db.ProfilePosts.FindAsync(post.Id);
        Assert.That(updated!.Content, Is.EqualTo("Updated content"));
    }

    [Test]
    public async Task EditPostAsync_ByNonOwner_ReturnsFail()
    {
        var otherProfile = new UserProfile
        {
            UserId = _other.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.Set<UserProfile>().Add(otherProfile);
        var post = new ProfilePost
        {
            UserProfileId = _ownerProfile.Id,
            Content = "Owner's post",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.ProfilePosts.Add(post);
        await _db.SaveChangesAsync();

        var result = await _service.EditPostAsync(post.Id, _other.Id, "Trying to edit");
        Assert.That(result.Success, Is.False);
    }

    [Test]
    public async Task EditPostAsync_EmptyContent_ReturnsFail()
    {
        var post = new ProfilePost
        {
            UserProfileId = _ownerProfile.Id,
            Content = "Original",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.ProfilePosts.Add(post);
        await _db.SaveChangesAsync();

        var result = await _service.EditPostAsync(post.Id, _owner.Id, "");
        Assert.That(result.Success, Is.False);
    }

    [Test]
    public async Task EditPostAsync_ContentExceeds500Chars_ReturnsFail()
    {
        var post = new ProfilePost
        {
            UserProfileId = _ownerProfile.Id,
            Content = "Original",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.ProfilePosts.Add(post);
        await _db.SaveChangesAsync();

        var result = await _service.EditPostAsync(post.Id, _owner.Id, new string('x', 501));
        Assert.That(result.Success, Is.False);
    }
}
