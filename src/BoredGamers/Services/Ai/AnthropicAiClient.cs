using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Anthropic;
using Anthropic.Models.Messages;
using Microsoft.Extensions.Configuration;

namespace BoredGamers.Services.Ai;

// Real implementation of IAiClient that calls Anthropic's Messages API via the
// official Anthropic NuGet package. SDK details (model id, request shape,
// response parsing) are isolated here so the rest of the app stays SDK-agnostic.
public class AnthropicAiClient : IAiClient
{
    private const string Model = "claude-haiku-4-5-20251001";
    private const int MaxTokens = 1024;

    private readonly AnthropicClient? _client;

    public AnthropicAiClient(IConfiguration config)
    {
        var apiKey = config["Anthropic:ApiKey"];
        if (!string.IsNullOrWhiteSpace(apiKey))
            _client = new AnthropicClient { ApiKey = apiKey };
    }

    public async Task<string> GetCompletionAsync(string systemPrompt, string userPrompt, CancellationToken ct = default)
    {
        if (_client == null)
            return string.Empty;

        var parameters = new MessageCreateParams
        {
            Model = Model,
            MaxTokens = MaxTokens,
            System = systemPrompt,
            Messages =
            [
                new() { Role = Role.User, Content = userPrompt }
            ]
        };

        var message = await _client.Messages.Create(parameters, ct);
        return ExtractText(message);
    }

    // Claude responses come back as a list of content blocks. ContentBlock is a
    // discriminated union (text, thinking, tool-use, etc.) — use TryPickText to
    // pull out the text variant. For a plain prompt with no tools we expect one
    // text block; concatenate any text blocks we find.
    private static string ExtractText(Message message)
    {
        if (message.Content == null) return string.Empty;

        var textParts = new List<string>();
        foreach (var block in message.Content)
        {
            if (block.TryPickText(out var textBlock))
            {
                textParts.Add(textBlock.Text);
            }
        }

        return string.Join("\n", textParts);
    }
}