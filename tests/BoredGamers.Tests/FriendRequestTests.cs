using BoredGamers.Controllers;
using BoredGamers.Data;
using BoredGamers.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace BoredGamers.Tests;

[TestFixture]
public class FriendRequestTests
{
    private ApplicationDbContext _db;
    private Mock<UserManager<User>> _userManagerMock;
    private FriendRequestController? _controller;


    // Test users
    private User _sender;
    private User _recipient;
    private User _bannedUser;
    private User _deactivatedUser;
    private UserProfile _senderProfile;
    private UserProfile _recipientProfile;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: System.Guid.NewGuid().ToString()).Options;
        _db = new ApplicationDbContext(options);

        var store = new Mock<IUserStore<User>>();
        _userManagerMock = new Mock<UserManager<User>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        
        SeedUsers();

        // Mock GetUserAsync to return the sender by default
        _userManagerMock.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(_sender);

        _userManagerMock.Setup(m => m.Users)
            .Returns(_db.Set<User>());

        _controller = new FriendRequestController(_db, _userManagerMock.Object);

        // Give the controller a fake HttpContext so User claims work
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

    private object? GetValue(object obj, string property) =>
    obj.GetType().GetProperty(property)?.GetValue(obj);

    private void SeedUsers()
    {
        _senderProfile = new UserProfile { IsProfilePublic = true };
        _recipientProfile = new UserProfile { IsProfilePublic = true };

        _sender = new User
        {
            Id = "sender-id",
            UserName = "sender",
            Email = "sender@test.com",
            IsBanned = false,
            IsDeactivated = false,
            Profile = _senderProfile
        };

        _recipient = new User
        {
            Id = "recipient-id",
            UserName = "recipient",
            Email = "recipient@test.com",
            IsBanned = false,
            IsDeactivated = false,
            Profile = _recipientProfile
        };

        _bannedUser = new User
        {
            Id = "banned-id",
            UserName = "banneduser",
            Email = "banned@test.com",
            IsBanned = true,
            IsDeactivated = false,
            Profile = new UserProfile { IsProfilePublic = true }
        };

        _deactivatedUser = new User
        {
            Id = "deactivated-id",
            UserName = "deactivateduser",
            Email = "deactivated@test.com",
            IsBanned = false,
            IsDeactivated = true,
            Profile = new UserProfile { IsProfilePublic = true }
        };

        _db.Users.AddRange(_sender, _recipient, _bannedUser, _deactivatedUser);
        _db.SaveChanges();
    }

    [Test]
    public async Task Send_ValidRequest_ReturnsSuccess()
    {
        var result = await _controller!.Send("recipient") as JsonResult;
        var success = GetValue(result!.Value!, "success");
        Assert.That(success, Is.EqualTo(true));
    }

    [Test]
    public async Task Send_ToSelf_IsRejected()
    {
        var result = await _controller!.Send("sender");

        // Sending to yourself should be rejected
        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>().Or.InstanceOf<NotFoundObjectResult>());
    }

    [Test]
    public async Task Send_ToBannedUser_ReturnsNotFound()
    {
        var result = await _controller!.Send("banneduser");
        Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
    }

    [Test]
    public async Task Send_ToDeactivatedUser_ReturnsNotFound()
    {
        var result = await _controller!.Send("deactivateduser");
        Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
    }

    [Test]
    public async Task Send_ToNonExistentUser_ReturnsNotFound()
    {
        var result = await _controller!.Send("nobody");
        Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
    }

    [Test]
    public async Task Send_WhenAlreadyPending_ReturnsFailure()
    {
        // Seed an existing pending request
        _db.Set<Friendship>().Add(new Friendship
        {
            RequesterProfileId = _senderProfile.Id,
            ReceiverProfileId = _recipientProfile.Id,
            Status = FriendshipStatus.Pending,
            RequestedAt = System.DateTime.UtcNow,
            CreatedAt = System.DateTime.UtcNow,
            UpdatedAt = System.DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        var result = await _controller!.Send("recipient") as JsonResult;
        var success = GetValue(result!.Value!, "success");
        Assert.That(success, Is.EqualTo(false));
    }

    [Test]
    public async Task Send_WhenAlreadyFriends_ReturnsFailure()
    {
        _db.Set<Friendship>().Add(new Friendship
        {
            RequesterProfileId = _senderProfile.Id,
            ReceiverProfileId = _recipientProfile.Id,
            Status = FriendshipStatus.Accepted,
            RequestedAt = System.DateTime.UtcNow,
            CreatedAt = System.DateTime.UtcNow,
            UpdatedAt = System.DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        var result = await _controller!.Send("recipient") as JsonResult;
        var success = GetValue(result!.Value!, "success");
        Assert.That(success, Is.EqualTo(false));
    }

    [Test]
    public async Task Send_DoesNotCreateDuplicateFriendship()
    {
        await _controller!.Send("recipient");
        await _controller!.Send("recipient"); // second send should not create another record

        var count = await _db.Set<Friendship>().CountAsync(f =>
            f.RequesterProfileId == _senderProfile.Id &&
            f.ReceiverProfileId == _recipientProfile.Id);

        Assert.That(count, Is.EqualTo(1));
    }

    [Test]
    public async Task Send_FriendshipRecord_HasCorrectTimestamp()
    {
        var before = System.DateTime.UtcNow;
        await _controller!.Send("recipient");
        var after = System.DateTime.UtcNow;

        var friendship = await _db.Set<Friendship>()
            .FirstOrDefaultAsync(f =>
                f.RequesterProfileId == _senderProfile.Id &&
                f.ReceiverProfileId == _recipientProfile.Id);

        Assert.That(friendship!.RequestedAt, Is.InRange(before, after));
    }

    // --- Cancel ---

    [Test]
    public async Task Cancel_PendingRequest_UpdatesStatusToCancelled()
    {
        _db.Set<Friendship>().Add(new Friendship
        {
            RequesterProfileId = _senderProfile.Id,
            ReceiverProfileId = _recipientProfile.Id,
            Status = FriendshipStatus.Pending,
            RequestedAt = System.DateTime.UtcNow,
            CreatedAt = System.DateTime.UtcNow,
            UpdatedAt = System.DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        await _controller!.Cancel("recipient");

        var friendship = await _db.Set<Friendship>()
            .FirstOrDefaultAsync(f =>
                f.RequesterProfileId == _senderProfile.Id &&
                f.ReceiverProfileId == _recipientProfile.Id);

        Assert.That(friendship!.Status, Is.EqualTo(FriendshipStatus.Cancelled));
    }

    [Test]
    public async Task Cancel_PendingRequest_ReturnsSuccess()
    {
        _db.Set<Friendship>().Add(new Friendship
        {
            RequesterProfileId = _senderProfile.Id,
            ReceiverProfileId = _recipientProfile.Id,
            Status = FriendshipStatus.Pending,
            RequestedAt = System.DateTime.UtcNow,
            CreatedAt = System.DateTime.UtcNow,
            UpdatedAt = System.DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        var result = await _controller!.Cancel("recipient") as JsonResult;
        var success = GetValue(result!.Value!, "success");
        Assert.That(success, Is.EqualTo(true));
    }

    [Test]
    public async Task Cancel_WhenNoPendingRequest_ReturnsFailure()
    {
        var result = await _controller!.Cancel("recipient") as JsonResult;
        var success = GetValue(result!.Value!, "success");
        Assert.That(success, Is.EqualTo(false));
    }

    [Test]
    public async Task Cancel_AcceptedFriendship_ReturnsFailure()
    {
        // Cannot cancel an already accepted friendship
        _db.Set<Friendship>().Add(new Friendship
        {
            RequesterProfileId = _senderProfile.Id,
            ReceiverProfileId = _recipientProfile.Id,
            Status = FriendshipStatus.Accepted,
            RequestedAt = System.DateTime.UtcNow,
            CreatedAt = System.DateTime.UtcNow,
            UpdatedAt = System.DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        var result = await _controller!.Cancel("recipient") as JsonResult;
        var success = GetValue(result!.Value!, "success");
        Assert.That(success, Is.EqualTo(false));
    }

    [Test]
    public async Task Cancel_DoesNotDeleteRecord_OnlyUpdatesStatus()
    {
        _db.Set<Friendship>().Add(new Friendship
        {
            RequesterProfileId = _senderProfile.Id,
            ReceiverProfileId = _recipientProfile.Id,
            Status = FriendshipStatus.Pending,
            RequestedAt = System.DateTime.UtcNow,
            CreatedAt = System.DateTime.UtcNow,
            UpdatedAt = System.DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        await _controller!.Cancel("recipient");

        // record should still exist but with Cancelled status
        var friendship = await _db.Set<Friendship>()
            .FirstOrDefaultAsync(f =>
                f.RequesterProfileId == _senderProfile.Id &&
                f.ReceiverProfileId == _recipientProfile.Id);

        Assert.That(friendship, Is.Not.Null);
        Assert.That(friendship!.Status, Is.EqualTo(FriendshipStatus.Cancelled));
    }
}