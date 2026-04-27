using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BoredGamers.Services.Ai;

public interface IAiRecommendationService
{
    Task<IReadOnlyList<string>> GetRecommendationsAsync(
        IEnumerable<string> ownedGameNames,
        CancellationToken ct = default);
}

// Builds the prompts, calls the AI, and parses the response into a flat list of
// recommended game names. Returns just the names — the controller is responsible
// for looking each one up in the local Games table to render full cards.
//
// Depends on IAiClient (not the SDK directly) so unit tests can substitute a
// mock that returns canned responses.
public class AiRecommendationService : IAiRecommendationService
{
    // Instructions for Claude. We ask for a strict newline-separated list with
    // nothing else so the parser can stay simple.
    private const string SystemPrompt =
        "You are a board game recommendation assistant. Given a list of board games " +
        "a user owns, recommend similar board games they might enjoy. " +
        "Respond with ONLY a list of game names, one per line. " +
        "No numbering, no bullet points, no introduction, no explanation, just game names " +
        "separated by newlines. Recommend up to 8 games.";

    private readonly IAiClient _aiClient;

    public AiRecommendationService(IAiClient aiClient)
    {
        _aiClient = aiClient;
    }

    public async Task<IReadOnlyList<string>> GetRecommendationsAsync(
        IEnumerable<string> ownedGameNames,
        CancellationToken ct = default)
    {
        var userPrompt = BuildUserPrompt(ownedGameNames);
        var response = await _aiClient.GetCompletionAsync(SystemPrompt, userPrompt, ct);
        return ParseResponse(response);
    }

    private static string BuildUserPrompt(IEnumerable<string> ownedGameNames)
    {
        var names = string.Join("\n", ownedGameNames);
        return $"The user owns these board games:\n{names}\n\nRecommend similar board games.";
    }

    // Defensive parsing: split on newlines, trim each line, drop blanks. If
    // Claude prepends "Here are some recommendations:" or similar despite the
    // system prompt, those non-name lines just won't match anything in the
    // local Games table and will be silently dropped at the controller layer.
    private static IReadOnlyList<string> ParseResponse(string response)
    {
        if (string.IsNullOrWhiteSpace(response))
            return Array.Empty<string>();

        return response
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Trim())
            .Where(line => line.Length > 0)
            .ToList();
    }
}