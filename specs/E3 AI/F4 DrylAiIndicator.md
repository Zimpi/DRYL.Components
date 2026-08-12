# DrylAiIndicator

## Meta
- **State:** Implemented
- **Source:** code/DRYL.Components/Components/AI/DrylAiIndicator.razor

## User Story

As a Blazor developer building an AI-aware UI, I want a small pill that says what
the AI is doing right now, so that I can mark an area as busy without inventing a
label, a color and a spinner of my own.

## Description

`DrylAiIndicator` is the library's status pill for AI activity: a sparkle icon, a
short label, a soft accent background and a shimmer that sweeps across it. It is
the smallest AI-aware surface DRYL has, and the one the rest of the library
composes — `DrylToolCall`, `DrylToolCallGroup` and `DrylCommandPalette` all place
one and override its label with their own status vocabulary.

The `State` parameter **is** the component's content, not a switch. The pill's
whole job is to display an `AiState`, and `State` selects which one — down to the
idle "AI" label it shows for `AiState.None`. That is why it is not called `Ai` and
does not default to `AiState.None`; the reasoning is in
[`_Api.md`](_Api.md) and in `AI-03` of [`../../harness/ai.md`](../../harness/ai.md).

The component is a leaf and owns nothing: it never times out, never settles itself
and never talks to `IDrylAiActivityService`. Whoever mounts it decides how long it
stays, which is what `AI-06` asks of its host.

## Public API

| Member | Type | Default | Purpose |
|---|---|---|---|
| `State` | `AiState` | `AiState.Active` | The AI state to display. Drives the default label and the animation speed. Not an opt-in (`AI-03`) — see `_Api.md`. |
| `ChildContent` | `RenderFragment?` | `null` | Replaces the state-derived label. The icon and the styling stay. |
| `Class` | `string?` | `null` | Extra CSS class(es) merged onto the pill's own. |
| `AdditionalAttributes` | `IDictionary<string, object>?` | `null` | Pass-through attributes on the root element. |

The component takes no `EventCallback` and no `Ai` parameter: it displays a state,
it does not produce one.

## Acceptance Criteria

### Content

- The component renders a single inline root element.
- The component renders a sparkle `DrylIcon` before the label.
- The component renders the state-derived label when `ChildContent` is `null`.
- The component renders `ChildContent` instead of the state-derived label when
  `ChildContent` is set.
- The component renders the sparkle icon whether or not `ChildContent` is set.
- `Class` is merged onto the root element's own classes.
- `AdditionalAttributes` are applied to the root element.

### AI state

- `State` defaults to `AiState.Active`.
- `State` accepts exactly the five values of `AiState`.
- The state-derived label reads "AI" when `State` is `AiState.None`.
- The state-derived label reads "AI" when `State` is `AiState.Active`.
- The state-derived label reads "Thinking…" when `State` is `AiState.Thinking`.
- The state-derived label reads "Streaming…" when `State` is `AiState.Streaming`.
- The state-derived label reads "Generated" when `State` is `AiState.Generated`.
- `AiState.None` and `AiState.Active` render identically: the pill is not switched
  off by `AiState.None`, because `State` is the component's content rather than an
  opt-in.
- The root carries the `is-thinking` modifier exactly when `State` is
  `AiState.Thinking`.
- The root carries the `is-streaming` modifier exactly when `State` is
  `AiState.Streaming`.
- The component renders no aura: it composes `.ai-indicator` from `AI-02`, never
  `.ai-aura` (`DESIGN-08` — a pill this small is an indicator, not a surface to
  ring).

### Motion

- The icon pulses continuously with `--ease-in-out`.
- A shimmer sweeps continuously across the pill's background with `--ease-in-out`.
- The icon's pulse is faster when `State` is `AiState.Thinking` than in the
  unmodified state.
- The icon's pulse is faster when `State` is `AiState.Streaming` than in the
  unmodified state, and slower than under `AiState.Thinking`.
- The shimmer's period changes with `is-thinking` and `is-streaming` in the same
  way, so speed alone communicates the state.
- Both animations stop under `prefers-reduced-motion: reduce`, leaving the pill
  legible and still.

  The periods of those two animations are written as literals. `DESIGN-10` governs
  one-shot motion; a continuous (`infinite`) segment may choose its own period, and
  `node scripts/check-motion-tokens.mjs` judges the segments accordingly. The
  easings are tokens either way.

- The pill itself has no enter or exit animation. This is the explicit exception
  `DESIGN-11` allows: the pill is an inline marker its host places beside existing
  content, the host owns its mount (`DrylToolCall` and `DrylToolCallGroup` both
  keep theirs mounted and change only `State`), and its "alive" quality is carried
  by the continuous pulse rather than by an entrance. A component that does mount
  one conditionally wraps it in `DrylPresence` on its own side (`DESIGN-12`).

### Keyboard and accessibility

- The root carries `role="status"`.
- The root carries `aria-live="polite"`, so a state change is announced without
  interrupting the user (`UX-04` — this component is that rule's precedent).
- A label change is announced through that region without the component being
  re-mounted.
- The pill holds no focusable element and takes no tab stop: it reports, it is not
  operated.
- The sparkle icon is decorative: it is rendered without an accessible label and
  is therefore `aria-hidden`, so the announcement is the label alone and never
  doubles it (`UX-07`).

### Appearance

- The label is rendered in `--font-mono`, matching the tool names it sits beside.
- The text is rendered in `--accent-fg`.
- The border is `--accent-line`.
- The background is `--accent-soft`.
- The sparkle icon is rendered in `--ai-b`.
- The shimmer derives from `--shimmer`.
- The corner radius is `--r-pill`.
- The accent appears as the soft background, the 1px border and the small icon —
  never as a saturated fill (`DESIGN-08`).
- The component branches on no color mode and holds no mode-assuming value, so the
  same markup serves light and dark (`DESIGN-02`).

  The pill's `height`, `padding` and `gap` are written as literals, as are its
  `font-size` and `letter-spacing`. `DESIGN-01` governs the first three and not the
  last two, and its check greps colors in `*.razor.css` only, so it does not see
  them: this component's styling lives in the shared
  `code/DRYL.Components/wwwroot/dryl.css` rather than in an isolated stylesheet.
  Recorded here as documented debt, not as compliance.

## Cross-cutting evidence (`SPEC-05`)

- **Both color modes** — token-only colors, verified by
  `node scripts/check-light-sync.mjs` and
  `node scripts/validate-light-contrast.mjs`; the component defines no
  mode-specific rule.
- **Enter/exit animation** — none, and the exception is written out under "Motion"
  above on the terms `DESIGN-11` sets. The continuous pulse and shimmer are the
  component's motion.
- **Keyboard and a11y** — the "Keyboard and accessibility" criteria above.
- **AI mode** — yes, and it is deliberately **not** an opt-in: the parameter is the
  pill's content, not a switch on an otherwise ordinary component, so `AI-03` was
  never about it and the name `State` with its `AiState.Active` default stands. See
  `_Api.md`.
- **Demo page** — `DRYL.Website/Components/Examples/Ai/Indicator.razor` and
  `.../Ai/Lifecycle.razor`.
- **`ComponentCatalog`** — registered as `"AI Mode"` / `ai` in
  `DRYL.Website/Components/ComponentCatalog.cs`.
