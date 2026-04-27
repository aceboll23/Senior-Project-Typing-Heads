using System.Threading;
using System.Threading.Tasks;
using BoredGamers.Services.Ai;
using Moq;
using NUnit.Framework;

namespace BoredGamers.Tests.Services.Ai;

[TestFixture]
public class AiRecommendationServiceTests
{
    [Test]
    public async Task GetRecommendationsAsync_PassesOwnedGameNamesIntoUserPrompt()
    {
        // Arrange
        var mockClient = new Mock<IAiClient>();
        string capturedUserPrompt = "";

        mockClient
            .Setup(c => c.GetCompletionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, CancellationToken>((_, user, _) => capturedUserPrompt = user)
            .ReturnsAsync("");

        var service = new AiRecommendationService(mockClient.Object);

        // Act
        await service.GetRecommendationsAsync(new[] { "Catan", "Wingspan" });

        // Assert — both owned game names should appear in what we send to the AI
        Assert.That(capturedUserPrompt, Does.Contain("Catan"));
        Assert.That(capturedUserPrompt, Does.Contain("Wingspan"));
    }

    [Test]
    public async Task GetRecommendationsAsync_ParsesNewlineSeparatedResponseIntoList()
    {
        // Arrange
        var mockClient = new Mock<IAiClient>();
        mockClient
            .Setup(c => c.GetCompletionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Game A\nGame B\nGame C");

        var service = new AiRecommendationService(mockClient.Object);

        // Act
        var result = await service.GetRecommendationsAsync(new[] { "Owned Game" });

        // Assert
        Assert.That(result, Is.EqualTo(new[] { "Game A", "Game B", "Game C" }));
    }

    [Test]
    public async Task GetRecommendationsAsync_TrimsWhitespaceAndDropsBlankLines()
    {
        // Arrange — Claude sometimes adds extra whitespace or stray newlines despite the system prompt
        var mockClient = new Mock<IAiClient>();
        mockClient
            .Setup(c => c.GetCompletionAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("  Game A  \n\n   \nGame B\n");

        var service = new AiRecommendationService(mockClient.Object);

        // Act
        var result = await service.GetRecommendationsAsync(new[] { "Owned" });

        // Assert — leading/trailing whitespace trimmed, blank lines dropped
        Assert.That(result, Is.EqualTo(new[] { "Game A", "Game B" }));
    }
}