using System;
using System.Threading.Tasks;
using System. Collections.Generic;
using BoredGamers.Models;

namespace BoredGamers.Services.GameNightEvents
{
  public interface IGameNightEventService
  {
    Task<GameNightEvent> CreateEventAsync(int playgroupId, string userId, string title, DateTime eventDateTime, string? description);
    Task<GameNightEvent?> GetEventByIdAsync(int eventId);
    Task<bool> UserCanAccessEventAsync(int eventId, string userId);
    Task<bool> UserIsPlaygroupMemberAsync(int playgroupId, string userId);
    Task<IReadOnlyList<Game>> GetUserCollectionForEventAsync(int eventId, string userId);
    Task<bool> AddGameToEventAsync(int eventId, int gameId, string userId);
    Task<bool> UserHasAnyCollectionGamesAsync(string userId);
    Task<int> GetUserCollectionCountAsync(string userId);
    Task<bool> PlaygroupHasEventOnDateAsync(int playgroupId, DateTime eventDateTime);
    Task<bool> UserCanRemoveEventGameAsync(int eventGameId, string userId);
    Task<bool> RemoveGameFromEventAsync(int eventGameId, string userId);
    Task<bool> UserCanEditEventAsync(int eventId, string userId);
    Task<bool> UpdateEventAsync(int eventId, string userId, string title, DateTime eventDateTime, string? description);
    Task<bool> CancelEventAsync(int eventId, string userId);
    Task<bool> RespondToEventAsync(int eventId, string userId, ResponseStatus status);
    Task<EventResponse?> GetUserResponseAsync(int eventId, string userId);
    Task<List<EventResponse>> GetEventResponsesAsync(int eventId);
    Task<Dictionary<string, string>> GetResponderNamesAsync(List<EventResponse> responses);
    // Voting
    Task<bool> OpenVotingAsync(int eventId, string userId);
    Task<bool> CloseVotingAsync(int eventId, string userId);
    Task<bool> SubmitRankingsAsync(int eventId, string userId, Dictionary<int, int> gameRanks);
    Task<List<GameVote>> GetVotesForEventAsync(int eventId);
    Task<Dictionary<int, int>> GetUserRankingsAsync(int eventId, string userId);
    Task<List<(GameNightEventGame EventGame, int TotalScore, bool IsWinner)>> GetVotingResultsAsync(int eventId);
  }
}