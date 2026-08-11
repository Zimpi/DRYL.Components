# DrylAiCanvas Phase Q — Robustheit & Qualität — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Six robustness fixes for the AI canvas and chart family: `{value}`-template value formats, error placeholders instead of endless skeletons, duplicate-id feedback, cancel settling, per-artifact form reset, and final prop sync for streaming shells.

**Architecture:** Q6 fixes `DrylChartBase.FormatValue` (template substitution) + `DrylDonutChart` (plain percent) + `CanvasPrompt` (docs). Q1 distinguishes streaming vs settled in `CanvasNodeView`. Q2 adds a duplicate-id check to the create receipt walk. Q3 adds `DrylCanvasRun.CancelGeneration`. Q4 adds an `ArtifactEpoch` watched by the canvas to clear `CanvasFormState`. Q7 syncs shell/root props in `CanvasStreamReveal`'s complete path. Spec: `docs/superpowers/specs/2026-07-24-canvas-robustness-design.md`.

**Tech Stack:** C# / .NET (multi-target net8.0/9.0/10.0), Blazor, `System.Text.Json`, xUnit + bUnit (`tests/DRYL.Components.Tests`).

## Global Constraints

- No public API change: `CancelGeneration`, `ArtifactEpoch`, `CanvasFormState.Clear` are all `internal`. The only public-surface edits are behavior fixes inside existing members.
- bUnit 2.7.2 has NO `SetParametersAndRender` — use `cut.Render(ps => ...)` for re-renders.
- Tests run from the repo root: `dotnet test tests/DRYL.Components.Tests/DRYL.Components.Tests.csproj --filter "<Filter>"`.
- Culture: chart display values stay culture-aware (existing `"0.##"` behavior) — never switch display formatting to invariant. Numeric assertions in tests must use integer values (no decimal separator) or mirror the culture via `v.ToString(...)` in the expected value.
- CSS: tokens only (`--line-strong`, `--fg-dim`, `--r-md`, `--sp-*`) — no literal colors/radii/spacing. `font-size: 13px` matches the existing `.canvas-waiting` literal and is allowed.
- Commit style: conventional, lowercase (`fix:`, `feat:`, `test:`), with `Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>` trailer.
- Versioning (CLAUDE.md §7.0/§7.1): version bumps + changelog release cut happen ONLY in Task 7.

---

### Task 1: Q6 Core — `FormatValue` `{value}` templates + donut percent fix

**Files:**
- Modify: `DRYL.Components/Components/Data/Charts/DrylChartBase.cs` (`FormatValue` ~line 66-68, `ValueFormat` XML doc ~line 23-27)
- Modify: `DRYL.Components/Components/Data/Charts/DrylDonutChart.razor` (pct usage ~lines 41-43, 52, 61)
- Test: `tests/DRYL.Components.Tests/ChartFormatValueTests.cs` (new)
- Test: `tests/DRYL.Components.Tests/DrylDonutChartTests.cs` (append)

**Interfaces:**
- Consumes: nothing from earlier tasks.
- Produces: `DrylChartBase.FormatValue` template semantics (used by all four chart components); no signature changes.

- [ ] **Step 1: Write the failing tests**

Create `tests/DRYL.Components.Tests/ChartFormatValueTests.cs`:

```csharp
using DRYL.Components;
using Xunit;

namespace DRYL.Components.Tests;

/// <summary>
/// <c>FormatValue</c> template support: <c>{value}</c> placeholders (what AI models
/// naturally emit), optional inner .NET format, back-compat with plain .NET format
/// strings, malformed-template fallback. Assertions mirror the culture via
/// <c>v.ToString(...)</c> instead of hardcoding separators.
/// </summary>
public class ChartFormatValueTests
{
    private sealed class TestChart : DrylChartBase
    {
        public string Format(double v) => FormatValue(v);
    }

    [Fact]
    public void Null_format_uses_default() =>
        Assert.Equal(80.ToString("0.##"), new TestChart().Format(80));

    [Fact]
    public void Dotnet_format_string_still_works() =>
        Assert.Equal(80.ToString("N0"), new TestChart { ValueFormat = "N0" }.Format(80));

    [Fact]
    public void Value_template_substitutes_the_number()
    {
        var chart = new TestChart { ValueFormat = "€{value} Tsd" };
        Assert.Equal($"€{80.ToString("0.##")} Tsd", chart.Format(80));
    }

    [Fact]
    public void Percent_template_substitutes_without_duplication() =>
        Assert.Equal("17%", new TestChart { ValueFormat = "{value}%" }.Format(17));

    [Fact]
    public void Inner_format_controls_the_number()
    {
        var chart = new TestChart { ValueFormat = "{value:0.0}%" };
        Assert.Equal(17.5.ToString("0.0") + "%", chart.Format(17.5));
    }

    [Fact]
    public void Malformed_template_falls_back_to_dotnet_formatting()
    {
        // "{valueX}" is not our placeholder — treated as a plain .NET format string.
        var chart = new TestChart { ValueFormat = "{valueX}" };
        Assert.Equal(80.ToString("{valueX}"), chart.Format(80));
    }
}
```

