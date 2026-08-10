namespace DRYL.Components.Theming;

/// <summary>
/// Curated, ready-to-use DRYL themes. Each preset sets only seeds; the rest of
/// the visual language is derived in <c>dryl.css</c>. Build your own by
/// constructing a <see cref="DrylTheme"/> directly.
/// </summary>
public static class DrylThemes
{
    /// <summary>
    /// The signature DRYL look — violet → cyan. Identical to the library's
    /// built-in default, so applying it changes nothing visually.
    /// </summary>
    public static DrylTheme Nebula { get; } = new()
    {
        Accent = new DrylAccent("#7c5cff", "#22d3ee"),
    };

    /// <summary>Warm amber → red. Energetic, product-launch feel.</summary>
    public static DrylTheme Ember { get; } = new()
    {
        Accent = new DrylAccent("#f59e0b", "#f43f5e"),
        // The derived series 1 is amber and would shadow the fixed amber
        // anchor in slot 3 — swap that slot to the default cyan.
        Charts = new DrylChartPalette { Series3 = "#0aa2b5" },
    };

    /// <summary>Green → teal. Calm, "systems healthy" feel.</summary>
    public static DrylTheme Verdant { get; } = new()
    {
        Accent = new DrylAccent("#34d399", "#22d3ee"),
        // The derived series 1 is green and would shadow the fixed green
        // anchor in slot 4 — swap that slot to the default violet.
        Charts = new DrylChartPalette { Series4 = "#8b7cf8" },
    };

    /// <summary>Desaturated, near-monochrome — accent recedes to a cool slate.</summary>
    public static DrylTheme Mono { get; } = new()
    {
        Accent = new DrylAccent("#9aa4b2", "#cbd5e1"),
        // Both slate seeds land on the same near-gray hue — derived series
        // would be indistinguishable. Charts keep the full validated default
        // palette: the chrome recedes, the data stays readable.
        Charts = new DrylChartPalette
        {
            Series1 = "#8b7cf8",
            Series2 = "#0aa2b5",
            Series3 = "#bd7a12",
            Series4 = "#26a058",
            Series5 = "#d6428e",
            Series6 = "#5583e3",
        },
    };

    /// <summary>The default theme when none is supplied. Equal to <see cref="Nebula"/>.</summary>
    public static DrylTheme Default => Nebula;
}
