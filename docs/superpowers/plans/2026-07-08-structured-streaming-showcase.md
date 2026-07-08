# Structured Streaming Showcase ("Living Recipe Card") Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Turn the "Structured streaming with `DrylAiGenerate<T>`" demo into a flagship showcase where a full recipe UI of real DRYL components assembles itself live from a streamed JSON token stream, with a raw-JSON x-ray panel as proof.

**Architecture:** Two small framework additions in `DRYL.Components` (a `Raw` buffer on `GenerationSnapshot<T>`, a `Clock` icon in `DrylIcon`), then a richer simulated `Recipe` JSON stream and a rewritten demo example in `DRYL.Website`. The website references both framework projects by `ProjectReference`, so changes flow immediately.

**Tech Stack:** Blazor (interactive server), bUnit tests, DRYL design tokens/motion primitives (`DrylPresence`, `.ai-aura*`), simulated token streams (no LLM).

**Repos:** `c:\Users\janzi\Desktop\DRYL\DRYL.Components` (framework) and `c:\Users\janzi\Desktop\DRYL\DRYL.Website` (demo site) — separate git repositories, both on `main`.

## Global Constraints

- Tokens, not literals: every color/spacing/radius/duration references a CSS variable (CLAUDE.md 2.1). Inline `style` only for layout one-offs.
- Motion only via `DrylPresence` / existing primitives; durations/easings from the fixed vocabulary (2.5, 2.12).
- AI vocabulary: only the shared `AiState` enum and `.ai-aura*` primitives (2.10).
- No new runtime dependencies.
- **Do not push to `main` of DRYL.Components** — pushing auto-publishes to NuGet (`publish.yml`). Commit locally; the maintainer decides the push.
- Framework changes require: CHANGELOG entry + version bump in the same commit (CLAUDE.md §7.0/§7.1). Core `DRYL.Components` 1.1.0 → **1.2.0** (new icon = MINOR, cut the changelog release in that commit); `DRYL.Components.Agents` 0.1.0 → **0.2.0** (new snapshot property = MINOR).
- German user communication; code and docs in English.

---

### Task 1: `GenerationSnapshot<T>.Raw` (framework, TDD)

**Files:**
- Modify: `DRYL.Components.Agents\Generation\GenerationSnapshot.cs`
- Modify: `DRYL.Components.Agents\Generation\DrylAiGenerate.razor`
- Modify: `DRYL.Components.Agents\DRYL.Components.Agents.csproj` (version 0.1.0 → 0.2.0, release notes)
- Modify: `CHANGELOG.md` (entry under `[Unreleased]`)
- Test: `tests\DRYL.Components.Tests\Agents\DrylAiGenerateTests.cs`

**Interfaces:**
- Produces: `GenerationSnapshot<T>.Raw` (`string`, never null, `""` before first token) — Task 4's demo binds `@snap.Raw`.

- [ ] **Step 1: Write the failing test**

Append to `DrylAiGenerateTests`:

```csharp
[Fact]
public void Exposes_raw_buffer_on_snapshot()
{
    var src = Stream(new[] { "{\"title\":\"Pan", "cakes\"}" });

    GenerationSnapshot<Recipe>? seen = null;
    Render<DrylAiGenerate<Recipe>>(p => p
        .Add(x => x.Source, src)
        .Add(x => x.ChildContent, (RenderFragment<GenerationSnapshot<Recipe>>)(snap =>
            builder => { seen = snap; })));

    // The snapshot instance is reused; at completion Raw holds the full accumulated buffer.
    this.WaitForAssertion(() => Assert.Equal("{\"title\":\"Pancakes\"}", seen!.Raw));
}
```

Note: `WaitForAssertion` is available on the rendered fragment — use the same pattern as the existing test (`cut.WaitForAssertion(...)`) by keeping the `var cut = Render<...>` form:

```csharp
var cut = Render<DrylAiGenerate<Recipe>>(...);
cut.WaitForAssertion(() => Assert.Equal("{\"title\":\"Pancakes\"}", seen!.Raw));
```

