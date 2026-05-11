using BoredGamers.Models.ViewModels;

namespace BoredGamers.Services.Transfers;

public class TransferResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
}

public interface IGameTransferService
{
    Task<TransferResult> InitiateTransferAsync(string fromUserId, int gameId, string toUsername, CancellationToken ct = default);
    Task<TransferResult> AcceptTransferAsync(string toUserId, int transferId, CancellationToken ct = default);
    Task<TransferResult> DeclineTransferAsync(string toUserId, int transferId, CancellationToken ct = default);
    Task<List<PendingTransferViewModel>> GetPendingTransfersForUserAsync(string userId, CancellationToken ct = default);
}
