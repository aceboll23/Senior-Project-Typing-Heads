using BoredGamers.Data;
using BoredGamers.Models;
using BoredGamers.Models.ViewModels;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace BoredGamers.Services.SocialFeed;

public class SocialFeedService : ISocialFeedService
{
    private readonly ApplicationDbContext _db;

    public SocialFeedService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<SocialFeedPostViewModel>> GetFeedForUserAsync(string userId)
    {
        var profile = await _db.Set<UserProfile>().FirstOrDefaultAsync(p => p.UserId == userId);
        if (profile == null)
            return Array.Empty<SocialFeedPostViewModel>();

        // Collect profile IDs of accepted friends (friendship works both directions)
        var friendProfileIds = await _db.Set<Friendship>()
            .Where(f =>
                (f.RequesterProfileId == profile.Id || f.ReceiverProfileId == profile.Id)
                && f.Status == FriendshipStatus.Accepted)
            .Select(f => f.RequesterProfileId == profile.Id
                ? f.ReceiverProfileId
                : f.RequesterProfileId)
            .ToListAsync();

        if (friendProfileIds.Count == 0)
            return Array.Empty<SocialFeedPostViewModel>();

        // Exclude profiles blocked in either direction
        var blockedProfileIds = await _db.Set<BlockedUser>()
            .Where(b => b.BlockerProfileId == profile.Id || b.BlockedProfileId == profile.Id)
            .Select(b => b.BlockerProfileId == profile.Id ? b.BlockedProfileId : b.BlockerProfileId)
            .ToListAsync();

        if (blockedProfileIds.Count > 0)
            friendProfileIds = friendProfileIds.Except(blockedProfileIds).ToList();

        if (friendProfileIds.Count == 0)
            return Array.Empty<SocialFeedPostViewModel>();

        var posts = await _db.ProfilePosts
            .Where(p => friendProfileIds.Contains(p.UserProfileId))
            .Include(p => p.UserProfile)
                .ThenInclude(up => up.User)
            .Include(p => p.Game)
            .Include(p => p.Replies)
                .ThenInclude(r => r.Author)
                    .ThenInclude(u => u.Profile)
            .Include(p => p.Likes)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        return posts.Select(p => new SocialFeedPostViewModel
        {
            PostId = p.Id,
            Content = p.Content,
            ImageUrl = p.ImageUrl,
            Category = p.Category,
            GameName = p.Game?.Name,
            CreatedAt = p.CreatedAt,
            AuthorUsername = p.UserProfile.User.UserName ?? "Unknown",
            AuthorAvatarUrl = p.UserProfile.AvatarUrl,
            LikeCount = p.Likes.Count,
            IsLikedByCurrentUser = p.Likes.Any(l => l.UserId == userId),
            Replies = p.Replies
                .OrderBy(r => r.CreatedAt)
                .Select(r => new PostReplyViewModel
                {
                    Id = r.Id,
                    Content = r.Content,
                    CreatedAt = r.CreatedAt,
                    AuthorUsername = r.Author.UserName ?? "Unknown",
                    AuthorAvatarUrl = r.Author.Profile?.AvatarUrl
                })
                .ToList()
        }).ToList();
    }
}
