# Streaming Artifact Reveal Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make an `update_<T>` artifact round materialize progressively (Apple "guided generation" feel, ~1.2 s/round) by replaying the already-known merged patch JSON as a growing prefix, instead of merging it atomically.

**Architecture:** `DrylArtifactRun<T>.ApplyPatch` becomes `ApplyPatchAsync`, which reveals the patch as a sequence of growing JSON prefixes over `RevealDuration` (reusing the existing `JsonPartialRepair` + `JsonMerge` engines on a stable committed base), then commits the exact full patch. The auto-injected `update_<T>` tool becomes an async delegate, so the framework's function-invocation loop awaits the reveal and the model's pacing follows naturally. `DrylAiBuild<T>` is unchanged — it already re-renders on `OnChange` and shows the `Streaming` aura.

**Tech Stack:** C# / .NET (multi-target net8/9/10), `System.Text.Json` (`JsonNode`/`JsonElement`), `Microsoft.Extensions.AI` (`AIFunctionFactory`), `Microsoft.Agents.AI`, xUnit (`DRYL.Components.Tests` with `InternalsVisibleTo`).

## Global Constraints

- Experimental package, independently versioned 0.1.0, entirely under `[Unreleased]`; core `DRYL.Components` stays dependency-free.
- Reuse the shared `AiState` vocabulary (`AiState.Streaming` for the reveal) — no new states.
- XML docs on all new/changed public members.
- `Dryl`/PascalCase naming; tokens-not-literals does not apply (no CSS here).
- No new public API except `DrylBuildOptions.RevealDuration` (additive); `ApplyPatch` is `internal`, so its signature change is non-breaking.
- `TimeSpan.Zero` (or negative) `RevealDuration` ⇒ atomic merge, behavior identical to today.
- Build: `dotnet build DRYL.Components.Agents/DRYL.Components.Agents.csproj -c Debug`.
- Test: `dotnet test tests/DRYL.Components.Tests/DRYL.Components.Tests.csproj --filter "FullyQualifiedName~Agents"`.
- Commit trailer: `Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>`.

---

## File Structure

| File | Change | Responsibility |
| --- | --- | --- |
| `DRYL.Components.Agents/Agents/DrylBuildOptions.cs` | Modify | Add `RevealDuration` knob. |
| `DRYL.Components.Agents/Agents/DrylRunBase.cs` | Modify | Add a `DisposalToken` cancelled by `DisposeAsync`, so a reveal can stop cleanly mid-flight. |
| `DRYL.Components.Agents/Agents/DrylArtifactRun.cs` | Modify | Replace `ApplyPatch` with `ApplyPatchAsync` + private `RevealAsync` stepping helper. |
| `DRYL.Components.Agents/Agents/DrylAgentRunner.Build.cs` | Modify | Make `CreateUpdateTool` build an async delegate; link `ct` + `DisposalToken` in `StartBuild`. |
| `tests/DRYL.Components.Tests/Agents/DrylArtifactRunTests.cs` | Modify | Migrate to `await ApplyPatchAsync(..., TimeSpan.Zero, default)`; add progressive / atomic / cancellation tests. |
| `CHANGELOG.md` | Modify | `[Unreleased] → Added` entry for `RevealDuration`. |
| `README.md` | Modify | Update the agents/build component note if user-visible. |

---

### Task 1: Add `RevealDuration` to `DrylBuildOptions`

**Files:**
- Modify: `DRYL.Components.Agents/Agents/DrylBuildOptions.cs`

**Interfaces:**
- Consumes: nothing.
- Produces: `DrylBuildOptions.RevealDuration` — `public TimeSpan RevealDuration { get; init; }`, default `TimeSpan.FromMilliseconds(1200)`. `TimeSpan.Zero` (or negative) disables the reveal.

- [ ] **Step 1: Add the property**

In `DRYL.Components.Agents/Agents/DrylBuildOptions.cs`, add after `UpdateToolName`:

```csharp
    /// <summary>
    /// Target wall-clock duration for each <c>update_&lt;T&gt;</c> round's progressive reveal
    /// (the round's new/changed fields type in over this span, Apple "guided generation" feel).
    /// This is a target per round, not a per-character delay — long and short patches both take
    /// roughly this long. <see cref="System.TimeSpan.Zero"/> (or a negative value) disables the
    /// reveal and merges the patch atomically (identical to a single merge). Default 1.2 s.
    /// </summary>
    public TimeSpan RevealDuration { get; init; } = TimeSpan.FromMilliseconds(1200);
```

