using System.Text;

namespace DRYL.Components.Agents.Generation;

/// <summary>
/// Turns a partial (mid-stream) JSON buffer into a parseable JSON string by closing
/// open strings and containers and dropping trailing incomplete tokens. Pure and
/// allocation-light; called once per streamed chunk.
/// </summary>
public static class JsonPartialRepair
{
    /// <summary>Return a parseable JSON string derived from <paramref name="partial"/>.</summary>
    public static string Close(string partial)
    {
        if (string.IsNullOrWhiteSpace(partial)) return "null";

        // Try closing the full buffer, then progressively drop the trailing char until it
        // parses. Bounded by the buffer length; in practice succeeds within a few chars.
        for (var len = partial.Length; len > 0; len--)
        {
            var candidate = CloseOnce(partial.AsSpan(0, len).ToString());
            if (IsParseable(candidate)) return candidate;
        }
        return "null";
    }

    private static bool IsParseable(string json)
    {
        try { using var _ = System.Text.Json.JsonDocument.Parse(json); return true; }
        catch (System.Text.Json.JsonException) { return false; }
    }

    private static string CloseOnce(string partial)
    {
        if (string.IsNullOrWhiteSpace(partial)) return "null";

        var stack = new Stack<char>();   // '{' or '['
        var inString = false;
        var escaped = false;
        var lastSignificant = -1;        // index of last non-whitespace char outside strings

        for (var i = 0; i < partial.Length; i++)
        {
            var c = partial[i];
            if (inString)
            {
                if (escaped) escaped = false;
                else if (c == '\\') escaped = true;
                else if (c == '"') inString = false;
                continue;
            }

            switch (c)
            {
                case '"': inString = true; break;
                case '{': case '[': stack.Push(c); break;
                case '}': case ']': if (stack.Count > 0) stack.Pop(); break;
            }
            if (!char.IsWhiteSpace(c)) lastSignificant = i;
        }

        var sb = new StringBuilder();
        if (inString)
        {
            sb.Append(partial);                 // keep partial string content as-is
            if (escaped) sb.Append(' ');        // neutralise a dangling backslash
            sb.Append('"');                      // close the open string
        }
        else
        {
            var end = lastSignificant + 1;
            var trimmed = partial.AsSpan(0, end).ToString();
            trimmed = DropTrailingIncomplete(trimmed);
            sb.Append(trimmed);
        }

        // Close remaining open containers, innermost first.
        foreach (var open in stack)
            sb.Append(open == '{' ? '}' : ']');

        return sb.Length == 0 ? "null" : sb.ToString();
    }

    // Drops a trailing token that can't be closed cleanly:
    //   trailing ',' (before a not-yet-arrived element)        -> drop
    //   trailing ':' (key with no value yet)                   -> drop the key too
    //   trailing incomplete number / literal value (12. / tru) -> drop the value (+ key)
    private static string DropTrailingIncomplete(string s)
    {
        var t = s.TrimEnd();
        if (t.Length == 0) return t;

        var last = t[^1];
        if (last == ',') return t[..^1].TrimEnd();
        if (last == ':') return DropKeyBefore(t.Length - 1, t);

        // Trailing value token (number or bare literal) — drop it if it's incomplete.
        var start = t.Length;
        while (start > 0 && IsValueChar(t[start - 1])) start--;
        if (start < t.Length)
        {
            var token = t[start..];
            var p = start - 1;
            while (p >= 0 && char.IsWhiteSpace(t[p])) p--;
            if (p >= 0 && !IsCompleteValue(token))
            {
                if (t[p] == ':') return DropKeyBefore(p, t);          // object value -> drop key too
                if (t[p] == ',') return t[..p].TrimEnd();             // array element -> drop with comma
                if (t[p] == '[') return t[..(p + 1)];                 // first array element -> keep '['
            }
        }
        return t;
    }

    // Given the index of a ':' (or the value position whose key should go), remove the
    // preceding key string and a dangling comma, returning the trimmed buffer.
    private static string DropKeyBefore(int colonOrValuePos, string t)
    {
        var idx = colonOrValuePos - 1;         // before ':'
        while (idx >= 0 && char.IsWhiteSpace(t[idx])) idx--;
        if (idx >= 0 && t[idx] == '"')
        {
            idx--;                             // skip closing quote of key
            while (idx >= 0 && !(t[idx] == '"' && (idx == 0 || t[idx - 1] != '\\'))) idx--;
            idx--;                             // skip opening quote of key
        }
        while (idx >= 0 && char.IsWhiteSpace(t[idx])) idx--;
        if (idx >= 0 && t[idx] == ',') idx--;
        return t[..(idx + 1)].TrimEnd();
    }

    private static bool IsValueChar(char c) =>
        char.IsLetterOrDigit(c) || c is '.' or '+' or '-';

    private static bool IsCompleteValue(string token) =>
        token is "true" or "false" or "null"
        || (char.IsDigit(token[^1]) &&
            double.TryParse(token, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out _));
}
