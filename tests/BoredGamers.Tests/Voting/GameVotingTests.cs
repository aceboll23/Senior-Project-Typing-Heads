using BoredGamers.Controllers;
using BoredGamers.Models;
using BoredGamers.Services.GameNightEvents;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace BoredGamers.Tests.Voting;

[TestFixture]
public class GameVotingTests
{
    private Mock<IGameNightEventService> _serviceMock;
    private GameNightEventController _controller;
    private const string UserId = "test-user-id";

    [SetUp]
    public void SetUp()
    {
        _serviceMock = new Mock<IGameNightEventService>();

        _controller = new GameNightEventController(_serviceMock.Object);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, UserId)
                }))
            }
        };
    }

    [TearDown]
    public void TearDown()
    {
        _controller.Dispose();
    }

    // --- OpenVoting ---

    [Test]
    public async Task OpenVoting_Success_RedirectsWithVotingOpenedStatus()
    {
        _serviceMock.Setup(s => s.OpenVotingAsync(1, UserId)).ReturnsAsync(true);

        var result = await _controller.OpenVoting(1) as RedirectToActionResult;

        Assert.That(result!.ActionName, Is.EqualTo("Details"));
        Assert.That(result.RouteValues!["status"], Is.EqualTo("voting-opened"));
    }

    [Test]
    public async Task OpenVoting_Failure_RedirectsWithErrorStatus()
    {
        _serviceMock.Setup(s => s.OpenVotingAsync(1, UserId)).ReturnsAsync(false);

        var result = await _controller.OpenVoting(1) as RedirectToActionResult;

        Assert.That(result!.RouteValues!["status"], Is.EqualTo("voting-open-error"));
    }

    [Test]
    public async Task OpenVoting_PassesCorrectEventIdToService()
    {
        _serviceMock.Setup(s => s.OpenVotingAsync(It.IsAny<int>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        await _controller.OpenVoting(42);

        _serviceMock.Verify(s => s.OpenVotingAsync(42, UserId), Times.Once);
    }

    // --- CloseVoting ---

    [Test]
    public async Task CloseVoting_Success_RedirectsWithVotingClosedStatus()
    {
        _serviceMock.Setup(s => s.CloseVotingAsync(1, UserId)).ReturnsAsync(true);

        var result = await _controller.CloseVoting(1) as RedirectToActionResult;

        Assert.That(result!.ActionName, Is.EqualTo("Details"));
        Assert.That(result.RouteValues!["status"], Is.EqualTo("voting-closed"));
    }

    [Test]
    public async Task CloseVoting_Failure_RedirectsWithErrorStatus()
    {
        _serviceMock.Setup(s => s.CloseVotingAsync(1, UserId)).ReturnsAsync(false);

        var result = await _controller.CloseVoting(1) as RedirectToActionResult;

        Assert.That(result!.RouteValues!["status"], Is.EqualTo("voting-close-error"));
    }

    [Test]
    public async Task CloseVoting_PassesCorrectEventIdToService()
    {
        _serviceMock.Setup(s => s.CloseVotingAsync(It.IsAny<int>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        await _controller.CloseVoting(42);

        _serviceMock.Verify(s => s.CloseVotingAsync(42, UserId), Times.Once);
    }

    // --- SubmitRankings ---

    [Test]
    public async Task SubmitRankings_Success_RedirectsWithRankingsSubmittedStatus()
    {
        var ranks = new Dictionary<int, int> { { 1, 1 }, { 2, 2 } };
        _serviceMock.Setup(s => s.SubmitRankingsAsync(1, UserId, ranks)).ReturnsAsync(true);

        var result = await _controller.SubmitRankings(1, ranks) as RedirectToActionResult;

        Assert.That(result!.ActionName, Is.EqualTo("Details"));
        Assert.That(result.RouteValues!["status"], Is.EqualTo("rankings-submitted"));
    }

    [Test]
    public async Task SubmitRankings_Failure_RedirectsWithErrorStatus()
    {
        var ranks = new Dictionary<int, int> { { 1, 1 } };
        _serviceMock.Setup(s => s.SubmitRankingsAsync(It.IsAny<int>(), It.IsAny<string>(),
            It.IsAny<Dictionary<int, int>>())).ReturnsAsync(false);

        var result = await _controller.SubmitRankings(1, ranks) as RedirectToActionResult;

        Assert.That(result!.RouteValues!["status"], Is.EqualTo("rankings-error"));
    }

    [Test]
    public async Task SubmitRankings_PassesCorrectDataToService()
    {
        var ranks = new Dictionary<int, int> { { 1, 1 }, { 2, 2 }, { 3, 3 } };
        _serviceMock.Setup(s => s.SubmitRankingsAsync(It.IsAny<int>(), It.IsAny<string>(),
            It.IsAny<Dictionary<int, int>>())).ReturnsAsync(true);

        await _controller.SubmitRankings(5, ranks);

        _serviceMock.Verify(s => s.SubmitRankingsAsync(5, UserId, ranks), Times.Once);
    }

    [Test]
    public async Task SubmitRankings_EmptyRanks_StillCallsService()
    {
        var ranks = new Dictionary<int, int>();
        _serviceMock.Setup(s => s.SubmitRankingsAsync(It.IsAny<int>(), It.IsAny<string>(),
            It.IsAny<Dictionary<int, int>>())).ReturnsAsync(false);

        var result = await _controller.SubmitRankings(1, ranks) as RedirectToActionResult;

        _serviceMock.Verify(s => s.SubmitRankingsAsync(1, UserId, ranks), Times.Once);
        Assert.That(result!.RouteValues!["status"], Is.EqualTo("rankings-error"));
    }

    // --- Details includes voting data ---

    [Test]
    public async Task Details_VotingNotStarted_SetsVotingStatusInViewData()
    {
        var gameNightEvent = new Models.GameNightEvent
        {
            Id = 1,
            PlaygroupId = 1,
            CreatedByUserId = UserId,
            Title = "Test Event",
            EventDateTime = DateTime.UtcNow.AddDays(7),
            VotingStatus = VotingStatus.NotStarted
        };

        _serviceMock.Setup(s => s.UserCanAccessEventAsync(1, UserId)).ReturnsAsync(true);
        _serviceMock.Setup(s => s.GetEventByIdAsync(1)).ReturnsAsync(gameNightEvent);
        _serviceMock.Setup(s => s.GetEventResponsesAsync(1)).ReturnsAsync(new List<EventResponse>());
        _serviceMock.Setup(s => s.GetUserResponseAsync(1, UserId)).ReturnsAsync((EventResponse?)null);
        _serviceMock.Setup(s => s.GetUserRankingsAsync(1, UserId))
            .ReturnsAsync(new Dictionary<int, int>());
        _serviceMock.Setup(s => s.GetResponderNamesAsync(It.IsAny<List<EventResponse>>()))
            .ReturnsAsync(new Dictionary<string, string>());

        var result = await _controller.Details(1, null) as ViewResult;

        Assert.That(result!.ViewData["VotingStatus"], Is.EqualTo(VotingStatus.NotStarted));
    }

    [Test]
    public async Task Details_VotingClosed_LoadsVotingResults()
    {
        var gameNightEvent = new Models.GameNightEvent
        {
            Id = 1,
            PlaygroupId = 1,
            CreatedByUserId = UserId,
            Title = "Test Event",
            EventDateTime = DateTime.UtcNow.AddDays(7),
            VotingStatus = VotingStatus.Closed
        };

        var results = new List<(GameNightEventGame, int, bool)>();

        _serviceMock.Setup(s => s.UserCanAccessEventAsync(1, UserId)).ReturnsAsync(true);
        _serviceMock.Setup(s => s.GetEventByIdAsync(1)).ReturnsAsync(gameNightEvent);
        _serviceMock.Setup(s => s.GetEventResponsesAsync(1)).ReturnsAsync(new List<EventResponse>());
        _serviceMock.Setup(s => s.GetUserResponseAsync(1, UserId)).ReturnsAsync((EventResponse?)null);
        _serviceMock.Setup(s => s.GetUserRankingsAsync(1, UserId))
            .ReturnsAsync(new Dictionary<int, int>());
        _serviceMock.Setup(s => s.GetVotingResultsAsync(1)).ReturnsAsync(results);
        _serviceMock.Setup(s => s.GetResponderNamesAsync(It.IsAny<List<EventResponse>>()))
            .ReturnsAsync(new Dictionary<string, string>());

        var result = await _controller.Details(1, null) as ViewResult;

        _serviceMock.Verify(s => s.GetVotingResultsAsync(1), Times.Once);
        Assert.That(result!.ViewData["VotingResults"], Is.Not.Null);
    }

    [Test]
    public async Task Details_VotingOpen_DoesNotLoadVotingResults()
    {
        var gameNightEvent = new Models.GameNightEvent
        {
            Id = 1,
            PlaygroupId = 1,
            CreatedByUserId = UserId,
            Title = "Test Event",
            EventDateTime = DateTime.UtcNow.AddDays(7),
            VotingStatus = VotingStatus.Open
        };

        _serviceMock.Setup(s => s.UserCanAccessEventAsync(1, UserId)).ReturnsAsync(true);
        _serviceMock.Setup(s => s.GetEventByIdAsync(1)).ReturnsAsync(gameNightEvent);
        _serviceMock.Setup(s => s.GetEventResponsesAsync(1)).ReturnsAsync(new List<EventResponse>());
        _serviceMock.Setup(s => s.GetUserResponseAsync(1, UserId)).ReturnsAsync((EventResponse?)null);
        _serviceMock.Setup(s => s.GetUserRankingsAsync(1, UserId))
            .ReturnsAsync(new Dictionary<int, int>());
        _serviceMock.Setup(s => s.GetResponderNamesAsync(It.IsAny<List<EventResponse>>()))
            .ReturnsAsync(new Dictionary<string, string>());

        await _controller.Details(1, null);

        _serviceMock.Verify(s => s.GetVotingResultsAsync(It.IsAny<int>()), Times.Never);
    }

    [Test]
    public async Task Details_SetsUserRankingsInViewData()
    {
        var rankings = new Dictionary<int, int> { { 1, 1 }, { 2, 2 } };
        var gameNightEvent = new Models.GameNightEvent
        {
            Id = 1,
            PlaygroupId = 1,
            CreatedByUserId = UserId,
            Title = "Test Event",
            EventDateTime = DateTime.UtcNow.AddDays(7),
            VotingStatus = VotingStatus.NotStarted
        };

        _serviceMock.Setup(s => s.UserCanAccessEventAsync(1, UserId)).ReturnsAsync(true);
        _serviceMock.Setup(s => s.GetEventByIdAsync(1)).ReturnsAsync(gameNightEvent);
        _serviceMock.Setup(s => s.GetEventResponsesAsync(1)).ReturnsAsync(new List<EventResponse>());
        _serviceMock.Setup(s => s.GetUserResponseAsync(1, UserId)).ReturnsAsync((EventResponse?)null);
        _serviceMock.Setup(s => s.GetUserRankingsAsync(1, UserId)).ReturnsAsync(rankings);
        _serviceMock.Setup(s => s.GetResponderNamesAsync(It.IsAny<List<EventResponse>>()))
            .ReturnsAsync(new Dictionary<string, string>());

        var result = await _controller.Details(1, null) as ViewResult;

        Assert.That(result!.ViewData["UserRankings"], Is.EqualTo(rankings));
    }
}