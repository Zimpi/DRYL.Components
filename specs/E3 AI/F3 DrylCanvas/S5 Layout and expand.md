# Layout and expand

## Meta
- **State:** Implemented

Fullscreen, the morph that gets there, and the width the artifact is allowed to
budget for.

## Acceptance Criteria

### Expand

- The header offers an expand button exactly when `AllowExpand` is `true`.
- Activating it expands the canvas to cover the viewport; activating it again
  collapses it.
- The expanded canvas is an **overlay, not a modal**: it neither traps focus nor
  blocks the page behind it.
- <kbd>Escape</kbd> collapses an expanded canvas.
- <kbd>Escape</kbd> does nothing while the canvas is inline, so it never
  swallows the key from a surrounding dialog or palette.
- The expand button reflects the current state through `aria-pressed`, and its
  label reads "Expand artifact" or "Exit fullscreen" accordingly.
- `AllowExpand="false"` is the setting for a canvas embedded in a surface that
  owns its own layering — a dialog, a fixed side panel.

### The top layer

- An expanded canvas is promoted to the browser's top layer, so it really
  covers the viewport rather than only the stacking context it sits in.
- The promotion uses `popover="manual"`, not `"auto"`: the component owns
  <kbd>Escape</kbd> and must stay in step with its own expanded flag, so
  light-dismiss behind its back is not allowed.
- A browser without the Popover API ignores the attribute and the CSS fallback
  positions the overlay instead.
- The promotion happens before the render is signalled, so the browser
  snapshots the finished state.

### Motion

- The morph between inline and fullscreen runs as a view transition, so the CSS
  describes only the destination and never the movement (`DESIGN-13`).
- Each canvas instance carries a document-unique `view-transition-name`; a
  duplicate name would void the entire transition, so two canvases on one page
  do not collide.
- Nodes appearing, disappearing or reordering in the body glide rather than
  snap, through `dryl.motion.autoFlip` on the body element.
- The FLIP observer is detached on disposal (`CODE-05`).
- The canvas signals every completed render to `IDrylViewTransition`, so a
  transition a wrapper started around a spec swap also completes.

### Width reporting

- The usable body width is measured and reported through `OnWidthChanged`
  whenever it moves past a deadband, so an artifact author can budget its layout
  for the space it really has.
- The reported width is in CSS px.
- Reporting the width never re-renders the artifact tree — nothing on screen
  depends on the value, and dragging a window must stay cheap.
- The resize observer is detached and its module disposed on disposal
  (`CODE-05`).

### Prerender and disconnect

- Every JS call is guarded, so the component prerenders on the server without a
  circuit and survives a circuit that has gone away.
- A failed interop call leaves no state claiming the DOM was touched — the FLIP
  and reorder teardown run only when the corresponding setup succeeded.
