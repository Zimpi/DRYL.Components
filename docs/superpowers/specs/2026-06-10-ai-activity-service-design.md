# DRYL AI Activity Service & Streaming — Design

**Date:** 2026-06-10
**Status:** Approved for planning

## Goal

Turn DRYL's "AI-native" promise from a per-component styling detail into real
orchestration. Today every AI-aware component sets its `Ai` (`AiState`) parameter
in isolation. This work adds a central service that coordinates `AiState` across
components keyed by operation, plus a first-class helper for binding LLM token
streams to the UI — without adding any external runtime dependency (rule 2.8).

## Non-goals

- No LLM SDK, no `Microsoft.Extensions.AI` hard dependency — DRYL stays
  dependency-free. Consumers bring their own backend and hand us an
  `IAsyncEnumerable<string>` / drive the service manually.
- No new `AiState` values, no new AI animation/color/token. We reuse the existing
  `.ai-aura*` primitives and the `AiState` enum (rules 2.10, 2.1). **No `dryl.css`
  change.**
- No `IDrylAiCompletionSource` / suggestion-provider abstraction (a separate idea,
  out of scope for this increment).

## Architecture

New folder `DRYL.Components/Ai/`, mirroring `Toasts/`, `Dialogs/`, `Notifications/`.

### 1. `IDrylAiActivityService` / `DrylAiActivityService` (scoped)

Registered in `AddDrylComponents()` alongside the existing three services.
Tracks the current `AiState` per string `key`.

```csharp
public interface IDrylAiActivityService
{
    /// Begin an operation on a key; returns a disposable handle.
    IDrylAiOperation Begin(string key, AiState initial = AiState.Thinking);

    /// Current state for a key (AiState.None if unknown).
    AiState GetState(string key);

    /// Force a key to a state (escape hatch; also used by the handle).
    void Set(string key, AiState state);

    /// Clear a key back to AiState.None.
    void Clear(string key);

    /// Raised whenever a key's state changes. Argument = the changed key.
    event Action<string>? OnChanged;

    /// Drive a token stream end-to-end: Thinking -> Streaming (first token)
    /// -> Generated (completion). Invokes onToken per chunk. Always clears
    /// (or settles) the key in a finally block, even on cancel/exception.
    Task StreamAsync(string key, IAsyncEnumerable<string> tokens,
                     Action<string> onToken, CancellationToken ct = default);
}

public interface IDrylAiOperation : IDisposable
{
    string Key { get; }
    void Thinking();
    void Streaming();
    void Generated();   // one-shot reveal state; Dispose() settles afterwards
}
```

**Threading:** no timers inside the scoped service (Blazor Server circuit
threading). `Generated` is raised as a state; settling back to `None`/`Active`
is the UI layer's job (`DrylAiScope` / `DrylAiStream`), so the one-shot reveal
plays once and then relaxes.

`Begin(...).Dispose()` calls `Clear(key)` (settles to `None`). `StreamAsync`
wraps `Begin` and guarantees `Clear` in `finally`.

### 2. `AiScope` context + `<DrylAiScope Key="...">`

`AiScope` is a small context object cascaded to descendants:

```csharp
public sealed class AiScope
{
    public string? Key { get; init; }
    public AiState State { get; internal set; }
}
```

`DrylAiScope` component:

- Parameters: `Key` (string), optional `State` (explicit override that bypasses
  the service), `ChildContent`.
- When `Key` is set and the service is resolvable, subscribes to `OnChanged`,
  reads `GetState(Key)`, calls `StateHasChanged` + updates the cascaded
  `AiScope.State`.
- Cascades the `AiScope` via `<CascadingValue Value="_scope" IsFixed="false">`.
- Unsubscribes from `OnChanged` in `Dispose`.
- The service is optional: resolve it via an injected `IServiceProvider`
  (`GetService<IDrylAiActivityService>()`, may be null) rather than `[Inject]`,
  so a scope used with an explicit `State` works even if `AddDrylComponents()`
  was never called.

### 3. Cascading consumption — `DrylAiAware` base class

A new base class so components opt in with one line instead of bespoke wiring:

```csharp
public abstract class DrylAiAware : ComponentBase
{
    [Parameter] public AiState Ai { get; set; } = AiState.None;
    [CascadingParameter] protected AiScope? AiScope { get; set; }

    /// Explicit Ai wins; otherwise inherit the surrounding scope.
    protected AiState EffectiveAi =>
        Ai != AiState.None ? Ai : (AiScope?.State ?? AiState.None);
}
```

Retrofit (this increment) — the curated set that realistically lives inside an
AI scope:

`DrylButton`, `DrylCard`, `DrylMessage`, `DrylChat`, `DrylInputText`,
`DrylTextarea`, `DrylAutocomplete`, `DrylSelect`.

