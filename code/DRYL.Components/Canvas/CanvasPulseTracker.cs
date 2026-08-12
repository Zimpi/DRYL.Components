namespace DRYL.Components.Canvas;

/// <summary>
/// Monotonic "this node just changed" stamps for one canvas — the single source of truth behind the
/// change-pulse. A content change is otherwise invisible (an insert enters, a move glides, a remove
/// exits), so <c>CanvasNodeView</c> re-keys its pulse overlay whenever a node's stamp moves.
///
/// <para>Two independent authors stamp here: the AI patcher (via <c>DrylCanvasRun</c>) and the data
/// binder on a refresh that actually changed a value. That is deliberate — a data refresh must look
/// exactly like an AI change, one language for "something moved here" (A8).</para>
///
/// <para>Stamps are monotonic rather than boolean: two consecutive changes to the same node must read
/// as two pulses.</para>
/// </summary>
public sealed class CanvasPulseTracker
{
    private readonly Dictionary<string, int> _ticks = new(StringComparer.Ordinal);
    private int _seq;

    /// <summary>The last stamp of <paramref name="id"/> — 0 means "never changed".</summary>
    public int TickOf(string id) => _ticks.TryGetValue(id, out var tick) ? tick : 0;

    /// <summary>Records a change of <paramref name="id"/>, bumping its stamp.</summary>
    public void Stamp(string id)
    {
        if (string.IsNullOrEmpty(id)) return;
        _ticks[id] = ++_seq;
    }

    /// <summary>Forgets every stamp. A fresh artifact recycles ids — a stale stamp would read as
    /// "this node just changed" and pulse a node that is in fact brand new (it enters instead).</summary>
    public void Clear() => _ticks.Clear();
}
