namespace BoredGamers.Services.Games
{
  public class GameSearchResult
  {
    public int? Id { get; set; }
    public int BggGameId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? YearPublished { get; set; }
    public string? ThumbnailUrl { get; set; }
    public string? ImageUrl { get; set; }
    public int? BggNumVoters { get; set; }
    public decimal? AverageRating { get; set; }
    public bool IsLocal { get; set; }
    public string SourceLabel => IsLocal ? "Local" : "From BoardGameGeek";
  }
}