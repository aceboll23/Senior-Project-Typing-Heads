namespace BoredGamers.Models.ViewModels;

public class PendingTransferViewModel
{
    public int TransferId { get; set; }
    public Game Game { get; set; } = null!;
    public string FromUsername { get; set; } = "";
    public DateTime InitiatedAt { get; set; }
}
