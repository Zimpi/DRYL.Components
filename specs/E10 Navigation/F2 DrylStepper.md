# DrylStepper

## Meta
- **State:** Implemented
- **Source:** code/DRYL.Components/Components/Navigation/DrylStepper.razor
              code/DRYL.Components/Components/Navigation/DrylStepper.razor.css

## User Story

As a Blazor developer, I want to declare a sequence of named steps with a bound
active step, so that I can build a wizard whose navigation, progress and current
content remain understandable to pointer and keyboard users.

## Description

`DrylStepper` displays a horizontal or vertical track of step headers and the
body of the active step. Each `DrylStep` supplies the label, optional description
and icon, completion/error state, disabled state, AI styling and body content.
The developer supplies any Back, Next, validation or submission controls.

This contract records the existing navigation behaviour and adopts the bounded
panel-motion and forced-colors focus repairs from
[`I13`](../../ideas/I13%20Performance%20and%20hardening%20update.md). Those repairs
are implemented and verified by the scoped browser matrix below. Other existing
limitations remain recorded debt; this package does not redesign navigation,
change public parameters or add a panel exit primitive.

## Public API

Namespace: `DRYL.Components`.

| Member | Type | Default | Meaning |
|---|---|---|---|
| `ActiveStep` | `string?` | `null` | Identifier of the active `DrylStep`; supports `@bind-ActiveStep`. |
| `ActiveStepChanged` | `EventCallback<string?>` | Unset | Reports a user-selected identifier or the fallback selected when the active declaration is removed. |
| `Orientation` | `StepperOrientation` | `StepperOrientation.Horizontal` | Layout axis of the header track. |
| `AriaLabel` | `string?` | `null` | Accessible name of the root group. |
| `ChildContent` | `RenderFragment?` | `null` | Step declarations. |
| `Class` | `string?` | `null` | Classes appended to the root. |
| `AdditionalAttributes` | `IDictionary<string, object>?` | `null` | Captured unmatched attributes applied to the root. |

`Dispose()` implements `IDisposable`. There are no public navigation methods,
validation hooks or form-integration parameters. `StepperOrientation` and the
step-registration boundary are documented in [`_Api.md`](_Api.md).
The declaration's API belongs to [`F3 DrylStep.md`](F3%20DrylStep.md).

## Acceptance Criteria

### Structure and content

- The root renders with class `stepper`.
- The root carries `role="group"`.
- `AriaLabel` supplies the root's `aria-label` when provided.
- `Class` is appended to the root's generated class list.
- Unmatched attributes are applied to the root.
- Each registered step produces one native `button` header.
- Each header has `type="button"`.
- Header order follows step registration order.
- A step's nonempty `Label` appears in its header.
- A step's nonempty `Description` appears below its label.
- `StepState.Completed` displays the `Check` icon in the indicator.
- `StepState.Error` displays the `X` icon in the indicator.
- A step in neither completed nor error state displays its nonempty `Icon` in
  the indicator when supplied.
- A step without a state icon or custom icon displays its one-based position.
- The final header has no connector to a following step.
- The active step's `ChildContent` is rendered in `.step-panel` when non-null.
- An inactive step's `ChildContent` is not rendered as a step panel.
- An active step with null `ChildContent` has no step panel.
- An identifier with no matching registered step produces no step panel.
- Changing the active identifier replaces the active body; inactive child
  component instances inside the previous body are not retained.

### Selection and callbacks

- `ActiveStep` defaults to `null`.
- With no active identifier, registering an enabled step selects it.
- Initial automatic selection emits no `ActiveStepChanged` callback.
- Registering a disabled step does not make it the automatic selection.
- Activating a different enabled header makes its identifier active.
- Activating a different enabled header emits `ActiveStepChanged` with that
  identifier once.
- Activating the already active header emits no `ActiveStepChanged` callback.
- A disabled header cannot be activated by pointer.
- A disabled header cannot be activated by keyboard.
- Assigning `ActiveStep` selects the matching step without emitting
  `ActiveStepChanged`.
- Assigning a disabled step's identifier through `ActiveStep` can select that
  step; `Disabled` blocks header activation rather than programmatic selection.
- Removing the active declaration selects the first remaining enabled step.
- Removing the active declaration emits `ActiveStepChanged` with the fallback
  identifier, or `null` if there is no remaining enabled step.
- Removing an inactive declaration does not emit `ActiveStepChanged`.
- `StepState.Active` alone does not select a step; selection is determined by
  `ActiveStep` and the registration fallback.
- A completed or error indicator keeps its state appearance when selected.
- Selection does not automatically mark preceding steps completed.

### Layout and appearance

- `Orientation` defaults to `StepperOrientation.Horizontal`.
- Horizontal orientation lays out headers in a row.
- A horizontal track scrolls when its headers do not fit the available width.
- Vertical orientation lays out headers in a column.
- The vertical active body appears after the track, indented to its labels.
- Enabled header hover uses `--glass-1`.
- Active header labels use `--fg`.
- Completed header labels use `--fg`.
- Other header labels use `--fg-muted`.
- Descriptions use `--fg-dim`.
- Completed indicators use `--success`.
- Error indicators use `--danger`.
- Completed connectors use `--success`.
- Other connectors use `--line-strong`.
- Colors resolve from the same selectors in both light and dark modes
  (`DESIGN-02`).

### Keyboard and announcements

