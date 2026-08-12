# DrylSplitButton

## Meta
- **State:** Modified
- **Source:** code/DRYL.Components/Components/Actions/DrylSplitButton.razor

## User Story

As a Blazor developer building an application on DRYL, I want a primary action
joined to a caret that drops a menu of its variants, so that "Save" and "Save &
new / Save & close" read as one control without me wiring a button, a menu and
the CSS that joins them together myself.

## Description

`DrylSplitButton` is the "Save ▾" enterprise pattern as a single component. It
renders one `div` holding two segments: a `DrylButton` carrying the main action,
and a `DrylMenu` whose trigger is a second, icon-only `DrylButton` carrying a
chevron. The two segments share `Variant` and `Size` and are joined by CSS into
one outline.

**The component is composition, not behaviour.** It owns its wrapper element,
the class list on it, the split of its own parameters across the two segments,
and the CSS that joins them. Everything else is the primitives':

- The two segments are `DrylButton`s. Variants, sizes, the disabled and loading
  looks, the focus ring, the hover and press motion, and the AI aura are the
  button's — see `F1 DrylButton`.
- The dropdown is a `DrylMenu`, which is itself built on `DrylPopover`. Open and
  close state, positioning, the portal to `<body>`, click-outside dismissal, the
  panel's `menu` role and its entrance animation belong to that pair; arrow-key
  navigation, `Escape`, and closing when an item is chosen belong to `DrylMenu`.
  `DrylSplitButton` holds no open state, exposes no way to open or close the
  menu, and raises no callback when it opens or closes.

What the component contributes to accessibility is exactly two names: the
caret's `aria-label`, from `MenuAriaLabel`, and the panel's `aria-label`, from
`MenuLabel`.

The caret's styling reaches through the menu's markup. `.split-btn > .popover-anchor`
and `.split-btn > .popover-anchor .btn` in
`code/DRYL.Components/wwwroot/dryl.css` select a class this component never
renders: `popover-anchor` is the root element of `DrylPopover`, which `DrylMenu`
wraps. That is a **structural dependency on `DrylMenu`'s internal markup**, in
the same class as `DrylButtonGroup`'s `> .btn` contract, and it is written into
the criteria below rather than left implicit.

The component has no codebehind and **no `.razor.css`**: its rules live in the
shared `code/DRYL.Components/wwwroot/dryl.css`, under the `.split-btn` family of
selectors, next to the `.btn` and `.btn-group` families.

Three of the component's defaults differ from the primitives' own, deliberately:
`Variant` defaults to `Secondary` where a lone `DrylButton` defaults to
`Primary`, because a split button is an outlined pair rather than the page's one
filled call to action; `MenuPlacement` defaults to `BottomEnd` where a lone
`DrylMenu` defaults to `BottomStart`, because the caret sits at the control's
trailing edge; `Size` matches `DrylButton`'s own default of `Medium`.

## Public API

| Member | Type | Default | Purpose |
|---|---|---|---|
| `ChildContent` | `RenderFragment?` | `null` | The main button's label. Must be written as an explicit `<ChildContent>` element whenever `<MenuItems>` is also given. |
| `MenuItems` | `RenderFragment?` | `null` | The dropdown's contents — intended to hold `DrylMenuItem` components. |
| `Variant` | `DrylButton.ButtonVariant` | `ButtonVariant.Secondary` | Visual style of **both** segments. |
| `Size` | `DrylButton.ButtonSize` | `ButtonSize.Medium` | Size of **both** segments. |
| `Disabled` | `bool` | `false` | Disables **both** segments. |
| `Loading` | `bool` | `false` | Puts the **main** button into its loading state. Not applied to the caret. |
| `LeadingIcon` | `string?` | `null` | `DrylIcon` name rendered before the main button's label. |
| `OnClick` | `EventCallback<MouseEventArgs>` | — | Fires when the main button is activated. |
| `MenuPlacement` | `DrylMenu.MenuPlacement` | `MenuPlacement.BottomEnd` | Where the panel opens relative to the caret. |
| `MenuLabel` | `string?` | `null` | Accessible name of the menu panel. |
| `MenuAriaLabel` | `string` | `"More actions"` | Accessible name of the icon-only caret button. |
| `Ai` | `AiState` | `AiState.None` | The AI opt-in (`AI-03`). Applied to the **main** button only. |
| `Block` | `bool` | `false` | Stretches the control to its container's full width. |
| `Class` | `string?` | `null` | Extra CSS class(es) merged onto the wrapper's own classes. |
| `AdditionalAttributes` | `IDictionary<string, object>?` | `null` | Pass-through attributes on the wrapper element. |

