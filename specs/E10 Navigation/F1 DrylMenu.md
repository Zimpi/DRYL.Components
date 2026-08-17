# DrylMenu

## Meta
- **State:** Implemented
- **Source:** code/DRYL.Components/Components/Navigation/DrylMenu.razor
              code/DRYL.Components/Components/Navigation/DrylMenuItem.razor

## User Story

As a Blazor developer building an application on DRYL, I want a dropdown of
actions hanging off a trigger I supply — with the roles a screen reader expects,
arrow-key navigation, and a close that puts focus back where the user left it —
so that I can offer "more actions" on a row, a toolbar or a card without writing
a menu's keyboard and ARIA behaviour myself.

## Description

`DrylMenu` is the library's action menu. It renders no surface, no anchor and no
positioning of its own: it places a `DrylPopover` and supplies the three things a
menu is that a popover is not — the `menu`/`menuitem` role pair, the keyboard
model, and a close driven by choosing an item.

**It is a consumer of `E11 Surfaces`/`F1 DrylPopover`, and this file does not
restate that component.** Everything about the panel node always being rendered,
the portal to `<body>`, the placement against the viewport, the two-key
visibility gate, the click-outside dismissal, the entrance and exit animations
and the trigger's `aria-haspopup`/`aria-expanded` claim belongs to
[`../E11 Surfaces/F1 DrylPopover.md`](../E11%20Surfaces/F1%20DrylPopover.md) and
is promised there. The reason for the split is the one `F3 DrylSplitButton`
sets out at length: a restatement of a dependency's behaviour goes stale in both
directions, when the dependency breaks and again when it is repaired. What is
promised below is `DrylMenu`'s own half — and where its half only works because
of the dependency's, the criterion says so without describing the dependency.

**The panel is the popover's, and the menu never touches it directly.** The
markup the consumer's `Items` produce are the panel's *direct children*:
measured on `/components/menu`, every `[role="menuitem"]` in an open panel has
the `role="menu"` element as its parent, with a `separator` between them where
one was asked for and nothing else in between. That flat shape is a requirement,
not an accident — `menu` owns `menuitem` directly, and a wrapper element between
them breaks the pair. It is also why the popover's exit animation runs on the
panel surface rather than on a presence wrapper around the content; the
alternative was measured and rejected for exactly this reason (see
[`../../ideas/I4 An exit animation for the popover surface.md`](../../ideas/I4%20An%20exit%20animation%20for%20the%20popover%20surface.md)).

**`DrylMenu` takes `Escape` from the popover and owes focus in return.** It
passes `CloseOnEscape="false"` and handles the key itself, because closing is
only half the job: focus is inside the panel, and the popover returns it
nowhere. So the menu returns it to the trigger — after `Escape`, and after an
item is chosen. Measured: `Escape` on an open menu closes it,
`document.activeElement` is the trigger button again and its `aria-expanded`
reads `false`; clicking an item gives the same three.

**Focus enters the panel on open, and the request has to wait for the portal.**
`DrylMenu` asks for it from its own `OnAfterRenderAsync`, which Blazor runs
before the child popover's — so the panel is not yet placed or revealed, and
`focus()` on an invisible element is silently a no-op. `dryl.menu.focusPanel`
therefore parks the request on the panel node and the popover applies it at the
moment it reveals the panel. Measured: opening by keyboard leaves focus on the
first item.

**The item is a `button`, and that decides more than it looks.** `DrylMenuItem`
renders `<button role="menuitem">`, so `Enter` and `Space` activate it through
the browser rather than through any handler of this component's, and `disabled`
is the real attribute rather than `aria-disabled`. Arrow navigation skips
disabled items for the same reason it can: they are excluded by the selector
that collects the items. Measured on the `File` menu, whose third of four items
is disabled — `ArrowDown` from the first goes `New` → `Open` → `Close`.

**Navigation clamps; it does not wrap.** `ArrowDown` on the last item stays
there and `ArrowUp` on the first stays there, rather than cycling. Measured
both. This is a deliberate difference from the WAI-ARIA menu pattern's
suggestion, and it is recorded under **Recorded debt** rather than defended
here.

## Public API

### `DrylMenu`

