using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using BoredGamers.Controllers;
using BoredGamers.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using NUnit.Framework;
using BoredGamers.Data;
using BoredGamers.Services;
using BoredGamers.Services.Email;
using Microsoft.EntityFrameworkCore;

namespace BoredGamers.Tests.Controllers
{
    [TestFixture]
    public class AccountControllerLoginTests
    {
        private Mock<UserManager<User>> _mockUserManager;
        private Mock<SignInManager<User>> _mockSignInManager;
        private AccountController _controller;

        [SetUp]
        public void SetUp()
        {
            // Setup UserManager mock
            var userStoreMock = new Mock<IUserStore<User>>();
            _mockUserManager = new Mock<UserManager<User>>(
                userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);

            // Setup SignInManager mock
            var contextAccessor = new Mock<IHttpContextAccessor>();
            var userPrincipalFactory = new Mock<IUserClaimsPrincipalFactory<User>>();
            _mockSignInManager = new Mock<SignInManager<User>>(
                _mockUserManager.Object,
                contextAccessor.Object,
                userPrincipalFactory.Object,
                null!, null!, null!, null!);

            var dbOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            var db = new ApplicationDbContext(dbOptions);

            var emailServiceMock = new Mock<IEmailService>();

            _controller = new AccountController(_mockUserManager.Object, _mockSignInManager.Object, db, emailServiceMock.Object);

            // Setup TempData (required for ModelState)
            _controller.TempData = new TempDataDictionary(
                new DefaultHttpContext(),
                Mock.Of<ITempDataProvider>());
        }

        [TearDown]
        public void TearDown()
        {
            _controller?.Dispose();
        }

        [Test]
        public void Login_Get_ReturnsViewResult()
        {
            // Act
            var result = _controller.Login();

            // Assert
            Assert.That(result, Is.TypeOf<ViewResult>());
        }

        [Test]
        public async Task Login_Post_ValidCredentials_WithUsername_ReturnsRedirectToHome()
        {
            // Arrange
            var model = new LoginViewModel
            {
                UsernameOrEmail = "franksinatra",
                Password = "Test123!"
            };

            var user = new User
            {
                UserName = "franksinatra",
                Email = "test@example.com"
            };

            _mockUserManager.Setup(x => x.FindByNameAsync("franksinatra"))
                .ReturnsAsync(user);

            _mockSignInManager.Setup(x => x.PasswordSignInAsync(
                user.UserName,
                model.Password,
                false,
                false))
                .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);

            // Act
            var result = await _controller.Login(model);

            // Assert
            Assert.That(result, Is.TypeOf<RedirectToActionResult>());
            var redirectResult = (RedirectToActionResult)result;
            Assert.That(redirectResult.ActionName, Is.EqualTo("Index"));
            Assert.That(redirectResult.ControllerName, Is.EqualTo("Home"));
        }

        [Test]
        public async Task Login_Post_ValidCredentials_WithEmail_ReturnsRedirectToHome()
        {
            // Arrange
            var model = new LoginViewModel
            {
                UsernameOrEmail = "frank@example.com",
                Password = "Test123!"
            };

            var user = new User
            {
                UserName = "franksinatra",
                Email = "test@example.com"
            };

            _mockUserManager.Setup(x => x.FindByNameAsync("frank@example.com"))
                .ReturnsAsync((User)null!);

            _mockUserManager.Setup(x => x.FindByEmailAsync("frank@example.com"))
                .ReturnsAsync(user);

            _mockSignInManager.Setup(x => x.PasswordSignInAsync(
                user.UserName,
                model.Password,
                false,
                false))
                .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);

            // Act
            var result = await _controller.Login(model);

