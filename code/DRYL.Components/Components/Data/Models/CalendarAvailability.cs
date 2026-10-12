namespace DRYL.Components;

/// <summary>
/// A span of working time or an absence of one person, drawn by a
/// <see cref="DrylCalendar"/> as information behind the events.
/// </summary>
public sealed record CalendarAvailability
{
    /// <summary>The person the span belongs to.</summary>
    public required string PersonId { get; init; }

    /// <summary>When the span starts, as a wall-clock time.</summary>
    public required DateTime Start { get; init; }

    /// <summary>When the span ends, exclusive.</summary>
    public required DateTime End { get; init; }

    /// <summary>Working time or an absence.</summary>
    public CalendarAvailabilityKind Kind { get; init; } = CalendarAvailabilityKind.Working;

    /// <summary>Names an absence, e.g. a holiday.</summary>
    public string? Label { get; init; }
}