- [ ] **Step 2: Build to verify it compiles**

Run: `dotnet build DRYL.Components.Agents/DRYL.Components.Agents.csproj -c Debug`
Expected: Build succeeded, 0 errors.

- [ ] **Step 3: Commit**

```bash
git add DRYL.Components.Agents/Agents/DrylBuildOptions.cs
git commit -m "feat(agents): add DrylBuildOptions.RevealDuration knob

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 2: Add a `DisposalToken` to `DrylRunBase`

**Files:**
- Modify: `DRYL.Components.Agents/Agents/DrylRunBase.cs`

**Interfaces:**
- Consumes: nothing.
- Produces: `internal CancellationToken DrylRunBase.DisposalToken { get; }` — a token that is cancelled when `DisposeAsync` runs. Used by the build wiring to stop an in-flight reveal.

- [ ] **Step 1: Add the CTS field and token**

In `DRYL.Components.Agents/Agents/DrylRunBase.cs`, add a field next to the existing `_completed` field (after line 17):

```csharp
    private readonly CancellationTokenSource _cts = new();
    private bool _disposed;
```

Add this property near `TextStream` (after line 41):

```csharp
    /// <summary>Cancelled when the run is disposed; lets in-flight work (e.g. an artifact reveal) stop cleanly.</summary>
    internal CancellationToken DisposalToken => _cts.Token;
```

- [ ] **Step 2: Cancel on dispose**

Replace the existing `DisposeAsync` body:

```csharp
    /// <summary>Cancels the run and releases its resources.</summary>
    public ValueTask DisposeAsync()
    {
        _textChannel.Writer.TryComplete();
        _completed.TrySetResult();
        return ValueTask.CompletedTask;
    }
```

with:

```csharp
    /// <summary>Cancels the run and releases its resources.</summary>
    public ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            _disposed = true;
            _textChannel.Writer.TryComplete();
            _completed.TrySetResult();
            _cts.Cancel();
            _cts.Dispose();
        }
        return ValueTask.CompletedTask;
    }
```

- [ ] **Step 3: Build to verify it compiles**

Run: `dotnet build DRYL.Components.Agents/DRYL.Components.Agents.csproj -c Debug`
Expected: Build succeeded, 0 errors.

- [ ] **Step 4: Commit**

```bash
git add DRYL.Components.Agents/Agents/DrylRunBase.cs
git commit -m "feat(agents): DrylRunBase exposes a DisposalToken cancelled on dispose

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 3: Migrate existing `DrylArtifactRunTests` to the async API (write the failing tests first)

This task converts the three existing tests to the new `ApplyPatchAsync` signature **before** the implementation exists, so they fail to compile, then pass once Task 4 lands. We do the test migration here and the implementation in Task 4 to keep the red→green cycle honest.

**Files:**
- Modify: `tests/DRYL.Components.Tests/Agents/DrylArtifactRunTests.cs`

**Interfaces:**
- Consumes (from Task 4, not yet written): `internal Task<string> DrylArtifactRun<T>.ApplyPatchAsync(JsonElement patch, int? maxRounds, TimeSpan revealDuration, CancellationToken ct)`.
- Produces: nothing.

- [ ] **Step 1: Rewrite the three existing tests to call `ApplyPatchAsync` with `TimeSpan.Zero`**

Replace the three `[Fact]` methods in `tests/DRYL.Components.Tests/Agents/DrylArtifactRunTests.cs` (keep the `Dish` class and `El` helper):

