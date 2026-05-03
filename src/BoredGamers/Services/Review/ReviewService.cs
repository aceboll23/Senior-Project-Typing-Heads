using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using BoredGamers.Data;
using BoredGamers.Models;

namespace BoredGamers.Services
{
  public class ReviewService
  {
    private readonly ApplicationDbContext _db;
    private readonly Services.ContentModeration.IContentModerationService _moderation;

    public ReviewService(ApplicationDbContext db, Services.ContentModeration.IContentModerationService moderation)
    {
      _db = db;
      _moderation = moderation;
    }

    public async Task<ServiceResult> CreateReviewAsync(string userId, int gameId, int rating, string text)
    {
      // Basic validation to satisfy tests
      if (string.IsNullOrWhiteSpace(userId))
        return ServiceResult.Fail("User is required.");

      if (rating < 1 || rating > 10)
        return ServiceResult.Fail("Rating must be between 1 and 10.");

      if (string.IsNullOrWhiteSpace(text))
        return ServiceResult.Fail("Text is required.");

      // Content moderation check
      var moderationResult = await _moderation.CheckContentAsync(text);
      if (moderationResult.IsFlagged)
          return ServiceResult.Fail("Your review was rejected for inappropriate language. Please revise and try again.");

      var gameExists = await _db.Games.AnyAsync(g => g.Id == gameId);
      if (!gameExists)
        return ServiceResult.Fail("Game not found.");

      var alreadyReviewed = await _db.Reviews.AnyAsync(r => r.GameId == gameId && r.UserId == userId);
      if (alreadyReviewed)
        return ServiceResult.Fail("You have already reviewed this game.");

      var review = new Review
      {
        GameId = gameId,
        UserId = userId,
        Rating = rating,
        Text = text.Trim(),
        CreatedAt = DateTime.UtcNow
      };

      _db.Reviews.Add(review);
      await _db.SaveChangesAsync();

      return ServiceResult.Ok();
    }
    public async Task<ServiceResult> EditReviewAsync(int reviewId, string userId, int rating, string text)
    {
      if (string.IsNullOrWhiteSpace(userId))
        return ServiceResult.Fail("User is required.");

      if (rating < 1 || rating > 10)
        return ServiceResult.Fail("Rating must be between 1 and 10.");

      if (string.IsNullOrWhiteSpace(text))
        return ServiceResult.Fail("Text is required.");

      var moderationResult = await _moderation.CheckContentAsync(text);
      if (moderationResult.IsFlagged)
          return ServiceResult.Fail("Your review was rejected for inappropriate language. Please revise and try again.");

      var review = await _db.Reviews.FirstOrDefaultAsync(r => r.ReviewId == reviewId);

      if (review == null)
        return ServiceResult.Fail("Review not found.");

      if (review.UserId != userId)
        return ServiceResult.Fail("You can only edit your own review.");

      review.Rating = rating;
      review.Text = text.Trim();

      await _db.SaveChangesAsync();

      return ServiceResult.Ok();
    }

    public async Task<Review?> GetReviewForEditAsync(int reviewId, string userId)
    {
      if (string.IsNullOrWhiteSpace(userId))
        return null;

      return await _db.Reviews
        .FirstOrDefaultAsync(r => r.ReviewId == reviewId && r.UserId == userId);
    }

    public async Task<decimal?> GetAverageUserRatingAsync(int gameId)
    {
      var ratings = await _db.Reviews
        .Where(r => r.GameId == gameId)
        .Select(r => r.Rating)
        .ToListAsync();

      if (ratings.Count == 0)
        return null;

      return (decimal)ratings.Average();
    }

    public IReadOnlyList<Review> SortReviews(IEnumerable<Review> reviews, string? sort)
    {
      return sort switch
      {
        "rating-desc" => reviews.OrderByDescending(r => r.Rating).ThenByDescending(r => r.CreatedAt).ToList(),
        "rating-asc" => reviews.OrderBy(r => r.Rating).ThenByDescending(r => r.CreatedAt).ToList(),
        _ => reviews.OrderByDescending(r => r.CreatedAt).ToList()
      };
    }

    public async Task<ServiceResult> DeleteReviewAsync(int reviewId, string userId)
    {
      if (string.IsNullOrWhiteSpace(userId))
        return ServiceResult.Fail("User is required.");

      var review = await _db.Reviews.FirstOrDefaultAsync(r => r.ReviewId == reviewId);

      if (review == null)
        return ServiceResult.Fail("Review not found.");
      
      if (review.UserId != userId)
        return ServiceResult.Fail("You can only delete your own review.");

      _db.Reviews.Remove(review);
      await _db.SaveChangesAsync();

      return ServiceResult.Ok();
    }
  }

  public class ServiceResult
  {
    public bool Success { get; }
    public string ErrorMessage { get; }

    private ServiceResult(bool success, string errorMessage)
    {
      Success = success;
      ErrorMessage = errorMessage;
    }

    public static ServiceResult Ok() => new ServiceResult(true, "");
    public static ServiceResult Fail(string message) => new ServiceResult(false, message);
  }
}