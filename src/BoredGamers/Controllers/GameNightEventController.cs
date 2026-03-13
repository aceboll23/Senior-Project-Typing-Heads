using System.Security.Claims;
using System.Threading.Tasks;
using BoredGamers.Models.ViewModels;
using BoredGamers.Services.GameNightEvents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BoredGamers.Controllers;

[Authorize]
public class GameNightEventController : Controller
{
  private readonly IGameNightEventService _gameNightEventService;

  public GameNightEventController(IGameNightEventService gameNightEventService)
  {
    _gameNightEventService = gameNightEventService;
  }

  private string GetUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

  // GET /GameNightEvent/Create?playgroupId=5
  public async Task<IActionResult> Create(int playgroupId)
  {
    var userId = GetUserId();

    var isMember = await _gameNightEventService.UserIsPlaygroupMemberAsync(playgroupId, userId);
    if (!isMember)
    {
      return NotFound();
    }

    var model = new CreateGameNightEventViewModel
    {
      PlaygroupId = playgroupId
    };

    return View(model);
  }

  // POST /GameNightEvent/Create
  [HttpPost]
  [ValidateAntiForgeryToken]
  public async Task<IActionResult> Create(CreateGameNightEventViewModel model)
  {
    var userId = GetUserId();

    var isMember = await _gameNightEventService.UserIsPlaygroupMemberAsync(model.PlaygroupId, userId);
    if (!isMember)
    {
      return NotFound();
    }

    if (model.EventDateTime < DateTime.Now)
    {
      ModelState.AddModelError(nameof(model.EventDateTime), "Event date and time cannot be in the past.");
    }

    if (!ModelState.IsValid)
    {
      return View(model);
    }

    var createdEvent = await _gameNightEventService.CreateEventAsync(
        model.PlaygroupId,
        userId,
        model.Title,
        model.EventDateTime,
        model.Description);

    return RedirectToAction("Details", new { id = createdEvent.Id });
  }

  // GET /GameNightEvent/Details/5
  public async Task<IActionResult> Details(int id, string? status)
  {
    var userId = GetUserId();

    var canAccess = await _gameNightEventService.UserCanAccessEventAsync(id, userId);
    if (!canAccess)
    {
      return NotFound();
    }

    var gameNightEvent = await _gameNightEventService.GetEventByIdAsync(id);
    if (gameNightEvent == null)
    {
      return NotFound();
    }

    ViewData["Status"] = status;
    
    return View(gameNightEvent);
  }

  //GET /GaameNightEvent/AddGame/5
  public async Task<IActionResult> AddGame(int id)
  {
    var userId = GetUserId();

    var canAccess = await _gameNightEventService.UserCanAccessEventAsync(id, userId);
    if (!canAccess)
    {
      return NotFound();
    }

    var gameNightEvent = await _gameNightEventService.GetEventByIdAsync(id);
    if (gameNightEvent == null)
    {
      return NotFound();
    }

    var games = await _gameNightEventService.GetUserCollectionForEventAsync(id, userId);
    var collectionCount = await _gameNightEventService.GetUserCollectionCountAsync(userId);

    ViewData["EventId"] = id;
    ViewData["EventTitle"] = gameNightEvent.Title;
    ViewData["CollectionCount"] = collectionCount;

    return View(games);
  }

  //POST /GameNightEvent/AddGame
  [HttpPost]
  [ValidateAntiForgeryToken]
  [ActionName("AddGame")]
  public async Task<IActionResult> AddGamePost(int eventId, int gameId)
  {
    var userId = GetUserId();

    var canAccess = await _gameNightEventService.UserCanAccessEventAsync(eventId, userId);
    if (!canAccess)
    {
      return NotFound();
    }

    var added = await _gameNightEventService.AddGameToEventAsync(eventId, gameId, userId);

    if(added)
    {
      return RedirectToAction("Details", new
      {
        id = eventId,
        status = "added"
      });
    }
    return RedirectToAction("Details", new
    {
      id = eventId,
      status = "error"
    });

  }
}