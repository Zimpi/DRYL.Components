using System.Text;

namespace DRYL.Components.Theming;

/// <summary>
/// A complete DRYL theme. A theme only carries <em>seed</em> values — the brand
/// accent, an optional separate AI accent, and optional semantic and
/// chart-palette overrides.
/// Everything else (soft fills, accent lines, glows, the AI aura) is
/// <em>derived</em> from these seeds in <c>dryl.css</c> via <c>color-mix()</c>,
/// so a theme can never drift out of visual coherence.
/// </summary>
public sealed record DrylTheme
{
    /// <summary>The brand accent gradient endpoints. Required.</summary>
    public required DrylAccent Accent { get; init; }

    /// <summary>
    /// An optional accent used only for AI surfaces (aura, indicators). When
    /// <c>null</c>, AI surfaces reuse <see cref="Accent"/> — so AI styling is
    /// unchanged unless a consumer opts in.
    /// </summary>
    public DrylAccent? AiAccent { get; init; }

    /// <summary>Optional semantic status-color overrides.</summary>
    public DrylSemantic? Semantic { get; init; }

    /// <summary>
    /// Optional chart series-palette overrides. When <c>null</c> (the default),
    /// chart series 1/2 are derived from <see cref="Accent"/> in <c>dryl.css</c>
    /// and series 3–6 keep the validated fixed anchors, so charts follow the
    /// theme automatically.
    /// </summary>
    public DrylChartPalette? Charts { get; init; }

    /// <summary>
    /// Emits the theme's seed custom properties as a <c>";"</c>-separated
    /// <c>--key:value;</c> string suitable for an inline <c>:root</c> style or
    /// for <c>document.documentElement.style</c>. Omits AI seeds when
    /// <see cref="AiAccent"/> is <c>null</c> and omits any unset semantic.
    /// </summary>
    internal string ToCssVariables()
    {
        var sb = new StringBuilder();
        Append(sb, "--accent-a", Accent.A);
        Append(sb, "--accent-b", Accent.B);

        if (AiAccent is { } ai)
        {
            Append(sb, "--ai-a", ai.A);
            Append(sb, "--ai-b", ai.B);
        }

        if (Semantic is { } s)
        {
            Append(sb, "--success", s.Success);
            Append(sb, "--warning", s.Warning);
            Append(sb, "--danger", s.Danger);
            Append(sb, "--info", s.Info);
        }

        if (Charts is { } c)
        {
            Append(sb, "--chart-1", c.Series1);
            Append(sb, "--chart-2", c.Series2);
            Append(sb, "--chart-3", c.Series3);
            Append(sb, "--chart-4", c.Series4);
            Append(sb, "--chart-5", c.Series5);
            Append(sb, "--chart-6", c.Series6);
        }

        return sb.ToString();
    }

    private static void Append(StringBuilder sb, string key, string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return;
        sb.Append(key).Append(':').Append(value).Append(';');
    }
}
