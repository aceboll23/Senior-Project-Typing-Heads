using BoredGamers.Data;
using BoredGamers.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BoredGamers.Services.Bdd;

public class BddPostTestDataService
{
    private const string OwnerUserName = "bdd_post_owner";
    private const string OwnerEmail = "bdd_post_owner@local.test";
    private const string OwnerPassword = "BddPost123";

    private const string FriendUserName = "bdd_post_friend";
    private const string FriendEmail = "bdd_post_friend@local.test";
    private const string FriendPassword = "BddFriend123";

    private const string SeededPostContent = "BDD seed post - Just finished an epic game of Terraforming Mars!";

    private readonly ApplicationDbContext _db;
    private readonly UserManager<User> _userManager;

    public BddPostTestDataService(ApplicationDbContext db, UserManager<User> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public async Task<BddPostSeedResult> ResetAndSeedPostTestDataAsync()
    {
        await CleanUpUserAsync(OwnerUserName);
        await CleanUpUserAsync(FriendUserName);

        // Create owner
        var owner = new User
        {
            UserName = OwnerUserName,
            Email = OwnerEmail,
            EmailConfirmed = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var ownerResult = await _userManager.CreateAsync(owner, OwnerPassword);
        if (!ownerResult.Succeeded)
            throw new InvalidOperationException($"Failed to create BDD post owner: {string.Join(", ", ownerResult.Errors.Select(e => e.Description))}");

        var ownerProfile = new UserProfile
        {
            UserId = owner.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.Set<UserProfile>().Add(ownerProfile);
        await _db.SaveChangesAsync();

        // Create friend
        var friend = new User
        {
            UserName = FriendUserName,
            Email = FriendEmail,
            EmailConfirmed = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var friendResult = await _userManager.CreateAsync(friend, FriendPassword);
        if (!friendResult.Succeeded)
            throw new InvalidOperationException($"Failed to create BDD post friend: {string.Join(", ", friendResult.Errors.Select(e => e.Description))}");

        var friendProfile = new UserProfile
        {
            UserId = friend.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.Set<UserProfile>().Add(friendProfile);
        await _db.SaveChangesAsync();

        // Create accepted friendship
        var friendship = new Friendship
        {
            RequesterProfileId = ownerProfile.Id,
            ReceiverProfileId = friendProfile.Id,
            Status = FriendshipStatus.Accepted,
            RequestedAt = DateTime.UtcNow,
            RespondedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.Set<Friendship>().Add(friendship);

        // Seed a game for the game-linking BDD test
        var seededGame = await _db.Games.FirstOrDefaultAsync(g => g.BggGameId == 9001);
        if (seededGame == null)
        {
            seededGame = new Game
            {
                BggGameId = 9001,
                Name = "Terraforming Mars BDD",
                LastSyncedAt = DateTime.UtcNow
            };
            _db.Games.Add(seededGame);
            await _db.SaveChangesAsync();
        }

        // Seed one existing post on the owner's profile
        var seededPost = new ProfilePost
        {
            UserProfileId = ownerProfile.Id,
            Content = SeededPostContent,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.ProfilePosts.Add(seededPost);
        await _db.SaveChangesAsync();

        return new BddPostSeedResult
        {
            OwnerUsername = OwnerUserName,
            OwnerPassword = OwnerPassword,
            FriendUsername = FriendUserName,
            FriendPassword = FriendPassword,
            ExistingPostContent = SeededPostContent,
            SeededGameId = seededGame.Id,
            SeededGameName = seededGame.Name
        };
    }

    private async Task CleanUpUserAsync(string username)
    {
        var user = await _db.Users
            .OfType<User>()
            .Include(u => u.Profile)
            .FirstOrDefaultAsync(u => u.UserName == username);

        if (user == null) return;

        if (user.Profile != null)
        {
            var posts = await _db.ProfilePosts
                .Where(p => p.UserProfileId == user.Profile.Id)
                .ToListAsync();
            if (posts.Count > 0)
                _db.ProfilePosts.RemoveRange(posts);

            var friendships = await _db.Set<Friendship>()
                .Where(f => f.RequesterProfileId == user.Profile.Id || f.ReceiverProfileId == user.Profile.Id)
                .ToListAsync();
            if (friendships.Count > 0)
                _db.Set<Friendship>().RemoveRange(friendships);

            await _db.SaveChangesAsync();
        }

        await _userManager.DeleteAsync(user);
    }
}

public class BddPostSeedResult
{
    public string OwnerUsername { get; set; } = "";
    public string OwnerPassword { get; set; } = "";
    public string FriendUsername { get; set; } = "";
    public string FriendPassword { get; set; } = "";
    public string ExistingPostContent { get; set; } = "";
    public int SeededGameId { get; set; }
    public string SeededGameName { get; set; } = "";
}
