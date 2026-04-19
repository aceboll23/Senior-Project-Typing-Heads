using BoredGamers.Data;
using BoredGamers.Models;
using BoredGamers.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace BoredGamers.Services.Block;

public class BlockService : IBlockService
{
    private readonly ApplicationDbContext _db;

    public BlockService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<ServiceResult> BlockUserAsync(string blockerUserId, string targetUsername)
    {
        var blockerProfile = await _db.Set<UserProfile>().FirstOrDefaultAsync(p => p.UserId == blockerUserId);
        if (blockerProfile == null)
            return ServiceResult.Fail("Blocker profile not found.");

        var targetProfile = await _db.Set<UserProfile>()
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.User.UserName == targetUsername);
        if (targetProfile == null)
            return ServiceResult.Fail("User not found.");

        if (targetProfile.UserId == blockerUserId)
            return ServiceResult.Fail("Cannot block yourself.");

        // Idempotent — already blocked is a no-op
        var alreadyBlocked = await _db.Set<BlockedUser>().AnyAsync(b =>
            b.BlockerProfileId == blockerProfile.Id && b.BlockedProfileId == targetProfile.Id);
        if (alreadyBlocked)
            return ServiceResult.Ok();

        // Remove any existing friendship in either direction
        var friendship = await _db.Set<Friendship>().FirstOrDefaultAsync(f =>
            (f.RequesterProfileId == blockerProfile.Id && f.ReceiverProfileId == targetProfile.Id) ||
            (f.RequesterProfileId == targetProfile.Id && f.ReceiverProfileId == blockerProfile.Id));
        if (friendship != null)
            _db.Set<Friendship>().Remove(friendship);

        _db.Set<BlockedUser>().Add(new BlockedUser
        {
            BlockerProfileId = blockerProfile.Id,
            BlockedProfileId = targetProfile.Id,
            BlockedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> UnblockUserAsync(string blockerUserId, string targetUsername)
    {
        var blockerProfile = await _db.Set<UserProfile>().FirstOrDefaultAsync(p => p.UserId == blockerUserId);
        if (blockerProfile == null)
            return ServiceResult.Fail("Blocker profile not found.");

        var targetProfile = await _db.Set<UserProfile>()
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.User.UserName == targetUsername);
        if (targetProfile == null)
            return ServiceResult.Fail("User not found.");

        var block = await _db.Set<BlockedUser>().FirstOrDefaultAsync(b =>
            b.BlockerProfileId == blockerProfile.Id && b.BlockedProfileId == targetProfile.Id);
        if (block == null)
            return ServiceResult.Fail("User is not blocked.");

        _db.Set<BlockedUser>().Remove(block);
        await _db.SaveChangesAsync();
        return ServiceResult.Ok();
    }

    public async Task<bool> IsBlockedByAsync(string viewerUserId, string ownerUserId)
    {
        var viewerProfile = await _db.Set<UserProfile>().FirstOrDefaultAsync(p => p.UserId == viewerUserId);
        var ownerProfile = await _db.Set<UserProfile>().FirstOrDefaultAsync(p => p.UserId == ownerUserId);

        if (viewerProfile == null || ownerProfile == null) return false;

        return await _db.Set<BlockedUser>().AnyAsync(b =>
            b.BlockerProfileId == ownerProfile.Id && b.BlockedProfileId == viewerProfile.Id);
    }

    public async Task<IReadOnlyList<BlockedUserViewModel>> GetBlockedUsersAsync(string userId)
    {
        var profile = await _db.Set<UserProfile>().FirstOrDefaultAsync(p => p.UserId == userId);
        if (profile == null) return Array.Empty<BlockedUserViewModel>();

        return await _db.Set<BlockedUser>()
            .Where(b => b.BlockerProfileId == profile.Id)
            .Include(b => b.BlockedProfile)
                .ThenInclude(p => p.User)
            .OrderByDescending(b => b.BlockedAt)
            .Select(b => new BlockedUserViewModel
            {
                Username = b.BlockedProfile.User.UserName ?? "Unknown",
                AvatarUrl = b.BlockedProfile.AvatarUrl,
                BlockedAt = b.BlockedAt
            })
            .ToListAsync();
    }
}
