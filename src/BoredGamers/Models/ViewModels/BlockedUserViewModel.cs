namespace BoredGamers.Models.ViewModels;

public class BlockedUserViewModel
{
    public string Username { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public DateTime BlockedAt { get; set; }
}
