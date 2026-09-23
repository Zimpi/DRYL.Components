# DrylAiCanvas

## Meta
- **State:** Implemented
- **Source:** code/DRYL.Components.Agents/Canvas/DrylAiCanvas.razor

## User Story

As a Blazor developer building an AI application on DRYL, I want the artifact my
agent generates to render live next to the conversation, breathe while it is
being built and speak my application's language, so that users see — and hear,
through a screen reader — where the AI is at work without reading a label.

## Description

`DrylAiCanvas` is `DrylCanvas` (`E3/F3`) with an author: it binds a
`DrylCanvasRun`, the observable handle the canvas tools (`create_artifact`,
`update_artifact`, `open_view`) write into, and adds everything that is specific
to an AI writing the artifact. The rendering of the tree, selection, data binding
and the expand-to-fullscreen overlay all stay in `DrylCanvas`.

What it adds: the shared AI aura around the whole surface, breathing with the
run's state and flashing once on completion; a `DrylAiIndicator` status pill in
the header that doubles as a live element counter while the artifact streams;
polite `aria-live` announcements for building, ready, updated and failed; a
morph when a second `create_artifact` replaces the whole artifact; and the
measured body width forwarded to the run, so the next generation is authored for
the space the artifact actually has.

Every text the component shows or announces is a parameter whose default is the
original English text, so a host in another language sets parameters instead of
living with an English pill and an English screen reader.

A host that shows the canvas only on demand — a dialog opened when the assistant
starts drawing — waits for the surface through the run
(`DrylCanvasRun.WaitForSurfaceAsync`, see [`_Api.md`](_Api.md)); the component's
part in that contract is to report the width once it is mounted and to withdraw
it once it is gone.

## Public API

| Member | Type | Default | Purpose |
|---|---|---|---|
| `Run` | `DrylCanvasRun?` | `null` | The run to render. Rebinding resets the interactive form state. |
| `OnInteraction` | `EventCallback<CanvasInteraction>` | — | A button intent fired inside the artifact. |
| `OnAction` | `EventCallback<CanvasActionOutcome>` | — | Raised after every completed action button. |
| `Selection` | `CanvasSelection?` | `null` | Opt-in for direct manipulation; share it with `DrylCanvasDock`. |
| `OnEdit` | `EventCallback<CanvasEdit>` | — | Raised after every completed direct manipulation. |
| `AllowExpand` | `bool` | `true` | Whether the header offers expand-to-fullscreen. |
| `EmptyText` | `string?` | `"Ask the assistant to create one."` | Message of the empty state. |
| `EmptyTitle` | `string` | `"No artifact yet"` | Headline of the empty state. |
| `FallbackTitle` | `string` | `"Artifact"` | Header title while the artifact has none of its own. |
| `ErrorTitle` | `string` | `"Artifact failed"` | Title of the alert shown when the run failed. |
| `IdleText` | `string` | `"Idle"` | Status pill while nothing runs. |
| `WorkingText` | `string` | `"Working"` | Status pill while the run thinks. |
| `BuildingText` | `Func<int, string>?` | `null` | Status pill while streaming, from the element count; `null` gives `Building` / `Building · n elements`. |
| `ReadyText` | `Func<int, string>?` | `null` | Status pill once generated, from the element count; `null` gives `Ready`. |
| `BuildingAnnouncement` | `string` | `"Building artifact…"` | Announced when a generation starts streaming. |
| `ReadyAnnouncement` | `string` | `"Artifact ready."` | Announced when the first generation completes. |
| `UpdatedAnnouncement` | `Func<int, string>?` | `null` | Announced when an update completes, from the number of changed elements; `null` gives `Artifact updated, n changes.` |
| `FailedAnnouncement` | `string` | `"Artifact failed."` | Announced when the run fails. |
| `Class` | `string?` | `null` | Extra CSS class(es) merged onto the canvas root. |
| `AdditionalAttributes` | `IDictionary<string, object>?` | `null` | Pass-through attributes on the root. |

## Acceptance Criteria

### Binding the run

- The component renders `Run.Spec` through `DrylCanvas`.
- The component re-renders when `Run` raises `OnChange`.
- Rebinding `Run` to a different instance unsubscribes from the old one and
  subscribes to the new one.
