# DrylEmptyState

## Meta
- **State:** Implemented
- **Source:** code/DRYL.Components/Components/Feedback/DrylEmptyState.razor
              code/DRYL.Components/Components/Feedback/DrylEmptyState.razor.css
              code/DRYL.Components/Components/Feedback/EmptyStateSize.cs

## User Story

As a Blazor developer, I want the "there is nothing here" case to look like a
designed state rather than a blank rectangle, with an icon, an explanation and
somewhere to go next, so that an empty table or an unmatched search reads as an
answer instead of as a failure.

## Description

`DrylEmptyState` is a centred column: an icon in a round chip, a headline, a
description, and a slot for the buttons that offer a way out. Every part is
optional, so the same component covers the two-line version inside a dropdown
and the full version on an empty page.

`Size` has two values rather than three, and that is the shape of the problem:
an empty state is either the main thing on the screen or a note inside
something else. `DrylNotifications` uses the small one for its own empty inbox.

While `Ai` is set the component grows a glass frame it does not otherwise have.
An empty state paints no surface at rest — it is text on whatever ground it sits
on — and the rotating gradient ring needs a shape to trace, so the frame appears
with the aura and leaves with it.

## Public API

| Member | Type | Default | Purpose |
|---|---|---|---|
| `Icon` | `string?` | `null` | Icon name shown above the title. |
| `Title` | `string?` | `null` | Headline. |
| `Description` | `string?` | `null` | Supporting text. |
| `ChildContent` | `RenderFragment?` | `null` | Richer description content, rendered after `Description`. |
| `ActionContent` | `RenderFragment?` | `null` | Call-to-action slot, typically buttons. |
| `Size` | `EmptyStateSize` | `EmptyStateSize.Medium` | Overall size. |
| `Ai` | `AiState` | `AiState.None` | Ambient AI state. |
| `Aura` | `AiAura?` | `null` | Pins the aura variant; `null` inherits a surrounding `DrylAiScope`. |
| `Class` | `string?` | `null` | Extra CSS class(es) merged onto the empty state's own classes. |
| `AdditionalAttributes` | `IDictionary<string, object>?` | `null` | Pass-through attributes on the root element. |

`Description` and `ChildContent` share one region rather than replacing each
other: a consumer can pass a plain sentence, markup, or a sentence followed by
markup.

## Acceptance Criteria

### Structure

- The component renders a single root element carrying the empty-state classes.
- The root carries the modifier class of `EmptyStateSize.Small`.
- `EmptyStateSize.Medium` is the unmodified root and adds no modifier class.
- The component renders an icon chip when `Icon` is non-empty.
- The component renders no icon chip when `Icon` is `null` or empty.
- The component renders a title element when `Title` is non-empty.
- The component renders no title element when `Title` is `null` or empty.
- The component renders a description region when `Description` is non-empty.
- The component renders a description region when `ChildContent` is set.
- The description region renders `Description` and then `ChildContent`, so both
  may be supplied at once.
- The component renders an action region when `ActionContent` is set.
- The component renders no action region when `ActionContent` is `null`.
- The icon, the title, the description and the actions render in that order.
- A component with none of its four content parameters set renders an empty
  centred block and throws nothing.
- `Class` is merged onto the root's own classes rather than replacing them.
- `AdditionalAttributes` are applied to the root.

### Layout

- The content is centred on both axes and the text is centre-aligned.
- The description's line length is capped, so a long explanation wraps into a
  readable column rather than spanning the container.
- The action region wraps when it holds more buttons than fit on one line.
- The action region centres its buttons.
- `EmptyStateSize.Small` reduces the block's padding, the gap between its parts
  and the icon chip's size relative to `EmptyStateSize.Medium`.
- `EmptyStateSize.Small` reduces the title's type size relative to
  `EmptyStateSize.Medium`.
- The icon rendered inside the chip is smaller at `EmptyStateSize.Small`.

### Keyboard and accessibility

- The root carries `role="region"`.
- The root carries `Title` as its accessible label when `Title` is non-empty.
- The root carries a fallback accessible label when `Title` is `null` or empty,
  so the region is never announced unnamed.
- The icon chip is decorative and adds no second announcement of the title.
- The component adds no `tabindex` and no key handling: the only focusable
  things inside it are whatever `ActionContent` contains, and they keep their
  own behaviour.
