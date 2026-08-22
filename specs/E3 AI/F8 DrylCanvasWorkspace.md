# DrylCanvasWorkspace

## Meta
- **State:** Implemented
- **Source:** code/DRYL.Components/Components/AI/DrylCanvasWorkspace.razor
              code/DRYL.Components/Components/AI/DrylCanvasWorkspace.razor.css

## User Story

As a Blazor developer building a line-of-business surface an assistant writes onto,
I want several named artifacts side by side with exactly one of them shown large,
so that a user can go back to "the overview" after asking about order 4711 instead
of losing it.

## Description

`DrylCanvasWorkspace` renders the named views of one `CanvasWorkspace` as a row of
chips and shows exactly one of them in its body. A line-of-business page is not one
artifact; it is a handful of them, and the workspace is what makes them navigable.

Switching runs through `IDrylMorph`, so the surface morphs into the other
view rather than blinking, while the shared `[data-dryl-ink]` indicator glides
between the chips — the same primitive `DrylTabs` uses. The morph is deliberately
owned here rather than by whatever sits in the body: nesting two morphs
loses one of the mutations, which is why `DrylAiCanvas` suppresses its own swap
morph for a workspace switch.

The body is a slot. With `View` the host renders each view however it likes —
typically a `DrylAiCanvas`; without it the workspace renders a plain `DrylCanvas`
over the active view's spec. That is also why the component takes **no `AiState`
parameter at all**: it renders an ordinary canvas and leaves AI to whatever wraps
it. [`_Api.md`](_Api.md) records that for the category.

Beyond the views it offers two optional facilities that share one mechanism.
`ShowHistory` adds undo, redo and a version list, driven by each view's snapshot
ring; `AutoSave` writes the settled state to a registered `ICanvasDocumentStore`
after a pause. Both hang off `Revision`: the host bumps that counter after every
settled round, and the workspace commits a version and schedules a save.

This spec is one file rather than a split folder. `SPEC-02` names three split
candidates — `DrylTable`, `DrylCommandPalette` and `DrylCanvas` — and asks for a
stated reason for anything else; this component is smaller than `DrylCanvas` and
its criteria fit into the thematic `###` sections `SPEC-03` allows for large specs.

## Public API

| Member | Type | Default | Purpose |
|---|---|---|---|
| `Workspace` | `CanvasWorkspace?` | `null` | The views to show. Without one the workspace renders its empty state. |
| `View` | `RenderFragment<CanvasView>?` | `null` | How the active view is rendered. Without it, a plain `DrylCanvas` over the view's spec. |
| `AllowClose` | `bool` | `true` | Whether each chip offers a close button. |
| `ShowBarWhenSingle` | `bool` | `false` | Whether the bar shows for a single view too. |
| `ShowHistory` | `bool` | `false` | Whether the bar offers undo, redo and the version history. |
| `Revision` | `int` | `0` | A counter the host bumps after every settled round; each change commits a version. |
| `RevisionLabel` | `string?` | `null` | Label for the next committed version — typically the prompt that produced it. |
| `AutoSave` | `bool` | `false` | Whether a settled revision is written to the registered `ICanvasDocumentStore`. |
| `AutoSaveDelayMs` | `int` | `1500` | How long to wait after the last revision before saving. |
| `DocumentId` | `string?` | `null` | The document autosave writes to. Null until the first save mints one. |
| `DocumentIdChanged` | `EventCallback<string>` | — | Reports the id the store assigned on the first save. |
| `DocumentTitle` | `string?` | `null` | Title stored with the document; defaults to the active view's title. |
| `OnSaved` | `EventCallback<CanvasDocumentInfo>` | — | Raised after every successful autosave. |
| `EmptyTitle` | `string` | `"No view yet"` | Headline shown while there is nothing to show. |
| `EmptyText` | `string?` | `"Ask the assistant to open one."` | Text shown while there is nothing to show, and the empty text of the fallback canvas. |
| `AriaLabel` | `string` | `"Views"` | Accessible label of the view bar. |
| `Class` | `string?` | `null` | Extra CSS class(es) merged onto the root. |
| `AdditionalAttributes` | `IDictionary<string, object>?` | `null` | Pass-through attributes on the root element. |

`Workspace` is plain observable state, not a parameter the component owns: a chip,
the host and an AI tool call all mutate it, and the workspace re-renders on its
`OnChange` event. There is no `Views` parameter and no `ActiveId` parameter — the
`CanvasWorkspace` instance is the contract.

## Acceptance Criteria

### The view bar

- The bar is shown when the workspace holds more than one view.
- The bar is hidden when the workspace holds exactly one view and neither
  `ShowBarWhenSingle` nor `ShowHistory` is set.
