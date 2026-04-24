using BoredGamers.Data;
using BoredGamers.Models;
using BoredGamers.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace BoredGamers.Services.Posts;

public class ProfilePostService : IProfilePostService
{
    private readonly ApplicationDbContext _db;
    private readonly IWebHostEnvironment _env;

    private static readonly HashSet<string> AllowedExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
    private const long MaxFileSizeBytes = 5 * 1024 * 1024;

    public ProfilePostService(ApplicationDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
    }

    public async Task<ServiceResult> CreatePostAsync(
        string userId,
        string content,
        IFormFile? image = null,
        PostCategory category = PostCategory.None,
        int? gameId = null)
    {
        var hasText = !string.IsNullOrWhiteSpace(content);
        var hasImage = image != null && image.Length > 0;

        if (!hasText && !hasImage)
            return ServiceResult.Fail("Post must include either text or an image.");

        if (hasText && content.Length > 500)
            return ServiceResult.Fail("Post content cannot exceed 500 characters.");

        string? imageUrl = null;
        if (hasImage)
        {
            var ext = Path.GetExtension(image!.FileName);
            if (!AllowedExtensions.Contains(ext))
                return ServiceResult.Fail("Only JPG, PNG, GIF, and WebP images are allowed.");

            if (image.Length > MaxFileSizeBytes)
                return ServiceResult.Fail("Image must be 5MB or smaller.");

            var uploadDir = Path.Combine(_env.WebRootPath, "uploads", "posts");
            Directory.CreateDirectory(uploadDir);

            var fileName = $"{userId}_{Guid.NewGuid()}{ext}";
            var filePath = Path.Combine(uploadDir, fileName);

            await using var stream = new FileStream(filePath, FileMode.Create);
            await image.CopyToAsync(stream);

            imageUrl = $"/uploads/posts/{fileName}";
        }

        var profile = await _db.Set<UserProfile>().FirstOrDefaultAsync(p => p.UserId == userId);
        if (profile == null)
            return ServiceResult.Fail("User profile not found.");

        var post = new ProfilePost
        {
            UserProfileId = profile.Id,
            Content = hasText ? content.Trim() : null,
            ImageUrl = imageUrl,
            Category = category,
            GameId = gameId,
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
            .Include(p => p.Game)
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

        if (!string.IsNullOrEmpty(post.ImageUrl))
        {
            var filePath = Path.Combine(_env.WebRootPath, post.ImageUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(filePath))
                File.Delete(filePath);
        }

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
