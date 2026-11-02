# DrylTreeNode

## Meta
- **State:** Implemented
- **Source:** code/DRYL.Components/Components/Data/DrylTreeNode.razor
              code/DRYL.Components/Components/Data/DrylTreeNode.razor.css

## User Story

As a Blazor developer, I want a tree node I can nest inside another node to
build a hierarchy, and that keeps a user's expand/collapse gesture even when the
page around it re-renders, so that a tree does not snap shut while the user is
working in it.

## Description

`DrylTreeNode` is one row of a `DrylTreeView` (`F20`), and it becomes a parent
simply by having nodes nested inside it — there is no "is folder" parameter.
Each node renders its own row: a chevron if it has children and a spacer if it
does not, an optional icon, and the label. Its depth is derived by walking its
ancestors and is published to the stylesheet, so the indent is one rule rather
than one class per level.

The subtlety worth naming is **how the node holds its expansion**. `Expanded` is
two-way bindable, so a consumer can open a branch programmatically — but a bare
re-render of the parent re-supplies the same literal value, and naively adopting
it would slam a user's open branch shut on every unrelated render. The node
therefore tracks the last value it *received as a parameter* separately from the
value it is *currently showing*, and adopts the parameter only when the consumer
actually changes it. A user's toggle survives; a consumer's assignment wins.

Selection and focus are not the node's: they belong to the view, which re-renders
each node when either moves. The node contributes the keyboard handler, because
the key event arrives on the row it rendered.

## Public API

| Member | Type | Default | Purpose |
|---|---|---|---|
| `Text` | `string?` | `null` | Node label. |
| `Icon` | `string?` | `null` | `DrylIcon` name shown before the label. |
| `Value` | `object?` | `null` | Value used for the tree's selection binding. |
| `Expanded` | `bool` | `false` | Whether the node is expanded. Supports `@bind-Expanded`. |
| `ExpandedChanged` | `EventCallback<bool>` | — | Raised when the expanded state changes. |
| `Disabled` | `bool` | `false` | Prevents selection of the node. |
| `ChildContent` | `RenderFragment?` | `null` | Child nodes. |

The component has **no** `Class` and **no** `AdditionalAttributes` — see
"Recorded gaps". It takes no `Ai` and no `Aura` — see "AI mode" below.

## Acceptance Criteria

### Structure

- The component renders an `li` as its root.
- The root holds a row element and, when `ChildContent` is set, a fold holding a
  nested `ul` for the children.
- The row holds a chevron or a spacer, an optional icon and the label, in that
  order.
- A node with registered children renders a chevron.
- A node without registered children renders a spacer of the same size, so
  labels at one level line up whether or not their siblings have children.
- `Icon` set renders one `DrylIcon` in the row.
- `Icon` unset renders no icon element.
- `Text` is rendered as the row's label.
- The label truncates with an ellipsis rather than wrapping, so a deep tree does
  not grow rows of different heights.
- The node's depth is published to the stylesheet as a custom property on the
  row, and the indent is derived from it.
- The node's index among its siblings is published the same way, for the stagger.
- The group publishes its parent's depth, so its guide line and thread sit under
  the parent's chevron.
- A node placed outside a `DrylTreeView` throws on initialisation rather than
  rendering a broken row.

### Expansion

- The node adopts `Expanded` as its initial state.
- Toggling the chevron flips the node's expanded state.
- Toggling raises `ExpandedChanged` with the new state, when a handler is bound.
- Toggling notifies the view, so the visible-node walk and the roving `tabindex`
  account for the change.
- A re-render that re-supplies the same `Expanded` value does not reset a user's
  toggle.
- A change of `Expanded` by the consumer does override the user's toggle.
- The children group stays in the document while the node is collapsed, in a fold
  that takes no room and is `visibility: hidden`, so nothing in it can take focus.
- An open subfolder of a closed folder is hidden with it.
- The chevron press does not also select the node, so opening a folder and
  choosing it are separate gestures.

### Selection

- The row reports itself as selected when the view says it is.
- Clicking the row selects the node.
- Clicking the row of a disabled node selects nothing.

### Keyboard

- `ArrowDown` and `ArrowUp` ask the view to move focus to the next and previous
  visible node.
- `Home` and `End` ask the view to move focus to the first and last visible
  node.
- `ArrowRight` on a collapsed parent expands it.
- `ArrowRight` on an expanded parent moves focus to its first child.
- `ArrowRight` on a node with no children does nothing.
- `ArrowLeft` on an expanded parent collapses it.
- `ArrowLeft` on a collapsed node or a leaf moves focus to its parent.
- `ArrowLeft` on a root-level node with nothing to collapse does nothing.
- `Enter` and `Space` select the node.
- `Enter` and `Space` on a disabled node select nothing.
- The row is focusable programmatically, so the view can move focus onto it.

### Keyboard and accessibility

- The root carries `role="treeitem"`.
- The children group carries `role="group"`.
- A node with children carries `aria-expanded` reflecting its state.
- A node without children carries no `aria-expanded`, because a leaf has no
  expanded state to report.
- Every node carries `aria-selected` reflecting whether it is the selected one.
- A disabled node carries `aria-disabled`.
- The chevron and the icon are hidden from assistive technology, so the node is
  announced by its label.
- The row carries `tabindex="0"` only when the view names it the focus target,
  and `-1` otherwise.

### Appearance

- Every color the component renders comes from a token; the component names no
  literal color (`DESIGN-01`).
