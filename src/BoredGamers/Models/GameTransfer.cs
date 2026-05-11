namespace BoredGamers.Models;

public enum GameTransferStatus
{
    Pending,
    Accepted,
    Declined
}

public class GameTransfer
{
    public int Id { get; set; }
    public string FromUserId { get; set; } = null!;
    public string ToUserId { get; set; } = null!;
    public int GameId { get; set; }
    public GameTransferStatus Status { get; set; } = GameTransferStatus.Pending;
    public DateTime InitiatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? RespondedAt { get; set; }
    public Game Game { get; set; } = null!;
}
