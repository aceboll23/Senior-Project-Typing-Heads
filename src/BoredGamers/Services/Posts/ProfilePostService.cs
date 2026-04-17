using BoredGamers.Data;
using BoredGamers.Models;
using BoredGamers.Services;
using Microsoft.EntityFrameworkCore;

namespace BoredGamers.Services.Posts;

public class ProfilePostService : IProfilePostService
{
    private readonly ApplicationDbContext _db;

    public ProfilePostService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<ServiceResult> CreatePostAsync(string userId, string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return ServiceResult.Fail("Post content cannot be empty.");

        if (content.Length > 500)
            return ServiceResult.Fail("Post content cannot exceed 500 characters.");

        var profile = await _db.Set<UserProfile>().FirstOrDefaultAsync(p => p.UserId == userId);
        if (profile == null)
            return ServiceResult.Fail("User profile not found.");

        var post = new ProfilePost
        {
            UserProfileId = profile.Id,
            Content = content.Trim(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.ProfilePosts.Add(post);
        await _db.SaveChangesAsync();

        return ServiceResult.Ok();
    }

    public async Task<IReadOnlyList<ProfilePost>> GetPostsByUserIdAsync(string userId)
    {
        var profile = await _db.Set<UserProfile>().FirstOrDefaultAsync(p => p.UserId == userId);
        if (profile == null)
            return Array.Empty<ProfilePost>();

        return await _db.ProfilePosts
            .Where(p => p.UserProfileId == profile.Id)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<ServiceResult> DeletePostAsync(int postId, string userId)
    {
        var profile = await _db.Set<UserProfile>().FirstOrDefaultAsync(p => p.UserId == userId);
        if (profile == null)
            return ServiceResult.Fail("User profile not found.");

        var post = await _db.ProfilePosts.FirstOrDefaultAsync(p => p.Id == postId);
        if (post == null)
            return ServiceResult.Fail("Post not found.");

        if (post.UserProfileId != profile.Id)
            return ServiceResult.Fail("You can only delete your own posts.");

        _db.ProfilePosts.Remove(post);
        await _db.SaveChangesAsync();

        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> EditPostAsync(int postId, string userId, string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return ServiceResult.Fail("Post content cannot be empty.");

        if (content.Length > 500)
            return ServiceResult.Fail("Post content cannot exceed 500 characters.");

        var profile = await _db.Set<UserProfile>().FirstOrDefaultAsync(p => p.UserId == userId);
        if (profile == null)
            return ServiceResult.Fail("User profile not found.");

        var post = await _db.ProfilePosts.FirstOrDefaultAsync(p => p.Id == postId);
        if (post == null)
            return ServiceResult.Fail("Post not found.");

        if (post.UserProfileId != profile.Id)
            return ServiceResult.Fail("You can only edit your own posts.");

        post.Content = content.Trim();
        post.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return ServiceResult.Ok();
    }
}
