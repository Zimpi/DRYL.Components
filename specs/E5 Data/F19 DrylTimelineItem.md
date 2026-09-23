# DrylTimelineItem

## Meta
- **State:** Implemented
- **Source:** code/DRYL.Components/Components/Data/DrylTimelineItem.razor
              code/DRYL.Components/Components/Data/DrylTimelineItem.razor.css
              code/DRYL.Components/Components/Data/TimelineVariant.cs

## User Story

As a Blazor developer, I want one event on a timeline to carry its own marker,
its own colour and its own activity state, so that a finished step, a failed one
and one still running are distinguishable at a glance in the same feed.

## Description

`DrylTimelineItem` is one entry of a `DrylTimeline` (`F18`), and it draws the
whole rail for its own row: a circular marker on the leading edge, the
connecting line beneath it, and a body holding a title, a timestamp and free
content.

The marker is where the component's two state systems meet. `Variant` tints it
semantically — a completed step green, a failed one red — and `Ai` wraps it in
the shared aura, so a tool call in flight glows while the steps above it sit
still. The two are independent: a `Success` step can be re-running.

The marker's content follows the same "always something" rule as `DrylAvatar`:
free `MarkerContent` when given, else a `Number`, else an `Icon`, and a plain
dot when none is. The dot takes the marker's own colour, so the variant reads
even without an icon.

An item can also be a **step**: given `OnClick`, the whole row becomes one
native button a user can jump to — the route of a trip, the stages of a
process — and `Active` marks the chosen one as the current step, on the rail
and in the accessibility tree. Without `OnClick` and `Active` the item is the
static event it always was.

The connecting line is drawn by every item except the last, and the bottom
spacing is dropped on the last one, so a timeline ends flush rather than
trailing off.

## Public API

| Member | Type | Default | Purpose |
|---|---|---|---|
| `Title` | `string?` | `null` | Title line for the event. |
| `Timestamp` | `string?` | `null` | Pre-formatted timestamp shown beside the title. |
| `Icon` | `string?` | `null` | `DrylIcon` name in the marker. `null` renders a dot. |
| `Number` | `int?` | `null` | A number in the marker; wins over `Icon`. |
| `MarkerContent` | `RenderFragment?` | `null` | Free marker content; wins over `Number` and `Icon`. |
| `OnClick` | `EventCallback<MouseEventArgs>` | — | Makes the row one native button. |
| `Active` | `bool` | `false` | Marks the item as the current step. |
| `Disabled` | `bool` | `false` | Disables the row's button. |
| `AriaLabel` | `string?` | `null` | Accessible name of the row's button; `null` uses `Title`. |
| `Variant` | `TimelineVariant` | `TimelineVariant.Default` | Colour treatment of the marker. |
| `ChildContent` | `RenderFragment?` | `null` | Body content for the event. |
| `Ai` | `AiState` | `AiState.None` | Ambient AI state; wraps the marker in the aura. |
| `Aura` | `AiAura?` | `null` | Pins the aura variant; `null` inherits a surrounding `DrylAiScope`. |
| `Class` | `string?` | `null` | Extra CSS class(es) merged onto the item's own class. |
| `AdditionalAttributes` | `IDictionary<string, object>?` | `null` | Pass-through attributes on the root element. |

`TimelineVariant`'s members are listed in [`_Api.md`](_Api.md).

## Acceptance Criteria

### Structure

- The component renders a root element holding a gutter and a body, in that
  order.
- The gutter holds the marker and the connecting line.
- The gutter does not shrink, so a long title cannot squeeze the rail.
- `MarkerContent` set renders that content inside the marker and nothing else.
- `Number` set, with no `MarkerContent`, renders the number inside the marker,
  formatted with the invariant culture.
- `Icon` set, with neither `MarkerContent` nor `Number`, renders one `DrylIcon`
  inside the marker.
- None of `MarkerContent`, `Number` and `Icon` set renders a dot inside the
  marker.
