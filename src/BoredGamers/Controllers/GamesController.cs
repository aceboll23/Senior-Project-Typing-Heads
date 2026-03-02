using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;
using BoredGamers.Data;
using BoredGamers.Services.Games;
using BoredGamers.Services.Collections;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BoredGamers.Controllers
{
  //GamesController (API)
  //Provides read-only endpoints that the UI can call to display and search games.
  //These endpoints read from our local database only (no live calls to BGG). 
  [ApiController]
  [Route("api/games")]
  public class GamesController : ControllerBase
  {
    private readonly IGameService _games;

    public GamesController(IGameService games)
    {
      _games = games;
    }

    //GET /api/games/top?limit=10
    [HttpGet("top")]
    public async Task<IActionResult> GetTopGames(int limit = 10)
    {
      if (limit <= 1) limit = 1;
      if (limit > 100) limit = 100;

      var games = await _games.GetTopGamesAsync(limit);
      
      return Ok(games.Select(g => new
      {
        g.Id,
        g.BggGameId,
        g.Name,
        g.YearPublished,
        g.ThumbnailUrl,
        g.ImageUrl,
        g.BggNumVoters,
        g.AverageRating
      }));
    }

    //GET /api/games/search?q=catan&limit=10
    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string q, [FromQuery] int limit = 10)
    {
      //Keep inputs sane
      q = (q ?? string.Empty).Trim();
      if (limit < 1) limit = 1;
      if (limit > 50) limit = 50;

      var results = await _games.SearchGamesAsync(q, limit);

      //Return a small payload (avoid returning the full entity by default)
      return Ok(results.Select(g => new
      {
        g.Id,
        g.BggGameId,
        g.Name,
        g.YearPublished,
        g.ThumbnailUrl,
        g.BggNumVoters
      }));
    }
    
    //GET /api/games/search/filtered?q=catan&minPlayTime=30&maxPlayTime=60&playerCount=4&minRating=7&limit=10
    [HttpGet("search/filtered")]
    public async Task<IActionResult> SearchFiltered(
        [FromQuery] string? q,
        [FromQuery] int? minPlayTime,
        [FromQuery] int? maxPlayTime,
        [FromQuery] int? playerCount,
        [FromQuery] decimal? minRating,
        [FromQuery] int limit = 10)
    {
      if (limit < 1) limit = 1;
      if (limit > 50) limit = 50;

      var results = await _games.SearchGamesFilteredAsync(q, minPlayTime, maxPlayTime, playerCount, minRating, limit);

      return Ok(results.Select(g => new
      {
        g.Id,
        g.BggGameId,
        g.Name,
        g.YearPublished,
        g.ThumbnailUrl,
        g.AverageRating,
        g.MinPlayers,
        g.MaxPlayers,
        g.PlayTime
      }));
    }
  }
}