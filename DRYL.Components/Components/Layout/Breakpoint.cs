namespace DRYL.Components;

/// <summary>
/// The fixed DRYL breakpoint scale. The pixel values live only in <c>dryl.css</c>
/// (CSS query conditions cannot read custom properties); this enum is the typed
/// handle consumers pass to responsive parameters such as
/// <see cref="DrylStack.CollapseBelow"/>.
/// </summary>
public enum Breakpoint
{
    /// <summary>480px — phone landscape / small slots.</summary>
    Sm,

    /// <summary>768px — tablet.</summary>
    Md,

    /// <summary>1024px — desktop.</summary>
    Lg,

    /// <summary>1280px — large.</summary>
    Xl
}
