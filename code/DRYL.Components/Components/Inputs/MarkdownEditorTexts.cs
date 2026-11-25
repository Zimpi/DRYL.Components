namespace DRYL.Components;

/// <summary>
/// Every human word a <see cref="DrylMarkdownEditor"/> renders of its own. The defaults are
/// English; replace what you translate with <c>MarkdownEditorTexts.Default with { … }</c>.
/// </summary>
public sealed record MarkdownEditorTexts
{
    /// <summary>The English defaults.</summary>
    public static MarkdownEditorTexts Default { get; } = new();

    /// <summary>Accessible name of the view switch.</summary>
    public string View { get; init; } = "View";

    /// <summary>Label of the switch segment that shows the source.</summary>
    public string Editor { get; init; } = "Editor";

    /// <summary>Label of the switch segment that shows the rendered document, and accessible name of that region.</summary>
    public string Preview { get; init; } = "Preview";

    /// <summary>Text of the preview while there is nothing to render.</summary>
    public string PreviewEmpty { get; init; } = "Nothing to preview";

    /// <summary>Accessible name of the formatting toolbar.</summary>
    public string Toolbar { get; init; } = "Formatting";

    /// <summary>Tooltip and accessible name of the bold command.</summary>
    public string Bold { get; init; } = "Bold";

    /// <summary>Tooltip and accessible name of the italic command.</summary>
    public string Italic { get; init; } = "Italic";

    /// <summary>Tooltip and accessible name of the strikethrough command.</summary>
    public string Strikethrough { get; init; } = "Strikethrough";

    /// <summary>Tooltip and accessible name of the heading command.</summary>
    public string Heading { get; init; } = "Heading";

    /// <summary>Tooltip and accessible name of the bullet list command.</summary>
    public string BulletList { get; init; } = "Bullet list";

    /// <summary>Tooltip and accessible name of the numbered list command.</summary>
    public string NumberedList { get; init; } = "Numbered list";

    /// <summary>Tooltip and accessible name of the task list command.</summary>
    public string TaskList { get; init; } = "Task list";

    /// <summary>Tooltip and accessible name of the quote command.</summary>
    public string Quote { get; init; } = "Quote";

    /// <summary>Tooltip and accessible name of the code command.</summary>
    public string Code { get; init; } = "Code";

    /// <summary>Tooltip and accessible name of the link command.</summary>
    public string Link { get; init; } = "Link";
}
