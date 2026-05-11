using BoredGamers.Data;
using BoredGamers.Models;
using BoredGamers.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace BoredGamers.Services.Transfers;

public class GameTransferService : IGameTransferService
{
    private readonly ApplicationDbContext _db;

    public GameTransferService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<TransferResult> InitiateTransferAsync(
        string fromUserId, int gameId, string toUsername, CancellationToken ct = default)
    {
        var senderEntry = await _db.UserGameCollections
            .FirstOrDefaultAsync(c => c.UserId == fromUserId && c.GameId == gameId && c.Status == CollectionStatus.Owned, ct);
        if (senderEntry == null)
            return Fail("You do not own this game.");

        var receiver = await _db.Set<User>()
            .FirstOrDefaultAsync(u => u.UserName == toUsername, ct);
        if (receiver == null)
            return Fail($"User '{toUsername}' not found.");
        if (receiver.Id == fromUserId)
            return Fail("You cannot transfer a game to yourself.");

        var receiverAlreadyOwns = await _db.UserGameCollections
            .AnyAsync(c => c.UserId == receiver.Id && c.GameId == gameId && c.Status == CollectionStatus.Owned, ct);
        if (receiverAlreadyOwns)
            return Fail($"{toUsername} already has this game in their collection.");

        var alreadyPending = await _db.GameTransfers
            .AnyAsync(t => t.FromUserId == fromUserId && t.GameId == gameId && t.Status == GameTransferStatus.Pending, ct);
        if (alreadyPending)
            return Fail("A pending transfer already exists for this game.");

        _db.UserGameCollections.Remove(senderEntry);
        _db.GameTransfers.Add(new GameTransfer
        {
            FromUserId = fromUserId,
            ToUserId = receiver.Id,
            GameId = gameId,
            Status = GameTransferStatus.Pending,
            InitiatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(ct);
        return Ok();
    }

    public async Task<TransferResult> AcceptTransferAsync(
        string toUserId, int transferId, CancellationToken ct = default)
    {
        var transfer = await _db.GameTransfers
            .FirstOrDefaultAsync(t => t.Id == transferId && t.Status == GameTransferStatus.Pending, ct);
        if (transfer == null)
            return Fail("Transfer not found.");
        if (transfer.ToUserId != toUserId)
            return Fail("You are not the recipient of this transfer.");

        var existing = await _db.UserGameCollections
            .FirstOrDefaultAsync(c => c.UserId == toUserId && c.GameId == transfer.GameId, ct);
        if (existing != null)
        {
            existing.Status = CollectionStatus.Owned;
            existing.IsAvailableForTrade = false;
        }
        else
        {
            _db.UserGameCollections.Add(new UserGameCollection
            {
                UserId = toUserId,
                GameId = transfer.GameId,
                DateAdded = DateTime.UtcNow,
                Status = CollectionStatus.Owned,
                IsAvailableForTrade = false
            });
        }

        transfer.Status = GameTransferStatus.Accepted;
        transfer.RespondedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return Ok();
    }

    public async Task<TransferResult> DeclineTransferAsync(
        string toUserId, int transferId, CancellationToken ct = default)
    {
        var transfer = await _db.GameTransfers
            .FirstOrDefaultAsync(t => t.Id == transferId && t.Status == GameTransferStatus.Pending, ct);
        if (transfer == null)
            return Fail("Transfer not found.");
        if (transfer.ToUserId != toUserId)
            return Fail("You are not the recipient of this transfer.");

        transfer.Status = GameTransferStatus.Declined;
        transfer.RespondedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return Ok();
    }

    public async Task<List<PendingTransferViewModel>> GetPendingTransfersForUserAsync(
        string userId, CancellationToken ct = default)
    {
        var transfers = await _db.GameTransfers
            .AsNoTracking()
            .Where(t => t.ToUserId == userId && t.Status == GameTransferStatus.Pending)
            .Include(t => t.Game)
            .OrderByDescending(t => t.InitiatedAt)
            .ToListAsync(ct);

        var senderIds = transfers.Select(t => t.FromUserId).Distinct().ToList();
        var senders = await _db.Set<User>()
            .Where(u => senderIds.Contains(u.Id))
            .Select(u => new { u.Id, u.UserName })
            .ToListAsync(ct);
        var senderLookup = senders.ToDictionary(u => u.Id, u => u.UserName ?? "");

        return transfers.Select(t => new PendingTransferViewModel
        {
            TransferId = t.Id,
            Game = t.Game,
            FromUsername = senderLookup.GetValueOrDefault(t.FromUserId, ""),
            InitiatedAt = t.InitiatedAt
        }).ToList();
    }

    private static TransferResult Ok() => new() { Success = true };
    private static TransferResult Fail(string error) => new() { Success = false, Error = error };
}
