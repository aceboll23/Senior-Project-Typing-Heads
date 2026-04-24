namespace BoredGamers.Models.ViewModels;

public class SocialFeedPostViewModel
{
    public int PostId { get; set; }
    public string? Content { get; set; }
    public string? ImageUrl { get; set; }
    public PostCategory Category { get; set; }
    public string? GameName { get; set; }
    public DateTime CreatedAt { get; set; }
    public string AuthorUsername { get; set; } = string.Empty;
    public string? AuthorAvatarUrl { get; set; }
}
