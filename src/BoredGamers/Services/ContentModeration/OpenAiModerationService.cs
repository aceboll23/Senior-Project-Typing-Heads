using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BoredGamers.Services.ContentModeration;

public class OpenAiModerationService : IContentModerationService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;
    private readonly ILogger<OpenAiModerationService> _logger;

    public OpenAiModerationService(
        HttpClient httpClient,
        IConfiguration config,
        ILogger<OpenAiModerationService> logger)
    {
        _httpClient = httpClient;
        _config = config;
        _logger = logger;

        _httpClient.BaseAddress = new Uri("https://api.openai.com/");
        _httpClient.Timeout = TimeSpan.FromSeconds(5);

        var apiKey = _config["OpenAi:ApiKey"];
        if (!string.IsNullOrEmpty(apiKey))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
        }
    }

    public async Task<ModerationResult> CheckContentAsync(string content, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(content))
            return new ModerationResult { IsFlagged = false };

        // Profanity word list check (runs first, doesn't need API)
        var profanityCategories = CheckProfanityWordList(content);

        // OpenAI moderation check
        var apiCategories = await CheckOpenAiModerationAsync(content, ct);

        var allCategories = profanityCategories.Concat(apiCategories).Distinct().ToList();

        return new ModerationResult
        {
            IsFlagged = allCategories.Count > 0,
            FlaggedCategories = allCategories
        };
    }

    private List<string> CheckProfanityWordList(string content)
    {
        var flagged = new List<string>();

        // Whole-word regex match, case-insensitive
        foreach (var word in ProfanityList.Words)
        {
            var pattern = $@"\b{System.Text.RegularExpressions.Regex.Escape(word)}\b";
            if (System.Text.RegularExpressions.Regex.IsMatch(content, pattern,
                System.Text.RegularExpressions.RegexOptions.IgnoreCase))
            {
                flagged.Add("profanity");
                break; // Only need to flag once
            }
        }

        return flagged;
    }

    private async Task<List<string>> CheckOpenAiModerationAsync(string content, CancellationToken ct)
    {
        var apiKey = _config["OpenAi:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogWarning("OpenAI API key not configured — skipping AI moderation.");
            return new List<string>();
        }

        try
        {
            var request = new
            {
                model = "omni-moderation-latest",
                input = content
            };

            var response = await _httpClient.PostAsJsonAsync("v1/moderations", request, ct);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<OpenAiModerationResponse>(cancellationToken: ct);
            if (result?.Results == null || result.Results.Count == 0)
                return new List<string>();

            var first = result.Results[0];
            var flaggedCategories = new List<string>();

            if (first.Flagged && first.Categories != null)
            {
                foreach (var (category, isFlagged) in first.Categories)
                {
                    if (isFlagged)
                        flaggedCategories.Add(category);
                }
            }

            return flaggedCategories;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OpenAI moderation check failed for content of length {Length}.", content.Length);
            return new List<string>(); // Fail-open
        }
    }

    private class OpenAiModerationResponse
    {
        [JsonPropertyName("results")]
        public List<OpenAiResult>? Results { get; set; }
    }

    private class OpenAiResult
    {
        [JsonPropertyName("flagged")]
        public bool Flagged { get; set; }

        [JsonPropertyName("categories")]
        public Dictionary<string, bool>? Categories { get; set; }
    }
}