using System.Text.Json;
using System.Text.Json.Serialization;

namespace DRYL.Components.Canvas;

/// <summary>Shared JSON handling for canvas specs and patches (camelCase, case-insensitive).</summary>
public static class CanvasJson
{
    /// <summary>Web-default serializer options used for every canvas (de)serialization.</summary>
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    /// <summary>Tolerant parse of a props/arguments JSON object; false on malformed input.</summary>
    public static bool TryParse<T>(string? json, out T? value) where T : class
    {
        value = null;
        if (string.IsNullOrWhiteSpace(json)) return false;
        try { value = JsonSerializer.Deserialize<T>(json, Options); }
        catch (JsonException) { return false; }
        return value is not null;
    }
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

    /// <summary>
    /// Optional binding to a registered host data source (see <c>AddDrylCanvasDataSource</c>).
    /// When present, the resolved result overwrites the props the shape owns; every other prop
    /// (<c>title</c>, <c>label</c>, <c>valueFormat</c>) stays as authored. A node without a
    /// binding behaves exactly as it always has.
    /// </summary>
    public CanvasDataBinding? Data { get; set; }

    /// <summary>
    /// Optional binding to a registered host action (see <c>AddDrylCanvasAction</c>). Only valid
    /// on a <c>button</c>. The AI authors and labels the button; only a user press ever runs the
    /// handler. A node without a binding behaves exactly as it always has.
    /// </summary>
    public CanvasActionBinding? Action { get; set; }

    /// <summary>
    /// Pinned by the user: the AI author may not change, move or remove this node, and may add
    /// nothing to it. Everything the user triggers — a data refresh, an action result, the node
    /// toolbar itself — still goes through (see <see cref="CanvasPatchAuthor"/>). Travels with the
    /// node into a saved document.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool Locked { get; set; }

    /// <summary>Transient exit flag: node plays its exit animation, then is purged. Never serialized.</summary>
    [JsonIgnore] public bool Removing { get; set; }

    /// <summary>
    /// Transient mutation stamp — bump it after every successful mutation of this node (its own
    /// props or its children list). Renderers memoize parse + validation work on node instance
    /// identity PLUS this stamp, so a mutation that leaves the stamp alone is invisible to them.
    /// Infrastructure: patchers and stream-reveal layers maintain it; never serialized.
    /// </summary>
    [JsonIgnore] public int Version { get; set; }
}

/// <summary>
/// A node's link to a registered host data source. Declared by the model as
/// <c>"data": { "source": "sales.byMonth", "params": { … }, "refresh": "interval:30s" }</c>.
/// </summary>
public sealed class CanvasDataBinding
{
    /// <summary>The registered source name, e.g. <c>sales.byMonth</c>.</summary>
    public string? Source { get; set; }

    /// <summary>Parameter object. Each value is a literal or a field reference
    /// <c>{ "$field": "&lt;name of an interactive node&gt;" }</c>.</summary>
    public JsonElement? Params { get; set; }

    /// <summary><c>manual</c> (default) or <c>interval:&lt;n&gt;s</c> with <c>n &gt;= 5</c>.</summary>
    public string? Refresh { get; set; }
}
