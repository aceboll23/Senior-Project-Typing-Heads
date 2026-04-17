using BoredGamers.Data;
using BoredGamers.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BoredGamers.Services.Bdd;

public class BddSocialFeedResult
{
    public string ViewerUsername { get; set; } = "";
    public string ViewerPassword { get; set; } = "";
    public string FriendUsername { get; set; } = "";
    public string FriendPassword { get; set; } = "";
    public string FriendPostContent { get; set; } = "";
    public string StrangerPostContent { get; set; } = "";
    public string OlderPostContent { get; set; } = "";
    public string NewerPostContent { get; set; } = "";
}

public class BddSocialFeedTestDataService
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<User> _userManager;

    private const string ViewerUsername = "bdd_feed_viewer";
    private const string FriendUsername = "bdd_feed_friend";
    private const string StrangerUsername = "bdd_feed_stranger";
    private const string Password = "Test1234!";
    private const string FriendPostContent = "Hello from your friend!";
    private const string StrangerPostContent = "You should not see this post.";
    private const string OlderPostContent = "This is the older post.";
    private const string NewerPostContent = "This is the newer post.";

    public BddSocialFeedTestDataService(ApplicationDbContext db, UserManager<User> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public async Task<BddSocialFeedResult> ResetAndSeedSocialFeedTestDataAsync()
    {
        await CleanupExistingDataAsync();

        var viewer = await CreateUserWithProfileAsync(ViewerUsername);
        var friend = await CreateUserWithProfileAsync(FriendUsername);
        var stranger = await CreateUserWithProfileAsync(StrangerUsername);

        // Create accepted friendship between viewer and friend
        var friendViewerProfile = await _db.Set<UserProfile>().FirstAsync(p => p.UserId == friend.Id);
        var viewerProfile = await _db.Set<UserProfile>().FirstAsync(p => p.UserId == viewer.Id);
        var strangerProfile = await _db.Set<UserProfile>().FirstAsync(p => p.UserId == stranger.Id);

        var friendship = new Friendship
        {
            RequesterProfileId = viewerProfile.Id,
            ReceiverProfileId = friendViewerProfile.Id,
            Status = FriendshipStatus.Accepted,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.Set<Friendship>().Add(friendship);

        // Friend has one basic post
        var friendPost = new ProfilePost
        {
            UserProfileId = friendViewerProfile.Id,
            Content = FriendPostContent,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.ProfilePosts.Add(friendPost);

        // Friend also has two posts for ordering test (older and newer)
        var olderPost = new ProfilePost
        {
            UserProfileId = friendViewerProfile.Id,
            Content = OlderPostContent,
            CreatedAt = DateTime.UtcNow.AddHours(-2),
            UpdatedAt = DateTime.UtcNow.AddHours(-2)
        };
        var newerPost = new ProfilePost
        {
            UserProfileId = friendViewerProfile.Id,
            Content = NewerPostContent,
            CreatedAt = DateTime.UtcNow.AddHours(-1),
            UpdatedAt = DateTime.UtcNow.AddHours(-1)
        };
        _db.ProfilePosts.Add(olderPost);
        _db.ProfilePosts.Add(newerPost);

        // Stranger has a post (viewer should NOT see this)
        var strangerPost = new ProfilePost
        {
            UserProfileId = strangerProfile.Id,
            Content = StrangerPostContent,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.ProfilePosts.Add(strangerPost);

        await _db.SaveChangesAsync();

        return new BddSocialFeedResult
        {
            ViewerUsername = ViewerUsername,
            ViewerPassword = Password,
            FriendUsername = FriendUsername,
            FriendPassword = Password,
            FriendPostContent = FriendPostContent,
            StrangerPostContent = StrangerPostContent,
            OlderPostContent = OlderPostContent,
            NewerPostContent = NewerPostContent
        };
    }

    private async Task CleanupExistingDataAsync()
    {
        foreach (var username in new[] { ViewerUsername, FriendUsername, StrangerUsername })
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
