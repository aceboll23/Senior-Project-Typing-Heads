using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using BoredGamers.Controllers;
using BoredGamers.Data;
using BoredGamers.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Moq;
using NUnit.Framework;

namespace BoredGamers.Tests.Profile;

[TestFixture]
public class ProfileControllerTests
{
    // ProfileController now takes TWO dependencies:
    //   1. ApplicationDbContext (for loading game collections)
    //   2. UserManager<User> (for finding users and checking who's logged in)
    private Mock<UserManager<User>> _mockUserManager;
    private ApplicationDbContext _db;
    private ProfileController _controller;

    // Sample users for testing
    private User _logan;
    private User _john;

    // SQLite in-memory DB helper (same pattern as other test files)
    private static async Task<ApplicationDbContext> CreateSqliteInMemoryDbAsync()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;

        var db = new ApplicationDbContext(options);
        await db.Database.EnsureCreatedAsync();

        return db;
    }

    [SetUp]
    public async Task SetUp()
    {
        // Create SQLite in-memory DB for the collection queries
        _db = await CreateSqliteInMemoryDbAsync();

        // Create the mock UserManager (same pattern Adler used in LoginTests)
        var userStoreMock = new Mock<IUserStore<User>>();
        _mockUserManager = new Mock<UserManager<User>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        // ProfileController now needs both db and userManager
        _controller = new ProfileController(_db, _mockUserManager.Object);

        // Create two fake users
        // User model now includes IsBanned, IsDeactivated, and Profile navigation
        _logan = new User
        {
            Id = "user-1",
            UserName = "LoganMontgomery",
            Email = "logan@example.com",
            FirstName = "Logan",
            LastName = "Montgomery",
            CreatedAt = new DateTime(2025, 1, 15),
            IsBanned = false,
            IsDeactivated = false,
            Profile = new UserProfile
            {
                UserId = "user-1",
                ShowEmail = true,
                ShowRealName = true,
                IsProfilePublic = true
            }
        };

        _john = new User
        {
            Id = "user-2",
            UserName = "JohnSmith",
            Email = "john@example.com",
            FirstName = "John",
            LastName = "Smith",
            CreatedAt = new DateTime(2025, 3, 20),
            IsBanned = false,
            IsDeactivated = false,
            Profile = new UserProfile
            {
                UserId = "user-2",
                ShowEmail = false,
                ShowRealName = true,
                IsProfilePublic = true
            }
        };
    }
    [TearDown]
    public void TearDown()
    {
        _db?.Dispose();
        _controller?.Dispose();
    }



    // Helper: makes the controller think a specific user is logged in
    private void SetLoggedInUser(User user)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Name, user.UserName!)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };

        // Tell the mock: when GetUserAsync is called, return this user
        _mockUserManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(user);
    }

    // Helper: makes the controller think no one is logged in
    // NOTE: ProfileController now has [Authorize] so anonymous access would normally
    // be blocked by middleware. These tests bypass middleware and test the action directly.
    private void SetLoggedInAsAnonymous()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity());
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };

        _mockUserManager.Setup(m => m.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync((User?)null);
    }

    // =====================================================================
    // Not Found — user doesn't exist or is banned/deactivated
    // =====================================================================

    [Test]
    // Navigating to /Profile/NonExistentUser should return NotFound
    public async Task Index_NonExistentUsername_ReturnsNotFound()
    {
        // The controller now queries UserManager.Users (IQueryable) with Include + filter
        // We need to set up the Users property to return our test data
        // Since the controller does .Include(u => u.Profile).FirstOrDefaultAsync(),
        // the simplest approach is to mock FindByNameAsync and use the db for the query

        // For this test, the mock Users queryable returns empty — user doesn't exist
        var emptyList = new List<User>().AsQueryable();
        _mockUserManager.Setup(m => m.Users).Returns(emptyList.BuildMockDbSet().Object);

        SetLoggedInUser(_logan);

        var result = await _controller.Index("NonExistentUser");

        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    [Test]
    // A banned user's profile should return NotFound
    public async Task Index_BannedUser_ReturnsNotFound()
    {
        var bannedUser = new User
        {
            Id = "user-banned",
            UserName = "BannedGuy",
            IsBanned = true,
            IsDeactivated = false,
            CreatedAt = DateTime.Now
        };

        var users = new List<User> { bannedUser }.AsQueryable();
        _mockUserManager.Setup(m => m.Users).Returns(users.BuildMockDbSet().Object);

        SetLoggedInUser(_logan);

        var result = await _controller.Index("BannedGuy");

        Assert.That(result, Is.InstanceOf<NotFoundResult>());
    }

    // =====================================================================
    // Own Profile — viewing your own page
    // =====================================================================

    [Test]
    // When Logan views /Profile/LoganMontgomery, IsOwnProfile should be true
    public async Task Index_ViewingOwnProfile_SetsIsOwnProfileTrue()
    {
        var users = new List<User> { _logan }.AsQueryable();
        _mockUserManager.Setup(m => m.Users).Returns(users.BuildMockDbSet().Object);
        SetLoggedInUser(_logan);

        var result = await _controller.Index("LoganMontgomery");

        Assert.That(result, Is.InstanceOf<ViewResult>());
        Assert.That(_controller.ViewData["IsOwnProfile"], Is.EqualTo(true));
    }

    [Test]
    // When viewing your own profile, email should be in ViewData
    public async Task Index_ViewingOwnProfile_SetsEmail()
    {
        var users = new List<User> { _logan }.AsQueryable();
        _mockUserManager.Setup(m => m.Users).Returns(users.BuildMockDbSet().Object);
        SetLoggedInUser(_logan);

        await _controller.Index("LoganMontgomery");

        Assert.That(_controller.ViewData["Email"], Is.EqualTo("logan@example.com"));
    }

    // =====================================================================
    // Other User's Profile — viewing someone else's page
    // =====================================================================

    [Test]
    // When Logan views /Profile/JohnSmith, IsOwnProfile should be false
    public async Task Index_ViewingOtherProfile_SetsIsOwnProfileFalse()
    {
        var users = new List<User> { _john }.AsQueryable();
        _mockUserManager.Setup(m => m.Users).Returns(users.BuildMockDbSet().Object);
        SetLoggedInUser(_logan);

        var result = await _controller.Index("JohnSmith");

        Assert.That(result, Is.InstanceOf<ViewResult>());
        Assert.That(_controller.ViewData["IsOwnProfile"], Is.EqualTo(false));
    }

    // =====================================================================
    // ViewData — correct data is passed to the view
    // =====================================================================

    [Test]
    // Verify all ViewData fields are set correctly
    public async Task Index_ValidUser_SetsCorrectViewData()
    {
        var users = new List<User> { _logan }.AsQueryable();
        _mockUserManager.Setup(m => m.Users).Returns(users.BuildMockDbSet().Object);
        SetLoggedInUser(_logan);

        await _controller.Index("LoganMontgomery");

        Assert.That(_controller.ViewData["ProfileUsername"], Is.EqualTo("LoganMontgomery"));
        Assert.That(_controller.ViewData["MemberSince"], Is.EqualTo("January 2025"));
        Assert.That(_controller.ViewData["FirstName"], Is.EqualTo("Logan"));
        Assert.That(_controller.ViewData["LastName"], Is.EqualTo("Montgomery"));
        Assert.That(_controller.ViewData["ShowEmail"], Is.EqualTo(true));
        Assert.That(_controller.ViewData["ShowRealName"], Is.EqualTo(true));
    }
}

