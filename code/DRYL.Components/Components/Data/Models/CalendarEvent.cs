namespace DRYL.Components;

/// <summary>
/// One event of a <see cref="DrylCalendar"/>. Times are wall-clock times in one zone:
/// the calendar reads the date and time of <see cref="Start"/> and <see cref="End"/>
/// and ignores their <see cref="DateTime.Kind"/>.
/// </summary>
public sealed record CalendarEvent
{
    /// <summary>Stable identity of the event.</summary>
    public required string Id { get; init; }

    /// <summary>The event's name.</summary>
    public string Title { get; init; } = "";

    /// <summary>When the event starts, as a wall-clock time.</summary>
    public required DateTime Start { get; init; }

    /// <summary>When the event ends, exclusive.</summary>
    public required DateTime End { get; init; }

    /// <summary>
    /// The event takes whole days; only the dates of <see cref="Start"/> and
    /// <see cref="End"/> count.
    /// </summary>
    public bool AllDay { get; init; }

    /// <summary>The ids of the people the event is assigned to.</summary>
    public IReadOnlyList<string> PersonIds { get; init; } = [];

    /// <summary>The event's colour slot.</summary>
    public CalendarColor Color { get; init; } = CalendarColor.Auto;

    /// <summary>Set when the event is one occurrence of a recurring series.</summary>
    public string? SeriesId { get; init; }

    /// <summary>The host's own object behind the event; the calendar never reads it.</summary>
    public object? Data { get; init; }

    /// <summary>
    /// The event cannot be moved or resized, even when the calendar is editable. It
    /// can still be activated, and still counts when conflicts are detected.
    /// </summary>
    public bool ReadOnly { get; init; }
}
