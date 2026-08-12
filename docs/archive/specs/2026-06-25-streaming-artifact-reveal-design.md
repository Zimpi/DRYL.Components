# Streaming Artifact Reveal — Design

> **Status:** Approved design, ready for implementation plan.
> **Package:** `DRYL.Components.Agents` (experimental, 0.1.0, all under `[Unreleased]`).
> **Branch:** `feature/agents-package`.

## Problem

The collaborative artifact builder (`DrylAgentRunner.StartBuild<T>` → `DrylArtifactRun<T>` → `DrylAiBuild<T>`) updates the rendered artifact through **discrete tool merges**: each `update_<T>` tool call merges a full patch atomically, so the card *appears* a whole round at a time rather than streaming in. Users expect the a guided, type-as-you-go reveal — content materializing token-by-token — which the sibling single-shot path (`DrylAiGenerate<T>`) already delivers.

## Why true token streaming is impossible here (spike result)

A feasibility spike (offline, inspecting `Microsoft.Extensions.AI` 10.x + `Microsoft.Agents.AI` 1.10.0) established that **the framework does not surface incremental tool-call argument deltas**:

- `OpenAIChatClient.FunctionCallInfo` is documented as a POCO that *"concatenates information for a single function call from across multiple streaming updates"* — the provider accumulates argument fragments **internally** and emits one complete `FunctionCallContent`.
- `FunctionCallContent.Arguments` is a fully-parsed `IDictionary<string, object?>`; there is no partial/fragment representation in the content model.

So through `agent.RunStreamingAsync` → `AgentResponseUpdate.Contents`, an `update_<T>` call always arrives **complete**. Getting raw argument tokens would require dropping below `Microsoft.Extensions.AI` to the raw OpenAI SDK and hand-managing tool calls — discarding the entire `AsAIAgent` function-invocation loop, provider-locked, high risk. **Rejected.**

The design below ("Approach B") therefore produces the effect by **replaying the real, already-known merged content** progressively, reusing the package's existing partial-JSON engine.

## Goal

When an `update_<T>` round lands, the artifact's **new/changed fields materialize progressively** (character/small-word granularity, ~1.2 s per round) while previously-committed fields stay stable. The collaborative ask → refine → ask loop, the round counter, and `MaxRounds` semantics are unchanged.

## Architecture

### Core mechanism — progressive prefix reveal

The full patch JSON is already known when the tool fires. Instead of one atomic merge, reveal it over `RevealDuration`:

```
for each growing prefix P of the patch's raw JSON text (≈ N steps):
    repaired   = JsonPartialRepair.Close(P)          // existing engine: valid JSON from a partial buffer
    patchNode  = JsonNode.Parse(repaired)
    merged     = JsonMerge.Merge(committedBase, patchNode)   // existing engine: base stays, patch overlays
    Artifact   = merged.Deserialize<T>(caseInsensitiveOptions)
    State      = Streaming
    Raise()                                            // DrylAiBuild re-renders (already subscribes to OnChange)
    await Task.Delay(stepDelay, ct)

// commit
_json   = full merge(committedBase, fullPatch)
Round  += 1
Artifact = _json.Deserialize<T>()
Raise()
```

Because the patch overlays a **stable committed base**, fields from earlier rounds never re-animate — only this round's fields type in, in document order. `JsonMerge`'s existing semantics (objects merge, arrays/scalars replace, null/absent leaves existing) are reused unchanged; arrays in the patch grow toward their final value as the prefix lengthens.

### Where it lives

- **`DrylArtifactRun<T>`** owns the reveal (it already owns `_json`, the merge, deserialization, and `Round`). `ApplyPatch` becomes async:
  `internal Task<string> ApplyPatchAsync(JsonElement patch, int? maxRounds, TimeSpan revealDuration, CancellationToken ct)`.
  It drives the reveal loop, sets `State = Streaming` during the reveal, returns the same receipt string (`"Updated (round N)."` / the `MaxRounds` finalize nudge) so the model's loop is unaffected.
- **`DrylAiBuild<T>`** is **unchanged** — it re-renders on `OnChange` and renders whatever `Artifact` is current. The existing `Streaming` aura now shows during the reveal.
- **Tool wiring** — `CreateUpdateTool<T>` builds an **async** delegate (`AIFunctionFactory.Create` supports `Func<…, Task<string>>`) calling `ApplyPatchAsync`. Because the function-invocation loop **awaits** the tool, the reveal naturally paces the run: the model's next turn does not stream until the reveal completes. No background threads, no races against the UI.

### Pacing

`RevealDuration` is a **target duration per round**, not a per-character delay. The reveal splits the patch into roughly `N = clamp(patchTextLength, minSteps, maxSteps)` steps and computes `stepDelay = RevealDuration / N`, so a long recipe and a short one both take ~1.2 s (never drags). Granularity (advance by a few characters / to the next word boundary per step) is an internal detail, not a public knob.

