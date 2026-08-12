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
- Every structural command is applied as one `CanvasOp` through the one
  patcher — which is what makes a user's edit animate exactly like an AI's edit.
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

### Announcements

- Selecting a node announces its label and its type.
- Clearing the selection announces that it was cleared.
- Pin, duplicate and remove each announce what happened to which element.
- A reorder announces the node's new position as "position n of m".