```csharp
    [Fact]
    public async Task ApplyPatchAsync_atomic_merges_and_counts_rounds()
    {
        var run = new DrylArtifactRun<Dish>(new JsonSerializerOptions());
        var changes = 0;
        run.OnChange += () => changes++;

        await run.ApplyPatchAsync(El("""{"title":"Pasta"}"""), maxRounds: null, TimeSpan.Zero, default);
        await run.ApplyPatchAsync(El("""{"steps":["boil","drain"]}"""), maxRounds: null, TimeSpan.Zero, default);

        Assert.Equal("Pasta", run.Artifact!.Title);
        Assert.Equal(2, run.Artifact.Steps.Count);
        Assert.Equal(2, run.Round);
        Assert.Equal(2, changes);   // exactly one OnChange per atomic round
    }

    [Fact]
    public async Task ApplyPatchAsync_returns_a_receipt_below_the_cap()
    {
        var run = new DrylArtifactRun<Dish>(new JsonSerializerOptions());
        var receipt = await run.ApplyPatchAsync(El("""{"title":"X"}"""), maxRounds: 12, TimeSpan.Zero, default);
        Assert.Contains("round 1", receipt);
    }

    [Fact]
    public async Task ApplyPatchAsync_returns_a_finalize_nudge_at_the_cap()
    {
        var run = new DrylArtifactRun<Dish>(new JsonSerializerOptions());
        var receipt = await run.ApplyPatchAsync(El("""{"title":"X"}"""), maxRounds: 1, TimeSpan.Zero, default);
        Assert.Contains("Maximum refinement rounds reached", receipt);
    }
```

- [ ] **Step 2: Run the tests to verify they fail to compile**

Run: `dotnet test tests/DRYL.Components.Tests/DRYL.Components.Tests.csproj --filter "FullyQualifiedName~DrylArtifactRunTests"`
Expected: BUILD FAILURE — `'DrylArtifactRun<Dish>' does not contain a definition for 'ApplyPatchAsync'`.

- [ ] **Step 3: Commit the failing tests**

```bash
git add tests/DRYL.Components.Tests/Agents/DrylArtifactRunTests.cs
git commit -m "test(agents): migrate DrylArtifactRunTests to async ApplyPatchAsync (red)

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 4: Implement `ApplyPatchAsync` + `RevealAsync` in `DrylArtifactRun<T>`

**Files:**
- Modify: `DRYL.Components.Agents/Agents/DrylArtifactRun.cs`

**Interfaces:**
- Consumes: `JsonMerge.Merge(JsonNode?, JsonNode?)`, `JsonPartialRepair.Close(string)`, `DrylRunBase.State` (`internal set`), `DrylRunBase.Raise()`.
- Produces:
  - `internal Task<string> ApplyPatchAsync(JsonElement patch, int? maxRounds, TimeSpan revealDuration, CancellationToken ct)` — drives the reveal, commits the exact full patch, increments `Round`, returns the same receipt strings as before (`"Updated (round N)."` / the cap nudge).

- [ ] **Step 1: Replace `ApplyPatch` with `ApplyPatchAsync` and add `RevealAsync`**

In `DRYL.Components.Agents/Agents/DrylArtifactRun.cs`, replace the entire `ApplyPatch` method (lines 28–47) with:

```csharp
    /// <summary>
    /// Merge a partial-<typeparamref name="T"/> patch into the running artifact and return a short
    /// receipt for the model. When <paramref name="revealDuration"/> is positive, the patch's
    /// new/changed fields materialize progressively (Apple "guided generation" feel) over that
    /// span while previously-committed fields stay stable; otherwise the merge is atomic. The
    /// commit always uses the exact, full patch, so committed state can never be a repaired prefix.
    /// When <paramref name="maxRounds"/> is reached, returns a finalize nudge instead of the receipt.
    /// </summary>
    internal async Task<string> ApplyPatchAsync(
        JsonElement patch, int? maxRounds, TimeSpan revealDuration, CancellationToken ct)
    {
        var committedBase = _json;   // stable base this round overlays; earlier fields never re-animate
        var patchRaw = patch.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null
            ? null
            : patch.GetRawText();

        if (patchRaw is not null && revealDuration > TimeSpan.Zero)
        {
            try
            {
                await RevealAsync(committedBase, patchRaw, revealDuration, ct);
            }
            catch (OperationCanceledException)
            {
                // Cancelled mid-reveal — fall through and commit the final merged state so no
                // half-revealed artifact is left behind.
            }
        }

        // Commit the exact, full patch (never a repaired prefix): committed state is always the true merge.
        var fullPatch = patchRaw is null ? null : JsonNode.Parse(patchRaw);
        _json = JsonMerge.Merge(committedBase, fullPatch);
        Round++;
        Artifact = _json is null ? default : _json.Deserialize<T>(_deserializeOptions);
        Raise();

        return maxRounds is { } m && Round >= m
            ? "Maximum refinement rounds reached — stop refining and give your final answer now."
            : $"Updated (round {Round}).";
    }

    // Replays patchRaw as a growing prefix in ~N steps over revealDuration. Each snapshot is
    // repaired into valid JSON (JsonPartialRepair) and overlaid onto the stable committed base, so
    // a mid-token snapshot holds the last good artifact rather than flashing to default.
    private async Task RevealAsync(JsonNode? committedBase, string patchRaw, TimeSpan revealDuration, CancellationToken ct)
    {
        const int minSteps = 4;
        const int maxSteps = 40;
        var steps = Math.Clamp(patchRaw.Length, minSteps, maxSteps);
        var stepDelay = revealDuration / steps;

        State = AiState.Streaming;

        var prev = 0;
        for (var step = 1; step <= steps; step++)
        {
            var target = (int)((long)patchRaw.Length * step / steps);
            target = ExtendToWordBoundary(patchRaw, target);
            if (target <= prev && step < steps) continue;   // skip zero-width steps; always emit the last
            prev = target;

            var repaired = JsonPartialRepair.Close(patchRaw[..target]);
            var merged = JsonMerge.Merge(committedBase, JsonNode.Parse(repaired));
            Artifact = merged is null ? default : merged.Deserialize<T>(_deserializeOptions);
            Raise();

            if (step < steps) await Task.Delay(stepDelay, ct);
        }
    }

    // Advance to the end of the current token so the reveal grows word-by-word, not mid-word.
    private static int ExtendToWordBoundary(string s, int idx)
    {
        if (idx >= s.Length) return s.Length;
        while (idx < s.Length && !char.IsWhiteSpace(s[idx])) idx++;
        return idx;
    }
