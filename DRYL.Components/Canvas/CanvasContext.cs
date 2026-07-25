using DRYL.Components.Ai;
using Microsoft.AspNetCore.Components;

namespace DRYL.Components.Canvas;

/// <summary>
/// Everything one canvas shares with every node it renders, handed down as a single
/// <c>IsFixed</c> cascade by <c>DrylCanvas</c>. Two canvases on a page share nothing.
/// </summary>
public sealed class CanvasContext
{
    /// <summary>Live values of the artifact's interactive nodes, keyed by their <c>name</c> prop.</summary>
    public CanvasFormState Form { get; } = new();

    /// <summary>Per-node change stamps driving the change-pulse (AI patch and data refresh alike).</summary>
    public CanvasPulseTracker Pulse { get; internal set; } = new();

    /// <summary>Resolves this artifact's <c>data</c> bindings; <c>null</c> when no data
    /// source registry is available (no host registrations, prerender, plain rendering).</summary>
    public CanvasDataBinder? Binder { get; internal set; }

    /// <summary>Runs this artifact's <c>action</c> bindings; <c>null</c> when no actions are
    /// registered — in which case a button falls back to its <c>intent</c>.</summary>
    public CanvasActionRunner? Actions { get; internal set; }

    /// <summary>Applies a patch op to the canvas's spec (an action result may carry some).
    /// Returns <c>null</c> on success, otherwise a skip reason. Set by <c>DrylCanvas</c>,
    /// which owns the spec.</summary>
    internal Func<CanvasOp, string?>? Patch { get; set; }

    /// <summary>The canvas's current AI state — drives the "waiting for …" skeleton on nodes
    /// that have not finished streaming.</summary>
    public AiState State { get; internal set; }

    /// <summary>Raised when the user triggers a button intent inside the artifact.</summary>
    public EventCallback<CanvasInteraction> Intent { get; internal set; }

    /// <summary>Drops a node from the tree once its exit animation finished. No-op when the
    /// host does not own a mutable spec.</summary>
    internal Func<string, Task>? Purge { get; set; }
}
