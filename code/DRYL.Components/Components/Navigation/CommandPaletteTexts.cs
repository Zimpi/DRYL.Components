namespace DRYL.Components;

/// <summary>
/// Every human word a <see cref="DrylCommandPalette"/> renders of its own. The defaults are
/// English; replace what you translate with <c>CommandPaletteTexts.Default with { … }</c>.
/// The palette's <see cref="DrylCommandPalette.Placeholder"/>, <see cref="DrylCommandPalette.EmptyText"/>
/// and <see cref="DrylCommandPalette.AriaLabel"/> win over the matching members when set.
/// </summary>
public sealed record CommandPaletteTexts
{
    /// <summary>The English defaults.</summary>
    public static CommandPaletteTexts Default { get; } = new();

    /// <summary>Placeholder of the search field.</summary>
    public string Placeholder { get; init; } = "Search commands…";

    /// <summary>Accessible name of the palette dialog.</summary>
    public string AriaLabel { get; init; } = "Command palette";

    /// <summary>Shown when nothing matches the query.</summary>
    public string EmptyText { get; init; } = "No results";

    /// <summary>Accessible name of the result skeleton while the first results load.</summary>
    public string Loading { get; init; } = "Loading results…";

    /// <summary>The back button of the argument view.</summary>
    public string Back { get; init; } = "Back";

    /// <summary>The run button of the argument view.</summary>
    public string Run { get; init; } = "Run";

    /// <summary>The pill in the search row while a resolver works.</summary>
    public string Thinking { get; init; } = "Thinking…";

    /// <summary>Accessible name of the AI result panel.</summary>
    public string AiResult { get; init; } = "AI result";

    /// <summary>Title of the confirmation asked before a destructive command runs.</summary>
    public string ConfirmTitle { get; init; } = "Confirm action";

    /// <summary>Confirm button of that confirmation.</summary>
    public string Confirm { get; init; } = "Run";

    /// <summary>Cancel button of that confirmation.</summary>
    public string Cancel { get; init; } = "Cancel";

    /// <summary>Footer hint beside ↑ ↓.</summary>
    public string HintNavigate { get; init; } = "Navigate";

    /// <summary>Footer hint beside ↵.</summary>
    public string HintRun { get; init; } = "Open";

    /// <summary>Footer hint beside Tab — moving to the next group.</summary>
    public string HintGroups { get; init; } = "Next group";

    /// <summary>Footer hint beside Esc.</summary>
    public string HintClose { get; init; } = "Close";
}
