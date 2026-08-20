# DrylErrorBoundary

## Meta
- **State:** Implemented
- **Source:** code/DRYL.Components/Components/Feedback/DrylErrorBoundary.razor
              code/DRYL.Components/Components/Feedback/DrylErrorBoundary.razor.css

## User Story

As a Blazor developer, I want a section of my page that survives its own
exceptions and says so in the library's own visual language, with a retry that
actually recovers, so that one failing panel does not leave a white unstyled
block in the middle of a glass layout — or take the whole page with it.

## Description

`DrylErrorBoundary` wraps Blazor's built-in `ErrorBoundary` and replaces its
default markup. When the protected content throws during a render or a lifecycle
method, the boundary shows a danger [`F1 DrylAlert`](F1%20DrylAlert.md) carrying
a title, an optional description, an optional collapsible exception panel, and a
retry button.

Retry is a two-step sequence, and the order is the whole design: the consumer's
`OnRetry` runs **first**, then the boundary recovers. A boundary that recovered
first would re-render the same failing content and throw again immediately;
running the callback first is what gives the consumer the chance to clear the
condition — reset the state, re-issue the request — before the child is asked to
render again.

`ShowDetails` is the parameter that must not be left on in production: it
reveals the full exception text, stack trace included. The component makes it
opt-in, defaults it off, and says so in its own documentation; it cannot enforce
it.

The component is AI-aware by forwarding rather than by rendering: `Ai` and
`Aura` are passed into the alert, so a failed AI block reads in the same aura
vocabulary that was on screen while the model was working.

## Public API

| Member | Type | Default | Purpose |
|---|---|---|---|
| `ChildContent` | `RenderFragment?` | `null` | The protected content. |
| `Title` | `string` | `"Something went wrong"` | Heading on the fallback surface. |
| `Description` | `string?` | a generic retry hint | Explanatory line under the title. |
| `ShowRetry` | `bool` | `true` | Renders the retry button. |
| `RetryText` | `string` | `"Try again"` | Label of the retry button. |
| `OnRetry` | `EventCallback` | — | Raised on retry, **before** the boundary recovers. |
| `ShowDetails` | `bool` | `false` | Reveals a collapsible panel with the full exception text. |
| `FallbackContent` | `RenderFragment<Exception>?` | `null` | Full override of the fallback surface. |
| `MaximumErrorCount` | `int` | `100` | Errors tolerated before the boundary stops recovering. |
| `Ai` | `AiState` | `AiState.None` | Forwarded to the fallback alert. |
| `Aura` | `AiAura?` | `null` | Forwarded to the fallback alert. |
| `Class` | `string?` | `null` | Extra CSS class(es) merged onto the fallback alert's own classes. |
| `AdditionalAttributes` | `IDictionary<string, object>?` | `null` | Pass-through attributes on the fallback surface. |

| Method | Signature | Purpose |
|---|---|---|
| `Recover` | `void Recover()` | Recovers the boundary from consumer code, without a user gesture. |

`Recover` is public because the failure a boundary caught is often cleared by
something other than a button — a reconnect, a new selection, a background
retry — and the host needs a way to say so.

## Acceptance Criteria

### While nothing has thrown

- The component renders `ChildContent` and nothing of its own.
- The component adds no wrapper element around `ChildContent`, so it does not
  disturb the layout it is placed in.

### After a throw

- An exception thrown while rendering `ChildContent` is caught rather than
  propagated.
- The fallback surface is rendered in place of `ChildContent`.
- The fallback surface is a danger alert, so a failure is announced assertively
  by the alert's own role (`F1`).
- The fallback renders `Title` as its heading.
- The fallback renders `Description` when it is non-empty.
- The fallback renders no description element when `Description` is `null` or
  empty.
- `FallbackContent` set replaces the entire built-in surface.
- `FallbackContent` receives the caught exception.
- `FallbackContent` set suppresses the title, the description, the details
  toggle and the retry button, because all four belong to the surface it
  replaced.
- The boundary stops recovering after `MaximumErrorCount` errors, so a child
  that throws on every render cannot loop forever.

### Retry

- The retry button is rendered only when `ShowRetry` is `true`.
- The retry button renders `RetryText` as its label.
- Activating retry raises `OnRetry` before the boundary recovers.
- The boundary recovers after `OnRetry` has completed, so an asynchronous
  handler finishes clearing the condition before the child re-renders.
- Activating retry recovers the boundary even when no handler is attached.
- Recovering re-renders `ChildContent`, and a child that no longer throws is
  shown again.
- Recovering collapses the details panel, so a second failure does not open
  onto the previous exception's text.
- `Recover` called from consumer code has the same effect as the button, minus
  the callback.

### Exception details

- The details toggle is rendered only when `ShowDetails` is `true`.
- The details panel is collapsed on first render.
- Activating the toggle expands the panel, and activating it again collapses it.
- The expanded panel renders the caught exception's full text.
- The toggle's label states which action it will perform, so it does not read
  the same in both states.
