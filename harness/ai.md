# AI Rules

Binding rules for AI mode — DRYL's distinguishing feature. Visual primitives:
[`design.md`](design.md). Component-level code rules: [`code.md`](code.md).
Announcement/accessibility baseline for AI activity (`aria-live="polite"`):
[`uiux.md`](uiux.md) `UX-04`. Token reference: [`tokens.md`](tokens.md).
Component anatomy: [`patterns.md`](patterns.md).

AI is treated as a first-class state of the UI, not a bolt-on spinner: any
AI-aware component accepts an `AiState` parameter that drives one shared
visual vocabulary — rotating gradient border, breathing glow, one-shot reveal
— so a user can feel where the AI is at work across the entire library
without ever reading a label. This file is expected to grow: as AI-aware
surfaces multiply, so do the rules that keep them speaking the same visual
language.

Every rule has a stable ID. IDs are never reused: if a rule is dropped, its
number is burned. Gaps between number blocks are intentional — they leave room
for later rules without renumbering.

**Status** — `binding` blocks the merge · `default` needs a reason in the PR ·
`guidance` is a recommendation.
**Enforced** — how compliance is established: `script`, `grep` or `review`.

---

### AI-01 — One shared `AiState` enum

Status: **binding** | Enforced: **grep**

DRYL has **one** AI vocabulary. Every AI-aware component reuses the shared
`AiState` enum (`None / Active / Thinking / Streaming / Generated`, defined in
`code/DRYL.Components/AiState.cs`). Do not invent a per-component state enum
— named anti-examples are `Loading`, `Generating`, `AiBusy`, `ChatLoadingState`.
No component invents its own AI vocabulary.

Check: `rg -n 'enum \w*(Ai|Loading|Generating)\w*State' code/` — currently
**green**: the only match is `code/DRYL.Components/AiState.cs:14: public enum
AiState`. No `ChatLoadingState`, `AiBusy`, or other per-component AI state
enum exists in the codebase today.

### AI-02 — The visual comes from the existing primitives

Status: **binding** | Enforced: **review**

The visual is delivered by the existing CSS primitives in `dryl.css`:
`.ai-aura` + `.ai-aura-ring` + `.ai-aura-comet` + `.ai-aura-glow` + (optional)
`.ai-aura-wash`, plus `.ai-indicator` for status pills. A new AI-aware
component composes these; it does not draw its own gradient border or glow.

Check: all six selectors exist in
`code/DRYL.Components/wwwroot/dryl.css` — confirmed via
`rg -n '\.ai-aura\b|\.ai-aura-ring|\.ai-aura-comet|\.ai-aura-glow|\.ai-aura-wash|\.ai-indicator\b' code/DRYL.Components/wwwroot/dryl.css`,
which currently returns matches for all six (`.ai-aura` base rule, `.ai-aura-ring`,
`.ai-aura-comet`, `.ai-aura-glow`, `.ai-aura-wash`, `.ai-indicator` with its
`.ai-indicator-ico` and `is-thinking`/`is-streaming` modifiers). That only
proves the primitives exist — it cannot prove a *new* component reused them
instead of hand-rolling a look-alike, so this stays review-enforced: a
reviewer confirms a new AI-aware surface's markup/CSS references these
classes rather than defining new ones.

### AI-03 — The opt-in parameter is always named `Ai` and defaults to `AiState.None`

Status: **binding** | Enforced: **grep** + **review**

The **opt-in** parameter — the one that turns AI styling on for an otherwise
ordinary component — is always named `Ai` (of type `AiState`) and defaults to
`AiState.None`. AI mode must be **off by default** so existing consumers see
no change.

Not every `AiState` parameter is an opt-in. The binding test asks what the
parameter *is*, and is answered by reading the component — not by the category
a component claims for itself:

> **Is the parameter a switch that turns AI styling on for a component that
> would otherwise render as an ordinary one?**
> **Yes** → it is an opt-in. It is named `Ai` and defaults to `AiState.None`.
> **No** → the value is the component's own content, its settle state, or a
> broadcast override. It carries its own descriptive name, and its default is
> that component's decision.

Where the parameter is not a switch, this rule was never about it. That is a
narrowing of the rule's subject, **not** an exemption for a category of
component: "AI-native" is self-declared and would grow by itself, while "is
this a switch" is answered from the component's own code. The reasoning is in
`ideas/I1 AI parameter naming for AI-native components.md`.

**On an earlier wording of this test.** From 2026-08-11 the test read "does the
component still render something meaningful with `AiState.None`?", taken from
`I1`. It was replaced the same day because it decides nothing: *all* eight
`AiState` parameters in the library answer yes. `DrylAiIndicator` renders an
idle "AI" pill with `None`, and `DrylAiStream`, `DrylAiScope`, `DrylAiGenerate`
and `DrylAiBuild` all render their `ChildContent` regardless of the parameter.
`I1` had asserted the opposite about `DrylAiIndicator` and it went unchecked.
The conclusions did not change — the three renames were right — but the reason
given for them was not the one that actually separates the cases.

Check: `grep -rn '\[Parameter\] public AiState' code/DRYL.Components code/DRYL.Components.Agents`
— currently **47 hits and no violations**. The grep half establishes the naming and the default;
"is this an opt-in" is not greppable, so the legitimate non-opt-ins are named
here explicitly, the way `CODE-01` names its own. A named list is verifiable;
a category is not.

