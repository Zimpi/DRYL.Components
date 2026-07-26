using System.Text.Json;
using DRYL.Components.Canvas;
using Xunit;

namespace DRYL.Components.Tests.Canvas;

public class CanvasCatalogTests
{
    private static CanvasNode Node(string type, string propsJson, params CanvasNode[] children) => new()
    {
        Id = "n1", Type = type,
        Props = JsonSerializer.Deserialize<JsonElement>(propsJson),
        Children = children.Length == 0 ? null : children.ToList(),
    };

    private static CanvasNode NodeNoProps(string type, params CanvasNode[] children) => new()
    {
        Id = "n1", Type = type,
        Props = null,
        Children = children.Length == 0 ? null : children.ToList(),
    };

    // ---- generic shape ----

    [Fact] public void Valid_stat_passes() =>
        Assert.Null(CanvasCatalog.Validate(Node("stat", """{ "label": "Revenue", "value": "48k" }""")));

    [Fact] public void Unknown_type_is_rejected() =>
        Assert.Contains("not in the canvas catalog", CanvasCatalog.Validate(Node("hologram", "{}")));

    [Fact] public void Children_on_leaf_are_rejected() =>
        Assert.NotNull(CanvasCatalog.Validate(Node("stat",
            """{ "label": "a", "value": "b" }""", Node("divider", "{}"))));

    [Fact] public void Empty_id_is_rejected()
    {
        var node = Node("divider", "{}");
        node.Id = "";
        Assert.NotNull(CanvasCatalog.Validate(node));
    }

    [Fact] public void Missing_props_is_treated_as_empty_object() =>
        Assert.Null(CanvasCatalog.Validate(NodeNoProps("divider")));

    [Fact] public void Container_without_children_is_valid() =>
        Assert.Null(CanvasCatalog.Validate(NodeNoProps("stack")));

    [Fact] public void Malformed_props_shape_is_rejected() =>
        Assert.NotNull(CanvasCatalog.Validate(Node("grid", """{ "columns": "two" }""")));

    // ---- stack ----

    [Fact] public void Stack_valid_gap_passes() =>
        Assert.Null(CanvasCatalog.Validate(Node("stack", """{ "gap": "md" }""")));

    [Fact] public void Stack_invalid_gap_is_rejected() =>
        Assert.NotNull(CanvasCatalog.Validate(Node("stack", """{ "gap": "huge" }""")));

    // ---- grid ----

    [Fact] public void Grid_valid_columns_passes() =>
        Assert.Null(CanvasCatalog.Validate(Node("grid", """{ "columns": 2 }""")));

    [Fact] public void Grid_columns_out_of_range_is_rejected() =>
        Assert.NotNull(CanvasCatalog.Validate(Node("grid", """{ "columns": 0 }""")));

    [Fact] public void Grid_columns_above_max_is_rejected() =>
        Assert.NotNull(CanvasCatalog.Validate(Node("grid", """{ "columns": 5 }""")));

    // ---- card ----

    [Fact] public void Card_with_no_required_props_passes() =>
        Assert.Null(CanvasCatalog.Validate(Node("card", """{ "title": "Overview" }""")));

    [Fact] public void Card_with_no_props_at_all_passes() =>
        Assert.Null(CanvasCatalog.Validate(NodeNoProps("card")));

    // ---- tabs ----

    [Fact] public void Tabs_matching_labels_and_children_passes() =>
        Assert.Null(CanvasCatalog.Validate(Node("tabs",
            """{ "labels": ["A", "B"] }""", Node("divider", "{}"), Node("divider", "{}"))));

    [Fact] public void Tabs_label_child_mismatch_is_rejected() =>
        Assert.NotNull(CanvasCatalog.Validate(Node("tabs",
            """{ "labels": ["A", "B"] }""", Node("divider", "{}"))));

    [Fact] public void Tabs_empty_labels_is_rejected() =>
        Assert.NotNull(CanvasCatalog.Validate(Node("tabs", """{ "labels": [] }""")));

    // ---- divider ----

    [Fact] public void Divider_needs_no_props() =>
        Assert.Null(CanvasCatalog.Validate(Node("divider", "{}")));

    // ---- markdown ----

    [Fact] public void Markdown_with_content_passes() =>
        Assert.Null(CanvasCatalog.Validate(Node("markdown", """{ "content": "**hi**" }""")));

    [Fact] public void Markdown_without_content_is_rejected() =>
        Assert.NotNull(CanvasCatalog.Validate(Node("markdown", "{}")));