```

- [ ] **Step 2: Run the migrated tests to verify they pass**

Run: `dotnet test tests/DRYL.Components.Tests/DRYL.Components.Tests.csproj --filter "FullyQualifiedName~DrylArtifactRunTests"`
Expected: PASS (3 tests). The `Zero`-duration tests confirm behavior preservation (one `OnChange`/round, same finals).

- [ ] **Step 3: Commit**

```bash
git add DRYL.Components.Agents/Agents/DrylArtifactRun.cs
git commit -m "feat(agents): ApplyPatchAsync reveals the patch progressively over RevealDuration

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 5: Wire the async update tool and link cancellation in `StartBuild`

**Files:**
- Modify: `DRYL.Components.Agents/Agents/DrylAgentRunner.Build.cs`

**Interfaces:**
- Consumes: `DrylArtifactRun<T>.ApplyPatchAsync(...)` (Task 4), `DrylBuildOptions.RevealDuration` (Task 1), `DrylRunBase.DisposalToken` (Task 2).
- Produces:
  - `internal static AITool CreateUpdateTool<T>(DrylArtifactRun<T> run, DrylBuildOptions options, CancellationToken ct = default)` — `ct` is optional (default keeps existing 2-arg call sites in `DrylAgentRunnerBuildTests` compiling); the delegate is now `Func<JsonElement, Task<string>>`.

- [ ] **Step 1: Make `CreateUpdateTool` async and accept a cancellation token**

In `DRYL.Components.Agents/Agents/DrylAgentRunner.Build.cs`, replace the `CreateUpdateTool<T>` method (lines 50–68) with:

```csharp
    /// <summary>
    /// Builds the auto-generated <c>update_&lt;T&gt;</c> tool: it accepts a partial-<typeparamref name="T"/>
    /// JSON patch, reveals it into <paramref name="run"/> over <see cref="DrylBuildOptions.RevealDuration"/>,
    /// and returns a receipt for the model. The delegate is async, so the framework's function-invocation
    /// loop awaits the reveal and the model's next turn paces behind it.
    /// </summary>
    internal static AITool CreateUpdateTool<T>(
        DrylArtifactRun<T> run, DrylBuildOptions options, CancellationToken ct = default)
    {
        var typeName = typeof(T).Name;
        var backtick = typeName.IndexOf('`');
        if (backtick >= 0) typeName = typeName[..backtick];
        var toolName = options.UpdateToolName ?? $"update_{typeName.ToLowerInvariant()}";
        var schema = AIJsonUtilities.CreateJsonSchema(typeof(T), serializerOptions: JsonOpts).GetRawText();
        var description =
            "Record or refine the artifact as you learn more. Call this repeatedly — include only " +
            "the fields you want to set or change; all fields are optional. Artifact shape: " + schema;

        return AIFunctionFactory.Create(
            (JsonElement patch) => run.ApplyPatchAsync(patch, options.MaxRounds, options.RevealDuration, ct),
            toolName, description);
    }
