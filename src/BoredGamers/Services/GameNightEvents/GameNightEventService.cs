using System;
using System.Linq;
using System.Threading.Tasks;
using BoredGamers.Data;
using BoredGamers.Models;
using Microsoft.EntityFrameworkCore;

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
  }
}