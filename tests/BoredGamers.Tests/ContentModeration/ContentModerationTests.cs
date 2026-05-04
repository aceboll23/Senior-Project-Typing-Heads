using BoredGamers.Data;
using BoredGamers.Models;
using BoredGamers.Services;
using BoredGamers.Services.ContentModeration;
using BoredGamers.Services.Posts;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using NUnit.Framework;

namespace BoredGamers.Tests.ContentModeration;

[TestFixture]
public class ContentModerationTests
{
    private ApplicationDbContext _db = null!;
    private Mock<IContentModerationService> _moderationMock = null!;
    private Mock<IWebHostEnvironment> _envMock = null!;
    private Mock<IConfiguration> _configMock = null!;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new ApplicationDbContext(options);

        _moderationMock = new Mock<IContentModerationService>();
        _envMock = new Mock<IWebHostEnvironment>();
        _envMock.Setup(e => e.WebRootPath).Returns(Path.GetTempPath());
        _configMock = new Mock<IConfiguration>();
    }

    [TearDown]
    public void TearDown()
    {
        _db.Dispose();
    }

    private async Task<string> SeedUserAsync()
    {
        var userId = Guid.NewGuid().ToString();
        _db.Users.Add(new User { Id = userId, UserName = "tester", Email = "t@test.com" });
        _db.Set<UserProfile>().Add(new UserProfile { UserId = userId });
        await _db.SaveChangesAsync();
        return userId;
    }

    [Test]
    public async Task CreatePost_WithCleanContent_Succeeds()
    {
        var userId = await SeedUserAsync();
        _moderationMock.Setup(m => m.CheckContentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ModerationResult { IsFlagged = false });

        var service = new ProfilePostService(_db, _envMock.Object, _configMock.Object, _moderationMock.Object);

        var result = await service.CreatePostAsync(userId, "I love board games!");

        Assert.That(result.Success, Is.True);
        Assert.That(_db.ProfilePosts.Count(), Is.EqualTo(1));
    }

    [Test]
    public async Task CreatePost_WithFlaggedContent_FailsAndDoesNotSave()
    {
        var userId = await SeedUserAsync();
        _moderationMock.Setup(m => m.CheckContentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ModerationResult { IsFlagged = true, FlaggedCategories = new List<string> { "harassment" } });

        var service = new ProfilePostService(_db, _envMock.Object, _configMock.Object, _moderationMock.Object);

        var result = await service.CreatePostAsync(userId, "some flagged text");

        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("inappropriate"));
        Assert.That(_db.ProfilePosts.Count(), Is.EqualTo(0));
    }

    [Test]
    public async Task EditPost_WithFlaggedContent_FailsAndDoesNotUpdate()
    {
        var userId = await SeedUserAsync();
        var profile = await _db.Set<UserProfile>().FirstAsync(p => p.UserId == userId);

        var post = new ProfilePost
        {
            UserProfileId = profile.Id,
            Content = "original",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.ProfilePosts.Add(post);
        await _db.SaveChangesAsync();

        _moderationMock.Setup(m => m.CheckContentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ModerationResult { IsFlagged = true });

        var service = new ProfilePostService(_db, _envMock.Object, _configMock.Object, _moderationMock.Object);

        var result = await service.EditPostAsync(post.Id, userId, "flagged edit");

        Assert.That(result.Success, Is.False);
        var unchanged = await _db.ProfilePosts.FindAsync(post.Id);
        Assert.That(unchanged!.Content, Is.EqualTo("original"));
    }

    [Test]
    public async Task CreateReview_WithCleanText_Succeeds()
    {
        var userId = Guid.NewGuid().ToString();
        _db.Users.Add(new User { Id = userId, UserName = "reviewer", Email = "r@test.com" });
        _db.Games.Add(new Game { Id = 1, BggGameId = 1, Name = "Test Game" });
        await _db.SaveChangesAsync();

        _moderationMock.Setup(m => m.CheckContentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ModerationResult { IsFlagged = false });

        var service = new ReviewService(_db, _moderationMock.Object);

        var result = await service.CreateReviewAsync(userId, 1, 8, "Great game!");

        Assert.That(result.Success, Is.True);
        Assert.That(_db.Reviews.Count(), Is.EqualTo(1));
    }

    [Test]
    public async Task CreateReview_WithFlaggedText_FailsAndDoesNotSave()
    {
        var userId = Guid.NewGuid().ToString();
        _db.Users.Add(new User { Id = userId, UserName = "reviewer", Email = "r@test.com" });
        _db.Games.Add(new Game { Id = 1, BggGameId = 1, Name = "Test Game" });
        await _db.SaveChangesAsync();

        _moderationMock.Setup(m => m.CheckContentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ModerationResult { IsFlagged = true });

        var service = new ReviewService(_db, _moderationMock.Object);

        var result = await service.CreateReviewAsync(userId, 1, 8, "flagged review text");

        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("inappropriate"));
        Assert.That(_db.Reviews.Count(), Is.EqualTo(0));
    }

    [Test]
    public async Task EditReview_WithFlaggedText_FailsAndDoesNotUpdate()
    {
        var userId = Guid.NewGuid().ToString();
        _db.Users.Add(new User { Id = userId, UserName = "reviewer", Email = "r@test.com" });
        _db.Games.Add(new Game { Id = 1, BggGameId = 1, Name = "Test Game" });
        var review = new Review
        {
            UserId = userId,
            GameId = 1,
            Rating = 7,
            Text = "original review",
            CreatedAt = DateTime.UtcNow
        };
        _db.Reviews.Add(review);
        await _db.SaveChangesAsync();

        _moderationMock.Setup(m => m.CheckContentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ModerationResult { IsFlagged = true });

        var service = new ReviewService(_db, _moderationMock.Object);

        var result = await service.EditReviewAsync(review.ReviewId, userId, 7, "flagged edit");

        Assert.That(result.Success, Is.False);
        var unchanged = await _db.Reviews.FindAsync(review.ReviewId);
        Assert.That(unchanged!.Text, Is.EqualTo("original review"));
    }

    [Test]
    public async Task CreatePost_ModerationCheckIsCalledExactlyOnce()
    {
        var userId = await SeedUserAsync();
        _moderationMock.Setup(m => m.CheckContentAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ModerationResult { IsFlagged = false });

        var service = new ProfilePostService(_db, _envMock.Object, _configMock.Object, _moderationMock.Object);

        await service.CreatePostAsync(userId, "Hello world");

        _moderationMock.Verify(m => m.CheckContentAsync("Hello world", It.IsAny<CancellationToken>()), Times.Once);
    }
}