| Member | Type | Default | Meaning |
|---|---|---|---|
| `Trigger` | `RenderFragment?` | `null` | The content that opens the menu — typically a `DrylButton`. |
| `Items` | `RenderFragment?` | `null` | The menu body; `DrylMenuItem` components go here. |
| `Label` | `string?` | `null` | Accessible name of the menu panel. |
| `Placement` | `DrylMenu.MenuPlacement` | `BottomStart` | Where the panel opens relative to the trigger. |
| `Block` | `bool` | `false` | Stretches the anchor to its container's full width. |
| `Class` | `string?` | `null` | Merged onto the anchor's own classes. |
| `AdditionalAttributes` | `IDictionary<string, object>?` | `null` | Splatted onto the anchor. |

`MenuPlacement` — `BottomStart`, `BottomEnd`, `TopStart`, `TopEnd`. Nested in
`DrylMenu`, so a consumer writes `DrylMenu.MenuPlacement.BottomEnd`.

`DrylMenu` exposes no `Open` parameter and no open/close method: the open state
is private and the trigger is the only way in. There is no `OnOpen`/`OnClose`
either — see **Recorded debt**.

### `DrylMenuItem`

| Member | Type | Default | Meaning |
|---|---|---|---|
| `ChildContent` | `RenderFragment?` | `null` | The item's label. |
| `Icon` | `string?` | `null` | Icon name rendered before the label. |
| `Shortcut` | `string?` | `null` | Keyboard-shortcut hint at the trailing edge; cosmetic only. |
| `Variant` | `DrylMenuItem.MenuItemVariant` | `Default` | `Default` or `Danger`. |
| `Disabled` | `bool` | `false` | The item is shown but cannot be activated. |
| `Separator` | `bool` | `false` | Renders a divider instead of an item; every other parameter is ignored. |
| `Header` | `string?` | `null` | Renders a section header instead of an item. |
| `OnClick` | `EventCallback` | — | Raised when the item is activated. |
| `Class` | `string?` | `null` | Merged onto the item's own classes. |
| `AdditionalAttributes` | `IDictionary<string, object>?` | `null` | Splatted onto the item's `button`. |

## Acceptance Criteria

### Structure

- `DrylMenu` renders exactly one `DrylPopover` and no element of its own.
- `Trigger` is rendered into the popover's trigger slot.
- `Items` are rendered into the popover's panel slot.
- The panel's ARIA role is `menu`.
- `Label` is rendered as the panel's accessible name.
- The panel is unnamed when `Label` is `null`.
- Every item produced by `Items` is a direct child of the panel element, with
  no wrapper element between the `menu` and its `menuitem`s.
- `Items` receive the menu as a fixed cascading value, so an item can close its
  own menu without the consumer wiring anything.

### Placement and width

- `Placement` defaults to `MenuPlacement.BottomStart`.
- `Placement` accepts exactly the four values of `MenuPlacement`.
- Each `MenuPlacement` value opens the panel on the side and alignment its name
  states.
- `Block` defaults to `false`.
- The anchor fills its container's width while `Block` is `true`.

### The item

- An item renders as a `button` carrying `role="menuitem"`.
- An item's `button` is of `type="button"`, so an item inside a form submits
  nothing.
- `ChildContent` is rendered as the item's label.
- An item renders its `Icon` before the label when one is given.
- An item renders no icon when `Icon` is `null` or empty.
- An item renders its `Shortcut` after the label when one is given.
- The shortcut hint is hidden from assistive technology, being a duplicate of a
  key the user pressed rather than information (`UX-07`).
- `Variant` defaults to `MenuItemVariant.Default`.
- An item carries the danger modifier class exactly while `Variant` is
  `MenuItemVariant.Danger`.
- `Disabled` defaults to `false`.
- A disabled item is rendered and is visibly distinct, but cannot be activated.
- A disabled item raises no `OnClick`.
- A disabled item does not close the menu.
- An item with `Separator` renders a divider carrying `role="separator"`.
- A separator is hidden from assistive technology, the role and the reading
  order carrying what it means.
- A separator ignores every other parameter on the same item.
- An item with `Header` renders a non-interactive section label.
- A section header is hidden from assistive technology, because a `menu` owns
  `menuitem`s and a `div` between them would be announced as nothing useful.
- A header carries no `menuitem` role, so nothing lands on it during
  navigation.

### Activation

- Activating an item raises its `OnClick`.
- Activating an item closes the menu.
- The menu is closed before `OnClick` runs, so a handler that opens a dialog
  does not race the menu's own teardown.
- Activating an item returns focus to the trigger.
- `Enter` on a focused item activates it.
- `Space` on a focused item activates it.
- Activation by keyboard runs the item's own activation exactly once, the item
  being a real `button` rather than a handler bound to the panel.

