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
    private const int MaxBggPromotions = 3;
    private const int BggCallTimeoutSeconds = 10;

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
        // Pull the top-N most recent owned games for the prompt context.
        var ownedForPrompt = await _db.UserGameCollections
            .AsNoTracking()
            .Where(c => c.UserId == userId && c.Status == CollectionStatus.Owned)
            .OrderByDescending(c => c.DateAdded)
            .Take(MaxOwnedGamesInPrompt)
            .Include(c => c.Game)
            .Select(c => c.Game)
            .ToListAsync(ct);

        if (ownedForPrompt.Count == 0)
            return Array.Empty<Game>();

        var userPrompt = BuildUserPrompt(ownedForPrompt);
        var response = await _aiClient.GetCompletionAsync(SystemPrompt, userPrompt, ct);
        var recommendedNames = ParseResponse(response);

        if (recommendedNames.Count == 0)
            return Array.Empty<Game>();

        // Pull the FULL set of owned GameIds (not just top 10) — used to
        // exclude games the user already owns from the result.
        var ownedGameIds = (await _db.UserGameCollections
            .AsNoTracking()
            .Where(c => c.UserId == userId && c.Status == CollectionStatus.Owned)
            .Select(c => c.GameId)
            .ToListAsync(ct))
            .ToHashSet();

        // Match recommended names against local Games. SQL Server default
        // collation is case-insensitive; the dictionary lookup below also
        // uses OrdinalIgnoreCase so light case differences from BGG don't
        // cause false misses.
        var localMatches = await _db.Games
            .AsNoTracking()
            .Where(g => recommendedNames.Contains(g.Name))
            .ToListAsync(ct);

        var byName = localMatches.ToDictionary(g => g.Name, StringComparer.OrdinalIgnoreCase);

        // Identify unmatched names, capped at MaxBggPromotions, preserving
        // their original positions in Claude's response so order survives.
        var unmatched = new List<(int index, string name)>();
        for (int i = 0; i < recommendedNames.Count; i++)
        {
            if (!byName.ContainsKey(recommendedNames[i]))
            {
                unmatched.Add((i, recommendedNames[i]));
                if (unmatched.Count >= MaxBggPromotions) break;
            }
        }

        var promotedByIndex = new Dictionary<int, Game>();
        if (unmatched.Count > 0)
        {
            var promoteTasks = unmatched.Select(u => TryPromoteOneAsync(u.name, ct)).ToArray();
            var promoted = await Task.WhenAll(promoteTasks);
            for (int i = 0; i < promoted.Length; i++)
            {
                if (promoted[i] != null)
                    promotedByIndex[unmatched[i].index] = promoted[i]!;
            }
        }

        // Build the final ordered, deduped, owned-filtered result.
        var seen = new HashSet<int>();
        var result = new List<Game>();
        for (int i = 0; i < recommendedNames.Count; i++)
        {
            Game? candidate = null;
            if (byName.TryGetValue(recommendedNames[i], out var localGame))
                candidate = localGame;
            else if (promotedByIndex.TryGetValue(i, out var promotedGame))
                candidate = promotedGame;

            if (candidate != null
                && !ownedGameIds.Contains(candidate.Id)
                && seen.Add(candidate.Id))
            {
                result.Add(candidate);
            }
        }

        return result;
    }

    private async Task<Game?> TryPromoteOneAsync(string name, CancellationToken ct)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(BggCallTimeoutSeconds));
        try
        {
            var hits = await _bgg.SearchGamesAsync(name, 1, timeoutCts.Token);
            var hit = hits.FirstOrDefault();
            if (hit == null) return null;
            return await _games.SaveGameFromBggAsync(hit.BggGameId);
        }
        catch
        {
            // Timeout, network error, BGG miss, EF/SQL race — drop silently.
            return null;
        }
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
