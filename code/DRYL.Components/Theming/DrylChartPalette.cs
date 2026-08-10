namespace DRYL.Components.Theming;

/// <summary>
/// Optional overrides for DRYL's categorical chart series palette
/// (<c>--chart-1</c> … <c>--chart-6</c>). Any slot left <c>null</c> keeps the
/// derived/default color for that token: slots 1 and 2 follow the theme's
/// accent seeds (hue from the accent, lightness/chroma normalized into the
/// dark-validated band), slots 3–6 are fixed anchors.
/// </summary>
/// <remarks>
/// Override a slot when a theme's accent hue collides with one of the fixed
/// anchors (e.g. an amber accent next to the amber slot 3) or when a brand
/// requires an exact series palette. Overridden palettes should keep the
/// validated properties: lightness ≈ 0.65 in oklch, chroma ≥ 0.1, adjacent
/// slots distinguishable under CVD.
/// </remarks>
public sealed record DrylChartPalette
{
    /// <summary>Series 1 — maps to <c>--chart-1</c> (default: derived from the accent's A seed).</summary>
    public string? Series1 { get; init; }

    /// <summary>Series 2 — maps to <c>--chart-2</c> (default: derived from the accent's B seed).</summary>
    public string? Series2 { get; init; }

    /// <summary>Series 3 — maps to <c>--chart-3</c> (default: amber <c>#bd7a12</c>).</summary>
    public string? Series3 { get; init; }

    /// <summary>Series 4 — maps to <c>--chart-4</c> (default: green <c>#26a058</c>).</summary>
    public string? Series4 { get; init; }

    /// <summary>Series 5 — maps to <c>--chart-5</c> (default: magenta <c>#d6428e</c>).</summary>
    public string? Series5 { get; init; }

    /// <summary>Series 6 — maps to <c>--chart-6</c> (default: blue <c>#5583e3</c>).</summary>
    public string? Series6 { get; init; }
}
