using BoredGamers.Controllers;
using BoredGamers.Data;
using BoredGamers.Models;
using BoredGamers.Services.Email;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Routing;

namespace BoredGamers.Tests;

[TestFixture]
public class SettingsControllerTests
{
    private ApplicationDbContext _db;
    private Mock<UserManager<User>> _userManagerMock;
    private Mock<SignInManager<User>> _signInManagerMock;
    private Mock<IEmailService> _emailServiceMock;
    private SettingsController _controller;

    private User _currentUser;

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

        var httpContextAccessor = new Mock<IHttpContextAccessor>();
        var userClaimsPrincipalFactory = new Mock<IUserClaimsPrincipalFactory<User>>();
        _signInManagerMock = new Mock<SignInManager<User>>(
            _userManagerMock.Object,
            httpContextAccessor.Object,
            userClaimsPrincipalFactory.Object,
            null!, null!, null!, null!);

        _emailServiceMock = new Mock<IEmailService>();

        _currentUser = new User
        {
            Id = "user-id",
            UserName = "testuser",
            Email = "test@example.com",
            IsBanned = false,
            IsDeactivated = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Users.Add(_currentUser);
        _db.SaveChanges();

        _userManagerMock.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(_currentUser);

        _userManagerMock.Setup(m => m.CheckPasswordAsync(It.IsAny<User>(), It.IsAny<string>()))
            .ReturnsAsync(true);

        _userManagerMock.Setup(m => m.UpdateAsync(It.IsAny<User>()))
            .ReturnsAsync(IdentityResult.Success);

        _userManagerMock.Setup(m => m.SetUserNameAsync(It.IsAny<User>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        _userManagerMock.Setup(m => m.SetEmailAsync(It.IsAny<User>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        _userManagerMock.Setup(m => m.FindByNameAsync(It.IsAny<string>()))
            .ReturnsAsync((User?)null);

        _userManagerMock.Setup(m => m.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((User?)null);

        _signInManagerMock.Setup(m => m.RefreshSignInAsync(It.IsAny<User>()))
            .Returns(Task.CompletedTask);

        _emailServiceMock.Setup(m => m.SendEmailAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        //because controller doesn't have a real http request context
        var urlHelperMock = new Mock<IUrlHelper>();
        urlHelperMock.Setup(u => u.Action(It.IsAny<UrlActionContext>()))
            .Returns("https://localhost/Settings/VerifyEmail?token=test");

        _controller = new SettingsController(
            _db,
            _userManagerMock.Object,
            _signInManagerMock.Object,
            _emailServiceMock.Object);

        _controller.Url = urlHelperMock.Object;

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, _currentUser.Id)
                }))
            }
        };
    }

    [TearDown]
    public void TearDown()
    {
        _controller.Dispose();
        _db.Dispose();
    }

    private SettingsViewModel ValidModel(string username = "testuser", string email = "test@example.com") =>
        new SettingsViewModel
        {
            Username = username,
            Email = email,
            CurrentPassword = "Password1!"
        };

    // --- GET Index ---

    [Test]
    public async Task Index_Get_ReturnsView()
    {
        var result = await _controller.Index();
        Assert.That(result, Is.InstanceOf<ViewResult>());
    }

    [Test]
    public async Task Index_Get_PopulatesModelWithCurrentUserDetails()
    {
        var result = await _controller.Index() as ViewResult;
        var model = result!.Model as SettingsViewModel;

        Assert.That(model!.Username, Is.EqualTo("testuser"));
        Assert.That(model.Email, Is.EqualTo("test@example.com"));
    }

    [Test]
    public async Task Index_Get_ShowsPendingEmailInViewData()
    {
        _currentUser.PendingEmail = "pending@example.com";

        var result = await _controller.Index() as ViewResult;

        Assert.That(result!.ViewData["PendingEmail"], Is.EqualTo("pending@example.com"));
    }

    [Test]
    public async Task Index_Get_NoPendingEmail_ViewDataIsNull()
    {
        _currentUser.PendingEmail = null;

        var result = await _controller.Index() as ViewResult;

        Assert.That(result!.ViewData["PendingEmail"], Is.Null);
    }

    // --- POST Index — invalid model ---

    [Test]
    public async Task Index_Post_InvalidModel_ReturnsView()
    {
        _controller.ModelState.AddModelError("Username", "Required");
        var result = await _controller.Index(ValidModel());
        Assert.That(result, Is.InstanceOf<ViewResult>());
    }

    [Test]
    public async Task Index_Post_InvalidModel_DoesNotCallUpdate()
    {
        _controller.ModelState.AddModelError("Username", "Required");
        await _controller.Index(ValidModel());
        _userManagerMock.Verify(m => m.UpdateAsync(It.IsAny<User>()), Times.Never);
    }

    // --- POST Index — wrong password ---

    [Test]
    public async Task Index_Post_WrongPassword_ReturnsView()
    {
        _userManagerMock.Setup(m => m.CheckPasswordAsync(It.IsAny<User>(), It.IsAny<string>()))
            .ReturnsAsync(false);

        var result = await _controller.Index(ValidModel());
        Assert.That(result, Is.InstanceOf<ViewResult>());
    }

