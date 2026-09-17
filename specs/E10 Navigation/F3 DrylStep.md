# DrylStep

## Meta
- **State:** Implemented
- **Source:** code/DRYL.Components/Components/Navigation/DrylStep.razor

## User Story

As a Blazor developer, I want to declare each wizard step with its content,
progress state and optional AI activity, so that its parent can display a
consistent header and show the body when that step is selected.

## Description

`DrylStep` is a declaration inside `DrylStepper`. It has no DOM or stylesheet
of its own. Its parent displays its header and, while selected, its body.
Use a stable, unique `Id` when binding the parent's `ActiveStep`.

The AI state belongs to this step's work, so an active or inactive header can
show its activity independently of the current wizard selection. AI styling
does not select, disable or complete a step. Header motion, body rendering and
the I13 forced-colors/reduced-motion targets belong to
[`F2 DrylStepper.md`](F2%20DrylStepper.md).

## Public API

Namespace: `DRYL.Components`.

| Member | Type | Default | Meaning |
|---|---|---|---|
| `Id` | `string?` | `null` | Stable identifier used by the parent's `ActiveStep`. An internal identifier is generated when omitted or empty. |
| `Label` | `string?` | `null` | Primary header text. |
| `Description` | `string?` | `null` | Secondary header text. |
| `Icon` | `string?` | `null` | Header indicator icon when completion/error state does not supply one. |
| `StepState` | `StepState` | `StepState.Pending` | Progress appearance; `StepState.Active` does not override parent selection. |
| `Disabled` | `bool` | `false` | Prevents activation through the header. |
| `Ai` | `AiState` | `AiState.None` | This step's explicit AI activity, displayed on its header. |
| `Aura` | `AiAura?` | `null` | Aura variant; null inherits the surrounding scope's variant, then falls back to `AiAura.Comet`. |
| `ChildContent` | `RenderFragment?` | `null` | Body displayed by the parent when this declaration is active. |

`Dispose()` implements `IDisposable`. There are no callbacks, `Class`, `Style`
or unmatched-attribute parameters. Cascading `Parent` and `AiScopeContext`,
`InternalId`, `EffectiveAura`, `GenTick` and `AuraFx` are implementation members,
not consumer parameters. Exact enum members and the parent boundary are recorded
in [`_Api.md`](_Api.md).

## Acceptance Criteria

### Declaration and identity

- `DrylStep` renders no DOM of its own.
- A declaration outside a cascading `DrylStepper` throws
  `InvalidOperationException` with message `DrylStep must be placed inside a
  DrylStepper.`
- A nonempty `Id` supplied at initialization becomes the identifier used by
  `ActiveStep`.
- An omitted or empty `Id` causes an internal identifier to be generated for
  that declaration instance.
- Changing `Id` after initialization does not change the registered identifier.
- The declaration registers once with its parent during initialization.
- Disposal unregisters the declaration from its parent.
- Disposal cancels the declaration's pending aura retirement/exit work
  (`CODE-05`).

### Content and progress

- `StepState` defaults to `StepState.Pending`.
- `Disabled` defaults to `false`.
- The parent receives this declaration's `Label` for its header.
- The parent receives this declaration's `Description` for its secondary text.
- The parent receives this declaration's `Icon` for the non-completed,
  non-error indicator.
- The parent receives this declaration's `ChildContent` for its selected body.
- Changing `StepState` requests a parent refresh.
- Changing `Disabled` requests a parent refresh.
- Changing `Label` requests a parent refresh.
- Changing `Description` requests a parent refresh.
- `StepState.Completed` does not prevent selection.
- `StepState.Error` does not prevent selection.
- `StepState.Active` does not force selection.

### AI state and lifecycle

- `Ai` defaults to `AiState.None` (`AI-03`).
- `Ai` uses the shared `AiState` vocabulary (`AI-01`).
- `Aura` defaults to `null`.
- A supplied `Aura` overrides a surrounding scope's aura variant.
- An unset `Aura` uses the surrounding scope's non-null aura variant.
- With neither an explicit nor a scoped variant, the header uses
  `AiAura.Comet`.
- A surrounding scope's AI state does not replace this component's `Ai` value;
  only the aura variant is inherited by the current implementation.
- A change to `Ai` requests a parent refresh.
- Non-`None` AI activity is rendered through the shared aura primitives on this
  step's header (`AI-02`).
- AI activity is independent of whether the step is selected.
- Changing AI activity does not change the step's progress state.
- On leaving live AI activity for `None`, the aura remains present for its
  shared graceful exit.
- Entering live AI activity during an exit cancels that pending exit.
- Entering `AiState.Generated` plays the shared one-shot reveal.
- `AiState.Generated` retires automatically through `AuraLifecycle`
  (`AI-07`).
- Re-rendering while the same generated state is already retired does not
  restart its aura.
