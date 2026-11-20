# DrylSegmentedControl

## Meta
- **State:** Implemented
- **Source:** code/DRYL.Components/Components/Inputs/DrylSegmentedControl.razor
              code/DRYL.Components/Components/Inputs/DrylSegmentedControl.razor.css
              code/DRYL.Components/Components/Inputs/SegmentedSize.cs

## User Story

As a Blazor developer building a toolbar or a view header, I want a compact
switch between a few exclusive modes (List / Board / Calendar) whose selection
glides between the options, so that I can bind the chosen mode with
`@bind-Value` without reaching for a radio group or a tab strip.

## Description

`DrylSegmentedControl<TValue>` renders a glass track holding one equal-width
button per `DrylSegment<TValue>` declared in its `ChildContent`, plus a single
accent indicator under the selected segment. The indicator moves by whole
segment widths and glides on a spring. Segments render no DOM of their own:
they register with the control through a cascading reference, and the control
draws them in registration order.

It is a lightweight `@bind-Value` control, not an `EditForm` input, and unlike
`DrylTabs` it owns no panels — it only reports the chosen value. The group is a
`radiogroup` with a roving tab stop and arrow-key selection.

## Public API

Namespace: `DRYL.Components`. Generic `TValue`; implements `IDisposable`.
`SegmentedSize` and the binding contract are listed in [`_Api.md`](_Api.md).
Segments are declared with [`F21 DrylSegment.md`](F21%20DrylSegment.md).

| Member | Type | Default | Purpose |
|---|---|---|---|
| `Value` | `TValue` | `default!` | The selected value; bind with `@bind-Value`. |
| `ValueChanged` | `EventCallback<TValue>` | — | Fires when the user selects a different segment. |
| `ChildContent` | `RenderFragment?` | `null` | The `DrylSegment<TValue>` declarations. |
| `Size` | `SegmentedSize` | `SegmentedSize.Medium` | Height, padding and type size of the segments. |
| `Block` | `bool` | `false` | Stretches the track to its container's width. |
| `Disabled` | `bool` | `false` | Disables every segment. |
| `AriaLabel` | `string?` | `null` | Accessible name of the group. |
| `Class` | `string?` | `null` | Extra classes merged onto the track. |
| `AdditionalAttributes` | `IDictionary<string, object>?` | `null` | Unmatched attributes, applied to the track. |

## Acceptance Criteria

### Structure

- The track is a `div` carrying `.seg`.
- The track carries `role="radiogroup"`.
- `AriaLabel` is applied as the track's `aria-label`.
- The track exposes the number of segments as the custom property `--seg-count`.
- `AdditionalAttributes` are applied to the track.
- A `class` attribute is merged into the track's classes through `Class`, keeping `.seg` and the size class.
- Each registered segment renders one `button type="button"` carrying `.seg-item`.
- Each segment button carries `role="radio"`.
- Segments render in the order in which they registered.
- A segment's `Icon` renders as a `DrylIcon` carrying `.seg-icon`, before its content.
- A segment's `ChildContent` renders in its button in place of `Label`.
- Without `ChildContent`, a nonempty `Label` renders in a `.seg-label` span.

### Size and layout

- `SegmentedSize.Small` adds `.seg--sm` to the track.
- `SegmentedSize.Medium` adds `.seg--md` to the track.
- `SegmentedSize.Large` adds `.seg--lg` to the track.
- All segments of one track are equally wide.
- Without `Block`, the track is as wide as its content, at most its container's width.
- `Block` adds `.seg--block`, which stretches the track to its container's width.

### Selection

- The segment whose `Value` equals the control's `Value` is selected.
- The selected segment carries `.seg-item--selected`.
- The selected segment carries `aria-checked="true"`.
- Every other segment carries `aria-checked="false"`.
- While a segment is selected, the track renders one `aria-hidden` `.seg-indicator`.
- While no segment matches `Value`, no indicator is rendered.
- The indicator is translated by the selected index times its own width.
- A click on an enabled, unselected segment sets the control's `Value` to the segment's `Value`.
- A click on an enabled, unselected segment invokes `ValueChanged` with the segment's `Value`.
- A click on the already selected segment invokes nothing.
- Selection works without a bound `ValueChanged`; the control then keeps the value itself.

### Disabled

- `Disabled` adds `.seg--disabled` to the track.
- `Disabled` sets the native `disabled` attribute on every segment.
- A segment with its own `Disabled` carries the native `disabled` attribute.
- A disabled segment cannot be selected.

