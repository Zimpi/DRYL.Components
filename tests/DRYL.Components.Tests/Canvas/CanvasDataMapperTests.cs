using System.Text.Json;
using DRYL.Components.Canvas;
using Xunit;

namespace DRYL.Components.Tests.Canvas;

public class CanvasDataMapperTests
{
    private static JsonElement Props(string json) => JsonDocument.Parse(json).RootElement.Clone();

    private static JsonElement Map(string nodeType, string baseProps, CanvasData data,
                                   out string? error, out bool truncated) =>
        CanvasDataMapper.Apply(nodeType, Props(baseProps), data, out error, out truncated)!.Value;

    [Fact]
    public void Scalar_fills_a_stat_and_leaves_its_label_alone()
    {
        var props = Map("stat", """{ "label": "Umsatz", "value": "—" }""",
            CanvasData.Scalar(184_000, "€184k", "+12.4%", "up"), out var error, out _);

        Assert.Null(error);
        Assert.Equal("€184k", props.GetProperty("value").GetString());
        Assert.Equal("+12.4%", props.GetProperty("delta").GetString());
        Assert.Equal("up", props.GetProperty("direction").GetString());
        Assert.Equal("Umsatz", props.GetProperty("label").GetString());   // presentation survives
    }

    [Fact]
    public void Scalar_fills_a_badge_text_and_a_progress_value()
    {
        var badge = Map("badge", """{ "kind": "success" }""", CanvasData.Scalar("12 offen"), out var e1, out _);
        Assert.Null(e1);
        Assert.Equal("12 offen", badge.GetProperty("text").GetString());
        Assert.Equal("success", badge.GetProperty("kind").GetString());

        var progress = Map("progress", """{ "label": "Auslastung" }""", CanvasData.Scalar(73), out var e2, out _);
        Assert.Null(e2);
        Assert.Equal(73, progress.GetProperty("value").GetDouble());
    }

    [Fact]
    public void Rows_fill_a_dataGrid_and_cap_at_1000()
    {
        var data = CanvasData.Rows(new[] { "A" }, Enumerable.Range(0, 1200).Select(i => new[] { $"{i}" }));
        var props = Map("dataGrid", "{}", data, out var error, out var truncated);
        Assert.Null(error);
        Assert.True(truncated);
        Assert.Equal(1000, props.GetProperty("rows").GetArrayLength());
    }

    [Fact]
    public void Rows_fill_a_list_with_title_and_text()
    {
        var data = CanvasData.Rows(new[] { "Titel", "Text" }, new[] { new[] { "Auftrag", "offen" } });
        var props = Map("list", "{}", data, out var error, out _);
        Assert.Null(error);
        var item = props.GetProperty("items")[0];
        Assert.Equal("Auftrag", item.GetProperty("title").GetString());
        Assert.Equal("offen", item.GetProperty("text").GetString());
    }

    [Fact]
    public void Rows_with_one_column_fill_a_list_without_text()
    {
        var data = CanvasData.Rows(new[] { "Titel" }, new[] { new[] { "Auftrag" } });
        var props = Map("list", "{}", data, out var error, out _);
        Assert.Null(error);
        Assert.False(props.GetProperty("items")[0].TryGetProperty("text", out _));
    }

    [Fact]
    public void Rows_fill_keyValue_pairs()
    {
        var data = CanvasData.Rows(new[] { "K", "V" }, new[] { new[] { "Status", "offen" } });
        var props = Map("keyValue", "{}", data, out var error, out _);
        Assert.Null(error);
        var pair = props.GetProperty("pairs")[0];
        Assert.Equal("Status", pair.GetProperty("key").GetString());
        Assert.Equal("offen", pair.GetProperty("value").GetString());
    }

    [Fact]
    public void Rows_with_three_columns_cannot_fill_keyValue()
    {
        var data = CanvasData.Rows(new[] { "A", "B", "C" }, new[] { new[] { "1", "2", "3" } });
        CanvasDataMapper.Apply("keyValue", Props("{}"), data, out var error, out _);
        Assert.Contains("2-column", error);
    }

    [Fact]
    public void Rows_sample_has_two_columns() =>
        Assert.Equal(2, ((CanvasRowData)CanvasDataMapper.Sample(CanvasDataShape.Rows)).Columns.Count);

    [Fact]
    public void A_text_only_scalar_cannot_fill_a_progress()
    {
        CanvasDataMapper.Apply("progress", Props("{}"), CanvasData.Scalar("gut"), out var error, out _);

        Assert.Contains("numeric value", error);
    }