- The bar is shown for a single view when `ShowBarWhenSingle` is `true`.
- The bar is shown for a single view when `ShowHistory` is `true`, because one
  artifact still deserves an undo.
- The bar is hidden when the workspace holds no view.
- The bar is hidden when `Workspace` is `null`.
- One chip is rendered per view, in the order the views were opened.
- A chip renders its view's title.
- A chip renders its view's icon before the title when the view has one.
- The chip of the active view is marked as active.
- A chip offers a close button when `AllowClose` is `true`.
- A chip offers no close button when `AllowClose` is `false`.
- The close button carries a tooltip and a matching accessible label naming the
  view it closes (`UX-05`).
- Only the chips scroll horizontally; the tool group stays reachable when the bar
  is wider than the viewport.

### The body

- The body renders `View` with the active view when `View` is set.
- The body renders a `DrylCanvas` over the active view's spec when `View` is
  `null`.
- The fallback canvas is given `EmptyText` as its own empty text.
- The body renders a `DrylEmptyState` with `EmptyTitle` and `EmptyText` when there
  is no active view.
- The body renders the empty state when `Workspace` is `null`.

### Switching views

- Activating a chip makes its view the active one.
- Activating the already-active chip changes nothing and starts no transition.
- The switch runs through `IDrylMorph`, so the surface morphs rather than
  snapping.
- The gliding indicator moves to the newly active chip rather than jumping.
- The indicator is re-measured only when the active chip changed.
- The indicator does not slide in from the left edge on the first render.
- The indicator is re-attached from scratch after the bar has been away, because a
  returning bar is a new element.

### Closing a view

- Closing a view starts its exit animation rather than removing it immediately.
- A view is removed from the workspace only once its exit animation has finished.
- Closing the active view hands the active slot to a neighbour right away, so the
  body never keeps showing something that is on its way out.
- Keyboard navigation skips views that are on their way out.

### History

- A change to `Revision` commits a version of the active view.
- The committed version is labelled with `RevisionLabel` when it is set.
- The committed version is labelled with a generated, invariant "Version *n*" when
  `RevisionLabel` is `null`.
- A commit whose serialized spec is identical to the current snapshot is dropped,
  so committing generously costs nothing.
- Undo is disabled while the active view has no earlier snapshot.
- Redo is disabled while the active view's undo cannot be taken back.
- The version history button is disabled while the active view has no versions.
- The version list renders the newest version first.
- The version list marks the version currently shown.
- Selecting a version restores it and closes the version list.
- Undo, redo and restore each run through `IDrylMorph`, so the artifact
  morphs rather than blinking.
- A history step that moved nothing announces nothing and leaves the cursor where
  it was.
- Undo, redo and restore each announce what happened through the workspace's
  `aria-live` region.
- History is per view: undo on one view never reaches another.

### Autosave

- Nothing is saved while `AutoSave` is `false`.
- Nothing is saved while no `ICanvasDocumentStore` is registered, and the absence
  of one is not an error.
- A save is scheduled by a change to `Revision`, never by an ordinary re-render.
- A save waits `AutoSaveDelayMs` after the last revision, so a burst of revisions
  results in one write.
- A scheduled save is cancelled by a newer revision.
- A scheduled save is cancelled when the component is disposed (`CODE-05`).
- The saved document carries `DocumentTitle` when it is set, and the active view's
  title otherwise.
- The saved document leaves out views that are on their way out.
- `DocumentIdChanged` is raised when the store assigns an id that differs from
  `DocumentId`.
- `DocumentIdChanged` is not raised when the id is unchanged.
- `OnSaved` is raised after every successful save.
- A store that throws is swallowed: a broken backend never takes a running
  dashboard down.
- The live field values of the fallback canvas are folded into the saved document,
  so a restored document shows what the user had typed.
- No field values are folded in when the host supplied its own `View` slot: the
  workspace holds no reference to a canvas it did not render, and capturing them is
  then the host's own job.

### Keyboard and accessibility

- The chip row carries `role="tablist"` and is labelled by `AriaLabel`.
- Each chip's label is a `<button type="button">` carrying `role="tab"`.
- The chip of the active view carries `aria-selected="true"`; every other chip
  carries `aria-selected="false"`.
- Exactly one chip is in the tab order at a time: the active chip carries
  `tabindex="0"` and the others `tabindex="-1"`.
- <kbd>→</kbd> activates the next open view, wrapping to the first.
- <kbd>←</kbd> activates the previous open view, wrapping to the last.
- <kbd>Home</kbd> activates the first open view.
- <kbd>End</kbd> activates the last open view.
- <kbd>Enter</kbd> and <kbd>Space</kbd> activate the focused chip.
- <kbd>Delete</kbd> and <kbd>Backspace</kbd> close the focused view when
  `AllowClose` is `true`.
