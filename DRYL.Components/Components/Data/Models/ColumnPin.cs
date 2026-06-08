namespace DRYL.Components;

/// <summary>
/// Freezes a <see cref="DrylColumn{TItem}"/> to an edge of <see cref="DrylTable{TItem}"/> so it
/// stays visible while the rest of the table scrolls horizontally.
/// </summary>
public enum ColumnPin
{
    /// <summary>Not pinned — scrolls with the table body. Default.</summary>
    None,

    /// <summary>Pinned to the leading (left) edge. Use for the identity column.</summary>
    Start,

    /// <summary>Pinned to the trailing (right) edge. Use for an actions / total column.</summary>
    End
}
