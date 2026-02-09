using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;

namespace BoredGamers.Services.Bgg
{
  // Fetches BGG Top ranked games by parsing the BGG browse ranking page.
  // IMPORTANT: This is used during sync/import only (never during page render).
  public class BggClient : IBggClient
  {
    private readonly HttpClient _http;
    private readonly ILogger<BggClient> _logger;

    private const string TopBrowseUrl = "https://boardgamegeek.com/browse/boardgame";

    public BggClient(HttpClient http, ILogger<BggClient> logger)
    {
      _http = http;
      _logger = logger;

      _http.Timeout = TimeSpan.FromSeconds(30);
      _http.DefaultRequestHeaders.UserAgent.ParseAdd("BoredGamers/1.0 (Senior Project)");
    }

    public async Task<IReadOnlyList<BggTopGame>> GetTopRankedGamesAsync(int limit = 100, CancellationToken ct = default)
    {
      if (limit <= 0) return Array.Empty<BggTopGame>();
      if (limit > 100) limit = 100;

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

      var doc = new HtmlDocument();
      doc.LoadHtml(html);

      var results = new List<BggTopGame>(capacity: limit);

      // Each game row contains:
      // - <td class="collection_rank"> ... rank ...
      // - <a href="/boardgame/{id}/...">Name</a>
      //
      // We'll iterate rows and extract both pieces.
      var rows = doc.DocumentNode.SelectNodes("//tr");
      if (rows == null)
      {
        _logger.LogWarning("No <tr> rows found in BGG browse HTML.");
        return Array.Empty<BggTopGame>();
      }

      foreach (var row in rows)
      {
        // Rank: grab the td with class containing 'collection_rank'
        var rankNode = row.SelectSingleNode(".//td[contains(@class,'collection_rank')]");
        if (rankNode == null) continue;

        var rankText = HtmlEntity.DeEntitize(rankNode.InnerText).Trim();
        if (!int.TryParse(rankText, out var rank)) continue;
        if (rank < 1 || rank > limit) continue;

        // Link: first boardgame link in the row
        var linkNode = row.SelectSingleNode(".//a[contains(@href,'/boardgame/')]");
        if (linkNode == null) continue;

        var href = linkNode.GetAttributeValue("href", "");
        var name = HtmlEntity.DeEntitize(linkNode.InnerText).Trim();

        if (string.IsNullOrWhiteSpace(href) || string.IsNullOrWhiteSpace(name)) continue;

        // href looks like /boardgame/174430/gloomhaven
        var parts = href.Split('/', StringSplitOptions.RemoveEmptyEntries);
        // parts: ["boardgame", "{id}", "{slug}"]
        if (parts.Length < 2) continue;
        if (!string.Equals(parts[0], "boardgame", StringComparison.OrdinalIgnoreCase)) continue;
        if (!int.TryParse(parts[1], out var bggId)) continue;

        results.Add(new BggTopGame
        {
          Rank = rank,
          BggGameId = bggId,
          Name = name
        });

        if (results.Count >= limit) break;
      }

      if (results.Count == 0)
      _logger.LogWarning("Parsed 0 top games from BGG browse page. HTML format may have changed.");

      _logger.LogInformation("Parsed {Count} games from BGG browse HTML.", results.Count);

      return results;
    }
  }
}