using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BoredGamers.Services.Bgg;

namespace BoredGamers.Tests.TestDoubles
{
    // FIX: New fake BGG client for tests so GameService can be constructed
    public class FakeBggClient : IBggClient
    {
        // FIX: Stub implementation for top ranked games
        public Task<IReadOnlyList<BggTopGame>> GetTopRankedGamesAsync(int limit = 100, CancellationToken ct = default)
        {
            IReadOnlyList<BggTopGame> result = new List<BggTopGame>();
            return Task.FromResult(result);
        }

        // FIX: Stub implementation for game details lookup
        public Task<IReadOnlyDictionary<int, BggGameDetails>> GetGameDetailsAsync(IEnumerable<int> bggGameIds, CancellationToken ct = default)
        {
            IReadOnlyDictionary<int, BggGameDetails> result = new Dictionary<int, BggGameDetails>();
            return Task.FromResult(result);
        }

        // FIX: Stub implementation for search
        public Task<IReadOnlyList<BggGameDetails>> SearchGamesAsync(string query, int limit = 10, CancellationToken ct = default)
        {
            IReadOnlyList<BggGameDetails> result = new List<BggGameDetails>();
            return Task.FromResult(result);
        }
    }
}