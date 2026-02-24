using System;
using System.Collections.Generic;
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
          game.LastSyncedAt = now;

          if (detailsById.TryGetValue(item.BggGameId, out var d))
          {
            game.YearPublished = d.YearPublished;
            game.ThumbnailUrl = d.ThumbnailUrl;
            game.ImageUrl = d.ImageUrl;
            game.AverageRating = d.AverageRating;
            game.Description = d.Description;
            game.MinPlayers = d.MinPlayers;
            game.MaxPlayers = d.MaxPlayers;
            game.PlayTime = d.PlayTime;
            game.BggNumVoters = d.UsersRated;
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
            YearPublished = d?.YearPublished,
            ThumbnailUrl = d?.ThumbnailUrl,
            ImageUrl = d?.ImageUrl,
            AverageRating = d?.AverageRating,
            Description = d?.Description,
            MinPlayers = d?.MinPlayers,
            MaxPlayers = d?.MaxPlayers,
            PlayTime = d?.PlayTime,
            BggNumVoters = d?.UsersRated,
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

    public async Task<int> SyncByIdsAsync(IEnumerable<int> bggIds, CancellationToken ct = default)
    {
      var now = DateTime.UtcNow;

      var ids = bggIds?
        .Where(id => id > 0)
        .Distinct()
        .ToList() ?? new List<int>();

      if (ids.Count == 0) return 0;

      //Fetch details for all requested IDs (batched inside the BggClient)
      var detailsById = await _bgg.GetGameDetailsAsync(ids, ct);

      //Load existing rows for these IDs
      var existing = await _db.Games
        .Where(g => ids.Contains(g.BggGameId))
        .ToDictionaryAsync(g => g.BggGameId, ct);

      int changes = 0;

      foreach (var id in ids)
      {
        //if BGG didn't return details for this id, skip it
        if (!detailsById.TryGetValue(id, out var d)) continue;

        if (existing.TryGetValue(id, out var game))
        {
          //Update existing row
          if (!string.IsNullOrWhiteSpace(d.Name))
            game.Name = d.Name;
          game.YearPublished = d.YearPublished;
          game.ThumbnailUrl = d.ThumbnailUrl;
          game.ImageUrl = d.ImageUrl;
          game.AverageRating = d.AverageRating;

          game.Description = d.Description;
          game.MinPlayers = d.MinPlayers;
          game.MaxPlayers = d.MaxPlayers;
          game.PlayTime = d.PlayTime;
          game.BggNumVoters = d.UsersRated;

          game.LastSyncedAt = now;

          changes ++;
        }
        else
        {
          //Insert new row
          _db.Games.Add(new Game
          {
            BggGameId = id,
            Name = d.Name ?? $"BGG Game {id}",
            YearPublished = d.YearPublished,
            ThumbnailUrl = d.ThumbnailUrl,
            ImageUrl = d.ImageUrl,
            AverageRating = d.AverageRating,

            Description = d.Description,
            MinPlayers = d.MinPlayers,
            MaxPlayers = d.MaxPlayers,
            PlayTime = d.PlayTime,
            BggNumVoters = d.UsersRated,

            LastSyncedAt = now
          });
          changes++;
        }
      }

      await _db.SaveChangesAsync(ct);

      _logger.LogInformation("BGG ID sync saved/updated {Count} games at {UtcNow}. Requested={Requested} DetailsReturned={Returned}.",
        changes, now, ids.Count, detailsById.Count);

      return changes;
    }

    //Backfill Bgg voters for all games currently in the DB
    public async Task<int> BackfillBggNumVotersAsync(CancellationToken ct = default)
    {
      var now = DateTime.UtcNow;

      //Grab all BGG IDs we currently have stored
      var ids = await _db.Games
        .AsNoTracking()
        .Where(g => g.BggGameId > 0)
        .Select(g => g.BggGameId)
        .Distinct()
        .ToListAsync(ct);

      if (ids.Count == 0) return 0;

      var detailsById = await _bgg.GetGameDetailsAsync(ids, ct);

      //Load tracked entities to update
      var games = await _db.Games
        .Where(g => ids.Contains(g.BggGameId))
        .ToListAsync(ct);

      int updates = 0;

      foreach (var game in games)
      {
        if (!detailsById.TryGetValue(game.BggGameId, out var d)) continue;

        game.BggNumVoters = d.UsersRated;
        game.LastSyncedAt = now;
        updates++;
      }

      await _db.SaveChangesAsync(ct);

      _logger.LogInformation("Backfilled BggNumVoters for {Count} games at {UtcNow}.", updates, now);

      return updates;
    }
  }
}