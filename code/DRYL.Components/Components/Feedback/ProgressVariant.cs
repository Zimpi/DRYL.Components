namespace DRYL.Components;

/// <summary>Colour treatment of a <see cref="DrylProgress"/> fill.</summary>
public enum ProgressVariant
{
    /// <summary>Violet→cyan accent gradient across the cells (default).</summary>
    Accent,
    /// <summary>Green gradient — completed / healthy progress.</summary>
    Success,
    /// <summary>Amber-to-orange gradient — near-limit / caution.</summary>
    Warning,
    /// <summary>Red-to-rose gradient — failing / over-limit.</summary>
    Danger
}

/// <summary>Rendered thickness of a <see cref="DrylProgress"/> row of cells.</summary>
public enum ProgressSize
{
    /// <summary>Thin 4px bar.</summary>
    Small,
    /// <summary>Default 6px bar.</summary>
    Medium,
    /// <summary>Bold 10px bar.</summary>
    Large
}
