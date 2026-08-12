# DrylButtonGroup

## Meta
- **State:** Implemented
- **Source:** code/DRYL.Components/Components/Actions/DrylButtonGroup.razor

## User Story

As a Blazor developer building an application on DRYL, I want a wrapper that joins
the buttons I place inside it into one segmented control, so that a toolbar cluster
or an exclusive toggle group reads as a single outlined unit without me writing any
of the border, corner or overlap CSS myself.

## Description

`DrylButtonGroup` is a layout wrapper. It renders one `div` carrying `role="group"`,
an optional `aria-label`, and whatever the consumer put in `ChildContent` — nothing
else. It places no button of its own: the segments are `DrylButton` components the
consumer writes, which is what lets the group serve both a cluster of independent
actions and an exclusive toggle group driven by each button's `Pressed` parameter.

Everything the component does visually is done by CSS keyed on its direct children.
The group flattens the inner corners of adjacent segments, pulls each segment onto
its neighbour's border so the cluster reads as one outline, raises the interacted
segment above its neighbours so its border and glow are not clipped, and — under
`Block` — stretches to the container and divides the width equally between the
segments. All of those rules select `> .btn`. That makes the child shape a
**contract**: a segment must be a direct child and must carry the `.btn` class, which
in practice means a `DrylButton` placed directly inside the group. A button wrapped
in an intermediate element, or any other child, is laid out by the flex container but
receives none of the segmentation.

The component has no codebehind and **no `.razor.css`**: its rules live in the shared
`code/DRYL.Components/wwwroot/dryl.css`, under the `.btn-group` family of selectors,
next to the `.btn` family they extend.

`DrylSplitButton` is a separate component with its own `.split-btn` rules, not a
configuration of this one.

## Public API

| Member | Type | Default | Purpose |
|---|---|---|---|
| `ChildContent` | `RenderFragment?` | `null` | The segments. Intended to hold `DrylButton` components as direct children. |
| `AriaLabel` | `string?` | `null` | Accessible name of the group, describing what the segments have in common. |
| `Block` | `bool` | `false` | Stretches the group to its container's full width and divides that width equally between the segments. |
| `Class` | `string?` | `null` | Extra CSS class(es) merged onto the group's own classes. |
| `AdditionalAttributes` | `IDictionary<string, object>?` | `null` | Pass-through attributes on the group element. |

The component declares no enum, no `EventCallback` and no `Ai` parameter, and it
exposes nothing about the segments: variant, size, `Pressed` and `OnClick` are set on
each `DrylButton` individually.

## Acceptance Criteria

### Content and composition

- The component renders exactly one `div` element as its root.
- The component renders `ChildContent` as the only content of that `div`.
- The component renders no button of its own, so an empty group renders an empty
  element.
- The component reads no state from its children and coordinates no selection
  between them: an exclusive toggle group is produced by the consumer binding each
  segment's `Pressed`.

### The child contract

- A segment is styled as part of the group only when it is a **direct child** of the
  group's root element and carries the `.btn` class.
- A `DrylButton` placed directly inside the group satisfies that contract, because
  the button's root element carries `.btn`.
- A `DrylButton` wrapped in an intermediate element is not a direct child and
  therefore keeps all four of its own corners, takes no border overlap and is not
  raised on interaction — it is placed by the group's flex layout but is not
  segmented.
- A child that is not a `.btn` is likewise placed by the flex layout and receives no
  segmentation.
- Every segment except the first has its leading corners flattened, so only the first
  segment keeps a rounded leading edge.
- Every segment except the last has its trailing corners flattened, so only the last
  segment keeps a rounded trailing edge.
- A segment that is neither first nor last has both leading and trailing corners
  flattened and therefore renders square.
- A group with a **single** segment matches neither of those two conditions, so its
  one child keeps all four of its own corners and takes no border overlap — it renders
  exactly as a lone button of the same variant and size.
- Every segment except the first is pulled onto its predecessor by one hairline, so
  the two adjacent borders occupy the same pixels and the cluster reads as one
  outline rather than as a row of touching boxes.
- The outer corners of the group are the segments' own corners: the group overrides
  no radius that survives flattening, so a segment sized `ButtonSize.Small` keeps the
  smaller radius it defines for itself on its outer edge.
- The group defines no radius of its own for a segment sized `ButtonSize.Large`,
  which therefore keeps the default button radius on its outer edge — the size adds
  no distinct radius to be honoured.
- The hovered segment is raised above its neighbours in the stacking order, so the
  border it shares with them is drawn by the hovered segment rather than by the one
  overlapping it.
