namespace DRYL.Components;

/// <summary>
/// Every human word a <see cref="DrylMultiSelect"/> renders or announces of its own. The defaults
/// are English; replace what you translate with <c>MultiSelectTexts.Default with { … }</c>.
/// The field's <see cref="DrylMultiSelect.Placeholder"/>, <see cref="DrylMultiSelect.SearchPlaceholder"/>
/// and <see cref="DrylMultiSelect.EmptyText"/> win over the matching members when set.
/// </summary>
public sealed record MultiSelectTexts
{
    /// <summary>The English defaults.</summary>
    public static MultiSelectTexts Default { get; } = new();

    /// <summary>Shown while nothing is picked.</summary>
    public string Placeholder { get; init; } = "Select options…";

    /// <summary>Placeholder of the search field while values are picked.</summary>
    public string SearchPlaceholder { get; init; } = "Search…";

    /// <summary>Text of an open list whose query matches no option.</summary>
    public string EmptyText { get; init; } = "No results";

    /// <summary>Text of an open list that has no options at all.</summary>
    public string NoOptions { get; init; } = "No options";

    /// <summary>
    /// Accessible name of the list. Receives the field's <see cref="DrylMultiSelect.Label"/>,
    /// which is <c>null</c> or empty when the field has none.
    /// </summary>
    public Func<string?, string> ListLabel { get; init; } =
        label => string.IsNullOrEmpty(label) ? "Options" : $"{label} options";

    /// <summary>Accessible name of a chip's remove button. Receives the chip's label.</summary>
    public Func<string, string> Remove { get; init; } = label => $"Remove {label}";

    /// <summary>
    /// Announced when <c>Backspace</c> in the empty search field marks the last picked value.
    /// Receives that value's label.
    /// </summary>
    public Func<string, string> RemoveArmed { get; init; } =
        label => $"{label}. Press Backspace again to remove.";

    /// <summary>Announced when a picked value is removed from the field. Receives its label.</summary>
    public Func<string, string> Removed { get; init; } = label => $"{label} removed.";

    /// <summary>
    /// Announced after a query changed the list. Receives the number of options it lists;
    /// a query without matches is announced through <see cref="EmptyText"/> instead.
    /// </summary>
    public Func<int, string> Results { get; init; } =
        count => count == 1 ? "1 result" : $"{count} results";
}
