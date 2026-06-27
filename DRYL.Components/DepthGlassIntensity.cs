namespace DRYL.Components;

/// <summary>
/// How strong the 3D depth warp of a <see cref="DrylDepthGlass"/> surface (or a
/// <see cref="DrylCard"/> with <c>Depth</c> set) is — the maximum tilt angle and
/// the parallax shift of its layers.
/// </summary>
public enum DepthGlassIntensity
{
    /// <summary>Gentle tilt and parallax.</summary>
    Subtle,

    /// <summary>Balanced default.</summary>
    Medium,

    /// <summary>Deep tilt and pronounced parallax.</summary>
    Strong
}
