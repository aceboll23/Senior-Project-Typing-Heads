using Microsoft.AspNetCore.Mvc;

namespace BoredGamers.Controllers
{
    // Serves the Games HTML pages (Razor views). this is separate from GamesController
    // because that controller uses [ApiController] and returns JSON, and mixing API and
    // view responses in one controller would require removing [ApiController], which
    // change how model binding/error handling work for the existing API endpoints.
    public class GamesPageController : Controller
    {
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
    }
}