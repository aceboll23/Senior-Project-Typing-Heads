using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using BoredGamers.Controllers;
using BoredGamers.Data;
using BoredGamers.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;

namespace BoredGamers.Tests.Controllers;

[TestFixture]
public class AdminReportsEndpointTests
{
    private static async Task<ApplicationDbContext> CreateSqliteDbAsync(SqliteConnection conn)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(conn)
            .Options;

        var db = new BoredGamers.Tests.TestUtilities.TestApplicationDbContext(options);
        await db.Database.EnsureCreatedAsync();
        return db;
    }

    private static Mock<UserManager<User>> CreateUserManagerMock()
    {
        var store = new Mock<IUserStore<User>>();
        return new Mock<UserManager<User>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
    }

    private static AdminController CreateController(
        ApplicationDbContext db,
        Mock<UserManager<User>> mockUserManager,
        string currentUserId)
    {
        var controller = new AdminController(db, mockUserManager.Object);

        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, currentUserId) };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };

        return controller;
    }

    // T4 — Non-allowlisted user gets Forbid
    [Test]
    public async Task Reports_UserNotOnAllowlist_ReturnsForbid()
    {
        await using var conn = new SqliteConnection("DataSource=:memory:");
        await conn.OpenAsync();
        await using var db = await CreateSqliteDbAsync(conn);

        var user = new User { Id = "user-x", UserName = "RandomNonAllowlistedUser", Email = "x@test.com" };

        var mockUserManager = CreateUserManagerMock();
        mockUserManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);

        var controller = CreateController(db, mockUserManager, "user-x");

        var result = await controller.Reports(CancellationToken.None);

        Assert.That(result, Is.TypeOf<ForbidResult>());
    }

    // T4b — Allowlisted user gets Ok
    [Test]
    public async Task Reports_AllowlistedUser_ReturnsOk()
    {
        await using var conn = new SqliteConnection("DataSource=:memory:");
        await conn.OpenAsync();
        await using var db = await CreateSqliteDbAsync(conn);

        var user = new User { Id = "user-1", UserName = "PersonThree", Email = "p3@test.com" };

        var mockUserManager = CreateUserManagerMock();
        mockUserManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);

        var controller = CreateController(db, mockUserManager, "user-1");

        var result = await controller.Reports(CancellationToken.None);

        Assert.That(result, Is.TypeOf<OkObjectResult>());
    }
}
