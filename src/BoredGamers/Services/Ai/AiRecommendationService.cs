using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BoredGamers.Data;
using BoredGamers.Models;
using BoredGamers.Services.Bgg;
using BoredGamers.Services.Games;
using Microsoft.EntityFrameworkCore;

namespace BoredGamers.Services.Ai;

public interface IAiRecommendationService
{
    // Legacy signature — still in use by CollectionController until the v2 flow
    // is complete. Will be removed once the controller switches to the new method.
    Task<IReadOnlyList<string>> GetRecommendationsAsync(
        IEnumerable<string> ownedGameNames,
        CancellationToken ct = default);

    // TYP-245: smarter recommendations using collection context. Pulls owned
    // games for the user, builds a rich prompt, and resolves Claude's response
    // to a list of local Game entities (with on-demand BGG promotion for misses).
    Task<IReadOnlyList<Game>> GetRecommendationsAsync(
        string userId,
        CancellationToken ct = default);
}

public class AiRecommendationService : IAiRecommendationService
{
    private const int MaxOwnedGamesInPrompt = 10;
    private const int DescriptionMaxChars = 400;

    private const string SystemPrompt =
        "You are a board game recommendation assistant. The user owns the board " +
        "games listed below; for each game, the player count, play time, and a " +
        "short description are included. Use the player counts, play times, and " +
        "descriptions to identify the themes, group sizes, and session lengths " +
        "the user enjoys, and recommend similar board games. " +
        "Respond with ONLY a list of game names, one per line. " +
        "No numbering, no bullet points, no introduction, no explanation, just " +
        "game names separated by newlines. Recommend up to 8 games.";

    // Legacy system prompt — used by the legacy method only. Will be removed
    // along with the legacy method.
    private const string LegacySystemPrompt =
        "You are a board game recommendation assistant. Given a list of board games " +
        "a user owns, recommend similar board games they might enjoy. " +
        "Respond with ONLY a list of game names, one per line. " +
        "No numbering, no bullet points, no introduction, no explanation, just game names " +
        "separated by newlines. Recommend up to 8 games.";

    private readonly IAiClient _aiClient;
    private readonly ApplicationDbContext _db;
    private readonly IBggClient _bgg;
    private readonly IGameService _games;

    public AiRecommendationService(
        IAiClient aiClient,
        ApplicationDbContext db,
        IBggClient bgg,
        IGameService games)
    {
        _aiClient = aiClient;
        _db = db;
        _bgg = bgg;
        _games = games;
    }

    public async Task<IReadOnlyList<string>> GetRecommendationsAsync(
        IEnumerable<string> ownedGameNames,
        CancellationToken ct = default)
    {
        var userPrompt = BuildLegacyUserPrompt(ownedGameNames);
        var response = await _aiClient.GetCompletionAsync(LegacySystemPrompt, userPrompt, ct);
        return ParseResponse(response);
    }

    public async Task<IReadOnlyList<Game>> GetRecommendationsAsync(
        string userId,
        CancellationToken ct = default)
    {
        var ownedGames = await _db.UserGameCollections
            .AsNoTracking()
            .Where(c => c.UserId == userId && c.Status == CollectionStatus.Owned)
            .OrderByDescending(c => c.DateAdded)
            .Take(MaxOwnedGamesInPrompt)
            .Include(c => c.Game)
            .Select(c => c.Game)
            .ToListAsync(ct);

        if (ownedGames.Count == 0)
            return Array.Empty<Game>();

        var userPrompt = BuildUserPrompt(ownedGames);
        await _aiClient.GetCompletionAsync(SystemPrompt, userPrompt, ct);

        // Orchestration (matching local, BGG promotion, owned-filter, ordering)
        // is implemented in the next step (T6-T9). For now, return empty.
        return Array.Empty<Game>();
    }

    private static string BuildUserPrompt(IEnumerable<Game> ownedGames)
    {
        var sb = new StringBuilder("The user owns these board games:\n");
        foreach (var game in ownedGames)
        {
            sb.AppendLine(FormatGame(game));
        }
        sb.AppendLine();
        sb.AppendLine("Recommend similar board games.");
        return sb.ToString();
    }

    private static string FormatGame(Game game)
    {
        var sb = new StringBuilder("- ").Append(game.Name);

        var hasPlayerCount = game.MinPlayers.HasValue && game.MaxPlayers.HasValue;
        var hasPlayTime = game.PlayTime.HasValue;

        if (hasPlayerCount || hasPlayTime)
        {
            sb.Append(" (");
            if (hasPlayerCount)
            {
                sb.Append(game.MinPlayers!.Value).Append('-').Append(game.MaxPlayers!.Value).Append(" players");
                if (hasPlayTime) sb.Append(", ");
            }
            if (hasPlayTime)
            {
                sb.Append(game.PlayTime!.Value).Append(" min");
            }
            sb.Append(')');
        }

        if (!string.IsNullOrWhiteSpace(game.Description))
        {
            var truncated = DescriptionTruncator.Truncate(game.Description, DescriptionMaxChars);
            sb.Append(": ").Append(truncated);
        }

        return sb.ToString();
    }

    private static string BuildLegacyUserPrompt(IEnumerable<string> ownedGameNames)
    {
        var names = string.Join("\n", ownedGameNames);
        return $"The user owns these board games:\n{names}\n\nRecommend similar board games.";
    }

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