```

- [ ] **Step 2: Link `ct` + `DisposalToken` in `StartBuild` and pass it through**

In the same file, replace the `StartBuild<T>` method body (lines 29–48) with:

```csharp
    public DrylArtifactRun<T> StartBuild<T>(
        AIAgent agent, AgentSession session, string prompt,
        DrylBuildOptions? options = null, string? aiKey = null, CancellationToken ct = default)
    {
        options ??= new DrylBuildOptions();
        var run = new DrylArtifactRun<T>(JsonOpts);

        // Stop an in-flight reveal when the caller cancels OR the run is disposed.
        var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, run.DisposalToken);

        var runOptions = new ChatClientAgentRunOptions
        {
            ChatOptions = new ChatOptions
            {
                Instructions = options.Guidance ?? DefaultBuildGuidance,
                Tools = new List<AITool> { CreateUpdateTool(run, options, linkedCts.Token) },
            }
        };

        var updates = agent.RunStreamingAsync(prompt, session, runOptions, linkedCts.Token);
        _ = ProcessAsync(run, updates, aiKey, linkedCts.Token)
            .ContinueWith(_ => linkedCts.Dispose(), TaskScheduler.Default);
        return run;
    }
```

- [ ] **Step 3: Build to verify it compiles**

Run: `dotnet build DRYL.Components.Agents/DRYL.Components.Agents.csproj -c Debug`
Expected: Build succeeded, 0 errors.

- [ ] **Step 4: Run the existing build/tool-name tests to confirm they still pass**

Run: `dotnet test tests/DRYL.Components.Tests/DRYL.Components.Tests.csproj --filter "FullyQualifiedName~DrylAgentRunnerBuildTests"`
Expected: PASS (3 tests) — the 2-arg `CreateUpdateTool(run, new DrylBuildOptions())` calls still compile via the default `ct`.

- [ ] **Step 5: Commit**

```bash
git add DRYL.Components.Agents/Agents/DrylAgentRunner.Build.cs
git commit -m "feat(agents): wire async update tool and link reveal cancellation

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 6: Test — progressive reveal grows a field monotonically while the base stays intact

**Files:**
- Modify: `tests/DRYL.Components.Tests/Agents/DrylArtifactRunTests.cs`

**Interfaces:**
- Consumes: `ApplyPatchAsync` (Task 4).
- Produces: nothing.

- [ ] **Step 1: Write the failing test**

Add to `DrylArtifactRunTests`:

```csharp
    [Fact]
    public async Task ApplyPatchAsync_reveals_a_field_progressively_keeping_the_base()
    {
        var run = new DrylArtifactRun<Dish>(new JsonSerializerOptions());
        // Commit a base round atomically.
        await run.ApplyPatchAsync(El("""{"title":"Soup","steps":["chop"]}"""), maxRounds: null, TimeSpan.Zero, default);

        var titles = new List<string?>();
        run.OnChange += () => titles.Add(run.Artifact?.Title);

        // Reveal a long new title over a short span; capture every snapshot.
        await run.ApplyPatchAsync(
            El("""{"title":"Roasted Tomato Basil Soup With Garlic Croutons"}"""),
            maxRounds: null, TimeSpan.FromMilliseconds(120), default);

        // The title length is non-decreasing and strictly increases across the reveal.
        var lengths = titles.Where(t => t is not null).Select(t => t!.Length).ToList();
        Assert.True(lengths.Count >= 3, $"expected several intermediate snapshots, got {lengths.Count}");
        for (var i = 1; i < lengths.Count; i++)
            Assert.True(lengths[i] >= lengths[i - 1], "title length must never shrink during the reveal");
        Assert.True(lengths[^1] > lengths[0], "title must grow across the reveal");

        // Final commit is exact, and the previously-committed base field is preserved throughout.
        Assert.Equal("Roasted Tomato Basil Soup With Garlic Croutons", run.Artifact!.Title);
        Assert.Single(run.Artifact.Steps);
        Assert.Equal("chop", run.Artifact.Steps[0]);
    }
```

- [ ] **Step 2: Run the test to verify it passes**

