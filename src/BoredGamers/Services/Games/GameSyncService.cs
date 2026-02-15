using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BoredGamers.Data;
using BoredGamers.Models;
using BoredGamers.Services.Bgg;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BoredGamers.Services.Games
{
  //Syncs Top ranked games from BGG into our local DB.
  //IMPORTANT: Used during refresh only; the home page reads from DB.
  public class GameSyncService : IGameSyncService
  {
    private readonly ApplicationDbContext _db;
    private readonly IBggClient _bgg;
    private readonly ILogger<GameSyncService> _logger;

    public GameSyncService(ApplicationDbContext db, IBggClient bgg, ILogger<GameSyncService> logger)
    {
      _db = db;
      _bgg = bgg;
      _logger = logger;
    }

    public async Task<int> SyncTopRankedAsync(int limit = 100, CancellationToken ct = default)
    {
      var now = DateTime.UtcNow;

      var top = await _bgg.GetTopRankedGamesAsync(limit, ct);
      if (top.Count == 0)
      {
        _logger.LogWarning("BGG Top sync returned 0 items. Keeping existing cached Games data.");
        return 0;
      }
       //Load exisiting games that match the incoming IDs
       var incomingIds = top.Select(t => t.BggGameId).ToList();
        //Fetch extra metadata in batches (year/thumb/image/rating)
       var detailsById = await _bgg.GetGameDetailsAsync(incomingIds, ct);

       var existing = await _db.Games
        .Where(g => incomingIds.Contains(g.BggGameId))
        .ToDictionaryAsync(g => g.BggGameId, ct);

      int changes = 0;

      foreach (var item in top)
      {
        if (existing.TryGetValue(item.BggGameId, out var game))
        {
          //Update exisiting row (rank/name can change)
          game.Name = item.Name;
          game.BggRank = item.Rank;
          game.LastSyncedAt = now;

          if (detailsById.TryGetValue(item.BggGameId, out var d))
          {
            game.YearPublished = d.YearPublished;
            game.ThumbnailUrl = d.ThumbnailUrl;
            game.ImageUrl = d.ImageUrl;
            game.AverageRating = d.AverageRating;
          }
          changes ++;
        }
        else
        {
          //Insert new row

          detailsById.TryGetValue(item.BggGameId, out var d);

          _db.Games.Add(new Game
          {
            BggGameId = item.BggGameId,
            Name = item.Name,
            BggRank = item.Rank,
            YearPublished = d?.YearPublished,
            ThumbnailUrl = d?.ThumbnailUrl,
            ImageUrl = d?.ImageUrl,
            AverageRating = d?.AverageRating,
            LastSyncedAt = now
          });
          changes++;
        }
      }

      try
      {
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("BGG Top sync saved/updated {Count} games at {UtcNow}.", changes, now);
        return changes;
      }
      catch (Exception ex)
      {
        //If BGG was available but DB save fails, we still keep serving old cached data.
        _logger.LogError(ex, "BGG Top sync failed while saving to DB. Keeping existing cached Games data.");
        return 0;
      }
    }
  }
}