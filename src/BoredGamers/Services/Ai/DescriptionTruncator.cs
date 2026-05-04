namespace BoredGamers.Services.Ai;

// Truncates long descriptions for inclusion in AI prompts.
// Cuts at the next sentence boundary (period followed by space) at or after
// the requested character limit. Falls back to a hard char cut if no
// sentence boundary is found in the remainder of the text.
public static class DescriptionTruncator
{
    public static string Truncate(string text, int maxChars)
    {
        if (text is null) return string.Empty;
        if (text.Length <= maxChars) return text;

        var boundary = text.IndexOf(". ", maxChars, System.StringComparison.Ordinal);
        if (boundary >= 0)
            return text.Substring(0, boundary + 1);

        return text.Substring(0, maxChars);
    }
}