- `Title` or `Timestamp` set renders a head row inside the body.
- Neither set renders no head row.
- `Title` set renders the title in the head row.
- `Timestamp` set renders the timestamp in the head row, after the title.
- `ChildContent` set renders a content area inside the body.
- `ChildContent` unset renders no content area.
- An item with none of `Title`, `Timestamp` and `ChildContent` renders a marker
  and an empty body rather than failing.
- The title and the timestamp sit on the same text baseline and wrap onto a
  second line rather than overflowing.
- The body may shrink below its content's intrinsic width, so a long word wraps
  rather than widening the timeline.
- `Class` is merged onto the root's own class rather than replacing it.
- `AdditionalAttributes` are applied to the root.

### The rail

- Every item draws a connecting line beneath its marker.
- The last item in its container draws no connecting line, so the rail ends at
  the last marker.
- The connecting line stretches to fill the height of the item, so a tall body
  and a short one both end at the next marker.
- The connecting line has a minimum height, so two adjacent short items still
  read as connected.
- Every item carries bottom spacing under its body.
- The last item in its container carries none, so a timeline ends flush.

### Steps

- `OnClick` without a delegate renders no button, so a static item is unchanged.
- `OnClick` with a delegate renders one native `button` with `type="button"`
  stretched over the whole row.
- Pressing that button raises `OnClick`.
- The button's accessible name is `AriaLabel`, or `Title` when `AriaLabel` is
  `null`.
- `Active` set on an interactive item puts `aria-current="step"` on its button.
- `Active` set on a static item puts `aria-current="step"` on the root.
- `Active` unset renders no `aria-current`.
- `Disabled` set on an interactive item disables its button and dims the row.
- Operable elements inside `ChildContent` stay above the stretched button, so
  they remain reachable.

### Keyboard and accessibility

- The root carries `role="listitem"`, so the event is counted as one item of the
  timeline's list.
- A static item is not focusable and adds no stop to the tab order; anything
  operable in `ChildContent` is the consumer's own.
- An interactive item adds exactly one tab stop, its button, which Enter and
  Space press natively.
- The button's focus ring is the global `:focus-visible` outline and traces the
  whole row.
- The marker's icon is decorative and is not part of the item's accessible name,
  so the event is announced by its title rather than by its glyph.
- Every aura layer is hidden from assistive technology.

### Appearance

- Every color the component renders comes from a token; the component names no
  literal color (`DESIGN-01`).
- The marker is filled with `--glass-2` and outlined with `--line-strong` by
  default, and set in `--fg-muted`.
- `TimelineVariant.Accent` outlines the marker with `--accent-line` and sets it
  in `--accent-a`.
- `TimelineVariant.Success`, `TimelineVariant.Warning` and
  `TimelineVariant.Danger` each derive the marker's border and text from their
  own semantic token — `--success`, `--warning` and `--danger` respectively.
- The three semantic variants derive their border from the same token as their
  text, so a new semantic colour needs one value rather than two.
- Any `TimelineVariant` value the switch does not match is treated as
  `TimelineVariant.Default`, so an unmapped value still renders a marker.
- The dot takes `currentColor`, so it matches whatever variant the marker is
  without a rule of its own.
- Hovering an enabled interactive item outlines its marker with
  `--accent-line`, sets the marker and the title in `--accent-a`.
- An active item outlines its marker with `--accent-line`, sets it in
  `--accent-a` and rings it with `--glow-accent`.
- The marker's border and color change over `--dur-fast` and its glow over
  `--dur-med`, both with `--ease-out`, so hover and the current step glide
  rather than snap.
- Under `prefers-reduced-motion: reduce` those transitions are removed.
- The connecting line is drawn in `--line`, quieter than any marker.
- The title is set in `--fg`, the body in `--fg-muted` and the timestamp in
  `--fg-dim`, so the three levels of the entry are distinguishable in
  monochrome.
- The timestamp is set in `--font-mono`, so timestamps of different events align
  with each other.
- The marker sits in the flow rather than floating, so it carries no frost
  (`DESIGN-06`).
- The accent appears as a 1px marker border, a small dot and the aura's ring and
  glow — never as the fill of the marker or the row (`DESIGN-08`).
- The component uses its own isolated stylesheet rather than adding to the
  global one.
