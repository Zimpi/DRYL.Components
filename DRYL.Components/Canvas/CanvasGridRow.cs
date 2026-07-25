namespace DRYL.Components.Canvas;

/// <summary>One row of a <c>dataGrid</c> node — string cells plus a stable index for row identity.</summary>
internal sealed record CanvasGridRow(int Index, IReadOnlyList<string> Cells)
{
    /// <summary>The cell at <paramref name="i"/>, or an empty string when the row is short.</summary>
    public string Cell(int i) => i >= 0 && i < Cells.Count ? Cells[i] ?? string.Empty : string.Empty;
}
