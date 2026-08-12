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

`DrylButtonGroup` is a layout wrapper. It renders one `div` carrying its own class
list, `role="group"`, an optional `aria-label` and whatever the consumer put in
`ChildContent` — nothing else. It places no button of its own: the segments are
`DrylButton` components the
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
- A `DrylButton` wrapped in an intermediate element is not a direct child, so it
  keeps all four of its own corners.
- Such a wrapped button takes no border overlap.
- Such a wrapped button is not raised on interaction.
- Such a wrapped button is still placed by the group's flex layout, so the breakage
  is visual segmentation only, not position.
- A child that is not a `.btn` is likewise placed by the flex layout and receives no
  segmentation.
- The first and last positions are decided by element position among **all**
  children, not among the `.btn` children: a single foreign element in the group —
  a wrapper, a separator, a conditionally rendered element — shifts which real
  segments count as first and last.
- A genuine segment that a foreign element has displaced from the first position
  therefore loses its rounded leading edge and takes a border pull onto that foreign
  element, with no diagnostic of any kind.
- Every segment except the first has its leading corners flattened, so only the first
  segment keeps a rounded leading edge.
- Every segment except the last has its trailing corners flattened, so only the last
  segment keeps a rounded trailing edge.
- A segment that is neither first nor last has both leading and trailing corners
  flattened and therefore renders square.
- A group with a **single** segment matches neither of those two conditions, so its
  one child keeps all four of its own corners.
- That single segment also takes no border overlap, since the pull applies only to a
  child that is not the first.
- A single segment in a group with `Block` set to `false` therefore renders exactly
  as a lone button of the same variant and size.
- A single segment in a group with `Block` set to `true` does **not**: it is
  stretched to the container's full width, where a lone `DrylButton` is only as wide
  as its content.
- Every segment except the first is pulled onto its predecessor by one hairline, so
  the two adjacent borders occupy the same pixels and the cluster reads as one
  outline rather than as a row of touching boxes.
- The group overrides no radius on the corners it does not flatten, so the outer
  corners of the group are the segments' own corners.
- A segment sized `ButtonSize.Small` therefore keeps the smaller radius it defines
  for itself on its outer edge.
- A segment sized `ButtonSize.Large` defines no radius of its own, so it keeps the
  default button radius on its outer edge — that size adds no distinct radius for the
  group to honour.
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
- The group imposes no height on its segments: each segment's height comes from its
  own `Size`.
- The group therefore forms one unbroken outline only when its segments share a
  single `Size`; a group mixing `ButtonSize.Small` with `ButtonSize.Medium` renders
  segments of differing heights joined along a stepped edge.
- A segment's height does not depend on its label, so differing label lengths never
  break the outline on their own.
- The group applies no gap between segments; the segmented look depends on them
  touching.
- The group does not wrap its segments onto a second line.

  The height criteria above are worth one note, because the rules read as though
  they promise otherwise. The group's flex container asks its children to stretch on
  the cross axis, but stretching only sets a cross size that is otherwise automatic,
  and every button declares an explicit height for its size. The stretch request is
  therefore inert here, and mixing sizes inside one group is a visual break the CSS
  cannot rescue — which is why the component's own usage comment advises a consistent
  `Variant` and `Size` across the segments. The advice is real; the safety net is not.

  `Block` is undemonstrated: no example under
  `DRYL.Website/Components/Examples/ButtonGroup/` sets it, and no test covers it. The
  `SPEC-05` demo-page point is satisfied by the page as a whole, but this parameter's
  behaviour above rests on reading the rules, not on a rendered example.

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
- The group adds no key handling of its own, so no arrow key moves between segments.
- The group sets no `tabindex` on its segments, so it establishes no roving tab index.
- Each segment is therefore an independent tab stop, reached with `Tab` and
  `Shift+Tab` in DOM order (`UX-01`).
- Each segment is activated with `Enter` and `Space` by virtue of being a native
  `button`, without the group contributing anything (`UX-01`).
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
  baseline makes `AriaLabel` the fix. Both groups in
  `DRYL.Website/Components/Examples/ButtonGroup/Clustered.razor` and both in
  `.../ButtonGroup/Toggle.razor` set it; `.../ButtonGroup/Split.razor` contains no
  `DrylButtonGroup` at all, only two `DrylSplitButton`s, so it neither sets nor omits
  it. The group in `DRYL.Website/Components/Examples/LineChart/AiStates.razor` does
  omit it, so an unlabelled group is reachable in the website today. The component
  permits it; that one is a call-site gap.

### `UX-05` against the child contract

- An icon-only segment needs a `DrylTooltip` naming its action (`UX-05`), and the
  tooltip is an ancestor element, so supplying it is a call-site duty this component
  cannot discharge.
- Wrapping a segment in a `DrylTooltip` makes the tooltip's own root element the
  group's direct child, so that segment is no longer a direct `.btn` child and loses
  every part of the segmentation contract above.
- The wrapper also occupies a child position, so it shifts the first/last computation
  for the segments around it and can break their corners too.
