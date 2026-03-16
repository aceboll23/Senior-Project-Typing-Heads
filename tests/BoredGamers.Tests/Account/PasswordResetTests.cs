using BoredGamers.Controllers;
using BoredGamers.Data;
using BoredGamers.Models;
using BoredGamers.Services;
using BoredGamers.Services.Email;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BoredGamers.Tests;

[TestFixture]
public class PasswordResetTests
{
    private ApplicationDbContext _db;
    private Mock<UserManager<User>> _userManagerMock;
    private Mock<SignInManager<User>> _signInManagerMock;
    private Mock<IEmailService> _emailServiceMock;
    private AccountController? _controller;

    private User _existingUser;

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

        var contextAccessor = new Mock<IHttpContextAccessor>();
        var claimsFactory = new Mock<IUserClaimsPrincipalFactory<User>>();
        _signInManagerMock = new Mock<SignInManager<User>>(
            _userManagerMock.Object,
            contextAccessor.Object,
            claimsFactory.Object,
            null!, null!, null!, null!);

        _emailServiceMock = new Mock<IEmailService>();

        SeedUsers();

        _userManagerMock.Setup(m => m.FindByEmailAsync("existing@test.com"))
            .ReturnsAsync(_existingUser);
        _userManagerMock.Setup(m => m.FindByEmailAsync("unknown@test.com"))
            .ReturnsAsync((User?)null);
        _userManagerMock.Setup(m => m.UpdateAsync(It.IsAny<User>()))
            .ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(m => m.GeneratePasswordResetTokenAsync(It.IsAny<User>()))
            .ReturnsAsync("identity-reset-token");
        _userManagerMock.Setup(m => m.ResetPasswordAsync(It.IsAny<User>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        _controller = new AccountController(
            _userManagerMock.Object,
            _signInManagerMock.Object,
            _db,
            _emailServiceMock.Object);

        // Mock Url.Action so the reset link can be built
        var urlHelperMock = new Mock<IUrlHelper>();
        urlHelperMock.Setup(u => u.Action(It.IsAny<UrlActionContext>()))
            .Returns("http://localhost/Account/ResetPassword?token=abc");
        _controller.Url = urlHelperMock.Object;

        // Gives the controller a fake HttpContext
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    [TearDown]
    public void TearDown()
    {
        _controller?.Dispose();
        _db.Dispose();
    }

    private void SeedUsers()
    {
        _existingUser = new User
        {
            Id = "user-1",
            UserName = "testuser",
            Email = "existing@test.com",
            IsBanned = false,
            IsDeactivated = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Users.Add(_existingUser);
        _db.SaveChanges();
    }

    private object? GetValue(object obj, string property) =>
        obj.GetType().GetProperty(property)?.GetValue(obj);

    // --- ForgotPassword GET ---

    [Test]
    public void ForgotPassword_Get_ReturnsView()
    {
        var result = _controller!.ForgotPassword();
        Assert.That(result, Is.InstanceOf<ViewResult>());
    }

    // --- ForgotPassword POST ---

    [Test]
    public async Task ForgotPassword_Post_WithRegisteredEmail_RedirectsToConfirmation()
    {
        var model = new ForgotPasswordViewModel { Email = "existing@test.com" };
        var result = await _controller!.ForgotPassword(model);

        Assert.That(result, Is.InstanceOf<RedirectToActionResult>());
        var redirect = result as RedirectToActionResult;
        Assert.That(redirect!.ActionName, Is.EqualTo("ForgotPasswordConfirmation"));
    }

    [Test]
    public async Task ForgotPassword_Post_WithUnregisteredEmail_StillRedirectsToConfirmation()
    {
        // Should not reveal whether the email is registered
        var model = new ForgotPasswordViewModel { Email = "unknown@test.com" };
        var result = await _controller!.ForgotPassword(model);

        Assert.That(result, Is.InstanceOf<RedirectToActionResult>());
        var redirect = result as RedirectToActionResult;
        Assert.That(redirect!.ActionName, Is.EqualTo("ForgotPasswordConfirmation"));
    }

    [Test]
    public async Task ForgotPassword_Post_WithRegisteredEmail_SendsEmail()
    {
        var model = new ForgotPasswordViewModel { Email = "existing@test.com" };
        await _controller!.ForgotPassword(model);

        _emailServiceMock.Verify(
            e => e.SendEmailAsync("existing@test.com", It.IsAny<string>(), It.IsAny<string>()),
            Times.Once);
    }

    [Test]
    public async Task ForgotPassword_Post_WithUnregisteredEmail_DoesNotSendEmail()
    {
        var model = new ForgotPasswordViewModel { Email = "unknown@test.com" };
        await _controller!.ForgotPassword(model);

        _emailServiceMock.Verify(e => e.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    [Test]
    public async Task ForgotPassword_Post_WithRegisteredEmail_SetsTokenOnUser()
    {
        var model = new ForgotPasswordViewModel { Email = "existing@test.com" };
        await _controller!.ForgotPassword(model);

        Assert.That(_existingUser.PasswordResetToken, Is.Not.Null);
    }

    [Test]
    public async Task ForgotPassword_Post_WithRegisteredEmail_SetsExpiryTo15Minutes()
    {
        var before = DateTime.UtcNow.AddMinutes(14);
        var after = DateTime.UtcNow.AddMinutes(16);

        var model = new ForgotPasswordViewModel { Email = "existing@test.com" };
        await _controller!.ForgotPassword(model);

        Assert.That(_existingUser.PasswordResetTokenExpiry, Is.InRange(before, after));
    }

    [Test]
    public async Task ForgotPassword_Post_WithInvalidModel_ReturnsView()
    {
        _controller!.ModelState.AddModelError("Email", "Required");
        var model = new ForgotPasswordViewModel { Email = "" };
        var result = await _controller!.ForgotPassword(model);

        Assert.That(result, Is.InstanceOf<ViewResult>());
    }

    // --- ResetPassword GET ---

    [Test]
    public async Task ResetPassword_Get_WithValidToken_ReturnsView()
    {
        // Seed user with a valid token directly in the DB
        _existingUser.PasswordResetToken = "valid-token";
        _existingUser.PasswordResetTokenExpiry = DateTime.UtcNow.AddMinutes(10);
        await _db.SaveChangesAsync();

        var result = await _controller!.ResetPassword("valid-token");
        Assert.That(result, Is.InstanceOf<ViewResult>());
    }

    [Test]
    public async Task ResetPassword_Get_WithExpiredToken_ReturnsErrorView()
    {
        _existingUser.PasswordResetToken = "expired-token";
        _existingUser.PasswordResetTokenExpiry = DateTime.UtcNow.AddMinutes(-5); // already expired
        await _db.SaveChangesAsync();

        var result = await _controller!.ResetPassword("expired-token") as ViewResult;
        Assert.That(result!.ViewName, Is.EqualTo("ResetPasswordError"));
    }

    [Test]
    public async Task ResetPassword_Get_WithInvalidToken_ReturnsErrorView()
    {
        var result = await _controller!.ResetPassword("nonexistent-token") as ViewResult;
        Assert.That(result!.ViewName, Is.EqualTo("ResetPasswordError"));
    }

    [Test]
    public async Task ResetPassword_Get_WithNullToken_RedirectsToLogin()
    {
        var result = await _controller!.ResetPassword((string)null!);
        Assert.That(result, Is.InstanceOf<RedirectToActionResult>());
        var redirect = result as RedirectToActionResult;
        Assert.That(redirect!.ActionName, Is.EqualTo("Login"));
    }

    // --- ResetPassword POST ---

    [Test]
    public async Task ResetPassword_Post_WithValidToken_RedirectsToSuccess()
    {
        _existingUser.PasswordResetToken = "valid-token";
        _existingUser.PasswordResetTokenExpiry = DateTime.UtcNow.AddMinutes(10);
        await _db.SaveChangesAsync();

        var model = new ResetPasswordViewModel
        {
            Token = "valid-token",
            Password = "NewPassword1",
            ConfirmPassword = "NewPassword1"
        };

        var result = await _controller!.ResetPassword(model);
        Assert.That(result, Is.InstanceOf<RedirectToActionResult>());
        var redirect = result as RedirectToActionResult;
        Assert.That(redirect!.ActionName, Is.EqualTo("ResetPasswordSuccess"));
    }

    [Test]
    public async Task ResetPassword_Post_WithValidToken_ClearsTokenFromUser()
    {
        _existingUser.PasswordResetToken = "valid-token";
        _existingUser.PasswordResetTokenExpiry = DateTime.UtcNow.AddMinutes(10);
        await _db.SaveChangesAsync();

        var model = new ResetPasswordViewModel
        {
            Token = "valid-token",
            Password = "NewPassword1",
            ConfirmPassword = "NewPassword1"
        };

        await _controller!.ResetPassword(model);

        // Token should be nulled out so it can't be reused
        Assert.That(_existingUser.PasswordResetToken, Is.Null);
        Assert.That(_existingUser.PasswordResetTokenExpiry, Is.Null);
    }

    [Test]
    public async Task ResetPassword_Post_WithExpiredToken_ReturnsErrorView()
    {
        _existingUser.PasswordResetToken = "expired-token";
        _existingUser.PasswordResetTokenExpiry = DateTime.UtcNow.AddMinutes(-5);
        await _db.SaveChangesAsync();

        var model = new ResetPasswordViewModel
        {
            Token = "expired-token",
            Password = "NewPassword1",
            ConfirmPassword = "NewPassword1"
        };

        var result = await _controller!.ResetPassword(model) as ViewResult;
        Assert.That(result!.ViewName, Is.EqualTo("ResetPasswordError"));
    }

    [Test]
    public async Task ResetPassword_Post_WithInvalidToken_ReturnsErrorView()
    {
        var model = new ResetPasswordViewModel
        {
            Token = "nonexistent-token",
            Password = "NewPassword1",
            ConfirmPassword = "NewPassword1"
        };

        var result = await _controller!.ResetPassword(model) as ViewResult;
        Assert.That(result!.ViewName, Is.EqualTo("ResetPasswordError"));
    }

    [Test]
    public async Task ResetPassword_Post_WithInvalidModel_ReturnsView()
    {
        _controller!.ModelState.AddModelError("Password", "Required");

        var model = new ResetPasswordViewModel
        {
            Token = "valid-token",
            Password = "",
            ConfirmPassword = ""
        };

        var result = await _controller!.ResetPassword(model);
        Assert.That(result, Is.InstanceOf<ViewResult>());
    }

    [Test]
    public async Task ResetPassword_Post_WhenIdentityFails_ReturnsViewWithErrors()
    {
        _existingUser.PasswordResetToken = "valid-token";
        _existingUser.PasswordResetTokenExpiry = DateTime.UtcNow.AddMinutes(10);
        await _db.SaveChangesAsync();

        // Make Identity report a failure (e.g. password too weak)
        _userManagerMock.Setup(m => m.ResetPasswordAsync(It.IsAny<User>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Password too weak." }));

        var model = new ResetPasswordViewModel
        {
            Token = "valid-token",
            Password = "weak",
            ConfirmPassword = "weak"
        };

        var result = await _controller!.ResetPassword(model);
        Assert.That(result, Is.InstanceOf<ViewResult>());
    }

    // --- ForgotPasswordConfirmation ---

    [Test]
    public void ForgotPasswordConfirmation_ReturnsView()
    {
        var result = _controller!.ForgotPasswordConfirmation();
        Assert.That(result, Is.InstanceOf<ViewResult>());
    }

    // --- ResetPasswordSuccess ---

    [Test]
    public void ResetPasswordSuccess_ReturnsView()
    {
        var result = _controller!.ResetPasswordSuccess();
        Assert.That(result, Is.InstanceOf<ViewResult>());
    }
}