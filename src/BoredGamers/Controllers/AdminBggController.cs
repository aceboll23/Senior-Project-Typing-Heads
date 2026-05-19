using System.Threading;
using System.Threading.Tasks;
using BoredGamers.Services.Games;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;

namespace BoredGamers.Controllers
{
  [Authorize]
  [ApiController]
  [Route("api/admin/bgg")]
  [IgnoreAntiforgeryToken]
  public class AdminBggController : ControllerBase
  {
    private readonly IGameSyncService _sync;
    private readonly ILogger<AdminBggController> _logger;

    public AdminBggController(IGameSyncService sync, ILogger<AdminBggController> logger)
    {
      _sync = sync;
      _logger = logger;
    }

    //POST /api/admin/bgg/sync-top100?limit=100
    [HttpPost("sync-top")]
    public async Task<IActionResult> SyncTop([FromQuery] int limit = 100, CancellationToken ct = default)
    {
      //Safety: cap limit
      if (limit < 1) limit = 1;
      if (limit > 150) limit = 150;

      var count = await _sync.SyncTopRankedAsync(limit, ct);
      _logger.LogInformation("Manual BGG sync triggered. Saved/updated {Count} games.", count);

      return Ok(new { updated = count, limit });
    }

    //POST /api/admin/bgg/sync-ids
    [HttpPost("sync-ids")]
    public async Task<IActionResult> SyncIds([FromBody] List<int> ids, CancellationToken ct = default)
    {
      if (ids == null || ids.Count == 0)
        return BadRequest(new { error = "Provide a JSON array of BGG ids." });

      //Safety: cap list size
      if (ids.Count > 200)
        ids = ids.Take(200).ToList();

      var count = await _sync.SyncByIdsAsync(ids, ct);
      _logger.LogInformation("Manual BGG ID sync triggered.Saved/Updated {Count} games. Requested={Requested}.", count, ids.Count);

      return Ok(new { updated = count, requested = ids.Count });
    }
  }
}