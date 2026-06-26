# DRYL.Components.Agents — Microsoft Agent Framework Integration — Design

**Date:** 2026-06-25
**Status:** Approved for planning

## Goal

Add first-class support for the Microsoft Agent Framework (`Microsoft.Agents.AI`)
to DRYL, delivered as a **separate companion package** `DRYL.Components.Agents`.
The package takes real work off the developer's hands across three subsystems:

1. **Agent run → AiState bridge** — running an agent automatically drives the shared
   `AiState` vocabulary (Thinking / Streaming / Generated) across the UI, and surfaces
   tool calls plug-and-play — the developer never sets `Ai="…"` by hand.
2. **Structured streaming UI** — `DrylAiGenerate<T>` streams a typed object from the
   model and renders *partial snapshots* progressively, character-by-character, the
   way Apple's Foundation Models guided generation feels.
3. **Ready-made UI tool functions** — four `AIFunction` tools the developer simply hands
   to the agent, backed by `DrylDialog` for human-in-the-loop questions: single choice,
   multi choice, permission, and free text.

The package builds *on top of* the existing AI primitives (`AiState`, `DrylAiStream`,
`IDrylAiActivityService`, `DrylToolCall`, `IDrylDialogService`) rather than duplicating
them. It is the bridge whose absence the core deliberately documents today
("DRYL stays dependency-free: you bring the token stream").

## Non-goals (YAGNI)

- **No change to rule 2.8 for the core.** `DRYL.Components` stays dependency-free
  (Markdig only). The LLM SDK lives exclusively in the new package.
- **No agent construction, provider selection, or key management.** The developer
  builds their own `AIAgent` (with their provider/credentials) and hands it in.
- **No chat-history persistence**, no multi-agent orchestration.
- **No attribute-based auto-mapping** of objects to components (a possible later spec).
  Phase 1 uses a developer-supplied `RenderFragment<T>`.
- **No new `AiState` values, no new AI animation/color/token, no `dryl.css` change.**
  We reuse `.ai-aura*` and the `AiState` enum (rules 2.10, 2.1).

## Platform constraint (documented usage rule)

All three subsystems run meaningfully only in **interactive Blazor (Server or WASM)**
with a live circuit. The tool functions must show a dialog on the circuit and *await*
its `DialogResult` while the agent run is in flight. Therefore **the agent run must
execute in the same DI scope (circuit) as the UI**, because `IDrylDialogService` is
scoped per circuit. An agent run started server-side outside the circuit cannot ask the
user back. This is a documented usage rule, not a code guard.

## Architecture

```
DRYL.Components            (core, unchanged, zero deps + Markdig)
└─ knows NO agent SDK

DRYL.Components.Agents     (NEW package)
├─ PackageReference → DRYL.Components
├─ PackageReference → Microsoft.Agents.AI  (+ Microsoft.Extensions.AI)
├─ Agents/        Subsystem 1: run → AiState bridge + tool-call surfacing
├─ Generation/    Subsystem 2: DrylAiGenerate<T> + PartialJsonReader
├─ Tools/         Subsystem 3: 4 DrylDialog-backed tool functions + 3 dialogs
└─ Extensions/    AddDrylAgents() — DI registration
```

- Multi-target `net8.0;net9.0;net10.0` like the core, **provided** the installed
  `Microsoft.Agents.AI` (v1.10.0 per prior work) supports all three TFMs; if it drops
  net8, the floor rises accordingly. Pin during planning against the installed package.
- Own NuGet identity and **independent version**, starting at `0.1.0` (experimental).
  Deliberately decoupled from core `1.0.0` so the agent SDK can mature without breaking
  core SemVer.
- `AddDrylAgents()` extension mirrors `AddDrylComponents()`.
- Exact Agent Framework API names (`AgentSession` not `AgentThread`, `RunStreamingAsync`,
  response-format/JSON-schema setup) are pinned during planning against the installed
  v1.10.0; this spec uses indicative names.

