# Canvas Phase 4 — Katalog-Ausbau Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Neun neue Canvas-Node-Typen (`dataGrid`, `form`, `kpi`, `list`, `keyValue`, `accordion`, `image`, `code`, `emptyState`) samt Schema, Datenbindung und Demo — Spec: `docs/superpowers/specs/2026-07-25-canvas-catalog-design.md`.

**Architecture:** Alle Typen rendern über bestehende DRYL-Komponenten in `CanvasNodeView`; Validierung zentral in `CanvasCatalog`; Rows-Bindung wird auf dataGrid/list/keyValue geweitet; das Agents-Schema wächst um eine Zeile je Typ mit Budget-Wächter-Test. Keine neue öffentliche Komponente, keine neuen Tokens/Animationen.

**Tech Stack:** Blazor (net8/9/10 multi-target), xUnit + bUnit in `tests/DRYL.Components.Tests`.

## Global Constraints

- Kern `DRYL.Components/DRYL.Components.csproj` `<Version>`: 2.14.1 → **2.15.0**; Agents `DRYL.Components.Agents/DRYL.Components.Agents.csproj`: 0.12.0 → **0.13.0** — erst in Task 11, zusammen mit dem Changelog-Release-Schnitt.
- Fehlertexte sind korrigierende, modellgerichtete englische Sätze im Stil `"{type} node '{id}': …"` (`CanvasCatalog.Err`).
- Zahlen-Interpolation immer `FormattableString.Invariant` (deutsche Locale!).
- Nur bestehende CSS-Tokens/Animationen; neue Klassen in `DRYL.Components/Components/AI/DrylCanvas.razor.css` referenzieren ausschließlich Tokens.
- Tests laufen mit `dotnet test tests/DRYL.Components.Tests -f net10.0` (ein Framework reicht während der Entwicklung; Task 11 läuft komplett).
- Commits: Konvention wie Repo-Historie (`feat(canvas): …` / `test(canvas): …`), Co-Authored-By-Zeile.

---

### Task 1: Leaf-Props + Validierung — kpi, list, keyValue, image, code, emptyState

**Files:**
- Modify: `DRYL.Components/Canvas/CanvasPropTypes.cs` (neue Prop-Klassen ans Ende)
- Modify: `DRYL.Components/Canvas/CanvasCatalog.cs` (AllTypes + ValidateShape-Cases)
- Test: `tests/DRYL.Components.Tests/Canvas/CanvasCatalogTests.cs` (anhängen)

**Interfaces:**
- Produces: `internal sealed class CanvasKpiProps { IReadOnlyList<CanvasKpiItemProps>? Items; string? Validate() }`, `CanvasKpiItemProps { string Label; string Value; string? Delta; string? Direction; DeltaDirection ParsedDirection }`, `CanvasListProps { IReadOnlyList<CanvasListItemProps>? Items; string? Validate() }`, `CanvasListItemProps { string Title; string? Text; string? Icon }`, `CanvasKeyValueProps { IReadOnlyList<CanvasKeyValuePairProps>? Pairs; int? Columns; string? Validate() }`, `CanvasKeyValuePairProps { string Key; string Value }`, `ImageNodeProps { string? Src; string? Alt; string? Ratio; string? Fit; string? Caption }`, `CodeNodeProps { string? Code; string? Language; bool? LineNumbers }`, `EmptyStateNodeProps { string? Title; string? Description; string? Icon }` — alle in Namespace `DRYL.Components.Canvas`.
- Produces: `CanvasCatalog.IsKnownType` kennt die sechs neuen Typen.

- [ ] **Step 1: Failing Tests schreiben** — in `CanvasCatalogTests` anhängen (Muster: vorhandene `Node(...)`-Helper):

```csharp
// ---- kpi ----

[Fact] public void Kpi_valid_items_pass() =>
    Assert.Null(CanvasCatalog.Validate(Node("kpi",
        """{ "items": [{ "label": "Umsatz", "value": "48k", "delta": "+4%", "direction": "up" }] }""")));

[Fact] public void Kpi_empty_items_rejected() =>
    Assert.NotNull(CanvasCatalog.Validate(Node("kpi", """{ "items": [] }""")));

[Fact] public void Kpi_more_than_six_items_rejected() =>
    Assert.Contains("at most 6", CanvasCatalog.Validate(Node("kpi",
        """{ "items": [{"label":"a","value":"1"},{"label":"b","value":"2"},{"label":"c","value":"3"},
             {"label":"d","value":"4"},{"label":"e","value":"5"},{"label":"f","value":"6"},
             {"label":"g","value":"7"}] }""")));

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
```

- [ ] **Step 2: Tests laufen lassen — sie müssen FAILEN**

Run: `dotnet test tests/DRYL.Components.Tests -f net10.0 --filter "FullyQualifiedName~CanvasCatalogTests"`
Expected: die neuen Tests scheitern mit „not in the canvas catalog".

- [ ] **Step 3: Prop-Klassen in `CanvasPropTypes.cs` anhängen**

```csharp
/// <summary>One tile of a <c>kpi</c> node.</summary>
internal sealed class CanvasKpiItemProps
{
    /// <summary>Short metric label, e.g. 'Revenue'.</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>The headline value, pre-formatted as text.</summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>Optional change indicator text, e.g. '+12.4%'.</summary>
    public string? Delta { get; set; }

    /// <summary>Trend direction of the delta: 'up', 'down' or 'neutral'.</summary>
    public string? Direction { get; set; }

    internal DeltaDirection ParsedDirection => Direction?.ToLowerInvariant() switch
    {
        "up" => DeltaDirection.Up,
        "down" => DeltaDirection.Down,
        "neutral" => DeltaDirection.Neutral,
        _ => DeltaDirection.None,
    };
}

/// <summary>Props of the <c>kpi</c> node — a row of compact stats.</summary>
internal sealed class CanvasKpiProps
{
    /// <summary>The tiles, 1–6.</summary>
    public IReadOnlyList<CanvasKpiItemProps>? Items { get; set; }

    /// <summary>Null when valid; otherwise a corrective, model-facing error sentence.</summary>
    public string? Validate()
    {
        if (Items is null || Items.Count == 0)
            return "items must contain at least one item.";
        if (Items.Count > 6)
            return "at most 6 items are supported — aggregate the rest.";
        foreach (var i in Items)
        {
            if (string.IsNullOrWhiteSpace(i.Label)) return "every item needs a non-empty label.";
            if (string.IsNullOrWhiteSpace(i.Value)) return "every item needs a non-empty value.";
            if (i.Direction is not (null or "up" or "down" or "neutral"))
                return $"direction '{i.Direction}' is invalid — use 'up', 'down' or 'neutral'.";
        }
        return null;
    }
}

/// <summary>One entry of a <c>list</c> node.</summary>
internal sealed class CanvasListItemProps
{
    /// <summary>Title line of the entry.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Optional secondary text below the title.</summary>
    public string? Text { get; set; }

    /// <summary>Optional DrylIcon name; unknown names render no icon.</summary>
    public string? Icon { get; set; }
}

/// <summary>Props of the <c>list</c> node — a vertical repeater.</summary>
internal sealed class CanvasListProps
{
    /// <summary>The entries, 1–50.</summary>
    public IReadOnlyList<CanvasListItemProps>? Items { get; set; }

    /// <summary>Null when valid; otherwise a corrective, model-facing error sentence.</summary>
    public string? Validate()
    {
        if (Items is null || Items.Count == 0)
            return "items must contain at least one item.";
        if (Items.Count > 50)
            return "at most 50 items are supported — aggregate or paginate the rest.";
        foreach (var i in Items)
            if (string.IsNullOrWhiteSpace(i.Title)) return "every item needs a non-empty title.";
        return null;
    }
}

/// <summary>One pair of a <c>keyValue</c> node.</summary>
internal sealed class CanvasKeyValuePairProps
{
    /// <summary>The term / label.</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>The value text.</summary>
    public string Value { get; set; } = string.Empty;
}

/// <summary>Props of the <c>keyValue</c> node — label/value pairs.</summary>
internal sealed class CanvasKeyValueProps
{
    /// <summary>The pairs, 1–20.</summary>
    public IReadOnlyList<CanvasKeyValuePairProps>? Pairs { get; set; }

    /// <summary>Layout columns: 1 (default) or 2.</summary>
    public int? Columns { get; set; }

    /// <summary>Null when valid; otherwise a corrective, model-facing error sentence.</summary>
    public string? Validate()
    {
        if (Pairs is null || Pairs.Count == 0)
            return "pairs must contain at least one pair.";
        if (Pairs.Count > 20)
            return "at most 20 pairs are supported.";
        if (Columns is not (null or 1 or 2))
            return FormattableString.Invariant($"columns must be 1 or 2 (was {Columns}).");
        foreach (var p in Pairs)
            if (string.IsNullOrWhiteSpace(p.Key)) return "every pair needs a non-empty key.";
        return null;
    }
}
```

