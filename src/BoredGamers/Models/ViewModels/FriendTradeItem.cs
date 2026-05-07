namespace BoredGamers.Models.ViewModels;

public class FriendTradeItem
{
    public Game Game { get; set; } = null!;
    public string OwnerUserId { get; set; } = "";
    public string OwnerUsername { get; set; } = "";
    public DateTime DateAdded { get; set; }
}
