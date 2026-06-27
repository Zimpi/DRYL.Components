namespace DRYL.Components;

/// <summary>
/// The enter/exit animation a <see cref="DrylPresence"/> plays when its child
/// mounts and unmounts. All variants reuse the fixed motion tokens and are
/// neutralised under <c>prefers-reduced-motion</c>.
/// </summary>
public enum PresenceTransition
{
    /// <summary>Opacity only.</summary>
    Fade,

    /// <summary>Opacity plus a subtle scale (grows in, shrinks out).</summary>
    Scale,

    /// <summary>Slides up on enter, down on exit.</summary>
    SlideUp,

    /// <summary>Slides down on enter, up on exit.</summary>
    SlideDown,

    /// <summary>Slides in from the right, out to the right.</summary>
    SlideLeft,

    /// <summary>Slides in from the left, out to the left.</summary>
    SlideRight
}
