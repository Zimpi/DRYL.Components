# DrylTreeView

## Meta
- **State:** Implemented
- **Source:** code/DRYL.Components/Components/Data/DrylTreeView.razor
              code/DRYL.Components/Components/Data/DrylTreeView.razor.css

## User Story

As a Blazor developer, I want a hierarchy I can write as nested markup and
navigate with the arrow keys, so that a file browser, a category picker or a
document outline behaves the way a tree is expected to behave without me
implementing the WAI-ARIA pattern myself.

## Description

`DrylTreeView` is the owner half of a two-part tree. It is declarative: a
consumer nests `DrylTreeNode` elements (`F21`) and the view discovers the shape
from the nodes that register with it.

The division of labour is the component's central decision. **The view owns
selection and focus; each node owns its own expansion.** Selection has to be
central because there is one selected node in a tree and it is two-way bound.
Focus has to be central because a tree is one tab stop with a roving
`tabindex`, and only something that can see every node can decide which one
carries it. Expansion, by contrast, is per node, is toggled by the user far more
often, and is two-way bound per node.

Keyboard navigation is the WAI-ARIA tree pattern: the arrow keys move through
the *visible* nodes — a pre-order walk that steps into expanded parents and over
collapsed ones — `ArrowRight` expands or steps in, `ArrowLeft` collapses or
steps out, `Home` and `End` jump to the ends, and `Enter` or `Space` selects.

One interop call supports this and does one thing: it stops the browser from
scrolling the page when the navigation keys are pressed, so the component's own
key handler can move focus instead. The tree's `keydown` handling itself is
Blazor's.

## Public API

| Member | Type | Default | Purpose |
|---|---|---|---|
| `SelectedValue` | `object?` | `null` | Value of the selected node. Supports `@bind-SelectedValue`. |
| `SelectedValueChanged` | `EventCallback<object?>` | — | Raised when the selection changes. |
| `ChildContent` | `RenderFragment?` | `null` | The root `DrylTreeNode` elements. |
| `AriaLabel` | `string` | `"Tree"` | Accessible label for the tree. |
| `Class` | `string?` | `null` | Extra CSS class(es) merged onto the tree's own class. |
| `AdditionalAttributes` | `IDictionary<string, object>?` | `null` | Pass-through attributes on the root element. |

The component takes **no** `Ai` and no `Aura` — see "AI mode" below.

## Acceptance Criteria

### Structure

- The component renders a `ul` as its root, so the hierarchy is a list in the
  document rather than a stack of divs.
- `ChildContent` is rendered inside a cascading value that hands the view itself
  to its nodes.
- The cascade is fixed, so a node never re-subscribes to it.
- `Class` is merged onto the root's own class rather than replacing it.
- `AdditionalAttributes` are applied to the root.
- The root renders no list marker, because each node draws its own chevron.

### Membership

- A node registers itself with the view when it initialises.
- Registering a node that is already registered changes nothing.
- A node unregisters itself when it is disposed.
- Unregistering the selected node clears the selection, so a removed node does
  not stay selected in absentia.
- Unregistering the focused node clears the focus target, so the roving
  `tabindex` falls back rather than pointing at a node that is gone.
- The view derives a node's children from the nodes that registered with it,
  rather than from the markup.

### Selection

- The view holds exactly one selected node at a time.
- Selecting a node raises `SelectedValueChanged` with that node's `Value`.
- Selecting a node also makes it the focus target, so the arrow keys continue
  from where the user clicked.
- A `SelectedValue` supplied before the nodes exist selects the matching node as
  soon as it registers, so a tree can be pre-selected by value.
- A `SelectedValue` changed to a value matching a registered node moves the
  selection to it.
- Values are compared by equality rather than by reference, so a `string` or a
  record works as a node value.
- Changing the selection re-renders every node, not only the view, so the
  previously selected node loses its highlight.

### Focus and keyboard

- The tree is a single tab stop: exactly one node carries `tabindex="0"` and
  every other carries `-1`.
- The focus target defaults to the focused node, then the selected node, then
  the first visible node.
- `ArrowDown` moves focus to the next visible node.
- `ArrowUp` moves focus to the previous visible node.
- `Home` moves focus to the first visible node.
- `End` moves focus to the last visible node.
- Movement is over *visible* nodes only — a pre-order walk that descends into
  expanded parents and skips the subtrees of collapsed ones.
- Moving to the first or last node from an end of the list is a no-op rather
  than a wrap-around.
- Moving focus re-renders every node, so the roving `tabindex` is correct on all
  of them before the browser moves focus.
- The browser's default page scroll is suppressed for the navigation keys, so
  arrowing through a long tree does not scroll the page.
- `Tab`, `Enter` and `Escape` are left to the browser and to the node's own
  handler, so focus can still leave the tree.

