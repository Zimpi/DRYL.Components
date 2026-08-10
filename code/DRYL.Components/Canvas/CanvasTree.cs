namespace DRYL.Components.Canvas;

/// <summary>The tree walks every canvas subsystem needs: find a node, find its parent, collect
/// ids. One implementation, so the patcher, the renderer and the cloner cannot drift apart.</summary>
internal static class CanvasTree
{
    /// <summary>The node with this id, or null.</summary>
    public static CanvasNode? Find(CanvasSpec spec, string id) => Find(spec.Root, id);

    /// <summary>The node with this id inside this subtree, or null.</summary>
    public static CanvasNode? Find(CanvasNode? node, string id)
    {
        if (node is null) return null;
        if (node.Id == id) return node;
        if (node.Children is null) return null;
        foreach (var child in node.Children)
        {
            var found = Find(child, id);
            if (found is not null) return found;
        }
        return null;
    }

    /// <summary>The parent of the node with this id, or null (also for the root itself).</summary>
    public static CanvasNode? FindParent(CanvasSpec spec, string id) => FindParent(spec.Root, id);

    /// <summary>The parent of the node with this id inside this subtree, or null.</summary>
    public static CanvasNode? FindParent(CanvasNode? node, string id)
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

    /// <summary>Every id in this subtree.</summary>
    public static HashSet<string> CollectIds(CanvasNode? node)
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);
        Collect(node, ids);
        return ids;
    }

    private static void Collect(CanvasNode? node, HashSet<string> ids)
    {
        if (node is null) return;
        ids.Add(node.Id);
        if (node.Children is null) return;
        foreach (var child in node.Children) Collect(child, ids);
    }
}
