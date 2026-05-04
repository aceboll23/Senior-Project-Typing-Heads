using BoredGamers.Services.Ai;
using NUnit.Framework;

namespace BoredGamers.Tests.Services.Ai;

[TestFixture]
public class DescriptionTruncatorTests
{
    private const int MaxChars = 400;

    // T1 — Passthrough cases: empty, whitespace, and short text are returned unchanged.
    [TestCase("")]
    [TestCase("   ")]
    [TestCase("This is a short description that fits well within the limit.")]
    public void Truncate_PassthroughCases_ReturnsUnchanged(string input)
    {
        var result = DescriptionTruncator.Truncate(input, MaxChars);

        Assert.That(result, Is.EqualTo(input));
    }

    // T2 — Long text is cut at the next sentence boundary at or after MaxChars.
    // Construction: 500 'a' chars, then ". more text...". The first ". " starts
    // at index 500. The truncator should return everything up to and including
    // that period (501 chars total) — i.e. 500 'a's followed by a single ".".
    [Test]
    public void Truncate_LongTextWithSentenceBoundary_CutsAtNextBoundary()
    {
        var text = new string('a', 500) + ". more text after the boundary.";

        var result = DescriptionTruncator.Truncate(text, MaxChars);

        Assert.That(result, Is.EqualTo(new string('a', 500) + "."));
    }

    // T3 — Long text with no sentence boundary anywhere falls back to a hard
    // character cut at exactly MaxChars. Defensive against pathological input.
    [Test]
    public void Truncate_LongTextWithoutSentenceBoundary_HardCutsAtMaxChars()
    {
        var text = new string('a', 1000);

        var result = DescriptionTruncator.Truncate(text, MaxChars);

        Assert.That(result.Length, Is.EqualTo(MaxChars));
        Assert.That(result, Is.EqualTo(new string('a', MaxChars)));
    }
}