Append to `tests/DRYL.Components.Tests/DrylDonutChartTests.cs`:

```csharp
    [Fact]
    public void Percent_value_format_does_not_duplicate_percent_signs()
    {
        var cut = Render<DrylDonutChart>(ps => ps
            .Add(p => p.Segments, new[] { new ChartSegment("A", 80), new ChartSegment("B", 20) })
            .Add(p => p.ValueFormat, "{value}%"));

        var label = cut.Find(".donut-slice path").GetAttribute("aria-label");
        Assert.Equal("A: 80% (80 %)", label);
    }
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests/DRYL.Components.Tests/DRYL.Components.Tests.csproj --filter "FullyQualifiedName~ChartFormatValueTests|FullyQualifiedName~DrylDonutChartTests"`
Expected: FAIL — `Value_template_substitutes_the_number` produces `€{value} Tsd`-garbage (the reported bug); the donut test shows duplicated/misplaced `%`.

- [ ] **Step 3: Implement `FormatValue` template support**

In `DRYL.Components/Components/Data/Charts/DrylChartBase.cs`, replace the `ValueFormat` XML doc:

```csharp
    /// <summary>
    /// Display format for axis ticks and tooltip values: either a .NET format string
    /// (e.g. "N0", "C0") or a template with a <c>{value}</c> placeholder where the number
    /// goes (e.g. "€{value} Tsd", "{value}%"), optionally with an inner .NET format
    /// ("{value:0.0}"). Display values are intentionally culture-aware.
    /// </summary>
    [Parameter] public string? ValueFormat { get; set; }
```

Replace `FormatValue`:

```csharp
    /// <summary>Culture-aware display value for ticks and tooltips — see <see cref="ValueFormat"/>.</summary>
    protected string FormatValue(double v)
    {
        if (ValueFormat is null) return v.ToString("0.##");

        var i = ValueFormat.IndexOf("{value", StringComparison.Ordinal);
        if (i < 0) return v.ToString(ValueFormat);

        var rest = ValueFormat.AsSpan(i + 6);
        var end = rest.IndexOf('}');
        if (end < 0) return v.ToString(ValueFormat);            // no closing brace — not a template

        var inner = "0.##";
        if (end > 0)
        {
            if (rest[0] != ':') return v.ToString(ValueFormat); // "{valueX}" — not our placeholder
            inner = rest.Slice(1, end - 1).ToString();
        }

        return string.Concat(
            ValueFormat.AsSpan(0, i),
            v.ToString(inner),
            ValueFormat.AsSpan(i + 6 + end + 1));
    }
```

- [ ] **Step 4: Fix the donut percent duplication**

In `DRYL.Components/Components/Data/Charts/DrylDonutChart.razor`, inside the `@{ }` block above the slice markup (after `var pct = seg.Value / total * 100;`), add:

```csharp
                var pctText = Math.Round(pct).ToString("0.##");
```

Change the path aria-label (the percent part uses the plain text, never `ValueFormat`):

```razor
                              aria-label="@($"{seg.Label}: {FormatValue(seg.Value)} ({pctText} %)")"
```

Change the tooltip row:

```razor
                            <span class="chart-tip-value">@FormatValue(seg.Value) · @pctText %</span>
```

- [ ] **Step 5: Run tests to verify they pass**

Run: `dotnet test tests/DRYL.Components.Tests/DRYL.Components.Tests.csproj --filter "FullyQualifiedName~Chart"`
Expected: all pass — the 6 new format tests, the new donut test, and every pre-existing chart test.

- [ ] **Step 6: Commit**

