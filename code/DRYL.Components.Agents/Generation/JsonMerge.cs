using System.Text.Json.Nodes;

namespace DRYL.Components.Agents.Generation;

/// <summary>
/// Deep-merges a partial JSON patch into a running JSON object. Objects merge recursively; scalars
/// and arrays in the patch replace; a JSON <c>null</c> value or an absent key leaves the existing
/// value untouched. Pure; used to apply <c>update_&lt;T&gt;</c> patches onto the live artifact.
/// </summary>
public static class JsonMerge
{
    /// <summary>Returns a new node: <paramref name="patch"/> deep-merged onto <paramref name="target"/>.</summary>
    public static JsonNode? Merge(JsonNode? target, JsonNode? patch)
    {
        if (patch is null) return target;
        if (target is not JsonObject t || patch is not JsonObject p)
            return patch.DeepClone();   // scalar / array / type-mismatch -> replace

        var result = (JsonObject)t.DeepClone();
        foreach (var (key, value) in p)
        {
            if (value is null) continue;   // explicit null -> leave existing
            result[key] = result.TryGetPropertyValue(key, out var existing) && existing is not null
                ? Merge(existing.DeepClone(), value.DeepClone())
                : value.DeepClone();
        }
        return result;
    }
}
