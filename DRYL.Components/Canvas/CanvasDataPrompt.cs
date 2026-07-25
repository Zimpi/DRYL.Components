using System.Text;

namespace DRYL.Components.Canvas;

/// <summary>
/// Turns the registered data sources into the block an artifact generator sees. The model never
/// gets rows, only descriptors (A3) — which is a privacy argument, a cost argument and, for a
/// line-of-business app, a selling point rather than a detail.
/// </summary>
public static class CanvasDataPrompt
{
    /// <summary>Past this many sources the block starts to dominate every generation; the real
    /// answer is catalog compression (phase 4), so until then this is a warning, not a limit.</summary>
    internal const int CrowdedAt = 40;

    /// <summary>
    /// The block for <paramref name="descriptors"/>, or an empty string when nothing is registered —
    /// in which case the generator's contract stays exactly as it was and existing chat artifacts
    /// keep writing their numbers inline (A2).
    /// </summary>
    public static string Block(IReadOnlyList<CanvasDataDescriptor>? descriptors)
    {
        if (descriptors is null || descriptors.Count == 0) return string.Empty;

        var sb = new StringBuilder();
        sb.Append("\nDATA SOURCES — bind nodes to these instead of writing numbers yourself:\n");
        foreach (var d in descriptors)
        {
            sb.Append("- ").Append(d.Name).Append('(').Append(Signature(d)).Append(") -> ")
              .Append(CanvasDataMapper.ShapeName(d.Shape));
            if (!string.IsNullOrWhiteSpace(d.Description))
                sb.Append(" — \"").Append(d.Description).Append('"');
            sb.Append('\n');
        }
        sb.Append(
            """
            Bind like this: "data": { "source": "<name>", "params": { … }, "refresh": "interval:30s"? }
            A param is a literal, or { "$field": "<name of an interactive node in this artifact>" }.
            "refresh" is "manual" (default, omit it) or "interval:<n>s" with n >= 5.
            Shapes map to types: scalar -> stat|badge|progress, series -> lineChart|areaChart|barChart,
            segments -> donutChart, rows -> table|dataGrid|list|keyValue (keyValue needs exactly 2 columns).
            A bound node still authors its presentation props (title, label, valueFormat) itself.
            Do NOT invent numbers when a matching source exists.

            """);
        return sb.ToString();
    }

    private static string Signature(CanvasDataDescriptor d) =>
        string.Join(", ", d.Params.Select(p => $"{p.Name}{(p.Required ? "" : "?")}: {p.TypeName}"));
}
