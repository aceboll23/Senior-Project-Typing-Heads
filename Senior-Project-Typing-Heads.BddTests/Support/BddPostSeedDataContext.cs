namespace Senior_Project_Typing_Heads.BddTests.Support;

public class BddPostSeedDataContext
{
    public string OwnerUsername { get; set; } = "";
    public string OwnerPassword { get; set; } = "";
    public string FriendUsername { get; set; } = "";
    public string FriendPassword { get; set; } = "";
    public string ExistingPostContent { get; set; } = "";
    public int SeededGameId { get; set; }
    public string SeededGameName { get; set; } = "";
}
