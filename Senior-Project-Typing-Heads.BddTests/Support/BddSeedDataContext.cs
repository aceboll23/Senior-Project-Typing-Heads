namespace Senior_Project_Typing_Heads.BddTests.Support;

public class BddSeedDataContext
{
  public int CreateGameId { get; set; }
  public int ExistingReviewGameId { get; set; }
  public string SeededReviewText { get; set; } = "";
  public int GameNightEventId { get; set; }

  // Playgroup collection tests
  public int CollectionPlaygroupId { get; set; }
  public int EmptyPlaygroupId { get; set; }
  public string CollectionGameName { get; set; } = "";
  public string OwnerUsername { get; set; } = "";
}