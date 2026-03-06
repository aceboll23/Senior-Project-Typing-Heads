using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using BoredGamers.Data;      // adjust if needed
using BoredGamers.Models;    // adjust if needed
using BoredGamers.Services;  // adjust if needed

namespace BoredGamers.Tests.Services
{
  [TestFixture]
  public class ReviewServiceTests
  {
    private ApplicationDbContext NewDb(string dbName)
    {
      var options = new DbContextOptionsBuilder<ApplicationDbContext>()
          .UseInMemoryDatabase(dbName)
          .Options;

      return new ApplicationDbContext(options);
    }

    [Test]
    public async Task CreateReviewAsync_WithValidInput_CreatesReview()
    {
      var db = NewDb(Guid.NewGuid().ToString());

      db.Games.Add(new Game { Id = 1, Name = "Catan" });
      await db.SaveChangesAsync();

      var sut = new ReviewService(db);
      var userId = "user-1";

      var result = await sut.CreateReviewAsync(userId, 1, 8, "Great game.");

      Assert.That(result.Success, Is.True);

      var saved = await db.Reviews
          .FirstOrDefaultAsync(r => r.GameId == 1 && r.UserId == userId);

      Assert.That(saved, Is.Not.Null);
      Assert.That(saved!.Rating, Is.EqualTo(8));
      Assert.That(saved.Text, Is.EqualTo("Great game."));
    }

    [TestCase(0)]
    [TestCase(11)]
    public async Task CreateReviewAsync_RatingOutOfRange_Fails(int rating)
    {
      var db = NewDb(Guid.NewGuid().ToString());

      db.Games.Add(new Game { Id = 1, Name = "Catan" });
      await db.SaveChangesAsync();

      var sut = new ReviewService(db);

      var result = await sut.CreateReviewAsync("user-1", 1, rating, "Text");

      Assert.That(result.Success, Is.False);
      Assert.That(result.ErrorMessage, Does.Contain("rating").IgnoreCase);
    }

    [TestCase("")]
    [TestCase("   ")]
    public async Task CreateReviewAsync_EmptyText_Fails(string text)
    {
      var db = NewDb(Guid.NewGuid().ToString());

      db.Games.Add(new Game { Id = 1, Name = "Catan" });
      await db.SaveChangesAsync();

      var sut = new ReviewService(db);

      var result = await sut.CreateReviewAsync("user-1", 1, 7, text);

      Assert.That(result.Success, Is.False);
      Assert.That(result.ErrorMessage, Does.Contain("text").IgnoreCase);
    }

    [Test]
    public async Task CreateReviewAsync_DuplicateReview_Fails()
    {
      var db = NewDb(Guid.NewGuid().ToString());

      db.Games.Add(new Game { Id = 1, Name = "Catan" });
      db.Reviews.Add(new Review
      {
        ReviewId = 1,
        GameId = 1,
        UserId = "user-1",
        Rating = 9,
        Text = "First review",
        CreatedAt = DateTime.UtcNow
      });

      await db.SaveChangesAsync();

      var sut = new ReviewService(db);

      var result = await sut.CreateReviewAsync("user-1", 1, 8, "Second attempt");

      Assert.That(result.Success, Is.False);
      Assert.That(result.ErrorMessage, Does.Contain("already").IgnoreCase);
    }

    [Test]
    public async Task CreateReviewAsync_GameDoesNotExist_Fails()
    {
      var db = NewDb(Guid.NewGuid().ToString());

      var sut = new ReviewService(db);

      var result = await sut.CreateReviewAsync("user-1", 999, 7, "Text");

      Assert.That(result.Success, Is.False);
      Assert.That(result.ErrorMessage, Does.Contain("game").IgnoreCase);
    }
  }
}