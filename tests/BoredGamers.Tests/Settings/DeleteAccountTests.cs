using BoredGamers.Controllers;
using BoredGamers.Data;
using BoredGamers.Models;
using BoredGamers.Services.Block;
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
public class DeleteAccountTests
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

        _userManagerMock.Setup(m => m.DeleteAsync(It.IsAny<User>()))
            .ReturnsAsync(IdentityResult.Success);

        _signInManagerMock.Setup(m => m.SignOutAsync())
            .Returns(Task.CompletedTask);

        _signInManagerMock.Setup(m => m.SignInAsync(
            It.IsAny<User>(), It.IsAny<bool>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        _controller = new SettingsController(
            _db,
            _userManagerMock.Object,
            _signInManagerMock.Object,
            _emailServiceMock.Object,
            new Mock<IBlockService>().Object);

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

    private DeleteAccountViewModel ValidModel() =>
        new DeleteAccountViewModel { CurrentPassword = "Password1!" };

    // --- GET DeleteAccount ---

    [Test]
    public void DeleteAccount_Get_ReturnsView()
    {
        var result = _controller.DeleteAccount();
        Assert.That(result, Is.InstanceOf<ViewResult>());
    }

    // --- POST DeleteAccount — invalid model ---

    [Test]
    public async Task DeleteAccount_Post_InvalidModel_ReturnsView()
    {
        _controller.ModelState.AddModelError("CurrentPassword", "Required");
        var result = await _controller.DeleteAccount(new DeleteAccountViewModel());
        Assert.That(result, Is.InstanceOf<ViewResult>());
    }

    [Test]
    public async Task DeleteAccount_Post_InvalidModel_DoesNotCallDelete()
    {
        _controller.ModelState.AddModelError("CurrentPassword", "Required");
        await _controller.DeleteAccount(new DeleteAccountViewModel());
        _userManagerMock.Verify(m => m.DeleteAsync(It.IsAny<User>()), Times.Never);
    }

    [Test]
    public async Task DeleteAccount_Post_InvalidModel_DoesNotSignOut()
    {
        _controller.ModelState.AddModelError("CurrentPassword", "Required");
        await _controller.DeleteAccount(new DeleteAccountViewModel());
        _signInManagerMock.Verify(m => m.SignOutAsync(), Times.Never);
    }

    // --- POST DeleteAccount — wrong password ---

    [Test]
    public async Task DeleteAccount_Post_WrongPassword_ReturnsView()
    {
        _userManagerMock.Setup(m => m.CheckPasswordAsync(It.IsAny<User>(), It.IsAny<string>()))
            .ReturnsAsync(false);

        var result = await _controller.DeleteAccount(ValidModel());
        Assert.That(result, Is.InstanceOf<ViewResult>());
    }

    [Test]
    public async Task DeleteAccount_Post_WrongPassword_AddsModelError()
    {
        _userManagerMock.Setup(m => m.CheckPasswordAsync(It.IsAny<User>(), It.IsAny<string>()))
            .ReturnsAsync(false);

        await _controller.DeleteAccount(ValidModel());

        Assert.That(
            _controller.ModelState.ContainsKey(nameof(DeleteAccountViewModel.CurrentPassword)),
            Is.True);
    }

    [Test]
    public async Task DeleteAccount_Post_WrongPassword_DoesNotCallDelete()
    {
        _userManagerMock.Setup(m => m.CheckPasswordAsync(It.IsAny<User>(), It.IsAny<string>()))
            .ReturnsAsync(false);

        await _controller.DeleteAccount(ValidModel());

        _userManagerMock.Verify(m => m.DeleteAsync(It.IsAny<User>()), Times.Never);
    }

    [Test]
    public async Task DeleteAccount_Post_WrongPassword_DoesNotSignOut()
    {
        _userManagerMock.Setup(m => m.CheckPasswordAsync(It.IsAny<User>(), It.IsAny<string>()))
            .ReturnsAsync(false);

        await _controller.DeleteAccount(ValidModel());

        _signInManagerMock.Verify(m => m.SignOutAsync(), Times.Never);
    }

    // --- POST DeleteAccount — successful deletion ---

    [Test]
    public async Task DeleteAccount_Post_ValidPassword_CallsDeleteAsync()
    {
        await _controller.DeleteAccount(ValidModel());
        _userManagerMock.Verify(m => m.DeleteAsync(_currentUser), Times.Once);
    }

    [Test]
    public async Task DeleteAccount_Post_ValidPassword_SignsOutBeforeDeleting()
    {
        var callOrder = new List<string>();

        _signInManagerMock.Setup(m => m.SignOutAsync())
            .Callback(() => callOrder.Add("signout"))
            .Returns(Task.CompletedTask);

        _userManagerMock.Setup(m => m.DeleteAsync(It.IsAny<User>()))
            .Callback(() => callOrder.Add("delete"))
            .ReturnsAsync(IdentityResult.Success);

        await _controller.DeleteAccount(ValidModel());

        Assert.That(callOrder.IndexOf("signout"), Is.LessThan(callOrder.IndexOf("delete")));
    }

    [Test]
    public async Task DeleteAccount_Post_ValidPassword_RedirectsToHome()
    {
        var result = await _controller.DeleteAccount(ValidModel()) as RedirectToActionResult;

        Assert.That(result!.ActionName, Is.EqualTo("Index"));
        Assert.That(result.ControllerName, Is.EqualTo("Home"));
    }

    [Test]
    public async Task DeleteAccount_Post_ValidPassword_CallsSignOut()
    {
        await _controller.DeleteAccount(ValidModel());
        _signInManagerMock.Verify(m => m.SignOutAsync(), Times.Once);
    }

    // --- POST DeleteAccount — deletion fails ---

    [Test]
    public async Task DeleteAccount_Post_DeleteFails_ReturnsView()
    {
        _userManagerMock.Setup(m => m.DeleteAsync(It.IsAny<User>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Delete failed." }));

        var result = await _controller.DeleteAccount(ValidModel());
        Assert.That(result, Is.InstanceOf<ViewResult>());
    }

    [Test]
    public async Task DeleteAccount_Post_DeleteFails_AddsModelError()
    {
        _userManagerMock.Setup(m => m.DeleteAsync(It.IsAny<User>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Delete failed." }));

        await _controller.DeleteAccount(ValidModel());

        Assert.That(_controller.ModelState.IsValid, Is.False);
    }

    [Test]
    public async Task DeleteAccount_Post_DeleteFails_SignsUserBackIn()
    {
        _userManagerMock.Setup(m => m.DeleteAsync(It.IsAny<User>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Delete failed." }));

        await _controller.DeleteAccount(ValidModel());

        _signInManagerMock.Verify(m =>
            m.SignInAsync(_currentUser, false, null), Times.Once);
    }

    [Test]
    public async Task DeleteAccount_Post_DeleteFails_DoesNotRedirect()
    {
        _userManagerMock.Setup(m => m.DeleteAsync(It.IsAny<User>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Delete failed." }));

        var result = await _controller.DeleteAccount(ValidModel());

        Assert.That(result, Is.Not.InstanceOf<RedirectToActionResult>());
    }
}