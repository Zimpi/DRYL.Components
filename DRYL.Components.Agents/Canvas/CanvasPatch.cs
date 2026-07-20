using System.Text.Json;

namespace DRYL.Components.Agents;

/// <summary>A batch of operations the model emits to mutate a live <see cref="CanvasSpec"/> in place.</summary>
public sealed class CanvasPatchDoc
{
    /// <summary>The ordered operations to apply, one after another.</summary>
    public List<CanvasOp>? Ops { get; set; }
}

/// <summary>
/// One patch operation against a <see cref="CanvasSpec"/> tree, applied by <see cref="CanvasPatcher"/>.
/// <see cref="Op"/> selects which of the other members are relevant:
/// <list type="bullet">
/// <item><description><c>setProps</c> — <see cref="Id"/> (target node) + <see cref="Props"/> (partial props to shallow-merge).</description></item>
/// <item><description><c>insert</c> — <see cref="Parent"/> (container to insert into) + <see cref="Index"/> + <see cref="Node"/> (new subtree).</description></item>
/// <item><description><c>remove</c> — <see cref="Id"/> (node to mark <see cref="CanvasNode.Removing"/>).</description></item>
/// <item><description><c>move</c> — <see cref="Id"/> (node to move) + <see cref="Parent"/> (new parent) + <see cref="Index"/> (new position).</description></item>
/// </list>
/// </summary>
public sealed class CanvasOp
{
    /// <summary>The operation kind: <c>setProps</c>, <c>insert</c>, <c>remove</c> or <c>move</c>.</summary>
    public string Op { get; set; } = string.Empty;

    /// <summary>The target node id — the node being patched, removed or moved.</summary>
    public string? Id { get; set; }

    /// <summary>The (new) parent container id — used by <c>insert</c> and <c>move</c>.</summary>
    public string? Parent { get; set; }

    /// <summary>The target child index within <see cref="Parent"/>; clamped to the parent's bounds. Used by <c>insert</c> and <c>move</c>.</summary>
    public int? Index { get; set; }

    /// <summary>For <c>setProps</c>: the partial props object shallow-merged onto the target node's existing props.</summary>
    public JsonElement? Props { get; set; }

    /// <summary>For <c>insert</c>: the new node (and its subtree) to insert.</summary>
    public CanvasNode? Node { get; set; }
}
