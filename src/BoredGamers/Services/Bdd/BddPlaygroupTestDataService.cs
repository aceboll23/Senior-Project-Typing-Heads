using BoredGamers.Data;
using BoredGamers.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BoredGamers.Services.Bdd;

public class BddPlaygroupTestDataService
{
    private const string PlaygroupTestUserName = "bdd_playgroup_user";
    private const string PlaygroupTestEmail = "bdd_playgroup_user@local.test";
    private const string PlaygroupTestPassword = "BddPlaygroup123!";

    private readonly ApplicationDbContext _db;
    private readonly UserManager<User> _userManager;

    public BddPlaygroupTestDataService(ApplicationDbContext db, UserManager<User> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public async Task<BddPlaygroupSeedResult> ResetAndSeedPlaygroupTestDataAsync()
    {
        // 1. Remove existing BDD playgroup test user
        var existingUser = await _db.Users
            .OfType<User>()
            .FirstOrDefaultAsync(u => u.UserName == PlaygroupTestUserName);

        if (existingUser != null)
        {
            await _userManager.DeleteAsync(existingUser);
        }

        // 2. Create fresh BDD playgroup test user
        var user = new User
        {
            UserName = PlaygroupTestUserName,
            Email = PlaygroupTestEmail,
            EmailConfirmed = true
        };

        var createUserResult = await _userManager.CreateAsync(user, PlaygroupTestPassword);
        if (!createUserResult.Succeeded)
        {
            var errors = string.Join("; ", createUserResult.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to create seeded BDD playgroup user: {errors}");
        }

        return new BddPlaygroupSeedResult
        {
            Username = PlaygroupTestUserName,
            Password = PlaygroupTestPassword
        };
    }
}