namespace DRYL.Components;

/// <summary>
/// How much of the "Depth Glass" view-transition vocabulary a morph target gets.
/// Both tiers glide on the viscous easing (<c>--ease-viscous</c>); only
/// <see cref="DepthGlass"/> pays for the blur/merge filter pass.
/// </summary>
public enum DrylViewTransitionStyle
{
    /// <summary>Viscous easing only — the shape glides, no blur/merge pass. Cheap
    /// enough for high-frequency interactions (table row reorder, list re-sort).</summary>
    Glide,

    /// <summary>Full "Depth Glass" choreography — translucency pulse + mercury
    /// merge filter + decoupled crystalline clarity. Reserved for low-frequency,
    /// high-meaning merges (shared-element morphs such as card→dialog).</summary>
    DepthGlass
}
