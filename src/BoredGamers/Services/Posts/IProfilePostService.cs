using BoredGamers.Models;
using BoredGamers.Services;

namespace BoredGamers.Services.Posts;

public interface IProfilePostService
{
    Task<ServiceResult> CreatePostAsync(string userId, string content);
    Task<IReadOnlyList<ProfilePost>> GetPostsByUserIdAsync(string userId);
    Task<ServiceResult> DeletePostAsync(int postId, string userId);
    Task<ServiceResult> EditPostAsync(int postId, string userId, string content);
}
