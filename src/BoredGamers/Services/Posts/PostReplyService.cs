using BoredGamers.Data;
using BoredGamers.Models;
using BoredGamers.Models.ViewModels;
using BoredGamers.Services;
using Microsoft.EntityFrameworkCore;

namespace BoredGamers.Services.Posts;

public class PostReplyService : IPostReplyService
{
    private readonly ApplicationDbContext _db;

    public PostReplyService(ApplicationDbContext db) => _db = db;

    public async Task<ServiceResult> CreateReplyAsync(int postId, string authorUserId, string content, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(content))
            return ServiceResult.Fail("Reply cannot be empty.");

        if (content.Length > 500)
            return ServiceResult.Fail("Reply cannot exceed 500 characters.");

        var post = await _db.ProfilePosts
            .Include(p => p.UserProfile)
                .ThenInclude(up => up.User)
            .FirstOrDefaultAsync(p => p.Id == postId, ct);
        if (post == null)
            return ServiceResult.Fail("Post not found.");

        var reply = new PostReply
        {
            PostId = postId,
            AuthorId = authorUserId,
            Content = content.Trim(),
            CreatedAt = DateTime.UtcNow
        };
        _db.PostReplies.Add(reply);
        await _db.SaveChangesAsync(ct);

        // Notify post author unless they replied to their own post
        if (post.UserProfile.UserId != authorUserId)
        {
            var replierUsername = await _db.Users
                .Where(u => u.Id == authorUserId)
                .Select(u => u.UserName)
                .FirstOrDefaultAsync(ct) ?? "Someone";

            _db.Set<Notification>().Add(new Notification
            {
                UserProfileId = post.UserProfileId,
                Type = "PostReply",
                Title = "New Reply to Your Post",
                Message = $"{replierUsername} replied to your post.",
                ActionUrl = $"/Profile/Index/{post.UserProfile.User.UserName}",
                RelatedEntityId = reply.Id,
                CreatedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync(ct);
        }

        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> EditReplyAsync(int replyId, string userId, string content, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(content))
            return ServiceResult.Fail("Reply cannot be empty.");

        if (content.Length > 500)
            return ServiceResult.Fail("Reply cannot exceed 500 characters.");

        var reply = await _db.PostReplies.FindAsync(new object[] { replyId }, ct);
        if (reply == null)
            return ServiceResult.Fail("Reply not found.");

        if (reply.AuthorId != userId)
            return ServiceResult.Fail("Not authorized.");

        reply.Content = content.Trim();
        await _db.SaveChangesAsync(ct);
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> DeleteReplyAsync(int replyId, string userId, CancellationToken ct = default)
    {
        var reply = await _db.PostReplies
            .Include(r => r.Post).ThenInclude(p => p.UserProfile)
            .FirstOrDefaultAsync(r => r.Id == replyId, ct);

        if (reply == null)
            return ServiceResult.Fail("Reply not found.");

        var isAuthor = reply.AuthorId == userId;
        var isPostOwner = reply.Post.UserProfile.UserId == userId;

        if (!isAuthor && !isPostOwner)
            return ServiceResult.Fail("Not authorized.");

        _db.PostReplies.Remove(reply);
        await _db.SaveChangesAsync(ct);
        return ServiceResult.Ok();
    }

    public async Task<IReadOnlyList<PostReplyViewModel>> GetRepliesForPostAsync(int postId, CancellationToken ct = default)
    {
        return await _db.PostReplies
            .Where(r => r.PostId == postId)
            .Include(r => r.Author)
                .ThenInclude(u => u.Profile)
            .OrderBy(r => r.CreatedAt)
            .Select(r => new PostReplyViewModel
            {
                Id = r.Id,
                Content = r.Content,
                CreatedAt = r.CreatedAt,
                AuthorUsername = r.Author.UserName ?? "Unknown",
                AuthorAvatarUrl = r.Author.Profile != null ? r.Author.Profile.AvatarUrl : null
            })
            .ToListAsync(ct);
    }
}
