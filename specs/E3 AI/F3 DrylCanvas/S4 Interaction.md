# Interaction

## Meta
- **State:** Implemented

Buttons inside the artifact, and — when the host opts in with a `Selection` —
direct manipulation of the artifact's own elements.

## Acceptance Criteria

### Intents and actions

- A button inside the artifact raises `OnInteraction` with a
  `CanvasInteraction`.
- A button bound to a registered host action runs that action instead of only
  raising its intent.
- A registered action's result may carry a ready-made chat turn, which arrives
  in `CanvasInteraction.Message`.
- `OnAction` is raised after every completed action button, successful or not.
- `OnAction` is raised after the canvas has already applied the result — the
  patch, refresh, toast or inline error has happened by then, so the callback is
  for the host's own logging or reactions.
- An artifact renders and its buttons still raise intents when the host
  registered no actions at all.

### Selection

- Direct manipulation is off until the host passes a `Selection`; without it
  nothing about the canvas changes.
- A node becomes selectable by click and by keyboard once a `Selection` is
  present.
- The selected node exposes a toolbar to prompt about it, pin, duplicate, remove
  or reorder it.
- Sharing one `CanvasSelection` instance with `DrylCanvasDock` carries the
  selected element into the next prompt as a context chip.
- While nothing is selected, the tab stop sits on the artifact's first top-level
  node.
- Replacing `Spec` clears the selection (see `S1`).
- The selection subscription is removed when the selection instance is replaced
  and when the component is disposed (`CODE-05`).

### Keyboard navigation

- Keyboard navigation resolves a step against the whole tree — first child,
  parent, previous and next sibling, first and last sibling.
- A step to the parent stops at the root: the root itself is never selected.
- A step skips nodes that are on their way out.
- A resolved step moves focus to the target, not only the selection.

### Node commands

- Pinning a node marks it locked and leaves the artifact content untouched: the
  pin is metadata, so it never goes through the patcher and never pulses.
- A locked node cannot be duplicated, removed or reordered.
- A node inside a locked parent cannot be reordered.
- Duplicating a node inserts the copy directly after the original.
- Duplicating a node assigns the copy fresh ids, so no id occurs twice in the
  tree.
- Duplicating a node selects the copy.
- Removing a node clears the selection.
- Reordering moves the node one slot among its siblings, or to a named index
  when a drop supplied one.
- Reordering past either end of the sibling list does nothing.
- Duplicate, remove and reorder are each applied as one `CanvasOp` through the
  one patcher — which is what makes a user's edit animate exactly like an AI's
  edit. Pin is the exception named above: it is metadata, not a change to the
  artifact, so it bypasses the patcher deliberately.
- A completed command raises `OnEdit` with a `CanvasEdit` carrying a label
  ("Pinned …", "Duplicated …", "Removed …", "Moved …").
- A command that was refused raises no `OnEdit`.

### Reorder gesture

- The drag-to-reorder gesture is attached only when a `Selection` is present — a
  canvas nobody may edit needs no gesture.
- A drop onto an unchanged position does nothing.
- A drop outside the sibling range does nothing.
- A drop on a locked node, or inside a locked parent, does nothing.
- The gesture is detached on disposal (`CODE-05`).
- Disposing during a drag also cancels the active gesture: window listeners,
  pointer capture, dragging transform/class and drop markers are released.
- `Escape`, `pointercancel` and replacement by a new gesture cancel without
  committing a reorder. Only the active pointer may move or complete the drag.
- Events delivered after cancellation or disposal cannot mutate the node or
  report `OnNodeReorder`; a valid changed drop reports exactly once.
- Reinitializing the gesture cancels an active drag before replacing its
  delegated handler and .NET callback target.
- Losing pointer capture cancels the drag without reporting a reorder.
- Cancellation restores the node's previous inline transform, including its
  priority, rather than discarding a transform already present before the drag.
- A release after the canvas or dragged node has been removed cancels rather
  than committing a reorder.
- A rejected reorder interop promise does not become an unhandled rejection.

### Announcements

- Selecting a node announces its label and its type name.
- Clearing the selection announces that it was cleared.
- Pin, duplicate and remove each announce what happened to which element.
- A reorder announces the node's new position as "position n of m".

### Element names

- An element is named by `CanvasLabel.For(node, Texts.TypeName)`: the first
  non-blank of its `title`, `label`, `text`, `submitLabel`, `name` and
  `content` props, at most 60 characters.