Und in `CanvasCatalog.cs` (bei den anderen `*NodeProps`-Klassen am Dateiende):

```csharp
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
```

- [ ] **Step 4: `CanvasCatalog` erweitern** — `AllTypes` um `"kpi", "list", "keyValue", "image", "code", "emptyState"` ergänzen; in `ValidateShape` vor `default:` einfügen:

```csharp
case "kpi":
{
    if (!TryProps<CanvasKpiProps>(node, out var p)) return Err(node, "props are not valid JSON.");
    return Prefix(node, p!.Validate());
}

case "list":
{
    if (!TryProps<CanvasListProps>(node, out var p)) return Err(node, "props are not valid JSON.");
    return Prefix(node, p!.Validate());
}

case "keyValue":
{
    if (!TryProps<CanvasKeyValueProps>(node, out var p)) return Err(node, "props are not valid JSON.");
    return Prefix(node, p!.Validate());
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
```

- [ ] **Step 5: Tests laufen lassen — PASS**

Run: `dotnet test tests/DRYL.Components.Tests -f net10.0 --filter "FullyQualifiedName~CanvasCatalogTests"`

- [ ] **Step 6: Commit**

```bash
git add DRYL.Components/Canvas/CanvasPropTypes.cs DRYL.Components/Canvas/CanvasCatalog.cs tests/DRYL.Components.Tests/Canvas/CanvasCatalogTests.cs
git commit -m "feat(canvas): kpi/list/keyValue/image/code/emptyState im Katalog validiert"
```

---

### Task 2: Container-Typen — accordion + form (Validierung)

**Files:**
- Modify: `DRYL.Components/Canvas/CanvasCatalog.cs`
- Test: `tests/DRYL.Components.Tests/Canvas/CanvasCatalogTests.cs`, `tests/DRYL.Components.Tests/Canvas/CanvasActionValidationTests.cs`

**Interfaces:**
- Consumes: `CanvasValidationContext` (Actions, FieldNames), `CanvasCatalog.ValidateAction` (bestehend, wird geweitet).
- Produces: `AccordionNodeProps { List<string>? Labels; int? Open }`, `FormNodeProps { string? SubmitLabel; List<string>? Required }`; `CanvasCatalog.IsContainer("accordion") == true`, `IsContainer("form") == true`; `ValidateAction` akzeptiert `form`-Nodes.

- [ ] **Step 1: Failing Tests** — in `CanvasCatalogTests` anhängen:

```csharp
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
```

Dazu einen Helper neben `Node(...)` (Kinder mit eigener Id/Props):

```csharp
private static CanvasNode NodeChild(string id, string type, string propsJson = "{}") => new()
{
    Id = id, Type = type, Props = JsonSerializer.Deserialize<JsonElement>(propsJson),
};
```

In `CanvasActionValidationTests` (Muster der Datei übernehmen — sie baut `CanvasValidationContext` mit Action-Deskriptoren):

```csharp
[Fact]
public void Action_on_form_node_is_accepted()
{
    var form = new CanvasNode
    {
        Id = "f", Type = "form",
        Props = JsonSerializer.Deserialize<JsonElement>("""{ "submitLabel": "Go" }"""),
        Action = new CanvasActionBinding { Name = "order.create" },
    };
    // Kontext wie in den bestehenden Tests der Datei mit einer Aktion "order.create" ohne Args.
    Assert.Null(CanvasCatalog.Validate(form, ContextWithAction("order.create")));
}

[Fact]
public void Action_on_stat_node_is_still_rejected()
{
    var stat = new CanvasNode
    {
        Id = "s", Type = "stat",
        Props = JsonSerializer.Deserialize<JsonElement>("""{ "label": "a", "value": "1" }"""),
        Action = new CanvasActionBinding { Name = "order.create" },
    };
    Assert.Contains("button or form", CanvasCatalog.Validate(stat, ContextWithAction("order.create")));
}
```

(`ContextWithAction` = vorhandener Helper der Datei bzw. analog anlegen.)

- [ ] **Step 2: Tests laufen lassen — FAIL**

Run: `dotnet test tests/DRYL.Components.Tests -f net10.0 --filter "FullyQualifiedName~CanvasCatalogTests|FullyQualifiedName~CanvasActionValidationTests"`

- [ ] **Step 3: Implementieren** — in `CanvasCatalog.cs`:

`ContainerTypes` → `{ "stack", "grid", "card", "tabs", "accordion", "form" }`; `AllTypes` um `"accordion", "form"`.

`ValidateAction`, erste Prüfung ersetzen:

```csharp
// One command, one trigger — a press on a button, or a form's submit. Anything else would
// mean an action could fire from something the user does not experience as a deliberate act.
if (node.Type is not ("button" or "form"))
    return Err(node, "an action can only sit on a button or form — move it to the node that triggers it.");
```

`ValidateShape`-Cases:

```csharp
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
```

Prop-Klassen (bei den anderen `*NodeProps`):

```csharp
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
```

**Achtung Reihenfolge in `Validate(node, context)`:** der bestehende Frühausstieg `if (node.Data is not { Source: not null }) return ValidateShape(node);` läuft nach `ValidateAction` — das bleibt korrekt, weil `ValidateAction` für form-Nodes jetzt durchläuft und `ValidateShape` die restliche Formprüfung übernimmt.

