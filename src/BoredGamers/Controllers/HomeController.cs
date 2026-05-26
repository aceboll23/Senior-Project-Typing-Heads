using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using BoredGamers.Models;
using BoredGamers.Models.ViewModels;
using BoredGamers.Services.Games;

namespace BoredGamers.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IGameService _games;

    public HomeController(ILogger<HomeController> logger, IGameService games)
    {
        _logger = logger;
        _games = games;
    }

    public async Task<IActionResult> Index()
    {
        //Featured list for the landing page (DB-only; no live BGG calls)
        var featured = await _games.GetTopGamesAsync(limit: 4);

        _logger.LogInformation("Featured games = {Games}",
            string.Join(",", featured.Select(g => $"{g.Name} (Avg: {g.AverageRating}, Voters: {g.BggNumVoters})")));


        _logger.LogInformation("Home FeaturedGames count = {Count}", featured.Count);

        var vm = new HomeIndexViewModel
        {
            FeaturedGames = featured
        };

        return View(vm);
    }

    public IActionResult Privacy()
    {
        return View();
    }

    public IActionResult About() => View();

    public IActionResult Contact() => View();

    public IActionResult Terms() => View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
