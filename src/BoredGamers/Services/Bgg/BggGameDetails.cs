namespace BoredGamers.Services.Bgg
{
  //Extra metadata puled from XML API2 /thing
  public class BggGameDetails
  {
    public int BggGameId { get; set; }
    public int? YearPublished { get; set; }
    public string? ThumbnailUrl { get; set; }
    public string? ImageUrl { get; set; }
    public decimal? AverageRating { get; set; }
  }
}