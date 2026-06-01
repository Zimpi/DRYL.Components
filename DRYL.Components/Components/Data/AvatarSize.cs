namespace DRYL.Components;

/// <summary>Rendered diameter of a <see cref="DrylAvatar"/>.</summary>
public enum AvatarSize
{
    /// <summary>Compact 24px avatar — dense lists, inline mentions.</summary>
    Small,
    /// <summary>Default 28px avatar — table rows, menus.</summary>
    Medium,
    /// <summary>Prominent 40px avatar — headers, profile cards, chat messages.</summary>
    Large
}
