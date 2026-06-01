namespace DRYL.Components;

/// <summary>Presence indicator dot shown on a <see cref="DrylAvatar"/>.</summary>
public enum AvatarStatus
{
    /// <summary>No status dot.</summary>
    None,
    /// <summary>Online / available — green dot.</summary>
    Online,
    /// <summary>Busy / do-not-disturb — red dot.</summary>
    Busy,
    /// <summary>Away / idle — amber dot.</summary>
    Away,
    /// <summary>Offline — muted grey dot.</summary>
    Offline
}
