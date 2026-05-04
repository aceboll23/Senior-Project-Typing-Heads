namespace BoredGamers.Services.ContentModeration;

internal static class ProfanityList
{
    private static readonly Lazy<HashSet<string>> _words = new(LoadWords);

    public static HashSet<string> Words => _words.Value;

    private static HashSet<string> LoadWords()
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // The file lives next to the running assembly. In dev that's bin/Debug/...
        // In production it's the deployed app folder. We copy the file there via .csproj.
        var path = Path.Combine(AppContext.BaseDirectory, "ProfanityModeration/profanity.txt");

        if (!File.Exists(path))
        {
            // Fail-open if the file is missing — moderation falls back to AI-only
            return set;
        }

        foreach (var raw in File.ReadAllLines(path))
        {
            var line = raw.Trim();
            if (string.IsNullOrEmpty(line) || line.StartsWith("#"))
                continue;

            set.Add(line);
        }

        return set;
    }
}