### Keyboard and focus

- The selected segment carries `tabindex="0"`.
- While a segment is selected, every other segment carries `tabindex="-1"`.
- With no segment selected, the first enabled segment carries `tabindex="0"`.
- `ArrowRight` and `ArrowDown` select and focus the next enabled segment, wrapping to the first.
- `ArrowLeft` and `ArrowUp` select and focus the previous enabled segment, wrapping to the last.
- `Home` selects and focuses the first enabled segment.
- `End` selects and focuses the last enabled segment.
- Arrow keys, `Home` and `End` skip disabled segments.
- With no enabled segment, the keys do nothing.
- A keyboard-focused segment shows a `--accent-soft` ring (`:focus-visible`).

### Appearance and motion

- The track reads `--glass-1`.
- The track has a `--line` border.
- The track has the `--r-md` radius.
- The track has `--sp-1` padding.
- The indicator reads `--glass-3`.
- The indicator has an `--accent-line` border.
- The indicator carries a glow mixed from `--accent-line` and `--accent-a`.
- The indicator has the `--r-sm` radius.
- The indicator's movement transitions `transform` on `--dur-med` / `--ease-spring`.
- Under `prefers-reduced-motion: reduce`, the indicator moves without a transition.
- Segment text reads `--fg-muted` at rest.
- A hovered enabled segment reads `--fg`.
- The selected segment reads `--fg`.
- Segment color transitions on `--dur-fast` / `--ease-out`.
- A disabled segment reads at reduced opacity with a `not-allowed` cursor.
- The stylesheet branches on no colour mode (`DESIGN-02`).

### Lifecycle

- Disposing the control clears its segment registrations.

## Recorded debt

- **The indicator snaps in and out.** It mounts when a segment first matches
  `Value` and unmounts when none does, without `DrylPresence` (`DESIGN-12`).
- **Arrow keys keep their browser defaults**; on a scrollable page `ArrowUp`/
  `ArrowDown`/`Home`/`End` also scroll it.
- **Focus is invisible under forced colors.** `.seg-item:focus-visible` removes
  the outline and draws only a box shadow (`UX-02`).
- **`AriaLabel` is not enforced**, and an icon-only segment has no accessible
  name — `DrylSegment` has no `AriaLabel` and the icon is decorative (`UX-01`).
- **Late segments go to the end.** A segment that registers after the first
  render (for example one inside an `@if` that turns true) is appended last,
  whatever its position in the markup.
- **Inline style.** `--seg-count` and the indicator's `translateX` are written
  as inline `style` attributes.
- **Literal values** — the segment heights, horizontal paddings and type sizes
  of `.seg--sm`, `.seg--md` and `.seg--lg`, the indicator's glow blur and mix,
  the focus ring width and the disabled opacity in
  `DrylSegmentedControl.razor.css`; the icon size passed to `DrylIcon`
  (`DESIGN-01`).

## Cross-cutting evidence (`SPEC-05`)

- **Both color modes** — mode tokens only; inspected on `/components/segmented-control` in dark and light (2026-11-20)
- **Enter/exit animation** — **no** enter or exit of its own: an in-flow
  control its host mounts. The selection glides (`DESIGN-11` layout movement),
  and segment colors are transitioned. The snapping indicator mount is
  recorded above.
- **Keyboard and a11y** — `radiogroup` / `radio` / `aria-checked`, a roving
  tab stop, arrow keys with wrap, `Home`/`End`; the gaps are recorded above.
- **AI mode** — **no**: it switches the view of content rather than holding
  content a model writes; a proposed mode belongs to the surrounding surface
  (`AI-05`).
- **Demo page** — `/components/segmented-control`
  (`DRYL.Website/Components/Pages/DemoSegmentedControl.razor`), with the
  examples `Components/Examples/SegmentedControl/Basic.razor`,
  `IconsAndLabels.razor`, `Sizes.razor`, `Block.razor` and `Disabled.razor`.
- **`ComponentCatalog`** — `"Segmented Control"` / `segmented-control`, group
  `Inputs`, `ClassName` `"DrylSegmentedControl"`, AI flag `false` (`REL-04`).
- **Tests** — `tests/DRYL.Components.Tests/ClassMergeTests.cs`
  (`SegmentedControl_merges_consumer_class_without_clobbering`) covers the class
  merge; selection and keyboard criteria have no test of their own.
