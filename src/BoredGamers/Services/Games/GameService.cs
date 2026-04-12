using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BoredGamers.Data;
using BoredGamers.Models;
using BoredGamers.Services.Bgg;
using Microsoft.EntityFrameworkCore;

namespace BoredGamers.Services.Games
{
  //GameService
  //Central place for game queries 
  //DB-only queries (no live calls to BGG).
  public class GameService : IGameService
  {
    private readonly ApplicationDbContext _db;
    private readonly IBggClient _bgg;

    public GameService(ApplicationDbContext db, IBggClient bgg)
    {
      _db = db;
      _bgg = bgg;
    }

    public async Task<IReadOnlyList<Game>> GetTopGamesAsync(int limit = 100)
    {
      if (limit < 1) limit = 1;
      if (limit > 50) limit = 50;

      return await _db.Games
        .AsNoTracking()
        .Where(g => g.BggNumVoters.HasValue && g.BggNumVoters.Value >= 100)
        .OrderByDescending(g => (double?)g.AverageRating)
        .ThenBy(g => g.Id)
        .Take(limit)
        .ToListAsync();
    }

    public async Task<IReadOnlyList<GameSearchResult>> SearchGamesAsync(string query, int limit)
    {
      query = (query ?? string.Empty).Trim();

      if (limit < 1) limit = 1;
      if (limit > 50) limit = 50; //keeps search "cheap"

      if (string.IsNullOrWhiteSpace(query))
      {
        return new List<GameSearchResult>();
      }

      var localResults = await _db.Games
        .AsNoTracking()
        .Where(g => g.Name.ToLower().Contains(query.ToLower()))
        .OrderByDescending(g => (double?)g.AverageRating)
        .ThenBy(g => g.Name)
        .Take(limit)
        .ToListAsync();

      if (localResults.Any())
      {
        return localResults.Select(g => new GameSearchResult
        {
          Id = g.Id,
          BggGameId = g.BggGameId,
          Name = g.Name,
          YearPublished = g.YearPublished,
          ThumbnailUrl = g.ThumbnailUrl,
          ImageUrl = g.ImageUrl,
          BggNumVoters = g.BggNumVoters,
          AverageRating = g.AverageRating,
          IsLocal = true
        }).ToList();
      }

      var apiResults = await _bgg.SearchGamesAsync(query, limit);


      return apiResults.Select(g => new GameSearchResult
      {
        Id = null,
        BggGameId = g.BggGameId,
        Name = g.Name ?? string.Empty,
        YearPublished = g.YearPublished,
        ThumbnailUrl = g.ThumbnailUrl,
        ImageUrl = g.ImageUrl,
        BggNumVoters = g.UsersRated,
        AverageRating = g.AverageRating,
        IsLocal = false
      }).ToList();

    }

     public async Task<Game?> GetGameByIdAsync(int id)
    {
      return await _db.Games
        .AsNoTracking() //we're just reading, not editing
        .Include(g => g.Reviews)
          .ThenInclude(r => r.User)
        .FirstOrDefaultAsync(g => g.Id == id);
    }
    public async Task<IReadOnlyList<Game>> SearchGamesFilteredAsync(
        string? query, int? minPlayTime, int? maxPlayTime,
        int? playerCount, decimal? minRating, int limit)
    {
      if (limit < 1) limit = 1;
      if (limit > 50) limit = 50;

      var games = _db.Games.AsNoTracking().AsQueryable();

      if (!string.IsNullOrWhiteSpace(query))
        games = games.Where(g => g.Name.ToLower().Contains(query.ToLower()));

      if (minPlayTime.HasValue)
        games = games.Where(g => g.PlayTime.HasValue && g.PlayTime.Value >= minPlayTime.Value);

      if (maxPlayTime.HasValue)
        games = games.Where(g => g.PlayTime.HasValue && g.PlayTime.Value <= maxPlayTime.Value);

      if (playerCount.HasValue)
        games = games.Where(g => g.MinPlayers.HasValue && g.MaxPlayers.HasValue
            && g.MinPlayers.Value <= playerCount.Value
            && g.MaxPlayers.Value >= playerCount.Value);

      if (minRating.HasValue)
        games = games.Where(g => g.AverageRating.HasValue && g.AverageRating.Value >= minRating.Value);

      return await games
        .OrderByDescending(g => (double?)g.AverageRating)
        .ThenBy(g => g.Name)
        .Take(limit)
        .ToListAsync();
    }

    public async Task<IReadOnlyList<Game>> GetBrowseGamesAsync(int limit)
    {
      if (limit < 1) limit = 1;
      if (limit > 50) limit = 50;

      return await _db.Games
        .AsNoTracking()
        .OrderBy(g => g.Name)
        .Take(limit)
        .ToListAsync();
    }    
    public async Task<Game?> SaveGameFromBggAsync(int bggGameId)
    {
      if (bggGameId <= 0)
      {
        return null;
      }

      var existingGame = await _db.Games
        .FirstOrDefaultAsync(g => g.BggGameId == bggGameId);

      if (existingGame != null)
      {
        return existingGame;
      }

      var details = await _bgg.GetGameDetailsAsync(new[] { bggGameId });

      if (!details.TryGetValue(bggGameId, out var bggGame) || string.IsNullOrWhiteSpace(bggGame.Name))
      {
        return null;
      }

      var game = new Game
      {
        BggGameId = bggGame.BggGameId,
        Name = bggGame.Name,
        YearPublished = bggGame.YearPublished,
        ThumbnailUrl = bggGame.ThumbnailUrl,
        ImageUrl = bggGame.ImageUrl,
        AverageRating = bggGame.AverageRating,
        BggNumVoters = bggGame.UsersRated,
        Description = bggGame.Description,
        MinPlayers = bggGame.MinPlayers,
        MaxPlayers = bggGame.MaxPlayers,
        PlayTime = bggGame.PlayTime,
        LastSyncedAt = DateTime.UtcNow
      };

      _db.Games.Add(game);
      await _db.SaveChangesAsync();

      return game;
    }
  }
}