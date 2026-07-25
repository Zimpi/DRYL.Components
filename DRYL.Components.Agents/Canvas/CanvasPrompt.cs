using DRYL.Components.Canvas;

namespace DRYL.Components.Agents;

/// <summary>
/// The canvas node catalog contract handed to the artifact generator, plus the prompt
/// builder for the <c>create_artifact</c> generation. Internal — <see cref="DrylCanvasTools"/>
/// is the public surface.
/// </summary>
internal static class CanvasPrompt
{
    /// <summary>The compact catalog contract: shape, node types and their props, verbatim in every generation prompt.</summary>
    public const string SchemaText =
        """
        You produce ONLY one JSON object — no markdown fences, no prose.
        Shape: { "title": string, "root": Node }
        Node: { "id": string, "type": string, "props": object, "children": Node[] }
        - "id": short, unique, stable, kebab-case. Never reuse an id.
        - "children" only on container types: stack, grid, card, tabs, accordion, form.
        Types and props:
        - stack { "gap": "sm"|"md"|"lg"? } — vertical layout; use as root.
        - grid { "columns": 1|2|3|4 } — equal-width responsive grid.
        - card { "title": string? } — glass card grouping its children.
        - tabs { "labels": string[] } — exactly one child per label.
        - divider { }
        - markdown { "content": string } — rich text (headings, lists, tables).
        - stat { "label": string, "value": string, "delta": string?, "direction": "up"|"down"|"neutral"? }
        - badge { "text": string, "kind": "default"|"success"|"warning"|"danger"? }
        - progress { "value": number 0..100, "label": string? }
        - table { "columns": string[], "rows": string[][] } — max 30 rows.
        - timeline { "events": [{ "title": string, "timestamp": string?, "text": string?, "kind": "default"|"success"|"warning"|"danger"? }] }
        - lineChart|areaChart|barChart { "title": string?, "labels": string[], "series": [{ "name": string, "data": number[] }], "valueFormat": string? } — one value per label. "valueFormat" is a display template: put {value} where the number goes, e.g. "€{value} Tsd" or "{value}%".
        - donutChart { "title": string?, "segments": [{ "label": string, "value": number }], "valueFormat": string? } — max 6 segments. Same {value} display template for "valueFormat".
        - inputText { "name": string, "label": string, "placeholder": string?, "value": string? }
        - select { "name": string, "label": string, "options": string[], "value": string? }
        - slider { "name": string, "label": string, "min": number, "max": number, "step": number?, "value": number? }
        - toggle { "name": string, "label": string, "value": boolean? }
        - button { "label": string, "intent": string, "kind": "primary"|"secondary"|"danger"? } — "intent" is a short
          machine-readable action id; clicking sends the intent plus all current input values back to you.
        - dataGrid { "columns": string[], "rows": string[][]?, "sortable": boolean?, "filterable": boolean?, "searchable": boolean?, "pageSize": number? } — interactive table with sorting, filters, search and paging. Max 12 columns, max 100 literal rows; bind a rows source for more. Use "table" only for a small static table.
        - form { "submitLabel": string, "required": string[]? } — container bundling its interactive children into ONE action. Put the action binding on the form node itself; the submit button is rendered for you. Prefer one form over a button per field.
        - kpi { "items": [{ "label": string, "value": string, "delta": string?, "direction": "up"|"down"|"neutral"? }] } — one row of 1..6 compact stats.
        - list { "items": [{ "title": string, "text": string?, "icon": string? }] } — vertical list, max 50 items.
        - keyValue { "pairs": [{ "key": string, "value": string }], "columns": 1|2? } — label/value pairs, max 20.
        - accordion { "labels": string[], "open": number? } — collapsible sections, exactly one child per label; "open" is the initially expanded index.
        - image { "src": string, "alt": string, "ratio": "auto"|"1:1"|"16:9"|"21:9"?, "fit": "cover"|"contain"?, "caption": string? } — "src" must start with https://, / or data:image/.
        - code { "code": string, "language": string?, "lineNumbers": boolean? } — read-only source block.
        - emptyState { "title": string, "description": string?, "icon": string? } — for a "nothing here yet" view instead of empty markdown.
        Interactive nodes (inputText/select/slider/toggle) each need a unique "name".
        Prefer charts and stats over prose for numbers. Keep the artifact focused.
        """;