```bash
git add DRYL.Components/Components/Data/Charts/DrylChartBase.cs DRYL.Components/Components/Data/Charts/DrylDonutChart.razor tests/DRYL.Components.Tests/ChartFormatValueTests.cs tests/DRYL.Components.Tests/DrylDonutChartTests.cs
git commit -m "fix: support {value} templates in chart ValueFormat, dedupe donut percent

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 2: Q2 + Q6-prompt — duplicate-id receipt + `valueFormat` prompt docs

**Files:**
- Modify: `DRYL.Components.Agents/Canvas/DrylCanvasTools.cs` (create-final walk, ~line 95-102)
- Modify: `DRYL.Components.Agents/Canvas/CanvasPrompt.cs` (the two chart lines in `SchemaText`)
- Test: `tests/DRYL.Components.Tests/Agents/Canvas/DrylCanvasToolsCreateTests.cs` (append)
- Test: `tests/DRYL.Components.Tests/Agents/Canvas/CanvasPromptTests.cs` (new)

**Interfaces:**
- Consumes: `DrylCanvasTools.CreateReplay` + `Script`/`InvokeAsync` helpers (existing test file).
- Produces: duplicate-id feedback in the create receipt; `valueFormat` documented in `SchemaText`.

- [ ] **Step 1: Write the failing tests**

Append to `DrylCanvasToolsCreateTests`:

```csharp
    [Fact]
    public async Task Create_reports_duplicate_ids_in_receipt()
    {
        var run = new DrylCanvasRun();
        var full = """
            {"title":"T","root":{"id":"root","type":"stack","children":[
            {"id":"a","type":"stat","props":{"label":"L","value":"1"}},
            {"id":"a","type":"divider"}]}}
            """;

        var tools = DrylCanvasTools.CreateReplay(run, (_, _) => Script(full));

        var receipt = await InvokeAsync(tools.CreateArtifact, "dup ids");

        Assert.Equal(AiState.Generated, run.State);
        Assert.Contains("duplicate id 'a'", receipt);
    }
```

Create `tests/DRYL.Components.Tests/Agents/Canvas/CanvasPromptTests.cs`:

```csharp
using DRYL.Components.Agents;
using Xunit;

namespace DRYL.Components.Tests.Agents.Canvas;

