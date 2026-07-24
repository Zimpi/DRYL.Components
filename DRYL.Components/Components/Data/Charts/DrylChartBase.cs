using Microsoft.AspNetCore.Components;
using DRYL.Components.Ai;

namespace DRYL.Components;

/// <summary>
/// Shared base for the chart family: sizing, legend policy, value formatting,
/// palette slots and the AI aura lifecycle. Series-agnostic — cartesian charts
/// derive via <see cref="DrylCartesianChartBase"/>, <see cref="DrylDonutChart"/>
/// derives directly.
/// </summary>
public abstract class DrylChartBase : DrylAiAware, IDisposable
{
    /// <summary>Chart height in pixels. Width always fills the container.</summary>
    [Parameter] public int Height { get; set; } = 260;

    /// <summary>
    /// Legend visibility. Default (null) is automatic: shown for two or more
    /// series, hidden for one (the title/context already names a single series).
    /// </summary>
    [Parameter] public bool? ShowLegend { get; set; }

    /// <summary>
    /// Display format for axis ticks and tooltip values: either a .NET format string
    /// (e.g. "N0", "C0") or a template with a <c>{value}</c> placeholder where the number
    /// goes (e.g. "€{value} Tsd", "{value}%"), optionally with an inner .NET format
    /// ("{value:0.0}"). Display values are intentionally culture-aware.
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

    /// <summary>
    /// Aura mount lifecycle shared by the whole chart family — keeps the ring/glow
    /// mounted for one <c>--dur-slow</c> beat after leaving AI mode so it dissolves
    /// instead of snapping. Bind it from the chart markup:
    /// <c>&lt;DrylAuraElements Aura="AuraFx" GenTick="GenTick" /&gt;</c>.
    /// </summary>
    protected readonly AuraLifecycle AuraFx = new();

    private AiState _prevAi = AiState.None;

    protected override void OnParametersSet()
    {
        // Re-key the wash element each time we transition into Generated so the
        // one-shot animation replays on every completion.
        if (EffectiveAi == AiState.Generated && _prevAi != AiState.Generated) GenTick++;
        _prevAi = EffectiveAi;
        AuraFx.Sync(EffectiveAi, () => InvokeAsync(StateHasChanged));
    }

    public void Dispose() => AuraFx.Dispose();

    /// <summary>Invariant-culture number for SVG/CSS interpolation.</summary>
    protected static string Inv(double v) => Internal.ChartMath.F(v);

    /// <summary>Culture-aware display value for ticks and tooltips — see <see cref="ValueFormat"/>.</summary>
    protected string FormatValue(double v)
    {
        if (ValueFormat is null) return v.ToString("0.##");

        var i = ValueFormat.IndexOf("{value", StringComparison.Ordinal);
        if (i < 0) return v.ToString(ValueFormat);

        var rest = ValueFormat.AsSpan(i + 6);
        var end = rest.IndexOf('}');
        if (end < 0) return v.ToString(ValueFormat);            // no closing brace — not a template

        var inner = "0.##";
        if (end > 0)
        {
            if (rest[0] != ':') return v.ToString(ValueFormat); // "{valueX}" — not our placeholder
            inner = rest.Slice(1, end - 1).ToString();
        }

        return string.Concat(
            ValueFormat.AsSpan(0, i),
            v.ToString(inner),
            ValueFormat.AsSpan(i + 6 + end + 1));
    }

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
        AiAuraCss.Append(classes, AuraFx, EffectiveAura);
        if (!string.IsNullOrWhiteSpace(Class)) classes.Add(Class!);
        return string.Join(' ', classes);
    }
}