`Variant`, `Size` and `MenuPlacement` are typed as enums nested inside other
components, so a consumer writes them qualified:
`Variant="DrylButton.ButtonVariant.Ghost"`,
`Size="DrylButton.ButtonSize.Small"`,
`MenuPlacement="DrylMenu.MenuPlacement.TopEnd"`. The component declares no enum
of its own.

The component exposes no `Aura` parameter, no `AriaLabel` for the main button,
no `IsSubmit`, no `TrailingIcon`, no `Pressed`, and no parameter for the caret's
icon.

`MenuItems` is a **named** `RenderFragment`, which changes how the main label is
written: a split button with no menu items may put its label directly between
the tags, but as soon as a `<MenuItems>` element appears, Razor requires every
piece of content to sit in a named fragment, so the label has to be wrapped in
`<ChildContent>` as well. That is the shape every instance in
`DRYL.Website/Components/Examples/ButtonGroup/Split.razor` uses, and the shape a
consumer will almost always need — the mixed form is a compile error, not a
runtime surprise.

## Acceptance Criteria

### Content and composition

- The component renders exactly one `div` element as its root.
- The root element carries the `split-btn` identity class.
- The component renders one `DrylButton` as the main segment.
- The component renders one `DrylMenu` as the second segment.
- The `DrylMenu`'s trigger slot holds a second `DrylButton`, which is the caret.
- The component renders `ChildContent` as the main button's label.
- The component passes `MenuItems` into the menu's items slot unchanged.
- A split button given no `MenuItems` still renders its caret, which opens an
  empty panel.
- The component places nothing between the two segments — no separator element
  and no gap.

### The composition boundary

- The component holds no open state: whether the menu is open is the inner
  `DrylMenu`'s business.
- The component exposes no parameter and no method to open or close the menu.
- The component raises no callback when the menu opens or closes.
- `OnClick` is the component's only `EventCallback`, and it is forwarded to the
  main button alone.
- Choosing a menu item raises that item's own callback; the component observes
  neither the choice nor the close that follows it.
- `MenuLabel` is the only aspect of the panel the component exposes besides its
  placement.
- The panel that opens carries the menu's `menu` role, and the component adds no
  role of its own to it.
- The panel's accessible name is `MenuLabel`, and the panel is left unnamed when
  `MenuLabel` is `null`.
- The component adds no key handling of its own, on either segment or on its
  wrapper.

  A few criteria below — the panel's `menu` role, focus moving into the panel on
  open, the arrow-key and `Tab` behaviour inside it — describe surface this
  component does not own. They are kept here rather than demoted to prose because
  `SPEC-05` binds every component spec to evidence its keyboard and a11y
  behaviour, and for a component that is pure composition the only keyboard
  behaviour a consumer can observe *is* the composed behaviour: a spec that
  documented only the parts `DrylSplitButton` writes itself would evidence almost
  nothing. They also carry the half that genuinely is this component's — that the
  focus restored on `Escape` and on item choice lands on **the caret** — which
  cannot be stated without them, since it follows from
  `dryl.menu.focusTrigger`'s `.popover-trigger button:not([disabled])` selector
  finding the caret and nothing else inside the anchor. That is the "materially
  unavoidable" case `SPEC-06`'s Independent letter allows; the authority for each
  of them is `DrylMenu`'s and `DrylPopover`'s specs, and a change there overrides
  this file.

### The structural dependency on `DrylMenu`'s markup

- The main button is styled as the leading segment through a **child** selector,
  so it must be a direct child of the wrapper — which it is, because
  `DrylButton`'s root element carries the `.btn` class.
- The caret segment is styled through the `popover-anchor` class, which this
  component never renders: it is emitted by the `DrylPopover` that `DrylMenu`
  wraps.
- The caret's hairline pull onto the main button is written as a **child**
  selector on that anchor class, so it applies only while `DrylMenu`'s outermost
  rendered element is the popover's anchor.
- The caret button itself is matched by a **descendant** selector below that
  anchor, so any number of intermediate elements between the anchor and the
  caret button leave its corner flattening intact.
