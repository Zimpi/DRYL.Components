# DrylAiStream

## Meta
- **State:** Implemented
- **Source:** code/DRYL.Components/Components/AI/DrylAiStream.razor
              code/DRYL.Components/Ai/AiStreamContext.cs

## User Story

As a Blazor developer with a token stream from an LLM, I want to hand it to a
component that accumulates the text and drives the AI state for me, so that the
answer types itself out and the surrounding UI knows it is streaming without me
writing a state machine.

## Description

`DrylAiStream` consumes an `IAsyncEnumerable<string>` and turns it into two things
the UI can bind to: the text accumulated so far, and the `AiState` the stream is
currently in. Both are handed to `ChildContent` as an `AiStreamContext`; without
`ChildContent` the component renders the raw text. It contributes no element of its
own either way.

The state walks a fixed path — `Thinking` while the first token is awaited,
`Streaming` from the first token on, `Generated` when the source completes, and
after a short dwell the value of `SettleTo`. That dwell is what makes the
`Generated` reveal perceivable; it is a logical pause of the component's own, not a
CSS duration, so `DESIGN-01` does not govern it.

`SettleTo` is therefore not the opt-in `AI-03` governs: the live state comes from
the stream itself, and this parameter never turns anything on — it says where to
land. [`_Api.md`](_Api.md) records that for the category, alongside
`DrylAiGenerate` and `DrylAiBuild`, which carry the same parameter for the same
reason from the agents package.

With a `Key` and a registered `IDrylAiActivityService`, the stream pushes each
state to that key, so a surrounding `DrylAiScope Key="…"` lights up in lockstep
with the text.

`Smooth` exists for providers that buffer: some deliver a large part of a response
in one burst, and rendering that burst as it arrives makes a streaming answer land
as a paste. `Smooth` puts a backlog between the source and the screen and drains it
at a steady pace, faster the further behind it is, so a live token stream is not
slowed down and a burst still reads as typing.

## Public API

| Member | Type | Default | Purpose |
|---|---|---|---|
| `Source` | `IAsyncEnumerable<string>?` | `null` | The token stream. Replacing it cancels the previous run and starts over. |
| `Key` | `string?` | `null` | Operation key pushed to `IDrylAiActivityService`, so a surrounding `DrylAiScope` follows along. |
| `ChildContent` | `RenderFragment<AiStreamContext>?` | `null` | Renders the accumulated text and the current state. Without it the raw text is rendered. |
| `OnCompleted` | `EventCallback<string>` | — | Raised once the stream completed successfully, with the full text. |
| `SettleTo` | `AiState` | `AiState.None` | The state to settle to after the `AiState.Generated` reveal. Not an opt-in (`AI-03`) — see `_Api.md`. |
| `Smooth` | `bool` | `false` | Reveal incoming text at a steady, backlog-adaptive pace instead of rendering each chunk as it arrives. |

`AiStreamContext` exposes `Text` (`string`) and `State` (`AiState`), both settable
only from inside the library. The component reuses **one** context instance for the
lifetime of the stream and mutates it, so a consumer reads it during render and
does not cache it across renders.

The component takes no `Class` and no `AdditionalAttributes`: it has no element to
put them on.

## Acceptance Criteria

### Rendering

- The component renders `ChildContent` with the current context when `ChildContent`
  is set.
- The component renders the accumulated text when `ChildContent` is `null`.
- The component renders no element of its own in either case.
- The context's `Text` holds every token received so far, concatenated in arrival
  order.
- The rendered text is re-rendered as tokens arrive, without the consumer calling
  `StateHasChanged`.

### State progression

- The state is `AiState.Thinking` from the start of a run until the first token
  arrives.
- The state becomes `AiState.Streaming` when the first token arrives.
- The state remains `AiState.Streaming` for every further token.
- The state becomes `AiState.Generated` when the source completes.
- The state becomes the value of `SettleTo` after the `Generated` dwell elapses.
- `SettleTo` defaults to `AiState.None`.
- `SettleTo` accepts exactly the five values of `AiState`.
- The dwell between `Generated` and `SettleTo` is a fixed pause the component owns,
  identical for every run and not read from a CSS token.
- The dwell is cancellable: a run superseded or disposed during it never applies
  `SettleTo`.