    [Fact]
    public void Progress_clamps_into_the_catalog_range()
    {
        var over = Map("progress", "{}", CanvasData.Scalar(140), out _, out _);
        var under = Map("progress", "{}", CanvasData.Scalar(-5), out _, out _);

        Assert.Equal(100, over.GetProperty("value").GetDouble());
        Assert.Equal(0, under.GetProperty("value").GetDouble());
    }

    [Fact]
    public void Series_fills_labels_and_series_and_keeps_title_and_format()
    {
        var props = Map("lineChart", """{ "title": "Umsatz", "valueFormat": "N0" }""",
            CanvasData.Series(new[] { "Jan", "Feb" }, ("Umsatz", new[] { 1.5, 2.5 })),
            out var error, out _);

        Assert.Null(error);
        Assert.Equal(new[] { "Jan", "Feb" }, props.GetProperty("labels").EnumerateArray().Select(e => e.GetString()));
        var series = Assert.Single(props.GetProperty("series").EnumerateArray().ToList());
        Assert.Equal("Umsatz", series.GetProperty("name").GetString());
        Assert.Equal(new[] { 1.5, 2.5 }, series.GetProperty("data").EnumerateArray().Select(e => e.GetDouble()));
        Assert.Equal("Umsatz", props.GetProperty("title").GetString());
        Assert.Equal("N0", props.GetProperty("valueFormat").GetString());
    }

    [Fact]
    public void Segments_fill_a_donut()
    {
        var props = Map("donutChart", "{}",
            CanvasData.Segments(new[] { ("Nord", 3d), ("Süd", 7d) }), out var error, out _);

        Assert.Null(error);
        var segments = props.GetProperty("segments").EnumerateArray().ToList();
        Assert.Equal(2, segments.Count);
        Assert.Equal("Nord", segments[0].GetProperty("label").GetString());
        Assert.Equal(7d, segments[1].GetProperty("value").GetDouble());
    }

    [Fact]
    public void Rows_fill_a_table()
    {
        var props = Map("table", "{}",
            CanvasData.Rows(new[] { "Nr", "Kunde" }, new[] { new[] { "4711", "Meier" } }),
            out var error, out _);

        Assert.Null(error);
        Assert.Equal(new[] { "Nr", "Kunde" }, props.GetProperty("columns").EnumerateArray().Select(e => e.GetString()));
        var row = Assert.Single(props.GetProperty("rows").EnumerateArray().ToList());
        Assert.Equal(new[] { "4711", "Meier" }, row.EnumerateArray().Select(e => e.GetString()));
    }

    [Fact]
    public void Rows_beyond_the_catalog_ceiling_are_cut_and_flagged()
    {
        var many = Enumerable.Range(0, 45).Select(i => new[] { i.ToString() });

        var props = Map("table", "{}", CanvasData.Rows(new[] { "Nr" }, many), out var error, out var truncated);

        Assert.Null(error);
        Assert.True(truncated);
        Assert.Equal(30, props.GetProperty("rows").GetArrayLength());
    }

    [Theory]
    [InlineData("lineChart", "scalar")]
    [InlineData("table", "series")]
    [InlineData("stat", "rows")]
    [InlineData("donutChart", "rows")]
    public void A_mismatched_shape_reports_which_types_it_does_fit(string nodeType, string shape)
    {
        CanvasData data = shape switch
        {
            "scalar" => CanvasData.Scalar(1),
            "series" => CanvasData.Series(new[] { "a" }, ("s", new[] { 1d })),
            _ => CanvasData.Rows(new[] { "c" }, Array.Empty<string[]>()),
        };

        CanvasDataMapper.Apply(nodeType, Props("{}"), data, out var error, out _);

        Assert.Contains(shape, error);
        Assert.Contains(nodeType, error);
    }

    [Fact]
    public void The_mapped_props_still_pass_catalog_validation()
    {
        // The whole point: whatever the mapper writes must be a node the renderer accepts.
        var node = new CanvasNode
        {
            Id = "c", Type = "barChart",
            Props = CanvasDataMapper.Apply("barChart", Props("""{ "title": "Umsatz" }"""),
                CanvasData.Series(new[] { "Jan", "Feb" }, ("Ist", new[] { 1d, 2d }), ("Plan", new[] { 2d, 3d })),
                out _, out _),
        };

        Assert.Null(CanvasCatalog.Validate(node));
    }
}
