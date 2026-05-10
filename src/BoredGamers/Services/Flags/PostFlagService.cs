using System;
using System.Threading;
using System.Threading.Tasks;
using BoredGamers.Data;
using BoredGamers.Models;
using Microsoft.EntityFrameworkCore;

namespace BoredGamers.Services.Flags;

public class PostFlagService : IPostFlagService
{
    private readonly ApplicationDbContext _db;

    public PostFlagService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<ServiceResult> FlagPostAsync(string userId, int postId,
        CancellationToken ct = default)
    {
        var postExists = await _db.ProfilePosts
            .AnyAsync(p => p.Id == postId, ct);

        if (!postExists)
            return ServiceResult.Fail("Post not found.");

        var alreadyFlagged = await _db.ProfilePostFlags
            .AnyAsync(f => f.ProfilePostId == postId
                        && f.ReporterId == userId, ct);

        if (alreadyFlagged)
            return ServiceResult.Fail("You have already reported this post.");

        _db.ProfilePostFlags.Add(new ProfilePostFlag
        {
            ProfilePostId = postId,
            ReporterId = userId,
            ReportedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(ct);
        return ServiceResult.Ok();
    }
}
