using System.Linq;
using System.Threading.Tasks;
using BoredGamers.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BoredGamers.Controllersa
{
  //GamesController (API)
  //Provides read-only endpoints that the UI can call to display and search games.
  //These endpoints read from our local database only (no live calls to BGG). 
  [ApiController]
  [Route("api/games")]
  public class GamesController : ControllerBase
  {
    private readonly ApplicationDbContext _db;

    public GamesController(ApplicationDbContext db)
    {
      _db = db;
    }

    //GET /api/games/top?limit=10
    [HttpGet("top")]
    public async Task<IActionResult> GetTopGames(int limit = 10)
    {
      if (limit <= 1) limit = 1;
      if (limit > 100) limit = 100;

      var games = await _db.Games
        .OrderBy(g => g.BggRank)
        .Take(limit)
        .Select(g => new
        {
          g.Id,
          g.Name,
          g.YearPublished,
          g.ThumbnailUrl,
          g.ImageUrl,
          g.BggRank,
          g.AverageRating
        })
        .ToListAsync();

      return Ok(games);
    }
  }
}