- The state settles to `AiState.None` and the accumulated text is kept when the
  source throws.
- No state is applied when the run is cancelled, so a superseded run never
  overwrites the state its successor has already set.

### Source lifecycle

- A run starts when `Source` changes to a different instance.
- Passing the same `Source` instance again does not restart the run.
- Starting a run cancels the run in progress and clears the accumulated text.
- The state passes through `AiState.Thinking` and settles at `AiState.None` when
  `Source` is `null`.
- The run is cancelled when the component is disposed (`CODE-05`).

### Callbacks and service binding

- `OnCompleted` is raised with the full accumulated text when the source completes.
- `OnCompleted` is raised before the `Generated` dwell begins, not after it.
- `OnCompleted` is not raised when the source throws.
- `OnCompleted` is not raised when the run is cancelled.
- The state of each transition is pushed to `IDrylAiActivityService` under `Key`
  when `Key` is set and the service is registered.
- The key is cleared rather than set when the state is `AiState.None`, so a
  finished stream leaves no live state behind (`AI-06`).
- Nothing is pushed to the service when `Key` is `null`.
- Nothing is pushed to the service when none is registered — the service is
  resolved optionally, so the component works without `AddDrylComponents()`.

### Smooth reveal

- The final text is identical with and without `Smooth`.
- With `Smooth`, a burst larger than one reveal step is revealed over several
  renders rather than in one.
- With `Smooth`, the reveal rate grows with the size of the backlog, so a large
  burst does not take proportionally longer to drain than a small one.
- With `Smooth`, the state still becomes `AiState.Streaming` on the first revealed
  text rather than on the first token buffered.
- With `Smooth`, a source that throws still settles the state as it does without
  `Smooth`: the fault is surfaced after the backlog loop ends, never swallowed.

### Keyboard, accessibility and motion

- The component contributes no element, no focusable node and no ARIA semantics; it
  changes neither the focus order nor the reading order of its children (`UX-07`).
- The component carries no `aria-live` region of its own. `UX-04` asks that AI
  activity be announced politely, and here that duty sits with whatever renders the
  text: the component has no element to put the attribute on, and wrapping the
  consumer's markup in one would announce every token as it arrives. A consumer
  that needs the announcement places a `DrylAiIndicator`, or an `aria-live="polite"`
  region of its own, around the content it passes as `ChildContent`.
- The component has no animation of its own. This is the explicit exception
  `DESIGN-11` allows: it renders no markup, so there is no surface to move. Its
  contribution to the library's motion is the pacing of `Smooth` and the `AiState`
  it drives, which animates the surfaces bound to it.

### Appearance

- The component contributes no CSS and no class of its own.
- The component branches on no color mode and holds no mode-assuming value
  (`DESIGN-02`) — trivially, since it paints nothing.

## Cross-cutting evidence (`SPEC-05`)

- **Both color modes** — the component renders no markup and defines no style, so
  it is mode-agnostic by construction; `node scripts/check-light-sync.mjs` and
  `node scripts/validate-light-contrast.mjs` have no rule of its to check.
- **Enter/exit animation** — none, and the exception is written out under
  "Keyboard, accessibility and motion" above on the terms `DESIGN-11` sets.
- **Keyboard and a11y** — the criteria above, including the deliberate placement of
  the `aria-live` duty on the consumer.
- **AI mode** — yes, and it is deliberately **not** an opt-in: `SettleTo` names the
  state to land on after the reveal and never turns anything on, so `AI-03` was
  never about it. See `_Api.md`.
- **Demo page** — `DRYL.Website/Components/Examples/AiActivity/Streaming.razor`.
- **`ComponentCatalog`** — covered by the `"AI Activity"` / `ai-activity` entry in
  `DRYL.Website/Components/ComponentCatalog.cs`, whose `ClassName` names
  `DrylAiScope`. The two components share one demo page because they are used
  together, and the catalog carries 95 entries for 127 components precisely so that
  a page may document more than one. `DrylAiStream` has no entry of its own.

## Notes for tests

The behaviour above is partly covered by
`tests/DRYL.Components.Tests/DrylAiStreamTests.cs`, which asserts direct-mode
accumulation, the gradual reveal of a 600-character burst under `Smooth`, and that
`Smooth` does not change the final text. The state progression, the callback
ordering and the service binding are not yet covered there.
