using BoredGamers.Controllers;
using BoredGamers.Data;
using BoredGamers.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
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
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _db = new ApplicationDbContext(options);

        var store = new Mock<IUserStore<User>>();
        _userManagerMock = new Mock<UserManager<User>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        SeedData();

        _userManagerMock.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(_sender);

        _userManagerMock.Setup(m => m.Users)
            .Returns(_db.Set<User>());

        _controller = new MessagesController(_db, _userManagerMock.Object);
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
            IsProfilePublic = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _recipientProfile = new UserProfile
        {
            IsProfilePublic = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _nonFriendProfile = new UserProfile
        {
            IsProfilePublic = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _sender = new User
        {
            Id = "sender-id",
            UserName = "sender",
            Email = "sender@test.com",
            IsBanned = false,
            IsDeactivated = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Profile = _senderProfile
        };
        _recipient = new User
        {
            Id = "recipient-id",
            UserName = "recipient",
            Email = "recipient@test.com",
            IsBanned = false,
            IsDeactivated = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Profile = _recipientProfile
        };
        _nonFriend = new User
        {
            Id = "nonfriend-id",
            UserName = "nonfriend",
            Email = "nonfriend@test.com",
            IsBanned = false,
            IsDeactivated = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Profile = _nonFriendProfile
        };
        _bannedUser = new User
        {
            Id = "banned-id",
            UserName = "banneduser",
            Email = "banned@test.com",
            IsBanned = true,
            IsDeactivated = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Profile = new UserProfile { CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
        };
        _deactivatedUser = new User
        {
            Id = "deactivated-id",
            UserName = "deactivateduser",
            Email = "deactivated@test.com",
            IsBanned = false,
            IsDeactivated = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Profile = new UserProfile { CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
        };

        _db.Users.AddRange(_sender, _recipient, _nonFriend, _bannedUser, _deactivatedUser);

        // Sender and recipient are friends
        _db.Set<Friendship>().Add(new Friendship
        {
            RequesterProfileId = _senderProfile.Id,
            ReceiverProfileId = _recipientProfile.Id,
            Status = FriendshipStatus.Accepted,
            RequestedAt = DateTime.UtcNow,
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
        var result = await _controller!.Send("recipient", "Hello!");

        var message = await _db.DirectMessages.FirstOrDefaultAsync();
        Assert.That(message, Is.Not.Null);
        Assert.That(message!.Content, Is.EqualTo("Hello!"));
    }

    [Test]
    public async Task Send_ValidMessage_ReturnsSuccess()
    {
        var result = await _controller!.Send("recipient", "Hello!") as JsonResult;
        var success = GetValue(result!.Value!, "success");
        Assert.That(success, Is.EqualTo(true));
    }

    [Test]
    public async Task Send_ValidMessage_SetsCorrectSenderAndRecipient()
    {
        await _controller!.Send("recipient", "Hello!");

        var message = await _db.DirectMessages.FirstOrDefaultAsync();
        Assert.That(message!.SenderProfileId, Is.EqualTo(_senderProfile.Id));
        Assert.That(message.RecipientProfileId, Is.EqualTo(_recipientProfile.Id));
    }

    [Test]
    public async Task Send_ValidMessage_StatusIsSent()
    {
        await _controller!.Send("recipient", "Hello!");

        var message = await _db.DirectMessages.FirstOrDefaultAsync();
        Assert.That(message!.Status, Is.EqualTo(MessageStatus.Sent));
    }

    [Test]
    public async Task Send_EmptyContent_ReturnsFailure()
    {
        var result = await _controller!.Send("recipient", "") as JsonResult;
        var success = GetValue(result!.Value!, "success");
        Assert.That(success, Is.EqualTo(false));
    }

    [Test]
    public async Task Send_WhitespaceContent_ReturnsFailure()
    {
        var result = await _controller!.Send("recipient", "   ") as JsonResult;
        var success = GetValue(result!.Value!, "success");
        Assert.That(success, Is.EqualTo(false));
    }

    [Test]
    public async Task Send_ContentOver1000Chars_ReturnsFailure()
    {
        var longContent = new string('a', 1001);
        var result = await _controller!.Send("recipient", longContent) as JsonResult;
        var success = GetValue(result!.Value!, "success");
        Assert.That(success, Is.EqualTo(false));
    }

    [Test]
    public async Task Send_ContentExactly1000Chars_ReturnsSuccess()
    {
        var content = new string('a', 1000);
        var result = await _controller!.Send("recipient", content) as JsonResult;
        var success = GetValue(result!.Value!, "success");
        Assert.That(success, Is.EqualTo(true));
    }

    [Test]
    public async Task Send_EmptyContent_DoesNotSaveToDb()
    {
        await _controller!.Send("recipient", "");
        var count = await _db.DirectMessages.CountAsync();
        Assert.That(count, Is.EqualTo(0));
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

    [Test]
    public async Task Send_ToNonExistentUser_ReturnsNotFound()
    {
        var result = await _controller!.Send("nobody", "Hello!");
        Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
    }

    [Test]
    public async Task Send_ToSelf_ReturnsBadRequest()
    {
        var result = await _controller!.Send("sender", "Hello!");
        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
    }

    [Test]
    public async Task Send_ResponseContainsExpectedFields()
    {
        var result = await _controller!.Send("recipient", "Hello!") as JsonResult;
        var value = result!.Value!;
        var type = value.GetType();

        Assert.That(type.GetProperty("messageId"), Is.Not.Null);
        Assert.That(type.GetProperty("sentAt"), Is.Not.Null);
        Assert.That(type.GetProperty("content"), Is.Not.Null);
        Assert.That(type.GetProperty("status"), Is.Not.Null);
    }

    [Test]
    public async Task Send_ResponseContent_MatchesSentContent()
    {
        var result = await _controller!.Send("recipient", "Hello!") as JsonResult;
        var content = GetValue(result!.Value!, "content");
        Assert.That(content, Is.EqualTo("Hello!"));
    }

    // --- Index ---

    [Test]
    public async Task Index_ReturnsView()
    {
        var result = await _controller!.Index();
        Assert.That(result, Is.InstanceOf<ViewResult>());
    }

    [Test]
    public async Task Index_WithNoMessages_ReturnEmptyConversations()
    {
        var result = await _controller!.Index() as ViewResult;
        var friendConversations = result!.ViewData["FriendConversations"] as List<DirectMessage>;
        var messageRequests = result!.ViewData["MessageRequests"] as List<DirectMessage>;

        Assert.That(friendConversations, Is.Empty);
        Assert.That(messageRequests, Is.Empty);
    }

    [Test]
    public async Task Index_MessageFromFriend_AppearsInFriendConversations()
    {
        _db.DirectMessages.Add(new DirectMessage
        {
            SenderProfileId = _recipientProfile.Id,
            RecipientProfileId = _senderProfile.Id,
            Content = "Hey!",
            Status = MessageStatus.Sent,
            SentAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        var result = await _controller!.Index() as ViewResult;
        var friendConversations = result!.ViewData["FriendConversations"] as List<DirectMessage>;

        Assert.That(friendConversations, Is.Not.Empty);
    }

    [Test]
    public async Task Index_MessageFromNonFriend_AppearsInMessageRequests()
    {
        _db.DirectMessages.Add(new DirectMessage
        {
            SenderProfileId = _nonFriendProfile.Id,
            RecipientProfileId = _senderProfile.Id,
            Content = "Hey stranger!",
            Status = MessageStatus.Sent,
            SentAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        var result = await _controller!.Index() as ViewResult;
        var messageRequests = result!.ViewData["MessageRequests"] as List<DirectMessage>;

        Assert.That(messageRequests, Is.Not.Empty);
    }

    [Test]
    public async Task Index_DeletedBySender_NotShownToSender()
    {
        _db.DirectMessages.Add(new DirectMessage
        {
            SenderProfileId = _senderProfile.Id,
            RecipientProfileId = _recipientProfile.Id,
            Content = "Deleted message",
            Status = MessageStatus.Sent,
            SentAt = DateTime.UtcNow,
            DeletedBySender = true
        });
        await _db.SaveChangesAsync();

        var result = await _controller!.Index() as ViewResult;
        var friendConversations = result!.ViewData["FriendConversations"] as List<DirectMessage>;
        var messageRequests = result!.ViewData["MessageRequests"] as List<DirectMessage>;

        Assert.That(friendConversations, Is.Empty);
        Assert.That(messageRequests, Is.Empty);
    }

    [Test]
    public async Task Index_MultipleMessagesWithSameUser_ShowsOnlyLatest()
    {
        _db.DirectMessages.AddRange(
            new DirectMessage
            {
                SenderProfileId = _senderProfile.Id,
                RecipientProfileId = _recipientProfile.Id,
                Content = "First",
                Status = MessageStatus.Sent,
                SentAt = DateTime.UtcNow.AddMinutes(-10)
            },
            new DirectMessage
            {
                SenderProfileId = _senderProfile.Id,
                RecipientProfileId = _recipientProfile.Id,
                Content = "Latest",
                Status = MessageStatus.Sent,
                SentAt = DateTime.UtcNow
            }
        );
        await _db.SaveChangesAsync();

        var result = await _controller!.Index() as ViewResult;
        var friendConversations = result!.ViewData["FriendConversations"] as List<DirectMessage>;

        // Should only show one conversation entry for this user pair
        Assert.That(friendConversations!.Count, Is.EqualTo(1));
        Assert.That(friendConversations[0].Content, Is.EqualTo("Latest"));
    }
}