- A change to `DrylMenu`'s or `DrylPopover`'s root markup therefore breaks this
  component's appearance, with no diagnostic of any kind and no compile-time
  signal.

### Variants, sizes and placement

- `Variant` defaults to `DrylButton.ButtonVariant.Secondary`.
- `Variant` accepts exactly the four values of `DrylButton.ButtonVariant`.
- `Variant` is applied to the main button.
- `Variant` is applied to the caret button.
- The two segments therefore always share one variant; a mismatched pair is not
  expressible.
- `Size` defaults to `DrylButton.ButtonSize.Medium`.
- `Size` accepts exactly the three values of `DrylButton.ButtonSize`.
- `Size` is applied to both segments, so the two segments always share one size
  and therefore one height.
- `LeadingIcon` is applied to the main button only.
- The caret's icon is fixed to the library's chevron-down icon and is not
  configurable.
- `MenuPlacement` defaults to `DrylMenu.MenuPlacement.BottomEnd`.
- `MenuPlacement` accepts exactly the four values of `DrylMenu.MenuPlacement`.
- `MenuPlacement` is measured against the caret, not against the whole control,
  because the menu's anchor is the caret's segment.

### Disabled and loading

- `Disabled` defaults to `false`.
- `Disabled` is applied to the main button.
- `Disabled` is applied to the caret button.
- A disabled caret cannot open the menu: the menu's toggle listens on the
  trigger wrapper around the caret rather than on the caret itself, and a
  disabled native `button` dispatches no click for that wrapper to receive.
- The trigger wrapper offers no clickable area of its own beside the caret,
  because it shrink-wraps its content, so a disabled caret leaves no gap through
  which the menu could still be opened.
- `Loading` defaults to `false`.
- `Loading` is applied to the main button only.
- The main button therefore shows a spinner in place of its leading icon and
  becomes inert while `Loading` is `true`.
- The caret stays fully operable while `Loading` is `true` and `Disabled` is
  `false`.
- The menu can therefore be opened and an item chosen while the main action is
  still running — which is the intended shape for "Save is in flight, Save &
  close is still offered", and is a consumer's decision to prevent by also
  setting `Disabled`.
- A split button that is both `Loading` and `Disabled` has both segments inert.

### Layout and `Block`

- `Block` defaults to `false`.
- The control lays its two segments out in a row.
- The control is only as wide as its two segments while `Block` is `false`, so
  it sits inline beside other content.
- The control stretches to its container's full width while `Block` is `true`.
- The main button absorbs the surplus width while `Block` is `true`.
- The caret keeps its intrinsic width while `Block` is `true`, because the rule
  that shares the width matches direct `.btn` children of the wrapper and the
  caret is not one — it sits inside the menu's anchor element.
- `Block` is **not** forwarded to the inner `DrylMenu`, so the menu's anchor is
  never put into its own block mode and never stretches.
- The width of the open panel does not depend on `Block`, because the menu never
  asks the popover to match its trigger's width.
- The control does not wrap its segments onto a second line.

### Class merging and attribute precedence

- The wrapper always carries the `split-btn` identity class.
- The wrapper carries the block modifier class exactly while `Block` is `true`.
- `Class` is merged onto the wrapper's own classes and never replaces them.
- A consumer-supplied `class` attribute is merged the same way, because Blazor
  matches parameter names case-insensitively and binds it to `Class`.
- Because `class` binds to a declared parameter, it never reaches
  `AdditionalAttributes` and can never clobber the wrapper's own classes.
- `AdditionalAttributes` is splatted onto the wrapper after the `class`
  attribute the component writes, so a pass-through attribute of the same name
  would win.
- `class` is the only attribute the component writes on its wrapper, so no other
  pass-through attribute has anything of the component's to override.
- The wrapper carries no `role` and no ARIA attribute of its own, so a consumer
  adding one through `AdditionalAttributes` overrides nothing.
- Neither `Class` nor `AdditionalAttributes` reaches either segment or the menu:
  they stop at the wrapper.

### Keyboard and accessibility

- Both segments are native `button` elements, so each is reachable with `Tab`
  and activated with `Enter` and `Space` without the component adding key
  handling (`UX-01`).
- The control costs two tab stops: the component establishes no roving tab index
  and no arrow-key movement between its two segments.
- Each segment shows the library's shared `:focus-visible` ring, unchanged by
  this component (`UX-02`).