- [ ] **Step 4: Tests laufen lassen — PASS** (gleicher Befehl wie Step 2; zusätzlich einmal die ganze Suite: `dotnet test tests/DRYL.Components.Tests -f net10.0` — der bestehende Test auf die Fehlermeldung „an action can only sit on a button" muss ggf. auf den neuen Text angepasst werden).

- [ ] **Step 5: Commit**

```bash
git add DRYL.Components/Canvas/CanvasCatalog.cs tests/DRYL.Components.Tests/Canvas/
git commit -m "feat(canvas): accordion- und form-Container im Katalog; Aktionen auch am form-Node"
```

---

### Task 3: dataGrid — Props + Validierung

**Files:**
- Modify: `DRYL.Components/Canvas/CanvasCatalog.cs`
- Test: `tests/DRYL.Components.Tests/Canvas/CanvasCatalogTests.cs`

**Interfaces:**
- Produces: `internal sealed class DataGridNodeProps { List<string>? Columns; List<List<string>>? Rows; bool? Sortable; bool? Filterable; bool? Searchable; int? PageSize }`; `"dataGrid"` in `AllTypes` (kein Container).

- [ ] **Step 1: Failing Tests**

```csharp
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
```

- [ ] **Step 2: FAIL bestätigen** (Filter wie Task 1)

- [ ] **Step 3: Implementieren** — `AllTypes` + `"dataGrid"`; `ValidateShape`-Case:

```csharp
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
```

Prop-Klasse:

```csharp
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
```

- [ ] **Step 4: PASS bestätigen**
- [ ] **Step 5: Commit** — `feat(canvas): dataGrid-Typ im Katalog validiert`

---

### Task 4: Mapper — Rows auf dataGrid/list/keyValue weiten

**Files:**
- Modify: `DRYL.Components/Canvas/CanvasDataMapper.cs`
- Test: `tests/DRYL.Components.Tests/Canvas/CanvasDataMapperTests.cs`

**Interfaces:**
- Consumes: `CanvasRowData` (Columns/Rows), `CanvasData.Rows(...)`.
- Produces: `Allows(Rows, "dataGrid"|"list"|"keyValue") == true`; `Apply` füllt `columns`+`rows` (dataGrid, Kappung 1000), `items` (list: Spalte 0→title, Spalte 1→text, Kappung 50), `pairs` (keyValue: exakt 2 Spalten, Kappung 20); `Sample(Rows)` liefert **2 Spalten**; `ExpectedShape`/`AllowedTypes` aktualisiert.

- [ ] **Step 1: Failing Tests** (Muster der Datei übernehmen — sie ruft `CanvasDataMapper.Apply` intern via `InternalsVisibleTo` bzw. liegt im selben Assembly-Testprojekt):

```csharp
[Fact]
public void Rows_fill_a_dataGrid_and_cap_at_1000()
{
    var data = CanvasData.Rows(new[] { "A" }, Enumerable.Range(0, 1200).Select(i => new[] { $"{i}" }));
    var props = CanvasDataMapper.Apply("dataGrid", null, data, out var error, out var truncated);
    Assert.Null(error);
    Assert.True(truncated);
    Assert.Equal(1000, props!.Value.GetProperty("rows").GetArrayLength());
}

[Fact]
public void Rows_fill_a_list_with_title_and_text()
{
    var data = CanvasData.Rows(new[] { "Titel", "Text" }, new[] { new[] { "Auftrag", "offen" } });
    var props = CanvasDataMapper.Apply("list", null, data, out var error, out _);
    Assert.Null(error);
    var item = props!.Value.GetProperty("items")[0];
    Assert.Equal("Auftrag", item.GetProperty("title").GetString());
    Assert.Equal("offen", item.GetProperty("text").GetString());
}

[Fact]
public void Rows_with_one_column_fill_a_list_without_text()
{
    var data = CanvasData.Rows(new[] { "Titel" }, new[] { new[] { "Auftrag" } });
    var props = CanvasDataMapper.Apply("list", null, data, out var error, out _);
    Assert.Null(error);
    Assert.False(props!.Value.GetProperty("items")[0].TryGetProperty("text", out _));
}

[Fact]
public void Rows_fill_keyValue_pairs()
{
    var data = CanvasData.Rows(new[] { "K", "V" }, new[] { new[] { "Status", "offen" } });
    var props = CanvasDataMapper.Apply("keyValue", null, data, out var error, out _);
    Assert.Null(error);
    var pair = props!.Value.GetProperty("pairs")[0];
    Assert.Equal("Status", pair.GetProperty("key").GetString());
    Assert.Equal("offen", pair.GetProperty("value").GetString());
}

[Fact]
public void Rows_with_three_columns_cannot_fill_keyValue()
{
    var data = CanvasData.Rows(new[] { "A", "B", "C" }, new[] { new[] { "1", "2", "3" } });
    CanvasDataMapper.Apply("keyValue", null, data, out var error, out _);
    Assert.Contains("2-column", error);
}

[Fact]
public void Rows_sample_has_two_columns() =>
    Assert.Equal(2, ((CanvasRowData)CanvasDataMapper.Sample(CanvasDataShape.Rows)).Columns.Count);
```

- [ ] **Step 2: FAIL bestätigen** — `--filter "FullyQualifiedName~CanvasDataMapperTests"`

- [ ] **Step 3: Implementieren** in `CanvasDataMapper.cs`:

```csharp
public static bool Allows(CanvasDataShape shape, string nodeType) => shape switch
{
    CanvasDataShape.Scalar => nodeType is "stat" or "badge" or "progress",
    CanvasDataShape.Series => nodeType is "lineChart" or "areaChart" or "barChart",
    CanvasDataShape.Segments => nodeType is "donutChart",
    CanvasDataShape.Rows => nodeType is "table" or "dataGrid" or "list" or "keyValue",
    _ => false,
};
```

`AllowedTypes(Rows)` → `"table, dataGrid, list or keyValue"`. `Sample(Rows)` → `CanvasData.Rows(new[] { "—", "—" }, new[] { new[] { "—", "—" } })` (2 Spalten, damit der keyValue-Stand-in bei der Autorierungs-Validierung nicht fälschlich scheitert).

`Apply`, `case CanvasRowData rows:` ersetzen durch ein Switch über `nodeType`:

```csharp
case CanvasRowData rows:
    switch (nodeType)
    {
        case "table" or "dataGrid":
        {
            var cap = nodeType == "table" ? MaxTableRows : MaxGridRows;
            props["columns"] = new JsonArray(rows.Columns.Select(c => (JsonNode?)JsonValue.Create(c)).ToArray());
            var kept = rows.Rows;
            if (kept.Count > cap) { kept = kept.Take(cap).ToList(); truncated = true; }
            props["rows"] = new JsonArray(kept.Select(r => (JsonNode?)new JsonArray(
                r.Select(c => (JsonNode?)JsonValue.Create(c ?? string.Empty)).ToArray())).ToArray());
            break;
        }
        case "list":
        {
            var kept = rows.Rows;
            if (kept.Count > MaxListItems) { kept = kept.Take(MaxListItems).ToList(); truncated = true; }
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
            if (kept.Count > MaxKeyValuePairs) { kept = kept.Take(MaxKeyValuePairs).ToList(); truncated = true; }
            props["pairs"] = new JsonArray(kept.Select(r => (JsonNode?)new JsonObject
            {
                ["key"] = r.Count > 0 ? r[0] : string.Empty,
                ["value"] = r.Count > 1 ? r[1] : string.Empty,
            }).ToArray());
            break;
        }
    }
    break;
```

Konstanten neben `MaxTableRows`:

```csharp
/// <summary>A bound <c>dataGrid</c> renders at most this many rows.</summary>
internal const int MaxGridRows = 1000;

/// <summary>A bound <c>list</c> renders at most this many items.</summary>
internal const int MaxListItems = 50;

/// <summary>A bound <c>keyValue</c> renders at most this many pairs.</summary>
internal const int MaxKeyValuePairs = 20;
```

In `CanvasCatalog.ExpectedShape` ergänzen: `"dataGrid" or "list" or "keyValue" => "rows"` (Zeile mit `"table"` erweitern: `"table" or "dataGrid" or "list" or "keyValue" => "rows"`).

- [ ] **Step 4: PASS bestätigen**; ganze Suite einmal laufen lassen (Sample-Änderung kann bestehende Binding-Validierungs-Tests berühren — bei Assertions auf 1-Spalten-Sample die Tests auf 2 Spalten anpassen, das ist die gewollte neue Wahrheit).
- [ ] **Step 5: Commit** — `feat(canvas): Rows-Bindung für dataGrid, list und keyValue`

---

### Task 5: Renderer — sechs Leaves + CSS + SkeletonFor

**Files:**
- Modify: `DRYL.Components/Canvas/CanvasNodeView.razor`
- Modify: `DRYL.Components/Components/AI/DrylCanvas.razor.css`
- Test: `tests/DRYL.Components.Tests/Canvas/CanvasCatalogRenderTests.cs` (neu)

**Interfaces:**
- Consumes: Prop-Klassen aus Task 1; `DrylStat` (Label/Value/Delta/Direction/CountUp/Ai), `DrylList`/`DrylListItem` (Icon/ChildContent + `End`-Slot), `DrylDescriptionList` (`Columns`)/`DrylDescriptionItem` (`Term`), `DrylImage` (Src/Alt/Fit/Ratio als `DrylImage.ImageFit`/`DrylImage.ImageRatio`), `DrylCodeBlock` (Code/Language/ShowLineNumbers), `DrylEmptyState` (Icon/Title/Description).

- [ ] **Step 1: Failing bUnit-Tests** — neue Datei, Muster `DrylCanvasStandaloneTests` (BunitContext, `JSInterop.Mode = Loose`, `Services.AddDrylComponents()`, `Parse(json)`-Helper identisch übernehmen):

```csharp
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
        Assert.Contains("SELECT 1;", cut.Markup);
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
```

- [ ] **Step 2: FAIL bestätigen** — `--filter "FullyQualifiedName~CanvasCatalogRenderTests"` (Nodes rendern als `.canvas-invalid`, Assertions scheitern)

- [ ] **Step 3: Render-Cases in `CanvasNodeView.razor`** — im `switch (Node.Type)` vor `case "inputText"` einfügen:

```razor
case "kpi":
{
    var p = Props<CanvasKpiProps>();
    <div class="canvas-kpi">
        @foreach (var item in p!.Items!)
        {
            <DrylStat Label="@item.Label" Value="@item.Value" Delta="@item.Delta"
                      Direction="@item.ParsedDirection" CountUp Ai="AiState.Generated" />
        }
    </div>
    break;
}

case "list":
{
    var p = Props<CanvasListProps>();
    <DrylList>
        @foreach (var item in p!.Items!)
        {
            <DrylListItem Icon="@item.Icon">
                @item.Title
                @if (!string.IsNullOrWhiteSpace(item.Text))
                {
                    <span class="canvas-list-text">@item.Text</span>
                }
            </DrylListItem>
        }
    </DrylList>
    break;
}

case "keyValue":
{
    var p = Props<CanvasKeyValueProps>();
    <DrylDescriptionList Columns="@(p!.Columns ?? 1)">
        @foreach (var pair in p.Pairs!)
        {
            <DrylDescriptionItem Term="@pair.Key">@pair.Value</DrylDescriptionItem>
        }
    </DrylDescriptionList>
    break;
}

case "image":
{
    var p = Props<ImageNodeProps>();
    <DrylImage Src="@p!.Src!" Alt="@p.Alt!" Fit="MapImageFit(p.Fit)" Ratio="MapImageRatio(p.Ratio)"
               Rounded="DrylImage.ImageRounded.Md" />
    @if (!string.IsNullOrWhiteSpace(p.Caption))
    {
        <div class="canvas-image-caption">@p.Caption</div>
    }
    break;
}

case "code":
{
    var p = Props<CodeNodeProps>();
    <DrylCodeBlock Code="@p!.Code!" Language="@p.Language" ShowLineNumbers="@(p.LineNumbers == true)" />
    break;
}

case "emptyState":
{
    var p = Props<EmptyStateNodeProps>();
    <DrylEmptyState Icon="@p!.Icon" Title="@p.Title" Description="@p.Description" />
    break;
}
```

Mapper-Helfer im `@code`-Block (bei `MapGap`/`MapBadge`):

```csharp
private static DrylImage.ImageFit MapImageFit(string? fit) => fit switch
{
    "contain" => DrylImage.ImageFit.Contain,
    _ => DrylImage.ImageFit.Cover,
};

private static DrylImage.ImageRatio MapImageRatio(string? ratio) => ratio switch
{
    "1:1" => DrylImage.ImageRatio.Square,
    "16:9" => DrylImage.ImageRatio.Video,
    "21:9" => DrylImage.ImageRatio.Wide,
    _ => DrylImage.ImageRatio.Auto,
};
```

`SkeletonFor` erweitern:

```csharp
private static DrylSkeleton.SkeletonVariant SkeletonFor(string type) => type switch
{
    "lineChart" or "areaChart" or "barChart" or "donutChart" or "table"
        or "dataGrid" or "list" or "keyValue" or "image" or "code" or "accordion" =>
        DrylSkeleton.SkeletonVariant.Card,
    _ => DrylSkeleton.SkeletonVariant.Text,
};
```

- [ ] **Step 4: CSS in `DrylCanvas.razor.css`** — bei den anderen `.canvas-*`-Regeln (Stil der Datei übernehmen, nur Tokens; `::deep`, wo Kind-Komponenten getroffen werden):

```css
/* kpi — a wrapping row of compact stats; tiles share the width and stack when narrow. */
::deep .canvas-kpi {
    display: flex;
    flex-wrap: wrap;
    gap: var(--sp-3);
}

::deep .canvas-kpi > * {
    flex: 1 1 140px;
    min-width: 0;
}

/* list — secondary text line inside a canvas list item. */
::deep .canvas-list-text {
    display: block;
    color: var(--fg-dim);
    font-size: var(--fs-sm);
}

/* image — quiet caption line below a canvas image. */
::deep .canvas-image-caption {
    margin-top: var(--sp-1);
    color: var(--fg-dim);
    font-size: var(--fs-sm);
}
```

(Vorher in der Datei prüfen, ob `.canvas-*`-Regeln dort mit oder ohne `::deep` geschrieben sind — exakt dem Bestand folgen; `--fs-sm` nur verwenden, wenn der Token existiert, sonst den in der Datei üblichen Font-Size-Token nehmen.)

- [ ] **Step 5: PASS bestätigen** — Render-Tests + ganze Suite `-f net10.0`.
- [ ] **Step 6: Commit** — `feat(canvas): kpi, list, keyValue, image, code, emptyState rendern`

---

### Task 6: Renderer — accordion + form (Submit, Pflichtfeld-Hinweise)

**Files:**
- Create: `DRYL.Components/Canvas/CanvasFormScope.cs`
- Modify: `DRYL.Components/Canvas/CanvasNodeView.razor`
- Modify: `DRYL.Components/Components/AI/DrylCanvas.razor.css`
- Test: `tests/DRYL.Components.Tests/Canvas/CanvasCatalogRenderTests.cs`, `tests/DRYL.Components.Tests/Canvas/CanvasActionRenderTests.cs` (Muster für Action-Stubs: `CanvasActionStubs.cs`)

**Interfaces:**
- Consumes: `CanvasActionRunner.InvokeAsync(string nodeId, string? label, CanvasActionBinding action)`, `Ctx.Actions`/`Ctx.Form` aus `CanvasContext`, `DrylExpansion` (Title/Open/OpenChanged/ChildContent).
- Produces: `internal sealed class CanvasFormScope { event Action? OnChanged; bool IsMissing(string name); internal void SetMissing(IEnumerable<string>); internal void Clear(string name) }` — kaskadiert vom form-Node an die interaktiven Kind-Views.

- [ ] **Step 1: Failing Tests**

In `CanvasCatalogRenderTests`:

```csharp
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
    // Section 0 ist initial offen (aria-expanded der DrylExpansion-Header prüfen).
    var headers = cut.FindAll("[aria-expanded]");
    Assert.Contains(headers, h => h.GetAttribute("aria-expanded") == "true");
}
```

In `CanvasActionRenderTests` (die Datei registriert Aktionen über die bestehenden Stubs — deren Setup-Helper wiederverwenden):

```csharp
[Fact]
public void Form_submit_with_empty_required_field_shows_hint_and_does_not_invoke()
{
    var invoked = false;
    // Aktion "order.create" via Stub registrieren, Handler setzt invoked = true (Muster der Datei).
    var cut = RenderFormCanvas(); // Helper unten
    cut.Find(".canvas-form-submit button").Click();
    Assert.False(invoked);
    Assert.Contains("canvas-field-required", cut.Markup);
}

[Fact]
public void Form_submit_with_filled_required_field_invokes_action()
{
    var invoked = false;
    var cut = RenderFormCanvas();
    cut.Find("[data-cid='f1'] input").Change("ACME");
    cut.Find(".canvas-form-submit button").Click();
    Assert.True(invoked);
}
```

Der Form-Spec für beide Tests:

```json
{"root":{"id":"root","type":"form",
  "props":{"submitLabel":"Anlegen","required":["customer"]},
  "action":{"name":"order.create","args":{"customer":{"$field":"customer"}}},
  "children":[{"id":"f1","type":"inputText","props":{"name":"customer","label":"Kunde"}}]}}
```

(`RenderFormCanvas` als privater Helper in der Testklasse: rendert `DrylCanvas` mit diesem Spec und dem Action-Service-Setup der Datei.)

- [ ] **Step 2: FAIL bestätigen** — `--filter "FullyQualifiedName~CanvasCatalogRenderTests|FullyQualifiedName~CanvasActionRenderTests"`

- [ ] **Step 3: `CanvasFormScope.cs` anlegen**

```csharp
namespace DRYL.Components.Canvas;

/// <summary>
/// What a <c>form</c> node shares with its interactive children: which required fields failed
/// the last submit. Cascaded (fixed) from the form's view; children re-render via
/// <see cref="OnChanged"/>, and typing into a field clears its flag immediately.
/// </summary>
internal sealed class CanvasFormScope
{
    private readonly HashSet<string> _missing = new(StringComparer.Ordinal);

    /// <summary>Raised when the missing set changes; subscribed views re-render.</summary>
    public event Action? OnChanged;

    /// <summary>True when <paramref name="name"/> failed the last submit and has not been edited since.</summary>
    public bool IsMissing(string name) => _missing.Contains(name);

    internal void SetMissing(IEnumerable<string> names)
    {
        _missing.Clear();
        foreach (var n in names) _missing.Add(n);
        OnChanged?.Invoke();
    }

    internal void Clear(string name)
    {
        if (_missing.Remove(name)) OnChanged?.Invoke();
    }
}
```

- [ ] **Step 4: Render-Cases + Verdrahtung in `CanvasNodeView.razor`**

Cases (im `switch`, bei den Containern):

```razor
case "accordion":
{
    var p = Props<AccordionNodeProps>();
    var labels = p!.Labels!;
    var children = Node.Children!;
    @for (var i = 0; i < labels.Count; i++)
    {
        var idx = i;                 // capture per-iteration for the fragment
        var child = children[idx];
        <DrylExpansion Title="@labels[idx]"
                       Open="@_openSections.Contains(idx)"
                       OpenChanged="@(v => ToggleSection(idx, v))">
            <CanvasNodeView @key="child.Id" Node="child" />
        </DrylExpansion>
    }
    break;
}

case "form":
{
    var p = Props<FormNodeProps>();
    var actionState = Ctx.Actions?.StateOf(Node.Id);
    <div class="canvas-form">
        <CascadingValue Value="_formScope" IsFixed="true">
            @RenderChildren
        </CascadingValue>
        <div class="canvas-form-submit">
            <DrylButton Variant="DrylButton.ButtonVariant.Primary"
                        Loading="@(actionState?.Busy == true)"
                        OnClick="@(_ => SubmitFormAsync(p!))">
                @p!.SubmitLabel
            </DrylButton>
        </div>
        <DrylPresence Visible="@(actionState?.Error is not null)"
                      Transition="PresenceTransition.SlideUp" Speed="PresenceSpeed.Fast">
            <span class="canvas-action-error">
                <DrylIcon Name="Alert" Size="14" />
                @actionState?.Error
            </span>
        </DrylPresence>
    </div>
    break;
}
```

`@code`-Ergänzungen:

```csharp
[CascadingParameter] internal CanvasFormScope? FormScope { get; set; }

// Accordion: which sections are open — renderer-local UI state like the active tab,
// seeded once per node INSTANCE (same reuse trap as _seededNode).
private readonly HashSet<int> _openSections = new();
private CanvasNode? _seededAccordion;
private readonly CanvasFormScope _formScope = new();
private CanvasFormScope? _subscribedScope;

private void ToggleSection(int index, bool open)
{
    if (open) _openSections.Add(index);
    else _openSections.Remove(index);
}

private void SeedAccordionOnce()
{
    if (Node.Type != "accordion" || ReferenceEquals(_seededAccordion, Node)) return;
    _seededAccordion = Node;
    _openSections.Clear();
    if (Props<AccordionNodeProps>() is { Open: { } open }) _openSections.Add(open);
}

// A required hint only makes sense under a form; subscribe to the scope so SetMissing
// from the form's submit re-renders this field's view.
private void SyncFormScope()
{
    if (ReferenceEquals(_subscribedScope, FormScope)) return;
    if (_subscribedScope is not null) _subscribedScope.OnChanged -= OnFormScopeChanged;
    _subscribedScope = FormScope;
    if (_subscribedScope is not null) _subscribedScope.OnChanged += OnFormScopeChanged;
}

private void OnFormScopeChanged() => InvokeAsync(StateHasChanged);

// Missing = a required field whose value is null or a whitespace-only string.
private async Task SubmitFormAsync(FormNodeProps p)
{
    var missing = (p.Required ?? new List<string>())
        .Where(name => Ctx.Form.Get(name) is var v
            && (v is null || (v is string s && string.IsNullOrWhiteSpace(s))))
        .ToList();
    _formScope.SetMissing(missing);
    if (missing.Count > 0) return;

    if (Node.Action is { Name.Length: > 0 } action && Ctx.Actions is { } runner)
        await runner.InvokeAsync(Node.Id, p.SubmitLabel, action);
}
```

In `OnParametersSet()` ergänzen: `SeedAccordionOnce(); SyncFormScope();`. In `Dispose()` ergänzen: `if (_subscribedScope is not null) _subscribedScope.OnChanged -= OnFormScopeChanged;`.

Pflichtfeld-Hinweis: hinter den vier interaktiven Cases (`inputText`, `select`, `slider`, `toggle`) jeweils nach dem Input einfügen (Beispiel `inputText`; bei den anderen drei identisch mit deren `name`):

```razor
@RequiredHint(name)
```

mit dem Fragment und dem erweiterten Setter im `@code`-Block:

```csharp
private RenderFragment RequiredHint(string name) => __builder =>
{
    if (FormScope is null) return;
    <DrylPresence Visible="@FormScope.IsMissing(name)"
                  Transition="PresenceTransition.SlideUp" Speed="PresenceSpeed.Fast">
        <span class="canvas-field-required">
            <DrylIcon Name="Alert" Size="12" />
            Required
        </span>
    </DrylPresence>
};
```

und in den `ValueChanged`-Lambdas der vier interaktiven Cases zusätzlich das Missing-Flag löschen, z. B. `inputText`:

```razor
ValueChanged="@(v => { Ctx.Form.Set(name, v); FormScope?.Clear(name); })"
```

CSS (`DrylCanvas.razor.css`, Stil an `.canvas-action-error` der Datei anlehnen):

```css
/* form — submit row and the per-field required hint after a failed submit. */
::deep .canvas-form-submit {
    margin-top: var(--sp-3);
}

::deep .canvas-field-required {
    display: inline-flex;
    align-items: center;
    gap: var(--sp-1);
    margin-top: var(--sp-1);
    color: var(--danger);
    font-size: var(--fs-sm);
}
```

(Farb-Token für Danger exakt aus dem Bestand von `.canvas-action-error` übernehmen.)

- [ ] **Step 5: PASS bestätigen** — Filter aus Step 2, dann ganze Suite.
- [ ] **Step 6: Commit** — `feat(canvas): accordion-Sektionen und form-Container mit Submit auf eine Aktion`

---

### Task 7: Renderer — dataGrid über DrylTable

**Files:**
- Create: `DRYL.Components/Canvas/CanvasGridRow.cs`
- Modify: `DRYL.Components/Canvas/CanvasNodeView.razor`
- Test: `tests/DRYL.Components.Tests/Canvas/CanvasCatalogRenderTests.cs`

**Interfaces:**
- Consumes: `DrylTable<TItem>` (Items/ShowToolbar/Searchable/PageSize/AriaLabel/RowIdSelector), `DrylColumn<TItem>` (ColumnKey/Title/Field/Sortable/Searchable/Filterable/FilterType), `ColumnFilterType.Select`.
- Produces: `internal sealed record CanvasGridRow(int Index, IReadOnlyList<string> Cells) { public string Cell(int i); }`.

- [ ] **Step 1: Failing Tests**

```csharp
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
    var cut = RenderSpec($$"""
        {"id":"g","type":"dataGrid","props":{"columns":["A"],"rows":[{{rows}}],"pageSize":10}}
        """);
    Assert.Equal(10, cut.FindAll("tbody tr").Count);   // Seite 1
    Assert.NotEmpty(cut.FindAll(".tbl-footer"));        // Pagination sichtbar
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
```

- [ ] **Step 2: FAIL bestätigen**

- [ ] **Step 3: `CanvasGridRow.cs`**

```csharp
namespace DRYL.Components.Canvas;

/// <summary>One row of a <c>dataGrid</c> node — string cells plus a stable index for row identity.</summary>
internal sealed record CanvasGridRow(int Index, IReadOnlyList<string> Cells)
{
    /// <summary>The cell at <paramref name="i"/>, or an empty string when the row is short.</summary>
    public string Cell(int i) => i >= 0 && i < Cells.Count ? Cells[i] ?? string.Empty : string.Empty;
}
```

- [ ] **Step 4: Render-Case in `CanvasNodeView.razor`** (bei den anderen Daten-Leaves):

```razor
case "dataGrid":
{
    var p = Props<DataGridNodeProps>();
    var rows = GridRows(p!);
    var sortable = p!.Sortable ?? true;
    var searchable = p.Searchable == true;
    var filterable = p.Filterable == true;
    var pageSize = p.PageSize ?? 10;
    // Paging only when it has something to page — a footer under three rows is noise.
    var effectivePageSize = pageSize > 0 && rows.Count > pageSize ? pageSize : 0;
    <DrylTable TItem="CanvasGridRow" Items="@rows"
               ShowToolbar="@searchable" Searchable="@searchable"
               PageSize="@effectivePageSize"
               AriaLabel="Data grid"
               RowIdSelector="@(r => r.Index)">
        <Columns>
            @for (var i = 0; i < p.Columns!.Count; i++)
            {
                var idx = i;             // capture per-iteration for the expression
                <DrylColumn TItem="CanvasGridRow"
                            ColumnKey="@($"c{idx}")" Title="@p.Columns[idx]"
                            Field="@(r => r.Cell(idx))"
                            Sortable="@sortable" Searchable="@searchable"
                            Filterable="@filterable"
                            FilterType="@(filterable ? ColumnFilterType.Select : ColumnFilterType.Auto)" />
            }
        </Columns>
    </DrylTable>
    break;
}
```

`@code`-Ergänzung (Referenz-Cache — `Props<T>` liefert pro Version dasselbe Objekt, damit bleibt auch die `Items`-Referenz stabil, was `DrylTable`s `itemsChanged`-Logik und A8 trägt):

```csharp
private object? _gridPropsRef;
private IReadOnlyList<CanvasGridRow> _gridRows = [];

private IReadOnlyList<CanvasGridRow> GridRows(DataGridNodeProps p)
{
    if (ReferenceEquals(_gridPropsRef, p)) return _gridRows;
    _gridPropsRef = p;
    _gridRows = (p.Rows ?? []).Select((cells, i) => new CanvasGridRow(i, cells)).ToList();
    return _gridRows;
}
```

- [ ] **Step 5: PASS bestätigen** — dazu ein Binding-Smoke-Test in `CanvasBindingRenderTests` (Muster der Datei mit registrierter Rows-Quelle): eine Rows-Quelle an einen `dataGrid`-Node binden und asserten, dass Zellen der Quelle im Markup stehen.
- [ ] **Step 6: Commit** — `feat(canvas): dataGrid rendert über DrylTable mit Sortierung, Filter und Paging`

---

### Task 8: Schema, Prompt-Blöcke, LayoutBudget (Agents) + Budget-Wächter

**Files:**
- Modify: `DRYL.Components.Agents/Canvas/CanvasPrompt.cs`
- Modify: `DRYL.Components/Canvas/CanvasDataPrompt.cs`
- Modify: `DRYL.Components/Canvas/CanvasCatalog.cs` (KnownTypes-Accessor)
- Test: `tests/DRYL.Components.Tests/Agents/Canvas/CanvasPromptTests.cs`

**Interfaces:**
- Produces: `CanvasCatalog.KnownTypes` (`public static IReadOnlyCollection<string>`) für den Schema-Abgleich; `CanvasPrompt.SchemaText` mit neun neuen Zeilen.

- [ ] **Step 1: Failing Tests** — in `CanvasPromptTests` anhängen:

```csharp
[Fact]
public void Schema_mentions_every_catalog_type()
{
    foreach (var type in CanvasCatalog.KnownTypes)
        Assert.Contains(type, CanvasPrompt.SchemaText);
}

[Fact]
public void Schema_stays_under_budget() =>
    Assert.InRange(CanvasPrompt.SchemaText.Length, 1, 4500);

[Fact]
public void Schema_lists_new_container_types() =>
    Assert.Contains("stack, grid, card, tabs, accordion, form", CanvasPrompt.SchemaText);

[Fact]
public void Data_prompt_maps_rows_to_all_row_types()
{
    var block = CanvasDataPrompt.Block(new[]
    {
        new CanvasDataDescriptor("orders.open", "Offene Aufträge", CanvasDataShape.Rows,
            Array.Empty<CanvasDataParam>()),
    });
    Assert.Contains("rows -> table|dataGrid|list|keyValue", block);
}

[Fact]
public void Layout_budget_mentions_dataGrid_below_480()
{
    Assert.Contains("dataGrid", CanvasPrompt.LayoutBudget(400));
    Assert.Contains("kpi", CanvasPrompt.LayoutBudget(400));
}
```

(Descriptor-Konstruktion an die tatsächliche `CanvasDataDescriptor`-API der Codebasis anpassen — Signatur in `CanvasDataRegistry.cs` nachschlagen.)

- [ ] **Step 2: FAIL bestätigen** — `--filter "FullyQualifiedName~CanvasPromptTests"` (KnownTypes existiert noch nicht → Compile-Fehler ist das erwartete „Fail").

- [ ] **Step 3: Implementieren**

`CanvasCatalog`:

```csharp
/// <summary>Every known catalog type — the schema/prompt layer keeps itself in sync against this.</summary>
public static IReadOnlyCollection<string> KnownTypes => AllTypes;
```

`CanvasPrompt.SchemaText` — die `children`-Zeile ersetzen durch:

```
- "children" only on container types: stack, grid, card, tabs, accordion, form.
```

und nach der `button`-Zeile die neuen Typzeilen einfügen:

```
- dataGrid { "columns": string[], "rows": string[][]?, "sortable": boolean?, "filterable": boolean?, "searchable": boolean?, "pageSize": number? } — interactive table (sort/filter/search/paging). Max 12 columns, max 100 literal rows; bind a rows source for more. Use `table` only for small static tables.
- form { "submitLabel": string, "required": string[]? } — container; bundles its interactive children into ONE action. The action binding sits on the form node itself; a submit button is rendered automatically. Prefer one form over separate buttons per field.
- kpi { "items": [{ "label": string, "value": string, "delta": string?, "direction": "up"|"down"|"neutral"? }] } — one row of 1..6 compact stats.
- list { "items": [{ "title": string, "text": string?, "icon": string? }] } — vertical list, max 50 items.
- keyValue { "pairs": [{ "key": string, "value": string }], "columns": 1|2? } — label/value pairs, max 20.
- accordion { "labels": string[], "open": number? } — collapsible sections, exactly one child per label; "open" is the initially expanded index.
- image { "src": string, "alt": string, "ratio": "auto"|"1:1"|"16:9"|"21:9"?, "fit": "cover"|"contain"?, "caption": string? } — src must start with https://, / or data:image/.
- code { "code": string, "language": string?, "lineNumbers": boolean? } — read-only source block.
- emptyState { "title": string, "description": string?, "icon": string? } — use for "nothing here yet" views instead of empty markdown.
```

`CanvasDataPrompt.Block` — die Shape-Map-Zeile ersetzen:

```
Shapes map to types: scalar -> stat|badge|progress, series -> lineChart|areaChart|barChart,
segments -> donutChart, rows -> table|dataGrid|list|keyValue (keyValue needs exactly 2 columns).
```

`CanvasPrompt.LayoutBudget` — je Stufe eine Zeile ergänzen:

- `< 480`: `- dataGrid: at most 3 columns. kpi: at most 2 items.`
- `< 900`: `- dataGrid: at most 5 columns. kpi: at most 4 items.`
- default: `- dataGrid: at most 8 columns.`

- [ ] **Step 4: PASS bestätigen** — Prompt-Tests + ganze Suite (bestehende Prompt-Snapshot-Tests können den neuen Text erwarten müssen — anpassen, sie pinnen bewusst den Vertrag).
- [ ] **Step 5: Commit** — `feat(canvas): Schema-Zeilen für die neun Phase-4-Typen + Budget-Wächter`

---

### Task 9: Replay-Vertragstest — alle neun Typen in einem Artefakt

**Files:**
- Test: `tests/DRYL.Components.Tests/Canvas/CanvasCatalogRenderTests.cs`

**Interfaces:** — nur konsumierend.

- [ ] **Step 1: Test schreiben** (er muss nach Tasks 1–7 direkt grün sein — er pinnt den Vertrag, dass ein Artefakt mit allen neun Typen ohne `.canvas-invalid` durchrendert):

```csharp
[Fact]
public void An_artifact_with_all_nine_new_types_renders_without_invalid_nodes()
{
    var cut = RenderSpec("""
        {"id":"root","type":"stack","children":[
            {"id":"k","type":"kpi","props":{"items":[{"label":"Umsatz","value":"48k"}]}},
            {"id":"g","type":"dataGrid","props":{"columns":["A"],"rows":[["1"]]}},
            {"id":"l","type":"list","props":{"items":[{"title":"Eintrag"}]}},
            {"id":"kv","type":"keyValue","props":{"pairs":[{"key":"K","value":"V"}]}},
            {"id":"a","type":"accordion","props":{"labels":["S"]},"children":[
                {"id":"a1","type":"markdown","props":{"content":"Inhalt"}}]},
            {"id":"i","type":"image","props":{"src":"/img/a.png","alt":"Bild"}},
            {"id":"c","type":"code","props":{"code":"SELECT 1;"}},
            {"id":"e","type":"emptyState","props":{"title":"Leer"}},
            {"id":"f","type":"form","props":{"submitLabel":"Go"},
             "action":{"name":"noop"},
             "children":[{"id":"f1","type":"inputText","props":{"name":"x","label":"X"}}]}]}
        """);
    Assert.Empty(cut.FindAll(".canvas-invalid"));
    Assert.Empty(cut.FindAll(".canvas-data-error"));
}
```

- [ ] **Step 2: PASS bestätigen** — falls FAIL: das ist ein echter Regressionsfund aus Tasks 1–7; Ursache dort fixen, nicht den Test aufweichen.
- [ ] **Step 3: Commit** — `test(canvas): Replay-Vertrag — alle neun Phase-4-Typen rendern sauber`

---

### Task 10: Website — Demo + ComponentCatalog

**Files:**
- Modify: `c:\Users\janzi\Desktop\DRYL\DRYL.Website` — die Canvas-Demo-Seite (`Components/Pages/DemoAiCanvas.razor`) und/oder die Workspace-Demo aus Phase 3; `Components/ComponentCatalog.cs`
- Kein Test (Website-Projekt).

**Interfaces:** — konsumiert nur das neue Paketverhalten via ProjectReference.

- [ ] **Step 1: Replay-Beispiel „Auftrags-Cockpit" ergänzen** — auf der Canvas-Demo-Seite ein weiteres Replay-Artefakt (Muster der vorhandenen Replay-Beispiele der Seite exakt übernehmen), Spec-JSON:

```json
{"title":"Auftrags-Cockpit","root":{"id":"root","type":"stack","children":[
  {"id":"kpis","type":"kpi","props":{"items":[
    {"label":"Offen","value":"38","delta":"+4","direction":"up"},
    {"label":"Umsatz","value":"€48k","delta":"+12%","direction":"up"},
    {"label":"Reklamationen","value":"3","delta":"-2","direction":"down"}]}},
  {"id":"grid","type":"dataGrid","props":{
    "columns":["Auftrag","Kunde","Status","Summe"],
    "rows":[["4711","ACME","offen","€1.200"],["4712","Globex","erledigt","€860"],
            ["4713","Initech","offen","€2.400"],["4714","Umbrella","geprüft","€540"]],
    "sortable":true,"searchable":true,"pageSize":10}},
  {"id":"details","type":"accordion","props":{"labels":["Stammdaten","Notizen"],"open":0},"children":[
    {"id":"kv","type":"keyValue","props":{"pairs":[
      {"key":"Kunde","value":"ACME"},{"key":"Zahlungsziel","value":"14 Tage"}],"columns":2}},
    {"id":"note","type":"markdown","props":{"content":"Lieferung ab KW 32 möglich."}}]},
  {"id":"anlegen","type":"form","props":{"submitLabel":"Auftrag anlegen","required":["kunde"]},
   "action":{"name":"order.create","args":{"customer":{"$field":"kunde"}}},
   "children":[{"id":"kunde","type":"inputText","props":{"name":"kunde","label":"Kunde"}}]}]}}
```

Für die Replay-Variante die Demo-Aktion `order.create` registrieren wie die Phase-2/3-Demos es tun (Muster der Seite); Live-Variante bleibt hinter dem bestehenden Umgebungs-Flag.

- [ ] **Step 2: `ComponentCatalog` aktualisieren** — die Beschreibungstexte der Canvas-Einträge (DrylAiCanvas/DrylCanvas/Workspace) um die neuen Typen ergänzen; **keine** neuen Einträge.
- [ ] **Step 3: Website bauen:** `dotnet build c:\Users\janzi\Desktop\DRYL\DRYL.Website` — Expected: Build succeeded.
- [ ] **Step 4: Commit im Website-Repo** (eigenes Repo/Arbeitsverzeichnis beachten) — `feat(canvas): Auftrags-Cockpit-Demo für die Phase-4-Typen`

---

### Task 11: Changelog, Versionen, Gesamtverifikation

**Files:**
- Modify: `CHANGELOG.md`, `DRYL.Components/DRYL.Components.csproj`, `DRYL.Components.Agents/DRYL.Components.Agents.csproj`

- [ ] **Step 1: Ganze Suite über alle Frameworks:** `dotnet test tests/DRYL.Components.Tests` — Expected: alle grün (net8/9/10).
- [ ] **Step 2: `CHANGELOG.md`** — unter `[Unreleased]` sammeln und im selben Schritt Release schneiden: `## [2.15.0] - 2026-07-25` (Kern) mit `Added`-Bullets für die neun Typen + Rows-Mapper-Weitung + `CanvasCatalog.KnownTypes`; Agents-Abschnitt der Datei (dem Bestandsmuster für Agents-Releases folgen): `0.13.0` mit Schema-/LayoutBudget-Zeilen. Frisches, leeres `[Unreleased]` oben.
- [ ] **Step 3: Versionen bumpen** — Kern-`<Version>` → `2.15.0`, Agents-`<Version>` → `0.13.0`.
- [ ] **Step 4: DoD-Sichtprüfung** (Roadmap §6): beide Farbmodi + 375 px + `prefers-reduced-motion` auf der Demo-Seite prüfen (Skill `verify`/Playwright); A8: dataGrid-Refresh pulst statt neu aufzubauen.
- [ ] **Step 5: Memory fortschreiben** — `project_ai_canvas`-Notiz um Phase 4 ergänzen (neun Typen, CanvasFormScope, Sample-2-Spalten-Gotcha, Budget-Wächter 4500).
- [ ] **Step 6: Commit** — `feat(canvas): Katalog-Ausbau Phase 4 — 2.15.0 / 0.13.0` (Changelog + beide csproj), dann Push nach Freigabe des Nutzers.

---

## Self-Review (erledigt)

- **Spec-Abdeckung:** §2.1→T3/T4/T7, §2.2→T2/T6, §2.3–2.9→T1/T5/T6, Skeleton→T5, §3→T8, §4→T4, §5→in T5–T7 verankert + T11 Step 4, §6→T1–T9, §7→T10/T11. Keine Lücken.
- **Platzhalter:** keine — alle Code-Schritte tragen konkreten Code; wo Bestands-Helper referenziert werden (`ContextWithAction`, Replay-Muster der Demo-Seite), ist die Quelle benannt.
- **Typ-Konsistenz:** `CanvasKpiProps`/`CanvasListProps`/`CanvasKeyValueProps` (CanvasPropTypes.cs) vs. `ImageNodeProps`/`CodeNodeProps`/`EmptyStateNodeProps`/`AccordionNodeProps`/`FormNodeProps`/`DataGridNodeProps` (CanvasCatalog.cs) — konsistent zwischen T1/T2/T3 und den Renderer-Tasks T5–T7; `CanvasGridRow.Cell(int)` in T7 deckt sich mit dem `Field`-Ausdruck; `CanvasFormScope`-API in T6 einheitlich.
