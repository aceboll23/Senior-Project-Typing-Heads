using System.Security.Claims;
using System.Threading.Tasks;
using BoredGamers.Models;
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

    var hasEventSameDay = await _gameNightEventService.PlaygroupHasEventOnDateAsync(
        model.PlaygroupId,
        model.EventDateTime);

    if (hasEventSameDay && !model.ConfirmDuplicateDate)
    {
      model.WarningMessage = "Your playgroup already has an event scheduled on that day. Do you want to continue anyway?";
      return View(model);
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

    // RSVP data
    var responses = await _gameNightEventService.GetEventResponsesAsync(id);
    var userResponse = await _gameNightEventService.GetUserResponseAsync(id, userId);
    ViewData["UserResponse"] = userResponse?.Status;
    ViewData["GoingCount"] = responses.Count(r => r.Status == ResponseStatus.Going);
    ViewData["MaybeCount"] = responses.Count(r => r.Status == ResponseStatus.Maybe);
    ViewData["NotGoingCount"] = responses.Count(r => r.Status == ResponseStatus.NotGoing);
    ViewData["Responses"] = responses;
    // Voting data
    var userRankings = await _gameNightEventService.GetUserRankingsAsync(id, userId);
    ViewData["VotingStatus"] = gameNightEvent.VotingStatus;
    ViewData["UserRankings"] = userRankings;

    if (gameNightEvent.VotingStatus == VotingStatus.Closed)
    {
      var results = await _gameNightEventService.GetVotingResultsAsync(id);
      ViewData["VotingResults"] = results;
    }
     
    ViewData["CurrentUserId"] = userId;
    ViewData["ResponderNames"] = await _gameNightEventService.GetResponderNamesAsync(responses);

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

  //POST /GameNightEvent/Remove
  [HttpPost]
  [ValidateAntiForgeryToken]
  public async Task<IActionResult> RemoveGame(int eventGameId, int eventId)
  {
    var userId = GetUserId();

    var canAccess = await _gameNightEventService.UserCanAccessEventAsync(eventId, userId);
    if (!canAccess)
    {
      return NotFound();
    }

    var removed = await _gameNightEventService.RemoveGameFromEventAsync(eventGameId, userId);

    if (removed)
    {
      return RedirectToAction("Details", new
      {
        id = eventId,
        status = "removed"
      });
    }

    return RedirectToAction("Details", new
    {
      id = eventId,
      status = "remove-error"
    });
  }

  //GET /GameNightEvent/Edit/5
  public async Task<IActionResult> Edit(int id)
  {
    var userId = GetUserId();

    var canEdit = await _gameNightEventService.UserCanEditEventAsync(id, userId);
    if (!canEdit)
    {
      return NotFound();
    }

    var gameNightEvent = await _gameNightEventService.GetEventByIdAsync(id);
    if (gameNightEvent == null)
    {
      return NotFound();
    }

    var model = new CreateGameNightEventViewModel
    {
      PlaygroupId = gameNightEvent.PlaygroupId,
      Title = gameNightEvent.Title,
      EventDateTime = gameNightEvent.EventDateTime,
      Description = gameNightEvent.Description
    };

    ViewData["EventId"] = id;
    ViewData["IsEdit"] = true;

    return View("Create", model);
  }

  //POST /GameNightEvent/Edit/5
  [HttpPost]
  [ValidateAntiForgeryToken]
  public async Task<IActionResult> Edit(int id, CreateGameNightEventViewModel model)
  {
    var userId = GetUserId();

    var canEdit = await _gameNightEventService.UserCanEditEventAsync(id, userId);
    if (!canEdit)
    {
      return NotFound();
    }

    if (model.EventDateTime < DateTime.Now)
    {
      ModelState.AddModelError(nameof(model.EventDateTime), "Event date and time cannot be in the past.");
    }

    var hasEventSameDay = await _gameNightEventService.PlaygroupHasEventOnDateAsync(
      model.PlaygroupId,
      model.EventDateTime);
    
    if (hasEventSameDay && !model.ConfirmDuplicateDate)
    {
      var existingEvent = await _gameNightEventService.GetEventByIdAsync(id);

      if (existingEvent == null || existingEvent.EventDateTime.Date != model.EventDateTime.Date)
      {
        model.WarningMessage = "Your playgroup already has an event scheduled on that day. Do you want to continue anyway?";
        ViewData["EventId"] = id;
        ViewData["IsEdit"] = true;
        return View("Create", model);
      }
    }

    if (!ModelState.IsValid)
    {
      ViewData["EventId"] = id;
      ViewData["IsEdit"] = true;
      return View("Create", model);
    }

    var updated = await _gameNightEventService.UpdateEventAsync(
      id,
      userId,
      model.Title,
      model.EventDateTime,
      model.Description);
    
    if (!updated)
    {
      return NotFound();
    }

    return RedirectToAction("Details", new { id, status = "updated" });
  }

  //POST /GameNightEvent/CancelEvent
  [HttpPost]
  [ValidateAntiForgeryToken]
  public async Task<IActionResult> CancelEvent(int eventId)
  {
    var userId = GetUserId();

    var canEdit = await _gameNightEventService.UserCanEditEventAsync(eventId, userId);
    if(!canEdit)
    {
      return NotFound();
    }

    var gameNightEvent = await _gameNightEventService.GetEventByIdAsync(eventId);
    if (gameNightEvent == null)
    {
      return NotFound();
    }

    var playgroupId = gameNightEvent.PlaygroupId;

    var cancelled = await _gameNightEventService.CancelEventAsync(eventId, userId);
    if (!cancelled)
    {
      return NotFound();
    }

    return RedirectToAction("Details", "Playgroup", new { id = playgroupId, status = "event-cancelled" });
  }
  // POST /GameNightEvent/Respond
  [HttpPost]
  [ValidateAntiForgeryToken]
  public async Task<IActionResult> Respond(int eventId, ResponseStatus status)
  {
    var userId = GetUserId();

    var responded = await _gameNightEventService.RespondToEventAsync(eventId, userId, status);
    if (!responded)
    {
      return NotFound();
    }

    return RedirectToAction("Details", new { id = eventId, status = "responded" });
  }

  // POST /GameNightEvent/OpenVoting
  [HttpPost]
  [ValidateAntiForgeryToken]
  public async Task<IActionResult> OpenVoting(int eventId)
  {
      var userId = GetUserId();
      var opened = await _gameNightEventService.OpenVotingAsync(eventId, userId);
      return RedirectToAction("Details", new
      {
          id = eventId,
          status = opened ? "voting-opened" : "voting-open-error"
      });
  }

  // POST /GameNightEvent/CloseVoting
  [HttpPost]
  [ValidateAntiForgeryToken]
  public async Task<IActionResult> CloseVoting(int eventId)
  {
      var userId = GetUserId();
      var closed = await _gameNightEventService.CloseVotingAsync(eventId, userId);
      return RedirectToAction("Details", new
      {
          id = eventId,
          status = closed ? "voting-closed" : "voting-close-error"
      });
  }

  // POST /GameNightEvent/SubmitRankings
  [HttpPost]
  [ValidateAntiForgeryToken]
  public async Task<IActionResult> SubmitRankings(int eventId, [FromForm] Dictionary<int, int> gameRanks)
  {
      var userId = GetUserId();
      var submitted = await _gameNightEventService.SubmitRankingsAsync(eventId, userId, gameRanks);
      return RedirectToAction("Details", new
      {
          id = eventId,
          status = submitted ? "rankings-submitted" : "rankings-error"
      });
  }
}