- The focused segment is raised the same way while its `:focus-visible` ring is
  shown, so the ring is not clipped by a neighbour.
- A segment in its active state is raised the same way, so a selected segment in a
  toggle group draws its own border and glow over its neighbours.
- The raise applies to direct `.btn` children only, so a wrapped or non-`.btn` child
  can be overlapped by its neighbour's border.

### Layout

- `Block` defaults to `false`.
- The group lays its segments out in a row.
- The group is only as wide as its segments while `Block` is `false`, so it sits
  inline beside other content.
- The group stretches to its container's full width while `Block` is `true`.
- Every direct `.btn` child receives an equal share of the group's width while
  `Block` is `true`, independent of its label length.
- The segments are stretched to a common height, so segments of differing content
  still form one unbroken outline.
- The group applies no gap between segments; the segmented look depends on them
  touching.
- The group does not wrap its segments onto a second line.

### Class merging and attribute precedence

- The group's root always carries the group identity class.
- `Class` is merged onto the group's own classes and never replaces them.
- A consumer-supplied `class` attribute is merged the same way, because Blazor
  matches parameter names case-insensitively and binds it to `Class`.
- Because `class` binds to a declared parameter, it never reaches
  `AdditionalAttributes` and can never clobber the group's own classes.
- `AdditionalAttributes` is splatted onto the `div` after every attribute the
  component writes itself, so a pass-through attribute of the same name wins.
- A consumer-supplied `role` attribute therefore replaces `role="group"`.
- A consumer-supplied `aria-label` attribute therefore overrides `AriaLabel`.

  Both class paths are corroborated by
  `ButtonGroup_merges_consumer_class_without_clobbering` in
  `tests/DRYL.Components.Tests/ClassMergeTests.cs`. It renders the component with an
  unmatched `class` attribute and asserts that the class list of the `[role=group]`
  element contains both the group identity class and the consumer's class. That is
  all it asserts: it does not cover the typed `Class` parameter, the `Block`
  modifier class, or the ordering of the two classes.

### Keyboard and accessibility

- The group's root element carries `role="group"`.
- `AriaLabel` is rendered as the group's `aria-label`.
- The `aria-label` attribute is omitted when `AriaLabel` is `null`, leaving an
  unnamed `role="group"`.
- The group itself holds no tab stop: it renders a `div` and sets no `tabindex`.
- The group adds no key handling of its own — no arrow-key navigation and no roving
  tab index — so each segment stays an independent tab stop reached with `Tab` and
  activated with `Enter` or `Space` by virtue of being a native `button` (`UX-01`).
- The group changes no focus behaviour of its segments: the shared `:focus-visible`
  ring is the button's own, and the group only raises the focused segment's stacking
  order so the ring is drawn whole.
- The group exposes no selection semantics: it is neither an ARIA `radiogroup` nor a
  `toolbar`, and a toggle group's selected segment is announced through each button's
  own `aria-pressed` rather than through the group.

  Two consequences are worth stating plainly for a consumer. First, the keyboard
  model is deliberate rather than missing: a visually segmented control in DRYL is a
  row of ordinary buttons, so `n` segments cost `n` tab stops and no arrow key moves
  between them. A consumer who needs the single-tab-stop behaviour of an ARIA toolbar
  or radiogroup builds it at the call site; this component neither provides it nor
  stands in its way.

  Second, `role="group"` without an accessible name adds a grouping boundary that
  screen readers may announce with nothing to announce it as. `UX-01`'s naming
  baseline makes `AriaLabel` the fix, and every demo in
  `DRYL.Website/Components/Examples/ButtonGroup/` sets it — but the group in
  `DRYL.Website/Components/Examples/LineChart/AiStates.razor` does not, so an
  unlabelled group is reachable in the website today. The component permits it; that
  is a call-site gap. The same file-level point applies to `UX-05`: the icon-only
  pagination segments in
  `DRYL.Website/Components/Examples/ButtonGroup/Clustered.razor` set `AriaLabel` but
  are not wrapped in a `DrylTooltip`, which `UX-05` requires. The tooltip wrapper is
  an ancestor of the button and therefore a call-site duty, not something this
  component can supply.

### AI mode

- The component declares no `Ai` parameter and inherits no AI-aware base, so it
  renders no aura and adds no AI-specific class of its own.
- A group whose segments are AI-aware shows their auras unchanged: the group applies
  no `overflow` clipping and defines no stacking context that would cut an aura ring
  off.

### Motion

