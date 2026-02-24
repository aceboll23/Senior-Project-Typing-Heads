namespace BoredGamers.Services.Bgg
{
  //Extra metadata puled from XML API2 /thing
  public class BggGameDetails
  {
    public string? Name { get; set;}
    public int BggGameId { get; set; }
    public int? YearPublished { get; set; }
    public string? ThumbnailUrl { get; set; }
    public string? ImageUrl { get; set; }
    public decimal? AverageRating { get; set; }
    public int? UsersRated { get; set; }
    public string? Description { get; set; }
    public int? MinPlayers { get; set; }
    public int? MaxPlayers { get; set; }
    public int? PlayTime { get; set; }
  }
}