namespace DRYL.Components;

/// <summary>How a <see cref="DrylCommandPalette"/> orders the groups of its result rows.</summary>
public enum CommandGroupOrder
{
    /// <summary>Groups sorted by name, case-insensitively; rows without a group come last. The default.</summary>
    Alphabetical,

    /// <summary>Groups in the order their first row arrives — provider items first, then
    /// registry commands. Rows without a group form a group of their own at that place.</summary>
    AsProvided,
}
