# DrylListItem

## Meta
- **State:** Implemented
- **Source:** code/DRYL.Components/Components/Layout/DrylListItem.razor

## User Story

As a Blazor developer building a settings list, a people list or a nested
outline, I want one row component that carries the parent list's marker, an
optional leading and trailing slot and an optional click action, so that every
row of a list lines up and behaves the same without hand-written flex markup.

## Description

`DrylListItem` renders one `li.list-item` inside a `DrylList`. The row body is a
flex row of *leading | content | end*: the leading position holds the `Start`
slot, else a `DrylIcon` named by `Icon`, else the marker the parent list's
`Variant` asks for. Supplying `OnClick` turns the row body into a native
`button`; without it the body is a `div`. `Selected` marks the row with the
accent line and a glass tint; `Disabled` dims it and makes it inert. The content
may contain a nested `DrylList`, which the parent list indents behind a
connector rail.

The list container, its markers, density and dividers are specified with
`DrylList`; `DrylListItem` reads the marker style from the `DrylList` it is
cascaded from.

## Public API

Namespace: `DRYL.Components`.

| Member | Type | Default | Purpose |
|---|---|---|---|
| `Icon` | `string?` | `null` | `DrylIcon` name shown in place of the list marker. |
| `Disabled` | `bool` | `false` | Dims the row and blocks interaction. |
| `Selected` | `bool` | `false` | Highlights the row with the accent line and a glass tint. |
| `OnClick` | `EventCallback` | Unset | Raised when the row is clicked or activated by keyboard; supplying it makes the row interactive. |
| `Start` | `RenderFragment?` | `null` | Leading slot (avatar, checkbox, custom icon); wins over `Icon` and the marker. |
| `End` | `RenderFragment?` | `null` | Trailing slot pinned to the row's end. |
| `ChildContent` | `RenderFragment?` | `null` | The row's main content; may contain a nested `DrylList`. |
| `Class` | `string?` | `null` | Classes merged onto the `li`'s own classes. |
| `AdditionalAttributes` | `IDictionary<string, object>?` | `null` | Captured unmatched attributes applied to the `li`. |

`DrylList` is received as a cascading parameter; its `DrylList.ListVariant`
(see [`_Api.md`](_Api.md)) selects the marker.

## Acceptance Criteria

### Structure

- The component renders one `li` carrying `.list-item`.
- `Class` is appended to the `li`'s class list.
- Unmatched attributes are applied to the `li`.
- The row body carries `.list-item-row`.
- `ChildContent` renders inside `.list-item-content` within the row body.
- With `End` set, the component renders `.list-item-end` after the row body and
  outside it.
- Without `End`, no `.list-item-end` is rendered.

### Leading position

- With `Start` set, the leading position renders `.list-item-start` holding
  `Start`, and neither an icon nor a marker.
- With `Icon` set and `Start` unset, the leading position renders
  `.list-item-icon` holding a `DrylIcon` with that name.
- With neither, the leading position renders a `span.list-marker` carrying
  `aria-hidden="true"`.
- The marker carries `.list-marker--default` when the parent list's variant is
  `DrylList.ListVariant.Default`.
- The marker carries `.list-marker--disc` for `DrylList.ListVariant.Disc`.
- The marker carries `.list-marker--decimal` for `DrylList.ListVariant.Decimal`.
- The marker carries `.list-marker--dash` for `DrylList.ListVariant.Dash`.
- Under `DrylList.ListVariant.None`, no marker is rendered.
- A row rendered outside any `DrylList` uses the `.list-marker--default` marker.

### Interaction

- With `OnClick` set and `Disabled` false, the row body is a `button` with
  `type="button"`.
- Without `OnClick`, the row body is a `div`.
- Clicking the interactive row fires `OnClick`.
- Activating the focused interactive row with Enter or Space fires `OnClick`
  (native button).
- With `OnClick` set, the `li` carries `.is-interactive`.
- Hovering an interactive row tints its body with `--glass-1`, gives it a
  `--line` border and lifts its text to `--fg`.

### Selected and disabled

- `Selected` adds `.is-selected` to the `li`.
- `Selected` sets `aria-current="true"` on the row body.
- Without `Selected`, the row body carries no `aria-current`.
- A selected row body reads `--glass-2` with an `--accent-line` border on its
  left edge and `--fg` text.
- A selected row's marker reads `--accent-b`.
- `Disabled` adds `.is-disabled` to the `li`.
- A disabled row with `OnClick` renders its body as a `div`, not a `button`.
- A disabled row reads at reduced opacity and ignores pointer events.

### Motion

- The row body transitions `background` and `border-color` on `--dur-fast` /
  `--ease-out`, so hover and selection fade rather than step.

### Keyboard and accessibility

- Tab reaches an interactive, enabled row; a static or disabled row is not a
  tab stop.
- Keyboard focus on an interactive row shows an `--accent-line` border and an
  `--accent-soft` ring (`button.list-item-row:focus-visible`).
- The marker is hidden from assistive technology.

## Recorded debt

- **Disabled is not announced.** A disabled row renders a plain `div` with no
  `aria-disabled`, so assistive technology cannot tell it from a static row
  (`UX-01`). The `disabled` attribute bound on the `button` is therefore never
  rendered true.
- **`.is-interactive` stays on a disabled row** whose body is no longer a
  button.
- **Literal geometry (`DESIGN-01`).** The default marker's dot size, the
  decimal marker's font size, the selected row's edge width, the focus ring's
  spread, the disabled opacity and the `DrylIcon` size are literals.
- **Physical properties.** The selected edge is a `border-left` and the row text
  is `text-align: left`; neither flips in a right-to-left document.

## Cross-cutting evidence (`SPEC-05`)

- **Both color modes** — mode tokens only; inspected on `/components/list` in dark and light (2026-11-20)
- **Enter/exit animation** — **no** of its own: the row does not mount
  conditionally; a consumer that adds or removes rows wraps them in
  `DrylPresence` (`DESIGN-12`). Hover, focus and selection are animated as
  above.
- **Keyboard and a11y** — native `button` semantics for interactive rows,
  visible focus ring, `aria-current` for the selected row; the unannounced
  disabled state is recorded above.
- **AI mode** — **no** (`AI-05`): a row is a slot for content, not a surface;
  AI state belongs to the content or to the surface around the list.
- **Demo page** — `/components/list`
  (`DRYL.Website/Components/Pages/DemoList.razor`), examples `List/Markers`,
  `List/Interactive` and `List/Nested`.
- **`ComponentCatalog`** — no entry of its own; covered by the `"List"` / `list`
  entry (`REL-04`).
- **Tests** — none of its own.