**Five legitimate non-opt-ins** — none of them is a switch, so each keeps its
own name and default:

- `DrylAiIndicator.razor` (`State`, default `AiState.Active`) — the parameter
  **is the component's content**: the pill's whole job is to display an AI
  state, and `State` selects which one, down to the idle "AI" label it shows
  for `None`. Renaming it `Ai` and defaulting it to `None` would say "AI
  styling off" about a component that is nothing but that display.
- `DrylAiStream.razor`, `DrylAiGenerate.razor`, `DrylAiBuild.razor`
  (`SettleTo`, default `AiState.None`) — the state to settle to *after* the
  `AiState.Generated` reveal. The live state comes from the stream itself, so
  this parameter never turns anything on; it says where to land.
- `DrylAiScope.razor` (`AiState? State`, no default) — a broadcast override for
  descendants rather than styling for itself. `null` means "follow
  `IDrylAiActivityService`", `None` means "actively force AI off"; they are two
  different things, and a default of `AiState.None` would collapse them.

**Three obsolete aliases, not violations** — `DrylToolCall.razor`,
`DrylToolCallGroup.razor` and `DrylCanvas.razor` were the rule's three genuine
violations and were renamed on 2026-08-11. Each is an ordinary component with a
switch bolted on: a card, a summary row and an artifact tree that render
identically without AI, and whose parameter only adds the aura, the status
vocabulary or the build line. Each is therefore an opt-in and is now called
`Ai`. The rename is staged rather than immediate: `State` remains as an `[Obsolete]` property delegating to `Ai`
until the next planned `3.0.0` (`REL-01` in [`releasing.md`](releasing.md)), so
the grep still finds three `State` declarations. A Razor call site that sets one
compiles with `CS0618` naming the replacement.

The remaining hits — the opt-in pattern on ordinary components, e.g.
`DrylInputText.razor`, `DrylTable.razor`, `DrylAlert.razor` — are clean. The
three renamed components' specs are `specs/E3 AI/F1 DrylToolCall.md`,
`F2 DrylToolCallGroup.md` and `F3 DrylCanvas/`.

The review half: every **new** `AiState` parameter is read against the switch
test before merge. One that is a switch and is not named `Ai` blocks the merge;
one that is not a switch is named for what it is, and the reason is stated in
its spec — in the same terms as the five above: content, settle state, or
broadcast override.

### AI-04 — Never invent a new AI animation, color, gradient or duration

Status: **binding** | Enforced: **review**

If you think you need a new AI animation, color, gradient, or duration,
propose adding it to `dryl.css` and ask the maintainer — same bar as
`DESIGN-03`. The five `AiState` values map to the existing `.ai-aura*` and
`.ai-indicator` primitives (`AI-02`); do not extend that mapping with a
one-off.

Check: any new AI-specific value exists in `dryl.css` and is documented in
[`tokens.md`](tokens.md) before merge (reviewer confirms both — same gate as
`DESIGN-03`).

### AI-05 — Components that cannot host AI mode do not get the parameter

Status: **binding** | Enforced: **review**

Components that semantically can't host AI mode (e.g. `DrylBadge`,
`DrylToggle`) do not get an `Ai` parameter. Don't add it "just in case".

Check: `rg -n '\[Parameter\] public AiState' code/DRYL.Components/Components/Data/DrylBadge.razor code/DRYL.Components/Components/Inputs/DrylToggle.razor`
— currently returns **nothing**, confirming neither carries an `Ai`
parameter today. The command only proves the two named precedents are still
clean; it cannot prove a *future* component was deliberately withheld rather
than simply not yet written, so new components are still a reviewer call per
`CODE-21`'s "AI mode" clarifying question.

### AI-06 — An aura runs only while the AI is actually working there

Status: **binding** | Enforced: **review**

`Active` / `Thinking` / `Streaming` are live states — a surface left in one of
them animates forever for nothing. Never set an AI state decoratively, and
never leave one behind when the work ends.

Check: reviewer confirms every path that sets `Ai`/`State` to `Active`,
`Thinking`, or `Streaming` has a corresponding path that drops it back to
`None` (or hands off to `Generated`, see `AI-07`) once the underlying work
finishes — no automated scan documented yet, since "is this AI actually still
working" is a runtime/business-logic question a grep cannot answer.

### AI-07 — `Generated` is a one-shot; `AuraLifecycle` removes it

Status: **binding** | Enforced: **review**

`Generated` is a one-shot: it plays, holds, and `AuraLifecycle` then takes it
off the surface by itself, so a host announces "done" and is finished.

Check: `code/DRYL.Components/Ai/AuraLifecycle.cs` special-cases
`AiState.Generated` in its `Sync` method — on entering `Generated` it starts
`RetireGeneratedAsync`, which schedules the aura's own removal after
`generatedHoldMs` (default 1900ms) without the host having to hand it back to
`None`. Confirmed present in the current file (`Sync(AiState effective, ...)`,
`RetireGeneratedAsync`). Reviewer confirms a new host composes `AuraLifecycle`
(via `Sync` from `OnParametersSet`) rather than managing the `Generated`
one-shot by hand — no automated scan documented yet, since a hand-rolled
"set `Generated`, then set `None` after a `Task.Delay`" is not reliably
distinguishable from `AuraLifecycle` usage by grep.

---