- [ ] **Step 2: Run test to verify it fails**

Run (from `c:\Users\janzi\Desktop\DRYL\DRYL.Components`):
`dotnet test tests/DRYL.Components.Tests --filter "FullyQualifiedName~Exposes_raw_buffer" -f net10.0`
Expected: FAIL — `GenerationSnapshot<Recipe>` contains no definition for `Raw` (compile error).

- [ ] **Step 3: Implement**

`GenerationSnapshot.cs` — add after `IsComplete`:

```csharp
/// <summary>The raw model output streamed so far (the accumulated JSON buffer, before repair/parsing).</summary>
public string Raw { get; internal set; } = "";
```

`DrylAiGenerate.razor` — `Apply` gains a `raw` parameter and every call site passes it explicitly:

```csharp
private async Task RunAsync()
{
    _cts?.Cancel();
    _cts?.Dispose();
    _cts = new CancellationTokenSource();
    var ct = _cts.Token;

    var reader = new PartialJsonReader<T>();
    Apply(AiState.Thinking, default, complete: false, raw: "");

    if (Source is null) { Apply(AiState.None, default, complete: false, raw: ""); return; }

    try
    {
        await foreach (var token in Source.WithCancellation(ct))
        {
            var value = reader.Append(token);
            Apply(AiState.Streaming, value, complete: false, raw: reader.Buffer);
        }
    }
    catch (OperationCanceledException) { return; }
    catch { Apply(AiState.None, _snapshot.Value, complete: false, raw: _snapshot.Raw); return; }

    Apply(AiState.Generated, reader.Current, complete: true, raw: reader.Buffer);

    try { await Task.Delay(SettleDelayMs, ct); }
    catch (OperationCanceledException) { return; }

    Apply(SettleTo, _snapshot.Value, complete: true, raw: _snapshot.Raw);
}

private void Apply(AiState state, T? value, bool complete, string raw)
{
    _snapshot.State = state;
    _snapshot.Value = value;
    _snapshot.IsComplete = complete;
    _snapshot.Raw = raw;

    if (_service is not null && Key is not null)
    {
        if (state == AiState.None) _service.Clear(Key);
        else _service.Set(Key, state);
    }
    _ = InvokeAsync(StateHasChanged);
}
```