- The focused segment is raised above the other while its ring is shown, so the
  ring is not clipped by the neighbour it overlaps.
- `MenuAriaLabel` is rendered as the caret's `aria-label`.
- `MenuAriaLabel` defaults to the English string `More actions`, so an
  unconfigured caret still has an accessible name.
- `MenuAriaLabel` is non-nullable, so the caret loses its accessible name only if
  a consumer explicitly supplies an empty one.
- The caret carries **no** `aria-haspopup` attribute, so assistive technology is
  not told that it opens a menu.
- The caret carries **no** `aria-expanded` attribute, so assistive technology is
  not told whether the menu is currently open.
- The main button takes its accessible name from `ChildContent` alone: the
  component exposes no `AriaLabel` for it.
- The main button is never in icon-only mode, because the component always
  supplies it a child-content fragment — even when `ChildContent` is `null`.
- A split button given only a `LeadingIcon` and no `ChildContent` therefore
  renders a main button that has no visible label, no accessible name and none
  of the icon-only sizing.
- Opening the menu moves focus into the panel, onto its first item.
- Opening a panel that has no items moves focus onto the panel element itself,
  so focus is never left behind on the caret while the panel is open.
- `Escape` inside the open panel closes the menu and returns focus to the caret.
- Choosing a menu item closes the menu and returns focus to the caret.
- `Tab` inside the open panel closes the menu and does **not** return focus to
  the caret.
- The main button renders as a non-submitting button, because the component
  exposes no `IsSubmit`; a split button inside a form therefore never submits it.

  Two notes for a consumer. First, `Escape` is handled by `DrylMenu` rather than
  by the popover underneath it: the menu passes `CloseOnEscape="false"` to
  `DrylPopover` and implements `Escape` in its own `HandleKeyDown`. The reason is
  focus, not closing — the popover's own `Escape` path closes the panel and stops
  there, while the menu's also calls `dryl.menu.focusTrigger`, which puts focus
  back on the enabled button inside the popover's trigger slot. For this
  component that button is the caret. Handing `Escape` to the popover as well
  would close the panel twice and skip the focus restore, so the menu takes the
  key over completely.

  Second, the key handler lives on the panel. `Escape` therefore only closes the
  menu while focus is inside it — which it is, because opening moves focus there.

  Third, the two missing attributes are a gap against the library's own practice
  rather than a neutral choice. No numbered rule mandates `aria-haspopup` or
  `aria-expanded`, which is why they are not counted in this spec's `State`, but
  every other trigger of this shape in the library emits both: `DrylSelect` and
  `DrylMultiSelect` with `aria-haspopup="listbox"`, and `DrylDatePicker`,
  `DrylTimePicker` and `DrylNotifications` with `aria-haspopup="dialog"`, each
  paired with an `aria-expanded` tracking its own open state. The caret emits
  neither, so a screen-reader user meets it as an ordinary button and is never
  told the menu is open.

### `UX-05` and the caret

- The caret renders an icon and no visible label, so `UX-05` applies to it.
- The caret is wrapped in a `DrylTooltip` whose text names the same action as
  `MenuAriaLabel` (`UX-05`). — **not met by the code today; see the deviations
  section.**
- The duty cannot be discharged at the call site: the caret is rendered by this
  component and the trigger slot is not exposed, so a consumer has nothing to
  wrap.
- Wrapping the caret in a `DrylTooltip` inside the trigger slot would **not**
  break the segment styling, because the caret is matched by a descendant
  selector below the popover's anchor rather than by a child selector.

  This is where `DrylSplitButton` differs from `DrylButtonGroup`. In the group,
  every rule that produces the segmented look selects `> .btn`, so a
  `DrylTooltip` wrapper — whose root is a `span` carrying `tt-wrap` — displaces
  the segment out of the contract and `UX-05` and the segmentation cannot both
  be satisfied. Here the caret's two rules are `.split-btn > .popover-anchor`
  (which would still match, since the tooltip would sit *below* the anchor, not
  above it) and `.split-btn > .popover-anchor .btn` (a descendant selector, which
  matches through any wrapper). `.tt-wrap` is `display: inline-flex`, so it also
  passes the trigger's flex layout through. The library gap that blocks the fix
  in `DrylButtonGroup` does **not** exist here; what is missing is only the
  tooltip itself, and adding it is a code change this spec deliberately does not
  make.

### AI mode

