namespace DRYL.Components;

/// <summary>
/// Colour slot of a <see cref="CalendarEvent"/> or a <see cref="CalendarPerson"/>.
/// The slots are the six categorical colours the charts use; there is deliberately
/// no free colour value, so every slot holds its contrast in both colour modes and
/// follows a theme.
/// </summary>
public enum CalendarColor
{
    /// <summary>
    /// Resolved by the calendar: an event takes the colour of its first person,
    /// a person takes a slot by their position. Default.
    /// </summary>
    Auto,

    /// <summary>The first categorical slot (<c>--chart-1</c>).</summary>
    Slot1,

    /// <summary>The second categorical slot (<c>--chart-2</c>).</summary>
    Slot2,

    /// <summary>The third categorical slot (<c>--chart-3</c>).</summary>
    Slot3,

    /// <summary>The fourth categorical slot (<c>--chart-4</c>).</summary>
    Slot4,

    /// <summary>The fifth categorical slot (<c>--chart-5</c>).</summary>
    Slot5,

    /// <summary>The sixth categorical slot (<c>--chart-6</c>).</summary>
    Slot6
}