    /// <summary>
    /// The layout budget for a rendering surface <paramref name="width"/> CSS px wide, or an empty
    /// string when the width is unknown (prerender, no JS). The generator is stateless and never sees
    /// the page, so this is the only way it can tell a 360px phone column from a 1200px desktop panel —
    /// without it, a three-column grid of stats is as likely on a phone as on a desktop, and the CSS
    /// step-down can only rescue the columns, never the eight-column table or the six-series chart.
    /// </summary>
    internal static string LayoutBudget(int? width)
    {
        if (width is not > 0) return string.Empty;

        var w = width.Value;
        var rules = w switch
        {
            < 480 =>
                """
                - grid: use "columns": 1 only — two cards side by side do not fit.
                - stat: label at most 14 characters, value at most 8 (a long value wraps one glyph per line).
                - table, dataGrid: at most 3 columns. kpi: at most 2 items.
                - charts: at most 2 series and at most 6 labels; keep labels short ("Apr", not "April 2025").
                - Prefer few tall nodes over dense ones. Do not nest a grid inside a grid.
                """,
            < 900 =>
                """
                - grid: at most 2 columns.
                - stat: label at most 20 characters.
                - table, dataGrid: at most 5 columns. kpi: at most 4 items.
                - charts: at most 3 series and at most 12 labels.
                """,
            _ =>
                """
                - grid: up to 3 columns; 4 only for short stats or badges.
                - table, dataGrid: at most 8 columns.
                - charts: at most 4 series.
                """,
        };

        return FormattableString.Invariant(
            $"\nLAYOUT BUDGET — the artifact renders in a column about {w}px wide. Author for that width:\n{rules}\n");
    }

    /// <summary>Builds the full create-artifact prompt: <see cref="SchemaText"/> + the layout budget for
    /// <paramref name="width"/> + the data source block + the action block (both empty when nothing is
    /// registered, so an app that uses neither sees the contract it always saw) + the brief
    /// (+ optional title).</summary>
    internal static string CreatePrompt(string brief, string? title, int? width = null,
                                       IReadOnlyList<CanvasDataDescriptor>? sources = null,
                                       IReadOnlyList<CanvasActionDescriptor>? actions = null) =>
        $"{SchemaText}\n{LayoutBudget(width)}{CanvasDataPrompt.Block(sources)}{CanvasActionPrompt.Block(actions)}\nBuild a new artifact{(title is null ? "" : $" titled \"{title}\"")} for this request:\n{brief}";

    /// <summary>Builds the full update-artifact prompt: <see cref="SchemaText"/> + the layout budget for
    /// <paramref name="width"/> + the data source block + the action block + the current spec + a closed
    /// set of patch ops the model may emit against it, + the brief.</summary>
    internal static string UpdatePrompt(string brief, string currentSpecJson, int? width = null,
                                       IReadOnlyList<CanvasDataDescriptor>? sources = null,
                                       IReadOnlyList<CanvasActionDescriptor>? actions = null) =>
        SchemaText + LayoutBudget(width) + CanvasDataPrompt.Block(sources) + CanvasActionPrompt.Block(actions) + """


            You UPDATE the existing artifact below. Output ONLY: { "ops": [Op, …] }
            Op is one of:
            - { "op": "setProps", "id": string, "props": object }  — shallow-merge props into the node
            - { "op": "insert", "parent": string, "index": number, "node": Node }
            - { "op": "remove", "id": string }
            - { "op": "move", "id": string, "parent": string, "index": number }
            Use existing ids; new nodes get fresh unique ids. Emit the smallest op set that fulfils the request.

            Current artifact:
            """ + currentSpecJson + "\n\nRequest:\n" + brief;
}
