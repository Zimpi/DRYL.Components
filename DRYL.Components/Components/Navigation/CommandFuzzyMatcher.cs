namespace DRYL.Components;

/// <summary>Ranks <see cref="DrylCommand"/>s against a query: case-insensitive substring/subsequence
/// scoring across title, keywords and description, with destructive commands de-prioritised on ties.
/// An empty query returns all commands (capped). Disabled commands are kept (the palette greys them).</summary>
internal static class CommandFuzzyMatcher
{
    public static IReadOnlyList<DrylCommand> Match(
        IReadOnlyList<DrylCommand> commands, string query, int max)
    {
        if (string.IsNullOrWhiteSpace(query))
            return commands.Take(max).ToList();

        var q = query.Trim();
        var scored = new List<(DrylCommand cmd, int score)>();
        foreach (var c in commands)
        {
            var best = Score(c.Title, q, 100);
            best = Math.Max(best, Score(c.Description, q, 40));
            if (c.Keywords is not null)
                foreach (var k in c.Keywords)
                    best = Math.Max(best, Score(k, q, 60));
            if (best > 0)
            {
                if (c.Destructive) best -= 1; // tie-break only
                scored.Add((c, best));
            }
        }

        return scored
            .OrderByDescending(s => s.score)
            .ThenBy(s => s.cmd.Title, StringComparer.OrdinalIgnoreCase)
            .Take(max)
            .Select(s => s.cmd)
            .ToList();
    }

    // Substring hit scores highest (weight), then subsequence; 0 = no match.
    private static int Score(string? haystack, string needle, int weight)
    {
        if (string.IsNullOrEmpty(haystack)) return 0;
        var h = haystack.ToLowerInvariant();
        var n = needle.ToLowerInvariant();
        var idx = h.IndexOf(n, StringComparison.Ordinal);
        if (idx == 0) return weight + 5;       // prefix
        if (idx > 0) return weight;            // substring
        return IsSubsequence(h, n) ? weight / 2 : 0;
    }

    private static bool IsSubsequence(string h, string n)
    {
        var i = 0;
        foreach (var ch in h)
            if (i < n.Length && ch == n[i]) i++;
        return i == n.Length;
    }
}
