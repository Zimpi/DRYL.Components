namespace DRYL.Components;

/// <summary>A person whose events a <c>DrylCalendar</c> shows.</summary>
public sealed record CalendarPerson
{
    /// <summary>Stable identity of the person.</summary>
    public required string Id { get; init; }

    /// <summary>Shown in the legend, in column headers and in accessible names.</summary>
    public required string Name { get; init; }

    /// <summary>
    /// Image for the avatar; without one the avatar shows initials from
    /// <see cref="Name"/>.
    /// </summary>
    public string? AvatarSrc { get; init; }

    /// <summary>The person's colour slot.</summary>
    public CalendarColor Color { get; init; } = CalendarColor.Auto;
}
