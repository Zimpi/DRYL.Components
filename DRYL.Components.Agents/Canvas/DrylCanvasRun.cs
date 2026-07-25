using DRYL.Components.Canvas;

namespace DRYL.Components.Agents;

/// <summary>Observable handle to an AI-built canvas artifact rendered by <c>DrylAiCanvas</c>.</summary>
public sealed class DrylCanvasRun : DrylRunBase
{
    private readonly HashSet<string> _changedIds = new(StringComparer.Ordinal);

    /// <summary>
    /// The change stamps behind the canvas's change-pulse. The run stamps a node on every
    /// applied <c>setProps</c>; a data binder bound to the same canvas stamps its refreshes into
    /// the very same tracker, so an AI change and a data change read as one language (A8).
    /// Hand this to <c>DrylCanvas.Pulse</c> — <c>DrylAiCanvas</c> does it for you.
    /// </summary>
    public CanvasPulseTracker Pulse { get; } = new();

    /// <summary>The live spec (progressively richer as generations complete), or null before the first one.</summary>
    public CanvasSpec? Spec { get; private set; }

    /// <summary>The number of completed create/update generations.</summary>
    public int Round { get; private set; }

    /// <summary>The ids touched by the latest batch of ops — drives move/reveal animations.</summary>
    public IReadOnlyCollection<string> ChangedIds => _changedIds;

    /// <summary>The number of nodes in the live tree — 0 while there is no artifact.</summary>
    public int NodeCount => Spec?.Root is { } root ? Count(root) : 0;

    /// <summary>
    /// The usable inline width (CSS px) of the surface the artifact renders into, as last
    /// measured by <c>DrylAiCanvas</c>; <c>null</c> before the first measurement (prerender,
    /// no JS, or no canvas mounted). Every generation reads the current value and hands the
    /// generator a matching layout budget, so an artifact built on a phone is authored for a
    /// phone rather than shrunk into one afterwards.
    /// <para>This is the canvas element's width, not the viewport's: on a wide desktop the
    /// canvas may still sit in a narrow side panel, and that panel is the real budget.</para>
    /// </summary>
    public int? AvailableWidth { get; private set; }

    /// <summary>
    /// Reports the measured width of the rendering surface (see <see cref="AvailableWidth"/>).
    /// Deliberately does not raise <c>OnChange</c> — the value only feeds the next generation's
    /// prompt, and a resize must never re-render the artifact tree.
    /// </summary>
    public void ReportWidth(int widthPx)
    {
        if (widthPx > 0) AvailableWidth = widthPx;
    }

    private static int Count(CanvasNode node)
    {
        var total = 1;
        if (node.Children is { } children)
            foreach (var child in children) total += Count(child);
        return total;
    }

    /// <summary>Starts a new create/update generation: state becomes streaming, <see cref="ChangedIds"/> and <see cref="DrylRunBase.Error"/> are cleared.</summary>
    internal void BeginGeneration()
    {
        State = AiState.Streaming;
        _changedIds.Clear();
        Error = null;
        Raise();
    }

    /// <summary>Create-streaming: replaces <see cref="Spec"/> with a freshly (possibly partial) parsed snapshot.</summary>
    internal void ApplySnapshot(CanvasSpec snapshot)
    {
        Spec = snapshot;
        Raise();
    }

    private bool _revealStarted;
    private int _artifactEpoch;

    /// <summary>Monotonic counter bumped by every <see cref="BeginCreate"/> — the canvas watches
    /// it to reset interactive form state for a fresh artifact.</summary>
    internal int ArtifactEpoch => _artifactEpoch;

    /// <summary>
    /// Starts a create generation. Unlike <see cref="BeginGeneration"/> alone, the next
    /// <see cref="RevealSnapshot"/> begins a fresh, empty live tree — a create replaces whatever artifact was
    /// there before. <see cref="Spec"/> is deliberately left untouched here (still <c>null</c> before the very
    /// first artifact): the tree is only swapped once the first snapshot actually arrives, so a consumer that
    /// keys create-vs-update on <c>Spec is null</c> still sees "no artifact yet" until generation produces one.
    /// </summary>
    internal void BeginCreate()
    {
        _artifactEpoch++;
        _revealStarted = false;
        // A fresh artifact recycles ids — a stale stamp would read as "this node just
        // changed" and pulse a node that is in fact brand new (it enters instead).
        Pulse.Clear();
        BeginGeneration();
    }

