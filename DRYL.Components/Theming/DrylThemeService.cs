namespace DRYL.Components.Theming;

/// <inheritdoc cref="IDrylThemeService"/>
public sealed class DrylThemeService : IDrylThemeService
{
    /// <inheritdoc/>
    public DrylTheme Current { get; private set; } = DrylThemes.Default;

    /// <inheritdoc/>
    public event Func<Task>? OnThemeChanged;

    /// <inheritdoc/>
    public async Task SetThemeAsync(DrylTheme theme)
    {
        ArgumentNullException.ThrowIfNull(theme);
        Current = theme;
        if (OnThemeChanged is { } handler)
            await handler.Invoke();
    }

    /// <inheritdoc/>
    public Task SetAccentAsync(string a, string b) =>
        SetThemeAsync(Current with { Accent = new DrylAccent(a, b) });

    /// <inheritdoc/>
    public DrylColorMode CurrentMode { get; private set; } = DrylColorMode.System;

    /// <inheritdoc/>
    public event Func<Task>? OnModeChanged;

    /// <inheritdoc/>
    public async Task SetModeAsync(DrylColorMode mode)
    {
        if (mode == CurrentMode) return;
        CurrentMode = mode;
        if (OnModeChanged is { } handler)
            await handler.Invoke();
    }
}
