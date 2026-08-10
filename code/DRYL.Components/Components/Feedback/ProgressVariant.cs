namespace DRYL.Components;

/// <summary>Colour treatment of a <see cref="DrylProgress"/> fill.</summary>
public enum ProgressVariant
{
    /// <summary>Violet→cyan accent gradient (default).</summary>
    Accent,
    /// <summary>Green fill — completed / healthy progress.</summary>
    Success,
    /// <summary>Amber fill — near-limit / caution.</summary>
    Warning,
    /// <summary>Red fill — failing / over-limit.</summary>
    Danger
}

/// <summary>Rendered thickness of a <see cref="DrylProgress"/> bar.</summary>
public enum ProgressSize
{
    /// <summary>Thin 4px bar.</summary>
    Small,
    /// <summary>Default 6px bar.</summary>
    Medium,
    /// <summary>Bold 10px bar.</summary>
    Large
}
