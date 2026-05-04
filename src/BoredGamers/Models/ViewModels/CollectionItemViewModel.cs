namespace BoredGamers.Models.ViewModels;

public class CollectionItemViewModel
{
    public Game Game { get; set; } = null!;
    public bool IsAvailableForTrade { get; set; }
}
