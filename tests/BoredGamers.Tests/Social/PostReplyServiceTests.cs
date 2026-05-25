using BoredGamers.Data;
using BoredGamers.Models;
using BoredGamers.Services.Posts;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace BoredGamers.Tests.Social;

[TestFixture]
public class PostReplyServiceTests
{
    private ApplicationDbContext _db = null!;
    private PostReplyService _service = null!;
    private User _author = null!;
    private User _replier = null!;
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
        _service = new PostReplyService(_db);

        _author = new User { Id = "author-1", UserName = "postauthor", Email = "a@test.com", EmailConfirmed = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        _replier = new User { Id = "replier-1", UserName = "replieruser", Email = "r@test.com", EmailConfirmed = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        _db.Users.AddRange(_author, _replier);

        var profile = new UserProfile { UserId = _author.Id, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
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
    public async Task CreateReplyAsync_WithValidContent_ReturnsOk()
    {
        var result = await _service.CreateReplyAsync(_post.Id, _replier.Id, "Great post!");
        Assert.That(result.Success, Is.True);
        Assert.That(await _db.PostReplies.CountAsync(), Is.EqualTo(1));
    }

    [Test]
    public async Task CreateReplyAsync_WithEmptyContent_ReturnsFail()
    {
        var result = await _service.CreateReplyAsync(_post.Id, _replier.Id, "");
        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("empty").IgnoreCase);
    }

    [Test]
    public async Task CreateReplyAsync_WithWhitespaceOnly_ReturnsFail()
    {
        var result = await _service.CreateReplyAsync(_post.Id, _replier.Id, "   ");
        Assert.That(result.Success, Is.False);
    }

    [Test]
    public async Task CreateReplyAsync_WithContentExceeding500Chars_ReturnsFail()
    {
        var result = await _service.CreateReplyAsync(_post.Id, _replier.Id, new string('x', 501));
        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("500").IgnoreCase);
    }

    [Test]
    public async Task CreateReplyAsync_WithNonExistentPost_ReturnsFail()
    {
        var result = await _service.CreateReplyAsync(99999, _replier.Id, "Hello");
        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("not found").IgnoreCase);
    }

    [Test]
    public async Task CreateReplyAsync_TrimsWhitespace()
    {
        await _service.CreateReplyAsync(_post.Id, _replier.Id, "  trimmed  ");
        var reply = await _db.PostReplies.FirstAsync();
        Assert.That(reply.Content, Is.EqualTo("trimmed"));
    }

    [Test]
    public async Task GetRepliesForPostAsync_ReturnsRepliesInChronologicalOrder()
    {
        var earlier = DateTime.UtcNow.AddMinutes(-5);
        var later = DateTime.UtcNow;
        _db.PostReplies.AddRange(
            new PostReply { PostId = _post.Id, AuthorId = _replier.Id, Content = "Second", CreatedAt = later },
            new PostReply { PostId = _post.Id, AuthorId = _replier.Id, Content = "First", CreatedAt = earlier }
        );
        await _db.SaveChangesAsync();

        var replies = await _service.GetRepliesForPostAsync(_post.Id);

        Assert.That(replies.Count, Is.EqualTo(2));
        Assert.That(replies[0].Content, Is.EqualTo("First"));
        Assert.That(replies[1].Content, Is.EqualTo("Second"));
    }

    [Test]
    public async Task EditReplyAsync_ByAuthor_UpdatesContent()
    {
        await _service.CreateReplyAsync(_post.Id, _replier.Id, "original");
        var reply = await _db.PostReplies.FirstAsync();
        var result = await _service.EditReplyAsync(reply.Id, _replier.Id, "updated");
        Assert.That(result.Success, Is.True);
        await _db.Entry(reply).ReloadAsync();
        Assert.That(reply.Content, Is.EqualTo("updated"));
    }

    [Test]
    public async Task EditReplyAsync_ByNonAuthor_ReturnsFail()
    {
        await _service.CreateReplyAsync(_post.Id, _replier.Id, "original");
        var reply = await _db.PostReplies.FirstAsync();
        var result = await _service.EditReplyAsync(reply.Id, _author.Id, "hack");
        Assert.That(result.Success, Is.False);
    }

