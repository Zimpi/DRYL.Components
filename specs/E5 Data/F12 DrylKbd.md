# DrylKbd

## Meta
- **State:** Implemented
- **Source:** code/DRYL.Components/Components/Data/DrylKbd.razor
              code/DRYL.Components/Components/Data/DrylKbd.razor.css

## User Story

As a Blazor developer, I want to show a keyboard shortcut the way a keyboard
shows it, so that a user scanning a menu, a command palette or a tooltip can
tell a shortcut apart from the words around it at a glance.

## Description

`DrylKbd` renders a shortcut as one or more key caps. It has two forms and picks
between them by which parameter is set.

Given `Keys`, it renders a **chord**: one cap per key, joined by a separator
that is decoration rather than content. Given `ChildContent` instead, it renders
a **single cap** holding whatever was passed — which is the form to use for a
composed glyph like `⌘K` that is one key press rather than two.

The component takes no view on what a key is called. It does not translate
`Ctrl` to `⌘` on macOS, does not know which platform the browser is on, and does
not reorder modifiers. A consumer who wants platform-aware shortcuts computes
the strings and passes them, which keeps the component from guessing wrong in
the one place a wrong guess is unrecoverable.

It is pure markup and CSS — no interop, no measurement, nothing to dispose.

## Public API

| Member | Type | Default | Purpose |
|---|---|---|---|
| `Keys` | `string[]?` | `null` | The keys of a chord, each in its own cap. When set, `ChildContent` is ignored. |
| `Separator` | `string` | `"+"` | Text shown between chord caps. |
| `ChildContent` | `RenderFragment?` | `null` | Content of a single cap. Ignored when `Keys` is set. |
| `Class` | `string?` | `null` | Extra CSS class(es) merged onto the component's own classes. |
| `AdditionalAttributes` | `IDictionary<string, object>?` | `null` | Pass-through attributes on the root element. |

The component takes **no** `Ai` and no `Aura` — see "AI mode" below.

## Acceptance Criteria

### Choosing a form

- `Keys` holding at least one entry renders the chord form.
- `Keys` left `null` renders the single-cap form.
- `Keys` set to an empty array renders the single-cap form, so an empty chord
  does not render an empty group.
- The chord form ignores `ChildContent` entirely.

### The chord form

- The chord form renders a group element as its root.
- The chord form renders one `kbd` element per entry of `Keys`.
- The caps appear in the order the entries appear in `Keys`.
- A separator element is rendered between every pair of adjacent caps.
- No separator is rendered before the first cap or after the last one.
- The separator element renders `Separator`.
- `Class` is merged onto the group's own class rather than replacing it.
- `AdditionalAttributes` are applied to the group.

### The single-cap form

- The single-cap form renders one `kbd` element as its root, with no group
  around it.
- `ChildContent` is rendered inside that `kbd`.
- `Class` is merged onto the cap's own class rather than replacing it.
- `AdditionalAttributes` are applied to the cap.

### Keyboard and accessibility

- Every key is rendered as a `kbd` element, so assistive technology announces it
  as keyboard input rather than as prose.
- The separator is hidden from assistive technology, so a chord is announced as
  its keys and not as "Ctrl plus K".
- The component is not focusable and adds no stop to the tab order, because it
  displays a shortcut rather than offering one.
- The component binds no key handler; showing a shortcut and handling it are
  separate jobs and this component does the first one only.

### Appearance

- Every color the component renders comes from a token; the component names no
  literal color (`DESIGN-01`).
- A cap is filled with `--glass-2`, outlined with `--line-strong` and set in
  `--fg-muted`.
- A cap carries a hairline of `--line-strong` under it, so it reads as a raised
  key rather than as a flat box.
- A cap's corner comes from `--r-xs`.
- A cap's text is set in `--font-mono`, so caps of different keys align on the
  same character grid.
- A cap is at least as wide as it is tall, so a single-letter key renders as a
  square rather than as a sliver.
- The separator is set in `--fg-dim`, quieter than the caps it joins.
- The gap between caps comes from `--sp-1`.
- The component uses its own isolated stylesheet rather than adding to the
  global one.
- The cap paints no frost, being a small in-flow surface rather than a floating
  one (`DESIGN-06`).
- The component renders no accent, so `DESIGN-08` has nothing to apply to.
- The component branches on no color mode and holds no mode-assuming value, so
  the same markup serves light and dark (`DESIGN-02`).

### AI mode

- The component takes no `Ai` parameter and renders no aura.
- The decision is deliberate (`AI-05`): a key cap is a piece of typography that
  names a physical key. Nothing about it is ever in progress, so there is no
  state for the aura vocabulary to express, and the surfaces that *do* place
  shortcuts — `DrylCommandPalette`, `DrylMenu`, `DrylTooltip` — carry their own
  AI state around it.

## Recorded gaps

- **The cap's geometry is literal.** `20px` of minimum width and height and the
  `11px` type are written into `.kbd` in `DrylKbd.razor.css` with no token
  behind them, as is the separator's type size (`DESIGN-01`). The paddings and
  the gap *are* tokens, so the file is half-converted rather than untouched.
- **`Separator` is rendered but never used as a value.** It is a `string`
  parameter whose only job is to be displayed, which is correct — but it is also
  the reason the chord form cannot be given a separator that is markup, an icon
  or nothing at all. Passing an empty string renders an empty element that still
  occupies a gap.
- **Nothing is animated.** The component has no enter, no exit and no
  transition of any kind (`DESIGN-11`, `DESIGN-12`). Unlike `DrylIcon` this is
  recorded as debt rather than claimed as an exception: a key cap has an obvious
  thing to animate — the press — and several DRYL surfaces show a shortcut at
  the moment it is being used.
- **No tests of its own.** None of the criteria above is guarded by a test,
  including the form-selection rule, which is the component's only logic, and
  the component is absent from `tests/DRYL.Components.Tests/ClassMergeTests.cs`
  despite carrying a `Class` parameter in two different roots.

## Cross-cutting evidence (`SPEC-05`)

- **Both color modes** — token-only colors, verified by
  `node scripts/check-light-sync.mjs` and
  `node scripts/validate-light-contrast.mjs`. `--glass-2`, `--line-strong`,
  `--fg-muted` and `--fg-dim` are the mode-dependent tokens; the component
  defines no mode-specific rule.
- **Enter/exit animation** — **absent**, and recorded above as debt rather than
  as an exception.
- **Keyboard and a11y** — the "Keyboard and accessibility" criteria above. The
  substantive decision is the hidden separator: the chord is announced as its
  keys, so a screen-reader user hears the shortcut rather than its punctuation.
- **AI mode** — explicitly no, with the reason under "AI mode" above.
- **Demo page** — `DRYL.Website/Components/Pages/DemoKbd.razor`, with the
  examples `Components/Examples/Kbd/SingleKey.razor`, `.../Chords.razor` and
  `.../Inline.razor`.
- **`ComponentCatalog`** — registered as `"Keyboard Key"` / `kbd` in
  `DRYL.Website/Components/ComponentCatalog.cs`, flagged not AI-capable.
