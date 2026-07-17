using System.Text.Json;
using System.Text.Json.Nodes;
using DRYL.Components.Agents.Generation;

namespace DRYL.Components.Agents;

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
    public static string? Apply(CanvasSpec spec, CanvasOp op) => op.Op switch
    {
        "setProps" => ApplySetProps(spec, op),
        "insert" => ApplyInsert(spec, op),
        "remove" => ApplyRemove(spec, op),
        "move" => ApplyMove(spec, op),
        _ => $"op '{op.Op}': unknown operation — use 'setProps', 'insert', 'remove' or 'move'.",
    };

    private static string? ApplySetProps(CanvasSpec spec, CanvasOp op)
    {
        if (string.IsNullOrWhiteSpace(op.Id))
            return "op 'setProps': id is required.";

        var node = FindNode(spec, op.Id);
        if (node is null)
            return $"op 'setProps': no node with id '{op.Id}'.";

        var before = node.Props;
        var merged = JsonMerge.Merge(ToJsonNode(node.Props), ToJsonNode(op.Props));
        node.Props = ToJsonElement(merged);

        var error = CanvasCatalog.Validate(node);
        if (error is not null)
        {
            node.Props = before;
            return error;
        }

        return null;
    }

    private static string? ApplyInsert(CanvasSpec spec, CanvasOp op)
    {
        if (op.Node is null)
            return "op 'insert': node is required.";
        if (string.IsNullOrWhiteSpace(op.Parent))
            return "op 'insert': parent is required.";

        var parent = FindNode(spec, op.Parent);
        if (parent is null)
            return $"op 'insert': no node with id '{op.Parent}'.";
        if (!CanvasCatalog.IsContainer(parent.Type))
            return $"op 'insert': node '{parent.Id}' is not a container.";

        var subtreeError = ValidateSubtree(op.Node);
        if (subtreeError is not null)
            return subtreeError;

        var newIds = new HashSet<string>(StringComparer.Ordinal);
        var internalDuplicate = CollectIdsOrFindDuplicate(op.Node, newIds);
        if (internalDuplicate is not null)
            return $"op 'insert': node ids within the inserted subtree must be unique ('{internalDuplicate}' appears twice).";

        var existingIds = CollectIds(spec.Root);
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

        return null;
    }

    private static string? ApplyRemove(CanvasSpec spec, CanvasOp op)
    {
        if (string.IsNullOrWhiteSpace(op.Id))
            return "op 'remove': id is required.";
        if (spec.Root is not null && op.Id == spec.Root.Id)
            return "op 'remove': the root node cannot be removed.";

        var node = FindNode(spec, op.Id);
        if (node is null)
            return $"op 'remove': no node with id '{op.Id}'.";

        node.Removing = true;
        return null;
    }

    private static string? ApplyMove(CanvasSpec spec, CanvasOp op)
    {
        if (string.IsNullOrWhiteSpace(op.Id))
            return "op 'move': id is required.";
        if (string.IsNullOrWhiteSpace(op.Parent))
            return "op 'move': parent is required.";
        if (spec.Root is not null && op.Id == spec.Root.Id)
            return "op 'move': the root node cannot be moved.";

        var node = FindNode(spec, op.Id);
        if (node is null)
            return $"op 'move': no node with id '{op.Id}'.";

        var newParent = FindNode(spec, op.Parent);
        if (newParent is null)
            return $"op 'move': no node with id '{op.Parent}'.";
        if (!CanvasCatalog.IsContainer(newParent.Type))
            return $"op 'move': node '{newParent.Id}' is not a container.";

        if (IsSelfOrDescendant(node, op.Parent))
            return $"op 'move': node '{op.Id}' cannot be moved into its own subtree.";

        var oldParent = FindParent(spec, op.Id);
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

        return null;
    }

    // ---- tree walks ----

    private static CanvasNode? FindNode(CanvasSpec spec, string id) => FindNode(spec.Root, id);

    private static CanvasNode? FindNode(CanvasNode? node, string id)
    {
        if (node is null) return null;
        if (node.Id == id) return node;
        if (node.Children is null) return null;
        foreach (var child in node.Children)
        {
            var found = FindNode(child, id);
            if (found is not null) return found;
        }
        return null;
    }

    private static CanvasNode? FindParent(CanvasSpec spec, string id) => FindParent(spec.Root, id);

    private static CanvasNode? FindParent(CanvasNode? node, string id)
    {
        if (node?.Children is null) return null;
        foreach (var child in node.Children)
        {
            if (child.Id == id) return node;
            var found = FindParent(child, id);
            if (found is not null) return found;
        }
        return null;
    }

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

    private static HashSet<string> CollectIds(CanvasNode? node)
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);
        CollectIds(node, ids);
        return ids;
    }

    private static void CollectIds(CanvasNode? node, HashSet<string> ids)
    {
        if (node is null) return;
        ids.Add(node.Id);
        if (node.Children is null) return;
        foreach (var child in node.Children)
            CollectIds(child, ids);
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