    [Test]
    public async Task Index_Post_WrongPassword_AddsModelError()
    {
        _userManagerMock.Setup(m => m.CheckPasswordAsync(It.IsAny<User>(), It.IsAny<string>()))
            .ReturnsAsync(false);

        await _controller.Index(ValidModel());

        Assert.That(_controller.ModelState.ContainsKey(nameof(SettingsViewModel.CurrentPassword)), Is.True);
    }

    [Test]
    public async Task Index_Post_WrongPassword_DoesNotUpdateUser()
    {
        _userManagerMock.Setup(m => m.CheckPasswordAsync(It.IsAny<User>(), It.IsAny<string>()))
            .ReturnsAsync(false);

        await _controller.Index(ValidModel());

        _userManagerMock.Verify(m => m.UpdateAsync(It.IsAny<User>()), Times.Never);
    }

    // --- POST Index — no changes ---

    [Test]
    public async Task Index_Post_NoChanges_ReturnsView()
    {
        var result = await _controller.Index(ValidModel());
        Assert.That(result, Is.InstanceOf<ViewResult>());
    }

    [Test]
    public async Task Index_Post_NoChanges_ShowsSuccessMessage()
    {
        var result = await _controller.Index(ValidModel()) as ViewResult;
        Assert.That(result!.ViewData["SuccessMessage"], Is.Not.Null);
    }

    [Test]
    public async Task Index_Post_NoChanges_CallsRefreshSignIn()
    {
        await _controller.Index(ValidModel());
        _signInManagerMock.Verify(m => m.RefreshSignInAsync(_currentUser), Times.Once);
    }

    // --- POST Index — username change ---

    [Test]
    public async Task Index_Post_UsernameChanged_CallsSetUserName()
    {
        var result = await _controller.Index(ValidModel(username: "newusername"));
        _userManagerMock.Verify(m => m.SetUserNameAsync(_currentUser, "newusername"), Times.Once);
    }

    [Test]
    public async Task Index_Post_UsernameTaken_AddsModelError()
    {
        var otherUser = new User { Id = "other-id", UserName = "newusername" };
        _userManagerMock.Setup(m => m.FindByNameAsync("newusername"))
            .ReturnsAsync(otherUser);

        await _controller.Index(ValidModel(username: "newusername"));

        Assert.That(_controller.ModelState.ContainsKey(nameof(SettingsViewModel.Username)), Is.True);
    }