- <kbd>Delete</kbd> and <kbd>Backspace</kbd> do nothing when `AllowClose` is
  `false`.
- The chip label shows a focus ring in `--accent-line` on `:focus-visible`.
- A version list entry shows a focus ring in `--accent-line` on `:focus-visible`.
- The workspace carries an `aria-live="polite"` region for history announcements.
- That region is visually hidden but present in the accessibility tree, because
  the movement is the visual feedback and the words are for screen readers.
- The gliding indicator is `aria-hidden`, so a decorative marker adds nothing to
  the reading order (`UX-07`).

### Motion

- The bar animates in and out through `DrylPresence`, so a second view appearing
  does not make the bar pop into existence (`DESIGN-12`).
- Each chip animates in and out through `DrylPresence`, keyed by its view id.
- A chip's exit animation is what drives its removal, rather than running after it.
- The gliding indicator moves over `--dur-med` with `--ease-spring`.
- A chip's color, border and background transition over `--dur-fast` with
  `--ease-out` on hover.
- A version list entry transitions over `--dur-fast` with `--ease-out` on hover.
- The close button's opacity transitions over `--dur-fast` with `--ease-out`, and
  it reaches full opacity on chip hover and on its own `:focus-visible`.
- All of those transitions are disabled under `prefers-reduced-motion: reduce`.

### Interop and cleanup

- The gliding indicator is placed through `dryl.motion.moveIndicator` after render.
- The indicator is released through `dryl.motion.disposeIndicator` on disposal, and
  only when one was ever attached.
- A lost circuit, a prerender pass with no JS available, and a bar that left the
  DOM between render and interop are each tolerated without surfacing an error.
- The component unsubscribes from the workspace's `OnChange` when it is disposed,
  and when the `Workspace` parameter is replaced (`CODE-05`).

### Appearance

- Every color resolves to a token (`DESIGN-01`).
- Every radius, duration and easing resolves to a token (`DESIGN-01`).
- The active chip is separated from the body by `--line-strong`, an inactive one by
  nothing until it is hovered.
- The bar is closed off by a `--line` bottom border.
- The gliding indicator is painted in `--accent-grad` and is 2px tall, so the
  accent appears as a line rather than as a fill (`DESIGN-08`).
- A chip's resting label is rendered in `--fg-muted` and lifts to `--fg` on hover
  and while active, so an inactive view recedes.
- The version timestamp is rendered in `--fg-dim` at `--fs-xs`.
- The workspace sits in the document flow and hand-writes no `backdrop-filter`
  (`DESIGN-06`, `DESIGN-07`).
- A chip's background is `--glass-1` while it is hovered or active, and transparent
  at rest.
- A version list entry's background is `--glass-1` while it is hovered, and
  transparent at rest.
- The component branches on no color mode and holds no mode-assuming value, so the
  same markup serves light and dark (`DESIGN-02`).

  Four dimensions are written as literals in `DrylCanvasWorkspace.razor.css`: the
  chip row's `padding-bottom` and the indicator's `height`, both 2px hairlines that
  position the ink against the bar's border, and the version list's `min-width` and
  `max-height`, which size a popover rather than expressing spacing. `DESIGN-01`
  governs the padding; its check greps colors only and does not see it. Recorded
  here as documented debt, not as compliance.

## Cross-cutting evidence (`SPEC-05`)

- **Both color modes** — token-only colors, verified by
  `node scripts/check-light-sync.mjs` and
  `node scripts/validate-light-contrast.mjs`; the component defines no
  mode-specific rule.
- **Enter/exit animation** — the bar and every chip through `DrylPresence`, the
  view switch and every history step through `IDrylMorph`, and the
  indicator through `dryl.motion.moveIndicator`. See "Motion".
- **Keyboard and a11y** — the "Keyboard and accessibility" criteria above: a full
  tablist model with a roving tab stop, and a polite live region for history.
- **AI mode** — **no**, deliberately. The workspace renders a plain `DrylCanvas`
  and leaves AI to whatever wraps it, so it takes no `AiState` parameter of any
  kind; `AI-03` has no subject here and `AI-05` is satisfied by the decision being
  written down. The AI-facing surface is `DrylAiCanvas` in the agents package,
  placed in the `View` slot. See `_Api.md`.
- **Demo page** — `DRYL.Website/Components/Examples/CanvasWorkspace/Basic.razor`,
  `.../CanvasWorkspace/Direct.razor` and `.../CanvasWorkspace/Document.razor`.
- **`ComponentCatalog`** — registered as `"Canvas Workspace"` / `canvas-workspace`
  in `DRYL.Website/Components/ComponentCatalog.cs`.
