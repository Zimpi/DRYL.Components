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
    event Func<Task>? OnThemeChanged;
}
