# DrylSegment

## Meta
- **State:** Implemented
- **Source:** code/DRYL.Components/Components/Inputs/DrylSegment.razor

## User Story

As a Blazor developer, I want to declare each option of a segmented control as
a small child component with a value, a label and an optional icon, so that
the options read in markup the way they appear on screen.

## Description

`DrylSegment<TValue>` declares one option of a `DrylSegmentedControl<TValue>`.
It renders no DOM of its own: on initialization it registers with the
cascading parent control, which draws its button, and it unregisters when
disposed. When its `Disabled`, `Label` or `Icon` change, it asks the parent to
re-render. Placed outside a segmented control, it throws.

How a registered segment is drawn, selected and navigated is specified in
[`F20 DrylSegmentedControl.md`](F20%20DrylSegmentedControl.md).

## Public API

Namespace: `DRYL.Components`. Generic `TValue`; implements `IDisposable`.

| Member | Type | Default | Purpose |
|---|---|---|---|
| `Value` | `TValue?` | `default` | The value this segment selects; `[EditorRequired]`. |
| `Label` | `string?` | `null` | Text of the segment; optional when `ChildContent` is set. |
| `Icon` | `string?` | `null` | Leading `DrylIcon` name. |
| `Disabled` | `bool` | `false` | Makes this segment unselectable and skips it in keyboard navigation. |
| `ChildContent` | `RenderFragment?` | `null` | Custom content, rendered in place of `Label`. |

## Acceptance Criteria

### Registration

- The component renders no markup of its own.
- On initialization, the segment registers with the cascading `DrylSegmentedControl<TValue>`.
- A segment without a cascading `DrylSegmentedControl<TValue>` throws `InvalidOperationException` with the message `DrylSegment must be placed inside a DrylSegmentedControl.`
- Registering the same segment twice adds it once.
- Disposing the segment unregisters it from the control.
- Unregistering re-renders the control without that segment.

### Parameter changes

- A change of `Disabled` re-renders the parent control.
- A change of `Label` re-renders the parent control.
- A change of `Icon` re-renders the parent control.
- A re-render with unchanged `Disabled`, `Label` and `Icon` does not ask the parent to re-render.

### Content

- The parent renders the segment's `ChildContent` in place of its `Label`.
- The parent renders the segment's `Icon` before its content.
- A segment with its own `Disabled` is rendered disabled and is not selectable.

## Recorded debt

- **A changed `Value` or `ChildContent` does not notify the parent.** The parent
  reflects it only on its next own render.
- **No accessible name of its own.** There is no `AriaLabel`; an icon-only
  segment is an unnamed radio (`UX-01`).
- **The `TValue` must match the parent's exactly** — a segment whose `TValue`
  differs finds no cascading control and throws.

## Cross-cutting evidence (`SPEC-05`)

- **Both color modes** — mode tokens only; inspected on `/components/segmented-control` in dark and light (2026-11-20)
- **Enter/exit animation** — **no**: the segment renders nothing; its button
  and the gliding indicator belong to `DrylSegmentedControl` (see its spec).
- **Keyboard and a11y** — delegated to `DrylSegmentedControl`; the missing
  per-segment name is recorded above.
- **AI mode** — **no**, for the same reason as `DrylSegmentedControl`: an option
  of a view switch holds no model-written content (`AI-05`).
- **Demo page** — `/components/segmented-control`
  (`DRYL.Website/Components/Pages/DemoSegmentedControl.razor`); every example
  under `Components/Examples/SegmentedControl/` declares its options with
  `DrylSegment`.
- **`ComponentCatalog`** — no entry of its own; covered by the
  `"Segmented Control"` / `segmented-control` entry (`REL-04`).
- **Tests** — none of its own; `tests/DRYL.Components.Tests/ClassMergeTests.cs`
  renders the parent control without segments.
