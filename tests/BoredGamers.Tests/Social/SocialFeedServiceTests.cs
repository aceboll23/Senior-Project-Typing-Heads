using BoredGamers.Data;
using BoredGamers.Models;
using BoredGamers.Services.SocialFeed;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace BoredGamers.Tests.Social;

[TestFixture]
public class SocialFeedServiceTests
{
    private ApplicationDbContext _db = null!;
    private SocialFeedService _service = null!;

    private User _viewer = null!;
    private User _friend = null!;
    private User _stranger = null!;
    private UserProfile _viewerProfile = null!;
    private UserProfile _friendProfile = null!;
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
        _service = new SocialFeedService(_db);

        _viewer = new User { Id = "viewer-1", UserName = "Viewer", Email = "viewer@test.com", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        _friend = new User { Id = "friend-1", UserName = "Friend", Email = "friend@test.com", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        _stranger = new User { Id = "stranger-1", UserName = "Stranger", Email = "stranger@test.com", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };

        _db.Users.AddRange(_viewer, _friend, _stranger);
        await _db.SaveChangesAsync();

        _viewerProfile = new UserProfile { UserId = _viewer.Id, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        _friendProfile = new UserProfile { UserId = _friend.Id, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        _strangerProfile = new UserProfile { UserId = _stranger.Id, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };

        _db.Set<UserProfile>().AddRange(_viewerProfile, _friendProfile, _strangerProfile);
        await _db.SaveChangesAsync();
    }

    [TearDown]
    public void TearDown() => _db?.Dispose();

    private async Task<Friendship> AddAcceptedFriendshipAsync(UserProfile requester, UserProfile receiver)
    {
        var friendship = new Friendship
        {
            RequesterProfileId = requester.Id,
            ReceiverProfileId = receiver.Id,
            Status = FriendshipStatus.Accepted,
            RequestedAt = DateTime.UtcNow,
            RespondedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.Set<Friendship>().Add(friendship);
        await _db.SaveChangesAsync();
        return friendship;
    }

    private async Task<ProfilePost> AddPostAsync(UserProfile profile, string content, DateTime? createdAt = null)
    {
        var post = new ProfilePost
        {
            UserProfileId = profile.Id,
            Content = content,
            CreatedAt = createdAt ?? DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.ProfilePosts.Add(post);
        await _db.SaveChangesAsync();
        return post;
    }

    // =====================================================================
    // GetFeedForUserAsync
    // =====================================================================

    [Test]
    public async Task GetFeedForUserAsync_ReturnsFriendPosts()
    {
        await AddAcceptedFriendshipAsync(_viewerProfile, _friendProfile);
        await AddPostAsync(_friendProfile, "Friend's post");

        var feed = await _service.GetFeedForUserAsync(_viewer.Id);

        Assert.That(feed.Count, Is.EqualTo(1));
        Assert.That(feed[0].Content, Is.EqualTo("Friend's post"));
    }

    [Test]
    public async Task GetFeedForUserAsync_IncludesAuthorUsername()
    {
        await AddAcceptedFriendshipAsync(_viewerProfile, _friendProfile);
        await AddPostAsync(_friendProfile, "Friend's post");

        var feed = await _service.GetFeedForUserAsync(_viewer.Id);

        Assert.That(feed[0].AuthorUsername, Is.EqualTo("Friend"));
    }

    [Test]
    public async Task GetFeedForUserAsync_ReturnsPostsOrderedByMostRecent()
    {
        await AddAcceptedFriendshipAsync(_viewerProfile, _friendProfile);
        await AddPostAsync(_friendProfile, "Older post", DateTime.UtcNow.AddDays(-2));
        await AddPostAsync(_friendProfile, "Newer post", DateTime.UtcNow);

        var feed = await _service.GetFeedForUserAsync(_viewer.Id);

        Assert.That(feed[0].Content, Is.EqualTo("Newer post"));
        Assert.That(feed[1].Content, Is.EqualTo("Older post"));
    }

    [Test]
    public async Task GetFeedForUserAsync_ExcludesNonFriendPosts()
    {
        await AddAcceptedFriendshipAsync(_viewerProfile, _friendProfile);
        await AddPostAsync(_friendProfile, "Friend's post");
        await AddPostAsync(_strangerProfile, "Stranger's post");

        var feed = await _service.GetFeedForUserAsync(_viewer.Id);

        Assert.That(feed.Count, Is.EqualTo(1));
        Assert.That(feed.Any(p => p.Content == "Stranger's post"), Is.False);
    }

    [Test]
    public async Task GetFeedForUserAsync_ExcludesPendingFriendshipPosts()
    {
        var pendingFriendship = new Friendship
        {
            RequesterProfileId = _viewerProfile.Id,
            ReceiverProfileId = _friendProfile.Id,
            Status = FriendshipStatus.Pending,
            RequestedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.Set<Friendship>().Add(pendingFriendship);
        await _db.SaveChangesAsync();
        await AddPostAsync(_friendProfile, "Friend's post");

        var feed = await _service.GetFeedForUserAsync(_viewer.Id);

        Assert.That(feed, Is.Empty);
    }

    [Test]
    public async Task GetFeedForUserAsync_ReturnsEmptyWhenFriendsHaveNoPosts()
    {
        await AddAcceptedFriendshipAsync(_viewerProfile, _friendProfile);

        var feed = await _service.GetFeedForUserAsync(_viewer.Id);

        Assert.That(feed, Is.Empty);
    }

    [Test]
    public async Task GetFeedForUserAsync_ReturnsEmptyForUserWithNoProfile()
    {
        var feed = await _service.GetFeedForUserAsync("nonexistent-user");

        Assert.That(feed, Is.Empty);
    }

    [Test]
    public async Task GetFeedForUserAsync_HandlesFriendshipWhereViewerIsReceiver()
    {
        // Friend is the requester, viewer is the receiver
        await AddAcceptedFriendshipAsync(_friendProfile, _viewerProfile);
        await AddPostAsync(_friendProfile, "Friend's post");

        var feed = await _service.GetFeedForUserAsync(_viewer.Id);

        Assert.That(feed.Count, Is.EqualTo(1));
        Assert.That(feed[0].Content, Is.EqualTo("Friend's post"));
    }

    [Test]
    public async Task GetFeedForUserAsync_CombinesPostsFromMultipleFriends()
    {
        await AddAcceptedFriendshipAsync(_viewerProfile, _friendProfile);
        await AddAcceptedFriendshipAsync(_viewerProfile, _strangerProfile);
        await AddPostAsync(_friendProfile, "Friend's post");
        await AddPostAsync(_strangerProfile, "Stranger becomes friend's post");

        var feed = await _service.GetFeedForUserAsync(_viewer.Id);

        Assert.That(feed.Count, Is.EqualTo(2));
    }
}
