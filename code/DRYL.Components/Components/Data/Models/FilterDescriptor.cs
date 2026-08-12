namespace DRYL.Components;

/// <summary>Comparison operators available for column filters.</summary>
public enum FilterOperator
{
    Contains,
    Equals,
    NotEquals,
    GreaterThan,
    LessThan,
    GreaterThanOrEqual,
    LessThanOrEqual,
    In,
    IsNull,
    IsNotNull
}

/// <summary>
/// A single column-level filter rule. Multiple descriptors are AND-combined in the pipeline.
/// </summary>
/// <param name="ColumnKey">Stable key of the column (see <see cref="DrylColumn{TItem}.Key"/>).</param>
/// <param name="Operator">Comparison operator.</param>
/// <param name="Value">Value to compare against. May be a collection for <see cref="FilterOperator.In"/>.</param>
public sealed record FilterDescriptor(string ColumnKey, FilterOperator Operator, object? Value);