### Interop and cleanup

- The key-suppression handler is attached once, on the first render.
- The handler is detached when the component is disposed.
- Detaching is skipped entirely when nothing was ever attached, so a statically
  rendered tree does not attempt interop while being torn down.
- Detaching survives a disconnected circuit, a missing element and a static
  render without throwing.

### Keyboard and accessibility

- The root carries `role="tree"`.
- The root carries `AriaLabel` as its accessible label.
- `AriaLabel` has a non-null default, so a tree is never an unlabelled widget.
- The roving `tabindex` is what makes the tree one stop in the page's tab order,
  as the WAI-ARIA tree pattern requires.

### Appearance

- The component renders no colour of its own and therefore names no literal
  colour (`DESIGN-01`); the colours belong to the nodes.
- The tree paints no surface of its own — no fill, no border, no frost — so it
  inherits whatever ground it is placed on and `DESIGN-06` has nothing to apply
  to.
- The component renders no accent, so `DESIGN-08` has nothing to apply to.
- The component uses its own isolated stylesheet rather than adding to the
  global one.
- The component branches on no color mode and holds no mode-assuming value, so
  the same markup serves light and dark (`DESIGN-02`).

### AI mode

- The component takes no `Ai` parameter and renders no aura.
- The decision is deliberate (`AI-05`): a tree is a navigation structure the
  user drives, and the view paints no surface to put an aura on. Where a subtree
  is being generated — an agent building a file layout, say — the state belongs
  to the individual node, and `DrylTreeNode` does not carry it either. That is
  the honest position: the tree family currently has no route to the AI
  vocabulary, and adding one to the node would be a new feature, not a fix.

## Recorded gaps

- **Clearing `SelectedValue` does not deselect.** The view adopts an externally
  supplied `SelectedValue` only when it is non-null, so setting the bound value
  back to `null` leaves the previously selected node highlighted and still
  reported as selected. The only way to clear a selection is to remove the node.
- **Every node costs the view a render.** Registration calls `StateHasChanged`
  unconditionally, so mounting a tree of *n* nodes queues *n* extra renders of
  the view and its whole subtree during the first render pass — and each of
  those re-renders every node explicitly. It is the same gap `F2` records for
  the avatar group, at a size where it matters more.
- **`SelectedValue` is written to from inside the component.** Selection assigns
  the parameter directly before raising its callback, which Blazor's parameter
  contract does not sanction. It works with `@bind-SelectedValue` and it is what
  makes the pre-selection lookup in `OnParametersSet` see a consistent value —
  but a consumer who binds only `SelectedValueChanged` and re-supplies
  `SelectedValue` from their own state will find the component briefly
  disagreeing with them.
- **No type-ahead.** The WAI-ARIA tree pattern expects typing a character to
  move focus to the next node starting with it, which is how a user finds a node
  in a long list. The tree handles the arrow keys, `Home`, `End`, `Enter` and
  `Space` and nothing else.
- **No expand-all or collapse-all key.** The pattern's `*` on a level, and the
  `ArrowLeft`-to-root behaviour on a root node, are not implemented.
- **Nothing about the view is animated.** The rows transition their colours and
  the chevron rotates — both the node's — but expanding a subtree shows and
  hides it instantly, with no height or opacity transition and no `DrylPresence`
  (`DESIGN-11`, `DESIGN-12`). Expansion is the tree's main gesture.
- **No tests of its own.** None of the criteria above is guarded by a test —
  not the visible-node walk, not the roving `tabindex`, not the pre-selection by
  value, and not the interop attach/detach pair, which is the part with a
  known-recurring prerender failure mode in this repository.

## Cross-cutting evidence (`SPEC-05`)

- **Both color modes** — the component names no colour at all, so
  `node scripts/check-light-sync.mjs` and
  `node scripts/validate-light-contrast.mjs` have nothing of its own to check;
  the mode-dependent tokens are the nodes'.
- **Enter/exit animation** — **absent**, and recorded above as debt rather than
  as an exception.
- **Keyboard and a11y** — the "Focus and keyboard" and "Keyboard and
  accessibility" criteria above. This is the category's most substantial
  keyboard implementation: a real roving `tabindex` over a real visible-node
  walk. What it is missing against the full WAI-ARIA pattern — type-ahead and
  the expand-all keys — is recorded above.
- **AI mode** — explicitly no, with the reason under "AI mode" above.
- **Demo page** — `DRYL.Website/Components/Pages/DemoTreeView.razor`, with the
  example `Components/Examples/TreeView/Files.razor`.
- **`ComponentCatalog`** — registered as `"Tree View"` / `tree` in
  `DRYL.Website/Components/ComponentCatalog.cs`, flagged not AI-capable.
