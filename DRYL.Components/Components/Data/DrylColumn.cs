using System.Linq.Expressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;

namespace DRYL.Components;

/// <summary>
/// Declarative column definition for <see cref="DrylTable{TItem}"/>. Place these inside the
/// table's <c>Columns</c> slot. The column registers itself with the parent table via a
/// cascading reference and renders nothing on its own — the table iterates its registered
/// columns to build the header and body cells.
/// </summary>
/// <typeparam name="TItem">Row item type. Must match the parent table.</typeparam>
public class DrylColumn<TItem> : ComponentBase, IDisposable
{
    [CascadingParameter] internal DrylTable<TItem>? Owner { get; set; }

    /// <summary>
    /// Property accessor used as the column's value, default sort key, and default search source.
    /// Pass a lambda like <c>@(x =&gt; x.Name)</c>. Either <see cref="Field"/> or a
    /// <see cref="CellTemplate"/> is required.
    /// </summary>
    [Parameter] public Expression<Func<TItem, object?>>? Field { get; set; }

    /// <summary>Header text. Omit and use <see cref="HeaderTemplate"/> for custom header markup.</summary>
    [Parameter] public string? Title { get; set; }

    /// <summary>
    /// Stable key used for sort/filter/persistence. Auto-derived from <see cref="Field"/>'s
    /// member name when not set explicitly.
    /// </summary>
    [Parameter] public string? ColumnKey { get; set; }

    /// <summary>Enables click-to-sort on this column's header.</summary>
    [Parameter] public bool Sortable { get; set; }

    /// <summary>
    /// Includes this column's value in the global toolbar search. Has no effect when
    /// the table has its own <c>SearchPredicate</c> override.
    /// </summary>
    [Parameter] public bool Searchable { get; set; }

    /// <summary>Shows a filter icon in the header that opens a per-column filter popover.</summary>
    [Parameter] public bool Filterable { get; set; }

    /// <summary>Variant of the filter UI. Default <see cref="ColumnFilterType.Auto"/>.</summary>
    [Parameter] public ColumnFilterType FilterType { get; set; } = ColumnFilterType.Auto;

    /// <summary>
    /// Override for the list of values shown in a <see cref="ColumnFilterType.Select"/> filter.
    /// Receives the underlying items and returns the distinct option set. When null and
    /// <c>FilterType=Select</c>, the table auto-derives distinct values from <see cref="Field"/>.
    /// </summary>
    [Parameter] public Func<IEnumerable<TItem>, IEnumerable<object?>>? FilterValues { get; set; }

    /// <summary>
    /// Marks this column as the primary identifier — its cells get
    /// <c>tbl-td-primary</c> styling (full-color, medium weight).
    /// </summary>
    [Parameter] public bool Primary { get; set; }

    /// <summary>Horizontal alignment of header and cells. Default <see cref="ColumnAlign.Start"/>.</summary>
    [Parameter] public ColumnAlign Align { get; set; } = ColumnAlign.Start;

    /// <summary>Optional explicit column width (any CSS length, e.g. <c>"120px"</c> or <c>"15%"</c>).</summary>
    [Parameter] public string? Width { get; set; }

    /// <summary>Cell renderer. Receives the row item. When omitted, the column renders <c>Field</c>'s value as text.</summary>
    [Parameter] public RenderFragment<TItem>? CellTemplate { get; set; }

    /// <summary>Optional header renderer. When omitted, <see cref="Title"/> is rendered as text.</summary>
    [Parameter] public RenderFragment? HeaderTemplate { get; set; }

    private Func<TItem, object?>? _compiledFieldGetter;
    private bool _registered;

    /// <summary>Stable column key (auto-derived from <see cref="Field"/> when not set).</summary>
    public string Key { get; private set; } = string.Empty;

    /// <summary>Display title — falls back to <see cref="Key"/> when not set.</summary>
    public string DisplayTitle => Title ?? Key;

    /// <summary>Compiled field accessor (null when <see cref="Field"/> isn't set).</summary>
    public Func<TItem, object?>? FieldGetter => _compiledFieldGetter;

    /// <summary>Underlying CLR type of the field expression (null when <see cref="Field"/> isn't set).</summary>
    public Type? FieldType { get; private set; }

    /// <summary>Resolved filter type — <see cref="ColumnFilterType.Auto"/> mapped to the concrete kind based on <see cref="FieldType"/>.</summary>
    public ColumnFilterType ResolvedFilterType =>
        FilterType != ColumnFilterType.Auto
            ? FilterType
            : (FieldType is not null && (FieldType.IsEnum || FieldType == typeof(bool) || FieldType == typeof(bool?))
                ? ColumnFilterType.Select
                : ColumnFilterType.Text);

    protected override void OnInitialized()
    {
        if (Owner is null)
            throw new InvalidOperationException(
                "DrylColumn must be placed inside the Columns slot of a DrylTable<TItem>.");

        if (Field is not null)
        {
            _compiledFieldGetter = Field.Compile();
            Key = ColumnKey ?? ExtractMemberName(Field) ?? Guid.NewGuid().ToString("N");
            FieldType = ExtractMemberType(Field);
        }
        else
        {
            Key = ColumnKey ?? Guid.NewGuid().ToString("N");
        }

        Owner.AddColumn(this);
        _registered = true;
    }

    /// <summary>Returns the column value for an item, or null when no <see cref="Field"/> is set.</summary>
    public object? GetValue(TItem item) => _compiledFieldGetter?.Invoke(item);

    /// <summary>Maps <see cref="Align"/> to the matching CSS alignment value.</summary>
    public string TextAlign => Align switch
    {
        ColumnAlign.Center => "center",
        ColumnAlign.End => "right",
        _ => "left"
    };

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        // Columns render nothing — the parent table reads from its registered column list.
    }

    public void Dispose()
    {
        if (_registered) Owner?.RemoveColumn(this);
    }

    private static string? ExtractMemberName(Expression expr) => expr switch
    {
        LambdaExpression lambda => ExtractMemberName(lambda.Body),
        UnaryExpression { NodeType: ExpressionType.Convert } u => ExtractMemberName(u.Operand),
        MemberExpression m => m.Member.Name,
        _ => null
    };

    private static Type? ExtractMemberType(Expression expr) => expr switch
    {
        LambdaExpression lambda => ExtractMemberType(lambda.Body),
        UnaryExpression { NodeType: ExpressionType.Convert } u => ExtractMemberType(u.Operand),
        MemberExpression m => m.Type,
        _ => null
    };
}
