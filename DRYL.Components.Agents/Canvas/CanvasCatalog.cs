using System.Text.Json;
using DRYL.Components.Agents.Tools;

namespace DRYL.Components.Agents;

/// <summary>
/// Validation registry for the curated canvas node catalog. Every <see cref="CanvasNode"/> the model
/// (or a patch) produces is checked against here before <c>DrylAiCanvas</c> renders it.
/// </summary>
public static class CanvasCatalog
{
    private static readonly HashSet<string> ContainerTypes = new(StringComparer.Ordinal)
    {
        "stack", "grid", "card", "tabs",
    };

    private static readonly HashSet<string> InteractiveTypes = new(StringComparer.Ordinal)
    {
        "inputText", "select", "slider", "toggle",
    };

    private static readonly HashSet<string> AllTypes = new(StringComparer.Ordinal)
    {
        "stack", "grid", "card", "tabs", "divider", "markdown", "stat", "badge", "progress", "table",
        "timeline", "lineChart", "areaChart", "barChart", "donutChart",
        "inputText", "select", "slider", "toggle", "button",
    };

    /// <summary>True when <paramref name="type"/> is a recognized catalog entry.</summary>
    public static bool IsKnownType(string type) => AllTypes.Contains(type);

    /// <summary>True when <paramref name="type"/> may host <c>Children</c> (<c>stack</c>, <c>grid</c>, <c>card</c>, <c>tabs</c>).</summary>
    public static bool IsContainer(string type) => ContainerTypes.Contains(type);

    /// <summary>True when <paramref name="type"/> is a form control (<c>inputText</c>, <c>select</c>, <c>slider</c>, <c>toggle</c>).</summary>
    public static bool IsInteractive(string type) => InteractiveTypes.Contains(type);

