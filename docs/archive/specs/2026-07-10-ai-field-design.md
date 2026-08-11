# DrylAiField — AI affordance around any existing DRYL input

**Date:** 2026-07-10
**Package:** `DRYL.Components.Agents` (0.4.0 → 0.5.0; no core changes)
**Status:** Approved (text inputs only in v1; select support is a follow-up)

## Idea

A thin wrapper you lay around any existing DRYL input (`DrylInputText`,
`DrylTextarea`, `DrylInputMask`, even a raw `<input>`) that gives the field an
unobtrusive ✨ affordance. Empty field → the AI generates/completes the value;
text selected → only the selection is transformed (rephrase, translate,
shorten, change tone) — driven by a fixed `Instruction` or a free mini-prompt.
The result streams live back into the field while the shared ai-aura plays
around it; the user then accepts or rejects with one click / Esc.

For the developer it is **one line, zero wiring** — only the agent is passed
through; not a single existing component is touched:

```razor
<DrylAiField Agent="agent" Instruction="Formuliere professioneller">
    <DrylTextarea @bind-Value="mail.Body" />
</DrylAiField>
```

## Scope decisions (locked)

- **Text-like inputs only in v1** — the first `textarea` or text-like `input`
  inside the wrapper. `DrylSelect` (AI picks one of the existing options) is a
  separate mechanism and ships as a follow-up (Trello ticket).
- **DOM value bridge, not double binding.** The wrapper never takes a `Value`
  parameter; the inner input's `@bind-Value` keeps working because the bridge
  dispatches native `input` events.
- **Built-in UX** (overlay trigger + popover + accept/reject chips), not a
  headless API. A `ChildContent`-only headless mode is not in v1.
- **Context is explicit**: an optional `Context` string parameter the app
  fills; no automatic form scraping.

## Architecture

Three existing mechanisms plus one new small JS module:

### 1. Aura via `DrylAiScope` (existing)

The wrapper renders `<DrylAiScope State="_state">` around `ChildContent`. All
AI-aware inputs already consume the cascaded scope, so the inner field gets the
shared ai-aura (`Thinking` pulse → `Streaming` rotation → `Generated` wash →
settle to `None`) **without being touched**. `_state` mirrors the run's state
via `run.OnChange`. Non-AI-aware children (raw `<input>`) simply show no aura —
the value bridge still works.

The wrapper itself has **no `Ai` parameter** — it produces the state, it does
not consume one.

### 2. Agent run via `DrylAgentRunner` (existing)

Injects `DrylAgentRunner` (registered by `AddDrylAgents()`). Per invocation:

- Session: `await Agent.CreateSessionAsync(ct)` — fresh by default, exactly
  like `DrylAiCommandResolver`; an optional `SessionFactory` parameter lets
  apps reuse a session.
- `runner.Start(Agent, session, prompt)` → `DrylAgentRun`; the component
  consumes `run.TextStream` (stable reference, per the established rule) and
  forwards deltas to the DOM bridge.
- Errors follow the package convention: `run.Error != null` + `AiState.None`.

### 3. DOM value bridge (new JS, Agents package)

`wwwroot/js/dryl-aifield.js` — an **ES module in the Agents package**, lazily
imported via `IJSRuntime.InvokeAsync<IJSObjectReference>("import",
"./_content/DRYL.Components.Agents/js/dryl-aifield.js")`. No new `<script>`
tag for consumers, no change to the core `dryl.js`, no core version bump.

Functions (root = the wrapper's field region, excluding the popover):

| Function | Does |
| -------- | ---- |
| `snapshot(root)` | finds the first `textarea` / text-like `input`, returns `{ found, value, selStart, selEnd }` |
| `write(root, text)` | sets `el.value = text`, dispatches bubbling `input` event → inner `@bind-Value` updates itself |
| `setBusy(root, busy)` | toggles `readOnly` during the stream |
| `focusField(root)` | returns focus to the field after accept/reject |

- Selection transform: C# composes `prefix + streamed + suffix` and calls
  `write` with the full value each flush.
- Tokens are buffered C#-side and flushed at most every ~50 ms (Blazor-Server
  friendly), plus a final flush.
- Prerender-safe per CONVENTIONS §7: no JS before first interactive render,
  `IAsyncDisposable` guarded by `_attached`.
- Element discovery matches `textarea, input:not([type]), input[type=text|search|email|url|tel]`,
  skips `disabled` elements and anything inside the wrapper's own popover
  (marked `data-aifield-ui`).

## Public API

```csharp
[Parameter, EditorRequired] public AIAgent Agent { get; set; }

/// Fixed instruction ("Formuliere professioneller"). Null ⇒ the trigger opens the mini-prompt.
[Parameter] public string? Instruction { get; set; }

/// Forces the mini-prompt even when Instruction is set (Instruction becomes the prefill).
[Parameter] public bool ShowPrompt { get; set; }

/// Extra context sent with the prompt (e.g. the mail body when generating a subject).
[Parameter] public string? Context { get; set; }

/// Optional session reuse; default = fresh session per invocation.
[Parameter] public Func<CancellationToken, Task<AgentSession>>? SessionFactory { get; set; }

[Parameter] public string TriggerLabel { get; set; } = "Mit AI ausfüllen";   // tooltip + aria-label
[Parameter] public string PromptPlaceholder { get; set; } = "Was soll die AI tun?";
[Parameter] public string AcceptLabel { get; set; } = "Übernehmen";
[Parameter] public string RejectLabel { get; set; } = "Verwerfen";

[Parameter] public bool Disabled { get; set; }
[Parameter] public string? Class { get; set; }            // merged (CONVENTIONS §2)
[Parameter] public RenderFragment? ChildContent { get; set; }

[Parameter] public EventCallback<string> OnAccepted { get; set; }   // final field value
[Parameter] public EventCallback OnRejected { get; set; }
[Parameter] public EventCallback<DrylRunError> OnError { get; set; }
```

