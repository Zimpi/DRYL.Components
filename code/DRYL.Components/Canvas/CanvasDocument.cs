using System.Text.Json;
using System.Text.Json.Nodes;

namespace DRYL.Components.Canvas;

/// <summary>One view inside a <see cref="CanvasDocument"/>: what a workspace chip needs to come back.</summary>
public sealed class CanvasDocumentView
{
    /// <summary>The view's stable id (the slug of its title).</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>The label shown on the chip.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Optional <c>DrylIcon</c> name shown left of the title.</summary>
    public string? Icon { get; set; }

    /// <summary>The artifact this view showed.</summary>
    public CanvasSpec? Spec { get; set; }
}

/// <summary>
/// A saved <see cref="CanvasWorkspace"/> — every named view with its artifact, plus which one was
/// open. This is what makes a dashboard survive a reload, a user switch and a deployment.
/// </summary>
/// <remarks>
/// Data bindings travel with the document; the numbers do not. A restored document asks the host's
/// registered sources for fresh values, so a document is never a stale copy of a database.
/// </remarks>
public sealed class CanvasDocument
{
    /// <summary>The document schema this build writes and is able to read.</summary>
    public const int CurrentSchema = 1;

    /// <summary>Schema version of this document.</summary>
    public int Schema { get; set; } = CurrentSchema;

    /// <summary>Store key; null until an <see cref="ICanvasDocumentStore"/> has saved it.</summary>
    public string? Id { get; set; }

    /// <summary>Human-readable document title.</summary>
    public string? Title { get; set; }

    /// <summary>When this snapshot was taken (UTC).</summary>
    public DateTimeOffset SavedAt { get; set; }

    /// <summary>The views, in the order they were opened.</summary>
    public List<CanvasDocumentView>? Views { get; set; }

    /// <summary>Id of the view that was open.</summary>
    public string? ActiveId { get; set; }

    /// <summary>
    /// Takes a snapshot of <paramref name="workspace"/>. Views that are already closing are left
    /// out — what is animating away does not belong in a document.
    /// </summary>
    /// <param name="workspace">The workspace to capture.</param>
    /// <param name="title">Document title; defaults to the active view's title.</param>
    /// <param name="form">Live field values to fold into the active view's <c>value</c> props, so
    /// a restored document shows what the user had typed. Optional.</param>
    public static CanvasDocument Capture(CanvasWorkspace workspace,
                                         string? title = null,
                                         CanvasFormState? form = null)
    {
        ArgumentNullException.ThrowIfNull(workspace);

        var views = new List<CanvasDocumentView>();
        foreach (var view in workspace.Views)
        {
            if (view.Removing) continue;

            var spec = Copy(view.Spec);
            if (form is not null && view.Id == workspace.ActiveId && spec?.Root is not null)
                FoldFields(spec.Root, form);

            views.Add(new CanvasDocumentView
            {
                Id = view.Id,
                Title = view.Title,
                Icon = view.Icon,
                Spec = spec,
            });
        }

        return new CanvasDocument
        {
            Title = title ?? workspace.Active?.Title ?? "Canvas",
            SavedAt = DateTimeOffset.UtcNow,
            Views = views,
            ActiveId = views.Any(v => v.Id == workspace.ActiveId)
                ? workspace.ActiveId
                : views.FirstOrDefault()?.Id,
        };
    }

    /// <summary>
    /// Replaces everything in <paramref name="workspace"/> with this document. Each view gets its
    /// own spec instance, so the live workspace and the document can never drift into each other.
    /// </summary>
    /// <param name="workspace">The workspace to fill.</param>
    public void Restore(CanvasWorkspace workspace)
    {
        ArgumentNullException.ThrowIfNull(workspace);

        workspace.Clear();
        foreach (var v in Views ?? new List<CanvasDocumentView>())
            workspace.Open(v.Title, v.Icon).Spec = Copy(v.Spec);

        if (ActiveId is not null) workspace.Activate(ActiveId);
    }

    /// <summary>Serializes the document for an <see cref="ICanvasDocumentStore"/>.</summary>
    public string ToJson() => JsonSerializer.Serialize(this, CanvasJson.Options);

    /// <summary>
    /// Reads a document written by <see cref="ToJson"/>. The only supported entry point — it is
    /// where the schema gate lives.
    /// </summary>
    /// <param name="json">The stored document.</param>
    /// <param name="document">The parsed document, or null on failure.</param>
    /// <param name="error">A host-facing message explaining the refusal, or null on success.</param>
    public static bool TryFromJson(string json, out CanvasDocument? document, out string? error)
    {
        document = null;
        error = null;

        CanvasDocument? parsed;
        try { parsed = JsonSerializer.Deserialize<CanvasDocument>(json, CanvasJson.Options); }
        catch (JsonException) { parsed = null; }

        if (parsed is null)
        {
            error = "The document could not be read: it is not valid JSON.";
            return false;
        }

        if (parsed.Schema <= 0)
        {
            error = "The document has no schema version.";
            return false;
        }

        if (parsed.Schema > CurrentSchema)
        {
            error = FormattableString.Invariant(
                $"This document was written by a newer version of DRYL (schema {parsed.Schema}, this build reads up to {CurrentSchema}).");
            return false;
        }

        if (parsed.Views is not { Count: > 0 })
        {
            error = "The document contains no views.";
            return false;
        }

        document = parsed;
        return true;
    }

    /// <summary>
    /// A copy of this document as the starting point of a new one: same views, no store id, new
    /// title. That is how an application ships its standard dashboards.
    /// </summary>
    /// <param name="title">Title of the new document.</param>
    public CanvasDocument AsTemplate(string title) => new()
    {
        Schema = Schema,
        Id = null,
        Title = title,
        SavedAt = default,
        ActiveId = ActiveId,
        Views = (Views ?? new List<CanvasDocumentView>())
            .Select(v => new CanvasDocumentView { Id = v.Id, Title = v.Title, Icon = v.Icon, Spec = Copy(v.Spec) })
            .ToList(),
    };

    // A round trip is the cheapest deep copy that also proves the spec survives serialization.
    private static CanvasSpec? Copy(CanvasSpec? spec) =>
        spec is null
            ? null
            : JsonSerializer.Deserialize<CanvasSpec>(
                JsonSerializer.Serialize(spec, CanvasJson.Options), CanvasJson.Options);

    // Writes the live field values into the nodes' `value` props. Interactive nodes seed their
    // form value from that prop (CanvasNodeView.SeedFormOnce), so a restored document fills its
    // own form without a single line in the loading path.
    private static void FoldFields(CanvasNode node, CanvasFormState form)
    {
        if (CanvasCatalog.IsInteractive(node.Type)
            && node.Props is { } props
            && props.ValueKind == JsonValueKind.Object
            && props.TryGetProperty("name", out var nameProp)
            && nameProp.GetString() is { Length: > 0 } name
            && form.Get(name) is { } value)
        {
            var obj = JsonNode.Parse(props.GetRawText())!.AsObject();
            obj["value"] = value switch
            {
                bool b => JsonValue.Create(b),
                double d => JsonValue.Create(d),
                int i => JsonValue.Create((double)i),
                _ => JsonValue.Create(value.ToString()),
            };
            node.Props = JsonSerializer.Deserialize<JsonElement>(obj.ToJsonString(), CanvasJson.Options);
        }

        foreach (var child in node.Children ?? new List<CanvasNode>())
            FoldFields(child, form);
    }
}