### Keyboard

- Opening the menu moves focus to its first enabled item.
- Opening a menu that has no enabled item moves focus to the panel itself, so
  focus is never left behind on the trigger with an open menu on screen.
- `ArrowDown` moves focus to the next enabled item.
- `ArrowUp` moves focus to the previous enabled item.
- `Home` moves focus to the first enabled item.
- `End` moves focus to the last enabled item.
- Navigation skips disabled items in every direction.
- `ArrowDown` on the last item leaves focus where it is.
- `ArrowUp` on the first item leaves focus where it is.
- `Escape` closes the menu.
- `Escape` returns focus to the trigger.
- `Tab` closes the menu.

### Appearance and motion

- The panel paints the popover's default floating surface, so a menu looks like
  every other dropdown in the library without restating a single token.
- An item's hover and focus-visible states are drawn from the token set, with
  no colour of its own.
- A danger item is drawn in `--danger`.
- A disabled item is dimmed rather than hidden.
- The component branches on no colour mode and writes no mode-assuming value:
  both modes come from the token set alone (`DESIGN-02`).
- The panel's entrance and exit animations are the popover's, so a menu
  animates in and out without declaring any motion itself (`DESIGN-12`).

## Cross-cutting evidence (`SPEC-05`)

- **Both color modes** — neither `DrylMenu.razor` nor `DrylMenuItem.razor`
  carries a stylesheet; the item styles live in `dryl.css` (`.menu-item`,
  `.menu-item--danger`, `.menu-item-shortcut`, `.menu-separator`,
  `.menu-header`) and reference `--glass-2`, `--fg`, `--danger` and the spacing
  tokens. There is no `data-dryl-mode` selector and no colour literal in either
  component, so there is no mode branch to check. Checked by eye on
  `/components/menu` in both modes with a menu open: the panel is the popover's
  surface, and the items resolve their hover and danger colours per mode.
- **Enter/exit animation** — **yes**, and none of it is this component's. The
  panel is a `DrylPopover`, which animates in with `popover-in` and out with
  `presence-out-fade`, both `--dur-fast`, both suppressed under
  `prefers-reduced-motion`. `DrylMenu` adds no motion and needs none: it renders
  no element of its own that mounts conditionally, so `DESIGN-12` is satisfied
  by the surface it places rather than by an exception.
- **Keyboard and a11y** — the "Keyboard" and "Activation" criteria above, plus
  the role pair. Measured on `/components/menu` with the `Actions` menu: the
  trigger reads `aria-haspopup="menu"` and `aria-expanded="false"` while closed
  and `true` while open; opening by `Enter` puts focus on `Edit`, the first
  item; `ArrowDown` walks `Edit` → `Duplicate` → `Export` and stops there;
  `Home` returns to `Edit` and `ArrowUp` on it stays; `End` reaches `Export`;
  `Escape` closes the menu, returns focus to the trigger and puts
  `aria-expanded` back to `false`; clicking an item does the same three.
  Measured on the `File` menu: `ArrowDown` walks `New` → `Open` → `Close`,
  skipping the disabled `Save (no permission)`. Measured on the `Row actions`
  and `Edit menu` panels: the separator renders as
  `<hr role="separator" aria-hidden="true">` and the section header as an
  `aria-hidden` `div`, both as direct children of the `menu` alongside the
  `menuitem`s. One thing this component does **not** do is carried as debt
  below: `Tab` closes the menu and drops focus on `<body>`.
- **AI mode** — **no**, deliberately. `DrylMenu` declares no `Ai` parameter and
  does not inherit `DrylAiAware`. A menu has no action of its own for a model to
  be working on; it lists actions that belong to the page. The state a user
  needs to feel belongs to the control that opens it or to the surface the
  chosen action changes — which is where the library puts it, and which is why
  `DrylSplitButton` resolves the AI scope once and lights both of its segments
  while the menu it opens stays plain. The `ComponentCatalog` row agrees:
  `Menu` is registered with its AI flag `false` (`AI-05`).
- **Demo page** — its own page at `/components/menu`, built from six examples
  under `DRYL.Website/Components/Examples/Menu/`: `Basic`, `Placement`,
  `Headers`, `SeparatorDanger`, `Disabled` and `IconOnly`. Verified in the
  running site — seven menus on the page, every criterion above about the item
  variants was measured against them. What the page does **not** demonstrate is
  `Block` and `Label`. The page lives in `DRYL.Website`, a different
  repository; no acceptance criterion above is about it.