---

## Subsystem 1 — Agent run, automatic AiState & tool calls

### `DrylAgentRunner` (scoped service, registered via `AddDrylAgents()`)

Creates and owns agent runs. Produces an observable `DrylAgentRun` handle.

```csharp
public sealed class DrylAgentRunner
{
    DrylAgentRun Start(AIAgent agent, AgentSession session, string message,
                       string? aiKey = null, CancellationToken ct = default);

    // Subsystem 2 bridge: typed structured streaming (sets the JSON schema for T).
    IAsyncEnumerable<string> GenerateStreamingAsync<T>(
        AIAgent agent, AgentSession session, string prompt,
        string? aiKey = null, CancellationToken ct = default);
}
```

### `DrylAgentRun` (observable handle, `IAsyncDisposable`)

```csharp
public sealed class DrylAgentRun
{
    public AiState State { get; }                          // driven automatically
    public string Text { get; }                            // accumulated answer text
    public IReadOnlyList<DrylToolInvocation> ToolCalls { get; }
    public event Action? OnChange;
    public IAsyncEnumerable<string> TextStream { get; }    // for DrylAiStream/DrylMarkdown
}
```

### Automatic AiState logic

Fed from the agent framework's streaming update chunks (`Microsoft.Extensions.AI`
content types: reasoning / text / function-call / function-result):

| What happens in the run                          | AiState     |
| ------------------------------------------------ | ----------- |
| Run started, no output yet                       | `Thinking`  |
| Model emits **reasoning** content (real thinking)| `Thinking`  |
| **Tool call** executing                          | `Thinking`  |
| **Text** deltas arriving                         | `Streaming` |
| Run completed                                    | `Generated` → settles |

The run pushes this state to the existing `IDrylAiActivityService` under the optional
`aiKey`. The developer wraps a region in `<DrylAiScope Key="chat">` once; every AI-aware
component inside switches to Thinking/Streaming/Generated in lockstep — **no manual
`Ai="…"` anywhere.**

**Honest limitation:** a distinct "really thinking" phase is only visible if the
provider emits reasoning content (not all do). Without it, `Thinking` is still reliable
from start-until-first-token and during every tool call — just without the finer
reasoning-vs-waiting distinction.

### Tool-call surfacing — reuse, do not duplicate

`DrylToolCall` already exists in the **core** as a purely presentational component
(`ToolName`, `Arguments`, `Result`, `Error`, `State`). It stays in the core and is
reused as-is. The Agents package supplies only data + wiring:

- **`DrylToolInvocation`** (model in Agents package) — fields mapping 1:1 onto
  `DrylToolCall` params: `ToolName`, `Arguments` (JSON), `Result` (JSON), `Error`,
  and an auto-computed `State` (running → `Thinking`, done → `Generated`, error →
  `None` + `Error`).
- **`DrylAgentToolCalls`** (thin wrapper component, ~10 lines) — takes the `DrylAgentRun`,
  loops `run.ToolCalls`, renders one **core `DrylToolCall`** per item:

```razor
@foreach (var t in Run.ToolCalls)
{
    <DrylToolCall ToolName="@t.ToolName" Arguments="@t.Arguments"
                  Result="@t.Result" Error="@t.Error" State="@t.State" />
}
```

The package's value-add is maintaining the `DrylToolInvocation` list with correct live
`State` from raw update chunks. The *visual* comes unchanged from the core. By default
`DrylAgentToolCalls` shows **all** calls as a trace (core `DrylToolCall` is built to
stack); an optional `ActiveOnly` flag shows only the running one.

### Consumer experience

```razor
<DrylAiScope Key="chat">
  <DrylAiStream Source="@_run.TextStream" Key="chat">
    <DrylMarkdown Content="@context.Text" Ai="@context.State" />
  </DrylAiStream>
  <DrylAgentToolCalls Run="@_run" />
</DrylAiScope>

@code {
    _run = Runner.Start(_agent, _session, userMsg, aiKey: "chat");
}
```