    // ---- stat ----

    [Fact] public void Stat_missing_value_is_rejected() =>
        Assert.Contains("value", CanvasCatalog.Validate(Node("stat", """{ "label": "Revenue", "value": "" }""")));

    [Fact] public void Stat_missing_label_is_rejected() =>
        Assert.Contains("label", CanvasCatalog.Validate(Node("stat", """{ "label": "", "value": "1" }""")));

    [Fact] public void Stat_invalid_direction_is_rejected() =>
        Assert.NotNull(CanvasCatalog.Validate(Node("stat",
            """{ "label": "Revenue", "value": "1", "direction": "sideways" }""")));

    [Fact] public void Stat_valid_direction_passes() =>
        Assert.Null(CanvasCatalog.Validate(Node("stat",
            """{ "label": "Revenue", "value": "1", "direction": "up" }""")));

    // ---- badge ----

    [Fact] public void Badge_valid_kind_passes() =>
        Assert.Null(CanvasCatalog.Validate(Node("badge", """{ "text": "New", "kind": "success" }""")));

    [Fact] public void Badge_without_text_is_rejected() =>
        Assert.NotNull(CanvasCatalog.Validate(Node("badge", "{}")));

    [Fact] public void Badge_invalid_kind_is_rejected() =>
        Assert.NotNull(CanvasCatalog.Validate(Node("badge", """{ "text": "New", "kind": "sparkly" }""")));

    // ---- progress ----

    [Fact] public void Progress_in_range_passes() =>
        Assert.Null(CanvasCatalog.Validate(Node("progress", """{ "value": 42 }""")));

    [Fact] public void Progress_out_of_range_is_rejected() =>
        Assert.NotNull(CanvasCatalog.Validate(Node("progress", """{ "value": 120 }""")));

    [Fact] public void Progress_negative_is_rejected() =>
        Assert.NotNull(CanvasCatalog.Validate(Node("progress", """{ "value": -5 }""")));

    // ---- table ----

    [Fact] public void Table_valid_rows_pass() =>
        Assert.Null(CanvasCatalog.Validate(Node("table",
            """{ "columns": ["A", "B"], "rows": [["1", "2"], ["3", "4"]] }""")));

    [Fact] public void Table_without_columns_is_rejected() =>
        Assert.NotNull(CanvasCatalog.Validate(Node("table", """{ "columns": [] }""")));

    [Fact] public void Table_row_cell_mismatch_is_rejected() =>
        Assert.NotNull(CanvasCatalog.Validate(Node("table",
            """{ "columns": ["A", "B"], "rows": [["1"]] }""")));

    [Fact] public void Table_too_many_rows_is_rejected()
    {
        var rows = string.Join(",", Enumerable.Repeat("""["1"]""", 31));
        Assert.NotNull(CanvasCatalog.Validate(Node("table",
            $$"""{ "columns": ["A"], "rows": [{{rows}}] }""")));
    }

    // ---- timeline ----

    [Fact] public void Timeline_valid_events_pass() =>
        Assert.Null(CanvasCatalog.Validate(Node("timeline",
            """{ "events": [ { "title": "Order placed" } ] }""")));

    [Fact] public void Timeline_without_events_is_rejected() =>
        Assert.NotNull(CanvasCatalog.Validate(Node("timeline", "{}")));

    // ---- charts ----

    [Fact] public void Chart_validation_is_delegated() =>
        Assert.NotNull(CanvasCatalog.Validate(Node("lineChart",
            """{ "labels": ["Jan"], "series": [] }""")));   // empty series → CartesianChartArgs error

    [Fact] public void LineChart_valid_passes() =>
        Assert.Null(CanvasCatalog.Validate(Node("lineChart",
            """{ "labels": ["Jan"], "series": [ { "name": "Rev", "data": [1] } ] }""")));

    [Fact] public void AreaChart_valid_passes() =>
        Assert.Null(CanvasCatalog.Validate(Node("areaChart",
            """{ "labels": ["Jan"], "series": [ { "name": "Rev", "data": [1] } ] }""")));

    [Fact] public void BarChart_valid_passes() =>
        Assert.Null(CanvasCatalog.Validate(Node("barChart",
            """{ "labels": ["Jan"], "series": [ { "name": "Rev", "data": [1] } ] }""")));

    [Fact] public void DonutChart_valid_passes() =>
        Assert.Null(CanvasCatalog.Validate(Node("donutChart",
            """{ "segments": [ { "label": "A", "value": 1 } ] }""")));

