using System.Text.Json;
using System.Text.Json.Nodes;

namespace DRYL.Components.Canvas;

/// <summary>Who is patching. The pin (<see cref="CanvasNode.Locked"/>) only binds the AI author —
/// what the user triggers always goes through (roadmap A4).</summary>
public enum CanvasPatchAuthor
{
    /// <summary>The user, directly or through a command they pressed. Ignores pins.</summary>
    User,

    /// <summary>The AI author. Ops on pinned nodes come back as a corrective skip reason.</summary>
    Ai,
}

/// <summary>
/// Applies a single <see cref="CanvasOp"/> to a live <see cref="CanvasSpec"/> tree, in place.
/// Every op is validated before the tree is touched: on failure <see cref="Apply"/> returns a
/// corrective, model-facing skip reason and the spec is left exactly as it was.
/// </summary>
public static class CanvasPatcher
{
    /// <summary>
    /// Applies <paramref name="op"/> to <paramref name="spec"/>. Returns <c>null</c> on success;
    /// otherwise a model-facing skip reason, and <paramref name="spec"/> is left unchanged.
    /// </summary>
    /// <param name="spec">The live artifact.</param>
    /// <param name="op">The op to apply.</param>
    /// <param name="author">Who is patching — <see cref="CanvasPatchAuthor.Ai"/> respects pins.</param>
    public static string? Apply(CanvasSpec spec, CanvasOp op,
                                CanvasPatchAuthor author = CanvasPatchAuthor.User) => op.Op switch
    {
        "setProps" => ApplySetProps(spec, op, author),
        "insert" => ApplyInsert(spec, op, author),
        "remove" => ApplyRemove(spec, op, author),
        "move" => ApplyMove(spec, op, author),
        _ => $"op '{op.Op}': unknown operation — use 'setProps', 'insert', 'remove' or 'move'.",
    };

    private static string? ApplySetProps(CanvasSpec spec, CanvasOp op, CanvasPatchAuthor author)
    {
        if (string.IsNullOrWhiteSpace(op.Id))
            return "op 'setProps': id is required.";

        var node = CanvasTree.Find(spec, op.Id);
        if (node is null)
            return $"op 'setProps': no node with id '{op.Id}'.";

        if (author == CanvasPatchAuthor.Ai && node.Locked)
            return $"op 'setProps': node '{node.Id}' is pinned by the user — leave it unchanged and say so if asked.";

        var before = node.Props;
        var merged = CanvasJsonMerge.Merge(ToJsonNode(node.Props), ToJsonNode(op.Props));
        node.Props = ToJsonElement(merged);

        var error = CanvasCatalog.Validate(node);
        if (error is not null)
        {
            node.Props = before;
            return error;
        }

        node.Version++;
        return null;
    }

    private static string? ApplyInsert(CanvasSpec spec, CanvasOp op, CanvasPatchAuthor author)
    {
        if (op.Node is null)
            return "op 'insert': node is required.";
        if (string.IsNullOrWhiteSpace(op.Parent))
            return "op 'insert': parent is required.";

        var parent = CanvasTree.Find(spec, op.Parent);
        if (parent is null)
            return $"op 'insert': no node with id '{op.Parent}'.";
        if (!CanvasCatalog.IsContainer(parent.Type))
            return $"op 'insert': node '{parent.Id}' is not a container.";

        if (author == CanvasPatchAuthor.Ai && parent.Locked)
            return $"op 'insert': node '{parent.Id}' is pinned by the user — nothing may be added to it.";

        var subtreeError = ValidateSubtree(op.Node);
        if (subtreeError is not null)
            return subtreeError;

        var newIds = new HashSet<string>(StringComparer.Ordinal);
        var internalDuplicate = CollectIdsOrFindDuplicate(op.Node, newIds);
        if (internalDuplicate is not null)
            return $"op 'insert': node ids within the inserted subtree must be unique ('{internalDuplicate}' appears twice).";

        var existingIds = CanvasTree.CollectIds(spec.Root);
        var duplicate = newIds.FirstOrDefault(existingIds.Contains);
        if (duplicate is not null)
            return $"op 'insert': id '{duplicate}' already exists in the spec.";

        parent.Children ??= new List<CanvasNode>();
        var index = Math.Clamp(op.Index ?? parent.Children.Count, 0, parent.Children.Count);
        parent.Children.Insert(index, op.Node);

        var parentError = CanvasCatalog.Validate(parent);
        if (parentError is not null)
        {
            parent.Children.RemoveAt(index);
            return parentError;
        }

        parent.Version++;
        return null;
    }

