# DrylAiBuild

## Meta
- **State:** Implemented
- **Source:** code/DRYL.Components.Agents/Generation/DrylAiBuild.razor
              code/DRYL.Components.Agents/Generation/ArtifactSnapshot.cs

## User Story

As a Blazor developer letting an agent build a typed artifact round by round, I
want a component that renders the artifact as it stands after every merge and
settles when the agent is done, so that the user watches the brief take shape
instead of waiting for the final object.

## Description

`DrylAiBuild<T>` renders the live state of a `DrylArtifactRun<T>` — the handle
`DrylAgentRunner.StartBuild<T>` returns. Where `DrylAiGenerate<T>` follows a
token stream, this component follows discrete merges: the run applies each
`update_<T>` patch through `JsonMerge` and raises `OnChange`, and the component
copies the run's `Artifact`, `Round` and `State` into an `ArtifactSnapshot<T>`
for `ChildContent`. It renders no element of its own.

When the run reaches `AiState.Generated`, the component holds that state for a
fixed dwell — long enough for the `Generated` reveal to be seen — and then
settles to `SettleTo` and marks the snapshot complete.

The live glow of a surrounding `DrylAiScope` is driven by the runner, not by
this component: `DrylAgentRunner` owns the activity service under the `aiKey`
given to `StartBuild` for the whole run. `DrylAiBuild` touches the service only
at settle, so its `Key` should match that `aiKey`.

```razor
<DrylAiScope Key="build">
    <DrylAiBuild T="CampaignBrief" Run="_run" Key="build">
        <BriefCard Brief="@context.Artifact" Round="@context.Round" />
    </DrylAiBuild>
</DrylAiScope>
```

## Public API

Namespace: `DRYL.Components.Agents`. Type parameter `T`. Implements
`IDisposable`.

| Member | Type | Default | Purpose |
|---|---|---|---|
| `Run` | `DrylArtifactRun<T>?` | `null` | The build to render. A different reference resubscribes. |
| `Key` | `string?` | `null` | Operation key settled in `IDrylAiActivityService`; should match the `aiKey` of `StartBuild`. |
| `ChildContent` | `RenderFragment<ArtifactSnapshot<T>>?` | `null` | Renders the live snapshot. |
| `SettleTo` | `AiState` | `AiState.None` | The state to settle to after the `AiState.Generated` reveal. Not an opt-in (`AI-03`). |

`ArtifactSnapshot<T>` and `JsonMerge` are listed member by member in
[`_Api.md`](_Api.md). `DrylArtifactRun<T>` belongs to the agent runtime
(`E12 Agent Runtime`).

## Acceptance Criteria

### Rendering

- The component renders `ChildContent` with the current snapshot when
  `ChildContent` is set.
- The component renders nothing when `ChildContent` is `null`.
- The component renders no element of its own.
- The component hands `ChildContent` the same `ArtifactSnapshot<T>` instance
  for its whole lifetime and mutates it.

### Following the run

- On first render with a `Run`, the snapshot carries the run's `Artifact`,
  `Round` and `State`.
- Every `OnChange` of the run copies its current `Artifact`, `Round` and
  `State` into the snapshot and re-renders, without the consumer calling
  `StateHasChanged`.
- `Round` equals the number of merge steps the run has applied.
- `IsComplete` is `false` while the run has not settled.

### Settling

- When the run's `State` becomes `AiState.Generated`, the snapshot's `State`
  becomes `AiState.Generated`.
- The settle starts once per run; further `OnChange` notifications during the
  dwell do not restart it.
- During the dwell, further changes still update `Artifact` and `Round`, but
  not `State`.
- After the dwell, the snapshot's `State` becomes the value of `SettleTo`.
- After the dwell, `IsComplete` becomes `true`.
- The dwell is a fixed pause the component owns, identical for every run and
  not read from a CSS token.
- `SettleTo` defaults to `AiState.None`.
- `SettleTo` accepts exactly the six values of `AiState`.

### Run lifecycle

- Passing the same `Run` reference again does not resubscribe and does not
  reset the snapshot.
- Passing a different `Run` unsubscribes from the previous run's `OnChange`.
- Passing a different `Run` cancels a settle in progress, so the previous run
  never applies `SettleTo`.
- Passing a different `Run` resets `IsComplete` to `false`.
- Disposal unsubscribes from the run's `OnChange` (`CODE-05`).
- Disposal cancels a settle in progress.

### Service binding

- With `Key` set and `IDrylAiActivityService` registered, settling to
  `AiState.None` clears the key.
- With `Key` set and the service registered, settling to any other state sets
  the key to that state.
- The component writes nothing to the service before the settle; the live
  states belong to the runner.
- Nothing is written when `Key` is `null` or no service is registered; the
  service is resolved optionally.

### Keyboard, accessibility and motion

- The component contributes no element, no focusable node and no ARIA
  semantics, and changes neither focus order nor reading order (`UX-07`).
- The component carries no `aria-live` region; the announcement duty sits with
  the markup the consumer renders in `ChildContent` (`UX-04`).
- The component has no animation of its own — the explicit `DESIGN-11`
  exception for a component that renders no markup. Its contribution to motion
  is the `AiState` it settles and the reveal the run paces.

### Appearance

- The component contributes no CSS and no class of its own.
- The component branches on no color mode (`DESIGN-02`).

## Recorded debt

- **A `null` run leaves the last snapshot.** Setting `Run` to `null` cancels the
  settle and resets `IsComplete`, but `Artifact`, `Round` and `State` keep the
  previous run's values.
- **A failed run is not settled.** A run that ends in `AiState.None` with an
  `Error` is mirrored as `None`; the component neither clears `Key` nor marks
  the snapshot complete, and exposes no error of its own.

## Cross-cutting evidence (`SPEC-05`)

- **Both color modes** — the component renders no markup and defines no style;
  mode-agnostic by construction; **not yet inspected by eye**: the `/components/collaborative-build` demo stayed behind its clarifying dialogs in the 2026-11-20 session, so no artifact was rendered.
- **Enter/exit animation** — none; the `DESIGN-11` exception is stated under
  "Keyboard, accessibility and motion".
- **Keyboard and a11y** — the criteria above.
- **AI mode** — yes, and deliberately **not** an opt-in: `SettleTo` names where
  to land after the reveal; the live state comes from the run. It is one of
  `AI-03`'s five named non-opt-ins.
- **Demo page** — `/components/collaborative-build`
  (`DRYL.Website/Components/Pages/DemoCollaborativeBuild.razor`), example
  `Agents/CollaborativeBuild`, with the matching `DrylAiScope Key`.
- **`ComponentCatalog`** — the `"Collaborative Build"` / `collaborative-build`
  entry.
- **Tests** — `tests/DRYL.Components.Tests/Agents/DrylAiBuildTests.cs` (renders
  the current snapshot, re-renders on change) and
  `tests/DRYL.Components.Tests/Agents/JsonMergeTests.cs`. The settle, the run
  swap and the service binding have no test.