    [Fact] public void DonutChart_invalid_is_delegated() =>
        Assert.NotNull(CanvasCatalog.Validate(Node("donutChart", """{ "segments": [] }""")));

    // ---- inputText / textarea / select / slider / toggle ----

    [Fact] public void InputText_valid_passes() =>
        Assert.Null(CanvasCatalog.Validate(Node("inputText", """{ "name": "email", "label": "Email" }""")));

    [Fact] public void InputText_without_name_is_rejected() =>
        Assert.NotNull(CanvasCatalog.Validate(Node("inputText", """{ "label": "Email" }""")));

    [Fact] public void Textarea_valid_passes() =>
        Assert.Null(CanvasCatalog.Validate(Node("textarea", """{ "name": "body", "label": "About", "rows": 8 }""")));

    [Fact] public void Textarea_without_rows_passes() =>
        Assert.Null(CanvasCatalog.Validate(Node("textarea", """{ "name": "body", "label": "About" }""")));

    [Fact] public void Textarea_without_name_is_rejected() =>
        Assert.NotNull(CanvasCatalog.Validate(Node("textarea", """{ "label": "About" }""")));

    [Fact] public void Textarea_with_rows_out_of_range_is_rejected() =>
        Assert.Contains("rows", CanvasCatalog.Validate(
            Node("textarea", """{ "name": "body", "label": "About", "rows": 40 }"""))!);

    [Fact] public void Select_valid_passes() =>
        Assert.Null(CanvasCatalog.Validate(Node("select",
            """{ "name": "plan", "label": "Plan", "options": ["A", "B"] }""")));

    [Fact] public void Select_without_options_is_rejected() =>
        Assert.NotNull(CanvasCatalog.Validate(Node("select", """{ "name": "plan", "label": "Plan" }""")));

    [Fact] public void Slider_valid_passes() =>
        Assert.Null(CanvasCatalog.Validate(Node("slider",
            """{ "name": "n", "label": "L", "min": 0, "max": 10 }""")));

    [Fact] public void Slider_needs_min_below_max() =>
        Assert.NotNull(CanvasCatalog.Validate(Node("slider",
            """{ "name": "n", "label": "L", "min": 5, "max": 5 }""")));

    [Fact] public void Toggle_valid_passes() =>
        Assert.Null(CanvasCatalog.Validate(Node("toggle", """{ "name": "n", "label": "L" }""")));

    [Fact] public void Toggle_without_label_is_rejected() =>
        Assert.NotNull(CanvasCatalog.Validate(Node("toggle", """{ "name": "n" }""")));

    // ---- button ----

    [Fact] public void Button_valid_passes() =>
        Assert.Null(CanvasCatalog.Validate(Node("button", """{ "label": "Go", "intent": "submit" }""")));

    [Fact] public void Button_needs_intent() =>
        Assert.Contains("intent", CanvasCatalog.Validate(Node("button", """{ "label": "Go" }""")));

    [Fact] public void Button_invalid_kind_is_rejected() =>
        Assert.NotNull(CanvasCatalog.Validate(Node("button",
            """{ "label": "Go", "intent": "submit", "kind": "fancy" }""")));

    private static CanvasNode NodeChild(string id, string type, string propsJson = "{}") => new()
    {
        Id = id, Type = type, Props = JsonSerializer.Deserialize<JsonElement>(propsJson),
    };

    // ---- accordion ----

    [Fact] public void Accordion_matching_labels_and_children_passes() =>
        Assert.Null(CanvasCatalog.Validate(Node("accordion", """{ "labels": ["A", "B"] }""",
            NodeChild("c1", "divider"), NodeChild("c2", "divider"))));

    [Fact] public void Accordion_label_child_mismatch_rejected() =>
        Assert.NotNull(CanvasCatalog.Validate(Node("accordion", """{ "labels": ["A", "B"] }""",
            NodeChild("c1", "divider"))));

    [Fact] public void Accordion_open_out_of_range_rejected() =>
        Assert.NotNull(CanvasCatalog.Validate(Node("accordion", """{ "labels": ["A"], "open": 2 }""",
            NodeChild("c1", "divider"))));

    [Fact] public void Accordion_is_container() => Assert.True(CanvasCatalog.IsContainer("accordion"));

    // ---- form ----

