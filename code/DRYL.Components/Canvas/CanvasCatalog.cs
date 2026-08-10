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
        "stack", "grid", "card", "tabs", "accordion", "form",
    };

    private static readonly HashSet<string> InteractiveTypes = new(StringComparer.Ordinal)
    {
        "inputText", "textarea", "select", "slider", "toggle",
    };

    private static readonly HashSet<string> AllTypes = new(StringComparer.Ordinal)
    {
        "stack", "grid", "card", "tabs", "divider", "markdown", "stat", "badge", "progress", "table",
        "timeline", "lineChart", "areaChart", "barChart", "donutChart",
        "inputText", "textarea", "select", "slider", "toggle", "button",
        "kpi", "list", "keyValue", "image", "code", "emptyState", "accordion", "form", "dataGrid",
    };

    /// <summary>True when <paramref name="type"/> is a recognized catalog entry.</summary>
    public static bool IsKnownType(string type) => AllTypes.Contains(type);

    /// <summary>Every known catalog type. The prompt layer keeps its schema in sync against this —
    /// a type the model never sees can never be authored.</summary>
    public static IReadOnlyCollection<string> KnownTypes => AllTypes;

    /// <summary>True when <paramref name="type"/> may host <c>Children</c> (<c>stack</c>, <c>grid</c>, <c>card</c>, <c>tabs</c>).</summary>
    public static bool IsContainer(string type) => ContainerTypes.Contains(type);

    /// <summary>True when <paramref name="type"/> is a form control (<c>inputText</c>, <c>textarea</c>, <c>select</c>, <c>slider</c>, <c>toggle</c>).</summary>
    public static bool IsInteractive(string type) => InteractiveTypes.Contains(type);

    /// <summary>Validates a single node's shape and props. Returns null when valid, otherwise a corrective, model-facing error sentence.</summary>
    public static string? Validate(CanvasNode node) => Validate(node, null);

    /// <summary>
    /// Validates a node, and — when <paramref name="context"/> is supplied — its bindings too.
    /// <para>A <c>data</c> binding: the source exists, its result shape fits the node type, the
    /// parameters are complete and known, every <c>$field</c> points at an interactive node of
    /// this artifact, and <c>refresh</c> is syntactically valid. An <c>action</c> binding: it sits
    /// on a button, the action exists, its arguments are complete and known, and every
    /// <c>$field</c> resolves.</para>
    /// <para>The result is a corrective sentence for the model's receipt, never a hard stop: an
    /// invalid node renders as a placeholder and the model repairs it on its next turn.</para>
    /// </summary>
    public static string? Validate(CanvasNode node, CanvasValidationContext? context)
    {
        if (context is null) return ValidateShape(node);

        // A node may carry both bindings; checking the action first costs nothing, and neither
        // check may swallow the other.
        if (node.Action is { } action)
        {
            var actionError = ValidateAction(node, action, context);
            if (actionError is not null) return actionError;
        }

        if (node.Data is not { Source: not null }) return ValidateShape(node);

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
            Id = node.Id, Type = node.Type, Props = props, Children = node.Children, Action = node.Action,
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

            case "accordion":
            {
                if (!TryProps<AccordionNodeProps>(node, out var p)) return Err(node, "props are not valid JSON.");
                if (p!.Labels is null || p.Labels.Count == 0)
                    return Err(node, "labels must contain at least one label.");
                var childCount = node.Children?.Count ?? 0;
                if (p.Labels.Count != childCount)
                    return Err(node, $"labels.Count ({p.Labels.Count}) must equal the number of children ({childCount}).");
                if (p.Open is { } open && (open < 0 || open >= p.Labels.Count))
                    return Err(node, FormattableString.Invariant(
                        $"open ({open}) must be a valid section index (0..{p.Labels.Count - 1})."));
                return null;
            }

            case "form":
            {
                if (!TryProps<FormNodeProps>(node, out var p)) return Err(node, "props are not valid JSON.");
                if (string.IsNullOrWhiteSpace(p!.SubmitLabel))
                    return Err(node, "submitLabel must be non-empty.");
                if (string.IsNullOrWhiteSpace(node.Action?.Name))
                    return Err(node, "a form needs an action — put the action binding on the form node itself.");
                if (p.Required is { Count: > 0 } required)
                {
                    var fields = CanvasValidationContext.FieldNamesOf(node);
                    foreach (var name in required)
                        if (!fields.Contains(name))
                            return Err(node, $"required field '{name}' is not an interactive node inside this form.");
                }
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

            case "dataGrid":
            {
                if (!TryProps<DataGridNodeProps>(node, out var p)) return Err(node, "props are not valid JSON.");
                if (p!.Columns is null || p.Columns.Count == 0)
                    return Err(node, "columns must contain at least one column.");
                if (p.Columns.Count > 12)
                    return Err(node, "at most 12 columns are supported.");
                if (p.Rows is not null)
                {
                    if (p.Rows.Count > 100)
                        return Err(node, "at most 100 literal rows are supported — bind a rows data source for more.");
                    foreach (var row in p.Rows)
                        if (row.Count != p.Columns.Count)
                            return Err(node, $"a row has {row.Count} cells but there are {p.Columns.Count} columns — they must match 1:1.");
                }
                if (p.PageSize is { } size && (size < 0 || size > 100))
                    return Err(node, FormattableString.Invariant($"pageSize must be between 0 and 100 (was {size})."));
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

            case "kpi":
            {
                if (!TryProps<CanvasKpiProps>(node, out var p)) return Err(node, "props are not valid JSON.");
                return Prefix(node, p!.Validate());
            }

            // A bound list gets its entries from the source at runtime, so demanding literal ones
            // would make every bound list invalid — the same reason dataGrid's rows are optional.
            case "list":
            {
                if (!TryProps<CanvasListProps>(node, out var p)) return Err(node, "props are not valid JSON.");
                return Prefix(node, p!.Validate(node.Data is not null));
            }

            case "keyValue":
            {
                if (!TryProps<CanvasKeyValueProps>(node, out var p)) return Err(node, "props are not valid JSON.");
                return Prefix(node, p!.Validate(node.Data is not null));
            }

            case "image":
            {
                if (!TryProps<ImageNodeProps>(node, out var p)) return Err(node, "props are not valid JSON.");
                if (string.IsNullOrWhiteSpace(p!.Src))
                    return Err(node, "src must be non-empty.");
                if (!p.Src.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                    && !p.Src.StartsWith('/')
                    && !p.Src.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase))
                    return Err(node, "src must start with https://, / or data:image/ — other schemes are not allowed.");
                if (string.IsNullOrWhiteSpace(p.Alt))
                    return Err(node, "alt must be non-empty — describe the image.");
                if (p.Ratio is not (null or "auto" or "1:1" or "16:9" or "21:9"))
                    return Err(node, $"ratio '{p.Ratio}' is invalid — use 'auto', '1:1', '16:9' or '21:9'.");
                if (p.Fit is not (null or "cover" or "contain"))
                    return Err(node, $"fit '{p.Fit}' is invalid — use 'cover' or 'contain'.");
                return null;
            }

            case "code":
            {
                if (!TryProps<CodeNodeProps>(node, out var p)) return Err(node, "props are not valid JSON.");
                if (string.IsNullOrWhiteSpace(p!.Code))
                    return Err(node, "code must be non-empty.");
                return null;
            }

            case "emptyState":
            {
                if (!TryProps<EmptyStateNodeProps>(node, out var p)) return Err(node, "props are not valid JSON.");
                if (string.IsNullOrWhiteSpace(p!.Title))
                    return Err(node, "title must be non-empty.");
                return null;
            }

            case "inputText":
            {
                if (!TryProps<InputTextNodeProps>(node, out var p)) return Err(node, "props are not valid JSON.");
                return ValidateNameAndLabel(node, p!.Name, p.Label);
            }

            case "textarea":
            {
                if (!TryProps<TextareaNodeProps>(node, out var p)) return Err(node, "props are not valid JSON.");
                var err = ValidateNameAndLabel(node, p!.Name, p.Label);
                if (err is not null) return err;
                // A null rows falls through: the renderer uses the DrylTextarea default.
                if (p.Rows is < 2 or > 20)
                    return Err(node, FormattableString.Invariant($"rows ({p.Rows}) must be between 2 and 20."));
                return null;
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
                // An action-bound button carries its meaning in the action, not in an invented
                // intent string; a plain button still needs one to be reachable at all.
                if (string.IsNullOrWhiteSpace(p.Intent) && string.IsNullOrWhiteSpace(node.Action?.Name))
                    return Err(node, "a button needs an intent or an action.");
                if (p.Kind is not (null or "primary" or "secondary" or "danger"))
                    return Err(node, $"kind '{p.Kind}' is invalid — use 'primary', 'secondary' or 'danger'.");
                return null;
            }

            default:
                return $"type '{node.Type}' is not in the canvas catalog.";
        }
    }

    private static string? ValidateAction(CanvasNode node, CanvasActionBinding action,
                                          CanvasValidationContext context)
    {
        // One command, one trigger — a press on a button, or a form's submit. Anything else would
        // mean an action could fire from something the user does not experience as a deliberate act.
        if (node.Type is not ("button" or "form"))
            return Err(node, "an action can only sit on a button or form — move it to the node that triggers it.");

        if (string.IsNullOrWhiteSpace(action.Name))
            return Err(node, "action.name must name a registered action.");

        var descriptor = context.Actions.FirstOrDefault(a => a.Name == action.Name);
        if (descriptor is null)
        {
            var available = context.Actions.Take(5).Select(a => a.Name).ToList();
            return Err(node, available.Count == 0
                ? $"unknown action '{action.Name}' — no actions are registered."
                : $"unknown action '{action.Name}' — available: {string.Join(", ", available)}"
                  + (context.Actions.Count > available.Count ? ", …" : "") + ".");
        }

        if (action.Confirm is not null && string.IsNullOrWhiteSpace(action.Confirm))
            return Err(node, "action.confirm must be a question for the user, or be omitted entirely.");

        return ValidateActionArgs(node, action, descriptor, context);
    }

    private static string? ValidateActionArgs(CanvasNode node, CanvasActionBinding action,
                                              CanvasActionDescriptor descriptor,
                                              CanvasValidationContext context)
    {
        var given = new HashSet<string>(StringComparer.Ordinal);

        if (action.Args is { } a)
        {
            if (a.ValueKind != JsonValueKind.Object)
                return Err(node, "action.args must be an object.");

            foreach (var prop in a.EnumerateObject())
            {
                given.Add(prop.Name);
                if (descriptor.Args.All(x => x.Name != prop.Name))
                    return Err(node, $"action '{descriptor.Name}' has no argument '{prop.Name}' — it takes "
                                     + ActionSignature(descriptor) + ".");

                // A field reference is only useful if the field exists; a typo would otherwise
                // silently send null to a command handler.
                if (CanvasArgs.FieldReference(prop.Value) is { } field &&
                    !context.FieldNames.Contains(field))
                {
                    return Err(node, $"argument '{prop.Name}' references field '{field}', but this artifact has "
                                     + (context.FieldNames.Count == 0
                                         ? "no interactive nodes."
                                         : "no such interactive node — it has: "
                                           + string.Join(", ", context.FieldNames.Take(5)) + "."));
                }
            }
        }

        var missing = descriptor.Args.Where(x => x.Required && !given.Contains(x.Name))
                                     .Select(x => x.Name).ToList();
        return missing.Count == 0
            ? null
            : Err(node, $"action '{descriptor.Name}' is missing required arg"
                        + (missing.Count == 1 ? " " : "s ") + string.Join(", ", missing)
                        + " — it takes " + ActionSignature(descriptor) + ".");
    }

    private static string ActionSignature(CanvasActionDescriptor d) =>
        d.Args.Count == 0
            ? "no arguments"
            : "(" + string.Join(", ", d.Args.Select(a => $"{a.Name}{(a.Required ? "" : "?")}: {a.TypeName}")) + ")";

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
        "table" or "dataGrid" or "list" or "keyValue" => "rows",
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

    /// <summary>The registered actions (see <c>ICanvasActionService.Descriptors</c>).</summary>
    public IReadOnlyList<CanvasActionDescriptor> Actions { get; init; } = Array.Empty<CanvasActionDescriptor>();

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