Also update the `Raw`-related XML doc summary on the component comment if it mentions snapshot fields (it doesn't — skip).

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test tests/DRYL.Components.Tests --filter "FullyQualifiedName~DrylAiGenerate" -f net10.0`
Expected: PASS (both tests).

- [ ] **Step 5: Version + changelog (same commit)**

`DRYL.Components.Agents.csproj`: `<Version>0.2.0</Version>` and `<PackageReleaseNotes>Experimental 0.2.0. See CHANGELOG.md.</PackageReleaseNotes>`.

`CHANGELOG.md` — under `## [Unreleased]` add:

```markdown
### Added
- `GenerationSnapshot<T>.Raw` — (Agents) Every snapshot now carries the raw accumulated model output (the JSON buffer so far), so UIs can show the live token stream that drives the typed value
```

- [ ] **Step 6: Commit**

```bash
git add -A
git commit -m "feat(agents): expose raw model output on GenerationSnapshot<T>

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 2: `Clock` icon (framework)

**Files:**
- Modify: `DRYL.Components\Components\Data\DrylIcon.razor` (icon dictionary, after `["Calendar"]`)
- Modify: `DRYL.Components\DRYL.Components.csproj` (version 1.1.0 → 1.2.0)
- Modify: `CHANGELOG.md` (entry + cut release `[1.2.0] — 2026-07-08`)

**Interfaces:**
- Produces: `<DrylIcon Name="Clock" />` and `Icon="Clock"` on any icon-bearing component — Task 4 uses it on a `DrylBadge`.

- [ ] **Step 1: Add the icon**

In the `Paths` dictionary in `DrylIcon.razor`, directly after the `["Calendar"]` line, add (match the file's column alignment):

```csharp
["Clock"]        = """<circle cx="12" cy="12" r="10"/><polyline points="12 6 12 12 16 14"/>""",                                                                                                                                                                                             // lucide: clock
```

- [ ] **Step 2: Verify it renders**

Run: `dotnet build DRYL.Components/DRYL.Components.csproj -f net10.0`
Expected: Build succeeded.

- [ ] **Step 3: Version + changelog cut (same commit)**

`DRYL.Components.csproj`: `<Version>1.2.0</Version>`.

`CHANGELOG.md`: add to the same `[Unreleased] → Added` list:

```markdown
- `DrylIcon` — New `Clock` icon
```

Then cut the release per §7.1: rename `## [Unreleased]` (now holding the `Clock` + `GenerationSnapshot<T>.Raw` entries) to `## [1.2.0] — 2026-07-08` and start a fresh empty `## [Unreleased]` above it.

- [ ] **Step 4: Run the full test suite**

Run: `dotnet test -f net10.0` (solution root)
Expected: all tests PASS.

- [ ] **Step 5: Commit (do NOT push)**

```bash
git add -A
git commit -m "feat(icons): add Clock icon; release 1.2.0

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 3: Rich `Recipe` model + choreographed JSON stream + image asset (website)

**Files:**
- Modify: `c:\Users\janzi\Desktop\DRYL\DRYL.Website\Components\Examples\Agents\_Simulation\SimScenarios.cs`
- Create: `c:\Users\janzi\Desktop\DRYL\DRYL.Website\wwwroot\img\dish.jpg` (copy from `c:\Users\janzi\Desktop\DRYL\DRYL.Components\docs\screenshots\dish.jpg`)

**Interfaces:**
- Produces: `Recipe { Title, Description, Minutes, Serves, Difficulty, Calories, Tags, Ingredients, Steps, Nutrition }`, `Ingredient { Amount, Name }`, `RecipeStep { Title, Text }`, `Nutrition { ProteinG, CarbsG, FatG }` — all in namespace `DRYL.Website.Components.Examples.Agents.Simulation`; `SimScenarios.RecipeJson()` streams matching JSON. Task 4 consumes all of these.

- [ ] **Step 1: Copy the image**

```powershell
New-Item -ItemType Directory -Force "c:\Users\janzi\Desktop\DRYL\DRYL.Website\wwwroot\img"
Copy-Item "c:\Users\janzi\Desktop\DRYL\DRYL.Components\docs\screenshots\dish.jpg" "c:\Users\janzi\Desktop\DRYL\DRYL.Website\wwwroot\img\dish.jpg"
```

- [ ] **Step 2: Replace the `Recipe` class and `RecipeJson()`**

In `SimScenarios.cs`, replace the existing `Recipe` class (bottom of file) with:

```csharp
/// <summary>Structured-generation target for the DrylAiGenerate demo.</summary>
public sealed class Recipe
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public int Minutes { get; set; }
    public int Serves { get; set; }
    public int Difficulty { get; set; }
    public int Calories { get; set; }
    public List<string>? Tags { get; set; }
    public List<Ingredient>? Ingredients { get; set; }
    public List<RecipeStep>? Steps { get; set; }
    public Nutrition? Nutrition { get; set; }
}

/// <summary>One recipe ingredient (amount + name).</summary>
public sealed class Ingredient
{
    public string? Amount { get; set; }
    public string? Name { get; set; }
}

/// <summary>One cooking step (short title + instruction).</summary>
public sealed class RecipeStep
{
    public string? Title { get; set; }
    public string? Text { get; set; }
}