// =====================================================================
// Helper to mock IQueryable<User> for UserManager.Users
// The controller does _userManager.Users.Include(...).FirstOrDefaultAsync(...)
// so we need a mock DbSet that supports async LINQ queries
// =====================================================================
public static class MockDbSetExtensions
{
    public static Mock<DbSet<T>> BuildMockDbSet<T>(this IQueryable<T> data) where T : class
    {
        var mockSet = new Mock<DbSet<T>>();

        mockSet.As<IAsyncEnumerable<T>>()
            .Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
            .Returns(new TestAsyncEnumerator<T>(data.GetEnumerator()));

        mockSet.As<IQueryable<T>>().Setup(m => m.Provider)
            .Returns(new TestAsyncQueryProvider<T>(data.Provider));
        mockSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(data.Expression);
        mockSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(data.ElementType);
        mockSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(data.GetEnumerator());

        return mockSet;
    }
}

// Async query support classes — needed because EF Core's FirstOrDefaultAsync
// requires the provider to support async operations
internal class TestAsyncQueryProvider<T> : IAsyncQueryProvider
{
    private readonly IQueryProvider _inner;

    internal TestAsyncQueryProvider(IQueryProvider inner) => _inner = inner;

    public IQueryable CreateQuery(System.Linq.Expressions.Expression expression)
        => new TestAsyncEnumerable<T>(expression);

    public IQueryable<TElement> CreateQuery<TElement>(System.Linq.Expressions.Expression expression)
        => new TestAsyncEnumerable<TElement>(expression);

    public object Execute(System.Linq.Expressions.Expression expression)
        => _inner.Execute(expression)!;

    public TResult Execute<TResult>(System.Linq.Expressions.Expression expression)
        => _inner.Execute<TResult>(expression);

    public TResult ExecuteAsync<TResult>(System.Linq.Expressions.Expression expression,
        CancellationToken cancellationToken = default)
    {
        var resultType = typeof(TResult).GetGenericArguments()[0];
        var executeMethod = typeof(IQueryProvider)
            .GetMethod(nameof(IQueryProvider.Execute), 1, new[] { typeof(System.Linq.Expressions.Expression) })!
            .MakeGenericMethod(resultType);

        var result = executeMethod.Invoke(_inner, new object[] { expression });

        return (TResult)typeof(Task).GetMethod(nameof(Task.FromResult))!
            .MakeGenericMethod(resultType)
            .Invoke(null, new[] { result })!;
    }
}

internal class TestAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
{
    public TestAsyncEnumerable(IEnumerable<T> enumerable) : base(enumerable) { }
    public TestAsyncEnumerable(System.Linq.Expressions.Expression expression) : base(expression) { }

    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        => new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());

    IQueryProvider IQueryable.Provider => new TestAsyncQueryProvider<T>(this);
}

internal class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
{
    private readonly IEnumerator<T> _inner;

    public TestAsyncEnumerator(IEnumerator<T> inner) => _inner = inner;

    public T Current => _inner.Current;

    public ValueTask<bool> MoveNextAsync() => new ValueTask<bool>(_inner.MoveNext());

    public ValueTask DisposeAsync()
    {
        _inner.Dispose();
        return default;
    }
}
