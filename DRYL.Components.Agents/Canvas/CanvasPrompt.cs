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
        - "children" only on container types: stack, grid, card, tabs.
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
        - lineChart|areaChart|barChart { "title": string?, "labels": string[], "series": [{ "name": string, "data": number[] }], "valueFormat": string? } — one value per label.
        - donutChart { "title": string?, "segments": [{ "label": string, "value": number }], "valueFormat": string? } — max 6 segments.
        - inputText { "name": string, "label": string, "placeholder": string?, "value": string? }
        - select { "name": string, "label": string, "options": string[], "value": string? }
        - slider { "name": string, "label": string, "min": number, "max": number, "step": number?, "value": number? }
        - toggle { "name": string, "label": string, "value": boolean? }
        - button { "label": string, "intent": string, "kind": "primary"|"secondary"? } — "intent" is a short
          machine-readable action id; clicking sends the intent plus all current input values back to you.
        Interactive nodes (inputText/select/slider/toggle) each need a unique "name".
        Prefer charts and stats over prose for numbers. Keep the artifact focused.
        """;

    /// <summary>Builds the full create-artifact prompt: <see cref="SchemaText"/> + the brief (+ optional title).</summary>
    internal static string CreatePrompt(string brief, string? title) =>
        $"{SchemaText}\n\nBuild a new artifact{(title is null ? "" : $" titled \"{title}\"")} for this request:\n{brief}";
}
