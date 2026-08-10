namespace DRYL.Components;

/// <summary>
/// Filter UI variant for a <see cref="DrylColumn{TItem}"/>. <see cref="Auto"/> picks
/// a sensible default based on the column's field type.
/// </summary>
public enum ColumnFilterType
{
    /// <summary>Infer from the column's field type — string→Text, enum/bool→Select, anything else→Text.</summary>
    Auto,

    /// <summary>Single text input. Uses the <c>Contains</c> operator (case-insensitive).</summary>
    Text,

    /// <summary>Multi-select checkbox list of distinct values. Uses the <c>In</c> operator.</summary>
    Select
}
