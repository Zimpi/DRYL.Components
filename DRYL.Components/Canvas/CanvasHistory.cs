using System.Text.Json;

namespace DRYL.Components.Canvas;

/// <summary>
/// One recorded state of a view: what it was called, when it was taken, and the spec as JSON.
/// Storing the serialized form (not the tree) is what makes an entry immutable and makes
/// "did anything actually change?" a string comparison.
/// </summary>
/// <param name="Label">Human-readable name of the step that produced this state.</param>
/// <param name="At">When the snapshot was taken (UTC).</param>
/// <param name="Json">The serialized <see cref="CanvasSpec"/>, or <c>"null"</c> for an empty view.</param>
public sealed record CanvasHistoryEntry(string Label, DateTimeOffset At, string Json);

/// <summary>
/// The version history of one canvas view — a bounded ring of snapshots with a cursor.
/// </summary>
/// <remarks>
/// There is no op log in the canvas: patches mutate the spec in place and a fresh generation
/// replaces it wholesale. A snapshot ring covers both paths with one mechanism, and the entry it
/// stores is exactly what a document persists.
/// Renderer-thread state like <see cref="CanvasWorkspace"/> — no locking.
/// </remarks>
public sealed class CanvasHistory
{
    private readonly List<CanvasHistoryEntry> _entries = new();

    /// <summary>Creates a history that keeps at most <paramref name="capacity"/> snapshots.</summary>
    /// <param name="capacity">Ring size; clamped to 2…200.</param>
    public CanvasHistory(int capacity = 20) => Capacity = Math.Clamp(capacity, 2, 200);

    /// <summary>How many snapshots the ring keeps before it drops the oldest.</summary>
    public int Capacity { get; }

    /// <summary>The snapshots, oldest first.</summary>
    public IReadOnlyList<CanvasHistoryEntry> Entries => _entries;

    /// <summary>Index of the snapshot currently shown, or -1 while the history is empty.</summary>
    public int Position { get; private set; } = -1;

    /// <summary>True when there is an earlier snapshot to go back to.</summary>
    public bool CanUndo => Position > 0;

    /// <summary>True when an undo can still be taken back.</summary>
    public bool CanRedo => Position >= 0 && Position < _entries.Count - 1;

    /// <summary>Raised after every change to the entries or the cursor.</summary>
    public event Action? OnChange;

    /// <summary>
    /// Records the current state. A snapshot identical to the one at <see cref="Position"/> is
    /// dropped — a round that changed nothing must not fill the ring. Recording after an undo
    /// truncates the redo branch.
    /// </summary>
    /// <param name="spec">The state to remember; <c>null</c> is a legitimate (empty) state.</param>
    /// <param name="label">What produced this state, e.g. the prompt that was sent.</param>
    /// <returns>True when an entry was added.</returns>
    public bool Record(CanvasSpec? spec, string label)
    {
        var json = JsonSerializer.Serialize(spec, CanvasJson.Options);
        if (Position >= 0 && _entries[Position].Json == json) return false;

        if (Position < _entries.Count - 1)
            _entries.RemoveRange(Position + 1, _entries.Count - Position - 1);

        _entries.Add(new CanvasHistoryEntry(label, DateTimeOffset.UtcNow, json));
        if (_entries.Count > Capacity) _entries.RemoveAt(0);
        Position = _entries.Count - 1;

        OnChange?.Invoke();
        return true;
    }

    /// <summary>Steps one snapshot back. Null when there is nothing earlier.</summary>
    public CanvasSpec? Undo() => CanUndo ? Move(Position - 1) : null;

    /// <summary>Steps one snapshot forward. Null when there is nothing later.</summary>
    public CanvasSpec? Redo() => CanRedo ? Move(Position + 1) : null;

    /// <summary>Jumps to any snapshot ("back to version N"). Null when the index is unknown.</summary>
    /// <param name="index">Index into <see cref="Entries"/>.</param>
    public CanvasSpec? Restore(int index) =>
        index < 0 || index >= _entries.Count ? null : Move(index);

    /// <summary>Drops every snapshot.</summary>
    public void Clear()
    {
        if (_entries.Count == 0 && Position < 0) return;

        _entries.Clear();
        Position = -1;
        OnChange?.Invoke();
    }

    // Always a fresh tree: the caller mounts it into a live view and will mutate it.
    private CanvasSpec? Move(int index)
    {
        Position = index;
        OnChange?.Invoke();
        return JsonSerializer.Deserialize<CanvasSpec>(_entries[index].Json, CanvasJson.Options);
    }
}
