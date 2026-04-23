using BoredGamers.Data;
using BoredGamers.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BoredGamers.Services.Bdd;

public class BddProfilePictureResult
{
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
}

public class BddProfilePictureTestDataService
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<User> _userManager;

    private const string TestUsername = "bdd_profile_picture_user";
    private const string Password = "Test1234!";

    public BddProfilePictureTestDataService(ApplicationDbContext db, UserManager<User> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public async Task<BddProfilePictureResult> ResetAndSeedAsync()
    {
        // Clean up existing test user
        var existing = await _userManager.FindByNameAsync(TestUsername);
        if (existing != null)
        {
            var profile = await _db.Set<UserProfile>().FirstOrDefaultAsync(p => p.UserId == existing.Id);
            if (profile != null) _db.Set<UserProfile>().Remove(profile);
            await _db.SaveChangesAsync();
            await _userManager.DeleteAsync(existing);
        }

        var user = new User
        {
            UserName = TestUsername,
            Email = $"{TestUsername}@bdd.test",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var createResult = await _userManager.CreateAsync(user, Password);
        if (!createResult.Succeeded)
            throw new Exception($"Failed to create BDD profile picture test user: {string.Join(", ", createResult.Errors.Select(e => e.Description))}");

        _db.Set<UserProfile>().Add(new UserProfile
        {
            UserId = user.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        return new BddProfilePictureResult
        {
            Username = TestUsername,
            Password = Password
        };
    }
}
