# Row reordering and motion

## Meta
- **State:** Implemented

## Acceptance Criteria

### When reordering is possible

- `Reorderable` set renders a leading grip column.
- The grip column is rendered only over a plain client-side list: virtualization,
  grouping and a `DataProvider` each suppress it, because none of them keeps a
  1:1 mapping between a displayed row and a stable position.
- A suppressed configuration is reported once on the console.
- The grip is rendered but inoperable while any sort is applied, and its tooltip
  says to clear the sort — a manual order over a sorted view is meaningless, and
  a grip that vanished would be harder to understand than one that explains
  itself.

### Dragging

- Dragging a grip marks its row as the one being dragged.
- Dragging over another row marks that row as the drop target.
- Dropping moves the dragged row to the target's position.
- The drag highlight is cleared before the move, so no drop-target styling
  lingers through the animation.
- Ending a drag without dropping clears the highlight and moves nothing.
- A drop outside the valid range is clamped rather than ignored.
- Moving a row to its own position does nothing.

### The keyboard path

- `Alt` with the up or down arrow on a focused grip moves the row one position.
- A move at either end of the list is a no-op rather than a wrap-around.
- Focus follows the moved row to its new position, so repeated presses keep
  moving the same row.
- The grip carries an accessible label naming its position, the total, and the
  key combination that moves it.

### What a move does

- A move reorders the displayed view immediately, before the consumer is
  notified, so the table never appears to ignore the gesture.
- A move raises `OnRowReordered` with the old and the new index.
- The consumer's backing collection is theirs to update; the table's optimistic
  reorder is replaced by the next rebuild.

### The morph

- `AnimateReorder` set morphs rows between their old and new positions using a
  same-document morph.
- The morph is available only under the same constraints as reordering itself,
  minus the sort lock — a re-sort morphs too.
- Every row carries a view-transition name while a morph mode is active, and
  none when neither is, so the browser has stable per-row targets to glide
  between.
- A row's transition name is derived from `RowIdSelector` when one is supplied
  and from the item's hash code otherwise.
- The name is sanitised to a valid CSS identifier, so an item whose id contains
  punctuation does not abort the transition.
- The name is scoped per table instance, so two morph-enabled tables on one page
  cannot collide on a document-global name.
- A mutation that arrives while a transition is in flight is applied directly
  rather than starting a second one, because morphs serialise and the
  overlapping start would only be skipped.
- The mutation is guaranteed to run even when the transition never calls back —
  during prerender, on a disconnected circuit, or under a test renderer.
- A browser without morphs falls back to applying the mutation
  directly.

### The streaming glide

- `AiState.Streaming` and `AiState.Generated` turn the morph on without
  `AnimateReorder` being set, under the same three constraints.
- The glide runs only when the incoming `Items` actually carry rows the table
  has not rendered yet, so a re-handed reference with unchanged content does not
  pay for a no-op morph.
- The rows the table has rendered are recorded after each render, which is what
  makes that comparison possible.
- A row the table has not seen before, while streaming, is marked so it can be
  animated in by CSS — which is the fallback when the browser cannot morph.
- A rebuild caused only by a column change never morphs.

## Recorded gaps

- **The hash-code fallback can collide.** Without a `RowIdSelector`, two rows
  whose items share a hash code get the same view-transition name. A duplicate
  name aborts the whole transition, so the symptom is not a wrong animation but
  no animation at all, intermittently and without a message.
- **Reordering is drag-and-drop first.** The keyboard path exists and is
  labelled, which is more than most tables offer — but it is `Alt`+arrow on a
  grip the user has to find and focus first, and there is no announcement of the
  new position after a move.
- **Nothing announces a move.** Neither the drag nor the keyboard path updates a
  live region, so a screen-reader user gets no confirmation that a row moved or
  where it went.
- **The optimistic reorder can disagree with the consumer.** The view is
  reordered before `OnRowReordered` is raised; a consumer who ignores the event,
  or applies a different move, sees the table's order revert on the next
  rebuild, with the rows visibly jumping back.
- **The drop target is the row entered last, not a gap.** There is no insertion
  indicator between rows, so dropping onto a row is ambiguous about whether the
  dragged row lands above or below it until it happens.
