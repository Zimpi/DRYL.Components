namespace DRYL.Components.Canvas;

/// <summary>Where a keyboard step goes, resolved against the spec tree by <c>DrylCanvas</c>.</summary>
public enum CanvasNav
{
    /// <summary>The previous sibling.</summary>
    Previous,

    /// <summary>The next sibling.</summary>
    Next,

    /// <summary>The parent node (never the root).</summary>
    Parent,

    /// <summary>The first child, for container nodes.</summary>
    FirstChild,

    /// <summary>The first sibling.</summary>
    First,

    /// <summary>The last sibling.</summary>
    Last,
}

/// <summary>What the node toolbar (or a keyboard shortcut) asks the canvas to do.</summary>
public enum CanvasNodeCommand
{
    /// <summary>Pin or unpin the node — see <see cref="CanvasNode.Locked"/>.</summary>
    TogglePin,

    /// <summary>Insert a fresh copy right after the node.</summary>
    Duplicate,

    /// <summary>Remove the node (plays its exit animation first).</summary>
    Remove,

    /// <summary>Move the node one slot up among its siblings.</summary>
    MoveUp,

    /// <summary>Move the node one slot down among its siblings.</summary>
    MoveDown,
}

/// <summary>
/// One completed direct manipulation — raised by <c>DrylCanvas.OnEdit</c> so the host can commit
/// a version and let its document autosave run.
/// </summary>
/// <param name="NodeId">The node the command ran on.</param>
/// <param name="Command">What the user did.</param>
/// <param name="Label">A ready-made history label, e.g. <c>"Removed Revenue"</c>.</param>
public readonly record struct CanvasEdit(string NodeId, CanvasNodeCommand Command, string Label);

/// <summary>
/// The selected node of one canvas surface — the piece of state the renderer and the prompt dock
/// share so the user can point at an element and then talk about it.
/// </summary>
/// <remarks>
/// Plain observable renderer-thread state like <see cref="CanvasWorkspace"/>: no locking, no
/// <c>INotifyPropertyChanged</c>, exactly one <see cref="OnChange"/> per mutation that changed
/// something. One instance per canvas surface; two canvases on a page share nothing.
/// </remarks>
public sealed class CanvasSelection
{
    private string? _fallbackId;

    /// <summary>Id of the selected node, or null while nothing is selected.</summary>
    public string? Id { get; private set; }

    /// <summary>Catalog type of the selected node.</summary>
    public string? Type { get; private set; }

    /// <summary>Speakable name of the selected node (see <see cref="CanvasLabel"/>).</summary>
    public string? Label { get; private set; }

    /// <summary>Whether the selected node is pinned.</summary>
    public bool Locked { get; private set; }

    /// <summary>True while a node is selected.</summary>
    public bool HasSelection => Id is not null;

    /// <summary>
    /// The one node that carries <c>tabindex="0"</c>: the selection, or — while nothing is
    /// selected — the fallback the canvas registered (its root's first child). A whole artifact
    /// tree costs exactly one tab stop.
    /// </summary>
    public string? RovingId => Id ?? _fallbackId;

    /// <summary>Raised after every mutation that changed the selection.</summary>
    public event Action? OnChange;

    /// <summary>Raised by <see cref="RequestPrompt"/> — the dock opens and focuses its composer.</summary>
    public event Action? OnPromptRequested;

    /// <summary>
    /// Selects <paramref name="node"/>. Selecting the same node with the same lock state again
    /// changes nothing and raises nothing.
    /// </summary>
    /// <param name="node">The node the user pointed at.</param>
    /// <param name="focus">Whether the node should also take DOM focus — true for keyboard
    /// navigation, false for a click (the browser has already moved focus).</param>
    public void Select(CanvasNode node, bool focus = false)
    {
        ArgumentNullException.ThrowIfNull(node);

        if (focus) FocusTick++;

        var label = CanvasLabel.For(node);
        if (Id == node.Id && Locked == node.Locked && Label == label && Type == node.Type)
        {
            if (focus) OnChange?.Invoke();   // same node, but it has to take focus again
            return;
        }

        Id = node.Id;
        Type = node.Type;
        Label = label;
        Locked = node.Locked;
        OnChange?.Invoke();
    }

    /// <summary>Drops the selection. A no-op — and silent — when nothing was selected.</summary>
    public void Clear()
    {
        if (Id is null) return;

        Id = null;
        Type = null;
        Label = null;
        Locked = false;
        OnChange?.Invoke();
    }

    /// <summary>Asks the prompt dock to open and focus its composer for the selected element.
    /// Without a dock this is a no-op.</summary>
    public void RequestPrompt() => OnPromptRequested?.Invoke();

    /// <summary>Monotonic counter: a new value tells the selected node's view to take DOM focus.</summary>
    internal int FocusTick { get; private set; }

    /// <summary>Registers the node that owns the tab stop while nothing is selected. Set by
    /// <c>DrylCanvas</c> from the spec's root; silent, because it changes nothing the user sees.</summary>
    internal void SetFallback(string? id) => _fallbackId = id;
}