Run: `dotnet test tests/DRYL.Components.Tests/DRYL.Components.Tests.csproj --filter "FullyQualifiedName~ApplyPatchAsync_reveals_a_field_progressively_keeping_the_base"`
Expected: PASS. (If it fails because the base round emitted a snapshot before subscription, note the subscription is added *after* the base round — only reveal snapshots are captured.)

- [ ] **Step 3: Commit**

```bash
git add tests/DRYL.Components.Tests/Agents/DrylArtifactRunTests.cs
git commit -m "test(agents): assert progressive reveal grows a field and keeps the base

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 7: Test — atomic when `RevealDuration` is `Zero`

**Files:**
- Modify: `tests/DRYL.Components.Tests/Agents/DrylArtifactRunTests.cs`

**Interfaces:**
- Consumes: `ApplyPatchAsync` (Task 4).
- Produces: nothing.

- [ ] **Step 1: Write the failing test**

Add to `DrylArtifactRunTests`:

```csharp
    [Fact]
    public async Task ApplyPatchAsync_is_atomic_when_duration_is_zero()
    {
        var run = new DrylArtifactRun<Dish>(new JsonSerializerOptions());
        var snapshots = 0;
        run.OnChange += () => snapshots++;

        await run.ApplyPatchAsync(
            El("""{"title":"Roasted Tomato Basil Soup With Garlic Croutons"}"""),
            maxRounds: null, TimeSpan.Zero, default);

        Assert.Equal(1, snapshots);   // exactly one effective update — no intermediate reveal snapshots
        Assert.Equal("Roasted Tomato Basil Soup With Garlic Croutons", run.Artifact!.Title);
        Assert.Equal(1, run.Round);
    }
```

- [ ] **Step 2: Run the test to verify it passes**

Run: `dotnet test tests/DRYL.Components.Tests/DRYL.Components.Tests.csproj --filter "FullyQualifiedName~ApplyPatchAsync_is_atomic_when_duration_is_zero"`
Expected: PASS.

- [ ] **Step 3: Commit**

```bash
git add tests/DRYL.Components.Tests/Agents/DrylArtifactRunTests.cs
git commit -m "test(agents): assert ApplyPatchAsync is atomic at TimeSpan.Zero

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 8: Test — cancellation mid-reveal commits the final artifact, no exception escapes

**Files:**
- Modify: `tests/DRYL.Components.Tests/Agents/DrylArtifactRunTests.cs`

**Interfaces:**
- Consumes: `ApplyPatchAsync` (Task 4).
- Produces: nothing.

- [ ] **Step 1: Write the failing test**

Add to `DrylArtifactRunTests`:

```csharp
    [Fact]
    public async Task ApplyPatchAsync_cancelled_midreveal_commits_the_final_artifact()
    {
        var run = new DrylArtifactRun<Dish>(new JsonSerializerOptions());
        using var cts = new CancellationTokenSource();

        // Cancel almost immediately so the reveal is interrupted partway through a long span.
        run.OnChange += () => cts.CancelAfter(0);

        // Must NOT throw — cancellation is absorbed and the final state committed.
        await run.ApplyPatchAsync(
            El("""{"title":"Roasted Tomato Basil Soup","steps":["chop","simmer","blend"]}"""),
            maxRounds: null, TimeSpan.FromSeconds(5), cts.Token);

        // Final state is the exact, full merge — never a half-revealed prefix.
        Assert.Equal("Roasted Tomato Basil Soup", run.Artifact!.Title);
        Assert.Equal(3, run.Artifact.Steps.Count);
        Assert.Equal(1, run.Round);
    }
```

- [ ] **Step 2: Run the test to verify it passes**

Run: `dotnet test tests/DRYL.Components.Tests/DRYL.Components.Tests.csproj --filter "FullyQualifiedName~ApplyPatchAsync_cancelled_midreveal_commits_the_final_artifact"`
Expected: PASS — `ApplyPatchAsync` catches `OperationCanceledException` from `Task.Delay` and commits the full patch.

- [ ] **Step 3: Run the full Agents test slice**

Run: `dotnet test tests/DRYL.Components.Tests/DRYL.Components.Tests.csproj --filter "FullyQualifiedName~Agents"`
Expected: PASS (all Agents tests green).

- [ ] **Step 4: Commit**

