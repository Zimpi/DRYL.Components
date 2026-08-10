using System.Text.Json;
using System.Text.Json.Nodes;

namespace DRYL.Components.Canvas;

/// <summary>
/// Deep-copies a node for the toolbar's "duplicate": same shape, same bindings, fresh ids and
/// fresh field names — a copy that shares an id would break every patch, a copy that shares a
/// field name would share the user's input.
/// </summary>
public static class CanvasNodeClone
{
    /// <summary>
    /// A copy of <paramref name="node"/> whose every id is free of <paramref name="existingIds"/>.
    /// The copy starts unpinned; data and action bindings travel with it.
    /// </summary>
    /// <param name="node">The node to copy. Left untouched.</param>
    /// <param name="existingIds">Every id already present in the artifact.</param>
    public static CanvasNode Duplicate(CanvasNode node, IReadOnlySet<string> existingIds)
    {
        ArgumentNullException.ThrowIfNull(node);
        ArgumentNullException.ThrowIfNull(existingIds);

        // JSON roundtrip: the deep copy comes for free and it provably keeps exactly what a
        // saved document would keep (props, data, action) and nothing transient.
        var copy = JsonSerializer.Deserialize<CanvasNode>(
            JsonSerializer.Serialize(node, CanvasJson.Options), CanvasJson.Options)!;

        var takenIds = new HashSet<string>(existingIds, StringComparer.Ordinal);
        var takenNames = new HashSet<string>(StringComparer.Ordinal);
        Rename(copy, takenIds, takenNames);
        return copy;
    }

    private static void Rename(CanvasNode node, HashSet<string> takenIds, HashSet<string> takenNames)
    {
        node.Locked = false;                     // a copy is not the pinned original
        node.Id = FreeName(node.Id, takenIds);
        takenIds.Add(node.Id);

        if (CanvasCatalog.IsInteractive(node.Type)) RenameField(node, takenNames);

        if (node.Children is null) return;
        foreach (var child in node.Children) Rename(child, takenIds, takenNames);
    }

    // The field name is what the form state is keyed by — sharing it would make the copy and the
    // original edit one value.
    private static void RenameField(CanvasNode node, HashSet<string> takenNames)
    {
        if (node.Props is not { ValueKind: JsonValueKind.Object } props) return;
        if (JsonNode.Parse(props.GetRawText()) is not JsonObject obj) return;
        if (obj["name"]?.GetValue<string>() is not { Length: > 0 } name) return;

        var fresh = FreeName(name, takenNames);
        takenNames.Add(fresh);
        obj["name"] = fresh;
        node.Props = JsonSerializer.SerializeToElement(obj, CanvasJson.Options);
    }

    // "id" → "id-2", "id-3", … until free. Deterministic and readable, which matters: these ids
    // end up in the model's next update prompt.
    private static string FreeName(string original, IReadOnlySet<string> taken)
    {
        var stem = string.IsNullOrEmpty(original) ? "node" : original;
        for (var n = 2; ; n++)
        {
            var candidate = FormattableString.Invariant($"{stem}-{n}");
            if (!taken.Contains(candidate)) return candidate;
        }
    }
}
