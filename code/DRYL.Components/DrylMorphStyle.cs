namespace DRYL.Components;

/// <summary>
/// How much of the "Depth Glass" vocabulary a morph target gets while it moves.
/// Both tiers travel on the viscous easing (<c>--ease-viscous</c>); only
/// <see cref="DepthGlass"/> pays for the blur pass on the way.
/// </summary>
public enum DrylMorphStyle
{
    /// <summary>Shape and position only — the element glides to its new geometry, and when
    /// it changes size the face it had before travels with it and fades out, so the two
    /// views read as one object growing. A move with no change of size costs nothing but
    /// the move itself, which is why this tier is still cheap enough for high-frequency
    /// interactions such as a table row reorder.</summary>
    Glide,

    /// <summary>The full choreography — the surface passes through translucency and blur
    /// on the way and arrives clear. Reserved for low-frequency, high-meaning moves such
    /// as a card opening into a detail view or a dialog handoff.</summary>
    DepthGlass
}
