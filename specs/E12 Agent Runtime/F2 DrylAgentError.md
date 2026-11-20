# DrylAgentError

## Meta
- **State:** Implemented
- **Source:** code/DRYL.Components.Agents/Agents/DrylAgentError.razor
              code/DRYL.Components.Agents/Agents/DrylAgentError.razor.css

## User Story

As a Blazor developer running an agent through `DrylAgentRunner`, I want a run
that fails to show its error — with an optional retry — the moment it fails,
so that a broken provider or a thrown tool never leaves my user staring at an
aura that simply stopped.

## Description

`DrylAgentError` watches a `DrylRunBase` and surfaces its terminal error. A run
that faults settles at `AiState.None` with `Error` set to a `DrylRunError`
(see [`_Api.md`](_Api.md)); this component turns that into a danger
`DrylAlert` that slides in through `DrylPresence`, and renders nothing while
the run is healthy.

The alert carries the error's `Message`, an optional monospace meta line built
from the error's `Source` (the failing step of a multi-agent run) and
`ExceptionType`, and — when the consumer wires `OnRetry` — a small secondary
retry button. The component does not retry anything itself: retry is whatever
the consumer's callback does, typically starting a new run and passing it in.

`DrylHandoffTrace` composes it once per lane, with the title "Step failed".

## Public API

Namespace: `DRYL.Components.Agents`.

| Member | Type | Default | Purpose |
|---|---|---|---|
| `Run` | `DrylRunBase?` | `null` | The run whose error to surface. Nothing renders while it is `null` or its `Error` is `null`. |
| `OnRetry` | `EventCallback` | — | Optional retry action; the retry button exists only when it has a delegate. |
| `Title` | `string` | `"Agent run failed"` | Title of the danger alert. |
| `RetryLabel` | `string` | `"Retry"` | Label of the retry button. |

## Acceptance Criteria

### Rendering

- The component renders no alert while `Run` is `null`.
- The component renders no alert while `Run.Error` is `null`.
- Once `Run.Error` is set, the component renders one `DrylAlert` with
  `DrylAlert.AlertKind.Danger`.
- The alert's title is `Title`.
- The alert body renders the error's `Message` in `.agent-error-message`.
- With both `Source` and `ExceptionType` set, `.agent-error-meta` renders
  matching the format `{Source} — {ExceptionType}`.
- With only `Source` set, `.agent-error-meta` renders `Source`.
- With only `ExceptionType` set, `.agent-error-meta` renders `ExceptionType`.
- With neither set, no `.agent-error-meta` element renders.

### Retry

- Without an `OnRetry` delegate, no retry button renders.
- With an `OnRetry` delegate, one `DrylButton` renders in
  `.agent-error-actions`, with `DrylButton.ButtonVariant.Secondary`,
  `DrylButton.ButtonSize.Small` and the `Refresh` icon.
- The retry button's label is `RetryLabel`.
- Activating the retry button fires `OnRetry`.

### Live updates

- The component re-renders when the run raises `OnChange`, so an error set
  mid-run appears without a parent render.
- The component unsubscribes from the previous run's `OnChange` when `Run`
  changes to another instance.
- The component unsubscribes from the run's `OnChange` when it is disposed.

### Motion

- The alert enters through `DrylPresence` with `PresenceTransition.SlideUp`,
  including on first render (`Appear`).
- When `Run.Error` returns to `null` — for example when the consumer passes a
  fresh run — the `DrylPresence` wrapper runs its exit (see "Recorded debt"
  for what it exits with).

### Keyboard and accessibility

- The alert carries the role and live-region politeness of a danger
  `DrylAlert` — `role="alert"` with `aria-live="assertive"` — so the failure is
  announced when it appears.
- The retry button is a native button: reachable by `Tab`, operable by `Enter`
  and `Space`, with a visible text label.

### Appearance

- The body is a column with `--sp-2` between message, meta line and actions.
- The meta line uses `--font-mono` in `--fg-dim`.
- The actions row sits `--sp-1` below the meta line.

## Recorded debt

- **Literal font size.** `.agent-error-meta` sets an 11 px font size instead of
  a type token (`DESIGN-01`).
- **The exit is empty.** The alert is rendered under
  `@if (Run?.Error is { } error)` inside the `DrylPresence`, so once the error
  is gone the presence's exit pass renders no alert: the alert disappears at
  once and the slide-out plays on an empty wrapper (`DESIGN-12`). The fix is to
  hold the last error for the exit.
- **The meta line is not localisable.** Its em dash separator is fixed in code;
  only `Title` and `RetryLabel` are parameters.

## Cross-cutting evidence (`SPEC-05`)

- **Both color modes** — token-only colors, composed of `DrylAlert` and
  `DrylButton`; inspected on `/components/run-health`, with a failed run shown, in dark and light (2026-11-20).
- **Enter/exit animation** — enter yes: `DrylPresence` with
  `PresenceTransition.SlideUp`; the exit runs on an empty wrapper, recorded
  above.
- **Keyboard and a11y** — the "Keyboard and accessibility" criteria above.
- **AI mode** — **no** `Ai` parameter (`AI-05`). The component exists for the
  moment the AI stopped working: by the library's convention a failed run is
  `Error != null` at `AiState.None`, and an aura on the failure would claim
  work that is not happening (`AI-06`).
- **Demo page** — `/components/run-health`
  (`DRYL.Website/Components/Pages/DemoRunHealth.razor`), example
  `Agents/ErrorHandling`.
- **`ComponentCatalog`** — the `"Run Health"` / `run-health` entry in
  `DRYL.Website/Components/ComponentCatalog.cs`.
- **Tests** — `tests/DRYL.Components.Tests/Agents/DrylAgentErrorTests.cs`.