    /// <summary>Validates a single node's shape and props. Returns null when valid, otherwise a corrective, model-facing error sentence.</summary>
    public static string? Validate(CanvasNode node)
    {
        if (string.IsNullOrWhiteSpace(node.Id))
            return Err(node, "id must be non-empty.");

        if (!IsContainer(node.Type) && node.Children is { Count: > 0 })
            return Err(node, "children are not allowed on this node type.");

        switch (node.Type)
        {
            case "stack":
            {
                if (!TryProps<StackNodeProps>(node, out var p)) return Err(node, "props are not valid JSON.");
                if (p!.Gap is not (null or "sm" or "md" or "lg"))
                    return Err(node, $"gap '{p.Gap}' is invalid — use 'sm', 'md' or 'lg'.");
                return null;
            }

            case "grid":
            {
                if (!TryProps<GridNodeProps>(node, out var p)) return Err(node, "props are not valid JSON.");
                if (p!.Columns is < 1 or > 4)
                    return Err(node, $"columns must be between 1 and 4 (was {p.Columns}).");
                return null;
            }

            case "card":
                return TryProps<CardNodeProps>(node, out _) ? null : Err(node, "props are not valid JSON.");

            case "tabs":
            {
                if (!TryProps<TabsNodeProps>(node, out var p)) return Err(node, "props are not valid JSON.");
                if (p!.Labels is null || p.Labels.Count == 0)
                    return Err(node, "labels must contain at least one label.");
                var childCount = node.Children?.Count ?? 0;
                if (p.Labels.Count != childCount)
                    return Err(node, $"labels.Count ({p.Labels.Count}) must equal the number of children ({childCount}).");
                return null;
            }

            case "divider":
                return null;

            case "markdown":
            {
                if (!TryProps<MarkdownNodeProps>(node, out var p)) return Err(node, "props are not valid JSON.");
                if (string.IsNullOrWhiteSpace(p!.Content))
                    return Err(node, "content must be non-empty.");
                return null;
            }

            case "stat":
            {
                if (!TryProps<StatSpec>(node, out var p)) return Err(node, "props are not valid JSON.");
                if (string.IsNullOrWhiteSpace(p!.Label))
                    return Err(node, "label must be non-empty.");
                if (string.IsNullOrWhiteSpace(p.Value))
                    return Err(node, "value must be non-empty.");
                if (p.Direction is not (null or "up" or "down" or "neutral"))
                    return Err(node, $"direction '{p.Direction}' is invalid — use 'up', 'down' or 'neutral'.");
                return null;
            }

            case "badge":
            {
                if (!TryProps<BadgeNodeProps>(node, out var p)) return Err(node, "props are not valid JSON.");
                if (string.IsNullOrWhiteSpace(p!.Text))
                    return Err(node, "text must be non-empty.");
                if (p.Kind is not (null or "default" or "success" or "warning" or "danger"))
                    return Err(node, $"kind '{p.Kind}' is invalid — use 'default', 'success', 'warning' or 'danger'.");
                return null;
            }

            case "progress":
            {
                if (!TryProps<ProgressNodeProps>(node, out var p)) return Err(node, "props are not valid JSON.");
                if (p!.Value is < 0 or > 100 || double.IsNaN(p.Value))
                    return Err(node, FormattableString.Invariant($"value must be between 0 and 100 (was {p.Value})."));
                return null;
            }

            case "table":
            {
                if (!TryProps<TableNodeProps>(node, out var p)) return Err(node, "props are not valid JSON.");
                if (p!.Columns is null || p.Columns.Count == 0)
                    return Err(node, "columns must contain at least one column.");
                if (p.Rows is not null)
                {
                    if (p.Rows.Count > 30)
                        return Err(node, "at most 30 rows are supported — aggregate or paginate the rest.");
                    foreach (var row in p.Rows)
                        if (row.Count != p.Columns.Count)
                            return Err(node, $"a row has {row.Count} cells but there are {p.Columns.Count} columns — they must match 1:1.");
                }
                return null;
            }

            case "timeline":
            {
                if (!TryProps<TimelineArgs>(node, out var p)) return Err(node, "props are not valid JSON.");
                return Prefix(node, p!.Validate());
            }

            case "lineChart" or "areaChart" or "barChart":
            {
                if (!TryProps<CartesianChartArgs>(node, out var p)) return Err(node, "props are not valid JSON.");
                return Prefix(node, p!.Validate());
            }

            case "donutChart":
            {
                if (!TryProps<DonutChartArgs>(node, out var p)) return Err(node, "props are not valid JSON.");
                return Prefix(node, p!.Validate());
            }

            case "inputText":
            {
                if (!TryProps<InputTextNodeProps>(node, out var p)) return Err(node, "props are not valid JSON.");
                return ValidateNameAndLabel(node, p!.Name, p.Label);
            }

            case "select":
            {
                if (!TryProps<SelectNodeProps>(node, out var p)) return Err(node, "props are not valid JSON.");
                var err = ValidateNameAndLabel(node, p!.Name, p.Label);
                if (err is not null) return err;
                if (p.Options is null || p.Options.Count == 0)
                    return Err(node, "options must contain at least one option.");
                return null;
            }

            case "slider":
            {
                if (!TryProps<SliderNodeProps>(node, out var p)) return Err(node, "props are not valid JSON.");
                var err = ValidateNameAndLabel(node, p!.Name, p.Label);
                if (err is not null) return err;
                if (p.Min >= p.Max)
                    return Err(node, FormattableString.Invariant($"min ({p.Min}) must be less than max ({p.Max})."));
                return null;
            }

            case "toggle":
            {
                if (!TryProps<ToggleNodeProps>(node, out var p)) return Err(node, "props are not valid JSON.");
                return ValidateNameAndLabel(node, p!.Name, p.Label);
            }

            case "button":
            {
                if (!TryProps<ButtonNodeProps>(node, out var p)) return Err(node, "props are not valid JSON.");
                if (string.IsNullOrWhiteSpace(p!.Label))
                    return Err(node, "label must be non-empty.");
                if (string.IsNullOrWhiteSpace(p.Intent))
                    return Err(node, "intent must be non-empty.");
                if (p.Kind is not (null or "primary" or "secondary"))
                    return Err(node, $"kind '{p.Kind}' is invalid — use 'primary' or 'secondary'.");
                return null;
            }

            default:
                return $"type '{node.Type}' is not in the canvas catalog.";
        }
    }

    private static string? ValidateNameAndLabel(CanvasNode node, string? name, string? label)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Err(node, "name must be non-empty.");
        if (string.IsNullOrWhiteSpace(label))
            return Err(node, "label must be non-empty.");
        return null;
    }

    private static bool TryProps<T>(CanvasNode n, out T? value) where T : class =>
        DisplayJson.TryParse(PropsJson(n), out value);

    private static string PropsJson(CanvasNode n) =>
        n.Props is { } p && p.ValueKind is not (JsonValueKind.Null or JsonValueKind.Undefined)
            ? p.GetRawText()
            : "{}";

    private static string? Prefix(CanvasNode n, string? error) =>
        error is null ? null : $"{n.Type} node '{n.Id}': {error}";

    private static string Err(CanvasNode n, string msg) => $"{n.Type} node '{n.Id}': {msg}";
}

