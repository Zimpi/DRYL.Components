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
pill. `Dot` prefixes a small glowing status dot. For success, warning and danger,
the dot keeps the semantic color while the label uses the legible `--fg` token;
neutral and accent dots follow the label. The mark supplements the visible text.

The badge is alive (I40, "Puls & Zähler"). It grows out of its leading mark
when it mounts. A change of `Kind` squishes the pill once and flashes a ring in
the new tone. A dot on `Success`, `Warning` or `Danger` beats in a tempo that
matches the kind, so a reader feels the state before reading it. A label set
through `Value` animates when it changes: a number rolls only the digits that
changed, a word leaves upward while the new one rises in, and the pill's width
follows the label.

The badge is not a control. It has no press, no dismiss and no link — anything
that needs those is a `DrylChip` or a `DrylButton` instead.

## Public API

| Member | Type | Default | Purpose |
|---|---|---|---|
| `Kind` | `DrylBadge.BadgeKind` | `BadgeKind.Neutral` | Colour treatment. |
| `Dot` | `bool` | `false` | Prefixes the label with a glowing dot in the badge's own colour. |
| `Icon` | `string?` | `null` | `DrylIcon` name rendered before the label. |
| `Value` | `string?` | `null` | The label as text. Takes precedence over `ChildContent`; a change animates (digit roll or word swap) and the width follows. |
| `ChildContent` | `RenderFragment?` | `null` | The label, when `Value` is unset. Its changes do not animate, because a `RenderFragment` cannot be compared. |
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
- `ChildContent` is rendered inside a label element in the root.
- `Value` set renders the value instead of `ChildContent`.
- `Value` set renders its text once in a visually hidden element, and its
  animated label, split into per-character or per-digit elements, is
  `aria-hidden`.
- The `Value` label carries its length as `--badge-len`, written with the
  invariant culture.
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
- With `Value`, the accessible name is the current value as one string; the
  split animated copy and a leaving old value are hidden from assistive
  technology.

### Appearance

- Every color the component renders comes from a token; the component names no
  literal color (`DESIGN-01`).
- `BadgeKind.Neutral` is filled with `--glass-2`, outlined with `--line-strong`
  and set in `--fg-muted`.
- `BadgeKind.Accent` is filled with `--accent-soft`, outlined with
  `--accent-line` and set in `--accent-fg`.
- `BadgeKind.Success`, `BadgeKind.Warning` and `BadgeKind.Danger` use `--fg`
  for their small text, and derive their border, fill and optional dot from
  `--success`, `--warning` and `--danger` respectively.
- Small label text reaches the normal-text contrast floor on the default page,
  `--bg-1` and representative flow/floating surfaces in both modes. Evidence uses
  the rendered foreground and composed background, not a semantic token tested
  against an unrelated opaque ground (I13, `UX-03`).
- The pill's corner comes from `--r-pill`.
- The label is set in `--font-mono`, so a badge holding a number does not change
  width as the number changes.
- The dot retains its semantic color for success/warning/danger; for neutral
  and accent it follows the label's color.
- The dot carries a glow of `currentColor`.
- The badge paints no frost, being a small in-flow surface rather than a
  floating one (`DESIGN-06`).
- The accent appears as a soft tint behind a few characters with a 1px border,
  never as the fill of a large surface (`DESIGN-08`).
- The component branches on no color mode and holds no mode-assuming value, so
  the same markup serves light and dark (`DESIGN-02`).

### Motion

- On mount the pill grows out of its leading mark: a `clip-path` opens from the
  left edge over `--dur-slow` on `--ease-spring`, and the label rises in after
  `--delay-short` over `--dur-med` on `--ease-out`. With `Value`, its
  characters rise one after another, each delayed by `--reveal-step` times its
  index.
- The first render never plays a kind change.
- Every later change of `Kind` adds one of two identically drawn change classes,
  alternating, so each change restarts the animation without JS: the pill
  squishes once over `--dur-slow` on `--ease-spring`, and a ring in the new
  tone (`--badge-tone`) flashes outward over `--dur-choreo` on `--ease-out`.
  Fill, border and text glide to the new kind over `--dur-med`.