public class CanvasPromptTests
{
    [Fact]
    public void SchemaText_documents_the_value_template()
    {
        Assert.Contains("{value}", CanvasPrompt.SchemaText);
        Assert.Contains("display template", CanvasPrompt.SchemaText);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests/DRYL.Components.Tests/DRYL.Components.Tests.csproj --filter "FullyQualifiedName~DrylCanvasToolsCreateTests|FullyQualifiedName~CanvasPromptTests"`
Expected: FAIL — receipt lacks "duplicate id"; SchemaText lacks "{value}".

- [ ] **Step 3: Add the duplicate-id check to the create walk**

In `DRYL.Components.Agents/Canvas/DrylCanvasTools.cs`, replace the `problems` block in `CreateArtifactImpl`:

```csharp
            var problems = new List<string>();
            var seenIds = new HashSet<string>(StringComparer.Ordinal);
            int nodes = 0, interactive = 0;
            Walk(final.Root, n =>
            {
                nodes++;
                if (CanvasCatalog.IsInteractive(n.Type)) interactive++;
                if (!seenIds.Add(n.Id))
                    problems.Add($"duplicate id '{n.Id}' — ids must be unique across the artifact.");
                if (CanvasCatalog.Validate(n) is { } e) problems.Add(e);
            });
```

- [ ] **Step 4: Document `valueFormat` in the prompt**

In `DRYL.Components.Agents/Canvas/CanvasPrompt.cs`, replace the two chart lines inside `SchemaText`:

```
        - lineChart|areaChart|barChart { "title": string?, "labels": string[], "series": [{ "name": string, "data": number[] }], "valueFormat": string? } — one value per label. "valueFormat" is a display template: put {value} where the number goes, e.g. "€{value} Tsd" or "{value}%".
        - donutChart { "title": string?, "segments": [{ "label": string, "value": number }], "valueFormat": string? } — max 6 segments. Same {value} display template for "valueFormat".
```

- [ ] **Step 5: Run tests to verify they pass**

Run: `dotnet test tests/DRYL.Components.Tests/DRYL.Components.Tests.csproj --filter "FullyQualifiedName~DrylCanvasToolsCreateTests|FullyQualifiedName~CanvasPromptTests"`
Expected: all pass.

- [ ] **Step 6: Commit**

```bash
git add DRYL.Components.Agents/Canvas/DrylCanvasTools.cs DRYL.Components.Agents/Canvas/CanvasPrompt.cs tests/DRYL.Components.Tests/Agents/Canvas/DrylCanvasToolsCreateTests.cs tests/DRYL.Components.Tests/Agents/Canvas/CanvasPromptTests.cs
git commit -m "fix: report duplicate canvas ids to the model, document {value} template

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 3: Q1 — error placeholder once the run settles

**Files:**
- Modify: `DRYL.Components.Agents/Canvas/CanvasNodeView.razor` (invalid branch, ~lines 32-37)
- Modify: `DRYL.Components.Agents/Canvas/DrylAiCanvas.razor.css` (add `.canvas-invalid` next to `.canvas-waiting`)
- Test: `tests/DRYL.Components.Tests/Agents/Canvas/DrylAiCanvasTests.cs` (modify existing skeleton test + append)

**Interfaces:**
- Consumes: `Run.State` (cascaded `DrylCanvasRun` — property read at render time, no cascade change).
- Produces: `.canvas-invalid` placeholder element + class.

- [ ] **Step 1: Adjust the existing skeleton test to the streaming path, add the settled test**

In `DrylAiCanvasTests.cs`, REPLACE the existing `Invalid_node_renders_skeleton_fallback` test (it used `ApplySnapshot`, whose state is `None` — that now means "settled"):

```csharp
    [Fact]
    public void Invalid_node_renders_skeleton_fallback_while_streaming()
    {
        var run = new DrylCanvasRun();
        run.BeginCreate();
        // An invalid node in the complete zone (a later sibling follows) mid-stream:
        // still shown as the "waiting" skeleton.
        run.RevealSnapshot(Parse("""
            {"root":{"id":"root","type":"stack","props":{},"children":[
                {"id":"bad","type":"stat","props":{"label":"only a label"}},
                {"id":"ok","type":"divider"}]}}
            """));

        var cut = Render<DrylAiCanvas>(p => p.Add(x => x.Run, run));

        Assert.Contains("waiting for stat", cut.Markup);
        Assert.NotEmpty(cut.FindAll(".skel-wrap"));
        Assert.Empty(cut.FindAll(".canvas-invalid"));
    }

    [Fact]
    public void Invalid_node_shows_error_placeholder_once_settled()
    {
        var run = new DrylCanvasRun();
        // ApplySnapshot leaves the run at AiState.None (no live generation) —
        // an invalid node is finished-broken, not streaming.
        run.ApplySnapshot(Parse("""
            {"root":{"id":"root","type":"stack","children":[
                {"id":"bad","type":"stat","props":{"label":"only a label"}}]}}
            """));

        var cut = Render<DrylAiCanvas>(p => p.Add(x => x.Run, run));

        Assert.Empty(cut.FindAll(".canvas-waiting"));
        var placeholder = cut.Find(".canvas-invalid");
        Assert.Contains("value must be non-empty", placeholder.TextContent);
    }
```

- [ ] **Step 2: Run tests to verify the new one fails**

Run: `dotnet test tests/DRYL.Components.Tests/DRYL.Components.Tests.csproj --filter "FullyQualifiedName~DrylAiCanvasTests"`
Expected: `Invalid_node_shows_error_placeholder_once_settled` FAILS (no `.canvas-invalid` element exists yet); the streaming variant passes.

- [ ] **Step 3: Implement the settled-error branch**

In `DRYL.Components.Agents/Canvas/CanvasNodeView.razor`, replace the invalid branch:

```razor
        @if (error is not null)
        {
            @if (Run.State is AiState.Streaming or AiState.Thinking)
            {
                <DrylSkeleton Variant="DrylSkeleton.SkeletonVariant.Card" Ai="AiState.Streaming" />
                <span class="canvas-waiting">@($"waiting for {Node.Type}…")</span>
            }
            else
            {
                @* Settled and still invalid — finished-broken, not loading. *@
                <span class="canvas-invalid">@error</span>
            }
        }
```

In `DRYL.Components.Agents/Canvas/DrylAiCanvas.razor.css`, append (next to `.canvas-waiting`):

```css
/* Settled-but-invalid node: a quiet, honest placeholder instead of an endless
   "waiting…" skeleton. The text is the catalog's corrective message. */
::deep .canvas-invalid {
    display: block;
    padding: var(--sp-3);
    border: 1px dashed var(--line-strong);
    border-radius: var(--r-md);
    color: var(--fg-dim);
    font-size: 13px;
}
```

- [ ] **Step 4: Run the full canvas suite**

Run: `dotnet test tests/DRYL.Components.Tests/DRYL.Components.Tests.csproj --filter "FullyQualifiedName~Canvas"`
Expected: all pass.

- [ ] **Step 5: Commit**

```bash
git add DRYL.Components.Agents/Canvas/CanvasNodeView.razor DRYL.Components.Agents/Canvas/DrylAiCanvas.razor.css tests/DRYL.Components.Tests/Agents/Canvas/DrylAiCanvasTests.cs
git commit -m "fix: show error placeholder for invalid canvas nodes once settled

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 4: Q3 — `CancelGeneration` settles the run

**Files:**
- Modify: `DRYL.Components.Agents/Canvas/DrylCanvasRun.cs` (add method after `FailGeneration`, ~line 141)
- Modify: `DRYL.Components.Agents/Canvas/DrylCanvasTools.cs` (both OCE catches, ~line 109 and ~line 167)
- Test: `tests/DRYL.Components.Tests/Agents/Canvas/DrylCanvasToolsCreateTests.cs` (extend cancel test)
- Test: `tests/DRYL.Components.Tests/Agents/Canvas/DrylCanvasToolsUpdateTests.cs` (extend cancel test if present)

**Interfaces:**
- Consumes: nothing.
- Produces: `DrylCanvasRun.CancelGeneration()` — internal; contract: State → `AiState.None`, `Error` untouched, `Raise()`.

- [ ] **Step 1: Strengthen the cancel tests**

In `DrylCanvasToolsCreateTests.cs`, in `Create_rethrows_cancellation_instead_of_returning_a_failure_receipt`, extend the final assertions:

```csharp
        Assert.Null(run.Error);
        Assert.Equal(AiState.None, run.State);   // settled, not stuck "Building"
```

In `DrylCanvasToolsUpdateTests.cs`, find the cancellation test (search for `OperationCanceledException`); if one exists, add the same `Assert.Equal(AiState.None, run.State);`. If none exists, append a mirror test:

```csharp
    [Fact]
    public async Task Update_rethrows_cancellation_and_settles_the_run()
    {
        var run = new DrylCanvasRun();
        run.ApplySnapshot(System.Text.Json.JsonSerializer.Deserialize<CanvasSpec>(
            """{"root":{"id":"root","type":"stack","children":[]}}""", CanvasJson.Options)!);

        static async IAsyncEnumerable<string> Cancelling()
        {
            await Task.Yield();
            throw new OperationCanceledException();
#pragma warning disable CS0162
            yield break;
#pragma warning restore CS0162
        }

        var tools = DrylCanvasTools.CreateReplay(run, (_, _) => Cancelling());
        var fn = (AIFunction)tools.UpdateArtifact;
        var args = new Dictionary<string, object?> { ["brief"] = "anything" };

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => fn.InvokeAsync(new AIFunctionArguments(args!)).AsTask());

        Assert.Null(run.Error);
        Assert.Equal(AiState.None, run.State);
    }
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests/DRYL.Components.Tests/DRYL.Components.Tests.csproj --filter "FullyQualifiedName~DrylCanvasTools"`
Expected: FAIL — `run.State` is still `Streaming` after cancellation.

- [ ] **Step 3: Implement `CancelGeneration` + wire both catches**

In `DRYL.Components.Agents/Canvas/DrylCanvasRun.cs`, after `FailGeneration`:

```csharp
    /// <summary>
    /// Settles a cancelled generation: state returns to <see cref="AiState.None"/> without an
    /// error — the artifact stays as it was and a later generation may continue.
    /// </summary>
    internal void CancelGeneration()
    {
        State = AiState.None;
        Raise();
    }
```

In `DRYL.Components.Agents/Canvas/DrylCanvasTools.cs`, replace BOTH `catch (OperationCanceledException) { throw; }` lines:

```csharp
        catch (OperationCanceledException) { _run.CancelGeneration(); throw; }
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test tests/DRYL.Components.Tests/DRYL.Components.Tests.csproj --filter "FullyQualifiedName~DrylCanvasTools"`
Expected: all pass.

- [ ] **Step 5: Commit**

```bash
git add DRYL.Components.Agents/Canvas/DrylCanvasRun.cs DRYL.Components.Agents/Canvas/DrylCanvasTools.cs tests/DRYL.Components.Tests/Agents/Canvas/DrylCanvasToolsCreateTests.cs tests/DRYL.Components.Tests/Agents/Canvas/DrylCanvasToolsUpdateTests.cs
git commit -m "fix: settle canvas run to idle when a generation is cancelled

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 5: Q4 — `ArtifactEpoch` form reset + seed identity fix

**Files:**
- Modify: `DRYL.Components.Agents/Canvas/DrylCanvasRun.cs` (`ArtifactEpoch` + bump in `BeginCreate`)
- Modify: `DRYL.Components.Agents/Canvas/CanvasFormState.cs` (add `Clear`)
- Modify: `DRYL.Components.Agents/Canvas/DrylAiCanvas.razor` (epoch watch)
- Modify: `DRYL.Components.Agents/Canvas/CanvasNodeView.razor` (`_seeded` bool → `_seededNode` reference)
- Test: `tests/DRYL.Components.Tests/Agents/Canvas/DrylCanvasRunTests.cs` (append epoch test)
- Test: `tests/DRYL.Components.Tests/Agents/Canvas/DrylAiCanvasTests.cs` (append form-reset test)

**Interfaces:**
- Consumes: the identity-check precedent from the memo fix (`ReferenceEquals` in `EnsureMemo`).
- Produces: `DrylCanvasRun.ArtifactEpoch` (internal int, bumped by `BeginCreate`); `CanvasFormState.Clear()` (internal).

- [ ] **Step 1: Write the failing tests**

Append to `DrylCanvasRunTests.cs`:

```csharp
    [Fact]
    public void BeginCreate_bumps_the_artifact_epoch()
    {
        var run = new DrylCanvasRun();
        var e0 = run.ArtifactEpoch;

        run.BeginCreate();

        Assert.Equal(e0 + 1, run.ArtifactEpoch);
    }
```

Append to `DrylAiCanvasTests.cs`:

```csharp
    [Fact]
    public void Fresh_create_resets_form_state_and_reseeds_values()
    {
        var run = new DrylCanvasRun();
        run.ApplySnapshot(Parse("""
            {"root":{"id":"root","type":"stack","children":[
                {"id":"i1","type":"inputText","props":{"name":"region","label":"Region","value":"DE"}}]}}
            """));
        var cut = Render<DrylAiCanvas>(p => p.Add(x => x.Run, run));
        Assert.Equal("DE", cut.Find("input").GetAttribute("value"));

        cut.Find("input").Change("FR");   // user overwrites the seeded value

        // Second artifact: SAME node id and field name, different AI-provided value.
        cut.InvokeAsync(() => run.BeginCreate());
        cut.InvokeAsync(() => run.RevealSnapshot(Parse("""
            {"root":{"id":"root","type":"stack","props":{},"children":[
                {"id":"i1","type":"inputText","props":{"name":"region","label":"Region","value":"IT"}},
                {"id":"d1","type":"divider"}]}}
            """)));

        cut.WaitForAssertion(() => Assert.Equal("IT", cut.Find("input").GetAttribute("value")));
    }
```

Note: this single test pins BOTH fixes — without `Clear()` the old "FR" blocks seeding; without the seed identity fix the reused view's `_seeded` bool suppresses reseeding entirely.

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests/DRYL.Components.Tests/DRYL.Components.Tests.csproj --filter "FullyQualifiedName~DrylCanvasRunTests.BeginCreate_bumps|FullyQualifiedName~DrylAiCanvasTests.Fresh_create"`
Expected: BUILD FAILURE (`ArtifactEpoch` missing) for the first; the form test would fail at runtime (input stays "FR") — the build failure masks it for now, verified after implementation by stashing if desired. Proceed to implementation; both must pass at the end.

- [ ] **Step 3: Implement epoch + Clear + watch + seed identity**

`DRYL.Components.Agents/Canvas/DrylCanvasRun.cs` — add the field/property and bump in `BeginCreate`:

```csharp
    private int _artifactEpoch;

