namespace DRYL.Components;

/// <summary>
/// How pronounced the refraction / blur of a <see cref="DrylLiquidGlass"/>
/// surface is. Higher intensity displaces the edge sheen more and frosts the
/// backdrop harder.
/// </summary>
public enum LiquidGlassIntensity
{
    /// <summary>Barely-there warp — closest to the standard glass surface.</summary>
    Subtle,

    /// <summary>Balanced default.</summary>
    Medium,

    /// <summary>Heavy refraction and frost — the most "liquid" look.</summary>
    Strong
}
