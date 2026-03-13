using System;
using System.Linq;
using System.Threading.Tasks;
using BoredGamers.Data;
using BoredGamers.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace BoredGamers.Services.GameNightEvents
{
  // GameNightEventService
  // Central place for game night event queries and commands
  public class GameNightEventService : IGameNightEventService
  {
    private readonly ApplicationDbContext _db;

    public GameNightEventService(ApplicationDbContext db)
    {
      _db = db;
    }

    public async Task<GameNightEvent> CreateEventAsync(
      int playgroupId,
      string userId,
      string title,
      DateTime eventDateTime,
      string? description)
    {
      var gameNightEvent = new GameNightEvent
      {
        PlaygroupId = playgroupId,
        CreatedByUserId = userId,
        Title = title.Trim(),
        EventDateTime = eventDateTime,
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim()
      };

      _db.GameNightEvents.Add(gameNightEvent);
      await _db.SaveChangesAsync();

      return gameNightEvent;
    }

    public async Task<GameNightEvent?> GetEventByIdAsync(int eventId)
    {
      return await _db.GameNightEvents
        .AsNoTracking()
        .Include(e => e.Playgroup)
        .Include(e => e.CreatedByUser)
        .Include(e => e.EventGames)
          .ThenInclude(eg => eg.Game)
        .Include(e => e.EventGames)
          .ThenInclude(eg => eg.User)
        .FirstOrDefaultAsync(e => e.Id == eventId);
    }

    public async Task<bool> UserCanAccessEventAsync(int eventId, string userId)
    {
      var playgroupId = await _db.GameNightEvents
        .AsNoTracking()
        .Where(e => e.Id == eventId)
        .Select(e => (int?)e.PlaygroupId)
        .FirstOrDefaultAsync();

      if (!playgroupId.HasValue)
      {
        return false;
      }

      return await UserIsPlaygroupMemberAsync(playgroupId.Value, userId);
    }

    public async Task<bool> UserIsPlaygroupMemberAsync(int playgroupId, string userId)
    {
      return await _db.PlaygroupMembers
        .AsNoTracking()
        .AnyAsync(m => m.PlaygroupId == playgroupId && m.UserId == userId);
    }
    
    public async Task<IReadOnlyList<Game>> GetUserCollectionForEventAsync(int eventId, string userId)
    {
      var playgroupId = await _db.GameNightEvents
        .AsNoTracking()
        .Where(e => e.Id == eventId)
        .Select(e => (int?)e.PlaygroupId)
        .FirstOrDefaultAsync();

      if (!playgroupId.HasValue)
      {
        return new List<Game>();
      }

      var isMember = await UserIsPlaygroupMemberAsync(playgroupId.Value, userId);
      if (!isMember)
      {
        return new List<Game>();
      }

      return await _db.UserGameCollections
        .AsNoTracking()
        .Where(ugc => ugc.UserId == userId)
        .Include(ugc => ugc.Game)
        .Where(ugc => !_db.GameNightEventGames.Any(eg =>
          eg.GameNightEventId == eventId &&
          eg.GameId == ugc.GameId))
        .Select(ugc => ugc.Game)
        .OrderBy(g => g.Name)
        .ToListAsync();
    }

    public async Task<bool> AddGameToEventAsync(int eventId, int gameId, string userId)
    {
      var gameNightEvent = await _db.GameNightEvents
        .FirstOrDefaultAsync(e => e.Id == eventId);

      if (gameNightEvent == null)
      {
        return false;
      }

      var isMember = await UserIsPlaygroupMemberAsync(gameNightEvent.PlaygroupId, userId);
      if (!isMember)
      {
        return false;
      }

      var ownsGame = await _db.UserGameCollections
        .AnyAsync(ugc => ugc.UserId == userId && ugc.GameId == gameId);

      if (!ownsGame)
      {
        return false;
      }

      var alreadyAdded = await _db.GameNightEventGames
        .AnyAsync(eg => eg.GameNightEventId == eventId && eg.GameId == gameId);

      if (alreadyAdded)
      {
        return false;
      }

      var eventGame = new GameNightEventGame
      {
        GameNightEventId = eventId,
        GameId = gameId,
        UserId = userId
      };

      _db.GameNightEventGames.Add(eventGame);
      var rowsChanged = await _db.SaveChangesAsync();

      return rowsChanged > 0;
    }

    public async Task<bool> UserHasAnyCollectionGamesAsync(string userId)
    {
      return await _db.UserGameCollections
        .AsNoTracking()
        .AnyAsync(ugc => ugc.UserId == userId);
    }
    public async Task<int> GetUserCollectionCountAsync(string userId)
    {
      return await _db.UserGameCollections
        .AsNoTracking()
        .CountAsync(ugc => ugc.UserId == userId);
    }

    public async Task<bool> PlaygroupHasEventOnDateAsync(int playgroupId, DateTime eventDateTime)
    {
      var eventDate = eventDateTime.Date;

      return await _db.GameNightEvents
        .AsNoTracking()
        .AnyAsync(e => e.PlaygroupId == playgroupId
                    && e.EventDateTime.Date == eventDate);
    }

    public async Task<bool> UserCanRemoveEventGameAsync(int eventGameId, string userId)
    {
      return await _db.GameNightEventGames
        .AsNoTracking()
        .AnyAsync(eg => eg.Id == eventGameId && eg.UserId == userId);
    }

    public async Task<bool> RemoveGameFromEventAsync(int eventGameId, string userId)
    {
      var eventGame = await _db.GameNightEventGames
        .FirstOrDefaultAsync(eg => eg.Id == eventGameId && eg.UserId == userId);

      if (eventGame == null)
      {
        return false;
      }

      _db.GameNightEventGames.Remove(eventGame);
      var rowsChanged = await _db.SaveChangesAsync();

      return rowsChanged > 0;
    }

    public async Task<bool> UserCanEditEventAsync(int eventId, string userId)
    {
      return await _db.GameNightEvents
        .AsNoTracking()
        .AnyAsync(e => e.Id == eventId && e.CreatedByUserId == userId);
    }

    public async Task<bool> UpdateEventAsync(int eventId, string userId, string title, DateTime eventDateTime, string? description)
    {
      var gameNightEvent = await _db.GameNightEvents
        .FirstOrDefaultAsync(e => e.Id == eventId && e.CreatedByUserId == userId);
      
      if (gameNightEvent == null)
      {
        return false;
      }

      gameNightEvent.Title = title.Trim();
      gameNightEvent.EventDateTime = eventDateTime;
      gameNightEvent.Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

      var rowsChanged = await _db.SaveChangesAsync();
      return rowsChanged > 0;
    }

    public async Task<bool> CancelEventAsync(int eventId, string userId)
    {
      var gameNightEvent = await _db.GameNightEvents
        .Include(e => e.EventGames)
        .FirstOrDefaultAsync(e => e.Id == eventId && e.CreatedByUserId == userId);

      if (gameNightEvent == null)
      {
        return false;
      }

      if (gameNightEvent.EventGames.Any())
      {
        _db.GameNightEventGames.RemoveRange(gameNightEvent.EventGames);
      }

      _db.GameNightEvents.Remove(gameNightEvent);
      var rowsChanged = await _db.SaveChangesAsync();

      return rowsChanged > 0;
    }
  }
}