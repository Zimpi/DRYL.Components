# DrylAiGenerate

## Meta
- **State:** Implemented
- **Source:** code/DRYL.Components.Agents/Generation/DrylAiGenerate.razor
              code/DRYL.Components.Agents/Generation/GenerationSnapshot.cs
              code/DRYL.Components.Agents/Generation/PartialJsonReader.cs
              code/DRYL.Components.Agents/Generation/JsonPartialRepair.cs

## User Story

As a Blazor developer asking a model for a typed object, I want the JSON it
streams to arrive as a growing instance of my type, so that I can render real
components — a title, a list, a number — that fill in while the model is still
writing, instead of a spinner followed by a finished object.

## Description

`DrylAiGenerate<T>` is the structured sibling of `DrylAiStream`. It consumes an
`IAsyncEnumerable<string>` of raw JSON tokens — typically from
`DrylAgentRunner.GenerateStreamingAsync<T>` — and hands `ChildContent` a
`GenerationSnapshot<T>`: the best partial `T` parsed so far, the raw buffer, the
`AiState` of the generation and whether the stream has completed. It renders no
element of its own.

Each partial buffer is made parseable by `JsonPartialRepair` — open strings and
containers are closed, a trailing comma, dangling key or half-written literal is
dropped, and a half-written number keeps its longest complete prefix — and then
deserialized by `PartialJsonReader<T>`. A buffer that still fails to parse keeps
the last good snapshot, so a field never flickers back to empty. Open strings
surface their content so far, so text grows character by character.

Parsing the whole buffer is the costly part, so re-renders are coalesced to at
most one per `ThrottleMs`; every token is still buffered, a slow stream still
renders each token, and the final snapshot always renders.

The state walks the same path as `DrylAiStream`: `Thinking` until the first
render, `Streaming` while tokens arrive, `Generated` on completion and, after a
fixed dwell, the value of `SettleTo`. Like `DrylAiStream`, the component
restarts **and clears** whenever `Source` is a different reference, so a
consumer caches the stream in a field and never binds a property that builds a
new enumerable per read.

```razor
<DrylAiScope Key="recipe">
    <DrylAiGenerate T="Recipe" Source="_stream" Key="recipe">
        <RecipeCard Recipe="@context.Value" Done="@context.IsComplete" />
    </DrylAiGenerate>
</DrylAiScope>
```

## Public API

Namespace: `DRYL.Components.Agents`. Type parameter `T`. Implements
`IDisposable`.

| Member | Type | Default | Purpose |
|---|---|---|---|
| `Source` | `IAsyncEnumerable<string>?` | `null` | Raw JSON token stream. A different reference restarts and clears. |
| `Key` | `string?` | `null` | Operation key pushed to `IDrylAiActivityService`, so a surrounding `DrylAiScope Key="…"` follows along. |
| `ChildContent` | `RenderFragment<GenerationSnapshot<T>>?` | `null` | Renders the live snapshot. |
| `SettleTo` | `AiState` | `AiState.None` | The state to settle to after the `AiState.Generated` reveal. Not an opt-in (`AI-03`). |
| `ThrottleMs` | `int` | `66` | Minimum milliseconds between streaming re-renders; `0` renders on every token. |

`GenerationSnapshot<T>`, `PartialJsonReader<T>` and `JsonPartialRepair` are
listed member by member in [`_Api.md`](_Api.md).

## Acceptance Criteria

### Rendering

- The component renders `ChildContent` with the current snapshot when
  `ChildContent` is set.
- The component renders nothing when `ChildContent` is `null`.
- The component renders no element of its own.
- The component hands `ChildContent` the same `GenerationSnapshot<T>` instance
  for its whole lifetime and mutates it.
- The snapshot is re-rendered as tokens arrive, without the consumer calling
  `StateHasChanged`.

### Partial parsing

- `Value` holds the best partial `T` parsed from the tokens received so far.
- `Raw` holds the tokens received up to the last render, concatenated in
  arrival order.
- A string still open in the buffer surfaces its content so far in `Value`.
- A buffer that cannot be parsed even after repair leaves `Value` at the last
  successfully parsed snapshot.
- A number still being written keeps its longest complete numeric prefix
  instead of disappearing.
- A trailing comma, a key without a value yet and a half-written literal are
  ignored until they complete.
- Property names match case-insensitively.
- Trailing commas and comments in the JSON are tolerated.

### Throttling

- With `ThrottleMs` greater than `0`, the snapshot is re-parsed and re-rendered
  at most once per `ThrottleMs` while tokens arrive.
- With `ThrottleMs` `0` or less, every token re-parses and re-renders.
- Tokens arriving inside a throttle window are buffered, not dropped.
- When the stream completes, the tokens buffered since the last render are
  parsed and rendered before the state becomes `AiState.Generated`.
