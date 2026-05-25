namespace BoredGamers.Services.Posts;

public interface IPostLikeService
{
    Task<(bool isNowLiked, int likeCount)> ToggleLikeAsync(int postId, string userId, CancellationToken ct = default);
    Task<Dictionary<int, int>> GetLikeCountsAsync(IEnumerable<int> postIds, CancellationToken ct = default);
    Task<HashSet<int>> GetLikedPostIdsAsync(string userId, IEnumerable<int> postIds, CancellationToken ct = default);
}