    /// <summary>Monotonic counter bumped by every <see cref="BeginCreate"/> — the canvas watches
    /// it to reset interactive form state for a fresh artifact.</summary>
    internal int ArtifactEpoch => _artifactEpoch;
```

```csharp
    internal void BeginCreate()
    {
        _artifactEpoch++;
        _revealStarted = false;
        BeginGeneration();
    }
```

`DRYL.Components.Agents/Canvas/CanvasFormState.cs` — add:

```csharp
    /// <summary>Removes all values and fires <see cref="OnChanged"/>. Internal — the canvas
    /// calls this when a fresh artifact begins (see DrylCanvasRun.ArtifactEpoch).</summary>
    internal void Clear()
    {
        _values.Clear();
        OnChanged?.Invoke();
    }
```

`DRYL.Components.Agents/Canvas/DrylAiCanvas.razor` — add the field and the watch. Field (next to `_form`):

```csharp
    private int _epoch;
```

Private method + calls at the top of `OnParametersSet` (after the subscription block) and inside the `HandleChange` lambda (first statement):

```csharp
    // A fresh create means a fresh form — wipe user input from the previous artifact so
    // recycled field names show the new AI-provided values. One CanvasFormState instance
    // is kept (the cascade is IsFixed); Clear() empties it in place.
    private void SyncEpoch()
    {
        if (Run is not null && Run.ArtifactEpoch != _epoch)
        {
            _epoch = Run.ArtifactEpoch;
            _form.Clear();
        }
    }
```

In `OnParametersSet`, after the `_subscribed` block:

```csharp
        SyncEpoch();
```

In `HandleChange`, first statement inside the `InvokeAsync` lambda:

```csharp
    private void HandleChange() => InvokeAsync(() =>
    {
        SyncEpoch();
        // ... existing body unchanged
    });
```

`DRYL.Components.Agents/Canvas/CanvasNodeView.razor` — replace `_seeded` with `_seededNode`:

```csharp
    private CanvasNode? _seededNode;

