namespace DRYL.Components;

/// <summary>What kind of change a <see cref="CalendarChangeRequest"/> asks the host for.</summary>
public enum CalendarChangeKind
{
    /// <summary>A new event, made on an empty part of the grid.</summary>
    Create,

    /// <summary>
    /// An event's start changed and its duration was kept. A move across person
    /// columns also changes its people.
    /// </summary>
    Move,

    /// <summary>An event's start or end changed so that its duration changed.</summary>
    Resize
}
