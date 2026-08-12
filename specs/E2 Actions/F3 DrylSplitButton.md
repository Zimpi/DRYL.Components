# DrylSplitButton

## Meta
- **State:** Implemented
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
`MenuLabel`. Because the caret is icon-only, it is additionally wrapped in a
`DrylTooltip` (`UX-05`) whose text is that same `MenuAriaLabel`, so the visible
hint and the announced name are one string by construction.

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
| `Ai` | `AiState` | `AiState.None` | The AI opt-in (`AI-03`), **inherited from `DrylAiAware`**. Resolved against a surrounding `DrylAiScope` here, once, and the result applied to **both** segments. |
| `Aura` | `AiAura?` | `null` | The aura variant, **inherited from `DrylAiAware`**. `null` inherits the scope's, ultimately `AiAura.Comet`. Resolved here and applied to **both** segments. |
| `Block` | `bool` | `false` | Stretches the control to its container's full width. |
| `Class` | `string?` | `null` | Extra CSS class(es) merged onto the wrapper's own classes. |
| `AdditionalAttributes` | `IDictionary<string, object>?` | `null` | Pass-through attributes on the wrapper element. |

`Variant`, `Size` and `MenuPlacement` are typed as enums nested inside other
components, so a consumer writes them qualified:
`Variant="DrylButton.ButtonVariant.Ghost"`,
`Size="DrylButton.ButtonSize.Small"`,
`MenuPlacement="DrylMenu.MenuPlacement.TopEnd"`. The component declares no enum
of its own.

The component exposes no `AriaLabel` for the main button, no `IsSubmit`, no
`TrailingIcon`, no `Pressed`, and no parameter for the caret's icon.

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
- The caret is wrapped in a `DrylTooltip` inside the menu's trigger slot
  (`UX-05`).
- The tooltip's text is `MenuAriaLabel`, so the bubble and the caret's
  accessible name are always the same string and cannot drift apart.
- The component exposes no separate parameter for the tooltip's text.
- The tooltip's placement is the tooltip's own default, which the component does
  not set.
- The tooltip adds nothing to the caret's accessible name, because the bubble is
  a decorative body-level portal that the library's script marks `aria-hidden`.
- The duty cannot be discharged at the call site: the caret is rendered by this
  component and the trigger slot is not exposed, so a consumer has nothing to
  wrap.
- The tooltip wrapper does **not** break the segment styling, because the caret
  is matched by a descendant selector below the popover's anchor rather than by
  a child selector.
- The tooltip wrapper does **not** break the focus return to the caret, because
  `dryl.menu.focusTrigger` also finds the caret through a descendant selector
  below the popover's trigger element.
- The tooltip wrapper does **not** break the caret's click, because the wrapper
  is an ordinary `span` and the click bubbles from the caret through it to the
  popover's trigger element, which is where the handler lives.
- A **disabled** caret can still show its tooltip, because the text is carried
  by the wrapper rather than by the button, and the library's script resolves a
  hovered tooltip by walking up from the event's target.

  That last one is a change in behaviour, small and unasked for, and it is
  named rather than left to be discovered. A disabled native `button` dispatches
  no pointer events of its own, but browsers differ in what they hand the
  element underneath: where the event is retargeted to the parent, the wrapper
  is hit and the bubble appears; where it is swallowed entirely, nothing
  appears. So a disabled caret shows its tooltip in some browsers and not in
  others — never worse than the previous state, in which no caret showed one at
  all, and arguably better, since a control that explains itself while it is
  unavailable is more useful than one that stays silent. It is recorded as
  behaviour the component does not control rather than as a guarantee.

  This is where `DrylSplitButton` differs from `DrylButtonGroup`. In the group,
  every rule that produces the segmented look selects `> .btn`, so a
  `DrylTooltip` wrapper — whose root is a `span` carrying `tt-wrap` — displaces
  the segment out of the contract and `UX-05` and the segmentation cannot both
  be satisfied. Here the caret's two rules are `.split-btn > .popover-anchor`
  (which still matches, since the tooltip sits *below* the anchor, not above it)
  and `.split-btn > .popover-anchor .btn` (a descendant selector, which matches
  through any wrapper). `.tt-wrap` is `display: inline-flex`, so it also passes
  the trigger's flex layout through. The library gap that blocks the fix in
  `DrylButtonGroup` does **not** exist here, which is why the tooltip could be
  added to this component and not to that one.

