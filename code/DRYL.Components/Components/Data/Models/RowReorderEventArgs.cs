namespace DRYL.Components;

/// <summary>
/// Describes a row-reorder operation raised by <see cref="DrylTable{TItem}"/> when the user
/// drags a row to a new position or moves it with the keyboard (Alt+Arrow on the grip handle).
/// </summary>
/// <remarks>
/// Indices are relative to the <em>currently displayed rows</em>. When the table shows the full
/// list (no paging, search or filter active — the intended scenario for reordering) these map
/// directly onto the consumer's backing collection, so a
/// <c>list.RemoveAt(OldIndex); list.Insert(NewIndex, item)</c> applies the move. With a search,
/// filter or page active the indices are view-relative and the consumer is responsible for
/// mapping them onto the underlying data.
/// </remarks>
/// <param name="OldIndex">Zero-based index the row moved from, within the displayed rows.</param>
/// <param name="NewIndex">Zero-based index the row moved to, within the displayed rows.</param>
public sealed record RowReorderEventArgs(int OldIndex, int NewIndex);
