namespace DRYL.Components;

/// <summary>
/// A change made on the grid of an editable <see cref="DrylCalendar"/>, raised once
/// it is completed. The calendar changes no data: the host applies
/// <see cref="Proposed"/> to its events, or leaves them as they are.
/// </summary>
public sealed record CalendarChangeRequest
{
    /// <summary>What kind of change it is.</summary>
    public required CalendarChangeKind Kind { get; init; }

    /// <summary>
    /// The event the change was made to — the same instance the host supplied;
    /// <c>null</c> for a <see cref="CalendarChangeKind.Create"/>.
    /// </summary>
    public CalendarEvent? Original { get; init; }

    /// <summary>
    /// The event as it would be after the change. For a move or a resize it equals
    /// <see cref="Original"/> in every member except <c>Start</c>, <c>End</c> and
    /// <c>PersonIds</c>, and keeps its <c>Id</c>.
    /// </summary>
    public required CalendarEvent Proposed { get; init; }

    /// <summary>The conflicts <see cref="Proposed"/> would have; empty when it would have none.</summary>
    public IReadOnlyList<CalendarConflict> Conflicts { get; init; } = [];
}
