using BoredGamers.Data;
using BoredGamers.Models;
using BoredGamers.Services.Posts;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using NUnit.Framework;

namespace BoredGamers.Tests.Profile;

[TestFixture]
public class ProfilePostServiceTests
{
    private ApplicationDbContext _db = null!;
    private ProfilePostService _service = null!;
    private Mock<IWebHostEnvironment> _mockEnv = null!;

    private User _owner = null!;
    private User _other = null!;
    private UserProfile _ownerProfile = null!;

    private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".gif", ".webp"];

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

        _mockEnv = new Mock<IWebHostEnvironment>();
        _mockEnv.Setup(e => e.WebRootPath).Returns(Path.GetTempPath());

        var mockConfig = new Mock<IConfiguration>();
        mockConfig.Setup(c => c["Uploads:BasePath"]).Returns((string?)null);

        _service = new ProfilePostService(_db, _mockEnv.Object, mockConfig.Object);

        _owner = new User
        {
            Id = "owner-1",
            UserName = "PostOwner",
            Email = "owner@test.com",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _other = new User
        {
            Id = "other-1",
            UserName = "OtherUser",
            Email = "other@test.com",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
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
    // Helpers
    // =====================================================================

    private static IFormFile MakeImageFile(string fileName = "test.jpg", long size = 1024)
    {
        var content = new byte[size];
        var stream = new MemoryStream(content);
        return new FormFile(stream, 0, size, "postImage", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = "image/jpeg"
        };
    }

    // =====================================================================
    // CreatePostAsync — text only
    // =====================================================================

    [Test]
    public async Task CreatePostAsync_TextOnly_ReturnsSuccess()
    {
        var result = await _service.CreatePostAsync(_owner.Id, "Great game!", null, PostCategory.None, null);
        Assert.That(result.Success, Is.True);
    }

    [Test]
    public async Task CreatePostAsync_TextOnly_SavesContentToDatabase()
    {
        await _service.CreatePostAsync(_owner.Id, "Great game!", null, PostCategory.None, null);
        var post = await _db.ProfilePosts.FirstOrDefaultAsync(p => p.UserProfileId == _ownerProfile.Id);
        Assert.That(post, Is.Not.Null);
        Assert.That(post!.Content, Is.EqualTo("Great game!"));
    }

    [Test]
    public async Task CreatePostAsync_NoTextNoImage_ReturnsFail()
    {
        var result = await _service.CreatePostAsync(_owner.Id, "", null, PostCategory.None, null);
        Assert.That(result.Success, Is.False);
    }

    [Test]
    public async Task CreatePostAsync_WhitespaceTextNoImage_ReturnsFail()
    {
        var result = await _service.CreatePostAsync(_owner.Id, "   ", null, PostCategory.None, null);
        Assert.That(result.Success, Is.False);
    }

    [Test]
    public async Task CreatePostAsync_ContentExceeds500Chars_ReturnsFail()
    {
        var tooLong = new string('x', 501);
        var result = await _service.CreatePostAsync(_owner.Id, tooLong, null, PostCategory.None, null);
        Assert.That(result.Success, Is.False);
    }

    [Test]
    public async Task CreatePostAsync_UserWithNoProfile_ReturnsFail()
    {
        var result = await _service.CreatePostAsync("nonexistent-user", "Hello", null, PostCategory.None, null);
        Assert.That(result.Success, Is.False);
    }

    // =====================================================================
    // CreatePostAsync — image upload
    // =====================================================================

    [Test]
    public async Task CreatePostAsync_ImageOnly_ReturnsSuccess()
    {
        var file = MakeImageFile("photo.jpg");
        var result = await _service.CreatePostAsync(_owner.Id, "", file, PostCategory.None, null);
        Assert.That(result.Success, Is.True);
    }

    [Test]
    public async Task CreatePostAsync_ImageOnly_SetsImageUrl()
    {
        var file = MakeImageFile("photo.jpg");
        await _service.CreatePostAsync(_owner.Id, "", file, PostCategory.None, null);
        var post = await _db.ProfilePosts.FirstOrDefaultAsync(p => p.UserProfileId == _ownerProfile.Id);
        Assert.That(post!.ImageUrl, Is.Not.Null.And.StartsWith("/uploads/posts/"));
    }

    [Test]
    public async Task CreatePostAsync_TextAndImage_ReturnsSuccess()
    {
        var file = MakeImageFile("photo.png");
        var result = await _service.CreatePostAsync(_owner.Id, "Game night!", file, PostCategory.None, null);
        Assert.That(result.Success, Is.True);
    }

    [Test]
    public async Task CreatePostAsync_InvalidImageType_ReturnsFail()
    {
        var file = MakeImageFile("malware.exe");
        var result = await _service.CreatePostAsync(_owner.Id, "", file, PostCategory.None, null);
        Assert.That(result.Success, Is.False);
    }

    [Test]
    public async Task CreatePostAsync_ImageTooLarge_ReturnsFail()
    {
        var file = MakeImageFile("huge.jpg", 6 * 1024 * 1024);
        var result = await _service.CreatePostAsync(_owner.Id, "", file, PostCategory.None, null);
        Assert.That(result.Success, Is.False);
    }

    // =====================================================================
    // CreatePostAsync — category and game
    // =====================================================================

    [Test]
    public async Task CreatePostAsync_WithCategory_SavesCategory()
    {
        await _service.CreatePostAsync(_owner.Id, "Playing Wingspan!", null, PostCategory.CurrentlyPlaying, null);
        var post = await _db.ProfilePosts.FirstOrDefaultAsync(p => p.UserProfileId == _ownerProfile.Id);
        Assert.That(post!.Category, Is.EqualTo(PostCategory.CurrentlyPlaying));
    }

    [Test]
    public async Task CreatePostAsync_WithGameId_SavesGameId()
    {
        var game = new Game { BggGameId = 99, Name = "Wingspan", LastSyncedAt = DateTime.UtcNow };
        _db.Games.Add(game);
        await _db.SaveChangesAsync();

        await _service.CreatePostAsync(_owner.Id, "Love this game", null, PostCategory.None, game.Id);
        var post = await _db.ProfilePosts.FirstOrDefaultAsync(p => p.UserProfileId == _ownerProfile.Id);
        Assert.That(post!.GameId, Is.EqualTo(game.Id));
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
