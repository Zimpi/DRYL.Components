namespace DRYL.Components.Toasts;

/// <summary>
/// Where the toast stack is anchored on the viewport. Set once on
/// <see cref="DRYL.Components.DrylToastProvider"/>; all toasts share the same anchor.
/// </summary>
public enum ToastPosition
{
    /// <summary>Top, anchored to the right edge. Stacks downwards (newest below).</summary>
    TopRight,

    /// <summary>Top, anchored to the left edge. Stacks downwards (newest below).</summary>
    TopLeft,

    /// <summary>Top, horizontally centered. Stacks downwards (newest below).</summary>
    TopCenter,

    /// <summary>Bottom, anchored to the right edge. Stacks upwards (newest on top).</summary>
    BottomRight,

    /// <summary>Bottom, anchored to the left edge. Stacks upwards (newest on top).</summary>
    BottomLeft,

    /// <summary>Bottom, horizontally centered. Stacks upwards (newest on top).</summary>
    BottomCenter
}