- **`ComponentCatalog`** — registered in
  `DRYL.Website/Components/ComponentCatalog.cs` as `"Menu"` / `menu`, group
  `Actions`, `ClassName` `"DrylMenu"`, category `Navigation`, AI flag `false`.
  Checked in the file, so the component reaches the sidebar, the Ctrl+K search
  and the `/components` overview under its own name (`REL-04`).
  `DrylMenuItem` has no row of its own, being usable only inside `DrylMenu`.

## Recorded debt (`State: Implemented`)

**Deviations from the acceptance criteria above: none.** Every criterion was
read off this code or measured in the running application today (`SPEC-04`).
What follows is the component's debt against the harness rules and against what
a consumer would reasonably expect.

- **`Tab` closes the menu and drops focus on `<body>` (`UX-01`).** Measured:
  with focus on an item, `Tab` closes the panel and `document.activeElement` is
  the `body`. The menu returns focus to the trigger on both of its other close
  paths, and this one was left out — so the key a keyboard user presses to *move
  on* is the one that loses their place entirely, and the next `Tab` starts from
  the top of the document. Returning focus to the trigger first would make
  `Tab` mean what it says; the fix is one line and is not taken here because it
  is behaviour, and behaviour goes through a spec before it goes into code.
- **Navigation clamps instead of wrapping.** `ArrowDown` on the last item and
  `ArrowUp` on the first do nothing at all. The WAI-ARIA menu pattern has them
  cycle, and a user who holds `ArrowDown` to reach the bottom of a long menu
  gets no signal that they have arrived. It is written into the criteria as
  behaviour rather than hidden, because changing it is a decision and not a fix.
- **The menu opens no way but its trigger.** There is no `Open` parameter, no
  `OpenChanged`, no `SetOpenAsync` and no `OnOpen`/`OnClose` — all of which the
  underlying `DrylPopover` has and this component deliberately does not
  forward. That is defensible for a menu, and it does mean a consumer cannot
  open one from a keyboard shortcut, close it when the page navigates, or learn
  that it opened. `DrylSplitButton` works around the last of these by having
  the popover claim the caret's ARIA from JS, because the open state is out of
  its reach.
- **No tests of its own.** `tests/DRYL.Components.Tests/` holds no
  `DrylMenuTests`. Everything above about focus, arrow navigation and the close
  paths rests on measurement in a browser, because bUnit executes no `dryl.js`
  and manages no real focus — and all four of those behaviours are in
  `dryl.menu`. What could be tested there is the render side: the role pair,
  the item variants, the separator and header shapes, the flat panel structure.
  None of it is.

### Recorded gaps — not deviations, and not what `State` rests on

Each of the following breaks a criterion of no spec and a rule of no number, or
belongs to another component's code and another component's spec.

- **`dryl.menu.attach` and `dryl.menu.detach` are dead code.** They install and
  remove a capture-phase `pointerdown` listener for click-outside dismissal.
  Nothing calls either: the popover took that duty over, and the pair was left
  behind. Checked across both projects — no `.razor` or `.cs` file references
  them. They are harmless, and they are the kind of leftover that reads as a
  mechanism to the next person to open the file.
- **Five menu rules in `dryl.css` match nothing.** `.menu-anchor`,
  `.menu-anchor--block`, `.menu-trigger`, `.menu-panel` (with its `--end` and
  `--top` modifiers) date from before this component was rebuilt on
  `DrylPopover`, which renders `.popover-anchor`, `.popover-trigger` and
  `.popover-panel` instead. Checked across the library and the docs site: no
  markup produces any of them. `dryl.menu.focusTrigger`'s selector still lists
  `.menu-trigger button` ahead of `.popover-trigger button`, and only the second
  half of it has ever matched.
- **The `Escape`/focus pairing is a convention, not a mechanism.** This
  component sets `CloseOnEscape="false"` and takes on the key *and* the focus
  return; nothing enforces the second half. It belongs to
  [`../E11 Surfaces/F1 DrylPopover.md`](../E11%20Surfaces/F1%20DrylPopover.md),
  which records it as its own debt, and is named here because this component is
  one of the six that took the deal.
- **`F3 DrylSplitButton` carries an account of this component's trigger ARIA
  and focus behaviour**, written when neither `E10` nor `E11` had a spec. The
  popover half now lives in `E11`; this file is the account of the menu half,
  and `F3`'s copies should become references in a commit of their own.