- The panel scrolls within a bounded height, so a long stack trace does not
  push the retry button off the screen.
- The exception text wraps rather than scrolling sideways, so a long type name
  stays readable.
- Nothing about the exception is rendered while `ShowDetails` is `false`, so the
  default configuration leaks nothing.

### Keyboard and accessibility

- The details toggle carries `aria-expanded`, reflecting the panel's state.
- The details toggle is a `type="button"`, so a boundary inside a form cannot
  submit it.
- The details toggle is reachable by `Tab` and activated by `Enter` and `Space`,
  because it is a native button and the component adds no key handling.
- The details toggle shows a visible focus ring under `:focus-visible`.
- The retry button is a `DrylButton` and keeps that component's keyboard
  behaviour.
- The failure is announced by the alert's assertive live region, so a
  screen-reader user is told the section failed rather than finding it silently
  replaced.

### Appearance

- Every color the component renders comes from a token; the component names no
  literal color (`DESIGN-01`).
- The fallback surface's fill, frost, border and accent are the alert's, not
  this component's (`F1`).
- The exception panel paints `--glass-1` with a `--line` border and `--r-sm`, so
  it reads as a deeper layer inside the alert.
- The exception text and the details toggle are set in `--font-mono`, so a stack
  trace is legible as code.
- The details toggle is `--fg-dim` at rest and `--fg-muted` on hover, so a
  developer-only affordance does not compete with the retry button.
- The details toggle's focus ring is `--accent-line`.
- The component branches on no color mode and holds no mode-assuming value, so
  the same markup serves light and dark (`DESIGN-02`).

### Motion

- The details toggle transitions its color over `--dur-fast` with `--ease-out`.
- The toggle's chevron rotates a quarter turn over `--dur-fast` with
  `--ease-out` when the panel opens, so the control shows its own state.
- Both transitions are switched off under `prefers-reduced-motion: reduce`,
  leaving the toggle fully operable.

### AI mode

- `Ai` defaults to `AiState.None`, so AI styling is opt-in.
- `Ai` is forwarded to the fallback alert unchanged.
- `Aura` is forwarded to the fallback alert unchanged.
- The component renders no aura of its own, so there is exactly one aura on the
  fallback surface (`AI-02`).
- The AI state has no effect while nothing has thrown, because the component
  renders nothing of its own then.

## Recorded gaps

- **The exception panel appears and disappears without motion.** It is a
  conditional render with no `DrylPresence` around it, so expanding the details
  snaps a block of text into the layout while the chevron beside it animates
  (`DESIGN-12`).
- **`Class` and `AdditionalAttributes` reach the fallback surface, not the
  boundary.** While nothing has thrown, both are inert — there is no element of
  this component's own to put them on. A consumer styling the boundary is
  styling only its failure state.
- **`ShowDetails` cannot be enforced.** A consumer who hardcodes it to `true`
  ships stack traces to end users. The default is off and the documentation says
  why, which is as far as the component can go.
- **The two toggle labels and the default title and description are fixed
  English**, with `Title` and `Description` overridable and the toggle's two
  states not.
- **Only render-time exceptions are caught.** This is Blazor's own boundary
  semantics — an exception from an event handler or a background task is not a
  render exception and never reaches the fallback — but a consumer reading the
  component's name will expect more than it can deliver.
- **The type sizes and the panel's maximum height are literals** in
  `code/DRYL.Components/Components/Feedback/DrylErrorBoundary.razor.css`.
  `DESIGN-01` covers colors, radii, shadows, durations and easings, which are
  tokens here. Recorded as debt, not as compliance.
- **No tests of its own.** None of the criteria above is guarded by a test —
  including the retry ordering, which is the component's central claim.

## Cross-cutting evidence (`SPEC-05`)

- **Both color modes** — token-only colors, verified by
  `node scripts/check-light-sync.mjs` and
  `node scripts/validate-light-contrast.mjs`. `--glass-1`, `--line` and the
  foreground steps are the mode-dependent tokens; the component defines no
  mode-specific rule.
- **Enter/exit animation** — the component's own motion is the details toggle's
  color transition and its chevron rotation. The fallback surface's appearance
  is a replacement of the protected content, which the boundary does not
  animate; the missing motion on the details panel is recorded as a gap above.
- **Keyboard and a11y** — the "Keyboard and accessibility" criteria above. The
  substantive decision is that the failure inherits the alert's assertive live
  region rather than appearing silently.
- **AI mode** — yes, by forwarding. A model that failed should not be reported
  in a different visual language than the one it worked in, and rendering a
  second aura here would put two on the same surface.
- **Demo page** — `DRYL.Website/Components/Pages/DemoErrorBoundary.razor`, with
  the examples `Components/Examples/ErrorBoundary/Basic.razor`,
  `.../Custom.razor` and `.../AiDetails.razor`.
- **`ComponentCatalog`** — registered as `"Error Boundary"` / `error-boundary`
  in `DRYL.Website/Components/ComponentCatalog.cs`, flagged AI-capable.