Each: add `@inherits DrylAiAware`, remove its local `Ai` parameter declaration
(now inherited), and swap `Ai` → `EffectiveAi` in the `ai-aura` class logic.
Behavior is unchanged when no scope is present (explicit `Ai` still wins;
`EffectiveAi` collapses to `Ai`).

**Known limitation (documented, accepted for v1):** explicit `Ai="None"` cannot
opt a child *out* of an enclosing scope (it's indistinguishable from the default).
Opting a subtree out is a future enhancement (e.g. a nested `<DrylAiScope State="None">`).

The remaining ~39 AI-aware components keep working unchanged; converting them is
filed as a backlog ticket with the documented 2-line pattern above.

### 4. `<DrylAiStream Source="@tokens" Key="...">`

Declarative stream binder.

- Parameters: `Source` (`IAsyncEnumerable<string>`), optional `Key` (ties into the
  service so a surrounding `DrylAiScope` lights up too), `ChildContent`
  (`RenderFragment<AiStreamContext>` exposing `Text` + `State`), `OnCompleted`
  (`EventCallback<string>` with the full text), optional `SettleTo`
  (`AiState`, default `None`).
- On `Source` set / parameters change: cancels any in-flight stream, starts a new
  consume loop. Accumulates text, raises `StateHasChanged` per chunk
  (throttled to animation frames is a nice-to-have, not required for v1).
- Cleanup: holds a `CancellationTokenSource`, cancels + disposes it in `Dispose`
  / on restart. Guards interop-free so it is prerender-safe; no `setTimeout`
  without disposal (per project conventions / memory).
- After completion: sets `Generated`, then settles to `SettleTo`.

## Data flow

```
consumer code ── Begin/StreamAsync ──► IDrylAiActivityService
                                            │ OnChanged(key)
                                            ▼
                                      DrylAiScope (Key match)
                                            │ AiScope.State = …
                                            ▼ (cascade)
                              DrylButton / DrylCard / … (EffectiveAi)
                                            │
                                            ▼
                                  .ai-aura* classes render
```

`DrylAiStream` is an alternative entry point: it can drive the service by `Key`
(so scopes light up) *and* render the streamed text itself.

## Error handling

- `StreamAsync`: `try/finally`, always `Clear(key)`; `OperationCanceledException`
  from cancellation is swallowed (expected on restart/dispose); other exceptions
  propagate to the caller after the key is cleared.
- `DrylAiScope` / `DrylAiStream`: subscription + CTS disposed in `Dispose`;
  null-safe when the service isn't registered.

## Testing

- Service unit tests (xUnit, no Blazor): `Begin` → state transitions →
  `Dispose` clears; `OnChanged` fires with the right key; `StreamAsync` runs
  Thinking→Streaming→Generated and clears in `finally` on success, cancel, and
  throw.
- bUnit component tests (if the project already uses bUnit; otherwise manual):
  `DrylAiScope` cascades and updates on `OnChanged`; a retrofitted `DrylButton`
  inside a scope renders `ai-aura`; explicit `Ai` still overrides.
- Manual: a sample page (`samples/Pages/DemoAiActivity.razor`) showing a scope
  with a button + card + input lighting up together, and a `DrylAiStream` typing
  out tokens.

## Docs (mandatory, per CLAUDE.md §7)

- `CHANGELOG.md` → `[Unreleased] / Added`: `DrylAiActivityService`
  (`IDrylAiActivityService`), `DrylAiScope`, `DrylAiStream`, `DrylAiAware` base
  class; note the 8 retrofitted components under `Changed` if their public
  surface is affected (it isn't — `Ai` stays, just inherited).
- `README.md` → component table rows for `DrylAiScope`, `DrylAiStream`
  (Category: AI, AI mode: ✅, Status: ✅ Done).
- No `DESIGN_TOKENS.md` / `dryl.css` change (no new token).
- Backlog ticket (Trello) for retrofitting the remaining ~39 components.

## File inventory

New:
- `DRYL.Components/Ai/IDrylAiActivityService.cs`
- `DRYL.Components/Ai/DrylAiActivityService.cs`
- `DRYL.Components/Ai/IDrylAiOperation.cs` (+ internal impl, may live in the service file)
- `DRYL.Components/Ai/AiScope.cs`
- `DRYL.Components/Ai/DrylAiAware.cs`
- `DRYL.Components/Components/AI/DrylAiScope.razor`
- `DRYL.Components/Components/AI/DrylAiStream.razor` (+ `AiStreamContext`)
- `samples/.../DemoAiActivity.razor`

Edited:
- `DRYL.Components/Extensions/ServiceCollectionExtensions.cs` (register service)
- 8 curated components (`@inherits DrylAiAware`, `Ai` → `EffectiveAi`)
- `CHANGELOG.md`, `README.md`
