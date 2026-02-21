using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Globalization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BoredGamers.Services.Bgg
{
  //Uses BGG XML API2 
  //-hot (ranked-ish list)
  //-thing (details: year, images, rating)
  //Token-based access (stored in user secrets)
  public class BggClient : IBggClient
  {
    private readonly HttpClient _http;
    private readonly ILogger<BggClient> _logger;

    private const string HotUrl = 
      "https://boardgamegeek.com/xmlapi2/hot?type=boardgame";

    private const string ThingUrlBase = 
      "Https://boardgamegeek.com/xmlapi2/thing?id="; 

    private const string ThingUrlSuffix = 
      "&stats=1";
    //Keep a consistent UA that works well with cloudflare.
    private const string BrowserUserAgent = "Mozilla/5.0";

    public BggClient(HttpClient http, ILogger<BggClient> logger, IConfiguration config)
    {
      _http = http;
      _logger = logger;

      _http.Timeout = TimeSpan.FromSeconds(30);

      // Default headers for ALL requests made by this HttpClient
      _http.DefaultRequestHeaders.UserAgent.ParseAdd("BoredGamers/1.0 (Senior Project)");
      _http.DefaultRequestHeaders.Accept.ParseAdd("application/xml");

      //Add BGG XML API Authorization token
      var token = config["Bgg:ApiToken"];
      if (!string.IsNullOrWhiteSpace(token))
      {
        _http.DefaultRequestHeaders.Authorization =
          new AuthenticationHeaderValue("Bearer", token);
      }
      else
      {
        _logger.LogWarning("BGG API token not found. Set 'Bgg:ApiToken' in user-secrets.");
      }

    }
    public async Task<IReadOnlyList<BggTopGame>> GetTopRankedGamesAsync(int limit = 100, CancellationToken ct = default)
    {
      if (limit <= 0) return Array.Empty<BggTopGame>();
      if (limit > 150) limit = 150; 

      const string BrowseUrlTemplete = "https://boardgamegeek.com/browse/boardgame/page/{0}";

      var pagesNeeded = (int)Math.Ceiling(limit / 100.0);
      if (pagesNeeded < 1) pagesNeeded = 1;

      var results = new List<BggTopGame>(limit);

      for (int page = 1; page <= pagesNeeded; page++)
      {
        ct.ThrowIfCancellationRequested();

        var url = string.Format(BrowseUrlTemplete, page);
        _logger.LogInformation("BGG browse URL: {Url}", url);

        string html;
        try
        {

          if (page > 1)
          {
            await Task.Delay(1000, ct);
          }
          var request = new HttpRequestMessage(HttpMethod.Get, url);
          request.Headers.UserAgent.ParseAdd(BrowserUserAgent);
          request.Headers.Accept.ParseAdd("text/html");

          request.Headers.Referrer = new Uri("https://boardgamegeek.com/browse/boardgame");
          request.Headers.AcceptLanguage.ParseAdd("en-US,en;q=0.9");

          var response = await _http.SendAsync(request, ct);
          response.EnsureSuccessStatusCode();

          html = await response.Content.ReadAsStringAsync(ct);
        }
        catch (Exception ex)
        {
          _logger.LogError(ex, "Failed to fetch BGG browse page {Page}.", page);
          continue; // keep partial results
        }

        try
        {
          var rowRegex = new System.Text.RegularExpressions.Regex(
            @"collection_rank[^>]*>\s*(\d+)\s*<.*?href=""/boardgame/(\d+)/[^""]*""[^>]*>\s*([^<]+)\s*<",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase |
            System.Text.RegularExpressions.RegexOptions.Singleline);

          var matches = rowRegex.Matches(html);

          foreach(System.Text.RegularExpressions.Match m in matches)
          {
            if (!m.Success) continue;
            
            if (!int.TryParse(m.Groups[1].Value, out var rank)) continue;
            if (!int.TryParse(m.Groups[2].Value, out var id)) continue;

            var name = m.Groups[3].Value?.Trim();
            if (string.IsNullOrWhiteSpace(name)) name = "(unknown)";

            //Avoid duplicates across pages
            if (results.Any(r => r.BggGameId == id)) continue;

            results.Add(new BggTopGame
            {
              BggGameId = id,
              Rank = rank,
              Name = name
            });

            if (results.Count >= limit) break;
          }
        }
        catch (Exception ex)
        {
          _logger.LogError(ex, "Failed to parse BGG browse HTML for page {Page}.", page);
        }

        if(results.Count >= limit) break;
      }

      var finalList = results
        .OrderBy(x => x.Rank)
        .Take(limit)
        .ToList();

      _logger.LogInformation("Parsed {Count} games from BGG browse pages.", finalList.Count);
      return finalList;
/*
      string xml;
      try
      {
        var request = new HttpRequestMessage(HttpMethod.Get, HotUrl);
        request.Headers.UserAgent.ParseAdd(BrowserUserAgent);
        request.Headers.Accept.ParseAdd("application/xml");

        var response = await _http.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        xml = await response. Content.ReadAsStringAsync(ct);
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
*/
    }

    public async Task<IReadOnlyDictionary<int, BggGameDetails>> GetGameDetailsAsync(IEnumerable<int> bggGameIds, CancellationToken ct = default)
    {
      var ids = bggGameIds?.Distinct().ToList() ?? new List<int>();
      if (ids.Count == 0) return new Dictionary<int, BggGameDetails>();

      //Avoid giant URLs;
      const int batchSize = 10;

      var results = new Dictionary<int, BggGameDetails>();

      for(int i = 0; i < ids.Count; i += batchSize)
      {
        var batch = ids.Skip(i).Take(batchSize).ToList();
        var url = ThingUrlBase + string.Join(",", batch) + ThingUrlSuffix;

        _logger.LogInformation("Bgg thing URL: {Url}", url);

        string xml;
        try
        {
          var request = new HttpRequestMessage(HttpMethod.Get, url);
          request.Headers.UserAgent.ParseAdd(BrowserUserAgent);
          request.Headers.Accept.ParseAdd("application/xml");

          var response = await _http.SendAsync(request, ct);

          if (!response.IsSuccessStatusCode)
          {
            var body = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError("BGG thing request failed. Status={Status}. BodyStart={BodyStart}",
              (int)response.StatusCode,
              body.Length > 300 ? body.Substring(0,300) : body);
            continue;
          }

          xml = await response.Content.ReadAsStringAsync(ct);
          _logger.LogInformation("Downloaded BGG thing XML batch. Count={Count} Length={Length}", batch.Count, xml.Length);
        }
        catch (Exception ex)
        {
          _logger.LogError(ex, "Failed to fetch BGG thing details batch.");
          continue; // keep partial results
        }

        try
        {
          var doc = XDocument.Parse(xml);

          foreach (var item in doc.Descendants("item"))
          {
            var idAttr = item.Attribute("id")?.Value;
            if (!int.TryParse(idAttr, out var id)) continue;

            //yearpublished: <yearpublished value="" />
            int? year = null;
            var yearStr = item.Element("yearpublished")?.Attribute("value")?.Value;
            if (int.TryParse(yearStr, out var yearParsed)) year = yearParsed;

            //thumbnail/image are elements with text content in SML API2:
            //<thumbnail>https://...</thumbnail>
            //<image>https://...</image>
            var thumb = item.Element("thumbnail")?.Value?.Trim();
            var image = item.Element("image")?.Value?.Trim();

            //average rating: <statistics><ratings><average value="8.5" />
            decimal? avg = null;

            var avgStr = item
              .Descendants("ratings")
              .Descendants("average")
              .FirstOrDefault()
              ?.Attribute("value")
              ?.Value;
            
            if (decimal.TryParse(avgStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var avgParsed))
              avg = avgParsed;

            //MinPlayers
            int? minPlayers = null;
            var minPlayersStr = item.Element("minplayers")?.Attribute("value")?.Value;
            if (int.TryParse(minPlayersStr, out var minPlayersParsed))
              minPlayers = minPlayersParsed;

            //MaxPlayers
            int? maxPlayers = null;
            var maxPlayersStr = item.Element("maxplayers")?.Attribute("value")?.Value;
            if (int.TryParse(maxPlayersStr, out var maxPlayersParsed))
              maxPlayers = maxPlayersParsed;

            //PlayTime
            int? playTime = null;
            var playTimeStr = item.Element("playingtime")?.Attribute("value")?.Value;
            if (int.TryParse(playTimeStr, out var playTimeParsed))
              playTime = playTimeParsed;

            //Description
            var description = item.Element("description")?.Value;
            if (!string.IsNullOrWhiteSpace(description))
            {
              description = System.Net.WebUtility.HtmlDecode(description.Trim());
            }

            results[id] = new BggGameDetails
            {
              BggGameId = id,
              YearPublished = year,
              ThumbnailUrl = string.IsNullOrWhiteSpace(thumb) ? null : thumb,
              ImageUrl = string.IsNullOrWhiteSpace(image) ? null : image,
              AverageRating = avg,
              Description = string.IsNullOrWhiteSpace(description) ? null : description,
              MinPlayers = minPlayers,
              MaxPlayers = maxPlayers,
              PlayTime = playTime
            };
          }
        }
        catch (Exception ex)
        {
          _logger.LogError(ex, "Failed to parse BGG thing XML batch.");
        }
      }

      _logger.LogInformation("Parsed details for {Count} games via BGG thing endpoint.", results.Count);
      return results;
    }
  }
}