/// <summary>Props of the <c>stack</c> container: a vertical/horizontal flex stack.</summary>
internal sealed class StackNodeProps
{
    /// <summary>Gap between children: <c>sm</c>, <c>md</c> or <c>lg</c>.</summary>
    public string? Gap { get; set; }
}

/// <summary>Props of the <c>grid</c> container.</summary>
internal sealed class GridNodeProps
{
    /// <summary>Number of columns, 1–4.</summary>
    public int Columns { get; set; }
}

/// <summary>Props of the <c>card</c> container.</summary>
internal sealed class CardNodeProps
{
    /// <summary>Optional card title shown in the header.</summary>
    public string? Title { get; set; }
}

/// <summary>Props of the <c>tabs</c> container.</summary>
internal sealed class TabsNodeProps
{
    /// <summary>Tab labels, one per child node, in order.</summary>
    public List<string>? Labels { get; set; }
}

/// <summary>Props of the <c>markdown</c> leaf.</summary>
internal sealed class MarkdownNodeProps
{
    /// <summary>The Markdown source text to render.</summary>
    public string? Content { get; set; }
}

/// <summary>Props of the <c>badge</c> leaf.</summary>
internal sealed class BadgeNodeProps
{
    /// <summary>Badge label text.</summary>
    public string? Text { get; set; }

    /// <summary>Tint: <c>default</c>, <c>success</c>, <c>warning</c> or <c>danger</c>.</summary>
    public string? Kind { get; set; }
}

/// <summary>Props of the <c>progress</c> leaf.</summary>
internal sealed class ProgressNodeProps
{
    /// <summary>Progress value, 0–100.</summary>
    public double Value { get; set; }

    /// <summary>Optional label shown alongside the bar.</summary>
    public string? Label { get; set; }
}

/// <summary>Props of the <c>table</c> leaf.</summary>
internal sealed class TableNodeProps
{
    /// <summary>Column headers.</summary>
    public List<string>? Columns { get; set; }

    /// <summary>Row data; each row must have exactly <see cref="Columns"/>.Count cells.</summary>
    public List<List<string>>? Rows { get; set; }
}

/// <summary>Props of the <c>inputText</c> control.</summary>
internal sealed class InputTextNodeProps
{
    /// <summary>Form field name used in interaction events.</summary>
    public string? Name { get; set; }

    /// <summary>Visible field label.</summary>
    public string? Label { get; set; }

    /// <summary>Optional placeholder text.</summary>
    public string? Placeholder { get; set; }

    /// <summary>Current field value.</summary>
    public string? Value { get; set; }
}

/// <summary>Props of the <c>select</c> control.</summary>
internal sealed class SelectNodeProps
{
    /// <summary>Form field name used in interaction events.</summary>
    public string? Name { get; set; }

    /// <summary>Visible field label.</summary>
    public string? Label { get; set; }

    /// <summary>Selectable option values.</summary>
    public List<string>? Options { get; set; }

    /// <summary>Current selected value.</summary>
    public string? Value { get; set; }
}

/// <summary>Props of the <c>slider</c> control.</summary>
internal sealed class SliderNodeProps
{
    /// <summary>Form field name used in interaction events.</summary>
    public string? Name { get; set; }

    /// <summary>Visible field label.</summary>
    public string? Label { get; set; }

    /// <summary>Minimum value; must be less than <see cref="Max"/>.</summary>
    public double Min { get; set; }

    /// <summary>Maximum value; must be greater than <see cref="Min"/>.</summary>
    public double Max { get; set; }

    /// <summary>Optional step increment.</summary>
    public double? Step { get; set; }

    /// <summary>Current value.</summary>
    public double? Value { get; set; }
}

/// <summary>Props of the <c>toggle</c> control.</summary>
internal sealed class ToggleNodeProps
{
    /// <summary>Form field name used in interaction events.</summary>
    public string? Name { get; set; }

    /// <summary>Visible field label.</summary>
    public string? Label { get; set; }

    /// <summary>Current on/off value.</summary>
    public bool? Value { get; set; }
}

/// <summary>Props of the <c>button</c> leaf.</summary>
internal sealed class ButtonNodeProps
{
    /// <summary>Visible button label.</summary>
    public string? Label { get; set; }

    /// <summary>Semantic action id reported in interaction events.</summary>
    public string? Intent { get; set; }

    /// <summary>Visual weight: <c>primary</c> or <c>secondary</c>.</summary>
    public string? Kind { get; set; }
}
