using System.Text.Json;


namespace DRYL.Components.Canvas;

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
    public static string? Validate(CanvasNode node) => Validate(node, null);

    /// <summary>
    /// Validates a node, and — when <paramref name="context"/> is supplied and the node carries a
    /// <c>data</c> binding — the binding too: the source exists, its result shape fits the node
    /// type, the parameters are complete and known, every <c>$field</c> points at an interactive
    /// node of this artifact, and <c>refresh</c> is syntactically valid.
    /// <para>The result is a corrective sentence for the model's receipt, never a hard stop: an
    /// invalid node renders as a placeholder and the model repairs it on its next turn.</para>
    /// </summary>
    public static string? Validate(CanvasNode node, CanvasValidationContext? context)
    {
        if (context is null || node.Data is not { Source: not null }) return ValidateShape(node);

        // The binding comes first: a bound chart legitimately carries no "labels" — that is the
        // whole point — so reporting "labels must contain at least one label" would reject exactly
        // the artifacts data binding exists to enable.
        var bindingError = ValidateBinding(node, context);
        if (bindingError is not null) return bindingError;

        // The data itself only arrives at runtime, so validate the presentation props against a
        // stand-in of the declared shape. Everything the shape does not own is checked for real.
        var descriptor = context.Sources.First(d => d.Name == node.Data.Source);
        var props = CanvasDataMapper.Apply(node.Type, node.Props,
            CanvasDataMapper.Sample(descriptor.Shape), out _, out _);

        return ValidateShape(new CanvasNode
        {
            Id = node.Id, Type = node.Type, Props = props, Children = node.Children,
        });
    }

    private static string? ValidateShape(CanvasNode node)
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
                if (!TryProps<CanvasStatProps>(node, out var p)) return Err(node, "props are not valid JSON.");
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
                if (!TryProps<CanvasTimelineProps>(node, out var p)) return Err(node, "props are not valid JSON.");
                return Prefix(node, p!.Validate());
            }

            case "lineChart" or "areaChart" or "barChart":
            {
                if (!TryProps<CanvasChartProps>(node, out var p)) return Err(node, "props are not valid JSON.");
                return Prefix(node, p!.Validate());
            }

            case "donutChart":
            {
                if (!TryProps<CanvasDonutProps>(node, out var p)) return Err(node, "props are not valid JSON.");
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

    private static string? ValidateBinding(CanvasNode node, CanvasValidationContext context)
    {
        if (node.Data is not { } binding) return null;

        if (string.IsNullOrWhiteSpace(binding.Source))
            return Err(node, "data.source must name a registered data source.");

        var descriptor = context.Sources.FirstOrDefault(d => d.Name == binding.Source);
        if (descriptor is null)
        {
            var available = context.Sources.Take(5).Select(d => d.Name).ToList();
            return Err(node, available.Count == 0
                ? $"unknown data source '{binding.Source}' — no data sources are registered."
                : $"unknown data source '{binding.Source}' — available: {string.Join(", ", available)}"
                  + (context.Sources.Count > available.Count ? ", …" : "") + ".");
        }

        if (!CanvasDataMapper.Allows(descriptor.Shape, node.Type))
            return Err(node, $"source '{descriptor.Name}' returns {CanvasDataMapper.ShapeName(descriptor.Shape)}, " +
                             $"but a {node.Type} needs {ExpectedShape(node.Type)}.");

        var paramError = ValidateParams(node, binding, descriptor, context);
        if (paramError is not null) return paramError;

        return ValidateRefresh(node, binding.Refresh);
    }

    private static string? ValidateParams(CanvasNode node, CanvasDataBinding binding,
                                          CanvasDataDescriptor descriptor, CanvasValidationContext context)
    {
        var given = new HashSet<string>(StringComparer.Ordinal);

        if (binding.Params is { } p)
        {
            if (p.ValueKind != JsonValueKind.Object)
                return Err(node, "data.params must be an object.");

            foreach (var prop in p.EnumerateObject())
            {
                given.Add(prop.Name);
                var info = descriptor.Params.FirstOrDefault(x => x.Name == prop.Name);
                if (info is null)
                    return Err(node, $"source '{descriptor.Name}' has no parameter '{prop.Name}' — it takes "
                                     + Signature(descriptor) + ".");

                if (CanvasDataBinder.FieldReference(prop.Value) is { } field)
                {
                    // A field reference is only useful if the field exists; a typo would otherwise
                    // silently resolve to null on every load.
                    if (!context.FieldNames.Contains(field))
                        return Err(node, $"param '{prop.Name}' references field '{field}', but this artifact has "
                                         + (context.FieldNames.Count == 0
                                             ? "no interactive nodes."
                                             : "no such interactive node — it has: "
                                               + string.Join(", ", context.FieldNames.Take(5)) + "."));
                }
            }
        }

        var missing = descriptor.Params.Where(x => x.Required && !given.Contains(x.Name)).Select(x => x.Name).ToList();
        return missing.Count == 0
            ? null
            : Err(node, $"source '{descriptor.Name}' is missing required param"
                        + (missing.Count == 1 ? " " : "s ") + string.Join(", ", missing)
                        + " — it takes " + Signature(descriptor) + ".");
    }

    private static string? ValidateRefresh(CanvasNode node, string? refresh)
    {
        if (string.IsNullOrWhiteSpace(refresh) ||
            refresh.Equals("manual", StringComparison.OrdinalIgnoreCase)) return null;

        if (!CanvasDataBinder.TryParseInterval(refresh, out var seconds))
            return Err(node, $"data.refresh '{refresh}' is invalid — use \"manual\" or \"interval:<n>s\".");

        return seconds >= 5
            ? null
            : Err(node, FormattableString.Invariant(
                $"data.refresh 'interval:{seconds}s' is below the 5s floor — it was raised to interval:5s."));
    }

    private static string Signature(CanvasDataDescriptor d) =>
        d.Params.Count == 0
            ? "no parameters"
            : "(" + string.Join(", ", d.Params.Select(p => $"{p.Name}{(p.Required ? "" : "?")}: {p.TypeName}")) + ")";

    private static string ExpectedShape(string nodeType) => nodeType switch
    {
        "stat" or "badge" or "progress" => "scalar",
        "lineChart" or "areaChart" or "barChart" => "series",
        "donutChart" => "segments",
        "table" => "rows",
        _ => "no data at all — it cannot be bound",
    };

    private static string? ValidateNameAndLabel(CanvasNode node, string? name, string? label)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Err(node, "name must be non-empty.");
        if (string.IsNullOrWhiteSpace(label))
            return Err(node, "label must be non-empty.");
        return null;
    }

    private static bool TryProps<T>(CanvasNode n, out T? value) where T : class =>
        CanvasJson.TryParse(PropsJson(n), out value);

    private static string PropsJson(CanvasNode n) =>
        n.Props is { } p && p.ValueKind is not (JsonValueKind.Null or JsonValueKind.Undefined)
            ? p.GetRawText()
            : "{}";

    private static string? Prefix(CanvasNode n, string? error) =>
        error is null ? null : $"{n.Type} node '{n.Id}': {error}";

    private static string Err(CanvasNode n, string msg) => $"{n.Type} node '{n.Id}': {msg}";
}

