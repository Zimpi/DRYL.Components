# DrylScrollArea

## Meta
- **State:** Implemented
- **Source:** code/DRYL.Components/Components/Layout/DrylScrollArea.razor
              code/DRYL.Components/Components/Layout/DrylScrollArea.razor.css

## User Story

As a Blazor developer building panels such as log viewers, code blocks or long
lists, I want a bounded region that scrolls with the thin DRYL scrollbar, so
that every scrolling panel in my app looks the same without relying on the
browser's global scrollbar.

## Description

`DrylScrollArea` renders one `div.scroll-area` that scrolls vertically, or
horizontally with `Horizontal`. `MaxHeight` and `MaxWidth` bound it with any
CSS length; above the bound the content scrolls. The scrollbar is thin and
tinted with the line token, and the scrollbar styling is scoped to this
component's stylesheet. It is pure CSS — no JS interop.

## Public API

Namespace: `DRYL.Components`.

| Member | Type | Default | Purpose |
|---|---|---|---|
| `MaxHeight` | `string?` | `null` | Maximum height as a CSS length; above it the area scrolls vertically. |
| `MaxWidth` | `string?` | `null` | Maximum width as a CSS length; above it a `Horizontal` area scrolls horizontally. |
| `Horizontal` | `bool` | `false` | Scrolls horizontally instead of vertically. |
| `ChildContent` | `RenderFragment?` | `null` | Scrollable content. |
| `Class` | `string?` | `null` | Classes merged onto the root's own classes. |
| `Style` | `string?` | `null` | Inline declarations merged after the component's own; a `style` attribute binds here too. |
| `AdditionalAttributes` | `IDictionary<string, object>?` | `null` | Captured unmatched attributes applied to the root. |

## Acceptance Criteria

### Structure

- The component renders one `div` carrying `.scroll-area`.
- `ChildContent` renders directly inside the root.
- `Class` is appended to the root's class list.
- Unmatched attributes are applied to the root.
- `Style` is appended to the root's inline style after the component's own
  declarations, so `MaxHeight` and `MaxWidth` stay.
- A consumer `style` attribute binds to `Style` and is merged the same way; it
  does not reach `AdditionalAttributes`.
- For a property both set, the consumer's declaration comes last and wins.

### Direction

- `Horizontal` defaults to `false`.
- Without `Horizontal`, the root scrolls vertically and clips horizontal
  overflow.
- `Horizontal` adds `.scroll-area--horizontal` to the root.
- With `Horizontal`, the root scrolls horizontally and clips vertical overflow.

### Bounds

- `MaxHeight` is written to the root's inline `max-height`.
- `MaxWidth` is written to the root's inline `max-width`.
- With neither set, the root carries no inline `style`.

### Scrollbar

- The scrollbar is thin, with a `--line-strong` thumb on a transparent track
  (`scrollbar-width`, `scrollbar-color`).
- In engines that use the `::-webkit-scrollbar` pseudo-elements, the thumb is
  `--line-strong`, rounded with `--r-pill`, on a transparent track.
- In those engines, hovering the thumb turns it `--accent-line`.
- In those engines, the thumb's color change transitions on `--dur-fast` /
  `--ease-out`.

### Keyboard and accessibility

- The component adds no role, label or `tabindex`; keyboard scrolling follows
  the browser's handling of scroll containers.

## Recorded debt

- **The hover accent is likely dead in current Chromium.** The standard
  `scrollbar-width` / `scrollbar-color` are set on `.scroll-area`; engines that
  support both ignore the `::-webkit-scrollbar` rules, so the `--accent-line`
  hover and its transition do not show there.
- **No accessible name or guaranteed focus stop.** A scroll region holding no
  focusable content relies on the browser to make it keyboard-scrollable, and
  it cannot be given a name except through `AdditionalAttributes` (`UX-01`).
- **Literal geometry (`DESIGN-01`).** The WebKit scrollbar's thickness and the
  thumb's inset border are literal lengths.

## Cross-cutting evidence (`SPEC-05`)

- **Both color modes** — mode tokens only; inspected on `/components/scroll-area` in dark and light (2026-11-20)
- **Enter/exit animation** — **no**, by written exception (`DESIGN-11`): the
  region does not mount conditionally and has no state of its own beyond the
  scrollbar's hover, which is animated as above.
- **Keyboard and a11y** — browser-native scrolling; the missing name and focus
  guarantee are recorded above.
- **AI mode** — **no** (`AI-05`): a scroll container carries no content of its
  own; the AI state belongs to what it scrolls.
- **Demo page** — `/components/scroll-area`
  (`DRYL.Website/Components/Pages/DemoScrollArea.razor`), examples
  `ScrollArea/Vertical` and `ScrollArea/Horizontal`.
- **`ComponentCatalog`** — registered as `"Scroll Area"` / `scroll-area`,
  flagged not AI-capable (`REL-04`).
- **Tests** — none of its own.