    [Test]
    public async Task EditReplyAsync_WithEmptyContent_ReturnsFail()
    {
        await _service.CreateReplyAsync(_post.Id, _replier.Id, "original");
        var reply = await _db.PostReplies.FirstAsync();
        var result = await _service.EditReplyAsync(reply.Id, _replier.Id, "");
        Assert.That(result.Success, Is.False);
    }

    [Test]
    public async Task DeleteReplyAsync_ByAuthor_RemovesReply()
    {
        await _service.CreateReplyAsync(_post.Id, _replier.Id, "to delete");
        var reply = await _db.PostReplies.FirstAsync();
        var result = await _service.DeleteReplyAsync(reply.Id, _replier.Id);
        Assert.That(result.Success, Is.True);
        Assert.That(await _db.PostReplies.CountAsync(), Is.EqualTo(0));
    }

    [Test]
    public async Task DeleteReplyAsync_ByPostOwner_RemovesReply()
    {
        await _service.CreateReplyAsync(_post.Id, _replier.Id, "offensive reply");
        var reply = await _db.PostReplies.FirstAsync();
        var result = await _service.DeleteReplyAsync(reply.Id, _author.Id);
        Assert.That(result.Success, Is.True);
        Assert.That(await _db.PostReplies.CountAsync(), Is.EqualTo(0));
    }

    [Test]
    public async Task DeleteReplyAsync_ByUnrelatedUser_ReturnsFail()
    {
        var stranger = new User { Id = "stranger-1", UserName = "stranger", Email = "s@test.com", EmailConfirmed = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        _db.Users.Add(stranger);
        await _db.SaveChangesAsync();

        await _service.CreateReplyAsync(_post.Id, _replier.Id, "a reply");
        var reply = await _db.PostReplies.FirstAsync();
        var result = await _service.DeleteReplyAsync(reply.Id, stranger.Id);
        Assert.That(result.Success, Is.False);
        Assert.That(await _db.PostReplies.CountAsync(), Is.EqualTo(1));
    }

    [Test]
    public async Task GetRepliesForPostAsync_WithNoReplies_ReturnsEmptyList()
    {
        var replies = await _service.GetRepliesForPostAsync(_post.Id);
        Assert.That(replies, Is.Empty);
    }

    [Test]
    public async Task CreateReplyAsync_NotifiesPostAuthor()
    {
        await _service.CreateReplyAsync(_post.Id, _replier.Id, "Great post!");
        var notification = await _db.Set<Notification>().FirstOrDefaultAsync();
        Assert.That(notification, Is.Not.Null);
        Assert.That(notification!.Type, Is.EqualTo("PostReply"));
        Assert.That(notification.Message, Does.Contain(_replier.UserName));
    }

    [Test]
    public async Task CreateReplyAsync_DoesNotNotifyWhenAuthorRepliesOwnPost()
    {
        await _service.CreateReplyAsync(_post.Id, _author.Id, "My own reply");
        var count = await _db.Set<Notification>().CountAsync();
        Assert.That(count, Is.EqualTo(0));
    }

    [Test]
    public async Task GetRepliesForPostAsync_OnlyReturnsRepliesForSpecifiedPost()
    {
        var otherPost = new ProfilePost
        {
            UserProfileId = _post.UserProfileId,
            Content = "Other post",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.ProfilePosts.Add(otherPost);
        await _db.SaveChangesAsync();

        _db.PostReplies.AddRange(
            new PostReply { PostId = _post.Id, AuthorId = _replier.Id, Content = "On correct post", CreatedAt = DateTime.UtcNow },
            new PostReply { PostId = otherPost.Id, AuthorId = _replier.Id, Content = "On other post", CreatedAt = DateTime.UtcNow }
        );
        await _db.SaveChangesAsync();

        var replies = await _service.GetRepliesForPostAsync(_post.Id);

        Assert.That(replies.Count, Is.EqualTo(1));
        Assert.That(replies[0].Content, Is.EqualTo("On correct post"));
    }
}
