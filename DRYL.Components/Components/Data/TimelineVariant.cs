namespace DRYL.Components;

/// <summary>Marker colour treatment of a <see cref="DrylTimelineItem"/>.</summary>
public enum TimelineVariant
{
    /// <summary>Neutral glass marker.</summary>
    Default,
    /// <summary>Accent (violet) marker.</summary>
    Accent,
    /// <summary>Green marker — success / completed.</summary>
    Success,
    /// <summary>Amber marker — warning / pending.</summary>
    Warning,
    /// <summary>Red marker — error / failed.</summary>
    Danger
}
