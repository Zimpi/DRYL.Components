namespace DRYL.Components;

/// <summary>
/// Snapshot of the table state passed to a server-side <c>DataProvider</c> callback.
/// The provider returns a <see cref="DataResult{TItem}"/> with the matching page.
/// </summary>
/// <param name="Skip">Number of items to skip (page offset). Zero when pagination is disabled.</param>
/// <param name="Take">Page size. <c>int.MaxValue</c> when pagination is disabled.</param>
/// <param name="SearchText">Current global search text, or null when empty.</param>
/// <param name="Sort">Active sort descriptors in priority order.</param>
/// <param name="Filters">Active per-column filter descriptors.</param>
public sealed record DataRequest(
    int Skip,
    int Take,
    string? SearchText,
    IReadOnlyList<SortDescriptor> Sort,
    IReadOnlyList<FilterDescriptor> Filters);
