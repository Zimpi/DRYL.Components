namespace DRYL.Components;

/// <summary>How a <c>DrylCalendar</c> shows several people at once.</summary>
public enum CalendarPersonLayout
{
    /// <summary>Every person's events share one grid and are told apart by colour. Default.</summary>
    Overlay,

    /// <summary>
    /// A column per person beside a shared time axis. Applies to the day view;
    /// the week and month views stay overlaid.
    /// </summary>
    Columns
}
