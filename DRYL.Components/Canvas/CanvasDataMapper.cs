using System.Text.Json;
using System.Text.Json.Nodes;

namespace DRYL.Components.Canvas;

/// <summary>
/// Maps a data source's result shape onto a node's props. The shape owns exactly the props that
/// <em>are</em> the data; everything else the model authored (<c>title</c>, <c>label</c>,
/// <c>valueFormat</c>, <c>kind</c>) is presentation and survives untouched.
/// </summary>
internal static class CanvasDataMapper
{
    /// <summary>The catalog's row ceiling — a <c>table</c> renders at most this many rows.</summary>
    internal const int MaxTableRows = 30;

    /// <summary>A bound <c>dataGrid</c> renders at most this many rows.</summary>
    internal const int MaxGridRows = 1000;

    /// <summary>A bound <c>list</c> renders at most this many items.</summary>
    internal const int MaxListItems = 50;

    /// <summary>A bound <c>keyValue</c> renders at most this many pairs.</summary>
    internal const int MaxKeyValuePairs = 20;

    /// <summary>The node types each shape may be bound to.</summary>
    public static bool Allows(CanvasDataShape shape, string nodeType) => shape switch
    {
        CanvasDataShape.Scalar => nodeType is "stat" or "badge" or "progress",
        CanvasDataShape.Series => nodeType is "lineChart" or "areaChart" or "barChart",
        CanvasDataShape.Segments => nodeType is "donutChart",
        CanvasDataShape.Rows => nodeType is "table" or "dataGrid" or "list" or "keyValue",
        _ => false,
    };

    /// <summary>The node types a shape may be bound to, for an error sentence.</summary>
    public static string AllowedTypes(CanvasDataShape shape) => shape switch
    {
        CanvasDataShape.Scalar => "stat, badge or progress",
        CanvasDataShape.Series => "lineChart, areaChart or barChart",
        CanvasDataShape.Segments => "donutChart",
        CanvasDataShape.Rows => "table, dataGrid, list or keyValue",
        _ => "nothing",
    };

    /// <summary>The wire name of a shape, as it appears in the prompt block and in error sentences.</summary>
    public static string ShapeName(CanvasDataShape shape) => shape switch
    {
        CanvasDataShape.Scalar => "scalar",
        CanvasDataShape.Series => "series",
        CanvasDataShape.Segments => "segments",
        _ => "rows",
    };

    /// <summary>
    /// A minimal stand-in result of <paramref name="shape"/>. Validation runs before any data
    /// exists, so this fills the props the shape owns and lets everything else be checked for real.
    /// </summary>
    public static CanvasData Sample(CanvasDataShape shape) => shape switch
    {
        CanvasDataShape.Scalar => CanvasData.Scalar(0, "0"),
        CanvasDataShape.Series => CanvasData.Series(new[] { "—" }, ("—", new[] { 0d })),
        CanvasDataShape.Segments => CanvasData.Segments(new[] { ("—", 1d) }),
        // Two columns, so a keyValue stand-in passes authoring-time validation; a real source
        // with a different column count still errors at bind time, inline at the node.
        _ => CanvasData.Rows(new[] { "—", "—" }, new[] { new[] { "—", "—" } }),
    };

    /// <summary>
    /// Returns <paramref name="baseProps"/> with the props this shape owns overwritten by
    /// <paramref name="data"/>. On a mismatch returns the untouched base props and sets
    /// <paramref name="error"/> to a short, user-facing sentence.
    /// </summary>
    public static JsonElement? Apply(string nodeType, JsonElement? baseProps, CanvasData data,
                                     out string? error, out bool truncated)
    {
        error = null;
        truncated = false;

        if (!Allows(data.Shape, nodeType))
        {
            error = $"a {ShapeName(data.Shape)} source cannot fill a {nodeType} — it fits {AllowedTypes(data.Shape)}.";
            return baseProps;
        }

        var props = ToObject(baseProps);

        switch (data)
        {
            case CanvasScalarData s:
                switch (nodeType)
                {
                    case "stat":
                        props["value"] = s.Text;
                        if (s.Delta is not null) props["delta"] = s.Delta;
                        if (s.Direction is not null) props["direction"] = s.Direction;
                        break;
                    case "badge":
                        props["text"] = s.Text;
                        break;
                    case "progress":
                        if (s.Value is not { } v)
                        {
                            error = "a progress node needs a numeric value — this source returned text only.";
                            return baseProps;
                        }
                        props["value"] = Math.Clamp(v, 0, 100);
                        break;
                }
                break;

            case CanvasSeriesData series:
                props["labels"] = new JsonArray(series.Labels.Select(l => (JsonNode?)JsonValue.Create(l)).ToArray());
                props["series"] = new JsonArray(series.Series.Select(s => (JsonNode?)new JsonObject
                {
                    ["name"] = s.Name,
                    ["data"] = new JsonArray(s.Data.Select(d => (JsonNode?)JsonValue.Create(d)).ToArray()),
                }).ToArray());
                break;

            case CanvasSegmentData seg:
                props["segments"] = new JsonArray(seg.Segments.Select(s => (JsonNode?)new JsonObject
                {
                    ["label"] = s.Label,
                    ["value"] = JsonValue.Create(s.Value),
                }).ToArray());
                break;

            case CanvasRowData rows:
                switch (nodeType)
                {
                    case "table" or "dataGrid":
                    {
                        var cap = nodeType == "table" ? MaxTableRows : MaxGridRows;
                        props["columns"] = new JsonArray(rows.Columns.Select(c => (JsonNode?)JsonValue.Create(c)).ToArray());
                        var kept = rows.Rows;
                        if (kept.Count > cap)
                        {
                            kept = kept.Take(cap).ToList();
                            truncated = true;
                        }
                        props["rows"] = new JsonArray(kept.Select(r => (JsonNode?)new JsonArray(
                            r.Select(c => (JsonNode?)JsonValue.Create(c ?? string.Empty)).ToArray())).ToArray());
                        break;
                    }
                    case "list":
                    {
                        var kept = rows.Rows;
                        if (kept.Count > MaxListItems)
                        {
                            kept = kept.Take(MaxListItems).ToList();
                            truncated = true;
                        }
                        props["items"] = new JsonArray(kept.Select(r =>
                        {
                            var item = new JsonObject { ["title"] = r.Count > 0 ? r[0] : string.Empty };
                            if (r.Count > 1 && !string.IsNullOrEmpty(r[1])) item["text"] = r[1];
                            return (JsonNode?)item;
                        }).ToArray());
                        break;
                    }
                    case "keyValue":
                    {
                        if (rows.Columns.Count != 2)
                        {
                            error = FormattableString.Invariant(
                                $"a keyValue needs a 2-column rows source — this source returns {rows.Columns.Count} columns.");
                            return baseProps;
                        }
                        var kept = rows.Rows;
                        if (kept.Count > MaxKeyValuePairs)
                        {
                            kept = kept.Take(MaxKeyValuePairs).ToList();
                            truncated = true;
                        }
                        props["pairs"] = new JsonArray(kept.Select(r => (JsonNode?)new JsonObject
                        {
                            ["key"] = r.Count > 0 ? r[0] : string.Empty,
                            ["value"] = r.Count > 1 ? r[1] : string.Empty,
                        }).ToArray());
                        break;
                    }
                }
                break;
        }

        return JsonSerializer.SerializeToElement(props);
    }

    private static JsonObject ToObject(JsonElement? props) =>
        props is { } p && p.ValueKind == JsonValueKind.Object
            ? JsonNode.Parse(p.GetRawText())!.AsObject()
            : new JsonObject();
}
