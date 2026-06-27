namespace DRYL.Components;

/// <summary>
/// The entrance animation a <see cref="DrylReveal"/> plays for its children
/// when they scroll into view. Reuses the fixed motion tokens and is
/// neutralised under <c>prefers-reduced-motion</c>.
/// </summary>
public enum RevealTransition
{
    /// <summary>Opacity only.</summary>
    Fade,

    /// <summary>Opacity plus an upward rise into place.</summary>
    Rise,

    /// <summary>Opacity plus a subtle scale-up into place.</summary>
    ScaleIn
}
