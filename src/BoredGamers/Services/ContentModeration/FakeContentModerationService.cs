namespace BoredGamers.Services.ContentModeration;

/// <summary>
/// Test-only moderation service that flags content containing a known marker phrase.
/// Used when the app runs in BDD/test mode so tests don't depend on a live API.
/// </summary>
public class FakeContentModerationService : IContentModerationService
{
    // Any content containing this marker is flagged. Keep it obscure so real users
    // would never type it accidentally.
    public const string FlagMarker = "BDD_FLAG_THIS";

    public Task<ModerationResult> CheckContentAsync(string content, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(content))
            return Task.FromResult(new ModerationResult { IsFlagged = false });

        var flagged = content.Contains(FlagMarker, StringComparison.OrdinalIgnoreCase);
        return Task.FromResult(new ModerationResult
        {
            IsFlagged = flagged,
            FlaggedCategories = flagged ? new List<string> { "test" } : new List<string>()
        });
    }
}