    [Fact] public void Form_with_action_and_children_passes()
    {
        var form = Node("form", """{ "submitLabel": "Anlegen", "required": ["customer"] }""",
            NodeChild("f1", "inputText", """{ "name": "customer", "label": "Kunde" }"""));
        form.Action = new CanvasActionBinding { Name = "order.create" };
        Assert.Null(CanvasCatalog.Validate(form));
    }

    [Fact] public void Form_without_action_rejected() =>
        Assert.Contains("needs an action", CanvasCatalog.Validate(
            Node("form", """{ "submitLabel": "Anlegen" }""")));

    [Fact] public void Form_required_field_missing_in_subtree_rejected()
    {
        var form = Node("form", """{ "submitLabel": "Anlegen", "required": ["customer"] }""",
            NodeChild("f1", "inputText", """{ "name": "note", "label": "Notiz" }"""));
        form.Action = new CanvasActionBinding { Name = "order.create" };
        Assert.Contains("customer", CanvasCatalog.Validate(form));
    }

    [Fact] public void Form_is_container() => Assert.True(CanvasCatalog.IsContainer("form"));

    // ---- kpi ----

    [Fact] public void Kpi_valid_items_pass() =>
        Assert.Null(CanvasCatalog.Validate(Node("kpi",
            """{ "items": [{ "label": "Umsatz", "value": "48k", "delta": "+4%", "direction": "up" }] }""")));

    [Fact] public void Kpi_empty_items_rejected() =>
        Assert.NotNull(CanvasCatalog.Validate(Node("kpi", """{ "items": [] }""")));

    [Fact] public void Kpi_more_than_six_items_rejected() =>
        Assert.Contains("at most 6", CanvasCatalog.Validate(Node("kpi",
            """
            { "items": [{"label":"a","value":"1"},{"label":"b","value":"2"},{"label":"c","value":"3"},
                 {"label":"d","value":"4"},{"label":"e","value":"5"},{"label":"f","value":"6"},
                 {"label":"g","value":"7"}] }
            """)));

    [Fact] public void Kpi_invalid_direction_rejected() =>
        Assert.NotNull(CanvasCatalog.Validate(Node("kpi",
            """{ "items": [{ "label": "a", "value": "1", "direction": "sideways" }] }""")));

    // ---- list ----

    [Fact] public void List_valid_items_pass() =>
        Assert.Null(CanvasCatalog.Validate(Node("list",
            """{ "items": [{ "title": "Auftrag 4711", "text": "offen", "icon": "Package" }] }""")));

    [Fact] public void List_item_without_title_rejected() =>
        Assert.NotNull(CanvasCatalog.Validate(Node("list", """{ "items": [{ "text": "x" }] }""")));

    [Fact] public void List_more_than_fifty_items_rejected()
    {
        var items = string.Join(",", Enumerable.Range(0, 51).Select(i => $$"""{ "title": "t{{i}}" }"""));
        Assert.Contains("at most 50", CanvasCatalog.Validate(Node("list", $$"""{ "items": [{{items}}] }""")));
    }

    // ---- keyValue ----

    [Fact] public void KeyValue_valid_pairs_pass() =>
        Assert.Null(CanvasCatalog.Validate(Node("keyValue",
            """{ "pairs": [{ "key": "Status", "value": "offen" }], "columns": 2 }""")));

    [Fact] public void KeyValue_empty_pairs_rejected() =>
        Assert.NotNull(CanvasCatalog.Validate(Node("keyValue", """{ "pairs": [] }""")));

    [Fact] public void KeyValue_invalid_columns_rejected() =>
        Assert.NotNull(CanvasCatalog.Validate(Node("keyValue",
            """{ "pairs": [{ "key": "a", "value": "b" }], "columns": 3 }""")));

    // ---- image ----

    [Fact] public void Image_https_src_passes() =>
        Assert.Null(CanvasCatalog.Validate(Node("image",
            """{ "src": "https://example.com/a.png", "alt": "Diagramm" }""")));

    [Fact] public void Image_relative_and_data_src_pass()
    {
        Assert.Null(CanvasCatalog.Validate(Node("image", """{ "src": "/img/a.png", "alt": "a" }""")));
        Assert.Null(CanvasCatalog.Validate(Node("image", """{ "src": "data:image/png;base64,AAAA", "alt": "a" }""")));
    }

    [Fact] public void Image_javascript_src_rejected() =>
        Assert.NotNull(CanvasCatalog.Validate(Node("image",
            """{ "src": "javascript:alert(1)", "alt": "a" }""")));

    [Fact] public void Image_http_src_rejected() =>
        Assert.NotNull(CanvasCatalog.Validate(Node("image",
            """{ "src": "http://example.com/a.png", "alt": "a" }""")));