### AI mode

- `Ai` defaults to `AiState.None`, so a split button that was never given the
  parameter renders as an ordinary control (`AI-03`).
- `Ai` is inherited from `DrylAiAware` rather than declared on this component,
  with the same name, the same `AiState` type and the same `AiState.None`
  default.
- The component therefore carries the base class's `[CascadingParameter]`
  `AiScope` and resolves the surrounding scope itself, in `EffectiveAi`.
- `EffectiveAi` is forwarded to **both** segments.
- Each segment, being a `DrylButton`, still runs `AiScope.Resolve` on what it was
  given — but it is given an already-resolved value, and resolving it a second
  time against the same scope changes nothing: a non-`None` state wins over the
  scope, and a `None` one can only arise when the scope is `None` as well. The
  two segments therefore cannot diverge.
- Both segments of one split button render the same effective AI state.
- Outside any `DrylAiScope`, setting `Ai` lights **both** segments.
- Inside a `DrylAiScope`, a split button that leaves `Ai` at `AiState.None` puts
  both segments into the scope's state.
- Inside a `DrylAiScope`, a split button that sets `Ai` explicitly puts both
  segments into the **explicit** state, because the explicit value wins in the
  one resolution this component performs.
- `Aura` is inherited from `DrylAiAware`, defaults to `null`, and its resolved
  value `EffectiveAura` is likewise forwarded to both segments, so the aura
  variant can be pinned on a split button and cannot differ between its halves.
- A split button that leaves `Aura` at `null` follows the surrounding scope's
  variant, ultimately `AiAura.Comet`.
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
- **Keyboard and a11y** — the "Keyboard and accessibility" criteria above, and
  the "`UX-05` and the caret" criteria that follow them: two native buttons, two
  tab stops, no arrow-key movement between them, an `aria-label` on the caret
  from `MenuAriaLabel`, a `DrylTooltip` around the caret carrying that same text
  (`UX-05`), one `aria-label` on the panel from
  `MenuLabel`, focus into the panel on open and back to the caret on `Escape` and
  on item choice — and the three things the control does **not** do: no
  `aria-haspopup`, no `aria-expanded`, and no accessible name for a main button
  that was given only an icon.
- **AI mode** — **yes**, as an opt-in in the sense `AI-03` requires: the
  parameter is named `Ai`, is of type `AiState`, defaults to `AiState.None`, and
  is a switch on a component that renders as an ordinary control without it. It is
  none of the three non-opt-in shapes the rule names. `AI-05` is satisfied by the
  same fact — this component genuinely can host AI mode, since it owns a main
  action a model can be working on. The parameter is inherited from
  `DrylAiAware`, which is what makes its reach correct: the component resolves the
  surrounding scope once and hands the result to both segments, so a control that
  is drawn as one outline is lit as one.
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

## Deviations (`State: Implemented`)

**None.** Every acceptance criterion above is met by the code (`SPEC-04`). The
one deviation this section previously carried — the two segments ending up in
different AI states — was closed by making the component `@inherits DrylAiAware`
and forwarding `EffectiveAi` and `EffectiveAura` to both segments, so the scope
is resolved **once**, here, instead of twice and independently in two
`DrylButton`s. The regression is held by
`tests/DRYL.Components.Tests/DrylSplitButtonTests.cs`, which asserts the aura
classes on both segments outside a scope, inside a scope, and inside a scope that
disagrees with an explicit `Ai`.

The alternative that section used to offer — "pin the caret to `AiState.None` so
it is never lit by a scope" — **was not expressible in the code and has been
removed rather than left standing**: `AiScope.Resolve` is
`explicitAi != AiState.None ? explicitAi : (scope?.State ?? AiState.None)`, so
`AiState.None` means "inherit the scope", not "off". Passing `AiState.None` to
the caret would have left it inheriting the scope exactly as before.

### Recorded gaps — not deviations, and not what `State` rests on

Each of the following breaks a criterion of no spec and a rule of no number, or
belongs to another component's code and another component's spec.

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
- **No demo page and no `ComponentCatalog` entry** (`REL-04`), as the
  cross-cutting evidence above sets out. Both would live in `DRYL.Website`, a
  different repository; no acceptance criterion of this spec is about them.
