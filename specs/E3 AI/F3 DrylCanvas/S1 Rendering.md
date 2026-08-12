# Rendering

## Meta
- **State:** Implemented

The header, the body, and the three things the body can be: an artifact, an
error, or nothing yet.

## Acceptance Criteria

### Header

- The header renders `Spec.Title` as the artifact title.
- The header renders "Artifact" as the title when `Spec` is `null`.
- The header renders "Artifact" as the title when `Spec.Title` is `null`. A
  `Spec.Title` that is present but empty renders as empty — the fallback is on
  `null`, not on blank.
- The header renders `HeaderTools` in the tool row, before the built-in buttons.
- The header renders the refresh button only when the artifact has at least one
  data binding (see `S3`).
- The header renders the expand button only when `AllowExpand` is `true` (see
  `S5`).

### Body

- The body renders a danger `DrylAlert` titled "Artifact failed" when `Error` is
  non-null.
- The body renders the alert instead of the tree — an artifact with an error
  shows no partial tree.
- The body renders the spec's root through `CanvasNodeView` when `Spec.Root` is
  non-null and `Error` is null.
- The body renders a `DrylEmptyState` carrying `EmptyText` when there is no
  `Spec.Root` and no `Error`.
- `EmptyText` defaults to "Nothing to show yet.".
- The shared `CanvasContext` is cascaded to the tree as a single `IsFixed`
  cascade, so a node view never re-renders because the context object was
  replaced.

### Spec ownership

- The canvas applies every patch through one `CanvasPatcher`, whoever authored
  it — an AI `setProps` and a user's duplicate take the same path.
- A `setProps` patch stamps the node's id into the pulse tracker; other ops do
  not.
- Replacing `Spec` with a different instance resets the data binder, so nothing
  bound to the previous artifact keeps loading or shows a stale value.
- Replacing `Spec` with a different instance clears the selection, because a
  selection into the old artifact means nothing in the new one.
- Bumping `Epoch` clears the interactive form state in place, so a fresh
  artifact that recycles field names shows the new values rather than the
  previous user input.

### Purging

- A node that finished its exit animation is reported through `OnPurge` when the
  host handles it.
- The canvas removes the node from the tree itself when `OnPurge` has no
  delegate — a canvas fed from code has no host to do it, and the node would
  otherwise linger invisibly.
- Purging a node bumps its parent's version, so the tree re-renders.

### Announcements

- The canvas renders the host's `Announcement` in an `aria-live="polite"` region
  (`UX-04`).
- Selection messages are announced in a **second**, separate `aria-live` region,
  so a selection message never overwrites what the AI just announced, and vice
  versa.
- Both live regions are visually hidden.

### Appearance

- Every color, radius, spacing, duration and easing resolves to a token; the
  component writes no literal (`DESIGN-01`).
- The component branches on no color mode and holds no mode-assuming value, so
  the same markup serves light and dark (`DESIGN-02`).
- The root carries `glass-card`, so the canvas is a floating surface with the
  frost that goes with it (`DESIGN-06`).
