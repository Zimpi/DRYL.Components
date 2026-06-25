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

    // Drops a trailing structural token that can't be closed cleanly:
    //   trailing ',' (before a not-yet-arrived element)  -> drop
    //   trailing ':' (key with no value yet)             -> drop the key too
    private static string DropTrailingIncomplete(string s)
    {
        var t = s.TrimEnd();
        if (t.Length == 0) return t;

        var last = t[^1];
        if (last == ',') return t[..^1].TrimEnd();
        if (last == ':')
        {
            var idx = t.Length - 1;            // at ':'
            idx--;                              // before ':'
            while (idx >= 0 && char.IsWhiteSpace(t[idx])) idx--;
            if (idx >= 0 && t[idx] == '"')
            {
                idx--;                         // skip closing quote of key
                while (idx >= 0 && !(t[idx] == '"' && t[idx - 1] != '\\')) idx--;
                idx--;                         // skip opening quote of key
            }
            while (idx >= 0 && char.IsWhiteSpace(t[idx])) idx--;
            if (idx >= 0 && t[idx] == ',') idx--;
            return t[..(idx + 1)].TrimEnd();
        }
        return t;
    }
}
