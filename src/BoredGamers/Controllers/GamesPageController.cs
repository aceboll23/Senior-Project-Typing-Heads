using Microsoft.AspNetCore.Mvc;
using BoredGamers.Services.Games;
using BoredGamers.Services.Collections;
using System.Security.Claims;

namespace BoredGamers.Controllers
{
    // Serves the Games HTML pages (Razor views). this is separate from GamesController
    // because that controller uses [ApiController] and returns JSON, and mixing API and
    // view responses in one controller would require removing [ApiController], which
    // change how model binding/error handling work for the existing API endpoints.
    public class GamesPageController : Controller
    {
        private readonly IGameService _gameService;

        private readonly IUserCollectionService _collectionService;
        //dependency injection
        public GamesPageController(IGameService gameService, IUserCollectionService collectionService)
        {
            _gameService = gameService;
            _collectionService = collectionService;
        }


         // Example: GET /Games/SearchResults?q=catan&playTime=30-60&playerCount=4&minRating=7
        [Route("Games/SearchResults")]
        public IActionResult SearchResults(string q, string playTime, int? playerCount, decimal? minRating)
        {
            ViewData["SearchQuery"] = q;
            ViewData["PlayTime"] = playTime;
            ViewData["PlayerCount"] = playerCount;
            ViewData["MinRating"] = minRating;
            return View();
        }

        [Route("Games")]
        public async Task<IActionResult> Index(
            int page = 1,
            int? maxPlayTime = null,
            int? playerCount = null,
            decimal? minRating = null)
        {
            var games = await _gameService.GetBrowseGamesFilteredAsync(
                page,
                20,
                maxPlayTime,
                playerCount,
                minRating);
            
            ViewData["CurrentPage"] = page;
            ViewData["MaxPlayTime"] = maxPlayTime;
            ViewData["PlayerCount"] = playerCount;
            ViewData["MinRating"] = minRating;

            return View(games);
        }


        // Example: GET /Games/Details/5 (5 being a game id)
        [Route("Games/Details/{id}")]
        public async Task<IActionResult> Details(int id)
        {
            // Fetch the game from the database by its Id
            var game = await _gameService.GetGameByIdAsync(id);

            if (game == null)
            {
                return View("NotFound");
            }
            if (User.Identity?.IsAuthenticated == true)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (!string.IsNullOrEmpty(userId))
                {
                    ViewBag.IsInCollection = await _collectionService
                        .IsInCollectionAsync(userId, game.Id);
                }
                else
                {
                    ViewBag.IsInCollection = false;
                }
            }
            else
            {
                ViewBag.IsInCollection = false;
            }

            // Pass the game to Views/GamesPage/Details.cshtml
            return View(game);
        }
    }
}