- Satisfying `UX-05` on an icon-only segment therefore cannot currently be done
  without breaking the segmented control; the two requirements are in direct
  conflict.

  This is the finding a consuming developer most needs from this spec, so it is
  stated as a contract consequence rather than buried. The conflict is real and
  library-level: `DrylTooltip` renders a `span` carrying `tt-wrap` as its root, and
  every rule that produces the segmented look selects `> .btn`. The icon-only
  pagination segments in
  `DRYL.Website/Components/Examples/ButtonGroup/Clustered.razor` set `AriaLabel` but
  carry no tooltip — which reads as a call-site oversight and is in fact the only
  shape that renders correctly today. Closing it needs a library change (a group that
  tolerates a wrapper, or a tooltip that does not introduce an element), which is out
  of scope for this spec and is recorded here rather than fixed.

### AI mode

- The component declares no `Ai` parameter and inherits no AI-aware base, so it
  renders no aura and adds no AI-specific class of its own.
- The group neither clips nor isolates its segments: it sets no `overflow`, no
  `isolation`, no `position` and no stacking value, so it forms no stacking context of
  its own.
- An AI-aware segment's aura ring nevertheless follows the segment's flattened
  corners, because the aura layers inherit the host's radius: a middle segment's ring
  squares off at its inner corners.
- A resting AI-aware segment's outward halo is occluded on its trailing side by the
  next segment, because the segments overlap and paint in document order while the
  halo spreads outside its own segment.
- That occlusion is lifted for a segment that is hovered, focus-visible or active,
  since those are exactly the states the group raises in the stacking order — a
  resting AI state is not among them.

### Motion

- The group declares no transition of its own.
- The group declares no animation and no transform of its own.
- Every moving part of a segmented control — hover, press, the focus ring, the
  `Pressed` highlight — belongs to the segments and is unchanged by the group.
- The group does write one state-change rule: it raises the hovered, focus-visible
  or active segment in the stacking order.
- That raise steps rather than animating, which is inherent to what it changes — a
  stacking order has no meaningful intermediate value — while the visible transition
  on those same events remains the segment's own.
- The component mounts nothing conditionally: `ChildContent` is rendered
  unconditionally, so there is no surface of the group's own that appears or
  disappears (`DESIGN-12`).
- Switching `Block` re-lays the segments in one step rather than gliding their
  widths.

### Appearance

- The group sets no background and no border of its own.
- The group sets no radius and no shadow of its own.
- The segmented outline is therefore composed entirely from the segments' own
  surfaces.
- The group holds no color and no mode-assuming value, so it branches on no color
  mode and inherits its color-mode behaviour from its segments (`DESIGN-02`).
- The inner-corner flattening names physical left and right corners rather than
  logical ones, so it does not mirror under `direction: rtl`.
- The border overlap is equally physical — it is written as a leading-side negative
  margin on the left — so it does not mirror either.

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
  those terms. The group has no surface, no marker that moves between targets and no
  content to reveal. It is not quite true that it writes no state-change rule at all:
  it raises the hovered, focus-visible or active segment in the stacking order, which
  is a state change `DESIGN-11` would ordinarily want animated. A stacking order has
  no animated form, and the visible transition on those same three events is the
  segment's own — so the exception holds on the merits, with the rule named rather
  than the case overstated. `DESIGN-11` asks for the exception in the **PR
  description**, and its Check repeats that; it is written here for permanence and is
  owed there as well. A consumer who mounts a whole group conditionally wraps it in
  `DrylPresence` on their own side (`DESIGN-12`).
- **Keyboard and a11y** — the "Keyboard and accessibility" criteria above:
  `role="group"`, `aria-label` from `AriaLabel`, no tab stop of the group's own, and
  each segment an independent tab stop with no roving tab index and no arrow-key
  handling (`UX-01`). The "`UX-05` against the child contract" criteria record the one
  place where an accessibility requirement and this component's structural contract
  cannot both be satisfied today.
- **AI mode** — **no**, deliberately. `DrylButtonGroup` does not inherit
  `DrylAiAware` and declares no `Ai` parameter. It is a layout wrapper with no
  surface of its own to light up; the aura belongs to the segments, each of which is
  itself AI-aware through `DrylButton`. Adding an `Ai` parameter here would be the
  "just in case" `AI-05` forbids, and `AI-03` has no subject, since there is no
  `AiState` parameter to name. Recorded as compliance with `AI-05`, not as a waiver.
  `DRYL.Website/Components/Examples/LineChart/AiStates.razor` shows the intended
  shape: the group is the plain control, and the AI state it selects is applied to the
  chart beside it. What the group does not do is stay neutral towards an AI-aware
  *segment*: the "AI mode" criteria above record that the segmentation reshapes such
  a segment's aura ring and occludes its resting halo on the trailing side.
- **Demo page** — `DRYL.Website/Components/Pages/DemoButtonGroup.razor`, routed at
  `/components/button-group`, composing
  `DRYL.Website/Components/Examples/ButtonGroup/Clustered.razor`,
  `.../ButtonGroup/Toggle.razor` and `.../ButtonGroup/Split.razor` — the last of which
  demonstrates `DrylSplitButton`, which shares the page.
- **`ComponentCatalog`** — registered as `"Button Group"` / `button-group` with
  `ClassName` `"DrylButtonGroup"` in
  `DRYL.Website/Components/ComponentCatalog.cs`, in the `"Actions"` category, with its
  `Ai` flag set to `false`.