/// <summary>Props of the <c>accordion</c> container.</summary>
internal sealed class AccordionNodeProps
{
    /// <summary>Section labels, one per child node, in order.</summary>
    public List<string>? Labels { get; set; }

    /// <summary>Index of the initially expanded section; default all collapsed.</summary>
    public int? Open { get; set; }
}

/// <summary>Props of the <c>form</c> container — bundles its interactive children into one action.</summary>
internal sealed class FormNodeProps
{
    /// <summary>Label of the submit button.</summary>
    public string? SubmitLabel { get; set; }

    /// <summary>Names of interactive child nodes that must be filled before submit.</summary>
    public List<string>? Required { get; set; }
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

/// <summary>Props of the <c>textarea</c> control — the multi-line sibling of <c>inputText</c>.</summary>
internal sealed class TextareaNodeProps
{
    /// <summary>Form field name used in interaction events.</summary>
    public string? Name { get; set; }

    /// <summary>Visible field label.</summary>
    public string? Label { get; set; }

    /// <summary>Optional placeholder text.</summary>
    public string? Placeholder { get; set; }

    /// <summary>Current field value.</summary>
    public string? Value { get; set; }

    /// <summary>Visible rows, 2..20. Null renders the <c>DrylTextarea</c> default of 4.</summary>
    public int? Rows { get; set; }
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

/// <summary>Props of the <c>dataGrid</c> leaf — the interactive big brother of <c>table</c>.</summary>
internal sealed class DataGridNodeProps
{
    /// <summary>Column headers, 1–12.</summary>
    public List<string>? Columns { get; set; }