    private static string? ApplyRemove(CanvasSpec spec, CanvasOp op, CanvasPatchAuthor author)
    {
        if (string.IsNullOrWhiteSpace(op.Id))
            return "op 'remove': id is required.";
        if (spec.Root is not null && op.Id == spec.Root.Id)
            return "op 'remove': the root node cannot be removed.";

        var node = CanvasTree.Find(spec, op.Id);
        if (node is null)
            return $"op 'remove': no node with id '{op.Id}'.";

        if (author == CanvasPatchAuthor.Ai && node.Locked)
            return $"op 'remove': node '{node.Id}' is pinned by the user — it must stay.";

        node.Removing = true;
        node.Version++;
        return null;
    }

    private static string? ApplyMove(CanvasSpec spec, CanvasOp op, CanvasPatchAuthor author)
    {
        if (string.IsNullOrWhiteSpace(op.Id))
            return "op 'move': id is required.";
        if (string.IsNullOrWhiteSpace(op.Parent))
            return "op 'move': parent is required.";
        if (spec.Root is not null && op.Id == spec.Root.Id)
            return "op 'move': the root node cannot be moved.";

        var node = CanvasTree.Find(spec, op.Id);
        if (node is null)
            return $"op 'move': no node with id '{op.Id}'.";

        var newParent = CanvasTree.Find(spec, op.Parent);
        if (newParent is null)
            return $"op 'move': no node with id '{op.Parent}'.";
        if (!CanvasCatalog.IsContainer(newParent.Type))
            return $"op 'move': node '{newParent.Id}' is not a container.";

        if (IsSelfOrDescendant(node, op.Parent))
            return $"op 'move': node '{op.Id}' cannot be moved into its own subtree.";

        var oldParent = CanvasTree.FindParent(spec, op.Id);

        // A pin guards the node's own position and, for a container, everything about how it is
        // put together — but not what happens inside its children (see the phase 6 spec, E2).
        if (author == CanvasPatchAuthor.Ai)
        {
            if (node.Locked)
                return $"op 'move': node '{node.Id}' is pinned by the user — its position must stay.";
            if (newParent.Locked)
                return $"op 'move': node '{newParent.Id}' is pinned by the user — nothing may be moved out of or into it.";
            if (oldParent is { Locked: true })
                return $"op 'move': node '{oldParent.Id}' is pinned by the user — nothing may be moved out of or into it.";
        }

        var oldIndex = oldParent!.Children!.IndexOf(node);
        oldParent.Children.RemoveAt(oldIndex);

        newParent.Children ??= new List<CanvasNode>();
        var index = Math.Clamp(op.Index ?? newParent.Children.Count, 0, newParent.Children.Count);
        newParent.Children.Insert(index, node);

        var newParentError = CanvasCatalog.Validate(newParent);
        var oldParentError = CanvasCatalog.Validate(oldParent);
        var error = newParentError ?? oldParentError;
        if (error is not null)
        {
            newParent.Children.Remove(node);
            oldParent.Children.Insert(oldIndex, node);
            return error;
        }

        oldParent.Version++;
        newParent.Version++;
        return null;
    }

    // ---- tree walks ----
    // The plain "find a node / find its parent / collect ids" walks live in CanvasTree, shared
    // with the renderer and the cloner. What stays here is patch-specific validation.

    private static bool IsSelfOrDescendant(CanvasNode node, string id)
    {
        if (node.Id == id) return true;
        if (node.Children is null) return false;
        foreach (var child in node.Children)
            if (IsSelfOrDescendant(child, id)) return true;
        return false;
    }

    private static string? ValidateSubtree(CanvasNode node)
    {
        var error = CanvasCatalog.Validate(node);
        if (error is not null) return error;
        if (node.Children is null) return null;
        foreach (var child in node.Children)
        {
            var childError = ValidateSubtree(child);
            if (childError is not null) return childError;
        }
        return null;
    }

    /// <summary>
    /// Collects every id of <paramref name="node"/>'s subtree into <paramref name="ids"/>. Returns the
    /// first id found to repeat WITHIN the subtree itself (independent of any pre-existing spec ids), or
    /// <c>null</c> if all ids within the subtree are unique.
    /// </summary>
    private static string? CollectIdsOrFindDuplicate(CanvasNode node, HashSet<string> ids)
    {
        if (!ids.Add(node.Id))
            return node.Id;
        if (node.Children is null) return null;
        foreach (var child in node.Children)
        {
            var duplicate = CollectIdsOrFindDuplicate(child, ids);
            if (duplicate is not null) return duplicate;
        }
        return null;
    }

    // ---- JsonElement <-> JsonNode ----

    private static JsonNode? ToJsonNode(JsonElement? element) =>
        element is { } e && e.ValueKind is not (JsonValueKind.Null or JsonValueKind.Undefined)
            ? JsonNode.Parse(e.GetRawText())
            : null;

    private static JsonElement? ToJsonElement(JsonNode? node) =>
        node is null ? null : JsonSerializer.SerializeToElement(node);
}
