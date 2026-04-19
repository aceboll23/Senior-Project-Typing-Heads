using BoredGamers.Models.ViewModels;
using BoredGamers.Services;

namespace BoredGamers.Services.Block;

public interface IBlockService
{
    Task<ServiceResult> BlockUserAsync(string blockerUserId, string targetUsername);
    Task<ServiceResult> UnblockUserAsync(string blockerUserId, string targetUsername);
    /// <summary>Returns true if the profile owner (ownerUserId) has blocked the viewer (viewerUserId).</summary>
    Task<bool> IsBlockedByAsync(string viewerUserId, string ownerUserId);
    Task<IReadOnlyList<BlockedUserViewModel>> GetBlockedUsersAsync(string userId);
}
