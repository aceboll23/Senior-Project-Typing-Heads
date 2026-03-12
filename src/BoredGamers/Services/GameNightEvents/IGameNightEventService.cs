using System.Threading.Tasks;
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
  }
}