- `Tab` reaches enabled headers in track order (`UX-01`).
- `Shift+Tab` reaches enabled headers in reverse track order.
- Disabled headers are excluded from sequential keyboard focus.
- `Enter` activates a focused enabled header.
- `Space` activates a focused enabled header.
- The active header carries `aria-current="step"`.
- Inactive headers have no `aria-current` attribute.
- `.stepper-live` carries `role="status"`.
- `.stepper-live` carries `aria-live="polite"`.
- With a matching active step, the live text is `Step {position} of {count}`,
  with `: {Label}` appended when its label is nonempty.
- With no matching active step, the live region has no active-step text.
- Normal-mode keyboard focus uses the existing accent treatment from
  `.step-header:focus-visible` (`UX-02`).

### Motion and focus hardening — I13

- Normal-motion step panels retain their existing `menu-in` entrance using
  `--dur-med` and `--ease-out` (`DESIGN-10`).
- Under `prefers-reduced-motion: reduce`, a newly active `.step-panel` is
  immediately visible in its settled state with no entrance motion (`UX-06`).
- Changing to reduced motion while the panel enters leaves its content visible
  in the settled state.
- Reduced motion does not change the enabled headers' keyboard order (`UX-07`).
- Under `forced-colors: active`, a focused `.step-header` has a visible browser
  outline even when box shadows are suppressed (`UX-02`).
- The forced-colors treatment retains the existing normal-mode accent focus
  treatment when forced colors is inactive.
- The fallback uses existing color tokens and browser-provided outline geometry;
  it introduces no token, keyframe or public parameter.

## Cross-cutting evidence (`SPEC-05`)

- **Both color modes:** source review of `DrylStepper.razor.css` confirms the
  appearance tokens named above and no mode selector. This is source evidence,
  not a visual pass. Inspection in both rendered modes remains required before
  implementation completion (`DESIGN-02`).
- **Enter/exit:** `.step-panel` uses the existing shared `menu-in` keyframes.
  `.step-header`, `.step-indicator` and `.step-connector` have token-based
  transitions. The active body is conditionally mounted without an exit wrapper;
  that pre-existing `DESIGN-12` debt is retained below. No motion exception is
  claimed for the body.
- **Keyboard and a11y:** the native buttons, disabled attributes, `aria-current`
  and `.stepper-live` are present in `DrylStepper.razor`. Source review does not
  establish real keyboard focus or screen-reader announcement timing. I13
  requires browser checks of Tab/Shift+Tab, Enter/Space and both modes, plus
  forced-colors emulation and a Windows contrast-theme inspection. Those checks
  are pending; emulation alone is not the native contrast-theme evidence.
- **AI decision:** the container has no `Ai` parameter because work belongs to
  an individual step. It renders each step's shared `DrylAuraElements` and
  `AiAuraCss` classes on that header; no container-level aura is needed (`AI-05`).
  The child owns the state and lifecycle under `F3 DrylStep`.
- **Demo:** source verified in the sibling website repository at
  `DRYL.Website/Components/Pages/DemoStepper.razor`, route `/components/stepper`.
  `Components/Examples/Stepper/` contains `Horizontal`, `Vertical`, `States`,
  `Disabled` and `AiMode`. The first two demonstrate bound Back/Next navigation;
  `AiMode` exposes all five AI states. The examples have not been run as part of
  this spec-writing pass.
- **Catalog:** source verified in
  `DRYL.Website/Components/ComponentCatalog.cs`: `Stepper`, slug `stepper`, group
  `Layout`, class `DrylStepper`, category `Navigation`, AI flag `true`. The flag
  describes the family through its AI-aware child (`REL-04`).

I13's separate browser host must load the actual `dryl.css` and generated
CSS-isolation bundle. Computed animation/focus styles and live rendering must
exercise these selectors; token-sync success alone proves neither repair.

## Recorded debt and scope limits

- **Body exit (`DESIGN-12`):** changing steps removes the prior `.step-panel`
  immediately. There is no `DrylPresence` or exit lifecycle. I13's bounded panel
  motion repair does not add one.
- **Literal geometry (`DESIGN-01`):** `.step-indicator`, `.step-number`,
  `.step-label`, `.step-description`, `.step-label-group`, connector offsets,
  horizontal header sizing and focus/indicator shadow geometry retain literal
  design values. Their presence is documented debt, not a license for new ones.
- **Selection fallback is not binding initialization:** `RegisterStep` updates
  only the internal identifier, and `UnregisterStep` does not assign
  `ActiveStep`. A later parameter pass reasserts the supplied `ActiveStep`, even
  when it is still `null` or names a removed step. Consumers that require stable
  controlled selection should provide an identifier and handle its callback.
- **No focus transfer to the body:** header activation leaves native button
  focus in place. Programmatic changes and removal of focused body content have
  no managed focus restoration. The component has no arrow-key navigation or
  tab/tabpanel relationship; it exposes a group of buttons, not ARIA tabs.
- **No navigation guard:** disabling the current step does not deselect it.
  There is no built-in validation, sequential-only navigation or completion
  event. These are API limits rather than promises of this package.
- **Status naming (`UX-04`, `UX-05`):** completion/error icons and per-step AI
  changes have no dedicated live announcements. A header whose only visible
  content is an icon has no automatic tooltip or matching accessible label.
  The group name does not name each header. Supply descriptive step labels;
  I13 does not add a per-header naming API.
- **Verification gaps:** no `DrylStepper`/`DrylStep`-specific tests were found
  under `tests/DRYL.Components.Tests/` during the source inventory. The website
  examples do not demonstrate dynamic registration/removal, an `Aura` variant,
  reduced motion or forced colors. New I13 regression evidence remains pending.

The I13 reduced-motion panel override is implemented and exercised by
`tests/DRYL.BrowserTests/MotionTests.cs`; focus verification is recorded separately.

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