    /// <summary>
    /// Create-streaming: incrementally reveals only the fully-streamed nodes of <paramref name="snapshot"/>
    /// into the live <see cref="Spec"/> (see <see cref="CanvasStreamReveal"/>). Raises only when a new node
    /// is revealed, so the artifact fades in element by element instead of flickering as a half-parsed tree.
    /// The first reveal of a generation starts from a fresh empty tree.
    /// </summary>
    internal void RevealSnapshot(CanvasSpec snapshot)
    {
        if (!_revealStarted)
        {
            Spec = new CanvasSpec();
            _revealStarted = true;
        }

        if (CanvasStreamReveal.Reveal(Spec!, snapshot, streamDone: false))
            Raise();
    }

    /// <summary>
    /// Create path completion: flushes <paramref name="final"/> into the live tree — revealing the last
    /// node each level was still withholding while keeping every already-revealed instance frozen — then
    /// bumps <see cref="Round"/> and settles at <see cref="AiState.Generated"/>.
    /// </summary>
    internal void CompleteReveal(CanvasSpec final)
    {
        if (!_revealStarted)
        {
            Spec = new CanvasSpec();
            _revealStarted = true;
        }

        CanvasStreamReveal.Reveal(Spec!, final, streamDone: true);
        Round++;
        State = AiState.Generated;
        Raise();
    }

    /// <summary>
    /// Applies one patch op to the live <see cref="Spec"/> via <see cref="CanvasPatcher"/>, tracking the
    /// affected id in <see cref="ChangedIds"/> on success. Returns null on success, otherwise a
    /// model-facing skip reason; never throws. Raises OnChange only when the op succeeds.
    /// </summary>
    internal string? ApplyOp(CanvasOp op)
    {
        if (Spec is null)
            return $"op '{op.Op}': there is no artifact to patch.";

        var reason = CanvasPatcher.Apply(Spec, op);
        if (reason is null)
        {
            switch (op.Op)
            {
                case "setProps":
                    // The only op with no motion of its own — stamp it so the node pulses.
                    if (op.Id is not null) { _changedIds.Add(op.Id); Pulse.Stamp(op.Id); }
                    break;
                case "move":
                    if (op.Id is not null) _changedIds.Add(op.Id);
                    break;
                case "insert":
                    if (op.Node?.Id is not null) _changedIds.Add(op.Node.Id);
                    break;
            }

            Raise();
        }

        return reason;
    }

    /// <summary>Update path: ops already mutated <see cref="Spec"/> in place — bumps <see cref="Round"/> and settles at <see cref="AiState.Generated"/>.</summary>
    internal void CompleteGeneration()
    {
        Round++;
        State = AiState.Generated;
        Raise();
    }

    /// <summary>Create path: replaces <see cref="Spec"/> with the final, strict-parsed spec — bumps <see cref="Round"/> and settles at <see cref="AiState.Generated"/>.</summary>
    internal void CompleteGeneration(CanvasSpec final)
    {
        Spec = final;
        Round++;
        State = AiState.Generated;
        Raise();
    }

    /// <summary>
    /// Settles a failed generation: <see cref="DrylRunBase.Error"/> is set and state becomes
    /// <see cref="AiState.None"/>. The canvas itself stays alive — a later <c>create_artifact</c>
    /// call may still succeed — so this does NOT mark the run completed.
    /// </summary>
    internal void FailGeneration(Exception ex)
    {
        Error = new DrylRunError(ex.Message, ex);
        State = AiState.None;
        Raise();
    }

    /// <summary>
    /// Settles a cancelled generation: state returns to <see cref="AiState.None"/> without an
    /// error — the artifact stays as it was and a later generation may continue.
    /// </summary>
    internal void CancelGeneration()
    {
        State = AiState.None;
        Raise();
    }

    /// <summary>
    /// Drops a node (already flagged <see cref="CanvasNode.Removing"/>) from its parent's children
    /// once its exit animation has finished. Unknown or root ids are a no-op — an exit animation may
    /// race a fresh create.
    /// </summary>
    public void Purge(string id)
    {
        var root = Spec?.Root;
        if (root is null || id == root.Id) return;

        if (RemoveChild(root, id))
            Raise();
    }

    private static bool RemoveChild(CanvasNode node, string id)
    {
        if (node.Children is null) return false;
        for (var i = 0; i < node.Children.Count; i++)
        {
            if (node.Children[i].Id == id)
            {
                node.Children.RemoveAt(i);
                node.Version++;
                return true;
            }
        }
        foreach (var child in node.Children)
            if (RemoveChild(child, id)) return true;
        return false;
    }
}