- Re-entering `AiState.Generated` after another state can replay its reveal.
- Decorative aura elements are hidden from assistive technology (`UX-07`).

## Cross-cutting evidence (`SPEC-05`)

- **Both color modes:** the declaration paints no surface. Its header and body
  use the parent's isolated `.step-*` selectors and the global `.ai-aura*`
  primitives. Source review confirms this dependency; rendered inspection of
  both modes remains pending under the parent contract (`DESIGN-02`).
- **Enter/exit:** `AuraLifecycle.Sync` drives `DrylAuraElements` on the parent
  header and cancels pending work on disposal. The declaration itself has
  nothing to animate because it emits no element; this is the reasoned
  `DESIGN-11` exception for the declaration, not an exception for the parent's
  body exit debt. The parent owns I13's reduced-motion panel target. Shared aura
  reduction belongs to the existing aura/foundation contracts.
- **Keyboard and a11y:** the parent supplies the native header button,
  `disabled`, `aria-current` and live active-step text. Its contract includes
  keyboard and forced-colors evidence requirements. This declaration adds no
  focusable wrapper or key handling. Source evidence does not establish a
  completed keyboard or screen-reader pass.
- **AI decision:** yes, explicitly. A model can work on one wizard stage, and
  its `Ai` highlights that stage through `AiAuraCss.Append`, `DrylAuraElements`
  and `AuraLifecycle` without inventing a state or visual (`AI-01`–`AI-04`).
  Consumers must end live activity when the associated work ends (`AI-06`).
- **Demo:** the sibling repository's
  `DRYL.Website/Components/Pages/DemoStepper.razor` exposes
  `/components/stepper`. Source-verified `Components/Examples/Stepper/`
  declarations demonstrate labels, descriptions, both orientations, disabled
  state, all four `StepState` values and controls for all five `AiState` values.
  Custom `Icon`, explicit `Aura` and dynamic declaration changes are not
  demonstrated. This pass inspected source, not the running page.
- **Catalog:** source-verified `DRYL.Website/Components/ComponentCatalog.cs`
  registers the parent as `Stepper` / `stepper`, class `DrylStepper`, category
  `Navigation`, group `Layout`, AI flag `true`. `DrylStep` has no separate row;
  it is discoverable through the parent family and has no standalone surface
  (`REL-04`).

## Recorded debt and scope limits

- **Identity and ordering:** unique identifiers are a consumer requirement;
  there is no duplicate-ID validation. Declarations appended after initial
  render follow registration order, which is not a promise to reconcile a
  dynamically reordered declaration tree. `Id` changes require a replacement
  declaration instance if a different identifier is needed.
- **Partial change notification:** `OnParametersSet` compares `Ai`,
  `StepState`, `Disabled`, `Label` and `Description`. It does not request a
  parent refresh solely for `Icon`, `Aura` or `ChildContent` changes. Those
  values can be observed on another parent render, but an immediate update is
  not guaranteed. I13 does not broaden this component's update contract.
- **Scope state differs from other AI-aware controls:** `EffectiveAura` uses
  `AiScope.ResolveAura`, but `AuraFx.Sync` receives `Ai` directly. Automatic
  scope-state inheritance is not implemented; changing that behaviour needs
  its own contract update.
- **Announcements (`UX-04`):** the shared aura markup is decorative. Neither
  this declaration nor the parent's active-step live text announces changes to
  this step's AI state. The active-step announcement is not evidence that AI
  activity is announced.
- **Icon-only naming (`UX-05`):** a declaration can omit visible text while its
  parent renders a custom or progress-state icon. There is no per-step tooltip
  or accessible-label API. Provide meaningful visible labels; the parent's
  `AriaLabel` names the group only.
- **Pending verification:** this is a newly written contract supporting I13.
  Its parent’s reduced-motion and forced-colors changes, browser evidence,
  both-mode visual checks and reconciliation against this declaration remain
  outstanding. No `Implemented` claim is made from source review alone.

## I13 focus verification — 2026-09-17

`tests/DRYL.BrowserTests/FocusTests.cs` exercises Tab/Shift+Tab, Stepper
activation, text and multiline input, the actual Select popover with arrows,
Enter/Escape and focus return, and dialog close/return in both modes.
Ordinary keyboard cases pass in Chromium, Firefox and WebKit; Chromium and
installed Chrome/Edge also pass forced-colors outline assertions. Shared
controls and Stepper headers use existing `--accent-b` with browser-defined
outline geometry. Normal focus styles remain unchanged. Visual inspection
and final counts are recorded in `docs/2026-09-15-i13-implementation-plan.md`.

This supersedes the earlier pending-I13 verification wording. A native Windows
contrast-theme inspection and native Safari/Firefox smoke remain outstanding;
forced-colors emulation is not claimed as that native evidence. No full-library
accessibility audit or repair of the unrelated recorded debt is claimed.
