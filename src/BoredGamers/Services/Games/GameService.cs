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
        .Where(g => g.BggRank != null)
        .OrderBy(g => g.BggRank)
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
        .OrderBy(g => !g.BggRank.HasValue)
        .ThenBy(g => g.BggRank)
        .Take(limit)
        .ToListAsync();
    }

     public async Task<Game?> GetGameByIdAsync(int id)
    {
      return await _db.Games
        .AsNoTracking() //we're just reading, not editing
        .FirstOrDefaultAsync(g => g.Id == id);
    }

  }
}