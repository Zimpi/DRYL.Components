namespace DRYL.Components;

/// <summary>
/// The part a column plays in a record while its <see cref="DrylTable{TItem}"/> is compact —
/// below <see cref="DrylTable{TItem}.CompactBelow"/> the table is a list of records rather
/// than a grid, and each record is a small tile built from a few of its columns.
/// </summary>
public enum ColumnCompactRole
{
    /// <summary>
    /// The table decides: the <see cref="DrylColumn{TItem}.Primary"/> column is the title, the
    /// next column the trailing value, the two after that the meta line, and the rest are
    /// shown when the record is unfolded.
    /// </summary>
    Auto,

    /// <summary>The record's title, leading its first line.</summary>
    Title,

    /// <summary>The value at the end of the record's first line, e.g. a status.</summary>
    Trailing,

    /// <summary>A value on the record's second, quieter line. A record shows up to two.</summary>
    Meta,

    /// <summary>Shown only when the record is unfolded, under the column's title.</summary>
    Detail,

    /// <summary>Not shown while the table is compact.</summary>
    Hidden
}
