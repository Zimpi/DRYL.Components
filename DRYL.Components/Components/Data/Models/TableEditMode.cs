namespace DRYL.Components;

/// <summary>Granularity of inline editing in <see cref="DrylTable{TItem}"/>.</summary>
public enum TableEditMode
{
    /// <summary>
    /// Editing the row puts every editable column into edit mode at once, with a single
    /// commit/cancel affordance for the whole row. Default.
    /// </summary>
    Row,

    /// <summary>
    /// Only the activated cell becomes editable; Enter commits and Escape cancels that one cell.
    /// </summary>
    Cell
}