            // Assert
            Assert.That(result, Is.TypeOf<RedirectToActionResult>());
            var redirectResult = (RedirectToActionResult)result;
            Assert.That(redirectResult.ActionName, Is.EqualTo("Index"));
            Assert.That(redirectResult.ControllerName, Is.EqualTo("Home"));
        }

        [Test]
        public async Task Login_Post_UserNotFound_ReturnsViewWithError()
        {
            // Arrange
            var model = new LoginViewModel
            {
                UsernameOrEmail = "nonexistent",
                Password = "Test123!"
            };

            _mockUserManager.Setup(x => x.FindByNameAsync("nonexistent"))
                .ReturnsAsync((User)null!);

            _mockUserManager.Setup(x => x.FindByEmailAsync("nonexistent"))
                .ReturnsAsync((User)null!);

            // Act
            var result = await _controller.Login(model);

            // Assert
            Assert.That(result, Is.TypeOf<ViewResult>());
            Assert.That(_controller.ModelState.IsValid, Is.False);
            Assert.That(_controller.ModelState.ContainsKey(string.Empty), Is.True);
            Assert.That(_controller.ModelState[string.Empty]!.Errors[0].ErrorMessage,
                Is.EqualTo("Invalid login attempt."));
        }

        [Test]
        public async Task Login_Post_IncorrectPassword_ReturnsViewWithError()
        {
            // Arrange
            var model = new LoginViewModel
            {
                UsernameOrEmail = "franksinatra",
                Password = "WrongPassword!"
            };

            var user = new User
            {
                UserName = "franksinatra",
                Email = "test@example.com"
            };

            _mockUserManager.Setup(x => x.FindByNameAsync("franksinatra"))
                .ReturnsAsync(user);

            _mockSignInManager.Setup(x => x.PasswordSignInAsync(
                user.UserName,
                model.Password,
                false,
                false))
                .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Failed);

            // Act
            var result = await _controller.Login(model);

            // Assert
            Assert.That(result, Is.TypeOf<ViewResult>());
            Assert.That(_controller.ModelState.IsValid, Is.False);
            Assert.That(_controller.ModelState.ContainsKey(string.Empty), Is.True);
            Assert.That(_controller.ModelState[string.Empty]!.Errors[0].ErrorMessage,
                Is.EqualTo("Invalid login attempt."));
        }

        [Test]
        public async Task Login_Post_InvalidModelState_ReturnsView()
        {
            // Arrange
            var model = new LoginViewModel
            {
                UsernameOrEmail = "",
                Password = ""
            };

            _controller.ModelState.AddModelError("UsernameOrEmail", "Required");

            // Act
            var result = await _controller.Login(model);

            // Assert
            Assert.That(result, Is.TypeOf<ViewResult>());
            var viewResult = (ViewResult)result;
            Assert.That(viewResult.Model, Is.EqualTo(model));
            Assert.That(_controller.ModelState.IsValid, Is.False);
        }

        [Test]
        public async Task Login_Post_AccountLockedOut_ReturnsViewWithError()
        {
            // Arrange
            var model = new LoginViewModel
            {
                UsernameOrEmail = "franksinatra",
                Password = "Test123!"
            };

            var user = new User
            {
                UserName = "franksinatra",
                Email = "test@example.com"
            };

            _mockUserManager.Setup(x => x.FindByNameAsync("franksinatra"))
                .ReturnsAsync(user);

            _mockSignInManager.Setup(x => x.PasswordSignInAsync(
                user.UserName,
                model.Password,
                false,
                false))
                .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.LockedOut);

            // Act
            var result = await _controller.Login(model);

            // Assert
            Assert.That(result, Is.TypeOf<ViewResult>());
            Assert.That(_controller.ModelState.IsValid, Is.False);
        }

        [Test]
        public async Task Login_Post_VerifyPasswordSignInParameters()
        {
            // Arrange
            var model = new LoginViewModel
            {
                UsernameOrEmail = "franksinatra",
                Password = "Test123!"
            };

            var user = new User
            {
                UserName = "franksinatra",
                Email = "test@example.com"
            };

            _mockUserManager.Setup(x => x.FindByNameAsync("franksinatra"))
                .ReturnsAsync(user);

            _mockSignInManager.Setup(x => x.PasswordSignInAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<bool>(),
                It.IsAny<bool>()))
                .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);

            // Act
            await _controller.Login(model);

            // Assert
            _mockSignInManager.Verify(x => x.PasswordSignInAsync(
                user.UserName,
                model.Password,
                false, // isPersistent
                false  // lockoutOnFailure
            ), Times.Once);
        }

        [Test]
        public async Task Logout_Post_CallsSignOutAndRedirectsToHome()
        {
            // Arrange
            _mockSignInManager.Setup(x => x.SignOutAsync())
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.Logout();

            // Assert
            Assert.That(result, Is.TypeOf<RedirectToActionResult>());
            var redirectResult = (RedirectToActionResult)result;
            Assert.That(redirectResult.ActionName, Is.EqualTo("Index"));
            Assert.That(redirectResult.ControllerName, Is.EqualTo("Home"));

            _mockSignInManager.Verify(x => x.SignOutAsync(), Times.Once);
        }
    }
}