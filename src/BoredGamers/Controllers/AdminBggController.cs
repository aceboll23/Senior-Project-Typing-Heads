using System.Threading;
using System.Threading.Tasks;
using BoredGamers.Services.Games;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace BoredGamers.Controllers
{
  //Dev-only endpoint to sync Top games from BGG into the local DB.
  [ApiController]
  [Route("api/admin/bgg")]
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
  }
}