/// <summary>Macro nutrients per serving, in grams.</summary>
public sealed class Nutrition
{
    public int ProteinG { get; set; }
    public int CarbsG { get; set; }
    public int FatG { get; set; }
}
```

Replace `RecipeJson()` with (field order is deliberate choreography — title → description → meta → tags → ingredients → steps → nutrition last, so the donut is the finale):

```csharp
/// <summary>Streams JSON for a <see cref="Recipe"/> token by token (no model required).</summary>
public static async IAsyncEnumerable<string> RecipeJson([EnumeratorCancellation] CancellationToken ct = default)
{
    await Task.Delay(700, ct);
    const string json =
        "{\"title\":\"One-Pan Lemon Garlic Salmon\"," +
        "\"description\":\"Crisp-skinned salmon over charred asparagus and blistered cherry tomatoes, " +
        "finished with a bright lemon-garlic butter — one pan, twenty-five minutes.\"," +
        "\"minutes\":25," +
        "\"serves\":2," +
        "\"difficulty\":2," +
        "\"calories\":520," +
        "\"tags\":[\"One-Pan\",\"High Protein\",\"Gluten-Free\"]," +
        "\"ingredients\":[" +
        "{\"amount\":\"2\",\"name\":\"salmon fillets\"}," +
        "{\"amount\":\"250 g\",\"name\":\"asparagus\"}," +
        "{\"amount\":\"150 g\",\"name\":\"cherry tomatoes\"}," +
        "{\"amount\":\"1\",\"name\":\"lemon\"}," +
        "{\"amount\":\"3 cloves\",\"name\":\"garlic\"}," +
        "{\"amount\":\"2 tbsp\",\"name\":\"olive oil\"}," +
        "{\"amount\":\"1 tbsp\",\"name\":\"butter\"}," +
        "{\"amount\":\"a few sprigs\",\"name\":\"fresh dill\"}]," +
        "\"steps\":[" +
        "{\"title\":\"Sear the salmon\",\"text\":\"Heat the olive oil in a large pan over medium-high heat and " +
        "sear the seasoned fillets skin-side down for 4 minutes, pressing gently so the skin crisps evenly.\"}," +
        "{\"title\":\"Add the vegetables\",\"text\":\"Flip the salmon, add the asparagus, tomatoes and sliced " +
        "garlic, and cook for another 3 minutes until the tomatoes just start to blister.\"}," +
        "{\"title\":\"Make it glossy\",\"text\":\"Lower the heat, add the butter and a big squeeze of lemon, " +
        "and spoon the foaming juices over the fillets until everything is glossy.\"}," +
        "{\"title\":\"Serve\",\"text\":\"Finish with fresh dill, lemon wedges and a pinch of flaky salt — " +
        "straight from the pan to the table.\"}]," +
        "\"nutrition\":{\"proteinG\":42,\"carbsG\":11,\"fatG\":33}}";
    foreach (var chunk in Chunks(json, 6))
    {
        yield return chunk;
        await Task.Delay(24, ct);
    }
}
```

- [ ] **Step 3: Build**

Run: `dotnet build "c:\Users\janzi\Desktop\DRYL\DRYL.Website\DRYL.Website.csproj"`
Expected: Build succeeded (the old demo still compiles — it only uses properties that still exist).

- [ ] **Step 4: Commit (website repo)**

```bash
git -C "c:/Users/janzi/Desktop/DRYL/DRYL.Website" add -A
git -C "c:/Users/janzi/Desktop/DRYL/DRYL.Website" commit -m "feat(agents-demo): rich recipe model, choreographed JSON stream, dish photo

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 4: Living Recipe Card demo (website)

**Files:**
- Modify: `c:\Users\janzi\Desktop\DRYL\DRYL.Website\Components\Examples\Agents\StructuredGeneration.razor` (full rewrite)
- Modify: `c:\Users\janzi\Desktop\DRYL\DRYL.Website\Components\Pages\DemoAgents.razor` (description of the section)

**Interfaces:**
- Consumes: `GenerationSnapshot<T>.Raw` (Task 1), `Clock` icon (Task 2), `Recipe`/`Ingredient`/`RecipeStep`/`Nutrition` + `RecipeJson()` (Task 3).
- Notes: `PresenceTransition` and `ChartSegment` live in namespace `DRYL.Components` (already imported); `ImageRounded` is nested → `DrylImage.ImageRounded.Lg`; `BadgeKind` nested → `DrylBadge.BadgeKind.Accent`. `IDrylAiActivityService` lives in `DRYL.Components.Ai` (imported).

- [ ] **Step 1: Rewrite `StructuredGeneration.razor`**