- `Ai` defaults to `AiState.None`, so a split button that was never given the
  parameter renders as an ordinary control (`AI-03`).
- `Ai` is declared directly on this component rather than inherited from
  `DrylAiAware`.
- `Ai` is forwarded to the main button only.
- The caret receives no `Ai` value from the component.
- The component declares no cascading parameter for a surrounding `DrylAiScope`
  and resolves nothing itself.
- Each segment resolves a surrounding `DrylAiScope` on its own, because each is a
  `DrylButton` and the button is AI-aware.
- Outside any `DrylAiScope`, setting `Ai` lights the main button and leaves the
  caret plain.
- Inside a `DrylAiScope`, a split button that leaves `Ai` at `AiState.None` puts
  **both** segments into the scope's state, because each segment inherits it
  independently.
- Both segments of one split button render the same effective AI state.
  — **not met by the code today; see the deviations section.**
- The component exposes no `Aura` parameter, so the aura variant on the main
  button cannot be pinned and follows the surrounding scope, ultimately
  `AiAura.Comet`.
- The main button's aura ring follows its flattened trailing corners, because the
  aura layers inherit their host's radius: the ring squares off where the caret
  joins it.
- A resting AI-aware main button's outward halo is overlapped on its trailing
  side by the caret's anchor, because the two segments overlap and paint in
  document order while the halo spreads outside its own segment.
- That occlusion is lifted while the main button is hovered or focus-visible,
  since those are exactly the two states the split-button rules raise — a resting
  AI state is not among them.

  The occlusion is worth spelling out, because "positioned versus not" is the
  wrong reading and the neighbouring `.btn` rules invite it. Both segments are
  positioned: `.btn` itself declares `position: relative` (and `isolation: isolate`,
  which the shared stylesheet's own comment beside the AI-on-buttons block
  explains — the button carries no `overflow: hidden`, so the aura's default
  sizing traces its rounded box and glows outside it untouched), and
  `.popover-anchor` declares `position: relative` too. Neither declares a
  `z-index`, so both sit at `auto` and paint in document order — and the caret's
  anchor is the later sibling. That is the same mechanism `DrylButtonGroup`
  records for a middle segment's halo.

### Motion

- The component declares no transition of its own.
- The component declares no animation and no transform of its own.
- Hover, press, focus-ring and loading motion belong to the two `DrylButton`
  segments and are unchanged by this component.
- The panel's entrance animation belongs to the popover the menu wraps.
- The component does write one state-change rule: it raises the hovered or
  focus-visible segment in the stacking order.
- That raise steps rather than animating, which is inherent to what it changes —
  a stacking order has no meaningful intermediate value — while the visible
  transition on those same events remains the segment's own.
- The rules raise a segment on hover and on `:focus-visible` only: unlike
  `.btn-group`, they add no raise for a segment in its active state.
- The component mounts nothing conditionally: both segments are rendered
  unconditionally, so there is no surface of the component's own that appears or
  disappears (`DESIGN-12`).
- Switching `Block` re-lays the segments in one step rather than gliding their
  widths.

### Appearance

- The main button's trailing corners are flattened, so only its leading edge
  keeps a rounded corner.
- The caret button's leading corners are flattened, so only its trailing edge
  keeps a rounded corner.
- The control therefore reads as one pill with a divider rather than as two
  boxes.
- The caret segment is pulled onto the main button by one hairline, so the two
  adjacent borders occupy the same pixels.
- The component overrides no radius on the corners it does not flatten, so the
  control's outer corners are the segments' own corners.
- A control sized `ButtonSize.Small` therefore keeps the smaller radius the
  button defines for itself on its outer edges.
- The component sets no background and no border of its own.
- The component sets no shadow of its own.
- The joined outline is therefore composed entirely from the two segments' own
  surfaces.
- The component writes no color and holds no mode-assuming value, so it branches
  on no color mode and inherits its color-mode behaviour from its segments
  (`DESIGN-02`).
- The corner flattening names physical left and right corners rather than logical
  ones, so it does not mirror under `direction: rtl`.