- `ThrottleMs` defaults to `66`.
- A change of `ThrottleMs` takes effect for the next token without restarting
  the run.

### State progression

- `State` is `AiState.Thinking` from the start of a run until the first render
  with content.
- `State` is `AiState.Streaming` for every render while tokens arrive.
- `State` becomes `AiState.Generated` when the source completes.
- `IsComplete` becomes `true` together with `AiState.Generated`.
- `State` becomes the value of `SettleTo` after the `Generated` dwell elapses;
  `IsComplete` stays `true`.
- `SettleTo` defaults to `AiState.None`.
- `SettleTo` accepts exactly the six values of `AiState`.
- The dwell between `Generated` and `SettleTo` is a fixed pause the component
  owns, identical for every run and not read from a CSS token.
- A run superseded or disposed during the dwell never applies `SettleTo`.
- When the source throws, `State` becomes `AiState.None` and `Value` and `Raw`
  keep what was last rendered.
- When the source throws, `IsComplete` is `false`.
- The exception of a failing source is not rethrown and not surfaced.
- A cancelled run applies no further state.

### Source lifecycle

- A run starts when `Source` changes to a different reference.
- Passing the same `Source` reference again does not restart the run.
- Starting a run cancels the run in progress.
- Starting a run resets `Value` to its default, `Raw` to empty and `IsComplete`
  to `false`.
- With `Source` `null`, the state passes through `AiState.Thinking` and settles
  at `AiState.None`.
- The run is cancelled when the component is disposed (`CODE-05`).

### Service binding

- With `Key` set and `IDrylAiActivityService` registered, every state the
  snapshot takes is pushed to the service under `Key`.
- `AiState.None` clears the key rather than setting it, so a finished or failed
  generation leaves no live state behind (`AI-06`).
- Nothing is pushed when `Key` is `null`.
- Nothing is pushed when no service is registered; the service is resolved
  optionally.

### Keyboard, accessibility and motion

- The component contributes no element, no focusable node and no ARIA
  semantics, and changes neither focus order nor reading order (`UX-07`).
- The component carries no `aria-live` region; as for `DrylAiStream`, the
  announcement duty sits with the markup the consumer renders in
  `ChildContent` (`UX-04`).
- The component has no animation of its own — the explicit `DESIGN-11`
  exception for a component that renders no markup. Its contribution to motion
  is the paced growth of the snapshot and the `AiState` it drives.

### Appearance

- The component contributes no CSS and no class of its own.
- The component branches on no color mode (`DESIGN-02`).

## Recorded debt

- **Silent failure.** A source that throws settles at `AiState.None` with no
  callback and no error surface; a host that needs to show the failure wraps
  the stream itself (the demo does exactly that).
- **No completion callback.** Unlike `DrylAiStream`, there is no
  `OnCompleted`; a host reads `IsComplete` from the snapshot.
- **Failure drops the unrendered tail.** On a throw the snapshot falls back to
  the last *rendered* `Value` and `Raw`; tokens buffered inside the final
  throttle window are discarded.
- **No `Paused`.** The `DrylAiStream` pause contract (I22) has no counterpart
  here; an open stream that waits keeps reporting `AiState.Streaming`.
- **No raw-text fallback.** Without `ChildContent` nothing renders, where
  `DrylAiStream` renders its text.

## Cross-cutting evidence (`SPEC-05`)

- **Both color modes** — the component renders no markup and defines no style;
  mode-agnostic by construction; inspected on `/components/structured-generation`, after a full generation, in dark and light (2026-11-20).
- **Enter/exit animation** — none; the `DESIGN-11` exception is stated under
  "Keyboard, accessibility and motion".
- **Keyboard and a11y** — the criteria above, including the deliberate
  placement of the `aria-live` duty on the consumer.
- **AI mode** — yes, and deliberately **not** an opt-in: `SettleTo` names where
  to land after the reveal; the live state comes from the stream. It is one of
  `AI-03`'s five named non-opt-ins.
- **Demo page** — `/components/structured-generation`
  (`DRYL.Website/Components/Pages/DemoStructuredGeneration.razor`), example
  `Agents/StructuredGeneration`, which also demonstrates the stable `Source`
  reference and a live `ThrottleMs` slider.
- **`ComponentCatalog`** — the `"Structured Generation"` /
  `structured-generation` entry.
- **Tests** — `tests/DRYL.Components.Tests/Agents/DrylAiGenerateTests.cs`
  (partial then complete snapshot, raw buffer),
  `tests/DRYL.Components.Tests/Agents/PartialJsonReaderTests.cs` and
  `tests/DRYL.Components.Tests/Agents/JsonPartialRepairTests.cs`. Throttling,
  the settle dwell, the failure path and the service binding have no test.
