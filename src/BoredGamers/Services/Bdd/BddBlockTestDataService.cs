using BoredGamers.Data;
using BoredGamers.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BoredGamers.Services.Bdd;

public class BddBlockResult
{
    public string BlockerUsername { get; set; } = "";
    public string BlockerPassword { get; set; } = "";
    public string TargetUsername { get; set; } = "";
    public string TargetPassword { get; set; } = "";
    public string TargetPostContent { get; set; } = "";
}

public class BddBlockTestDataService
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<User> _userManager;

    private const string BlockerUsername = "bdd_block_blocker";
    private const string TargetUsername = "bdd_block_target";
    private const string Password = "Test1234!";
    private const string TargetPostContent = "BDD block test post - you should not see this!";

    public BddBlockTestDataService(ApplicationDbContext db, UserManager<User> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    /// <summary>Resets to a fresh state: blocker and target exist with an accepted friendship, no block.</summary>
    public async Task<BddBlockResult> ResetWithFriendshipAsync()
    {
        await CleanupAsync();

        var blocker = await CreateUserWithProfileAsync(BlockerUsername);
        var target = await CreateUserWithProfileAsync(TargetUsername);

        var blockerProfile = await _db.Set<UserProfile>().FirstAsync(p => p.UserId == blocker.Id);
        var targetProfile = await _db.Set<UserProfile>().FirstAsync(p => p.UserId == target.Id);

        _db.Set<Friendship>().Add(new Friendship
        {
            RequesterProfileId = blockerProfile.Id,
            ReceiverProfileId = targetProfile.Id,
            Status = FriendshipStatus.Accepted,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        return new BddBlockResult
        {
            BlockerUsername = BlockerUsername,
            BlockerPassword = Password,
            TargetUsername = TargetUsername,
            TargetPassword = Password,
            TargetPostContent = ""
        };
    }

    /// <summary>Resets to a pre-blocked state: blocker has blocked target, target has a post.</summary>
    public async Task<BddBlockResult> ResetWithBlockAsync()
    {
        await CleanupAsync();

        var blocker = await CreateUserWithProfileAsync(BlockerUsername);
        var target = await CreateUserWithProfileAsync(TargetUsername);

        var blockerProfile = await _db.Set<UserProfile>().FirstAsync(p => p.UserId == blocker.Id);
        var targetProfile = await _db.Set<UserProfile>().FirstAsync(p => p.UserId == target.Id);

        // Add a post for the target
        _db.ProfilePosts.Add(new ProfilePost
        {
            UserProfileId = targetProfile.Id,
            Content = TargetPostContent,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        // Block (no friendship)
        _db.Set<BlockedUser>().Add(new BlockedUser
        {
            BlockerProfileId = blockerProfile.Id,
            BlockedProfileId = targetProfile.Id,
            BlockedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        return new BddBlockResult
        {
            BlockerUsername = BlockerUsername,
            BlockerPassword = Password,
            TargetUsername = TargetUsername,
            TargetPassword = Password,
            TargetPostContent = TargetPostContent
        };
    }

    private async Task CleanupAsync()
    {
        foreach (var username in new[] { BlockerUsername, TargetUsername })
        {
            var user = await _userManager.FindByNameAsync(username);
            if (user == null) continue;

            var profile = await _db.Set<UserProfile>().FirstOrDefaultAsync(p => p.UserId == user.Id);
            if (profile != null)
            {
                var posts = await _db.ProfilePosts.Where(p => p.UserProfileId == profile.Id).ToListAsync();
                _db.ProfilePosts.RemoveRange(posts);

                var friendships = await _db.Set<Friendship>()
                    .Where(f => f.RequesterProfileId == profile.Id || f.ReceiverProfileId == profile.Id)
                    .ToListAsync();
                _db.Set<Friendship>().RemoveRange(friendships);

                var blocks = await _db.Set<BlockedUser>()
                    .Where(b => b.BlockerProfileId == profile.Id || b.BlockedProfileId == profile.Id)
                    .ToListAsync();
                _db.Set<BlockedUser>().RemoveRange(blocks);

                await _db.SaveChangesAsync();
                _db.Set<UserProfile>().Remove(profile);
                await _db.SaveChangesAsync();
            }

            await _userManager.DeleteAsync(user);
        }
    }

    private async Task<User> CreateUserWithProfileAsync(string username)
    {
        var user = new User
        {
            UserName = username,
            Email = $"{username}@bddtest.com",
            EmailConfirmed = true,
            UpdatedAt = DateTime.UtcNow
        };

        var result = await _userManager.CreateAsync(user, Password);
        if (!result.Succeeded)
            throw new InvalidOperationException($"Failed to create {username}: {string.Join(", ", result.Errors.Select(e => e.Description))}");

        var profile = new UserProfile
        {
            UserId = user.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.Set<UserProfile>().Add(profile);
        await _db.SaveChangesAsync();

        return user;
    }
}
