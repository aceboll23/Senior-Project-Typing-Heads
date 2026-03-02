using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BoredGamers.Data;
using BoredGamers.Models;
using Microsoft.EntityFrameworkCore;

namespace BoredGamers.Services.Games
{
  //GameService
  //Central place for game queries 
  //DB-only queries (no live calls to BGG).
  public class GameService : IGameService
  {
    private readonly ApplicationDbContext _db;

    public GameService(ApplicationDbContext db)
    {
      _db = db;
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

    public async Task<IReadOnlyList<Game>> SearchGamesAsync(string query, int limit)
    {
      query = (query ?? string.Empty).Trim();

      if (limit < 1) limit = 1;
      if (limit > 50) limit = 50; //keeps search "cheap"

      if (string.IsNullOrWhiteSpace(query))
      {
        return new List<Game>();
      }

      //Simple MVP search: name contains query (DB-only)
      //Later we can upgrade to full-text search or ranking logic
      return await _db.Games
        .AsNoTracking()
        .Where(g => g.Name.ToLower().Contains(query.ToLower()))
        .OrderByDescending(g => (double?)g.AverageRating)
        .ThenBy(g => g.Name)
        .Take(limit)
        .ToListAsync();
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
  }
}