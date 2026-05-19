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

      // Notify all playgroup members except the creator
      var memberUserIds = await _db.PlaygroupMembers
        .Where(m => m.PlaygroupId == playgroupId && m.UserId != userId)
        .Select(m => m.UserId)
        .ToListAsync();

      var memberProfiles = await _db.Set<UserProfile>()
        .Where(p => memberUserIds.Contains(p.UserId))
        .ToListAsync();

      foreach (var profile in memberProfiles)
      {
        _db.Set<Notification>().Add(new Notification
        {
          UserProfileId = profile.Id,
          Type = "GameNightEvent",
          Title = "New Game Night Event",
          Message = $"A new event '{gameNightEvent.Title}' was created in your playgroup!",
          ActionUrl = $"/GameNightEvent/Details/{gameNightEvent.Id}",
          RelatedEntityId = gameNightEvent.Id,
          CreatedAt = DateTime.UtcNow
        });
      }

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
        .Include(e => e.GameVotes)
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
          var eventGameIds = gameNightEvent.EventGames.Select(eg => eg.Id).ToList();

          // Delete votes first (they reference EventGames)
          var votes = await _db.GameVotes
              .Where(v => eventGameIds.Contains(v.GameNightEventGameId))
              .ToListAsync();
          if (votes.Any())
          {
              _db.GameVotes.RemoveRange(votes);
          }

          _db.GameNightEventGames.RemoveRange(gameNightEvent.EventGames);
      }

      _db.GameNightEvents.Remove(gameNightEvent);
      var rowsChanged = await _db.SaveChangesAsync();
      return rowsChanged > 0;
    }
    public async Task<bool> RespondToEventAsync(int eventId, string userId, ResponseStatus status)
    {
      var canAccess = await UserCanAccessEventAsync(eventId, userId);
      if (!canAccess) return false;

      var existing = await _db.EventResponses
        .FirstOrDefaultAsync(r => r.GameNightEventId == eventId && r.UserId == userId);

      if (existing != null)
      {
        existing.Status = status;
        existing.RespondedAt = DateTime.UtcNow;
      }
      else
      {
        _db.EventResponses.Add(new EventResponse
        {
          GameNightEventId = eventId,
          UserId = userId,
          Status = status,
          RespondedAt = DateTime.UtcNow
        });
      }

      return await _db.SaveChangesAsync() > 0;
    }

    public async Task<EventResponse?> GetUserResponseAsync(int eventId, string userId)
    {
      return await _db.EventResponses
        .AsNoTracking()
        .FirstOrDefaultAsync(r => r.GameNightEventId == eventId && r.UserId == userId);
    }

    public async Task<List<EventResponse>> GetEventResponsesAsync(int eventId)
    {
      return await _db.EventResponses
        .AsNoTracking()
        .Where(r => r.GameNightEventId == eventId)
        .ToListAsync();
    }

    public async Task<Dictionary<string, string>> GetResponderNamesAsync(List<EventResponse> responses)
    {
        var userIds = responses.Select(r => r.UserId).Distinct().ToList();
        return await _db.Users
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.UserName ?? "Unknown");
    }


    // voting
    public async Task<bool> OpenVotingAsync(int eventId, string userId)
    {
        var gameNightEvent = await _db.GameNightEvents
            .FirstOrDefaultAsync(e => e.Id == eventId && e.CreatedByUserId == userId);

        if (gameNightEvent == null) return false;
        if (!gameNightEvent.EventGames.Any())
        {
            gameNightEvent = await _db.GameNightEvents
                .Include(e => e.EventGames)
                .FirstOrDefaultAsync(e => e.Id == eventId);
        }

        if (gameNightEvent == null || !gameNightEvent.EventGames.Any()) return false;

        gameNightEvent.VotingStatus = VotingStatus.Open;
        return await _db.SaveChangesAsync() > 0;
    }

    public async Task<bool> CloseVotingAsync(int eventId, string userId)
    {
        var gameNightEvent = await _db.GameNightEvents
            .FirstOrDefaultAsync(e => e.Id == eventId && e.CreatedByUserId == userId);

        if (gameNightEvent == null) return false;

        gameNightEvent.VotingStatus = VotingStatus.Closed;
        return await _db.SaveChangesAsync() > 0;
    }

    public async Task<bool> SubmitRankingsAsync(int eventId, string userId, Dictionary<int, int> gameRanks)
    {
        var canAccess = await UserCanAccessEventAsync(eventId, userId);
        if (!canAccess) return false;

        var gameNightEvent = await _db.GameNightEvents
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == eventId);

        if (gameNightEvent == null || gameNightEvent.VotingStatus != VotingStatus.Open)
            return false;

        // Remove existing votes for this user on this event
        var existing = await _db.GameVotes
            .Where(v => v.GameNightEventId == eventId && v.UserId == userId)
            .ToListAsync();

        if (existing.Any())
            _db.GameVotes.RemoveRange(existing);

        // Add new rankings
        foreach (var (eventGameId, rank) in gameRanks)
        {
            _db.GameVotes.Add(new GameVote
            {
                GameNightEventId = eventId,
                GameNightEventGameId = eventGameId,
                UserId = userId,
                Rank = rank,
                SubmittedAt = DateTime.UtcNow
            });
        }

        return await _db.SaveChangesAsync() > 0;
    }

    public async Task<List<GameVote>> GetVotesForEventAsync(int eventId)
    {
        return await _db.GameVotes
            .AsNoTracking()
            .Where(v => v.GameNightEventId == eventId)
            .ToListAsync();
    }

    public async Task<Dictionary<int, int>> GetUserRankingsAsync(int eventId, string userId)
    {
        return await _db.GameVotes
            .AsNoTracking()
            .Where(v => v.GameNightEventId == eventId && v.UserId == userId)
            .ToDictionaryAsync(v => v.GameNightEventGameId, v => v.Rank);
    }

    public async Task<List<(GameNightEventGame EventGame, int TotalScore, bool IsWinner)>> GetVotingResultsAsync(int eventId)
    {
        var eventGames = await _db.GameNightEventGames
            .AsNoTracking()
            .Include(eg => eg.Game)
            .Include(eg => eg.User)
            .Where(eg => eg.GameNightEventId == eventId)
            .ToListAsync();

        var votes = await _db.GameVotes
            .AsNoTracking()
            .Where(v => v.GameNightEventId == eventId)
            .ToListAsync();

        // Sum ranks per game — lower is better
        var scores = eventGames.Select(eg => new
        {
            EventGame = eg,
            TotalScore = votes
                .Where(v => v.GameNightEventGameId == eg.Id)
                .Sum(v => v.Rank)
        }).ToList();

        if (!scores.Any())
            return scores.Select(s => (s.EventGame, s.TotalScore, false)).ToList();

        // Games with no votes get a high penalty score so they rank last
        var maxScore = scores.Max(s => s.TotalScore);
        var adjustedScores = scores.Select(s => new
        {
            s.EventGame,
            TotalScore = s.TotalScore == 0 ? maxScore + 1 : s.TotalScore
        }).ToList();

        var lowestScore = adjustedScores.Min(s => s.TotalScore);

        return adjustedScores
            .Select(s => (s.EventGame, s.TotalScore, s.TotalScore == lowestScore))
            .OrderBy(s => s.TotalScore)
            .ToList();
    }
  }
}