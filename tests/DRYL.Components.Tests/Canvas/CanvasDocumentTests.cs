using System.Text.Json;
using DRYL.Components.Canvas;
using Xunit;

namespace DRYL.Components.Tests.Canvas;

/// <summary>A dashboard that survives a reload: capture, serialize, restore.</summary>
public class CanvasDocumentTests
{
    private static CanvasSpec Spec(string title, string nodeId = "r") =>
        JsonSerializer.Deserialize<CanvasSpec>(
            $$"""
            { "title": "{{title}}", "root": { "id": "{{nodeId}}", "type": "stack", "children": [
                { "id": "{{nodeId}}-c", "type": "lineChart",
                  "data": { "source": "sales.byMonth", "params": { "year": 2026 } } }
            ] } }
            """,
            CanvasJson.Options)!;

    private static CanvasWorkspace TwoViews()
    {
        var ws = new CanvasWorkspace();
        var a = ws.Open("Overview", "Chart");
        a.Spec = Spec("Overview", "a");
        var b = ws.Open("Order 4711");
        b.Spec = Spec("Order 4711", "b");
        ws.Activate(a.Id);
        return ws;
    }

    [Fact]
    public void Capture_takes_every_view_the_active_id_and_a_title()
    {
        var doc = CanvasDocument.Capture(TwoViews(), "My dashboard");

        Assert.Equal(CanvasDocument.CurrentSchema, doc.Schema);
        Assert.Equal("My dashboard", doc.Title);
        Assert.Equal(2, doc.Views!.Count);
        Assert.Equal("overview", doc.ActiveId);
        Assert.Equal("Chart", doc.Views[0].Icon);
        Assert.NotEqual(default, doc.SavedAt);
    }

    [Fact]
    public void Capture_defaults_the_title_to_the_active_view()
    {
        Assert.Equal("Overview", CanvasDocument.Capture(TwoViews()).Title);
        Assert.Equal("Canvas", CanvasDocument.Capture(new CanvasWorkspace()).Title);
    }

    [Fact]
    public void Capture_is_a_deep_copy()
    {
        var ws = TwoViews();
        var doc = CanvasDocument.Capture(ws);

        ws.Active!.Spec!.Title = "changed live";

        Assert.Equal("Overview", doc.Views![0].Spec!.Title);
    }

    [Fact]
    public void Capture_skips_a_view_that_is_on_its_way_out()
    {
        var ws = TwoViews();
        ws.Close("order-4711");

        var doc = CanvasDocument.Capture(ws);

        Assert.Single(doc.Views!);
        Assert.Equal("overview", doc.Views![0].Id);
    }

    [Fact]
    public void A_roundtrip_keeps_views_data_bindings_and_the_active_view()
    {
        var json = CanvasDocument.Capture(TwoViews(), "My dashboard").ToJson();

        Assert.True(CanvasDocument.TryFromJson(json, out var doc, out var error));
        Assert.Null(error);
        Assert.Equal("My dashboard", doc!.Title);
        Assert.Equal("overview", doc.ActiveId);
        Assert.Equal("sales.byMonth", doc.Views![0].Spec!.Root!.Children![0].Data!.Source);
    }

    [Fact]
    public void Restore_rebuilds_the_workspace()
    {
        var doc = CanvasDocument.Capture(TwoViews(), "My dashboard");
        var target = new CanvasWorkspace();
        target.Open("Stale");

        doc.Restore(target);

        Assert.Equal(2, target.Views.Count);
        Assert.Equal("overview", target.ActiveId);
        Assert.Equal("Order 4711", target.Views[1].Title);
        Assert.Equal("Chart", target.Views[0].Icon);
    }

    [Fact]
    public void Restore_hands_the_workspace_its_own_spec_instances()
    {
        var doc = CanvasDocument.Capture(TwoViews());
        var target = new CanvasWorkspace();
        doc.Restore(target);

        target.Active!.Spec!.Title = "changed live";

        Assert.NotEqual("changed live", doc.Views![0].Spec!.Title);
    }

    [Fact]
    public void TryFromJson_rejects_garbage()
    {
        Assert.False(CanvasDocument.TryFromJson("not json", out var doc, out var error));
        Assert.Null(doc);
        Assert.Contains("not valid JSON", error);
    }

    [Fact]
    public void TryFromJson_rejects_a_document_from_a_newer_build()
    {
        var json = $$"""{ "schema": {{CanvasDocument.CurrentSchema + 1}}, "views": [] }""";

        Assert.False(CanvasDocument.TryFromJson(json, out _, out var error));
        Assert.Contains("newer version of DRYL", error);
    }

    [Fact]
    public void TryFromJson_rejects_a_document_without_schema_or_views()
    {
        Assert.False(CanvasDocument.TryFromJson("""{ "schema": 0, "views": [] }""", out _, out var noSchema));
        Assert.Contains("no schema version", noSchema);

        Assert.False(CanvasDocument.TryFromJson("""{ "schema": 1 }""", out _, out var noViews));
        Assert.Contains("no views", noViews);
    }

    [Fact]
    public void A_document_without_a_schema_field_is_read_as_the_current_one()
    {
        var json = """{ "views": [ { "id": "a", "title": "A" } ] }""";

        Assert.True(CanvasDocument.TryFromJson(json, out var doc, out _));
        Assert.Equal(CanvasDocument.CurrentSchema, doc!.Schema);
    }

    [Fact]
    public void AsTemplate_drops_the_id_and_takes_a_new_title()
    {
        var doc = CanvasDocument.Capture(TwoViews());
        doc.Id = "abc";

        var template = doc.AsTemplate("Copy of my dashboard");

        Assert.Null(template.Id);
        Assert.Equal("Copy of my dashboard", template.Title);
        Assert.Equal(default, template.SavedAt);
        Assert.Equal(2, template.Views!.Count);
        Assert.NotSame(doc.Views![0].Spec, template.Views[0].Spec);
    }

    [Fact]
    public void Capture_folds_live_field_values_into_the_value_props()
    {
        var ws = new CanvasWorkspace();
        var view = ws.Open("Form");
        view.Spec = JsonSerializer.Deserialize<CanvasSpec>(
            """
            { "root": { "id": "r", "type": "stack", "children": [
                { "id": "t", "type": "inputText", "props": { "name": "customer", "label": "Kunde", "value": "old" } },
                { "id": "s", "type": "select",    "props": { "name": "status", "options": ["a", "b"] } },
                { "id": "l", "type": "slider",    "props": { "name": "amount", "min": 0, "max": 10 } },
                { "id": "g", "type": "toggle",    "props": { "name": "rush", "label": "Eilt" } }
            ] } }
            """, CanvasJson.Options)!;

        var form = new CanvasFormState();
        form.Set("customer", "ACME");
        form.Set("status", "b");
        form.Set("amount", 7d);
        form.Set("rush", true);

        var doc = CanvasDocument.Capture(ws, form: form);
        var children = doc.Views![0].Spec!.Root!.Children!;

        Assert.Equal("ACME", children[0].Props!.Value.GetProperty("value").GetString());
        Assert.Equal("b", children[1].Props!.Value.GetProperty("value").GetString());
        Assert.Equal(7d, children[2].Props!.Value.GetProperty("value").GetDouble());
        Assert.True(children[3].Props!.Value.GetProperty("value").GetBoolean());
        Assert.Equal("Kunde", children[0].Props!.Value.GetProperty("label").GetString());
    }
}
