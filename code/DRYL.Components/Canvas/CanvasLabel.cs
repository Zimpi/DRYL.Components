using System.Text;
using System.Text.Json;

namespace DRYL.Components.Canvas;

/// <summary>
/// Turns a node into the short, speakable name a human — and a model — recognises it by.
/// Used by the node toolbar, the dock's context chip and every selection announcement, so all
/// three call the same element the same thing.
/// </summary>
public static class CanvasLabel
{
    private const int MaxLength = 60;

    // First non-blank wins. The order follows how the catalog names things: a title beats an
    // inline label, a label beats free text, and a field name is the last thing worth showing.
    private static readonly string[] Sources =
        ["title", "label", "text", "submitLabel", "name", "content"];

    /// <summary>The node's display name, at most 60 characters, never empty.</summary>
    /// <param name="node">The node to name.</param>
    public static string For(CanvasNode node)
    {
        ArgumentNullException.ThrowIfNull(node);

        if (node.Props is { ValueKind: JsonValueKind.Object } props)
        {
            foreach (var source in Sources)
            {
                if (!props.TryGetProperty(source, out var value)) continue;
                if (value.ValueKind != JsonValueKind.String) continue;

                var text = FirstLine(value.GetString());
                if (text.Length > 0) return Truncate(text);
            }
        }

        return TypeName(node.Type);
    }

    private static string FirstLine(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return string.Empty;
        var newline = raw.AsSpan().IndexOfAny('\r', '\n');
        var line = newline < 0 ? raw : raw[..newline];
        return line.Trim();
    }

    private static string Truncate(string text) =>
        text.Length <= MaxLength ? text : text[..(MaxLength - 1)] + "…";

    // "lineChart" → "Line chart", "keyValue" → "Key value", "stat" → "Stat".
    private static string TypeName(string? type)
    {
        if (string.IsNullOrWhiteSpace(type)) return "Element";

        var sb = new StringBuilder(type.Length + 4);
        sb.Append(char.ToUpperInvariant(type[0]));
        foreach (var ch in type.AsSpan(1))
        {
            if (char.IsUpper(ch)) sb.Append(' ').Append(char.ToLowerInvariant(ch));
            else sb.Append(ch);
        }
        return sb.ToString();
    }
}