    [Test]
    public async Task Index_Post_UsernameTaken_DoesNotCallSetUserName()
    {
        var otherUser = new User { Id = "other-id", UserName = "newusername" };
        _userManagerMock.Setup(m => m.FindByNameAsync("newusername"))
            .ReturnsAsync(otherUser);

        await _controller.Index(ValidModel(username: "newusername"));

        _userManagerMock.Verify(m => m.SetUserNameAsync(It.IsAny<User>(), It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task Index_Post_SetUsernameFails_AddsModelError()
    {
        _userManagerMock.Setup(m => m.SetUserNameAsync(It.IsAny<User>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Username invalid." }));

        await _controller.Index(ValidModel(username: "newusername"));

        Assert.That(_controller.ModelState.ContainsKey(nameof(SettingsViewModel.Username)), Is.True);
    }

    // --- POST Index — email change ---

    [Test]
    public async Task Index_Post_EmailChanged_SetsPendingEmail()
    {
        await _controller.Index(ValidModel(email: "new@example.com"));

        Assert.That(_currentUser.PendingEmail, Is.EqualTo("new@example.com"));
    }

    [Test]
    public async Task Index_Post_EmailChanged_SetsVerificationToken()
    {
        await _controller.Index(ValidModel(email: "new@example.com"));

        Assert.That(_currentUser.EmailVerificationToken, Is.Not.Null);
    }

    [Test]
    public async Task Index_Post_EmailChanged_SetsTokenExpiry24Hours()
    {
        await _controller.Index(ValidModel(email: "new@example.com"));

        Assert.That(_currentUser.EmailVerificationTokenExpiry,
            Is.GreaterThan(DateTime.UtcNow.AddHours(23)));
    }

    [Test]
    public async Task Index_Post_EmailChanged_SendsVerificationEmail()
    {
        await _controller.Index(ValidModel(email: "new@example.com"));

        _emailServiceMock.Verify(m => m.SendEmailAsync(
            "new@example.com",
            It.IsAny<string>(),
            It.IsAny<string>()), Times.Once);
    }

    [Test]
    public async Task Index_Post_EmailChanged_ShowsPendingEmailInViewData()
    {
        var result = await _controller.Index(ValidModel(email: "new@example.com")) as ViewResult;
        Assert.That(result!.ViewData["PendingEmail"], Is.EqualTo("new@example.com"));
    }

    [Test]
    public async Task Index_Post_EmailTaken_AddsModelError()
    {
        var otherUser = new User { Id = "other-id", Email = "taken@example.com" };
        _userManagerMock.Setup(m => m.FindByEmailAsync("taken@example.com"))
            .ReturnsAsync(otherUser);

        await _controller.Index(ValidModel(email: "taken@example.com"));

        Assert.That(_controller.ModelState.ContainsKey(nameof(SettingsViewModel.Email)), Is.True);
    }

    [Test]
    public async Task Index_Post_EmailTaken_DoesNotSendEmail()
    {
        var otherUser = new User { Id = "other-id", Email = "taken@example.com" };
        _userManagerMock.Setup(m => m.FindByEmailAsync("taken@example.com"))
            .ReturnsAsync(otherUser);

        await _controller.Index(ValidModel(email: "taken@example.com"));

        _emailServiceMock.Verify(m => m.SendEmailAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task Index_Post_UsernameAndEmailBothChanged_SuccessMessageMentionsBoth()
    {
        await _controller.Index(ValidModel(username: "newusername", email: "new@example.com"));

        var result = await _controller.Index(
            ValidModel(username: "newusername", email: "new@example.com")) as ViewResult;

        var msg = result!.ViewData["SuccessMessage"] as string;
        Assert.That(msg, Does.Contain("verification email").IgnoreCase);
    }

    // --- GET VerifyEmail ---

    [Test]
    public async Task VerifyEmail_NullToken_RedirectsToIndex()
    {
        var result = await _controller.VerifyEmail(null!);
        Assert.That(result, Is.InstanceOf<RedirectToActionResult>());
    }

    [Test]
    public async Task VerifyEmail_InvalidToken_ReturnsErrorView()
    {
        var result = await _controller.VerifyEmail("invalid-token") as ViewResult;
        Assert.That(result!.ViewName, Is.EqualTo("VerifyEmailError"));
    }

    [Test]
    public async Task VerifyEmail_ExpiredToken_ReturnsErrorView()
    {
        _currentUser.EmailVerificationToken = "expired-token";
        _currentUser.EmailVerificationTokenExpiry = DateTime.UtcNow.AddHours(-1);
        _currentUser.PendingEmail = "new@example.com";
        await _db.SaveChangesAsync();

        var result = await _controller.VerifyEmail("expired-token") as ViewResult;
        Assert.That(result!.ViewName, Is.EqualTo("VerifyEmailError"));
    }

    [Test]
    public async Task VerifyEmail_ValidToken_CallsSetEmail()
    {
        _currentUser.EmailVerificationToken = "valid-token";
        _currentUser.EmailVerificationTokenExpiry = DateTime.UtcNow.AddHours(24);
        _currentUser.PendingEmail = "new@example.com";
        await _db.SaveChangesAsync();

        await _controller.VerifyEmail("valid-token");

        _userManagerMock.Verify(m => m.SetEmailAsync(_currentUser, "new@example.com"), Times.Once);
    }

    [Test]
    public async Task VerifyEmail_ValidToken_ClearsPendingEmailFields()
    {
        _currentUser.EmailVerificationToken = "valid-token";
        _currentUser.EmailVerificationTokenExpiry = DateTime.UtcNow.AddHours(24);
        _currentUser.PendingEmail = "new@example.com";
        await _db.SaveChangesAsync();

        await _controller.VerifyEmail("valid-token");

        Assert.That(_currentUser.PendingEmail, Is.Null);
        Assert.That(_currentUser.EmailVerificationToken, Is.Null);
        Assert.That(_currentUser.EmailVerificationTokenExpiry, Is.Null);
    }

    [Test]
    public async Task VerifyEmail_ValidToken_ReturnsSuccessView()
    {
        _currentUser.EmailVerificationToken = "valid-token";
        _currentUser.EmailVerificationTokenExpiry = DateTime.UtcNow.AddHours(24);
        _currentUser.PendingEmail = "new@example.com";
        await _db.SaveChangesAsync();

        var result = await _controller.VerifyEmail("valid-token") as ViewResult;
        Assert.That(result!.ViewName, Is.EqualTo("VerifyEmailSuccess"));
    }

    [Test]
    public async Task VerifyEmail_ValidToken_CallsRefreshSignIn()
    {
        _currentUser.EmailVerificationToken = "valid-token";
        _currentUser.EmailVerificationTokenExpiry = DateTime.UtcNow.AddHours(24);
        _currentUser.PendingEmail = "new@example.com";
        await _db.SaveChangesAsync();

        await _controller.VerifyEmail("valid-token");

        _signInManagerMock.Verify(m => m.RefreshSignInAsync(_currentUser), Times.Once);
    }

    [Test]
    public async Task VerifyEmail_SetEmailFails_ReturnsErrorView()
    {
        _currentUser.EmailVerificationToken = "valid-token";
        _currentUser.EmailVerificationTokenExpiry = DateTime.UtcNow.AddHours(24);
        _currentUser.PendingEmail = "new@example.com";
        await _db.SaveChangesAsync();

        _userManagerMock.Setup(m => m.SetEmailAsync(It.IsAny<User>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Email in use." }));

        var result = await _controller.VerifyEmail("valid-token") as ViewResult;
        Assert.That(result!.ViewName, Is.EqualTo("VerifyEmailError"));
    }
}