Full file content:

```razor
@using DRYL.Website.Components.Examples.Agents.Simulation
@inject IDrylAiActivityService AiActivity
@implements IDisposable

<div class="col" style="gap: var(--sp-4);">
    <div class="row" style="gap: var(--sp-3); align-items: center;">
        <DrylButton OnClick="Generate" Disabled="Busy" Icon="Sparkle">
            @(_stream is null ? "Generate a recipe" : "Generate again")
        </DrylButton>
        <DrylPresence Visible="Busy" Transition="PresenceTransition.Fade">
            <DrylAiIndicator State="RecipeState">writing your recipe</DrylAiIndicator>
        </DrylPresence>
    </div>

    @if (_stream is not null)
    {
        <DrylAiGenerate T="Recipe" Source="_stream" Key="recipe">
            <ChildContent Context="snap">
                <div class="col" style="gap: var(--sp-3);">
                    <DrylCard Ai="@snap.State">
                        <div class="col" style="gap: var(--sp-4);">

                            <div class="row" style="flex-wrap: wrap; gap: var(--sp-5); align-items: flex-start;">
                                <DrylPresence Visible="@(snap.Value?.Title is not null)"
                                              Transition="PresenceTransition.Scale">
                                    <DrylImage Src="img/dish.jpg" Alt="@(snap.Value?.Title ?? "Generated dish")"
                                               Width="220" Height="220"
                                               Rounded="DrylImage.ImageRounded.Lg"
                                               Ai="@snap.State" />
                                </DrylPresence>

                                <div class="col" style="flex: 1; min-width: 240px; gap: var(--sp-3);">
                                    @if (snap.Value is null)
                                    {
                                        <DrylSkeleton Lines="3" Ai="AiState.Thinking" Label="Thinking about your recipe" />
                                    }
                                    else
                                    {
                                        <h3 style="margin: 0;">@snap.Value.Title</h3>
                                        @if (!string.IsNullOrEmpty(snap.Value.Description))
                                        {
                                            <p style="margin: 0; color: var(--fg-muted);">@snap.Value.Description</p>
                                        }
                                    }

                                    <div class="row" style="flex-wrap: wrap; gap: var(--sp-2); align-items: center;">
                                        <DrylPresence Visible="@(snap.Value is { Minutes: > 0 })" Appear
                                                      Transition="PresenceTransition.SlideUp">
                                            <DrylBadge Icon="Clock">@snap.Value?.Minutes min</DrylBadge>
                                        </DrylPresence>
                                        <DrylPresence Visible="@(snap.Value is { Serves: > 0 })" Appear
                                                      Transition="PresenceTransition.SlideUp">
                                            <DrylBadge Icon="Users">Serves @snap.Value?.Serves</DrylBadge>
                                        </DrylPresence>
                                        <DrylPresence Visible="@(snap.Value is { Calories: > 0 })" Appear
                                                      Transition="PresenceTransition.SlideUp">
                                            <DrylBadge Icon="Flame" Kind="DrylBadge.BadgeKind.Accent">@snap.Value?.Calories kcal</DrylBadge>
                                        </DrylPresence>
                                        <DrylPresence Visible="@(snap.Value is { Difficulty: > 0 })" Appear
                                                      Transition="PresenceTransition.SlideUp">
                                            <DrylRating Value="@snap.Value?.Difficulty" ReadOnly
                                                        AriaLabel="Difficulty" />
                                        </DrylPresence>
                                    </div>

                                    @if (snap.Value?.Tags is { Count: > 0 } tags)
                                    {
                                        <div class="row" style="flex-wrap: wrap; gap: var(--sp-2);">
                                            @for (var i = 0; i < tags.Count; i++)
                                            {
                                                <DrylPresence @key="i" Visible="true" Appear
                                                              Transition="PresenceTransition.Scale">
                                                    <DrylBadge Kind="DrylBadge.BadgeKind.Accent">@tags[i]</DrylBadge>
                                                </DrylPresence>
                                            }
                                        </div>
                                    }
                                </div>
                            </div>

                            <DrylPresence Visible="@(snap.Value?.Ingredients is { Count: > 0 })"
                                          Transition="PresenceTransition.SlideUp">
                                <div class="col" style="gap: var(--sp-2);">
                                    <DrylDivider />
                                    <h5 style="margin: 0;">Ingredients</h5>
                                    <div class="row" style="flex-wrap: wrap; gap: var(--sp-2);">
                                        @{ var ingredients = snap.Value?.Ingredients ?? []; }
                                        @for (var i = 0; i < ingredients.Count; i++)
                                        {
                                            <DrylPresence @key="i" Visible="true" Appear
                                                          Transition="PresenceTransition.Scale">
                                                <DrylBadge>@ingredients[i].Amount @ingredients[i].Name</DrylBadge>
                                            </DrylPresence>
                                        }
                                    </div>
                                </div>
                            </DrylPresence>

                            <DrylPresence Visible="@(snap.Value?.Steps is { Count: > 0 })"
                                          Transition="PresenceTransition.SlideUp">
                                <div class="col" style="gap: var(--sp-2);">
                                    <DrylDivider />
                                    <div class="row" style="flex-wrap: wrap; gap: var(--sp-5); align-items: flex-start;">
                                        <div class="col" style="flex: 2; min-width: 260px; gap: var(--sp-2);">
                                            <h5 style="margin: 0;">Steps</h5>
                                            <DrylTimeline AriaLabel="Recipe steps">
                                                @{ var steps = snap.Value?.Steps ?? []; }
                                                @for (var i = 0; i < steps.Count; i++)
                                                {
                                                    var live = !snap.IsComplete && i == steps.Count - 1;
                                                    <DrylPresence @key="i" Visible="true" Appear
                                                                  Transition="PresenceTransition.SlideUp">
                                                        <DrylTimelineItem Title="@steps[i].Title"
                                                                          Ai="@(live ? AiState.Streaming : AiState.None)">
                                                            @steps[i].Text
                                                        </DrylTimelineItem>
                                                    </DrylPresence>
                                                }
                                            </DrylTimeline>
                                        </div>

                                        <DrylPresence Visible="@(snap.IsComplete && snap.Value?.Nutrition is not null)"
                                                      Transition="PresenceTransition.Scale">
                                            <div class="col" style="flex: 1; min-width: 200px; gap: var(--sp-2);">
                                                <h5 style="margin: 0;">Nutrition</h5>
                                                <DrylDonutChart Segments="@Macros(snap.Value!.Nutrition!)">
                                                    <CenterContent>
                                                        <div class="col" style="align-items: center;">
                                                            <strong>@snap.Value!.Calories</strong>
                                                            <span style="color: var(--fg-dim); font-size: 12px;">kcal</span>
                                                        </div>
                                                    </CenterContent>
                                                </DrylDonutChart>
                                            </div>
                                        </DrylPresence>
                                    </div>
                                </div>
                            </DrylPresence>
                        </div>
                    </DrylCard>

                    <DrylExpansion Title="Raw model output" Icon="Code">
                        <HeaderTrailingContent>
                            <DrylPresence Visible="@(!snap.IsComplete)" Transition="PresenceTransition.Fade">
                                <DrylAiIndicator State="@snap.State">streaming JSON</DrylAiIndicator>
                            </DrylPresence>
                        </HeaderTrailingContent>
                        <ChildContent>
                            @* column-reverse keeps the scroll pinned to the newest tokens without JS *@
                            <div style="max-height: 220px; overflow: auto; display: flex; flex-direction: column-reverse;">
                                <pre class="mono" style="margin: 0; white-space: pre-wrap; word-break: break-all; font-size: 12px; color: var(--fg-dim);">@snap.Raw</pre>
                            </div>
                        </ChildContent>
                    </DrylExpansion>
                </div>
            </ChildContent>
        </DrylAiGenerate>
    }
</div>

@code {
    // Cache the stream in a field: DrylAiGenerate restarts whenever Source changes by reference,
    // so a fresh SimScenarios.RecipeJson() per render would reset the generation mid-stream.
    private IAsyncEnumerable<string>? _stream;

    // Key="recipe" publishes the run's AiState to the activity service — the same coordination
    // a DrylAiScope uses. Here it drives the button and the header indicator.
    private AiState RecipeState => AiActivity.GetState("recipe");
    private bool Busy => RecipeState != AiState.None;

    protected override void OnInitialized() => AiActivity.OnChanged += HandleAiChanged;

    private void HandleAiChanged(string key)
    {
        if (key == "recipe") _ = InvokeAsync(StateHasChanged);
    }

    private void Generate()
    {
        // In production: Runner.GenerateStreamingAsync<Recipe>(yourAgent, yourSession, prompt, aiKey: "recipe").
        _stream = SimScenarios.RecipeJson();
    }

    private static IReadOnlyList<ChartSegment> Macros(Nutrition n) =>
        new ChartSegment[] { new("Protein", n.ProteinG), new("Carbs", n.CarbsG), new("Fat", n.FatG) };

    public void Dispose() => AiActivity.OnChanged -= HandleAiChanged;
}
```