- The component branches on no color mode and holds no mode-assuming value, so
  the same markup serves light and dark (`DESIGN-02`).

### AI mode

- `Ai` defaults to `AiState.None`, so AI styling is opt-in.
- The aura variant follows `Aura` when set and a surrounding `DrylAiScope`
  otherwise.
- The aura is applied to the **marker**, not to the row, so a running step is
  marked on the rail where a reader is already looking.
- The component renders the shared aura vocabulary through the shared helper
  rather than a timeline-specific AI treatment (`AI-02`).
- `Ai` and `Variant` are independent, so a step can be both semantically
  successful and currently re-running.
- Leaving AI mode keeps the aura mounted for one `--dur-slow` beat, so it
  dissolves rather than snapping away.
- Entering `AiState.Generated` replays the one-shot completion wash, every time
  it is entered.
- The aura lifecycle's timer is disposed with the component.

## Recorded gaps

- **The timestamp is not a time.** It is a pre-formatted `string` rendered into
  a `span`, so a screen reader gets whatever the consumer wrote and nothing
  machine-readable — no `time` element, no `datetime` attribute. A relative
  label like "2 min ago" is therefore frozen text with no underlying instant.
- **The rail's end is positional.** The connecting line and the bottom spacing
  are dropped by a last-child selector, so any non-item element after the last
  item leaves a line dangling — the container-side half of this is recorded in
  `F18`.
- **An arriving item is not animated.** Hover and the current step glide, and
  the aura has its own lifecycle, but an event appended to a feed appears
  instantly, and a `Variant` change swaps colours between two frames
  (`DESIGN-11`, `DESIGN-12`). For an agent trace — the use the component
  documents — a step arriving is the moment the component exists for.
- **The marker's geometry is literal.** The marker's `28px` box, the dot's
  `7px`, the line's minimum height and margins, the body's inner gap and the
  type sizes — the title, time, content and the marker's number — are written
  into `DrylTimelineItem.razor.css` with no token behind them (`DESIGN-01`). The outer gaps and the bottom spacing *are* tokens,
  so the file is half-converted rather than untouched.
- **The marker's size does not follow the icon's.** The icon inside it is
  rendered at a bare pixel size chosen in the component, the same literal-size
  gap `F10` records.
- **The independence of `Ai` and `Variant` is untested.**
  `tests/DRYL.Components.Tests/DrylTimelineItemTests.cs` guards the marker
  precedence, the step button, `aria-current`, `Disabled` and the `Class` merge,
  but not the component's one non-obvious rule.

## Cross-cutting evidence (`SPEC-05`)

- **Both color modes** — token-only colors, verified by
  `node scripts/check-light-sync.mjs` and
  `node scripts/validate-light-contrast.mjs`. `--glass-2`, `--line-strong`,
  `--line`, `--fg`, `--fg-muted`, `--fg-dim`, `--accent-a`, `--accent-line`,
  `--success`, `--warning` and `--danger` are the mode-dependent tokens; the
  component defines no mode-specific rule.
- **Enter/exit animation** — **absent** for the item, and recorded above as debt
  rather than as an exception; the aura's enter, dissolve and completion wash
  are specified above.
- **Keyboard and a11y** — the "Steps" and "Keyboard and accessibility"
  criteria above. The substantive decisions are that the marker's icon never
  enters the accessible name, so the event is announced by its title, and that
  a step is one native button with `aria-current="step"`; the substantive
  omission is the unmachine-readable timestamp, recorded above.
- **AI mode** — yes, and placed deliberately: the aura wraps the marker rather
  than the row, so a running step is signalled on the rail rather than by
  lighting up a paragraph.
- **Demo page** — shown on `DRYL.Website/Components/Pages/DemoTimeline.razor`
  through the examples `Components/Examples/Timeline/EventFeed.razor` and
  `.../AiTrace.razor`.
- **`ComponentCatalog`** — reached through the `"Timeline"` / `timeline` entry
  in `DRYL.Website/Components/ComponentCatalog.cs`; the catalog registers the
  lead component of a family and not its parts.
