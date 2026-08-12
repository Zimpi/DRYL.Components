namespace DRYL.Components.Agents;

/// <summary>DOM snapshot of the wrapped field, returned by the <c>dryl-aifield.js</c> module.</summary>
internal sealed record AiFieldSnapshot
{
    /// <summary>Whether a text-like input/textarea was found inside the wrapper.</summary>
    public bool Found { get; init; }

    /// <summary>The field's current value ("" when not found).</summary>
    public string Value { get; init; } = string.Empty;

    /// <summary>Selection start, or -1 when the element exposes no selection API.</summary>
    public int SelStart { get; init; } = -1;

    /// <summary>Selection end, or -1 when the element exposes no selection API.</summary>
    public int SelEnd { get; init; } = -1;

    /// <summary>True when a non-empty text range is selected.</summary>
    public bool HasSelection => Found && SelStart >= 0 && SelEnd > SelStart;
}