```bash
git add tests/DRYL.Components.Tests/Agents/DrylArtifactRunTests.cs
git commit -m "test(agents): assert cancelled reveal commits the final artifact cleanly

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

### Task 9: Documentation — CHANGELOG + README

**Files:**
- Modify: `CHANGELOG.md`
- Modify: `README.md`

**Interfaces:**
- Consumes: nothing.
- Produces: nothing.

- [ ] **Step 1: Add the CHANGELOG entry**

In `CHANGELOG.md`, under `[Unreleased] → Added` (create the `### Added` sub-heading only if it does not already exist under `[Unreleased]`), add:

```markdown
- `DRYL.Components.Agents` — Collaborative artifact builds now reveal each `update_<T>` round progressively (Apple "guided generation" feel) instead of merging atomically; tunable via new `DrylBuildOptions.RevealDuration` (`TimeSpan`, default 1.2 s; `TimeSpan.Zero` = atomic)
```

- [ ] **Step 2: Update the README note if user-visible**

Open `README.md`, locate the agents / collaborative-build note (search for `StartBuild` or `DrylAiBuild`). If it describes the build behavior, append a short clause: "rounds reveal progressively (`DrylBuildOptions.RevealDuration`)". If there is no such note, skip this step (no row to change).

- [ ] **Step 3: Verify the docs reference real symbols**

Run: `dotnet build DRYL.Components.Agents/DRYL.Components.Agents.csproj -c Debug`
Expected: Build succeeded (sanity check the package still compiles; docs are prose so this just confirms nothing else regressed).

- [ ] **Step 4: Commit**

```bash
git add CHANGELOG.md README.md
git commit -m "docs(agents): changelog + readme for progressive artifact reveal

Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>"
```

---

## Self-Review

**Spec coverage:**
- Progressive prefix reveal mechanism (spec "Core mechanism") → Task 4 (`RevealAsync` + `ApplyPatchAsync`).
- `ApplyPatch` → async `ApplyPatchAsync` with the exact signature (spec "Where it lives") → Task 4.
- `DrylAiBuild<T>` unchanged (spec) → no task needed; re-renders on `OnChange`, `Streaming` aura shown because `RevealAsync` sets `State = AiState.Streaming`.
- Async tool delegate via `AIFunctionFactory.Create(Func<…,Task<string>>)` (spec "Tool wiring") → Task 5.
- Pacing: `N = clamp(len, min, max)`, `stepDelay = RevealDuration / N` (spec "Pacing") → Task 4 `RevealAsync`.
- Concurrency/cancellation: single writer (tool awaited in invocation loop); honors `ct`; commits final on cancel; observes `DisposeAsync` (spec "Concurrency & cancellation") → Tasks 2, 4, 5, 8.
- Public API: additive `RevealDuration`, `Zero` disables (spec "Public API impact") → Tasks 1, 7.
- Error handling: repaired prefixes hold last good; final commit is exact; `Zero`/negative short-circuits (spec "Error handling") → Task 4.
- Testing strategy — behavior preservation, progressive, atomic, cancellation (spec) → Tasks 3, 6, 7, 8.
- Global constraints (`[Unreleased]`, `AiState.Streaming`, XML docs, build/test cmds, commit trailer) → Global Constraints + every task.
- Out of scope (live token streaming, `DrylAiGenerate`, website, granularity config) → correctly excluded; no tasks.

**Placeholder scan:** No TBD/TODO; every code step shows full code; every command has expected output. Task 9 Step 2 is conditional on an existing README note (no invented row) — acceptable, the alternative (skip) is explicit.

**Type consistency:** `ApplyPatchAsync(JsonElement, int?, TimeSpan, CancellationToken)` is identical in Tasks 3 (consumer), 4 (producer), 5 (caller), 6–8 (tests). `RevealDuration` (`TimeSpan`) identical across Tasks 1, 4, 5. `DisposalToken` (`CancellationToken`) identical across Tasks 2, 5. `CreateUpdateTool<T>(run, options, ct = default)` matches the existing 2-arg test call sites. `State` set via `internal set` from within the assembly. Consistent.

---

**Plan complete and saved to `docs/superpowers/plans/2026-06-25-streaming-artifact-reveal.md`. Two execution options:**

**1. Subagent-Driven (recommended)** — I dispatch a fresh subagent per task, review between tasks, fast iteration.

**2. Inline Execution** — Execute tasks in this session using executing-plans, batch execution with checkpoints.

The user asked me to implement it, so I'll proceed inline unless told otherwise.
