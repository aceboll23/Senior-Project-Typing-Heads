using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BoredGamers.Services.Bgg
{
  //Placeholder implementation.
  public class BggClient : IBggClient
  {
    public Task<IReadOnlyList<BggTopGame>> GetTopRankedGamesAsync(int limit = 100, CancellationToken ct = default)
    {
      //Intentionally not implemented yet.
      //We'll implement BGG fetching/parsing next
      return Task.FromResult<IReadOnlyList<BggTopGame>>(new List<BggTopGame>());
    }
  }
}