- The row is set in `--fg-muted` at rest and `--fg` on hover.
- Under the mouse the row takes a quiet `--glass-1` fill and a soft accent light
  that follows the pointer; touch never lights a row. Without script the row keeps
  a static `--glass-2` hover fill.
- The selected row is set in `--fg` and drawn by the view's ink (`F20`); it gives
  up its own fill while the ink exists. Without script it is filled with
  `--accent-soft`.
- The selected row's icon is drawn in `--accent-a`, so selection is marked twice
  — by the ink and by icon colour — rather than by colour alone.
- The rows on the path to the selection are set in `--fg`.
- A disabled node's row is dimmed and shows a `not-allowed` cursor.
- Each open group has a 1 px guide line in `--line-strong` under its folder's
  chevron.
- Each group on the path to the selection has a thread: a 1 px elbow from the
  group's top to the row on the path, with a small glow, mixed from `--accent-a`
  towards `--accent-b` by depth. Without script it never shows.
- The chevron and the icon are drawn in `--fg-dim`, quieter than the label.
- The row's corner comes from `--r-sm` and the chevron's from `--r-xs`.
- The row shows an inset `--accent-line` focus ring when focused by keyboard. It
  is inset because the fold's clip would cut an outer ring off.
- The row's own outline is suppressed in favour of that ring.
- The row sits in the flow rather than floating, so it carries no frost
  (`DESIGN-06`).
- The accent appears as 1 px lines, one small icon and the view's ink, never as
  the fill of a large surface (`DESIGN-08`).
- The component uses its own isolated stylesheet rather than adding to the
  global one.
- The component branches on no color mode and holds no mode-assuming value, so
  the same markup serves light and dark (`DESIGN-02`).

### Motion

- The row transitions its fill and its text colour between rest, hover and
  selected, at `--dur-fast` with `--ease-out`.
- The chevron rotates between its collapsed and expanded positions on `--dur-med`
  with `--ease-spring`.
- A fold opens by its grid track from `0fr` to `1fr` on `--dur-slow` with
  `--ease-out`, and closes on `--dur-med` with `--ease-in-out`.
- The guide line draws itself from the top as the fold opens and withdraws as it
  closes.
- The children of a fold the user opens slide out of the guide line and fade in
  one after another, on `--dur-med` with `--ease-out`.
- The stagger step is half of `--reveal-step` and is capped at the seventh row, so
  the last row lands as the height finishes, however long the folder is.
- The contents of a row move, not its box, so the ink can measure the row at any
  moment.
- A tree that renders open plays no stagger.
- `prefers-reduced-motion: reduce` switches the fold, the guide line, the stagger,
  the chevron and the light off (`UX-06`).

### AI mode

- The component takes no `Ai` parameter and renders no aura.
- The decision follows `F20` (`AI-05`): the tree family has no route to the AI
  vocabulary at all. That is recorded as a deliberate present-tense "no" rather
  than as an oversight — adding one would be a new feature and belongs in the
  idea stage.

## Recorded gaps

- **No `Class`, no `AdditionalAttributes`.** The node is one of the two
  components in the category that carry neither, so a consumer cannot attach a
  test hook, a `data-*` attribute or a style class to one node. `F9` records the
  other; both are holes in the library-wide `Class` rollout.
- **`Disabled` only disables selection.** A disabled node is dimmed, but it still
  takes keyboard focus, still counts as a stop in the arrow-key walk, still expands
  and collapses, and can still be the roving-`tabindex` target. It reports
  `aria-disabled`, so assistive technology is told something the component does
  not enforce.
- **A node with non-node children renders an empty group.** The children `ul` is
  rendered whenever `ChildContent` is set, but the chevron and `aria-expanded`
  are driven by whether any child *node* registered. Putting anything else in a
  node's content produces a `role="group"` with no treeitems in it and a leaf
  that owns a group.
- **The indent step and the row's metrics are literal.** The per-level indent,
  the row's vertical padding, the chevron's box and the label's type size are
  written into `DrylTreeNode.razor.css` with no token behind them
  (`DESIGN-01`). The gaps and radii *are* tokens, so the file is half-converted
  rather than untouched.
- **The arrow keys have no test of their own.**
  `tests/DRYL.Components.Tests/DrylTreeViewTests.cs` guards the fold, the stagger
  marker and the parameter-versus-toggle rule; the node's key handler is only
  checked in the browser.

## Cross-cutting evidence (`SPEC-05`)

- **Both color modes** — token-only colors, verified by
  `node scripts/check-light-sync.mjs` and
  `node scripts/validate-light-contrast.mjs`. `--fg`, `--fg-muted`, `--fg-dim`,
  `--glass-2`, `--accent-soft` and `--accent-a` are the mode-dependent tokens;
  the component defines no mode-specific rule.
- **Enter/exit animation** — the fold, the guide line and the stagger under
  "Motion". The children stay mounted, so `DESIGN-12` has nothing to wrap.
- **Keyboard and a11y** — the "Keyboard" and "Keyboard and accessibility"
  criteria above. The substantive decisions are the full arrow-key contract and
  the leaf that reports no `aria-expanded`; the substantive omission is the
  under-enforced `Disabled`, recorded above.
- **AI mode** — explicitly no, with the reason under "AI mode" above.
- **Demo page** — shown on `DRYL.Website/Components/Pages/DemoTreeView.razor`
  through the example `Components/Examples/TreeView/Files.razor`.
- **`ComponentCatalog`** — reached through the `"Tree View"` / `tree` entry in
  `DRYL.Website/Components/ComponentCatalog.cs`; the catalog registers the lead
  component of a family and not its parts.
