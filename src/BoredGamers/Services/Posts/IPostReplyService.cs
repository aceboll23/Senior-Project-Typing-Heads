using BoredGamers.Models.ViewModels;
using BoredGamers.Services;

namespace BoredGamers.Services.Posts;

public interface IPostReplyService
{
    Task<ServiceResult> CreateReplyAsync(int postId, string authorUserId, string content, CancellationToken ct = default);
    Task<IReadOnlyList<PostReplyViewModel>> GetRepliesForPostAsync(int postId, CancellationToken ct = default);
}