---

## Subsystem 2 — `DrylAiGenerate<T>` (structured streaming → UI)

The most technically novel part. Two building blocks.

### 1. `PartialJsonReader` — the snapshot engine

The model is instructed to emit JSON conforming to `T`'s schema (response format =
JSON schema derived from `T`, provided by `Microsoft.Extensions.AI`). As tokens arrive
we accumulate the raw JSON buffer and attempt a **tolerant** deserialization on **every**
chunk:

- Track the bracket / brace / quote stack, virtually close open structures, then
  deserialize to `T?` with lenient options → a **partial snapshot** (completed fields
  set, not-yet-streamed fields `null`/default).
- **Show partial values live:** the currently-open string is surfaced with its
  content-so-far (not only at the closing `"`), so titles and text grow character by
  character — full streaming look & feel, like Apple.
- This does not exist out of the box → we build it **test-first (TDD)**, because all the
  edge cases live here (truncated strings, half-written numbers, arrays with a half last
  element).
- **On a parse failure, hold the last good snapshot** — never jump back to `null`/flicker.

### 2. `DrylAiGenerate<T>` (Agents package)

Parallel to `DrylAiStream` so it feels familiar (Source-based, not "owns the agent call"
— keeps the component pure, SDK-free, unit-testable; schema setup lives in the runner).

```razor
<DrylAiGenerate T="Recipe" Source="@_jsonStream" Key="recipe">
  <ChildContent Context="snap">
    <DrylCard Ai="@snap.State">
      <h3>@snap.Value?.Title</h3>
      @foreach (var step in snap.Value?.Steps ?? []) { <li>@step</li> }
    </DrylCard>
  </ChildContent>
</DrylAiGenerate>
```

- `Source` is `IAsyncEnumerable<string>` of raw JSON tokens — deliberately mirrors
  `DrylAiStream`.
- `ChildContent` receives a `GenerationSnapshot<T>`: `Value` (partial `T?`), `State`
  (Thinking→Streaming→Generated automatically), `IsComplete`.
- Drives the same `IDrylAiActivityService` key → the surrounding `DrylAiScope` lights up
  as everywhere else.

```csharp
public sealed class GenerationSnapshot<T>
{
    public T? Value { get; }
    public AiState State { get; }
    public bool IsComplete { get; }
}
```

### Bridge to Subsystem 1

`DrylAgentRunner.GenerateStreamingAsync<T>(...)` sets the schema and returns the raw JSON
token stream, ready to drop into `<DrylAiGenerate T Source="@…">`.

### Risk

`PartialJsonReader` is the only genuinely new and tricky piece; if unstable the UI
flickers. Mitigation: TDD with a corpus test suite of real partial JSON fragments;
hold-last-good-snapshot on parse error.

---

## Subsystem 3 — DrylDialog-backed tool functions (human-in-the-loop)

Four ready-made tools the developer hands to the agent. Each is an `AIFunction`
(`Microsoft.Extensions.AI`) that internally calls the existing `IDrylDialogService` and
**awaits** the `DialogResult`.

### Provisioning — a factory bound to the circuit

```csharp
var uiTools = DrylUiTools.Create(DialogService);  // scoped to this circuit
var agent = new ChatClientAgent(chatClient, new() {
    Tools = uiTools.All            // or pick individually
});
```

`DrylUiTools` exposes each tool individually (`AskChoice`, `AskMultiChoice`,
`RequestPermission`, `AskText`) **and** an `All` convenience collection (max
"takes work off your hands"; individual selection stays possible).

### The four tools

