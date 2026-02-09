using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace BoredGamers.Services.Bgg
{
  //Fetches BGG Top ranked games by parsing the BGG browse ranking page.
  //IMPORTANT: This is used during sync/import only (never during page render).
  public class BggClient : IBggClient
  {
    private readonly HttpClient _http;
    private readonly ILogger<BggClient> _logger;

    //BGG Top list source (ranked browse page)
    private const string TopBrowseUrl = "https://boardgamegeek.com/browse/boardgame";

    public BggClient(HttpClient http, ILogger<BggClient> logger)
    {
      _http = http;
      _logger = logger;

      //Helpful defaults
      _http.Timeout = TimeSpan.FromSeconds(30);
      _http.DefaultRequestHeaders.UserAgent.ParseAdd("BoredGamers/1.0 (Senior Project)");
    }

    public async Task<IReadOnlyList<BggTopGame>> GetTopRankedGamesAsync(int limit = 100, CancellationToken ct = default)
    {
      if (limit <= 0) return Array.Empty<BggTopGame>();
      if (limit > 100) limit = 100; //BGG Top page only shows 100 ranked games

      string html;
      try
      {
        html = await _http.GetStringAsync(TopBrowseUrl, ct);
        _logger.LogInformation("Downloaded BGG browse HTML. Length={Length}", html.Length);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Failed to fetch BGG browse page for top ranked games.");
        return Array.Empty<BggTopGame>();
      }

      //Parse ranks and game IDs/names from the HTML.
      //This is intentionally tolerant: HTML may change; if we can't parse, we return empty
      //
      //Typical patterns we rely on:
      // - A rank cell like: <td class="collection_rank">1</td>
      // - A link like: < a href="/boardgame/74430/gloomhaven">Gloomhaven</a>
      //
      //We'll extract (rank, id, name) per row.
      var results = new List<BggTopGame>(capacity: limit);

      //Find row-level chunks to reduce accidental matches.
      var rowRegex = new Regex(@"<tr[^>]*>.*?</tr>", RegexOptions.Singleline | RegexOptions.IgnoreCase);
      var rankRegex = new Regex(
        @"collection_rank[\s\S]*?<span[^>]*>\s*(\d+)\s*</span>",
        RegexOptions.IgnoreCase);
  
      var linkRegex = new Regex(
        @"href=""/boardgame/(\d+)/[^""]*""[^>]*class=""primary""[^>]*>\s*([^<]+)\s*</a>",
        RegexOptions.IgnoreCase);

      
      
      foreach (Match rowMatch in rowRegex.Matches(html))
      {
        var row = rowMatch.Value;

        var rankMatch = rankRegex.Match(row);
        if (!rankMatch.Success) continue;

        if (!int.TryParse(rankMatch.Groups[1].Value, out var rank)) continue;
        if (rank < 1 || rank > limit) continue;

        var linkMatch = linkRegex.Match(row);
        if (!linkMatch.Success) continue;

        if (!int.TryParse(linkMatch.Groups[1].Value, out var bggId)) continue;

        var name = WebUtilityHtmlDecode(linkMatch.Groups[2].Value).Trim();
        if (string.IsNullOrWhiteSpace(name)) continue;

        results.Add(new BggTopGame
        {
          Rank = rank,
          BggGameId = bggId,
          Name = name
        });

        if (results.Count >= limit) break;

      }

      //if Parsing fails (0 results), log once to help debugging
      if (results.Count == 0)
      {
        _logger.LogWarning("Parse 0 top games from BGG browse page. HTML format may have change.");
      }

       _logger.LogInformation("Parsed {Count} games from BGG browse HTML.", results.Count);

      return results;
    }

    //Minimal HTML decode (enough for common entities in names)
    private static string WebUtilityHtmlDecode(string input)
    {
      return input
        .Replace("&amp;", "&")
        .Replace("&quot;", "\"")
        .Replace("&#39;", "'")
        .Replace("&lt;", "<")
        .Replace("&gt;", ">");
    }
    
  }
}