- The group animates nothing of its own: it declares no transition, no animation and
  no transform, and every moving part of a segmented control — hover, press, focus
  ring, the `Pressed` highlight — belongs to the segments.
- The stacking-order raise on hover, focus and the active state steps rather than
  animating, which is inherent to what it changes.
- The component mounts nothing conditionally: `ChildContent` is rendered
  unconditionally, so there is no surface of the group's own that appears or
  disappears (`DESIGN-12`).
- Switching `Block` re-lays the segments in one step rather than gliding their
  widths.

### Appearance

- The group paints nothing: it sets no background, no border, no radius and no
  shadow, so the segmented outline is composed entirely from the segments' own
  surfaces.
- The group therefore inherits its color-mode behaviour from its segments and holds
  no color and no mode-assuming value of its own (`DESIGN-02`).
- The inner-corner flattening is written with physical leading and trailing corners
  rather than logical ones, so it does not mirror under `direction: rtl`.

  The literals in these rules are worth naming, because the check that would normally
  catch them cannot see this component at all: `DrylButtonGroup` has no isolated
  stylesheet, and `DESIGN-01`'s check greps colors in `*.razor.css` only — the green
  it reports says nothing about the `.btn-group` rules in the shared
  `code/DRYL.Components/wwwroot/dryl.css`.

  `DESIGN-01` enumerates color, padding, radius, shadow, duration and easing. Of
  those the group writes exactly one kind: the flattened inner corners are zero
  radii, written as the bare number on the four corner longhands. No token expresses
  "no radius", and zero is identical in both color modes, so this is named here rather
  than counted as compliant — the same treatment `DrylButton` gives its bare
  `transparent` keywords. The group writes no color, no padding, no shadow, no
  duration and no easing at all.

  Outside `DESIGN-01`'s enumeration, and therefore debt of a lesser kind: the
  one-pixel negative `margin-left` that produces the border overlap, the stacking
  value of the interaction raise, the flex shorthand that equalises the segments
  under `Block`, and the full-width percentage of the `Block` container. `DESIGN-01`
  governs none of them, and the overlap in particular is tied to the segments' border
  width rather than to a spacing scale.

## Cross-cutting evidence (`SPEC-05`)

- **Both color modes** — the component paints nothing of its own and writes no color,
  so there is no mode-specific rule and no mode-assuming literal to check; the
  segments carry the palette. `node scripts/check-light-sync.mjs` and
  `node scripts/validate-light-contrast.mjs` are green.
- **Enter/exit animation** — **none, and none of the four subsets of `DESIGN-11`
  apply.** This is the explicit written exception `DESIGN-11` allows for the rare
  component that genuinely has nothing to animate, and `SPEC-05` permits it only on
  those terms. The group has no surface, no state of its own, no marker that moves
  between targets and no content to reveal; the only property it changes on
  interaction is a stacking order, which cannot be animated meaningfully. A consumer
  who mounts a whole group conditionally wraps it in `DrylPresence` on their own side
  (`DESIGN-12`).
- **Keyboard and a11y** — the "Keyboard and accessibility" criteria above:
  `role="group"`, `aria-label` from `AriaLabel`, no tab stop of the group's own, and
  each segment an independent tab stop with no roving tab index and no arrow-key
  handling (`UX-01`).
- **AI mode** — **no**, deliberately. `DrylButtonGroup` does not inherit
  `DrylAiAware` and declares no `Ai` parameter. It is a layout wrapper with no
  surface of its own to light up; the aura belongs to the segments, each of which is
  itself AI-aware through `DrylButton`. Adding an `Ai` parameter here would be the
  "just in case" `AI-05` forbids, and `AI-03` has no subject, since there is no
  `AiState` parameter to name. Recorded as compliance with `AI-05`, not as a waiver.
  `DRYL.Website/Components/Examples/LineChart/AiStates.razor` shows the intended
  shape: the group is the plain control, and the AI state it selects is applied to the
  chart beside it.
- **Demo page** — `DRYL.Website/Components/Pages/DemoButtonGroup.razor`, routed at
  `/components/button-group`, composing
  `DRYL.Website/Components/Examples/ButtonGroup/Clustered.razor`,
  `.../ButtonGroup/Toggle.razor` and `.../ButtonGroup/Split.razor` — the last of which
  demonstrates `DrylSplitButton`, which shares the page.
- **`ComponentCatalog`** — registered as `"Button Group"` / `button-group` with
  `ClassName` `"DrylButtonGroup"` in
  `DRYL.Website/Components/ComponentCatalog.cs`, in the `"Actions"` category, with its
  `Ai` flag set to `false`.
