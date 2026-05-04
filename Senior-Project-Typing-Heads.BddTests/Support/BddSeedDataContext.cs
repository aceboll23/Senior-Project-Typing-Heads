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
  // Add these properties
  public int VotingEventId { get; set; }
  public int VotingEventGameId { get; set; }
  public string VotingGameName { get; set; } = "";
  public string VotingCreatorUsername { get; set; } = "";
  public string VotingCreatorPassword { get; set; } = "";
  public string VotingMemberUsername { get; set; } = "";
  public string VotingMemberPassword { get; set; } = "";
  public string ModerationUsername { get; set; } = "";
  public string ModerationPassword { get; set; } = "";
}