using BoredGamers.Models.ViewModels;
using BoredGamers.Services;

namespace BoredGamers.Services.Posts;

public interface IPostReplyService
{
    Task<ServiceResult> CreateReplyAsync(int postId, string authorUserId, string content, CancellationToken ct = default);
    Task<IReadOnlyList<PostReplyViewModel>> GetRepliesForPostAsync(int postId, CancellationToken ct = default);
    Task<ServiceResult> EditReplyAsync(int replyId, string userId, string content, CancellationToken ct = default);
    Task<ServiceResult> DeleteReplyAsync(int replyId, string userId, CancellationToken ct = default);
}
