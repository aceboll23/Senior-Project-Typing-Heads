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


        // Example: GET /Games/SearchResults?q=dewan
        [Route("Games/SearchResults")]
        public IActionResult SearchResults(string q)
        {
            ViewData["SearchQuery"] = q;
            
            // This looks for the view that matches the name "SearchResults", 
            // it takes GamesPageController, strips off "Controller" and 
            // looks for Views/{GamesPage}/{SearchResults}.cshtml
            return View(); 
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