Implementation notes (why it is built this way — keep these behaviours):
- Item lists key `DrylPresence` by **index**, not content: the last array element's string grows char-by-char, and keying by content would remount + replay the enter animation on every character.
- The nutrition donut is gated on `snap.IsComplete` so it scales in exactly once, synchronized with the `Generated` aura wash — the finale.
- The last streaming timeline item wears `Ai="AiState.Streaming"` (marker aura) while it is being written.
- `Busy` returns to false when `DrylAiGenerate` settles (`SettleTo` default `None` clears the key).

- [ ] **Step 2: Sharpen the section description in `DemoAgents.razor`**

Replace the `DemoExample` opening tag for the structured-streaming section with:

```razor
    <DemoExample Title="Structured streaming with DrylAiGenerate&lt;T&gt;" Source="Agents/StructuredGeneration"
                 Description="The model emits JSON for your type — and DRYL streams it straight into live Blazor components. A tolerant partial reader produces a growing typed snapshot on every chunk, so the recipe below assembles itself while the AI writes: image, badges, rating, timeline, donut chart. Open the raw-output panel to watch the JSON that drives it.">
```

- [ ] **Step 3: Build**

Run: `dotnet build "c:\Users\janzi\Desktop\DRYL\DRYL.Website\DRYL.Website.csproj"`
Expected: Build succeeded.

