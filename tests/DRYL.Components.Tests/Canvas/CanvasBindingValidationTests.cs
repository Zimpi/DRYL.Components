using System.Text.Json;
using DRYL.Components.Canvas;
using Xunit;

namespace DRYL.Components.Tests.Canvas;

/// <summary>
/// The model-facing side of a data binding: every mistake comes back as one corrective
/// sentence in the receipt, and the artifact still renders (an invalid node becomes a
/// placeholder — never a hard stop).
/// </summary>
public class CanvasBindingValidationTests
{
    private static readonly CanvasDataDescriptor Sales = new(
        "sales.byMonth", "Umsatz je Monat in Tsd €.", CanvasDataShape.Series,
        new[] { new CanvasParamInfo("year", "int", true), new CanvasParamInfo("region", "string", false) });

    private static readonly CanvasDataDescriptor Orders = new(
        "orders.open", "Offene Aufträge.", CanvasDataShape.Rows, Array.Empty<CanvasParamInfo>());

    private static CanvasValidationContext Context(params string[] fields) => new()
    {
        Sources = new[] { Sales, Orders },
        FieldNames = fields,
    };

    private static CanvasNode Bound(string type, string props, string data) =>
        JsonSerializer.Deserialize<CanvasNode>(
            $$"""{ "id": "n", "type": "{{type}}", "props": {{props}}, "data": {{data}} }""",
            CanvasJson.Options)!;

    [Fact]
    public void A_valid_binding_passes()
    {
        var node = Bound("lineChart", """{ "title": "Umsatz" }""",
            """{ "source": "sales.byMonth", "params": { "year": 2026 }, "refresh": "interval:30s" }""");

        Assert.Null(CanvasCatalog.Validate(node, Context()));
    }

    [Fact]
    public void An_unbound_node_is_unaffected_by_the_context()
    {
        // A2: everything that worked before Phase 1 must still work exactly as it did.
        var node = JsonSerializer.Deserialize<CanvasNode>(
            """{ "id": "s", "type": "stat", "props": { "label": "Umsatz", "value": "10k" } }""",
            CanvasJson.Options)!;

        Assert.Null(CanvasCatalog.Validate(node, Context()));
        Assert.Null(CanvasCatalog.Validate(node));
    }

    [Fact]
    public void Without_a_context_the_binding_itself_is_not_checked()
    {
        // The parameterless overload is what every pre-Phase-1 caller uses; it never learns
        // about sources, so it must not invent errors about them.
        var node = Bound("table", """{ "columns": ["Nr"] }""", """{ "source": "does.not.exist" }""");

        Assert.Null(CanvasCatalog.Validate(node));
        Assert.Contains("unknown data source", CanvasCatalog.Validate(node, Context()));
    }

    [Fact]
    public void An_unknown_source_lists_what_is_available()
    {
        var node = Bound("lineChart", "{}", """{ "source": "sales.byWeek", "params": { "year": 2026 } }""");

        var error = CanvasCatalog.Validate(node, Context());

        Assert.Contains("unknown data source 'sales.byWeek'", error);
        Assert.Contains("sales.byMonth", error);
        Assert.Contains("orders.open", error);
    }

    [Fact]
    public void A_shape_that_does_not_fit_the_node_type_names_both()
    {
        var node = Bound("lineChart", "{}", """{ "source": "orders.open" }""");

        var error = CanvasCatalog.Validate(node, Context());

        Assert.Contains("source 'orders.open' returns rows", error);
        Assert.Contains("a lineChart needs series", error);
    }

    [Fact]
    public void A_node_type_that_cannot_be_bound_says_so()
    {
        var node = Bound("markdown", """{ "content": "hi" }""", """{ "source": "orders.open" }""");

        Assert.Contains("cannot be bound", CanvasCatalog.Validate(node, Context()));
    }

    [Fact]
    public void A_missing_required_param_repeats_the_signature()
    {
        var node = Bound("lineChart", "{}", """{ "source": "sales.byMonth", "params": { "region": "north" } }""");

        var error = CanvasCatalog.Validate(node, Context());

        Assert.Contains("missing required param year", error);
        Assert.Contains("(year: int, region?: string)", error);
    }

    [Fact]
    public void An_optional_param_may_be_omitted()
    {
        var node = Bound("barChart", "{}", """{ "source": "sales.byMonth", "params": { "year": 2026 } }""");

        Assert.Null(CanvasCatalog.Validate(node, Context()));
    }

    [Fact]
    public void An_unknown_param_is_rejected()
    {
        var node = Bound("lineChart", "{}",
            """{ "source": "sales.byMonth", "params": { "year": 2026, "quarter": 2 } }""");

        Assert.Contains("has no parameter 'quarter'", CanvasCatalog.Validate(node, Context()));
    }

    [Fact]
    public void A_field_reference_must_point_at_an_interactive_node_of_this_artifact()
    {
        var node = Bound("lineChart", "{}",
            """{ "source": "sales.byMonth", "params": { "year": 2026, "region": { "$field": "gebiet" } } }""");

        var error = CanvasCatalog.Validate(node, Context("region", "year"));

        Assert.Contains("references field 'gebiet'", error);
        Assert.Contains("region", error);   // and says which fields do exist

        Assert.Null(CanvasCatalog.Validate(node, Context("gebiet")));
    }

    [Theory]
    [InlineData("interval:1s", "below the 5s floor")]
    [InlineData("every 30 seconds", "is invalid")]
    [InlineData("interval:30", "is invalid")]
    public void A_bad_refresh_is_corrected(string refresh, string expected)
    {
        var node = Bound("table", "{}", $$"""{ "source": "orders.open", "refresh": "{{refresh}}" }""");

        Assert.Contains(expected, CanvasCatalog.Validate(node, Context()));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("manual")]
    [InlineData("interval:5s")]
    [InlineData("interval:900s")]
    public void A_good_refresh_passes(string? refresh)
    {
        var data = refresh is null
            ? """{ "source": "orders.open" }"""
            : $$"""{ "source": "orders.open", "refresh": "{{refresh}}" }""";

        Assert.Null(CanvasCatalog.Validate(Bound("table", "{}", data), Context()));
    }

    [Fact]
    public void Field_names_are_collected_from_the_whole_subtree()
    {
        var root = JsonSerializer.Deserialize<CanvasNode>("""
            {"id":"root","type":"stack","children":[
                {"id":"c","type":"card","children":[
                    {"id":"f1","type":"select","props":{"name":"region","label":"Region","options":["a"]}}]},
                {"id":"f2","type":"toggle","props":{"name":"onlyOpen","label":"Nur offene"}},
                {"id":"s","type":"stat","props":{"label":"L","value":"1"}}]}
            """, CanvasJson.Options)!;

        var names = CanvasValidationContext.FieldNamesOf(root);

        Assert.Equal(new[] { "onlyOpen", "region" }, names.OrderBy(n => n, StringComparer.Ordinal));
    }
}
