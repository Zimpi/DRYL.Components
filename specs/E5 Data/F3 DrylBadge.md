# DrylBadge

## Meta
- **State:** Implemented
- **Source:** code/DRYL.Components/Components/Data/DrylBadge.razor

## User Story

As a Blazor developer, I want to attach a short, colour-coded label to a row, a
heading or a card — a status, a version, a count — so that a reader can classify
the thing beside it without reading a sentence about it.

## Description

`DrylBadge` is the library's smallest labelled surface: a pill holding a few
characters, tinted by one of five semantic kinds. It is content-agnostic — the
label is `ChildContent`, so it takes text, a number or a formatted value alike.

Two optional marks sit before the label. `Icon` renders a `DrylIcon` inside the
pill. `Dot` prefixes a small glowing dot that takes the badge's own foreground
colour, which is what turns a classification into a live status: "Healthy" reads
differently with a green dot pulsing beside it than as green text alone.

The badge is not a control. It has no press, no dismiss and no link — anything
that needs those is a `DrylChip` or a `DrylButton` instead.

## Public API

| Member | Type | Default | Purpose |
|---|---|---|---|
| `Kind` | `DrylBadge.BadgeKind` | `BadgeKind.Neutral` | Colour treatment. |
| `Dot` | `bool` | `false` | Prefixes the label with a glowing dot in the badge's own colour. |
| `Icon` | `string?` | `null` | `DrylIcon` name rendered before the label. |
| `ChildContent` | `RenderFragment?` | `null` | The label. |
| `Class` | `string?` | `null` | Extra CSS class(es) merged onto the badge's own classes. |
| `AdditionalAttributes` | `IDictionary<string, object>?` | `null` | Pass-through attributes on the root element. |

`BadgeKind` is nested in `DrylBadge` and is therefore written qualified —
`DrylBadge.BadgeKind.Success` — unless the file has the component in scope. Its
members are listed in [`_Api.md`](_Api.md).

The component takes **no** `Ai` and no `Aura` — see "AI mode" below.

## Acceptance Criteria

### Structure

- The component renders a single root element carrying the badge class.
- The root carries the modifier class of its `Kind`, one per value.
- `BadgeKind.Neutral` adds no modifier class, being the unmodified pill.
- Any `BadgeKind` value the switch does not match is treated as
  `BadgeKind.Neutral`, so an unmapped value still renders a badge.
- `Dot` set adds the dot modifier class to the root.
- `Icon` set renders one `DrylIcon` inside the root, before `ChildContent`.
- `Icon` unset renders no icon element.
- `ChildContent` is rendered inside the root.
- `Class` is merged onto the root's own classes rather than replacing them.
- `AdditionalAttributes` are applied to the root.
- The root is inline-flex, so a badge sits on the text row of a heading or a
  table cell without breaking the line.

### Keyboard and accessibility

- The badge is not focusable and adds no stop to the tab order, because it is a
  label and not a control.
- The badge carries no role of its own, so its text is announced as the text it
  is.
- The badge's meaning is carried by its label, not by its colour alone, because
  `ChildContent` is the only thing it renders that a screen reader can read.
- The dot is drawn by the stylesheet rather than by markup, so it contributes
  nothing to the accessible name.

### Appearance

- Every color the component renders comes from a token; the component names no
  literal color (`DESIGN-01`).
- `BadgeKind.Neutral` is filled with `--glass-2`, outlined with `--line-strong`
  and set in `--fg-muted`.
- `BadgeKind.Accent` is filled with `--accent-soft`, outlined with
  `--accent-line` and set in `--accent-fg`.
- `BadgeKind.Success`, `BadgeKind.Warning` and `BadgeKind.Danger` each derive
  their text, border and fill from their own semantic token — `--success`,
  `--warning` and `--danger` respectively.
- The three semantic kinds derive their border and fill from the same token as
  their text, so a new semantic colour needs one value rather than three.
- The pill's corner comes from `--r-pill`.
- The label is set in `--font-mono`, so a badge holding a number does not change
  width as the number changes.
- The dot takes `currentColor`, so it matches whatever kind the badge is without
  a rule of its own.
- The dot carries a glow of `currentColor`.
- The badge paints no frost, being a small in-flow surface rather than a
  floating one (`DESIGN-06`).
- The accent appears as a soft tint behind a few characters with a 1px border,
  never as the fill of a large surface (`DESIGN-08`).
- The component branches on no color mode and holds no mode-assuming value, so
  the same markup serves light and dark (`DESIGN-02`).

### AI mode

- The component takes no `Ai` parameter and renders no aura.
- The decision is deliberate (`AI-05`): the badge is a *classification*, and the
  library already has a component for AI provenance at this size — a
  `DrylBadge` with `BadgeKind.Accent` states what a thing is, while
  `DrylAiIndicator` in `E3 AI` states what a model is doing. Giving the badge an
  aura would put two competing AI signals in the same row.

## Recorded gaps

- **The pill's own geometry is literal.** `22px` of height, `10px` of padding,
  `6px` of gap, the `11px` type and the `6px` dot are written into the `.badge`
  rules in `dryl.css` with no token behind any of them, and the icon's size is a
  bare `11` passed from the component (`DESIGN-01`).
- **Nothing is animated.** The badge has no enter, no exit and no transition,
  and the dot that exists to say "live" does not move (`DESIGN-11`,
  `DESIGN-12`). A status badge whose kind changes from `Success` to `Danger`
  snaps between two colours.
- **`Icon` is not part of the accessible name.** The icon inside the badge is
  rendered without an `AriaLabel` and is therefore `aria-hidden`, which is right
  for decoration — but nothing stops a consumer from using the icon *as* the
  label and passing no `ChildContent`, producing a badge that is silent to a
  screen reader.

## Cross-cutting evidence (`SPEC-05`)

- **Both color modes** — token-only colors, verified by
  `node scripts/check-light-sync.mjs` and
  `node scripts/validate-light-contrast.mjs`. `--glass-2`, `--line-strong`,
  `--fg-muted`, `--accent-soft`, `--accent-line`, `--accent-fg`, `--success`,
  `--warning` and `--danger` are the mode-dependent tokens; the component
  defines no mode-specific rule.
- **Enter/exit animation** — **absent**, and recorded above as debt rather than
  as an exception.
- **Keyboard and a11y** — the "Keyboard and accessibility" criteria above. The
  substantive decision is that the badge claims no role, so it does not announce
  itself as something a user could act on.
- **AI mode** — explicitly no, with the reason under "AI mode" above.
- **Demo page** — `DRYL.Website/Components/Pages/DemoBadge.razor`, with the
  examples `Components/Examples/Badge/Kinds.razor`, `.../Dots.razor` and
  `.../Icons.razor`.
- **`ComponentCatalog`** — registered as `"Badge"` / `badges` in
  `DRYL.Website/Components/ComponentCatalog.cs`, flagged not AI-capable.
- **Tests** — `tests/DRYL.Components.Tests/DrylBadgeTests.cs` guards the child
  content, the unmodified neutral pill, the four modifier classes, the dot
  modifier and the attribute splat; the `Class` merge is guarded separately in
  `tests/DRYL.Components.Tests/ClassMergeTests.cs`. The badge is the
  best-covered component in the category, and the file says so explicitly — it
  doubles as the worked example of how a DRYL component is bUnit-tested.