    [Fact] public void Image_missing_alt_rejected() =>
        Assert.NotNull(CanvasCatalog.Validate(Node("image", """{ "src": "https://example.com/a.png" }""")));

    [Fact] public void Image_invalid_ratio_rejected() =>
        Assert.NotNull(CanvasCatalog.Validate(Node("image",
            """{ "src": "https://example.com/a.png", "alt": "a", "ratio": "4:3" }""")));

    // ---- code ----

    [Fact] public void Code_valid_passes() =>
        Assert.Null(CanvasCatalog.Validate(Node("code",
            """{ "code": "SELECT 1;", "language": "sql", "lineNumbers": true }""")));

    [Fact] public void Code_empty_code_rejected() =>
        Assert.NotNull(CanvasCatalog.Validate(Node("code", """{ "code": "" }""")));

    // ---- emptyState ----

    [Fact] public void EmptyState_valid_passes() =>
        Assert.Null(CanvasCatalog.Validate(Node("emptyState",
            """{ "title": "Noch keine Aufträge", "description": "Lege den ersten an.", "icon": "Package" }""")));

    [Fact] public void EmptyState_missing_title_rejected() =>
        Assert.NotNull(CanvasCatalog.Validate(Node("emptyState", """{ "description": "x" }""")));

    // ---- dataGrid ----

    [Fact] public void DataGrid_valid_passes() =>
        Assert.Null(CanvasCatalog.Validate(Node("dataGrid",
            """{ "columns": ["A", "B"], "rows": [["1", "2"]], "sortable": true, "pageSize": 10 }""")));

    [Fact] public void DataGrid_without_columns_rejected() =>
        Assert.NotNull(CanvasCatalog.Validate(Node("dataGrid", """{ "rows": [["1"]] }""")));

    [Fact] public void DataGrid_more_than_twelve_columns_rejected()
    {
        var cols = string.Join(",", Enumerable.Range(0, 13).Select(i => $"\"c{i}\""));
        Assert.Contains("at most 12", CanvasCatalog.Validate(Node("dataGrid", $$"""{ "columns": [{{cols}}] }""")));
    }

    [Fact] public void DataGrid_row_cell_mismatch_rejected() =>
        Assert.NotNull(CanvasCatalog.Validate(Node("dataGrid",
            """{ "columns": ["A", "B"], "rows": [["1"]] }""")));

    [Fact] public void DataGrid_more_than_hundred_literal_rows_rejected()
    {
        var rows = string.Join(",", Enumerable.Range(0, 101).Select(i => $"[\"{i}\"]"));
        Assert.Contains("at most 100", CanvasCatalog.Validate(Node("dataGrid",
            $$"""{ "columns": ["A"], "rows": [{{rows}}] }""")));
    }

    [Fact] public void DataGrid_pagesize_out_of_range_rejected() =>
        Assert.NotNull(CanvasCatalog.Validate(Node("dataGrid",
            """{ "columns": ["A"], "pageSize": 101 }""")));

    // ---- classification ----

    [Fact] public void Interactive_and_container_classification()
    {
        Assert.True(CanvasCatalog.IsContainer("grid"));
        Assert.True(CanvasCatalog.IsContainer("stack"));
        Assert.True(CanvasCatalog.IsContainer("card"));
        Assert.True(CanvasCatalog.IsContainer("tabs"));
        Assert.False(CanvasCatalog.IsContainer("stat"));
        Assert.True(CanvasCatalog.IsInteractive("toggle"));
        Assert.True(CanvasCatalog.IsInteractive("inputText"));
        Assert.True(CanvasCatalog.IsInteractive("textarea"));
        Assert.True(CanvasCatalog.IsInteractive("select"));
        Assert.True(CanvasCatalog.IsInteractive("slider"));
        Assert.False(CanvasCatalog.IsInteractive("button"));
    }

    [Fact] public void IsKnownType_covers_the_catalog()
    {
        foreach (var type in new[]
                 {
                     "stack", "grid", "card", "tabs", "divider", "markdown", "stat", "badge", "progress",
                     "table", "timeline", "lineChart", "areaChart", "barChart", "donutChart",
                     "inputText", "textarea", "select", "slider", "toggle", "button",
                 })
            Assert.True(CanvasCatalog.IsKnownType(type), $"'{type}' should be known");

        Assert.False(CanvasCatalog.IsKnownType("hologram"));
    }
}