| Tool                | Signature the model sees           | UI                                                  | Returns to model      |
| ------------------- | ---------------------------------- | --------------------------------------------------- | --------------------- |
| **AskChoice**       | `question, options[], recommended?`| new `DrylAskChoiceDialog` (radio list, recommended marked as badge) | chosen option (string) |
| **AskMultiChoice**  | `question, options[], recommended[]?`| new `DrylAskMultiChoiceDialog` (checkbox list, recommendations pre-checked) | chosen options (string[]) |
| **RequestPermission** | `action, details?`               | existing `DrylConfirmDialog` (reused)               | allowed: bool         |
| **AskText**         | `question, placeholder?`           | new `DrylAskTextDialog` (DrylInputText)             | entered text (string) |

### Dialog location

The three new dialogs (`DrylAskChoiceDialog`, `DrylAskMultiChoiceDialog`,
`DrylAskTextDialog`) live **in the Agents package** — they are specific to the
agent-question flow. They are composed from core components (`DrylInputText`,
`DrylRadio`/`DrylCheckbox`, etc.) and shown via the existing `ShowAsync<TDialog>`
pipeline. No new dialog system. `RequestPermission` reuses the core `DrylConfirmDialog`.

### Circuit dispatch & cancellation (the hard part)

- The tool function runs on the agent-run task, **not** necessarily on the renderer's
  `SynchronizationContext`. The call into `IDrylDialogService` must be dispatched onto
  the circuit via `InvokeAsync`, or Blazor throws.
- The tool task awaits the `DialogResult`. If the user dismisses the dialog
  (Cancel/Escape) → the tool returns a defined "user declined" / "user cancelled" value
  to the model (no throw that kills the run). If the whole agent run is cancelled → the
  open dialog is closed. Wired via the run's `CancellationToken`.
- Requires the agent run in the same circuit scope (see platform constraint).

---

## DI registration — `AddDrylAgents()`

Mirrors `AddDrylComponents()`. Registers `DrylAgentRunner` (scoped). `DrylUiTools` is
created per-circuit via its factory (it captures the scoped `IDrylDialogService`), not a
DI singleton.

---

## Testing (bUnit + xUnit, matching the existing suite)

- **`PartialJsonReader`** — pure unit tests with a corpus of partial JSON fragments.
  Highest priority, highest risk. Built test-first.
- **`DrylAgentRunner`** — against a fake `AIAgent`/fake stream: AiState transitions
  Thinking→Streaming→Generated, tool-call mapping.
- **Tool functions** — against a fake `IDrylDialogService`: correct return on
  Confirm/Cancel for all four tools.
- **Components** — bUnit render tests for `DrylAiGenerate<T>` snapshots and
  `DrylAgentToolCalls`.

## Phases (each independently buildable & testable)

1. **Foundation** — `DRYL.Components.Agents` project, `Microsoft.Agents.AI` reference,
   `AddDrylAgents()`, wired into the solution + CI. Smoke test: package builds, DI
   registers.
2. **Subsystem 1** — `DrylAgentRunner` + `DrylAgentRun` + `DrylToolInvocation` +
   `DrylAgentToolCalls`. Automatic AiState.
3. **Subsystem 2** — `PartialJsonReader` (TDD first, isolated) → `DrylAiGenerate<T>` +
   `GenerationSnapshot<T>` + typed `GenerateStreamingAsync<T>`.
4. **Subsystem 3** — the four tool functions, three new dialogs,
   `DrylUiTools.Create(...)` + `All`, circuit dispatch / cancellation.

## Documentation (CLAUDE.md §7 — mandatory)

- `CHANGELOG.md` → `[Unreleased] / Added`, one entry per public type.
- `README.md` → component table: rows for `DrylAiGenerate`, `DrylAgentToolCalls`, the
  tool dialogs; make clear they come from the **Agents package**.
- New `DRYL.Components.Agents/PACKAGE.md` + own NuGet identity (own `Version`, starts
  `0.1.0`, decoupled from core `1.0.0`).
- Sample pages under `samples/` for each subsystem (chat with auto-AiState, generative
  recipe card, HITL tool demo).
