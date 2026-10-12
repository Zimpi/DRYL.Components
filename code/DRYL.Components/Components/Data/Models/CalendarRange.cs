namespace DRYL.Components;

/// <summary>
/// The span of time a <c>DrylCalendar</c> is showing, raised by
/// <c>OnRangeChanged</c>. Both values are midnights.
/// </summary>
/// <param name="Start">Midnight of the first visible day.</param>
/// <param name="End">Midnight after the last visible day, exclusive.</param>
public readonly record struct CalendarRange(DateTime Start, DateTime End);
