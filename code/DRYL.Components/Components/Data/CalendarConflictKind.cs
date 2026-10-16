namespace DRYL.Components;

/// <summary>Why a <see cref="CalendarConflict"/> was detected. A conflict warns; it never blocks.</summary>
public enum CalendarConflictKind
{
    /// <summary>Another timed event of the same person overlaps the event.</summary>
    Overlap,

    /// <summary>The event overlaps an absence of the person.</summary>
    Absence,

    /// <summary>
    /// Part of the event lies outside the person's working spans, on a day for which
    /// the person has at least one.
    /// </summary>
    OutsideWorkingHours
}