- A re-render with an unchanged `Kind` or `Value` starts no animation.
- A dot on `Success` beats calmly (3.2 s), on `Warning` quicker (1.8 s), on
  `Danger` in a double beat (1.3 s), each on `--ease-in-out`, infinite.
  Neutral and accent dots rest.
- A `Value` change between two numbers (parsed with the invariant culture)
  rolls only the changed digit columns, right-aligned: up over `--dur-slow` on
  `--ease-spring` when the number grows, down when it shrinks. Unchanged
  columns keep their element and do not move.
- A `Value` change involving a non-number lets the old word leave upward over
  `--dur-fast` while the new word's characters rise in.
- The `Value` label's width is `--badge-len` monospace cells and transitions
  over `--dur-slow` on `--ease-spring`, so the pill follows the label.
- Conditional mounting is the consumer's: a badge that leaves wraps in
  `DrylPresence` (`DESIGN-12`).
- Under `prefers-reduced-motion: reduce` every badge animation and transition
  is off, and leaving words and digits are not drawn; the new state shows at
  once (`UX-06`). The rule's selectors match the specificity of the rules they
  switch off.

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
- **A `ChildContent` label does not animate its changes.** Only `Value` can be
  compared between renders; a consumer who changes the text inside
  `ChildContent` gets the kind change and the beat, but the label snaps.
- **The `Value` width assumes monospace cells.** A label holding wide glyphs
  (CJK, emoji) is wider than `--badge-len` cells and overflows the label box
  into the pill's padding.
- **`Icon` is not part of the accessible name.** The icon inside the badge is
  rendered without an `AriaLabel` and is therefore `aria-hidden`, which is right
  for decoration — but nothing stops a consumer from using the icon *as* the
  label and passing no `ChildContent`, producing a badge that is silent to a
  screen reader.

## Cross-cutting evidence (`SPEC-05`)

- **Both color modes** — token-only colors; synchronization is checked by
  `node scripts/check-light-sync.mjs`. I13's CSS-derived contrast script and
  rendered browser scenarios must establish actual label contrast; the former
  semantic-token check alone did not. `--glass-2`, `--line-strong`,
  `--fg-muted`, `--accent-soft`, `--accent-line`, `--accent-fg`, `--success`,
  `--warning` and `--danger` are the mode-dependent tokens; the component
  defines no mode-specific rule.
- **Enter/exit animation** — enter, kind change, beat and value change as
  under "Motion" (I40); exit through `DrylPresence` at the call site.
- **Keyboard and a11y** — the "Keyboard and accessibility" criteria above. The
  substantive decision is that the badge claims no role, so it does not announce
  itself as something a user could act on.
- **AI mode** — explicitly no, with the reason under "AI mode" above.
- **Demo page** — `DRYL.Website/Components/Pages/DemoBadge.razor`, with the
  examples `Components/Examples/Badge/Kinds.razor`, `.../Dots.razor`,
  `.../Icons.razor` and `.../Live.razor`.
- **`ComponentCatalog`** — registered as `"Badge"` / `badges` in
  `DRYL.Website/Components/ComponentCatalog.cs`, flagged not AI-capable.
- **Tests** — `tests/DRYL.Components.Tests/DrylBadgeTests.cs` guards the child
  content, the unmodified neutral pill, the four modifier classes, the dot
  modifier and the attribute splat, and for I40 the alternating kind-change
  class, `Value` precedence and its accessible copy, `--badge-len`, the digit
  roll and its direction, the word swap, invariant parsing and that an
  unchanged re-render starts nothing; the `Class` merge is guarded separately in
  `tests/DRYL.Components.Tests/ClassMergeTests.cs`. The badge is the
  best-covered component in the category, and the file says so explicitly — it
  doubles as the worked example of how a DRYL component is bUnit-tested.

## I13 contrast verification — 2026-09-17

`validate-light-contrast.mjs` derives its palette and badge rules from actual
CSS and passes all 50 checks, including 40 small-label compositions at 4.5:1.
`ContrastTests` checks 40 rendered badges per mode, including optional dots,
on four surfaces. Semantic labels use `--fg`; their dots, fills and borders
retain the semantic tokens. The browser matrix and visual inspection are
recorded in `docs/2026-09-15-i13-implementation-plan.md`; token synchronization
alone is not the contrast evidence.
