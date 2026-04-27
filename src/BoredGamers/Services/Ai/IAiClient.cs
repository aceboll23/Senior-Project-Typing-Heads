using System.Threading;
using System.Threading.Tasks;

namespace BoredGamers.Services.Ai;

// Thin contract over whatever AI provider we end up using. Lets the recommendation
// service depend on this interface (easily mockable in tests) instead of the SDK
// directly.
public interface IAiClient
{
    Task<string> GetCompletionAsync(string systemPrompt, string userPrompt, CancellationToken ct = default);
}