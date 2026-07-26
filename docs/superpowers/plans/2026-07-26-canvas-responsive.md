# Sidequest R — Responsive-Härtung des Canvas — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Ein Canvas-Artefakt bleibt in jedem Slot heil — der Donut skaliert auf seinen Platz statt
starr 260 px breit zu sein, und `.canvas-body` wird zum benannten Container-Kontext, damit
Canvas-Regeln auf die Breite des Canvas reagieren statt auf die des Viewports.

**Architecture:** Zwei kleine CSS-Änderungen (`dryl.css`, `DrylCanvas.razor.css`), abgesichert
durch CSS-Vertragstests nach dem Muster von `Theming/DrylCssDerivationTests`, danach ein
gemessener Playwright-Durchlauf bei 375 px und in einem 320-px-Slot, dessen Funde gefixt werden.

**Tech Stack:** .NET 10 / Blazor, xUnit + bUnit (`tests/DRYL.Components.Tests`), Playwright-MCP
gegen die Docs-Website (`../DRYL.Website`, `dotnet run --launch-profile http` → http://localhost:5044).

## Global Constraints

- Spec: `docs/superpowers/specs/2026-07-26-canvas-responsive-design.md`.
- **Nur Tokens, keine Literale** — Ausnahme: px-Werte in `@container`/`@media`-Bedingungen und die
  bestehende Breakpoint-Skala (Sm 480 · Md 768 · Lg 1024 · Xl 1280), weil `var()` dort illegal ist.
- Keine neuen Tokens, keine neuen Durations/Easings, keine neue Farbe.
- Container-Abfragen des Canvas sind **benannt**: `@container canvas (…)`. Anonyme Abfragen würden
  vom nächstgelegenen `.cq`-Wrapper (DrylGrid, DrylStack, DrylPagination, DrylDescriptionList)
  abgefangen.
- Beide Farbmodi und `prefers-reduced-motion: reduce` gelten für jeden Fix.
- Version: **Kern 2.17.0 → 2.17.1** (PATCH). `DRYL.Components.Agents` bleibt bei 0.14.0, es sei
  denn Task 4 findet einen Bruch im Dock.
- Tests laufen mit `dotnet test tests/DRYL.Components.Tests` aus dem Repo-Root.

---

### Task 1: Der Donut skaliert auf seinen Slot

**Files:**
- Modify: `DRYL.Components/wwwroot/dryl.css` (`.donut-box`, ca. Zeile 5708)
- Test: `tests/DRYL.Components.Tests/Canvas/CanvasResponsiveCssTests.cs` (neu)

**Interfaces:**
- Consumes: nichts.
- Produces: die Helfer `ReadDrylCss()` und `ReadCanvasCss()` in
  `CanvasResponsiveCssTests`, die Task 2 weiterverwendet.

- [ ] **Step 1: Write the failing test**

Neue Datei `tests/DRYL.Components.Tests/Canvas/CanvasResponsiveCssTests.cs`:

```csharp
namespace DRYL.Components.Tests.Canvas;

/// <summary>
/// Guards the responsive contract of the canvas surface (Sidequest R): the donut sizes to
/// min(height, available width) instead of a fixed 260px square, and .canvas-body is a NAMED
/// container context so canvas rules react to the canvas width, not the viewport.
/// Layout itself is verified with Playwright; these tests only stop the rules from being
/// silently refactored away.
/// </summary>
public class CanvasResponsiveCssTests
{
    private static string ReadCss(params string[] parts)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(new[] { dir.FullName }.Concat(parts).ToArray());
            if (File.Exists(candidate)) return File.ReadAllText(candidate);
            dir = dir.Parent;
        }
        throw new FileNotFoundException(string.Join('/', parts) + " not found from " + AppContext.BaseDirectory);
    }

    private static string ReadDrylCss() => ReadCss("DRYL.Components", "wwwroot", "dryl.css");

    private static string ReadCanvasCss() =>
        ReadCss("DRYL.Components", "Components", "AI", "DrylCanvas.razor.css");

    [Fact]
    public void Donut_root_is_a_query_container()
    {
        // The box cannot query itself — the container-type has to sit on the chart root.
        Assert.Contains(".chart-kind-donut { container-type: inline-size; }", ReadDrylCss());
    }

    [Fact]
    public void Donut_box_is_capped_by_its_available_width()
    {
        Assert.Contains("height: min(var(--chart-h, 260px), 100cqw);", ReadDrylCss());
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/DRYL.Components.Tests --filter CanvasResponsiveCssTests`
Expected: FAIL — beide Assertions finden ihre Zeile nicht.

- [ ] **Step 3: Write minimal implementation**

In `DRYL.Components/wwwroot/dryl.css`, im Abschnitt `/* ── Donut ── */`, `.donut-box` ersetzen:

```css
/* ── Donut ── */
/* The wheel is square (see .donut-slice), so a fixed height is also a fixed WIDTH: in any
   slot narrower than --chart-h it used to spill out both sides. The chart root is a query
   container and the box takes min(wanted height, available width) — the wheel shrinks to
   its slot instead of overflowing it. Tooltip anchors and --donut-hole are percentages and
   ride along. A container never queries itself, hence the type on the root. */
.chart-kind-donut { container-type: inline-size; }
.donut-box {
  position: relative;
  height: min(var(--chart-h, 260px), 100cqw);
  display: flex;
  justify-content: center;
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/DRYL.Components.Tests --filter CanvasResponsiveCssTests`
Expected: PASS (2 Tests).

- [ ] **Step 5: Commit**

```bash
git add DRYL.Components/wwwroot/dryl.css tests/DRYL.Components.Tests/Canvas/CanvasResponsiveCssTests.cs
git commit -m "fix(charts): the donut sizes to its slot instead of a fixed 260px square"
```

---

### Task 2: `.canvas-body` wird benannter Container-Kontext

**Files:**
- Modify: `DRYL.Components/Components/AI/DrylCanvas.razor.css` (`.canvas-body`, ca. Zeile 77)
- Test: `tests/DRYL.Components.Tests/Canvas/CanvasResponsiveCssTests.cs` (aus Task 1)

**Interfaces:**
- Consumes: `ReadCanvasCss()` aus Task 1.
- Produces: den Containernamen `canvas`, gegen den Task 4 seine Fixes schreibt.

- [ ] **Step 1: Write the failing test**

An `CanvasResponsiveCssTests` anhängen:

```csharp
    [Fact]
    public void Canvas_body_is_a_named_query_container()
    {
        // Named, not anonymous: DrylGrid/DrylStack/DrylPagination/DrylDescriptionList each
        // bring their own .cq wrapper, and an anonymous query would bind to that nearer
        // container instead of the canvas.
        Assert.Contains("container: canvas / inline-size;", ReadCanvasCss());
    }

    [Fact]
    public void Canvas_container_queries_are_all_named()
    {
        var css = ReadCanvasCss();
        var anonymous = System.Text.RegularExpressions.Regex.Matches(css, @"@container\s*\(");
        Assert.True(anonymous.Count == 0,
            $"{anonymous.Count} anonymous @container query/queries in DrylCanvas.razor.css — "
            + "use `@container canvas (…)` so a node's own .cq wrapper cannot hijack it.");
    }
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/DRYL.Components.Tests --filter CanvasResponsiveCssTests`
Expected: `Canvas_body_is_a_named_query_container` FAIL; die drei anderen PASS
(`Canvas_container_queries_are_all_named` ist grün, solange es keine Abfragen gibt — das ist
gewollt, sie ist ein Wächter für Task 4).

- [ ] **Step 3: Write minimal implementation**

In `DRYL.Components/Components/AI/DrylCanvas.razor.css` den `.canvas-body`-Block ersetzen:

```css
/* The body is the query context for everything the canvas renders: a node adapts to the
   width of the CANVAS, never to the viewport — the same artifact lives in a narrow chat
   column and, one morph later, in a fullscreen overlay.

   Named on purpose. A container query binds to the nearest ancestor container, and four
   primitives (DrylGrid, DrylStack, DrylPagination, DrylDescriptionList) bring their own
   .cq wrapper along, which would swallow an anonymous query in node depth.

   Containment makes this element a containing block for position: fixed descendants —
   harmless here, because the tooltip bubble and every popover panel are portaled to
   <body> (dryl.tooltip, dryl.popover) and the fullscreen box sits ABOVE the body. */
.canvas-body {
    container: canvas / inline-size;
    display: flex;
    flex-direction: column;
    gap: var(--sp-4);
    padding: var(--sp-4);
    overflow: auto;
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/DRYL.Components.Tests --filter CanvasResponsiveCssTests`
Expected: PASS (4 Tests).

- [ ] **Step 5: Commit**

```bash
git add DRYL.Components/Components/AI/DrylCanvas.razor.css tests/DRYL.Components.Tests/Canvas/CanvasResponsiveCssTests.cs
git commit -m "feat(canvas): the canvas body is a named container context"
```

---

### Task 3: Die Messfläche

Die Website demonstriert 15 Node-Typen, aber keinen Chart-Typ außer `areaChart`, keinen
`donutChart`, keine `timeline`, kein `image`/`markdown`/`emptyState`/`tabs`/`badge`/`progress`.
Für den Durchlauf braucht es eine Seite, die *alle* zeigt. Sie ist ein Messwerkzeug, kein
Produkt — Task 6 löscht sie wieder.

**Files:**
- Create: `../DRYL.Website/Components/Pages/DevCanvasResponsive.razor`

**Interfaces:**
- Consumes: `DrylCanvas`, `DRYL.Components.Canvas.CanvasJson`.
- Produces: die Route `/dev/canvas-responsive` mit zwei Canvas-Instanzen — `#wide` in voller
  Breite, `#narrow` in einem 320-px-Slot.

- [ ] **Step 1: Create the fixture page**

```razor
@page "/dev/canvas-responsive"
@using DRYL.Components.Canvas

@*  Measurement fixture for Sidequest R — deleted once the run is documented. Two canvases
    with the same spec: one full width, one in a 320px slot, so a finding can be attributed
    to the container rather than the viewport. *@

<div class="col" style="gap: var(--sp-5);">
    <div id="wide"><DrylCanvas Spec="_spec" AllowExpand="false" /></div>
    <div id="narrow" style="width: 320px;"><DrylCanvas Spec="_spec" AllowExpand="false" /></div>
</div>

@code {
    private const string SpecJson = """
        {
          "title": "Responsive fixture",
          "root": { "id": "root", "type": "stack", "props": { "gap": "lg" }, "children": [
            { "id": "kpi", "type": "kpi", "props": { "items": [
                { "label": "Quarterly target", "value": "€1.2M" },
                { "label": "Service level", "value": "98.4%", "delta": "+0.6%", "direction": "up" },
                { "label": "Open claims", "value": "3", "delta": "-2", "direction": "down" }
            ] } },
            { "id": "cols", "type": "grid", "props": { "columns": 2 }, "children": [
              { "id": "donut", "type": "donutChart", "props": { "title": "Revenue split",
                  "labels": ["Direct", "Partner", "Online"], "values": [42, 31, 27] } },
              { "id": "bars", "type": "barChart", "props": { "title": "Orders per week",
                  "labels": ["W1", "W2", "W3", "W4"], "values": [12, 19, 9, 22] } }
            ] },
            { "id": "line", "type": "lineChart", "props": { "title": "Trend",
                "labels": ["Jan", "Feb", "Mar", "Apr", "May"], "values": [3, 8, 5, 11, 9] } },
            { "id": "tl", "type": "timeline", "props": { "items": [
                { "title": "Order received", "text": "Customer portal, 09:12" },
                { "title": "Credit check passed", "text": "Automatic" },
                { "title": "Shipped", "text": "DHL 1Z-8841-2290-1147-0022" }
            ] } },
            { "id": "kv", "type": "keyValue", "props": { "columns": 2, "items": [
                { "key": "Customer", "value": "Nordwind Handels GmbH" },
                { "key": "Reference", "value": "ORD-2026-0044831-XZ" }
            ] } },
            { "id": "tabs", "type": "tabs", "props": { "labels": ["Summary", "Notes"] }, "children": [
              { "id": "sum", "type": "markdown", "props": { "text":
                  "**Summary** — a long unbroken token to probe wrapping: ORD20260044831XZNORDWINDHANDELSGMBH" } },
              { "id": "notes", "type": "emptyState", "props": { "title": "No notes",
                  "text": "Nothing has been written down for this order yet." } }
            ] },
            { "id": "tbl", "type": "table", "props": {
                "columns": ["Order", "Customer", "Country", "Status", "Total"],
                "rows": [["ORD-4481", "Nordwind Handels GmbH", "Deutschland", "Shipped", "€ 12.480"],
                         ["ORD-4482", "Café Zeitgeist", "Österreich", "Open", "€ 940"]] } },
            { "id": "prog", "type": "progress", "props": { "value": 62, "label": "Fulfilment" } },
            { "id": "row", "type": "stack", "props": { "gap": "sm" }, "children": [
              { "id": "b1", "type": "badge", "props": { "text": "Priority", "variant": "warning" } },
              { "id": "st", "type": "stat", "props": { "label": "Backlog", "value": "128" } }
            ] }
          ] }
        }
        """;

    private static readonly CanvasSpec _spec = CanvasJson.Parse(SpecJson)!;
}
```

- [ ] **Step 2: Verify the fixture renders**

`cd ../DRYL.Website && dotnet build` — Expected: Build erfolgreich.
Falls ein Node-Typ als „settled and still invalid“ rendert, sind seine Props falsch: die
Wahrheit steht in `DRYL.Components/Canvas/CanvasCatalog.cs` und `CanvasPropTypes.cs` —
dort die erwarteten Feldnamen ablesen und die Fixture korrigieren, **nicht** den Katalog.

- [ ] **Step 3: Commit**

```bash
cd ../DRYL.Website && git add Components/Pages/DevCanvasResponsive.razor
git commit -m "chore(dev): canvas responsive measurement fixture"
```

---

### Task 4: Der gemessene Durchlauf

**Files:**
- Modify: `DRYL.Components/Components/AI/DrylCanvas.razor.css` (nur bei Funden)
- Modify: `tests/DRYL.Components.Tests/Canvas/CanvasResponsiveCssTests.cs` (nur bei Funden)
- Create: `docs/superpowers/plans/2026-07-26-canvas-responsive-measurements.md`

**Interfaces:**
- Consumes: den Containernamen `canvas` aus Task 2, die Fixture-Route aus Task 3.
- Produces: die Messtabelle, die Task 5 im Changelog und in der Projektnotiz zitiert.

- [ ] **Step 1: Start the site**

```bash
cd ../DRYL.Website && dotnet run --launch-profile http
```
Erwartung: http://localhost:5044 antwortet. **Nicht** `--no-launch-profile` + `ASPNETCORE_URLS`
verwenden — dann liefert der Dev-Handler jedes fingerprinted Asset als 500.

- [ ] **Step 2: Measure the donut, before/after**

Auf `/components/donut-chart` per `browser_run_code_unsafe` den Chart in einen schmalen Slot
zwingen und messen:

```js
const chart = document.querySelector('.chart-kind-donut');
const slot = chart.closest('.glass-card') ?? chart.parentElement;
slot.style.width = '200px';
await new Promise(r => requestAnimationFrame(() => requestAnimationFrame(r)));
const box = chart.querySelector('.donut-box');
const slice = chart.querySelector('.donut-slice');
return { slot: slot.getBoundingClientRect().width,
         box: box.getBoundingClientRect().width,
         sliceW: slice.getBoundingClientRect().width,
         sliceH: slice.getBoundingClientRect().height,
         overflowsLeft: slice.getBoundingClientRect().left < slot.getBoundingClientRect().left };
```

Erwartung nach Task 1: `sliceW ≈ sliceH ≈ 200` (nicht 260), `overflowsLeft === false`.
Zahlen notieren — sie sind der Beweis, nicht der Screenshot.

- [ ] **Step 3: Measure both canvases for overflow**

`/dev/canvas-responsive` öffnen, einmal bei Viewport 375 × 812 (`browser_resize`) und einmal bei
1440 × 900 (dort trägt der 320-px-Slot die Last). Pro Durchgang:

```js
const rows = [];
for (const surface of document.querySelectorAll('.canvas')) {
    const body = surface.querySelector('.canvas-body');
    const bodyRect = body.getBoundingClientRect();
    rows.push({ where: surface.closest('#narrow') ? 'narrow' : 'wide',
                what: 'canvas-body', scroll: body.scrollWidth, client: body.clientWidth });
    for (const node of body.querySelectorAll('.canvas-node')) {
        const r = node.getBoundingClientRect();
        // .tbl-wrap scrolls on purpose — that is not an overflow finding.
        const excused = node.querySelector('.tbl-wrap') !== null;
        if (!excused && (node.scrollWidth > node.clientWidth + 1
                         || r.right > bodyRect.right + 1 || r.left < bodyRect.left - 1)) {
            rows.push({ where: surface.closest('#narrow') ? 'narrow' : 'wide',
                        what: node.dataset.nodeId ?? node.className,
                        scroll: node.scrollWidth, client: node.clientWidth,
                        overRight: +(r.right - bodyRect.right).toFixed(1),
                        overLeft: +(bodyRect.left - r.left).toFixed(1) });
        }
    }
}
return rows;
```

Jede Zeile mit `overRight > 1` oder `overLeft > 1` oder `scroll > client + 1` ist ein Fund.

- [ ] **Step 4: Counter-check the containment risk**

Im schmalen Canvas einen Tooltip öffnen (über eine Node-Werkzeugleiste hovern, dafür vorher einen
Node anklicken) und ein Select-Panel öffnen, dann messen:

```js
const tip = document.querySelector('.tt-portal.is-open');
const trigger = document.querySelector('[data-tt]:hover');
return tip && trigger
  ? { tipParent: tip.parentElement.tagName,          // erwartet: BODY
      dx: Math.round(tip.getBoundingClientRect().left
                     - trigger.getBoundingClientRect().left) }
  : 'no tooltip open';
```

Erwartung: `tipParent === 'BODY'` und die Bubble sitzt am Trigger (|dx| im zweistelligen
Pixelbereich, nicht um die Canvas-Position verschoben). Dasselbe für ein offenes Popover-Panel
(`.popover-panel`). Schlägt das fehl, ist Task 2 zurückzunehmen und die Spec zu korrigieren —
nicht mit `!important` zu übertünchen.

- [ ] **Step 5: Fix what the run found**

Für jeden Fund gilt diese Entscheidungsregel:

| Ursache | Fix |
| --- | --- |
| Ein Element ragt in **jedem** Slot heraus (auch „wide") | Strukturfehler, kein Responsive-Thema: die betreffende Regel in `DrylCanvas.razor.css` korrigieren, ohne Container-Abfrage. |
| Eine Zeile klemmt nur unter einer Canvas-Breite | `@container canvas (max-width: 480px) { … }` in `DrylCanvas.razor.css`, Breakpoint aus der Skala (480/768/1024/1280), **nichts dazwischen**. |
| Ein Kind-Element scrollt absichtlich (`.tbl-wrap`) | kein Fund, nichts tun. |

Die zwei erwarteten Kandidaten, falls sie auftauchen — wörtlich so schreiben:

```css
/* The header keeps title and tools on one line until the canvas is too narrow for both. */
@container canvas (max-width: 480px) {
    .canvas-head { flex-wrap: wrap; }
}

/* The toolbar hangs off the node's corner by --sp-1. On a narrow canvas that overhang
   lands outside the body, where the canvas' own overflow: hidden cuts it off — so it
   tucks back inside. */
@container canvas (max-width: 480px) {
    ::deep .canvas-node-tools { top: 0; right: 0; }
}
```

Hinweis: `.canvas-head` liegt **außerhalb** von `.canvas-body` und damit außerhalb des Containers
`canvas` — eine `@container canvas`-Abfrage greift dort **nicht**. Wenn der Kopf ein Fund ist,
bekommt `.canvas` selbst zusätzlich `container: canvas-shell / inline-size` und die Regel lautet
`@container canvas-shell (max-width: 480px)`; die Namensregel aus Task 2 (kein anonymes
`@container`) gilt unverändert, und der Test aus Task 2 deckt beide Namen ab.

Jeder Fix bekommt eine Assertion in `CanvasResponsiveCssTests`, z. B.:

```csharp
    [Fact]
    public void Narrow_canvas_wraps_the_header()
    {
        Assert.Contains("@container canvas-shell (max-width: 480px)", ReadCanvasCss());
    }
```

- [ ] **Step 6: Re-measure after the fixes**

Step 3 wiederholen, bis keine Fundzeile mehr übrig ist. Danach denselben Lauf zweimal
wiederholen: einmal mit `data-dryl-mode="light"` auf `<html>`, einmal mit erzwungenem
`prefers-reduced-motion: reduce` (`browser_run_code_unsafe`:
`document.documentElement.setAttribute('data-dryl-mode','light')` bzw. Playwright-Emulation).
Erwartung: identische Zahlen — die Fixes sind Layout, nicht Farbe oder Bewegung.

- [ ] **Step 7: Write the measurement record**

`docs/superpowers/plans/2026-07-26-canvas-responsive-measurements.md` anlegen: je eine Tabelle für
Donut vorher/nachher, für den 375-px-Lauf und den 320-px-Slot-Lauf (vor und nach den Fixes), plus
das Ergebnis der Gegenprobe aus Step 4. Zahlen, keine Adjektive. Falls ein Fund im
`DrylCanvasDock` liegt: hier festhalten, im selben Commit fixen und in Task 5 den
**Agents-PATCH 0.14.0 → 0.14.1** mit einplanen — inklusive der Frage, wie das Agents-Paket
veröffentlicht wird (`publish.yml` publiziert es nicht automatisch mit).

- [ ] **Step 8: Clean up and run the tests**

Screenshots im Repo-Root löschen (Playwright legt sie dort ab), Server stoppen.
Run: `dotnet test tests/DRYL.Components.Tests`
Expected: alles grün.

- [ ] **Step 9: Commit**

```bash
git add DRYL.Components/Components/AI/DrylCanvas.razor.css \
        tests/DRYL.Components.Tests/Canvas/CanvasResponsiveCssTests.cs \
        docs/superpowers/plans/2026-07-26-canvas-responsive-measurements.md
git commit -m "fix(canvas): harden the artifact surface at narrow canvas widths"
```

---

### Task 5: Release

**Files:**
- Modify: `DRYL.Components/DRYL.Components.csproj:8`
- Modify: `CHANGELOG.md`
- Modify: `docs/superpowers/specs/2026-07-25-canvas-platform-roadmap.md:284-289,349`
- Modify: `/Users/deryl/.claude/projects/-Users-deryl-Desktop-DRYL-DRYL-Components/memory/project-ai-canvas.md`

**Interfaces:**
- Consumes: die Messtabelle aus Task 4.
- Produces: Version 2.17.1.

- [ ] **Step 1: Bump the version**

In `DRYL.Components/DRYL.Components.csproj`: `<Version>2.17.0</Version>` → `<Version>2.17.1</Version>`.
`DRYL.Components.Agents.csproj` bleibt bei `0.14.0` — außer Task 4 fand einen Dock-Bruch, dann
dort `0.14.1`.

- [ ] **Step 2: Cut the changelog release**

In `CHANGELOG.md` unter dem leeren `## [Unreleased]` einen neuen Block einziehen:

```markdown
## [2.17.1] — 2026-07-26

Sidequest R — **Responsive**. Ein Artefakt weiß jetzt, wie breit es wirklich ist.

### Fixed
- `DrylDonutChart` — Das Rad war immer so breit wie hoch (260 px), unabhängig vom Slot, und lief in
  jedem schmaleren Container an beiden Seiten heraus. Es nimmt jetzt `min(Höhe, verfügbare Breite)`.
- `DrylCanvas` — Der Canvas-Body ist ein benannter Container-Kontext (`canvas`): die Nodes richten
  sich nach der Breite des Canvas, nicht nach der des Viewports — dieselbe Spec liegt einmal in
  einer schmalen Chat-Spalte und einen Morph später im Vollbild.
```

Fand Task 4 weitere Brüche, kommt pro Fund eine Zeile dazu — Komponentenname in Backticks, was
brach und was jetzt gilt. Keine Zeile ohne Fund.

- [ ] **Step 3: Correct the roadmap**

In `docs/superpowers/specs/2026-07-25-canvas-platform-roadmap.md`:
- Zeile 286: `**Agents PATCH · unabhängig, jederzeit**` → `**Kern PATCH · unabhängig, jederzeit**`
  und einen Halbsatz anhängen: „(nach dem A1-Umzug liegen beide Bausteine im Kern)".
- Die Tabellenzeile `| R Responsive | — | PATCH, wann immer |` → `| R Responsive | 2.17.0 → **2.17.1** | — |`.

- [ ] **Step 4: Run the full suite and the token sync**

```bash
dotnet test tests/DRYL.Components.Tests
node scripts/check-light-sync.mjs
```
Expected: Tests grün; check-light-sync grün (es kamen keine Tokens dazu, der Lauf ist die
Bestätigung, nicht die Hoffnung).

- [ ] **Step 5: Update the project note**

In `memory/project-ai-canvas.md` eine Zeile ergänzen: Sidequest R erledigt, Kern 2.17.1, was
gemessen wurde (Donut skaliert, Canvas-Body ist Container `canvas`) und — falls zutreffend — was
der Durchlauf zusätzlich fand.

- [ ] **Step 6: Commit**

```bash
git add CHANGELOG.md DRYL.Components/DRYL.Components.csproj \
        docs/superpowers/specs/2026-07-25-canvas-platform-roadmap.md
git commit -m "chore(release): DRYL.Components 2.17.1 — canvas responsive hardening"
```

---

### Task 6: Die Messfläche wieder abbauen

**Files:**
- Delete: `../DRYL.Website/Components/Pages/DevCanvasResponsive.razor`

- [ ] **Step 1: Delete the fixture**

```bash
cd ../DRYL.Website && git rm Components/Pages/DevCanvasResponsive.razor
```

- [ ] **Step 2: Verify the site still builds**

Run: `cd ../DRYL.Website && dotnet build`
Expected: Build erfolgreich, keine dangling Route.

- [ ] **Step 3: Commit**

```bash
cd ../DRYL.Website && git commit -m "chore(dev): drop the canvas responsive fixture"
```