    protected override void OnParametersSet() => SeedFormOnce();

    // Seed once per node INSTANCE: Blazor reuses this view across whole-tree
    // replacements (same trap as the memo identity check), so a bool flag would
    // suppress seeding for a new artifact's node with a recycled id.
    private void SeedFormOnce()
    {
        if (ReferenceEquals(_seededNode, Node)) return;
        _seededNode = Node;
        if (!CanvasCatalog.IsInteractive(Node.Type)) return;
        // ... existing switch unchanged
    }
```

- [ ] **Step 4: Run the full canvas suite**

Run: `dotnet test tests/DRYL.Components.Tests/DRYL.Components.Tests.csproj --filter "FullyQualifiedName~Canvas"`
Expected: all pass — including the two new tests.

- [ ] **Step 5: Commit**

```bash
git add DRYL.Components.Agents/Canvas/DrylCanvasRun.cs DRYL.Components.Agents/Canvas/CanvasFormState.cs DRYL.Components.Agents/Canvas/DrylAiCanvas.razor DRYL.Components.Agents/Canvas/CanvasNodeView.razor tests/DRYL.Components.Tests/Agents/Canvas/DrylCanvasRunTests.cs tests/DRYL.Components.Tests/Agents/Canvas/DrylAiCanvasTests.cs
git commit -m "fix: reset canvas form state when a fresh artifact begins

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 6: Q7 — final prop sync for shells and root

**Files:**
- Modify: `DRYL.Components.Agents/Canvas/CanvasStreamReveal.cs` (`Reveal` root-props condition ~line 48; `RevealChildren` complete-branch ~line 81-86)
- Test: `tests/DRYL.Components.Tests/Agents/Canvas/CanvasStreamRevealTests.cs` (append)

**Interfaces:**
- Consumes: `CanvasNode.Version` (Phase P).
- Produces: shell nodes' own props final-sync when they enter the complete zone; root props sync on the done flush too.

- [ ] **Step 1: Write the failing tests**

Append to `CanvasStreamRevealTests.cs` (uses the file's existing `Spec(string)` / `Child(run, id)` helpers and the `DrylCanvasRun.BeginCreate`/`RevealSnapshot`/`CompleteReveal` driving convention):

```csharp
    [Fact]
    public void Complete_zone_syncs_props_of_a_former_shell()
    {
        var run = new DrylCanvasRun();
        run.BeginCreate();

        // c1 is the streaming tail container -> revealed as a shell with partial props.
        run.RevealSnapshot(Spec("""
            {"root":{"id":"root","type":"stack","props":{},"children":[
                {"id":"d1","type":"divider"},
                {"id":"c1","type":"card","props":{"title":"A"},"children":[]}]}}
            """));

        // A later sibling starts -> c1 becomes complete; its props finished growing meanwhile.
        run.RevealSnapshot(Spec("""
            {"root":{"id":"root","type":"stack","props":{},"children":[
                {"id":"d1","type":"divider"},
                {"id":"c1","type":"card","props":{"title":"AB"},"children":[
                    {"id":"x","type":"divider"}]},
                {"id":"d2","type":"divider"}]}}
            """));

        var c1 = Child(run, "c1")!;
        Assert.Equal("AB", c1.Props!.Value.GetProperty("title").GetString());
    }

    [Fact]
    public void Done_flush_syncs_root_props()
    {
        var run = new DrylCanvasRun();
        run.BeginCreate();
        run.RevealSnapshot(Spec("""
            {"root":{"id":"root","type":"stack","props":{"gap":"sm"},"children":[
                {"id":"d1","type":"divider"},
                {"id":"d2","type":"divider"}]}}
            """));

        run.CompleteReveal(Spec("""
            {"root":{"id":"root","type":"stack","props":{"gap":"lg"},"children":[
                {"id":"d1","type":"divider"},
                {"id":"d2","type":"divider"}]}}
            """));

        Assert.Equal("lg", run.Spec!.Root!.Props!.Value.GetProperty("gap").GetString());
    }
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests/DRYL.Components.Tests/DRYL.Components.Tests.csproj --filter "FullyQualifiedName~CanvasStreamRevealTests"`
Expected: 2 FAIL — c1 keeps title "A"; root keeps gap "sm".

- [ ] **Step 3: Implement the sync**

In `DRYL.Components.Agents/Canvas/CanvasStreamReveal.cs`:

Site 1 — `Reveal`: drop the `!streamDone` gate on the root-props sync:

```csharp
        else if (PropsDiffer(live.Root.Props, snapRoot.Props))
        {
            live.Root.Props = snapRoot.Props;
            live.Root.Version++;
            changed = true;
        }
```

Site 2 — `RevealChildren`, complete-branch (`existing is not null`): sync a former shell's own props before recursing:

```csharp
            else
            {
                // A container we had shown as a still-filling shell has now completed:
                // its own props may have finished growing since the shell was seeded —
                // sync them (a reference-frozen node is the same instance as s, so
                // PropsDiffer is false there and this is a no-op).
                if (PropsDiffer(existing.Props, s.Props))
                {
                    existing.Props = s.Props;
                    existing.Version++;
                    changed = true;
                }
                // …then flush any child it was still withholding; sealed thereafter.
                changed |= RevealChildren(existing, s, streamDone: true);
            }
```

- [ ] **Step 4: Run the full canvas suite**

Run: `dotnet test tests/DRYL.Components.Tests/DRYL.Components.Tests.csproj --filter "FullyQualifiedName~Canvas"`
Expected: all pass.

- [ ] **Step 5: Commit**

```bash
git add DRYL.Components.Agents/Canvas/CanvasStreamReveal.cs tests/DRYL.Components.Tests/Agents/Canvas/CanvasStreamRevealTests.cs
git commit -m "fix: final-sync props of streaming shells and root on completion

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 7: Version bumps + CHANGELOG release cut + full verification

**Files:**
- Modify: `DRYL.Components/DRYL.Components.csproj` (`<Version>2.10.1</Version>` → `2.10.2`)
- Modify: `DRYL.Components.Agents/DRYL.Components.Agents.csproj` (`<Version>0.8.1</Version>` → `0.8.2`)
- Modify: `CHANGELOG.md`

**Interfaces:**
- Consumes: all previous tasks.
- Produces: publishable releases `v2.10.2` and `agents-v0.8.2` (tagged by the publish workflow on push to main).

- [ ] **Step 1: Bump both versions**

`DRYL.Components/DRYL.Components.csproj`: `<Version>2.10.2</Version>`
`DRYL.Components.Agents/DRYL.Components.Agents.csproj`: `<Version>0.8.2</Version>`

- [ ] **Step 2: Update CHANGELOG.md**

Under `## [Unreleased]`, add:

```markdown
### Changed
- Canvas prompt — (Agents 0.8.2) `valueFormat` is now documented as a `{value}` display template with examples, and duplicate node ids in a generated artifact are reported back to the model in the create receipt so it can repair them via `update_artifact`.

### Fixed
- `DrylLineChart` / `DrylBarChart` / `DrylAreaChart` / `DrylDonutChart` — `ValueFormat` now supports `{value}` templates (e.g. `"€{value} Tsd"`, `"{value}%"`, optional inner .NET format `"{value:0.0}"`) in addition to plain .NET format strings; AI-authored templates no longer render the literal `{value}` placeholder into axes and tooltips.
- `DrylDonutChart` — The tooltip/aria-label percentage is always plain-formatted, so a percent-style `ValueFormat` no longer produces duplicated `%` signs.
- `DrylAiCanvas` — (Agents 0.8.2) Nodes that stay invalid after a generation settles now show a compact error placeholder with the catalog's corrective message instead of an endless "waiting…" skeleton (the skeleton remains while streaming).
- `DrylAiCanvas` — (Agents 0.8.2) Cancelling a generation mid-stream settles the run back to idle instead of leaving the canvas stuck in "Building".
- `DrylAiCanvas` — (Agents 0.8.2) A fresh `create_artifact` resets the interactive form state, so a recycled field name no longer shows the previous artifact's user input over the new AI-provided value.
- `DrylAiCanvas` — (Agents 0.8.2) A container revealed as a streaming shell (and the root) now has its props final-synced when the stream completes — e.g. a card title that finished late now lands.
```

Then cut the release: rename `## [Unreleased]` to `## [2.10.2] — 2026-07-24` and add a fresh empty `## [Unreleased]` above it.

- [ ] **Step 3: Full build + full test suite**

Run: `dotnet build DRYL.slnx`
Expected: 0 errors.

Run: `dotnet test tests/DRYL.Components.Tests/DRYL.Components.Tests.csproj`
Expected: all tests pass.

- [ ] **Step 4: Commit**

```bash
git add DRYL.Components/DRYL.Components.csproj DRYL.Components.Agents/DRYL.Components.Agents.csproj CHANGELOG.md
git commit -m "chore: release core 2.10.2 + agents 0.8.2 (canvas robustness)

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

## Self-Review Notes (already applied)

- Spec coverage: Q6 → Tasks 1+2; Q2 → Task 2; Q1 → Task 3; Q3 → Task 4; Q4 → Task 5; Q7 → Task 6; Q5 explicitly deferred to Phase W (spec §Q5). Versions/changelog → Task 7.
- The existing test `Invalid_node_renders_skeleton_fallback` breaks under Q1 semantics (`ApplySnapshot` = settled) — Task 3 Step 1 replaces it rather than letting it rot.
- The existing cancel tests pin Q3's contract (`Error` stays null) and gain the `State == None` assertion.
- `CanvasFormState.Clear` is `internal` — no public API addition, PATCH bump stays valid.