- The hairline pull is equally physical — it is written as a leading-side
  negative margin on the left — so it does not mirror either, and a right-to-left
  reader gets the caret's overlap on the wrong side.

  The literals in these rules are worth naming, because the check that would
  normally catch them cannot see this component at all: `DrylSplitButton` has no
  isolated stylesheet, and `DESIGN-01`'s check greps colors in `*.razor.css`
  only — the green it reports says nothing whatsoever about the `.split-btn`
  rules in the shared `code/DRYL.Components/wwwroot/dryl.css`.

  `DESIGN-01` enumerates color, padding, radius, shadow, duration and easing. Of
  those, the component writes exactly one kind: **radius**, and only as zero — the
  two flattened corners on the main button and the two on the caret button, each
  written as a bare number on a corner longhand. No token expresses "no radius",
  and zero is identical in both color modes, so this is named here rather than
  counted as compliant, exactly as `DrylButtonGroup` names its own zero radii. The
  component writes no color, no padding, no shadow, no duration and no easing at
  all.

  Outside `DESIGN-01`'s enumeration, and therefore debt of a lesser kind, the
  complete inventory of every remaining declaration in the `.split-btn*` rules:
  the two `display` values (`inline-flex` at rest, `flex` under the block
  modifier), the cross-axis `align-items: stretch`, the full-width percentage of
  the block modifier, the flex shorthand that lets the main button absorb the
  surplus width, the one-pixel negative `margin-left` that produces the hairline
  pull, and the stacking value of the hover / focus raise. `DESIGN-01` governs
  none of them; the pull in particular is tied to the segments' border width
  rather than to a spacing scale.

## Cross-cutting evidence (`SPEC-05`)

- **Both color modes** — the component paints nothing of its own and writes no
  color, so there is no mode-specific rule and no mode-assuming literal to check;
  the two segments carry the palette. `node scripts/check-light-sync.mjs` and
  `node scripts/validate-light-contrast.mjs` are green.
- **Enter/exit animation** — none of the component's own, and none is owed: both
  segments render unconditionally, so `DESIGN-12` has no subject here, and the
  motion budget of a persistent control is spent on state changes, exactly as
  `F1 DrylButton` argues for a lone button. The one conditional mount reachable
  through this component is the menu panel's body, and it belongs to
  `DrylPopover`: the panel wrapper is always rendered so JS can portal it, while
  the body is gated on the open state by an `@if` that is **not** wrapped in
  `DrylPresence`. The panel therefore animates in (`popover-in`, bound to the
  reveal state so it replays on every open) but not out — closing drops the
  visibility gate and removes the body in the same frame. That is a `DESIGN-12`
  gap in `DrylPopover`, recorded here because it is visible through this
  component and fixed in that one; it is not this component's `@if` to wrap.
- **Keyboard and a11y** — the "Keyboard and accessibility" criteria above: two
  native buttons, two tab stops, no arrow-key movement between them, an
  `aria-label` on the caret from `MenuAriaLabel` and one on the panel from
  `MenuLabel`, focus into the panel on open and back to the caret on `Escape` and
  on item choice — and the three things the control does **not** do: no
  `aria-haspopup`, no `aria-expanded`, and no accessible name for a main button
  that was given only an icon.
- **AI mode** — **yes**, as an opt-in in the sense `AI-03` requires: the
  parameter is named `Ai`, is of type `AiState`, defaults to `AiState.None`, and
  is a switch on a component that renders as an ordinary control without it. It is
  none of the three non-opt-in shapes the rule names. `AI-05` is satisfied by the
  same fact — this component genuinely can host AI mode, since it owns a main
  action a model can be working on. What the implementation gets wrong is not
  *whether* to have the parameter but *how far it reaches*: see the deviations
  below.
- **Demo page** — **no page of its own.** `DrylSplitButton` is demonstrated by
  `DRYL.Website/Components/Examples/ButtonGroup/Split.razor`, which is composed
  into `DRYL.Website/Components/Pages/DemoButtonGroup.razor` — the **Button
  Group** page, routed at `/components/button-group`, as its third example
  ("Split button — primary action + variants"). The example shows three split
  buttons: a default one, a `Ghost` one with a `LeadingIcon`, and a `Danger`
  `Small` one; all three set `MenuLabel`, none sets `Block`, `Loading`,
  `Disabled`, `Ai`, `MenuPlacement`, `MenuAriaLabel` or `Class`. Those parameters
  are therefore undemonstrated, and every criterion about them above rests on
  reading the code and the rules rather than on a rendered example.
