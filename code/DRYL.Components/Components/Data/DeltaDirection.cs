namespace DRYL.Components;

/// <summary>Trend direction of a <see cref="DrylStat"/> delta.</summary>
public enum DeltaDirection
{
    /// <summary>No delta shown.</summary>
    None,
    /// <summary>Positive trend — green, up arrow.</summary>
    Up,
    /// <summary>Negative trend — red, down arrow.</summary>
    Down,
    /// <summary>Flat / unchanged — muted, no arrow.</summary>
    Neutral
}
