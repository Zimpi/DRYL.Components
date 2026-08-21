# DrylTimeline

## Meta
- **State:** Implemented
- **Source:** code/DRYL.Components/Components/Data/DrylTimeline.razor
              code/DRYL.Components/Components/Data/DrylTimeline.razor.css

## User Story

As a Blazor developer, I want a sequence of events shown as a vertical rail, so
that an activity feed, an audit log or an agent's steps read as one ordered
story rather than as a stack of unrelated rows.

## Description

`DrylTimeline` is a container for `DrylTimelineItem` entries (`F19`). It is
deliberately almost empty: it stacks its items and declares them a list for
assistive technology, and that is all. The rail a reader sees — the marker, the
connecting line, the spacing under each entry — is drawn by each item, not by
the container.

That split is what lets an item be styled by its own state. A marker's colour
follows the item's `Variant`, and a marker in AI mode carries the shared aura,
so an agent trace can show which step is in flight. A container that drew the
rail itself would have to know all of that.

The container cascades nothing and holds no state, so an application that
renders its events from a collection binds the loop itself.

## Public API

| Member | Type | Default | Purpose |
|---|---|---|---|
| `ChildContent` | `RenderFragment?` | `null` | The `DrylTimelineItem` entries. |
| `AriaLabel` | `string` | `"Timeline"` | Accessible label for the list. |
| `Class` | `string?` | `null` | Extra CSS class(es) merged onto the timeline's own class. |
| `AdditionalAttributes` | `IDictionary<string, object>?` | `null` | Pass-through attributes on the root element. |

The component takes **no** `Ai` and no `Aura` — see "AI mode" below.

## Acceptance Criteria

### Structure

- The component renders a single root element holding its items.
- The items are stacked vertically in the order they were written.
- `ChildContent` is rendered directly into the root, with no wrapper per item.
- `Class` is merged onto the root's own class rather than replacing it.
- `AdditionalAttributes` are applied to the root.
- The container draws no rail, no marker and no spacing of its own; each item
  draws its own.

### Keyboard and accessibility

- The root carries `role="list"`, so the events are announced as a list with a
  count rather than as loose text.
- The root carries `AriaLabel` as its accessible label.
- `AriaLabel` has a non-null default, so a timeline is never an unlabelled list.
- The timeline is not focusable and adds no stop to the tab order; anything
  operable inside an item is the item's own.
- The component binds no key handler and manages no focus, because it owns no
  interaction.

### Appearance

- The component renders no colour of its own and therefore names no literal
  colour (`DESIGN-01`); the colours belong to the items.
- The timeline paints no surface of its own — no fill, no border, no frost — so
  it inherits whatever ground it is placed on and `DESIGN-06` has nothing to
  apply to.
- The component renders no accent, so `DESIGN-08` has nothing to apply to.
- The component uses its own isolated stylesheet rather than adding to the
  global one.
- The component branches on no color mode and holds no mode-assuming value, so
  the same markup serves light and dark (`DESIGN-02`).

### AI mode

- The component takes no `Ai` parameter and renders no aura.
- The decision is deliberate (`AI-05`) and is the reason the family is split in
  two: in an agent trace, one step is thinking while the steps above it are
  finished. The state belongs per event, so `DrylTimelineItem` carries `Ai` and
  the rail around it does not. An aura on the whole timeline would say the
  entire history was in flight.

## Recorded gaps

- **The list role is asserted rather than structural.** The container is a `div`
  with `role="list"` and each item a `div` with `role="listitem"`, instead of an
  `ol`/`li` pair that would carry the same semantics natively and survive a CSS
  reset, a copy-paste into another document or an `AdditionalAttributes` splat
  that happens to set a role.
- **Anything but an item breaks the rail.** Each item hides its connecting line
  when it is the last child of the container. That test is positional, so a
  consumer who places any other element after the last item — a footer, a "load
  more" row, a conditional block that renders something — gets a line dangling
  off the end of the timeline into nothing.
- **Nothing is animated.** The timeline is the component in this category whose
  content most obviously arrives over time — an event feed, an agent trace — and
  neither the container nor a newly appended item is animated in any way
  (`DESIGN-11`, `DESIGN-12`). It neither is nor wraps a `DrylPresence`.
- **No tests of its own.** None of the criteria above is guarded by a test; the
  `Class` merge is the one thing about this component that is, in
  `tests/DRYL.Components.Tests/ClassMergeTests.cs`.

## Cross-cutting evidence (`SPEC-05`)

- **Both color modes** — the component names no colour at all, so
  `node scripts/check-light-sync.mjs` and
  `node scripts/validate-light-contrast.mjs` have nothing of its own to check;
  the mode-dependent tokens are the items'.
- **Enter/exit animation** — **absent**, and recorded above as debt rather than
  as an exception.
- **Keyboard and a11y** — the "Keyboard and accessibility" criteria above. The
  substantive decision is the always-labelled list role; its structural weakness
  is recorded above.
- **AI mode** — explicitly no, with the reason under "AI mode" above.
- **Demo page** — `DRYL.Website/Components/Pages/DemoTimeline.razor`, with the
  examples `Components/Examples/Timeline/EventFeed.razor` and
  `.../AiTrace.razor`.
- **`ComponentCatalog`** — registered as `"Timeline"` / `timeline` in
  `DRYL.Website/Components/ComponentCatalog.cs`, flagged AI-capable — the flag
  refers to the items, which is where `Ai` lives.
