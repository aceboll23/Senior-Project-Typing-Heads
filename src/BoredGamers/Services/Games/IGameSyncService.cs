using System.Threading;
using System.Threading.Tasks;

namespace BoredGamers.Services.Games
{
  public interface IGameSyncService
  {
    Task<int> SyncTopRankedAsync(int limit = 100, CancellationToken ct = default);
    Task<int> SyncByIdsAsync(IEnumerable<int> bggIds, CancellationToken ct = default);
  }
}