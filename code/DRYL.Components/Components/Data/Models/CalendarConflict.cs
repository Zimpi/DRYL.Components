namespace DRYL.Components;

/// <summary>
/// One conflict of an event, detected by a <see cref="DrylCalendar"/> from its events
/// and availability while it is editable. A conflict warns; it never blocks.
/// </summary>
public sealed record CalendarConflict
{
    /// <summary>The kind of conflict.</summary>
    public required CalendarConflictKind Kind { get; init; }

    /// <summary>The person the conflict is for.</summary>
    public required string PersonId { get; init; }

    /// <summary>
    /// The other event of an <see cref="CalendarConflictKind.Overlap"/>; <c>null</c>
    /// for the other kinds.
    /// </summary>
    public string? EventId { get; init; }
}
