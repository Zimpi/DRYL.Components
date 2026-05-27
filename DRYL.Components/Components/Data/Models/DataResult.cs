namespace DRYL.Components;

/// <summary>
/// Result of a <c>DataProvider</c> callback. Contains the slice of items for the current
/// page and the unpaged total count so pagination can render page numbers.
/// </summary>
/// <typeparam name="TItem">Item type.</typeparam>
/// <param name="Items">Items for the current page.</param>
/// <param name="TotalCount">Total count across all pages (unfiltered by paging, but filtered by search/filters).</param>
public sealed record DataResult<TItem>(
    IReadOnlyList<TItem> Items,
    int TotalCount);