- **`ComponentCatalog`** — **no entry of its own.**
  `DRYL.Website/Components/ComponentCatalog.cs` has no row whose `ClassName` is
  `"DrylSplitButton"`; the component reaches the sidebar, the Ctrl+K search and
  the `/components` overview only through the `"Button Group"` / `button-group`
  row, whose `ClassName` is `"DrylButtonGroup"` and whose `Ai` flag is `false`.
  Searching the catalog for "split button" therefore finds nothing, and the
  component is reachable only by someone who already knows it shares the button
  group's page. This is exactly the split `SPEC-05` warns about — shipped in the assembly, absent from the library's
  own index — and `REL-04` is not satisfied for this component. Recorded, not
  papered over: the fix is a catalog row (and, if it is given one, a page) in
  `DRYL.Website`, which is a different repository and out of this spec's scope.

## Deviations (`State: Modified`)

The spec is newly written and the code does not meet **two** of its criteria.
They are listed here so the state is checkable rather than asserted; neither is
fixed in this commit. Four further findings are recorded below them as design
gaps: each breaks a criterion of no spec and a rule of no number, or belongs to
another component's spec, so none of them is what `State` rests on.

1. **`UX-05` — the caret has no tooltip.** The component renders an icon-only
   button and no `DrylTooltip` anywhere. `UX-05` is binding and admits no
   exception, and unlike the icon-only cases in `F1` and `F2` the duty cannot be
   pushed to the call site, because the trigger is not a slot the consumer fills.
   The unmet criterion is the second one under "`UX-05` and the caret". The fix is
   a `DrylTooltip` around the caret inside the trigger slot, with its text equal
   to `MenuAriaLabel`; the criteria above establish that this breaks none of the
   segment styling.

2. **The two segments can end up in different AI states.** `Ai` is forwarded to
   the main button only, while the caret — being a `DrylButton` — resolves a
   surrounding `DrylAiScope` on its own. Inside a scope whose state is not
   `AiState.None`, a split button that sets `Ai` explicitly renders its main
   button in the explicit state and its caret in the scope's state, so one joined
   control shows two different auras; and a split button that sets `Ai` while
   *outside* a scope lights only half of a control that is drawn as one outline.
   The unmet criterion is "Both segments of one split button render the same
   effective AI state." Which way to close it is a design decision for the
   maintainer — forward the resolved state to both segments, or pin the caret to
   `AiState.None` so it is never lit by a scope — and is not made here.

### Recorded design gaps — not deviations

- **No `Aura`, and no scope resolution of its own.** Because `Ai` is a plain
  parameter rather than `DrylAiAware`'s, the component has neither the `Aura`
  parameter nor `EffectiveAi`. A consumer can pin the aura variant on every other
  AI-aware component in the library but not on this one. **No criterion above
  fails on it** — the criterion on the subject describes exactly this and is met —
  and no numbered rule is breached: `AI-03` binds only the `Ai` parameter's name
  and default, both of which are correct, and `AI-05` only whether the parameter
  exists at all. It is recorded here because closing deviation 2 properly means
  deciding this at the same time, not because it moves `State`.
- **No `aria-haspopup` and no `aria-expanded` on the caret.** No numbered rule
  mandates either, so this stays out of `State` — but it is a
  library-consistency gap rather than a neutral choice: `DrylSelect` and
  `DrylMultiSelect` emit `aria-haspopup="listbox"` with a live `aria-expanded`,
  and `DrylDatePicker`, `DrylTimePicker` and `DrylNotifications` emit
  `aria-haspopup="dialog"` with one. The caret is the same shape of trigger and
  emits neither, so a screen-reader user is told the split button's second
  segment is an ordinary button and is never told the menu is open.
- **The menu panel's conditional mount is not wrapped in `DrylPresence`**
  (`DESIGN-12`), so the panel a split button opens animates in but not out. The
  code that would change is not this component's — it is `DrylPopover`'s `@if` —
  so it belongs to `DrylPopover`'s spec and is recorded here only because it is
  visible through this component.
- **The component's own usage comment shows a form that does not compile.** The
  Razor comment at the top of `DrylSplitButton.razor` writes a bare `Save` label
  followed by a `<MenuItems>` element; because `MenuItems` is a named
  `RenderFragment`, the label has to be wrapped in `<ChildContent>` as soon as
  `<MenuItems>` appears — which is what all three instances in
  `DRYL.Website/Components/Examples/ButtonGroup/Split.razor` do. Documented as a
  comment defect, not fixed here (this spec changes no code).
