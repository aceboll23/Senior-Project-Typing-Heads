namespace Senior_Project_Typing_Heads.BddTests.Support;

public class BddChatSeedDataContext
{
    public string OwnerUsername { get; set; } = "";
    public string OwnerPassword { get; set; } = "";
    public string MemberUsername { get; set; } = "";
    public string MemberPassword { get; set; } = "";
    public string OutsiderUsername { get; set; } = "";
    public string OutsiderPassword { get; set; } = "";
    public int PlaygroupId { get; set; }
    public string PlaygroupName { get; set; } = "";
}
