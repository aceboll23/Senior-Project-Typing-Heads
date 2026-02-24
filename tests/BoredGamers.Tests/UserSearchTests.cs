using Microsoft.EntityFrameworkCore.InMemory;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BoredGamers.Controllers;
using BoredGamers.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using BoredGamers.Data;
using Moq;

namespace BoredGamers.Tests;

[TestFixture]
public class UserSearchTests
{
    private ApplicationDbContext _db;
    private Mock<UserManager<User>> _userManagerMock;
    private UserSearchController? _controller;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: System.Guid.NewGuid().ToString())
            .Options;

        _db = new ApplicationDbContext(options);

        var store = new Mock<IUserStore<User>>();
        _userManagerMock = new Mock<UserManager<User>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        SeedTestUsers();

        // Return the actual DbSet so .Include() works through EF Core's pipeline
        _userManagerMock.Setup(m => m.Users)
            .Returns(_db.Set<User>());

        _controller = new UserSearchController(_db, _userManagerMock.Object);
    }

    [TearDown]
    public void TearDown()
    {
        _controller?.Dispose();
        _db.Dispose();
    }

    private void SeedTestUsers()
    {
        var users = new List<User>
        {
            new User
            {
                Id = "1",
                UserName = "alice",
                Email = "alice@test.com",
                IsBanned = false,
                IsDeactivated = false,
                Profile = new UserProfile { IsProfilePublic = true }
            },
            new User
            {
                Id = "2",
                UserName = "alicia",
                Email = "alicia@test.com",
                IsBanned = false,
                IsDeactivated = false,
                Profile = new UserProfile { IsProfilePublic = true }
            },
            new User
            {
                Id = "3",
                UserName = "bob",
                Email = "bob@test.com",
                IsBanned = false,
                IsDeactivated = false,
                Profile = new UserProfile { IsProfilePublic = true }
            },
            new User
            {
                Id = "4",
                UserName = "banneduser",
                Email = "banned@test.com",
                IsBanned = true,
                IsDeactivated = false,
                Profile = new UserProfile { IsProfilePublic = true }
            },
            new User
            {
                Id = "5",
                UserName = "deactivateduser",
                Email = "deactivated@test.com",
                IsBanned = false,
                IsDeactivated = true,
                Profile = new UserProfile { IsProfilePublic = true }
            },
            new User
            {
                Id = "6",
                UserName = "privateuser",
                Email = "private@test.com",
                IsBanned = false,
                IsDeactivated = false,
                Profile = new UserProfile { IsProfilePublic = false }
            }
        };

        _db.Users.AddRange(users);
        _db.SaveChanges();
    }

    // --- Query length ---

    [Test]
    public async Task Search_WithEmptyQuery_ReturnsEmptyList()
    {
        var result = await _controller!.Search("") as JsonResult;
        var data = result!.Value as IEnumerable<object>;
        Assert.That(data, Is.Empty);
    }

    [Test]
    public async Task Search_WithOneCharacter_ReturnsEmptyList()
    {
        var result = await _controller!.Search("a") as JsonResult;
        var data = result!.Value as IEnumerable<object>;
        Assert.That(data, Is.Empty);
    }

    [Test]
    public async Task Search_WithTwoOrMoreCharacters_ReturnsResults()
    {
        var result = await _controller!.Search("al") as JsonResult;
        var data = result!.Value as IEnumerable<dynamic>;
        Assert.That(data, Is.Not.Empty);
    }

    // --- Partial matching ---

    [Test]
    public async Task Search_PartialUsername_ReturnsMatchingUsers()
    {
        var result = await _controller!.Search("al") as JsonResult;
        var data = (result!.Value as IEnumerable<dynamic>)!.ToList();

        Assert.That(data.Count, Is.EqualTo(2));
    }

    [Test]
    public async Task Search_NoMatch_ReturnsEmptyList()
    {
        var result = await _controller!.Search("zzz") as JsonResult;
        var data = (result!.Value as IEnumerable<dynamic>)!.ToList();

        Assert.That(data, Is.Empty);
    }

    // --- Banned / deactivated filtering ---

    [Test]
    public async Task Search_BannedUser_IsNotReturned()
    {
        var result = await _controller!.Search("banned") as JsonResult;
        var data = (result!.Value as IEnumerable<dynamic>)!.ToList();

        Assert.That(data, Is.Empty);
    }

    [Test]
    public async Task Search_DeactivatedUser_IsNotReturned()
    {
        var result = await _controller!.Search("deactivated") as JsonResult;
        var data = (result!.Value as IEnumerable<dynamic>)!.ToList();

        Assert.That(data, Is.Empty);
    }

    // --- Result cap ---

    [Test]
    public async Task Search_MoreThanTenMatches_ReturnsOnlyTen()
    {
        var extras = Enumerable.Range(1, 15).Select(i => new User
        {
            Id = $"extra{i}",
            UserName = $"testuser{i}",
            Email = $"testuser{i}@test.com",
            IsBanned = false,
            IsDeactivated = false,
            Profile = new UserProfile { IsProfilePublic = true }
        });

        _db.Users.AddRange(extras);
        await _db.SaveChangesAsync();

        var result = await _controller!.Search("testuser") as JsonResult;
        var data = (result!.Value as IEnumerable<dynamic>)!.ToList();

        Assert.That(data.Count, Is.EqualTo(10));
    }
}