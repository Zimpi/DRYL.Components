namespace DRYL.Components;

/// <summary>Direction of a sort on a column.</summary>
public enum SortDirection
{
    /// <summary>Ascending (A→Z, 0→9).</summary>
    Ascending,

    /// <summary>Descending (Z→A, 9→0).</summary>
    Descending
}

/// <summary>
/// A single column-level sort instruction. Multiple descriptors can be combined into
/// a stable multi-sort (first wins on equal values).
/// </summary>
/// <param name="ColumnKey">Stable key of the column (see <see cref="DrylColumn{TItem}.Key"/>).</param>
/// <param name="Direction">Sort direction.</param>
public sealed record SortDescriptor(string ColumnKey, SortDirection Direction);
