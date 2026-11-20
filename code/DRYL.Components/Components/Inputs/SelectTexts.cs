namespace DRYL.Components;

/// <summary>
/// Every human word a <see cref="DrylSelect"/> renders of its own. The defaults are English;
/// replace what you translate with <c>SelectTexts.Default with { … }</c>.
/// </summary>
public sealed record SelectTexts
{
    /// <summary>The English defaults.</summary>
    public static SelectTexts Default { get; } = new();

    /// <summary>Text of an open list that has no options.</summary>
    public string NoOptions { get; init; } = "No options";

    /// <summary>
    /// Accessible name of the list. Receives the field's <see cref="DrylSelect.Label"/>,
    /// which is <c>null</c> or empty when the field has none.
    /// </summary>
    public Func<string?, string> ListLabel { get; init; } =
        label => string.IsNullOrEmpty(label) ? "Options" : $"{label} options";
}
