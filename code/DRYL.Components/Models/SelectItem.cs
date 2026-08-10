namespace DRYL.Components;

/// <summary>A single option in a <see cref="DrylSelect"/> dropdown.</summary>
public sealed record SelectItem(
    /// <summary>The value written to the bound model when this item is selected.</summary>
    string Value,
    /// <summary>Human-readable text shown in the trigger and the dropdown list.</summary>
    string Label);