    /// <summary>Literal row data (max 100); bind a rows source for more.</summary>
    public List<List<string>>? Rows { get; set; }

    /// <summary>Click-to-sort on all columns. Default true.</summary>
    public bool? Sortable { get; set; }

    /// <summary>Per-column select filters. Default false.</summary>
    public bool? Filterable { get; set; }

    /// <summary>Toolbar search across all columns. Default false.</summary>
    public bool? Searchable { get; set; }

    /// <summary>Items per page; 0 disables paging. Default 10, max 100.</summary>
    public int? PageSize { get; set; }
}

/// <summary>Props of the <c>image</c> leaf.</summary>
internal sealed class ImageNodeProps
{
    /// <summary>Image URL — must start with https://, / or data:image/.</summary>
    public string? Src { get; set; }

    /// <summary>Alt text — required, accessibility is not optional.</summary>
    public string? Alt { get; set; }

    /// <summary>Aspect ratio: 'auto' (default), '1:1', '16:9' or '21:9'.</summary>
    public string? Ratio { get; set; }

    /// <summary>Object-fit: 'cover' (default) or 'contain'.</summary>
    public string? Fit { get; set; }

    /// <summary>Optional caption line below the image.</summary>
    public string? Caption { get; set; }
}

/// <summary>Props of the <c>code</c> leaf.</summary>
internal sealed class CodeNodeProps
{
    /// <summary>The source text to render.</summary>
    public string? Code { get; set; }

    /// <summary>Optional language hint for highlighting.</summary>
    public string? Language { get; set; }

    /// <summary>Show line numbers. Default false.</summary>
    public bool? LineNumbers { get; set; }
}

/// <summary>Props of the <c>emptyState</c> leaf.</summary>
internal sealed class EmptyStateNodeProps
{
    /// <summary>Headline, e.g. 'Nothing here yet'.</summary>
    public string? Title { get; set; }

    /// <summary>Optional supporting text.</summary>
    public string? Description { get; set; }

    /// <summary>Optional DrylIcon name; unknown names fall back to the default icon.</summary>
    public string? Icon { get; set; }
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
