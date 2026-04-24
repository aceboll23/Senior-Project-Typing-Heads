using BoredGamers.Models;
using BoredGamers.Services;
using Microsoft.AspNetCore.Http;

namespace BoredGamers.Services.Posts;

public interface IProfilePostService
{
    Task<ServiceResult> CreatePostAsync(string userId, string content, IFormFile? image = null, PostCategory category = PostCategory.None, int? gameId = null);
    Task<IReadOnlyList<ProfilePost>> GetPostsByUserIdAsync(string userId);
    Task<ServiceResult> DeletePostAsync(int postId, string userId);
    Task<ServiceResult> EditPostAsync(int postId, string userId, string content);
}
