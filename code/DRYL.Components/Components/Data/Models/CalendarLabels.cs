namespace DRYL.Components;

/// <summary>
/// Every text a <see cref="DrylCalendar"/> renders that is not a formatted date or
/// time. Each member defaults to an English text; day names, month names, dates and
/// times follow the calendar's culture instead.
/// </summary>
public sealed record CalendarLabels
{
    /// <summary>The today control.</summary>
    public string Today { get; init; } = "Today";

    /// <summary>Tooltip and accessible name of the previous control.</summary>
    public string Previous { get; init; } = "Previous";

    /// <summary>Tooltip and accessible name of the next control.</summary>
    public string Next { get; init; } = "Next";

    /// <summary>The day view in the view switch.</summary>
    public string Day { get; init; } = "Day";

    /// <summary>The week view in the view switch.</summary>
    public string Week { get; init; } = "Week";

    /// <summary>The month view in the view switch.</summary>
    public string Month { get; init; } = "Month";

    /// <summary>The all-day row, and an all-day event's accessible name.</summary>
    public string AllDay { get; init; } = "All day";

    /// <summary>
    /// The month view's overflow control; <c>{0}</c> is the number of events hidden.
    /// </summary>
    public string More { get; init; } = "+{0} more";

    /// <summary>Header of the column for events without a person.</summary>
    public string Unassigned { get; init; } = "Unassigned";

    /// <summary>Accessible name of the recurring-event mark.</summary>
    public string Recurring { get; init; } = "Recurring";

    /// <summary>Accessible label of the legend.</summary>
    public string People { get; init; } = "People";

    /// <summary>The loading status.</summary>
    public string Loading { get; init; } = "Loading";

    /// <summary>The narrow month view's list for a day without events.</summary>
    public string NoEvents { get; init; } = "No events";
}
