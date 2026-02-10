namespace BoredGamers.Services.Bgg
{
  //Represents a single ranked entry from BGG "Top" list.
  //This is a lightweight DTO used during sync before mapping to our Game entity.
  public class BggTopGame
  {
    public int Rank { get; set; } //1...100
    public int BggGameId { get; set; } //stable identity
    public string Name { get; set; } = string.Empty;
  }
}