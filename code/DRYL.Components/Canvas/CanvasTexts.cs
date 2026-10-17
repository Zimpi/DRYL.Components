namespace DRYL.Components.Canvas;

/// <summary>
/// The fixed words of a canvas's own chrome — header buttons, the node toolbar, node hints and
/// the screen-reader announcements — in one place, so a host can translate them in one place.
/// Hand an instance to <c>DrylCanvas.Texts</c> (or <c>DrylAiCanvas.Texts</c>); every member
/// defaults to the English text the canvas has always used, so set only what you replace:
/// <code>Texts="@(CanvasTexts.Default with { Expand = "Vergrößern", Refresh = "Aktualisieren" })"</code>
/// The artifact's own content (titles, labels, values) is authored by the spec, not here.
/// </summary>
public sealed record CanvasTexts
{
    /// <summary>The English defaults.</summary>
    public static CanvasTexts Default { get; } = new();

    // ───── header ─────

    /// <summary>Label and tooltip of the expand button while the canvas is inline.</summary>
    public string Expand { get; init; } = "Expand artifact";

    /// <summary>Label and tooltip of the expand button while the canvas is fullscreen.</summary>
    public string ExitFullscreen { get; init; } = "Exit fullscreen";

    /// <summary>Label of the refresh button shown when the artifact binds data.</summary>
    public string Refresh { get; init; } = "Refresh data";

    // ───── node toolbar ─────

    /// <summary>Label and tooltip of the toolbar button that prompts about the selected element.</summary>
    public string Prompt { get; init; } = "Prompt about this element";

    /// <summary>Label and tooltip of the pin button on an unpinned element.</summary>
    public string Pin { get; init; } = "Pin element";

    /// <summary>Label and tooltip of the pin button on a pinned element.</summary>
    public string Unpin { get; init; } = "Unpin element";

    /// <summary>Label and tooltip of the duplicate button.</summary>
    public string Duplicate { get; init; } = "Duplicate element";

    /// <summary>Label and tooltip of the remove button.</summary>
    public string Remove { get; init; } = "Remove element";

    /// <summary>Label and tooltip of the drag handle.</summary>
    public string Reorder { get; init; } = "Reorder element";

    // ───── nodes ─────

    /// <summary>Appended to a pinned element's accessible name ("Revenue, lineChart, pinned").</summary>
    public string PinnedState { get; init; } = "pinned";

    /// <summary>Caption under a node that has not finished streaming, from its node type.</summary>
    public Func<string, string> WaitingFor { get; init; } = type => $"waiting for {type}…";

    /// <summary>Hint on a required form field the user left empty on submit.</summary>
    public string Required { get; init; } = "Required";

    /// <summary>Marker on a bound node whose source failed while a good value is still shown.</summary>
    public string NotUpToDate { get; init; } = "Not up to date";

    /// <summary>Note under a bound table cut to its row limit, from that limit.</summary>
    public Func<int, string> Truncated { get; init; } =
        rows => FormattableString.Invariant($"Showing the first {rows} rows.");

    /// <summary>Accessible name of a <c>dataGrid</c> node's table.</summary>
    public string DataGrid { get; init; } = "Data grid";

    // ───── announcements (aria-live) ─────

    /// <summary>Announced when an element is selected, from its label and its node type.</summary>
    public Func<string, string, string> Selected { get; init; } = (label, type) => $"Selected: {label}, {type}";

    /// <summary>Announced when the selection is cleared.</summary>
    public string SelectionCleared { get; init; } = "Selection cleared.";

    /// <summary>Announced when an element is pinned, from its label.</summary>
    public Func<string, string> Pinned { get; init; } = label => $"{label} pinned.";

    /// <summary>Announced when an element is unpinned, from its label.</summary>
    public Func<string, string> Unpinned { get; init; } = label => $"{label} unpinned.";

    /// <summary>Announced when an element is duplicated, from its label.</summary>
    public Func<string, string> Duplicated { get; init; } = label => $"{label} duplicated.";

    /// <summary>Announced when an element is removed, from its label.</summary>
    public Func<string, string> Removed { get; init; } = label => $"{label} removed.";

    /// <summary>Announced when an element is reordered, from its label, its new 1-based position
    /// and the number of its siblings.</summary>
    public Func<string, int, int, string> MovedTo { get; init; } =
        (label, position, count) => FormattableString.Invariant($"{label} moved to position {position} of {count}.");

    // ───── edit labels (CanvasEdit.Label) ─────

    /// <summary><see cref="CanvasEdit.Label"/> of a pin or unpin, from the element's label.</summary>
    public Func<string, string> PinEdit { get; init; } = label => $"Pinned {label}";

    /// <summary><see cref="CanvasEdit.Label"/> of a duplicate, from the element's label.</summary>
    public Func<string, string> DuplicateEdit { get; init; } = label => $"Duplicated {label}";

    /// <summary><see cref="CanvasEdit.Label"/> of a remove, from the element's label.</summary>
    public Func<string, string> RemoveEdit { get; init; } = label => $"Removed {label}";

    /// <summary><see cref="CanvasEdit.Label"/> of a reorder, from the element's label.</summary>
    public Func<string, string> MoveEdit { get; init; } = label => $"Moved {label}";
}
