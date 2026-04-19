using BoredGamers.Data;
using BoredGamers.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BoredGamers.Services.Bdd;

public class BddDeleteFriendTestDataService
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<User> _userManager;

    private const string OwnerUsername = "bdd_delete_friend_owner";
    private const string FriendUsername = "bdd_delete_friend_target";
    private const string Password = "BddFriend123!";

    public BddDeleteFriendTestDataService(ApplicationDbContext db, UserManager<User> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public async Task<BddDeleteFriendSeedResult> ResetAndSeedDeleteFriendTestDataAsync()
    {
        await CleanupExistingDataAsync();

        var owner = await CreateUserWithProfileAsync(OwnerUsername);
        var friend = await CreateUserWithProfileAsync(FriendUsername);

        var ownerProfile = await _db.Set<UserProfile>().FirstAsync(p => p.UserId == owner.Id);
        var friendProfile = await _db.Set<UserProfile>().FirstAsync(p => p.UserId == friend.Id);

        _db.Set<Friendship>().Add(new Friendship
        {
            RequesterProfileId = ownerProfile.Id,
            ReceiverProfileId = friendProfile.Id,
            Status = FriendshipStatus.Accepted,
            RequestedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        return new BddDeleteFriendSeedResult
        {
            Username = OwnerUsername,
            Password = Password,
            FriendUsername = FriendUsername,
            FriendProfileId = friendProfile.Id
        };
    }

    private async Task CleanupExistingDataAsync()
    {
        foreach (var username in new[] { OwnerUsername, FriendUsername })
        {
            var user = await _userManager.FindByNameAsync(username);
            if (user == null) continue;

            var profile = await _db.Set<UserProfile>().FirstOrDefaultAsync(p => p.UserId == user.Id);
            if (profile != null)
            {
                var friendships = await _db.Set<Friendship>()
                    .Where(f => f.RequesterProfileId == profile.Id || f.ReceiverProfileId == profile.Id)
                    .ToListAsync();
                _db.Set<Friendship>().RemoveRange(friendships);
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
            throw new InvalidOperationException($"Failed to create user {username}: {string.Join(", ", result.Errors.Select(e => e.Description))}");

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
