using Microsoft.AspNetCore.Components;

namespace DRYL.Components;

/// <summary>
/// Shared base for the chart family: sizing, legend policy, value formatting,
/// palette slots and the AI aura lifecycle. Series-agnostic — cartesian charts
/// derive via <see cref="DrylCartesianChartBase"/>, <see cref="DrylDonutChart"/>
/// derives directly.
/// </summary>
public abstract class DrylChartBase : DrylAiAware
{
    /// <summary>Chart height in pixels. Width always fills the container.</summary>
    [Parameter] public int Height { get; set; } = 260;

    /// <summary>
    /// Legend visibility. Default (null) is automatic: shown for two or more
    /// series, hidden for one (the title/context already names a single series).
    /// </summary>
    [Parameter] public bool? ShowLegend { get; set; }

    /// <summary>
    /// .NET format string for axis ticks and tooltip values (e.g. "N0", "C0").
    /// Display values are intentionally culture-aware.
    /// </summary>
    [Parameter] public string? ValueFormat { get; set; }

    /// <summary>Accessible summary label. Defaults to an auto-generated description.</summary>
    [Parameter] public string? AriaLabel { get; set; }

    /// <summary>Extra CSS class(es) merged onto the chart's own classes.</summary>
    [Parameter] public string? Class { get; set; }

    /// <summary>Pass-through HTML attributes on the chart root.</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IDictionary<string, object>? AdditionalAttributes { get; set; }

    /// <summary>Re-key counter for the one-shot Generated wash.</summary>
    protected int GenTick { get; private set; }

    private AiState _prevAi = AiState.None;

    protected override void OnParametersSet()
    {
        // Re-key the wash element each time we transition into Generated so the
        // one-shot animation replays on every completion.
        if (EffectiveAi == AiState.Generated && _prevAi != AiState.Generated) GenTick++;
        _prevAi = EffectiveAi;
    }

    /// <summary>Invariant-culture number for SVG/CSS interpolation.</summary>
    protected static string Inv(double v) => Internal.ChartMath.F(v);

    /// <summary>Culture-aware display value for ticks and tooltips.</summary>
    protected string FormatValue(double v) =>
        ValueFormat is null ? v.ToString("0.##") : v.ToString(ValueFormat);

    /// <summary>
    /// Palette color for a series/segment. <paramref name="slot"/> is the 1-based
    /// pinned slot (wins when set); <paramref name="position"/> is the 0-based
    /// list position. Anything beyond slot 6 renders muted — never cycle colors.
    /// </summary>
    protected static string SlotColor(int? slot, int position)
    {
        var s = slot ?? position + 1;
        return s is >= 1 and <= 6 ? $"var(--chart-{s})" : "var(--fg-dim)";
    }

    /// <summary>Root class string: base + AI aura classes + merged Class.</summary>
    protected string RootCss(string baseClass)
    {
        var classes = new List<string> { baseClass };
        if (EffectiveAi != AiState.None) classes.Add("ai-aura");
        switch (EffectiveAi)
        {
            case AiState.Thinking:  classes.Add("ai-thinking");  break;
            case AiState.Streaming: classes.Add("ai-streaming"); break;
            case AiState.Generated: classes.Add("ai-generated"); break;
        }
        if (!string.IsNullOrWhiteSpace(Class)) classes.Add(Class!);
        return string.Join(' ', classes);
    }
}
