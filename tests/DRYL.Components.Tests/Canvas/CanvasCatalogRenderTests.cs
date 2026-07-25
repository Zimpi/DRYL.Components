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
    public void DataGrid_renders_headers_and_rows()
    {
        var cut = RenderSpec("""
            {"id":"g","type":"dataGrid","props":{
                "columns":["Auftrag","Status"],
                "rows":[["4711","offen"],["4712","erledigt"]]}}
            """);
        Assert.Contains("Auftrag", cut.Markup);
        Assert.Contains("4712", cut.Markup);
        Assert.Equal(2, cut.FindAll("tbody tr").Count);
    }

    [Fact]
    public void DataGrid_pages_when_rows_exceed_pagesize()
    {
        var rows = string.Join(",", Enumerable.Range(0, 15).Select(i => $"[\"r{i}\"]"));
        var cut = RenderSpec($$$"""
            {"id":"g","type":"dataGrid","props":{"columns":["A"],"rows":[{{{rows}}}],"pageSize":10}}
            """);
        Assert.Equal(10, cut.FindAll("tbody tr").Count);
        Assert.NotEmpty(cut.FindAll(".tbl-footer"));
    }

    [Fact]
    public void DataGrid_hides_paging_when_rows_fit()
    {
        var cut = RenderSpec("""
            {"id":"g","type":"dataGrid","props":{"columns":["A"],"rows":[["1"]]}}
            """);
        Assert.Empty(cut.FindAll(".tbl-footer"));
    }

    [Fact]
    public void DataGrid_sorts_on_header_click()
    {
        var cut = RenderSpec("""
            {"id":"g","type":"dataGrid","props":{"columns":["A"],"rows":[["b"],["a"]]}}
            """);
        cut.FindAll(".tbl-th-clickable")[0].Click();
        var cells = cut.FindAll("tbody td").Select(td => td.TextContent.Trim()).ToList();
        Assert.Equal("a", cells[0]);
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
