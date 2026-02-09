using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BoredGamers.Services.Bgg
{
  //Contract for retrieving BGG top ranked games.
  //Implementation wil fetch from BGG during sync only (never during page render).
  public interface IBggClient
  {
    Task<IReadOnlyList<BggTopGame>> GetTopRankedGamesAsync(int limit = 100, CancellationToken ct = default);
  }
}