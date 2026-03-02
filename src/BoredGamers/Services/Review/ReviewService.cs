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

    public ReviewService(ApplicationDbContext db)
    {
      _db = db;
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