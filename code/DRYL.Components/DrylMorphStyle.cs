namespace DRYL.Components;

/// <summary>
/// How much of the "Depth Glass" vocabulary a morph target gets while it moves.
/// Both tiers travel on the viscous easing (<c>--ease-viscous</c>); only
/// <see cref="DepthGlass"/> pays for the blur pass on the way.
/// </summary>
public enum DrylMorphStyle
{
    /// <summary>Shape and position only — the element glides to its new geometry and its
    /// content is counter-scaled so type never distorts. Cheap enough for high-frequency
    /// interactions such as a table row reorder.</summary>
    Glide,

    /// <summary>The full choreography — the surface passes through translucency and blur
    /// on the way and arrives clear. Reserved for low-frequency, high-meaning moves such
    /// as a card opening into a detail view or a dialog handoff.</summary>
    DepthGlass
}
