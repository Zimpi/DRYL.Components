namespace DRYL.Components;

/// <summary>A single option in a <see cref="DrylSelect"/> dropdown.</summary>
/// <param name="Value">The value written to the bound model when this item is selected.</param>
/// <param name="Label">Human-readable text shown in the trigger and the dropdown list.</param>
public sealed record SelectItem(string Value, string Label);