### Concurrency & cancellation

- `ApplyPatchAsync` runs on the function-invocation continuation; the `await foreach` in `ProcessAsync` is suspended until it returns, so there is a single writer to `_json`/`Artifact`/`Round` at a time.
- The reveal honors a `CancellationToken` (sourced from the run's cancellation / `DisposeAsync`). On cancel it stops the loop and **commits the final merged state** (no half-revealed artifact left behind), then returns.
- `DrylRunBase.DisposeAsync` already completes the run; the reveal observes the same cancellation so disposing mid-reveal is clean.

## Public API impact

- **No breaking change.** `ApplyPatch` is `internal` — its signature change is invisible to consumers (`DRYL.Components.Tests` has `InternalsVisibleTo`, so its callers are updated in-repo).
- **Additive:** `DrylBuildOptions.RevealDuration` — `TimeSpan`, default `~1.2 s`. `TimeSpan.Zero` disables the reveal (atomic merge, identical to today's behavior) for headless/test/opt-out use. This is the single new knob (YAGNI — no granularity/enum config).

## Components & responsibilities

| Unit | Responsibility | Depends on |
| --- | --- | --- |
| `DrylArtifactRun<T>.ApplyPatchAsync` | Drive the progressive reveal, commit, count rounds, return receipt | `JsonMerge`, `JsonPartialRepair`, `RevealDuration` |
| `DrylBuildOptions.RevealDuration` | Per-round reveal target duration (`Zero` = atomic) | — |
| `CreateUpdateTool<T>` / `StartBuild<T>` | Wire the async tool delegate, pass `RevealDuration` + run cancellation | `ApplyPatchAsync` |
| `DrylAiBuild<T>` | Unchanged — renders `Artifact` on `OnChange`; shows `Streaming` aura during reveal | `DrylArtifactRun<T>` |

A small private helper (e.g. an internal `ArtifactReveal` stepping routine) may be extracted to keep `ApplyPatchAsync` focused; this is an implementation detail for the plan.

## Error handling

- Mid-reveal parse failures are absorbed by `JsonPartialRepair.Close` (it returns a parseable prefix or `"null"`); a failed intermediate snapshot **holds the last good** artifact rather than flashing to default (same guarantee `PartialJsonReader` already gives `DrylAiGenerate`).
- The final commit uses the full, exact patch (not a repaired prefix), so the committed artifact is always the true merge — the reveal can never corrupt committed state.
- A `Zero`/negative `RevealDuration` short-circuits to a single atomic merge.

## Testing strategy

- **Existing `DrylArtifactRunTests`** — updated to `await ApplyPatchAsync(..., TimeSpan.Zero, default)`. With `Zero` the finals (`Artifact`, `Round`, `OnChange` count for the commit) match today's assertions exactly — proves behavior preservation.
- **New: progressive reveal** — with a small non-zero duration, subscribe to `OnChange`, capture the sequence of `Artifact` snapshots for one round, and assert a patch field **grows monotonically** across ≥2 intermediate snapshots (e.g. `Title` length strictly increases) while a previously-committed base field stays intact. Proves materialization is progressive, not atomic, and that the stable base is preserved.
- **New: atomic when `Zero`** — assert exactly one effective artifact update (no intermediate snapshots) when `RevealDuration == TimeSpan.Zero`.
- **New: cancellation** — cancel mid-reveal; assert the run ends with the fully-committed artifact (not a partial) and no exception escapes.
- All tests use `dotnet test … --filter "FullyQualifiedName~Agents"`; the build/test commands and commit-trailer conventions from the prior plan's Global Constraints carry over.

## Out of scope

- Token streaming sourced from the live model (proven infeasible — see spike).
- Changes to `DrylAiGenerate<T>` / the single-shot structured path.
- The `DRYL.Website` demo wiring (separate repo); a follow-up may set `RevealDuration` there, but it is not part of this package change.
- Per-field/granularity configuration, easing curves, or animation modes (YAGNI).

## Global constraints (carried from the Agents package)

- Experimental package, independently versioned 0.1.0, entirely under `[Unreleased]`; core `DRYL.Components` stays dependency-free.
- Reuse the shared `AiState` vocabulary (`Streaming` for the reveal) — no new states.
- XML docs on all new/changed public members.
- `Dryl`/PascalCase naming.
- Build: `dotnet build DRYL.Components.Agents/DRYL.Components.Agents.csproj -c Debug`.
- Test: `dotnet test tests/DRYL.Components.Tests/DRYL.Components.Tests.csproj --filter "FullyQualifiedName~Agents"`.
- Commit trailer: `Co-Authored-By: Claude Opus 4.8 <noreply@anthropic.com>`.