Naming per CONVENTIONS: action events `On<Verb>`, booleans plain adjectives,
merged `Class`, `EditorRequired` for the one required value.

## Interaction flow

State machine: `Idle → (Prompting) → Running → Review → Idle`.

1. **Idle** — ✨ trigger (`DrylIcon`, icon-only ⇒ wrapped in `DrylTooltip`
   with `TriggerLabel`, same text as `aria-label`, rule 2.11) floats in the
   top-right corner of the field region. It fades/scales in on hover or
   focus-within (`--dur-fast` / `--ease-out`) and stays visible while
   Running/Review. Hidden entirely when `Disabled`.
2. **Prompting** — click: with `Instruction` and `!ShowPrompt` → straight to
   Running. Otherwise a small popover (wrapped in `DrylPresence`; glass panel,
   `data-aifield-ui`) opens with a `DrylInputText` (placeholder
   `PromptPlaceholder`, prefilled with `Instruction` when `ShowPrompt`) and a
   send button. Enter starts, Esc closes.
3. **Running** — JS `snapshot` captures value + selection; the prompt is built
   (see template below); `setBusy(true)`; the run starts. Aura shows
   `Thinking` until the first token, then `Streaming` while deltas flush into
   the field. Clicking the trigger (now a stop affordance) or Esc cancels
   (CTS) → restore snapshot, settle to `None`.
4. **Review** — stream complete: aura shows `Generated` (one-shot wash),
   `setBusy(false)`, and a chip row (✓ `AcceptLabel` / ✗ `RejectLabel`,
   `DrylPresence`-animated) appears at the field edge. ✓ keeps the value and
   fires `OnAccepted(newValue)`; ✗ or **Esc** restores the snapshot via
   `write` and fires `OnRejected`. Either way the aura settles to `None` and
   focus returns to the field.
5. **Error** — `run.Error != null`: restore the snapshot automatically, show a
   compact inline error hint (dismissable, `DrylPresence`), fire
   `OnError(run.Error)`, settle to `None`.

Status changes (running / ready for review / error) are announced through an
`aria-live="polite"` region, mirroring `DrylAiIndicator`.

Re-entrancy: one run at a time per field; starting while Running cancels the
previous run first.

## Prompt template

Built in English (better model adherence); user-facing strings stay German.

```
{Instruction | mini-prompt text}

{Context, when set:}
Additional context:
{Context}

{when the field has a value:}
Current field value:
"""
{value}
"""

{when a selection exists:}
Selected portion (transform ONLY this):
"""
{selection}
"""

Respond with ONLY the replacement text for {the selection | the field} —
no quotes, no markdown fences, no explanation.
```

A defensive post-trim strips one leading/trailing pair of quotes or a fenced
code block if the model adds them anyway.

## Motion (rule 2.12)

- Trigger: fade + scale-in `--dur-fast`/`--ease-out`; hover lift.
- Popover & chip row: `DrylPresence` enter **and** exit.
- Aura: exclusively the shared `.ai-aura*` primitives via `DrylAiScope` — no
  new AI visuals, colors, or durations.
- `prefers-reduced-motion` honoured (primitives already do; custom CSS in
  `DrylAiField.razor.css` mirrors it). All values are tokens.

## Testing

bunit tests in `tests/DRYL.Components.Tests` (JS module mocked via bunit's
`JSInterop`):

- Prompt building: instruction/context/value/selection combinations, exact
  template output.
- State flow: drive with `runner.Replay(updates)` — Idle→Running→Review, aura
  states observed via the cascaded scope.
- Accept: value kept, `OnAccepted` fires with final text, state settles.
- Reject/Esc/cancel: `write` called with the snapshot value, `OnRejected` fires.
- Error path: restore + `OnError`, `AiState.None`.
- Trigger accessibility: `aria-label` present, tooltip text matches.
- Prerender safety: dispose before first render does not throw.

## Docs & packaging

- `DRYL.Components.Agents.csproj` → **0.5.0**; `PackageReleaseNotes` updated.
- `CHANGELOG.md` — Added entry under `[Unreleased]` (Agents section style).
- `PACKAGE.md` — new "DrylAiField" section with the one-liner example.
- `ComponentCatalog` in DRYL.Website + demo page in the Agents section using
  the established simulation pattern (`SimScenarios`) so the demo runs without
  an API key.
- Follow-up ticket (Trello): `DrylAiField` select support (AI picks an option;
  options list goes into the prompt).
- Note: `publish.yml` does not publish the Agents package — release remains
  the existing manual flow.