- [ ] **Step 4: Commit (website repo)**

```bash
git -C "c:/Users/janzi/Desktop/DRYL/DRYL.Website" add -A
git -C "c:/Users/janzi/Desktop/DRYL/DRYL.Website" commit -m "feat(agents-demo): living recipe card — structured streaming showcase

Co-Authored-By: Claude Fable 5 <noreply@anthropic.com>"
```

---

### Task 5: Runtime verification

**Files:** none (verification only)

- [ ] **Step 1: Invoke the project `verify` skill** (docs website + Playwright) to launch the site.

- [ ] **Step 2: Verify the choreography on `/components/agents`:**
- Click "Generate a recipe"; button disables, indicator appears.
- Skeleton (Thinking) → title/description stream in → image scales in → badges pop individually → tags/ingredient chips pop in sequence → timeline items slide in with the last one wearing the streaming aura → at completion the donut scales in and the card plays the Generated wash → everything settles, button re-enables as "Generate again".
- Open "Raw model output" during a run: JSON grows, scroll pinned to bottom, `DrylAiIndicator` pulses in the header and disappears at completion.
- Click "Generate again": clean restart, no duplicated content.
- Take screenshots mid-stream and settled; check the browser console for errors.

- [ ] **Step 3: Check 375px width** (browser resize) — card stacks: image above text, donut below timeline, no horizontal overflow.

- [ ] **Step 4: Reduced motion** — emulate `prefers-reduced-motion: reduce` via Playwright and confirm content appears instantly but completely (no stuck invisible presences).

- [ ] **Step 5: Report** findings to the maintainer with screenshots; fix anything broken before declaring done.
