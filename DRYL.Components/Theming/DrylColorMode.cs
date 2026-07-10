namespace DRYL.Components.Theming;

/// <summary>
/// The color mode DRYL renders in. <see cref="System"/> (the default) follows
/// the operating-system preference via <c>prefers-color-scheme</c>;
/// <see cref="Dark"/> and <see cref="Light"/> force a mode explicitly.
/// </summary>
public enum DrylColorMode
{
    /// <summary>Follow the operating-system preference (default).</summary>
    System,
    /// <summary>Force the dark rendition.</summary>
    Dark,
    /// <summary>Force the light rendition.</summary>
    Light,
}
