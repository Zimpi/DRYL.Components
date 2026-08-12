namespace DRYL.Components;

/// <summary>Controls the resting elevation of a <see cref="DrylAppBar"/>.</summary>
public enum AppBarElevation
{
    /// <summary>Flush with the content — a 1px bottom border only (default).</summary>
    Flat,
    /// <summary>Lifted off the content with <c>var(--shadow-md)</c> and a denser glass tint.</summary>
    Raised
}
