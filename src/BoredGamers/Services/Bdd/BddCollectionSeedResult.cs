namespace BoredGamers.Services.Bdd;

public class BddCollectionSeedResult
{
    public string MemberUsername { get; set; } = "";
    public string MemberPassword { get; set; } = "";
    public string NonMemberUsername { get; set; } = "";
    public string NonMemberPassword { get; set; } = "";
    public string OwnerUsername { get; set; } = "";
    public int CollectionPlaygroupId { get; set; }
    public int EmptyPlaygroupId { get; set; }
    public string CollectionGameName { get; set; } = "";
}