/// <summary>
/// What <see cref="CanvasCatalog.Validate(CanvasNode, CanvasValidationContext?)"/> needs to check a
/// node's <c>data</c> binding: which sources exist, and which interactive field names this artifact
/// offers a <c>$field</c> reference.
/// </summary>
public sealed class CanvasValidationContext
{
    /// <summary>The registered data sources (see <c>ICanvasDataService.Descriptors</c>).</summary>
    public IReadOnlyList<CanvasDataDescriptor> Sources { get; init; } = Array.Empty<CanvasDataDescriptor>();

    /// <summary>The <c>name</c> props of every interactive node in the same artifact.</summary>
    public IReadOnlyCollection<string> FieldNames { get; init; } = Array.Empty<string>();

    /// <summary>Collects the <c>name</c> of every interactive node in <paramref name="root"/>'s subtree.</summary>
    public static IReadOnlyCollection<string> FieldNamesOf(CanvasNode? root)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);
        Collect(root, names);
        return names;
    }

    private static void Collect(CanvasNode? node, HashSet<string> names)
    {
        if (node is null) return;
        if (CanvasCatalog.IsInteractive(node.Type) &&
            node.Props is { ValueKind: JsonValueKind.Object } p &&
            p.TryGetProperty("name", out var n) && n.ValueKind == JsonValueKind.String &&
            n.GetString() is { Length: > 0 } name)
        {
            names.Add(name);
        }
        if (node.Children is null) return;
        foreach (var child in node.Children) Collect(child, names);
    }
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
