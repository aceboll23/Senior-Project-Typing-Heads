using System.Collections.Generic;
using System.Threading.Tasks;
using BoredGamers.Models;

namespace BoredGamers.Services.Games
{
  public interface IGameService
  {
    Task<IReadOnlyList<Game>> GetTopGamesAsync(int limit = 100);
    Task<IReadOnlyList<Game>> SearchGamesAsync(string query, int limit);
  }
}