using System.Text.Json;
using System.Text.Json.Serialization;

namespace DRYL.Components.Agents;

/// <summary>Shared JSON handling for canvas specs and patches (camelCase, case-insensitive).</summary>
public static class CanvasJson
{
    /// <summary>Web-default serializer options used for every canvas (de)serialization.</summary>
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);
}

/// <summary>An AI-generated artifact: a titled tree of catalog nodes rendered by <c>DrylAiCanvas</c>.</summary>
public sealed class CanvasSpec
{
    /// <summary>Short artifact title shown in the canvas header.</summary>
    public string? Title { get; set; }

    /// <summary>The root node — by convention a <c>stack</c> container.</summary>
    public CanvasNode? Root { get; set; }
}

/// <summary>One node of a canvas artifact. <see cref="Type"/> selects a curated catalog entry.</summary>
public sealed class CanvasNode
{
    /// <summary>Stable unique id — the anchor for patches, move animations and interaction events.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Catalog type key, e.g. <c>stack</c>, <c>stat</c>, <c>lineChart</c>, <c>button</c>.</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>Type-specific properties; parsed and validated by <c>CanvasCatalog</c>.</summary>
    public JsonElement? Props { get; set; }

    /// <summary>Child nodes (container types only).</summary>
    public List<CanvasNode>? Children { get; set; }

    /// <summary>Transient exit flag: node plays its exit animation, then is purged. Never serialized.</summary>
    [JsonIgnore] public bool Removing { get; set; }

    /// <summary>Transient mutation stamp — bumped by every successful patcher/reveal/purge
    /// mutation touching this node (own props or its children list). Renderers memoize
    /// parse + validation work on it. Never serialized.</summary>
    [JsonIgnore] internal int Version { get; set; }
}