- A run's error renders as the canvas's error alert, titled `ErrorTitle`.
- A second `create_artifact` that replaces the whole artifact is animated as a
  morph from the old tree into the new one.
- A replacement caused by a workspace view switch is not morphed by this
  component, because `DrylCanvasWorkspace` owns that morph.

### Surface width

- The measured body width is forwarded to `Run` through `ReportWidth`.
- A run bound after the first measurement receives the last measured width at
  once.
- Rebinding to a different run withdraws the width from the old run, so it no
  longer claims a surface it lost.
- Disposing the component withdraws the width from its run, so the next
  generation waits for, or does without, a new measurement.
- A run whose width this component never reported keeps its width when the
  component is disposed.

### Texts

- The empty state renders `EmptyTitle` as its headline and `EmptyText` as its
  message.
- The header renders `FallbackTitle` while the artifact has no title of its own.
- The status pill reads `IdleText` while the run is idle.
- The status pill reads `WorkingText` while the run is thinking.
- The status pill reads `BuildingText` applied to the element count while the
  run streams, when `BuildingText` is set.
- The status pill reads `Building` before the first element and
  `Building · n elements` after it, when `BuildingText` is `null`.
- The status pill reads `ReadyText` applied to the element count once
  generated, when `ReadyText` is set.
- The status pill reads `Ready` once generated, when `ReadyText` is `null`.
- The built-in element count is composed with `InvariantCulture`.
- Every text parameter defaults to the English text listed under "Public API",
  so a canvas that sets none of them reads exactly as before.

### Keyboard and accessibility

- The announcement region is polite, so an announcement never steals focus.
- A streaming run announces `BuildingAnnouncement`.
- The first completed generation announces `ReadyAnnouncement`.
- A completed update announces `UpdatedAnnouncement` applied to the number of
  changed elements, or `Artifact updated, n changes.` when it is `null`.
- A failed run announces `FailedAnnouncement`.
- Keyboard operation of the artifact, the selection and the expand overlay is
  `DrylCanvas`'s (`E3/F3`).

### AI mode

- The whole surface is the aura host: the shared aura rings the canvas with the
  `AiAura.Comet` variant (`AI-02`).
- The aura follows `Run.State`, so it breathes while thinking and streaming.
- Entering `AiState.Generated` replays the one-shot completion wash.
- Leaving AI mode keeps the aura mounted for one `--dur-slow` beat, so it
  dissolves rather than snapping away.
- The status pill is a `DrylAiIndicator` carrying `Run.State`.

## Recorded gaps

- **`Aura` is not a parameter.** The canvas always uses `AiAura.Comet` and does
  not inherit a surrounding `DrylAiScope`, although `harness/conventions.md`
  section 4 gives every aura host a nullable `Aura`. Its AI state comes from the
  run rather than from an `Ai` parameter, which is the `DrylToolCall` pattern.
- **`DrylCanvas`'s remaining built-in texts are English.** The expand and
  refresh labels and the selection announcements are fixed in `DrylCanvas` and
  not yet reachable from here.

## Cross-cutting evidence (`SPEC-05`)

- **Both color modes** — the component names no color of its own; the aura and
  the canvas surface are token-only (`DESIGN-02`).
- **Enter/exit animation** — the aura enters, breathes and dissolves; a replaced
  artifact morphs; streamed nodes reveal one by one through `DrylCanvas`.
- **Keyboard and a11y** — the "Keyboard and accessibility" criteria above.
- **AI mode** — yes; this is an AI-native component whose state is the run's.
- **Demo page** — `DRYL.Website/Components/Pages/DemoAiCanvas.razor`.
- **`ComponentCatalog`** — registered as `"AI Canvas"` / `ai-canvas` in
  `DRYL.Website/Components/ComponentCatalog.cs`, flagged AI-capable.
- **Tests** — `tests/DRYL.Components.Tests/Agents/Canvas/DrylAiCanvasTests.cs`,
  `DrylAiCanvasTextTests.cs`, `CanvasSurfaceSignalTests.cs` and
  `CanvasLayoutBudgetTests.cs` in the same folder.
