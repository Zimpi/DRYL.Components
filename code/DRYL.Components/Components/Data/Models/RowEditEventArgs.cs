namespace DRYL.Components;

/// <summary>
/// Describes a committed inline edit raised by <see cref="DrylTable{TItem}"/> when the user
/// confirms changes to a row (Enter, or the commit button).
/// </summary>
/// <remarks>
/// When a <see cref="DrylTable{TItem}.CloneRow"/> function is supplied the table edits an
/// isolated working copy, so <see cref="EditedItem"/> is that copy and <see cref="Item"/> is the
/// untouched original — apply the copy back onto your backing collection in the handler (e.g.
/// replace the element, or copy the fields across). Without a clone function the edits are applied
/// live to the original item, so <see cref="Item"/> and <see cref="EditedItem"/> are the same
/// reference and the handler only needs to persist.
/// </remarks>
/// <typeparam name="TItem">Row item type.</typeparam>
/// <param name="Item">The original row item as it exists in the backing collection.</param>
/// <param name="EditedItem">The item carrying the user's edits (a working copy when a clone function is set; otherwise the same reference as <paramref name="Item"/>).</param>
public sealed record RowEditEventArgs<TItem>(TItem Item, TItem EditedItem);
