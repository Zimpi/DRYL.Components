namespace DRYL.Components.Theming;

/// <summary>
/// Holds the application's current <see cref="DrylTheme"/> and notifies the
/// <c>DrylThemeProvider</c> when it changes so the new seeds can be applied
/// (with the live transition). Registered as scoped by
/// <c>AddDrylComponents()</c>.
/// </summary>
public interface IDrylThemeService
{
    /// <summary>The currently active theme. Starts as <see cref="DrylThemes.Default"/>.</summary>
    DrylTheme Current { get; }

    /// <summary>Switch to a new theme and notify listeners. Animates if motion is allowed.</summary>
    Task SetThemeAsync(DrylTheme theme);

    /// <summary>Convenience: replace only the brand accent, keeping everything else.</summary>
    Task SetAccentAsync(string a, string b);

    /// <summary>Raised after <see cref="Current"/> changes. The provider subscribes to apply it.</summary>
    /// <remarks>
    /// This event is designed for a <strong>single subscriber</strong> — normally one
    /// <c>DrylThemeProvider</c> per Blazor scope. Multicast use (multiple subscribers
    /// registered at the same time) is unsupported and may cause duplicate style
    /// injections or race conditions.
    /// </remarks>
    event Func<Task>? OnThemeChanged;

    /// <summary>The currently chosen color mode. Starts as <see cref="DrylColorMode.System"/>.</summary>
    DrylColorMode CurrentMode { get; }

    /// <summary>Switch the color mode and notify listeners. Animates if motion is allowed.</summary>
    Task SetModeAsync(DrylColorMode mode);

    /// <summary>Raised after <see cref="CurrentMode"/> changes.</summary>
    /// <remarks>
    /// Unlike <see cref="OnThemeChanged"/>, this event supports <strong>multiple
    /// subscribers</strong>: the <c>DrylThemeProvider</c> applies the mode, and any
    /// number of switch UIs (e.g. <c>DrylColorModeToggle</c>) subscribe to re-render.
    /// All delegates are awaited sequentially.
    /// </remarks>
    event Func<Task>? OnModeChanged;
}
