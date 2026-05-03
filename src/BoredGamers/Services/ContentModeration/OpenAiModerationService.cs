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
        // Fail-open if no API key configured (e.g., during local dev without setup)
        var apiKey = _config["OpenAi:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogWarning("OpenAI API key not configured — skipping moderation check.");
            return new ModerationResult { IsFlagged = false };
        }

        if (string.IsNullOrWhiteSpace(content))
            return new ModerationResult { IsFlagged = false };

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
                return new ModerationResult { IsFlagged = false };

            var first = result.Results[0];
            var flaggedCategories = new List<string>();

            if (first.Categories != null)
            {
                foreach (var (category, isFlagged) in first.Categories)
                {
                    if (isFlagged)
                        flaggedCategories.Add(category);
                }
            }

            return new ModerationResult
            {
                IsFlagged = first.Flagged,
                FlaggedCategories = flaggedCategories
            };
        }
        catch (Exception ex)
        {
            // Fail-open — if moderation fails, allow the content through but log it
            _logger.LogError(ex, "Content moderation check failed for content of length {Length}.", content.Length);
            return new ModerationResult { IsFlagged = false };
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