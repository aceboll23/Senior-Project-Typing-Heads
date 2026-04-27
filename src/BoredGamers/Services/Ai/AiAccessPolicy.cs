using System;
using System.Collections.Generic;

namespace BoredGamers.Services.Ai;

// Hardcoded allowlist of usernames permitted to call the AI recommendations
// endpoint. Each call costs real money against the Anthropic API key, so during
// the minimal/demo phase we restrict access to the dev team.
//
// Both the controller and the Razor view consult this so the UI and server stay
// in sync (button hidden for non-allowlisted users; endpoint returns Forbid()
// if hit directly).
//
// To expand access later: add usernames to the set, or move the list to
// IConfiguration so it can be changed without a redeploy.
public static class AiAccessPolicy
{
    private static readonly HashSet<string> AllowedUsernames = new(StringComparer.OrdinalIgnoreCase)
    {
        "PersonThree",
        "TODO_IAN",
        "TODO_ADLER"
    };

    public static bool IsAllowed(string? username)
        => !string.IsNullOrWhiteSpace(username) && AllowedUsernames.Contains(username);
}