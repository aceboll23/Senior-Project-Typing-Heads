namespace BoredGamers.Services.ContentModeration;

public class ModerationResult
{
    public bool IsFlagged { get; set; }
    public List<string> FlaggedCategories { get; set; } = new();
}

public interface IContentModerationService
{
    Task<ModerationResult> CheckContentAsync(string content, CancellationToken ct = default);
}