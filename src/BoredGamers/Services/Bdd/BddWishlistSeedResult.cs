namespace BoredGamers.Services.Bdd;

public class BddWishlistSeedResult
{
  public string Username { get; set; } = "";
  public string Password { get; set; } = "";
  public int GameNotOnWishlistId { get; set; }
  public int GameAlreadyOnWishlistId { get; set; }
  public string GameNotOnWishlistName { get; set; } = "";
  public string GameAlreadyOnWishlistName { get; set; } = "";
}