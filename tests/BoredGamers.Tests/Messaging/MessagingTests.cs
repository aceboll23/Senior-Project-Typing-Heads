using BoredGamers.Controllers;
using BoredGamers.Data;
using BoredGamers.Models;
using BoredGamers.Hubs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace BoredGamers.Tests;

[TestFixture]
public class MessagesControllerTests
{
    private ApplicationDbContext _db;
    private Mock<UserManager<User>> _userManagerMock;
    private Mock<IHubContext<DirectMessageHub>> _hubContextMock;
    private Mock<IHubClients> _hubClientsMock;
    private Mock<IClientProxy> _clientProxyMock;

    private MessagesController? _controller;

    private User _sender;
    private User _recipient;
    private User _nonFriend;
    private User _bannedUser;
    private User _deactivatedUser;
    private UserProfile _senderProfile;
    private UserProfile _recipientProfile;
    private UserProfile _nonFriendProfile;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db = new ApplicationDbContext(options);

        var store = new Mock<IUserStore<User>>();
        _userManagerMock = new Mock<UserManager<User>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        // --- SignalR mocks ---
        _hubContextMock = new Mock<IHubContext<DirectMessageHub>>();
        _hubClientsMock = new Mock<IHubClients>();
        _clientProxyMock = new Mock<IClientProxy>();

        _hubClientsMock
            .Setup(c => c.Group(It.IsAny<string>()))
            .Returns(_clientProxyMock.Object);

        _hubContextMock
            .Setup(h => h.Clients)
            .Returns(_hubClientsMock.Object);

        SeedData();

        _userManagerMock.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(_sender);

        _userManagerMock.Setup(m => m.Users)
            .Returns(_db.Set<User>());

        _controller = new MessagesController(
            _db,
            _userManagerMock.Object,
            _hubContextMock.Object
        );

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, _sender.Id)
                }))
            }
        };
    }

    [TearDown]
    public void TearDown()
    {
        _controller?.Dispose();
        _db.Dispose();
    }

    private void SeedData()
    {
        _senderProfile = new UserProfile
        {
            UserId = "sender-id",
            IsProfilePublic = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _recipientProfile = new UserProfile
        {
            UserId = "recipient-id",
            IsProfilePublic = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _nonFriendProfile = new UserProfile
        {
            UserId = "nonfriend-id",
            IsProfilePublic = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _sender = new User
        {
            Id = "sender-id",
            UserName = "sender",
            Profile = _senderProfile
        };

        _recipient = new User
        {
            Id = "recipient-id",
            UserName = "recipient",
            Profile = _recipientProfile
        };

        _nonFriend = new User
        {
            Id = "nonfriend-id",
            UserName = "nonfriend",
            Profile = _nonFriendProfile
        };

        _bannedUser = new User
        {
            Id = "banned-id",
            UserName = "banneduser",
            IsBanned = true,
            Profile = new UserProfile { UserId = "banned-id" }
        };

        _deactivatedUser = new User
        {
            Id = "deactivated-id",
            UserName = "deactivateduser",
            IsDeactivated = true,
            Profile = new UserProfile { UserId = "deactivated-id" }
        };

        _db.Users.AddRange(_sender, _recipient, _nonFriend, _bannedUser, _deactivatedUser);

        _db.Set<Friendship>().Add(new Friendship
        {
            RequesterProfileId = _senderProfile.Id,
            ReceiverProfileId = _recipientProfile.Id,
            Status = FriendshipStatus.Accepted,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        _db.SaveChanges();
    }

    private object? GetValue(object obj, string property) =>
        obj.GetType().GetProperty(property)?.GetValue(obj);

    // --- Send ---

    [Test]
    public async Task Send_ValidMessage_SavesMessageToDb()
    {
        await _controller!.Send("recipient", "Hello!");

        var message = await _db.DirectMessages.FirstOrDefaultAsync();
        Assert.That(message, Is.Not.Null);
        Assert.That(message!.Content, Is.EqualTo("Hello!"));
    }

    [Test]
    public async Task Send_ValidMessage_ReturnsSuccess()
    {
        var result = await _controller!.Send("recipient", "Hello!") as JsonResult;
        Assert.That(GetValue(result!.Value!, "success"), Is.EqualTo(true));
    }

    [Test]
    public async Task Send_ToSelf_ReturnsBadRequest()
    {
        var result = await _controller!.Send("sender", "Hello!");
        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task Send_EmptyContent_ReturnsFailure()
    {
        var result = await _controller!.Send("recipient", "") as JsonResult;
        Assert.That(GetValue(result!.Value!, "success"), Is.EqualTo(false));
    }

    [Test]
    public async Task Send_ToBannedUser_ReturnsNotFound()
    {
        var result = await _controller!.Send("banneduser", "Hello!");
        Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
    }

    [Test]
    public async Task Send_ToDeactivatedUser_ReturnsNotFound()
    {
        var result = await _controller!.Send("deactivateduser", "Hello!");
        Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
    }

    // --- Index ---

    [Test]
    public async Task Index_ReturnsView()
    {
        var result = await _controller!.Index();
        Assert.That(result, Is.InstanceOf<ViewResult>());
    }

    [Test]
    public async Task Index_WithNoMessages_ReturnEmpty()
    {
        var result = await _controller!.Index() as ViewResult;

        var friends = result!.ViewData["FriendConversations"] as List<DirectMessage>;
        var requests = result.ViewData["MessageRequests"] as List<DirectMessage>;

        Assert.That(friends, Is.Empty);
        Assert.That(requests, Is.Empty);
    }
}