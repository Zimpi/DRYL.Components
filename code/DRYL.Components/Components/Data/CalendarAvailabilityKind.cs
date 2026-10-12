namespace DRYL.Components;

/// <summary>What a <see cref="CalendarAvailability"/> span states about its person.</summary>
public enum CalendarAvailabilityKind
{
    /// <summary>A span in which the person works. Default.</summary>
    Working,

    /// <summary>A span in which the person is away.</summary>
    Absent
}
