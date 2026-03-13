using System.Collections.Generic;
using System.Threading.Tasks;
using BoredGamers.Models;

namespace BoredGamers.Services.Games
{
  public interface IGameService
  {
    Task<IReadOnlyList<Game>> GetTopGamesAsync(int limit = 100);
    Task<IReadOnlyList<GameSearchResult>> SearchGamesAsync(string query, int limit);
    Task<Game?> GetGameByIdAsync(int id);
    Task<IReadOnlyList<Game>> SearchGamesFilteredAsync(string? query, int? minPlayTime, int? maxPlayTime, int? playerCount, decimal? minRating, int limit);
  }
}