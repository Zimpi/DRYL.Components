# Overlay, motion and loading

## Meta
- **State:** Implemented

## Acceptance Criteria

### The overlay

- While `Open` is `false` and no exit is playing, the component renders no
  overlay markup — only its `ChildContent`.
- Setting `Open` to `true` mounts the overlay.
- The overlay is moved to `document.body` once it is mounted, so it sits in the
  root stacking context wherever the palette is placed.
- The overlay layer carries `--z-modal`.
- The backdrop is the library's dialog backdrop (`.dialog-backdrop`).
- The panel sits towards the top of the viewport, not in its centre, so a
  software keyboard does not cover the results.
- The panel uses `--panel-grad-strong`, `--glass-fx-float`, `--line-strong` and
  `--shadow-lg` (`DESIGN-06`).
- The panel never exceeds the viewport width less `--sp-6`, so it keeps a gutter
  on a phone.
- The component branches on no colour mode; both modes are served by the same
  tokens (`DESIGN-02`), verified by eye in light and dark.

### Enter and exit

- Opening fades the backdrop in at `--dur-med` with `--ease-out`.
- Opening scales and lifts the panel in at `--dur-med` with `--ease-spring`.
- Closing fades the backdrop out and scales the panel out at `--dur-med` with
  `--ease-out` before the overlay leaves the DOM (`DESIGN-12`).
- While the exit plays the overlay takes no pointer input.
- Reopening while the exit plays cancels the exit and plays the enter again.
- The backdrop's blur is present for the whole fade.

### Rows entering and leaving

- A row whose key was not shown before grows in from no height while it fades
  and rises, at `--dur-med` with `--ease-out`.
- A row whose key is still part of a new result set keeps its element and plays
  no animation.
- A row whose key is missing from a new result set stays in place, collapses and
  fades out at `--dur-fast` with `--ease-out`, then is removed.
- A leaving row is not an option: it carries no `role="option"`, no `id`, is
  hidden from assistive technology and takes no pointer input.
- A group whose rows are all leaving keeps its header until its last row has
  left.
- The rows of the first result set after opening enter staggered by
  `--reveal-step`, at most eight steps.
- Because rows grow and collapse to their own height, the list changes height
  smoothly instead of snapping.
- The empty state fades in, and collapses out when results arrive.

### Loading

- While the first result set of an opening is loading from `SearchProvider`, the
  list shows a `DrylSkeleton` in the shape of result rows (icon, label line,
  description line).
- The palette shows no spinner while loading.
- When the first result set arrives the skeleton collapses out while the rows
  grow in.
- While a later query loads, the previous rows stay visible, quieted to a lower
  opacity, until the new rows arrive.
- The list carries `aria-busy="true"` while a search is loading.
- A provider that throws ends the loading state; the list then shows what it had
  or the empty state.

### Reduced motion

- Under `prefers-reduced-motion: reduce` (`UX-06`) the overlay appears and disappears
  without animation.
- Under reduced motion rows enter without animation.
- Under reduced motion a dropped row is removed at once instead of leaving.
- Under reduced motion the quieting of stale rows is instant.
