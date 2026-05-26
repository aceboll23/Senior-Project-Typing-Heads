using BoredGamers.Data;
using BoredGamers.Models;
using Microsoft.EntityFrameworkCore;

namespace BoredGamers.Services.Posts;

public class PostLikeService : IPostLikeService
{
    private readonly ApplicationDbContext _db;

    public PostLikeService(ApplicationDbContext db) => _db = db;

    public async Task<(bool isNowLiked, int likeCount)> ToggleLikeAsync(int postId, string userId, CancellationToken ct = default)
    {
        var existing = await _db.PostLikes
            .FirstOrDefaultAsync(l => l.PostId == postId && l.UserId == userId, ct);

        if (existing != null)
        {
            _db.PostLikes.Remove(existing);
        }
        else
        {
            _db.PostLikes.Add(new PostLike
            {
                PostId = postId,
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            });
        }

        await _db.SaveChangesAsync(ct);

        var likeCount = await _db.PostLikes.CountAsync(l => l.PostId == postId, ct);
        return (existing == null, likeCount);
    }

    public async Task<Dictionary<int, int>> GetLikeCountsAsync(IEnumerable<int> postIds, CancellationToken ct = default)
    {
        var ids = postIds.ToList();
        return await _db.PostLikes
            .Where(l => ids.Contains(l.PostId))
            .GroupBy(l => l.PostId)
            .Select(g => new { PostId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.PostId, x => x.Count, ct);
    }

    public async Task<HashSet<int>> GetLikedPostIdsAsync(string userId, IEnumerable<int> postIds, CancellationToken ct = default)
    {
        var ids = postIds.ToList();
        var liked = await _db.PostLikes
            .Where(l => l.UserId == userId && ids.Contains(l.PostId))
            .Select(l => l.PostId)
            .ToListAsync(ct);
        return liked.ToHashSet();
    }
}
