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
- The root holds a row element and, when `ChildContent` is set, a nested `ul`
  for the children.
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
- The children group is hidden while the node is collapsed and shown while it is
  expanded.
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
- The row is set in `--fg-muted` at rest and `--fg` on hover, over `--glass-2`.
- The selected row is filled with `--accent-soft` and set in `--fg`.
- The selected row's icon is drawn in `--accent-a`, so selection is marked twice
  — by fill and by icon colour — rather than by colour alone.
- The chevron and the icon are drawn in `--fg-dim`, quieter than the label.
- The row's corner comes from `--r-sm` and the chevron's from `--r-xs`.
- The row shows a focus ring when focused by keyboard.
- The row's own outline is suppressed in favour of that ring.
- The row sits in the flow rather than floating, so it carries no frost
  (`DESIGN-06`).
- The accent appears as a soft fill behind one row and the colour of one small
  icon, never as the fill of a large surface (`DESIGN-08`).
- The component uses its own isolated stylesheet rather than adding to the
  global one.
- The component branches on no color mode and holds no mode-assuming value, so
  the same markup serves light and dark (`DESIGN-02`).

### Motion

- The row transitions its fill and its text colour between rest, hover and
  selected.
- The chevron rotates between its collapsed and expanded positions.
- Both transitions run at `--dur-fast` with `--ease-out`.

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
- **`Disabled` only disables selection.** A disabled node still takes keyboard
  focus, still counts as a stop in the arrow-key walk, still expands and
  collapses, and can still be the roving-`tabindex` target. It reports
  `aria-disabled`, so assistive technology is told something the component does
  not enforce.
- **The focus ring is drawn from `--accent-soft`.** That token is a heavily
  transparent tint intended for fills, so the keyboard focus indicator on a tree
  row is far fainter than a focus indicator should be — and the component
  suppresses the browser's own outline in its favour. This is the one appearance
  criterion above whose *result* is questionable even though its token is legal.
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
- **Expansion is not animated.** The children group is shown and hidden by an
  attribute, with no height or opacity transition and no `DrylPresence`
  (`DESIGN-12`) — recorded on the view side in `F20` as well, because it is the
  tree's main gesture and neither half owns it today.
- **No tests of its own.** None of the criteria above is guarded by a test —
  in particular not the parameter-versus-toggle rule, which is the component's
  most subtle behaviour and the one a future refactor is most likely to break.

## Cross-cutting evidence (`SPEC-05`)

- **Both color modes** — token-only colors, verified by
  `node scripts/check-light-sync.mjs` and
  `node scripts/validate-light-contrast.mjs`. `--fg`, `--fg-muted`, `--fg-dim`,
  `--glass-2`, `--accent-soft` and `--accent-a` are the mode-dependent tokens;
  the component defines no mode-specific rule.
- **Enter/exit animation** — the row's hover and selection transitions and the
  chevron's rotation are the component's own motion; the expansion itself is
  **not** animated and is recorded above as debt rather than as an exception.
- **Keyboard and a11y** — the "Keyboard" and "Keyboard and accessibility"
  criteria above. The substantive decisions are the full arrow-key contract and
  the leaf that reports no `aria-expanded`; the substantive omissions are the
  under-enforced `Disabled` and the faint focus ring, both recorded above.
- **AI mode** — explicitly no, with the reason under "AI mode" above.
- **Demo page** — shown on `DRYL.Website/Components/Pages/DemoTreeView.razor`
  through the example `Components/Examples/TreeView/Files.razor`.
- **`ComponentCatalog`** — reached through the `"Tree View"` / `tree` entry in
  `DRYL.Website/Components/ComponentCatalog.cs`; the catalog registers the lead
  component of a family and not its parts.