- The action buttons are reached by `Tab` in the order they were written.

### Appearance

- Every color the component renders comes from a token; the component names no
  literal color (`DESIGN-01`).
- The component paints no surface of its own while `Ai` is `AiState.None` — no
  fill, no border, no frost — so it inherits whatever ground it is placed on and
  `DESIGN-06` has nothing to apply to.
- The icon chip paints `--glass-2` with a `--line` border, so the icon reads as
  seated rather than floating.
- The title is `--fg`, the description is `--fg-dim` and the block's own color is
  `--fg-muted`, so the headline is the loudest of the three.
- The icon chip is fully round and the icon inside it is `--fg-dim`, so the
  empty state is quiet rather than alarming.
- The component branches on no color mode and holds no mode-assuming value, so
  the same markup serves light and dark (`DESIGN-02`).

### Motion

- The component has no motion of its own: it is a static block, and its only
  moving parts are the AI aura and whatever `ActionContent` brings.
- Nothing in the component moves under `prefers-reduced-motion: reduce`, because
  nothing moves without it either.

### AI mode

- `Ai` defaults to `AiState.None`, so AI styling is opt-in.
- The aura variant follows `Aura` when set and a surrounding `DrylAiScope`
  otherwise.
- The empty state renders the shared aura vocabulary — ring, comet, glow, wash —
  rather than an empty-state-specific AI treatment (`AI-02`).
- While an aura is present the root gains a `--glass-1` fill, a `--line` border
  and a `--r-lg` radius, so the rotating ring has a shape to trace.
- That frame is removed again when the aura is gone, so an empty state at rest
  is still surfaceless.
- Leaving AI mode keeps the aura mounted for one `--dur-slow` beat, so it
  dissolves rather than snapping away.
- Entering `AiState.Generated` replays the one-shot completion wash, every time
  it is entered.
- The AI state changes the root's fill and border but not its size, so the block
  does not reflow when an operation starts or ends.

## Recorded gaps

- **The frame appearing with the aura is a visible jump.** The fill, the border
  and the radius are switched by a class with no transition, so entering AI mode
  snaps a box around the text. Everything else about the aura fades.
- **The icon chip's dimensions and the type sizes are literals** in
  `code/DRYL.Components/Components/Feedback/DrylEmptyState.razor.css`.
  `DESIGN-01` covers colors, radii, shadows, durations and easings, which are
  tokens here; a chip's diameter and a headline's type size are not covered by a
  token today. Recorded as debt, not as compliance.
- **The description's maximum line length is a literal** in the same file.
- **The fallback accessible label is fixed English** (`"Empty"`), with no
  parameter to change it. Every other string on the component comes from the
  consumer.
- **`role="region"` on a decorative block is heavy.** A landmark is added for
  every empty state on a page, including the small in-panel ones, and each shows
  up in a screen reader's landmark list.
- **No tests of its own.** None of the criteria above is guarded by a test.

## Cross-cutting evidence (`SPEC-05`)

- **Both color modes** — token-only colors, verified by
  `node scripts/check-light-sync.mjs` and
  `node scripts/validate-light-contrast.mjs`. `--glass-1`, `--glass-2`,
  `--line` and the three foreground steps are the mode-dependent tokens; the
  component defines no mode-specific rule.
- **Enter/exit animation** — none of its own, and that is the written exception
  `DESIGN-11` allows: an empty state is what remains when content is absent, it
  is mounted and unmounted by its host, and a host that wants it to fade in
  wraps it in `DrylPresence`. The frame that appears with the aura is the one
  place this shows as a gap, and it is recorded above.
- **Keyboard and a11y** — the "Keyboard and accessibility" criteria above. The
  substantive decision is that the block labels itself from its own title, so a
  screen-reader user hears which region is empty.
- **AI mode** — yes. "Nothing here yet" is exactly the state a model is asked to
  fill, so the component supports showing the work in the space the result will
  occupy.
- **Demo page** — `DRYL.Website/Components/Pages/DemoEmptyState.razor`, with the
  examples `Components/Examples/EmptyState/Default.razor`, `.../Minimal.razor`,
  `.../Small.razor` and `.../AiMode.razor`.
- **`ComponentCatalog`** — registered as `"Empty State"` / `empty-state` in
  `DRYL.Website/Components/ComponentCatalog.cs`, flagged AI-capable.
