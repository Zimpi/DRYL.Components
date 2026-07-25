using System.Text.Json;
using Bunit;
using DRYL.Components.Canvas;
using Xunit;

namespace DRYL.Components.Tests.Canvas;

/// <summary>Render happy-paths of the phase-4 catalog types (spec 2026-07-25-canvas-catalog-design).</summary>
public class CanvasCatalogRenderTests : BunitContext
{
    public CanvasCatalogRenderTests()
    {
        JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddDrylComponents();
    }

    private static CanvasSpec Parse(string json) =>
        JsonSerializer.Deserialize<CanvasSpec>(json, CanvasJson.Options)!;

    private IRenderedComponent<DrylCanvas> RenderSpec(string rootJson) =>
        Render<DrylCanvas>(p => p.Add(x => x.Spec,
            Parse($$"""{"title":"T","root":{{rootJson}}}""")));

    [Fact]
    public void Kpi_renders_one_stat_per_item()
    {
        var cut = RenderSpec("""
            {"id":"k","type":"kpi","props":{"items":[
                {"label":"Umsatz","value":"48k","delta":"+4%","direction":"up"},
                {"label":"Aufträge","value":"312"}]}}
            """);
        Assert.Equal(2, cut.FindAll(".canvas-kpi .stat").Count);
    }

    [Fact]
    public void List_renders_items_with_title_and_text()
    {
        var cut = RenderSpec("""
            {"id":"l","type":"list","props":{"items":[
                {"title":"Auftrag 4711","text":"offen","icon":"Package"}]}}
            """);
        Assert.Contains("Auftrag 4711", cut.Markup);
        Assert.Contains("offen", cut.Markup);
    }

    [Fact]
    public void KeyValue_renders_terms_and_values()
    {
        var cut = RenderSpec("""
            {"id":"kv","type":"keyValue","props":{"pairs":[
                {"key":"Status","value":"offen"},{"key":"Kunde","value":"ACME"}],"columns":2}}
            """);
        Assert.Contains("Status", cut.Markup);
        Assert.Contains("ACME", cut.Markup);
    }

    [Fact]
    public void Image_renders_img_with_alt_and_caption()
    {
        var cut = RenderSpec("""
            {"id":"i","type":"image","props":{
                "src":"https://example.com/a.png","alt":"Diagramm","caption":"Abb. 1"}}
            """);
        Assert.Contains("alt=\"Diagramm\"", cut.Markup);
        Assert.Contains("Abb. 1", cut.Markup);
    }

    [Fact]
    public void Code_renders_code_block()
    {
        var cut = RenderSpec("""
            {"id":"c","type":"code","props":{"code":"SELECT 1;","language":"sql"}}
            """);
        // The highlighter splits the source into token spans, so assert on text content.
        Assert.Contains("SELECT 1;", cut.Find(".code-block-pre").TextContent);
        Assert.Contains("sql", cut.Find(".code-block-lang").TextContent);
    }

    [Fact]
    public void Accordion_renders_sections_and_open_index()
    {
        var cut = RenderSpec("""
            {"id":"a","type":"accordion","props":{"labels":["Erster","Zweiter"],"open":0},"children":[
                {"id":"c1","type":"markdown","props":{"content":"Inhalt eins"}},
                {"id":"c2","type":"markdown","props":{"content":"Inhalt zwei"}}]}
            """);
        Assert.Contains("Erster", cut.Markup);
        Assert.Contains("Zweiter", cut.Markup);
        var headers = cut.FindAll("[aria-expanded]");
        Assert.Contains(headers, h => h.GetAttribute("aria-expanded") == "true");
    }

    [Fact]
    public void EmptyState_renders_title_and_description()
    {
        var cut = RenderSpec("""
            {"id":"e","type":"emptyState","props":{
                "title":"Noch keine Aufträge","description":"Lege den ersten an."}}
            """);
        Assert.Contains("Noch keine Aufträge", cut.Markup);
    }
}
