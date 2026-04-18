namespace BoredGamers.Services.Bdd;

public class BddVotingSeedResult
{
    public string CreatorUsername { get; set; } = "";
    public string CreatorPassword { get; set; } = "";
    public string MemberUsername { get; set; } = "";
    public string MemberPassword { get; set; } = "";
    public int EventId { get; set; }
    public int EventGameId { get; set; }
    public string GameName { get; set; } = "";
}