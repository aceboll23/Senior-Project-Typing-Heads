namespace Senior_Project_Typing_Heads.BddTests.Support;

public class BddTradeCompleteSeedDataContext
{
    public string SenderUsername { get; set; } = "";
    public string SenderPassword { get; set; } = "";
    public string ReceiverUsername { get; set; } = "";
    public string ReceiverPassword { get; set; } = "";
    public string HasGameUsername { get; set; } = "";
    public string TransferGameName { get; set; } = "";
    public string PendingGameName { get; set; } = "";
    public int PendingTransferId { get; set; }
}