- An element with none of those props is named by `Texts.TypeName` of its
  catalog type — by default the readable English name ("Line chart" for
  `lineChart`, "Element" for a missing type).
- A blank result from a host's `TypeName` falls back to the default name, so an
  element's name is never empty.
- The type part of a selectable element's accessible name, the type in the
  selection announcement and the type in the "waiting for …" caption all come
  from `Texts.TypeName` — the raw catalog id never reaches a human.
- The same name reaches every place an element is named: its accessible name,
  the announcements, the `CanvasEdit` label and `CanvasSelection.Label`.
- `DrylCanvas` hands its `Texts.TypeName` to its `Selection` on every parameter
  set, so the dock's context chip and the prompt to the model carry the
  translated name; `CanvasSelection.Type` keeps the raw catalog id, so the model
  can still identify the element.
- `CanvasLabel.For(node)` without a type-name function keeps the default name.

### Texts

- Every fixed word of the interaction chrome comes from `Texts` (`CanvasTexts`):
  the toolbar's labels and tooltips (`Prompt`, `Pin` / `Unpin`, `Duplicate`,
  `Remove`, `Reorder`), the name of an element type (`TypeName`), the pinned
  state in an element's accessible name (`PinnedState`), the announcements (`Selected`, `SelectionCleared`, `Pinned`,
  `Unpinned`, `Duplicated`, `Removed`, `MovedTo`) and the `CanvasEdit` labels
  (`PinEdit`, `DuplicateEdit`, `RemoveEdit`, `MoveEdit`). The form's
  required-field hint (`Required`) and a `dataGrid` table's accessible name
  (`DataGrid`) come from it as well.
- An action button's own messages come from `Texts` too: the inline error of
  a handler that threw (`ActionFailed`, from the action's name), the inline
  error when a confirmation is required but no dialog service is registered
  (`ConfirmUnavailable`), and the confirmation dialog's title for an unlabelled
  button and its buttons (`Confirm`, `Cancel`). `DrylCanvas` hands its `Texts`
  to its `CanvasActionRunner` on every parameter set.
- Without `Texts` every one of them reads as before, in English — the quoted
  wording above is `CanvasTexts.Default`.
- A replaced text lands everywhere its default did: the tooltip, the
  `aria-label` and the `aria-live` region alike.

## I13 gesture regression evidence

- `tests/js/gestures.test.mjs` executes the real exports from
  `code/DRYL.Components/wwwroot/js/dryl-canvas.js` against controlled event
  targets. Before the H02 repair, the combined Canvas/table suite failed all
  16 initial cases: disposal left Canvas's four active window listeners in
  place; capture was not released; a foreign pointer could complete the drag;
  replacement preserved the old preview; late callbacks could still act.
- The repaired gesture owns one cancellable operation per initialized root.
  Its `finish` closure is one-shot, and `disposeReorder` cancels that operation
  before detaching the delegated handler. The expanded Node suite passes all
  19 cases, including unchanged drops, horizontal siblings, replacement,
  disconnection and rejected interop promises.
- `dotnet test DRYL.slnx -c Release --no-build --filter
  "FullyQualifiedName~Canvas|FullyQualifiedName~Table"` passed all 606 matching
  existing tests. These .NET tests do not execute the gesture module.
- `tests/DRYL.BrowserTests/GestureTests.cs` exercises the working-tree Canvas
  and .NET edit result through the real browser host. Native pointer cancellation,
  late-event silence, remount and one-shot commit cases now pass in both modes;
  final engine counts are recorded in the I13 implementation plan. The layering
  gap below remains outside the ownership repair.

## Recorded gap

The selected toolbar's reorder button can be visible while its center is
covered for pointer hit testing by a paragraph in the following `.md` content.
This occurred in Chromium, Firefox and WebKit on `#canvas-scene` in the browser
host. The toolbar is inside a `DrylPresence` in
`code/DRYL.Components/Canvas/Internal/CanvasNodeView.razor`; its positioning
rules are `.canvas-node-tools` in `DrylCanvas.razor.css`. The H02 ownership
scene tests a genuinely exposed part of the existing grip without force-clicks
or stylesheet changes. That does not establish that the whole visible button
is pointer-reachable (`UX-01`); the existing layering issue remains separate
from gesture cleanup.
