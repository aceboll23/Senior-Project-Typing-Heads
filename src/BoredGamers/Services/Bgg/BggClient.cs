using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;

namespace BoredGamers.Services.Bgg
{
  //Uses BGG XML API2 "hot" endpoint as our ranked source for now
  //No API key required
  public class BggClient : IBggClient
  {
    private readonly HttpClient _http;
    private readonly ILogger<BggClient> _logger;

    private const string HotUrl = "https://api.geekdo.com/xmlapi2/hot?type=boardgame";

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

      string xml;
      try
      {
        xml = await _http.GetStringAsync(HotUrl, ct);
        _logger.LogInformation("Downloaded BGG hot XML. Length={Length}", xml.Length);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Failed to fetch BGG hot list (XML API).");
        return Array.Empty<BggTopGame>();
      }

      try
      {
        var doc = XDocument.Parse(xml);

        //Structure:
        //<items>
        //  <item id="174430" rank="1">
        //    <name value="Gloomhaven" />
        //   ...
        //  </items>
        //</items>
        var items = doc.Descendants("item")
          .Select(x =>
          {
            var idAttr = x.Attribute("id")?.Value;
            var rankAttr = x.Attribute("rank")?.Value;
            var nameVal = x.Element("name")?.Attribute("value")?.Value;

            if (!int.TryParse(idAttr, out var id)) return null;
            if (!int.TryParse(rankAttr, out var rank)) return null;
            if (string.IsNullOrWhiteSpace(nameVal)) nameVal = "(unknown)";

            return new BggTopGame
            {
              BggGameId = id,
              Rank = rank,
              Name = nameVal.Trim()
            };
          })
          .Where(x => x != null)
          .Cast<BggTopGame>()
          .OrderBy(x => x.Rank)
          .Take(limit)
          .ToList();
        
        _logger.LogInformation("Parsed {Count} games from BGG hot XML.", items.Count);
        return items;
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Failed to parse BGG hot XML response.");
        return Array.Empty<BggTopGame>();
      }
    }
  }
}