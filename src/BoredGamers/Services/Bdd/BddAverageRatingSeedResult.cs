namespace BoredGamers.Services.Bdd;

public class BddAverageRatingSeedResult
{
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public int GameId { get; set; }
    public decimal ExpectedAverage { get; set; }
}
