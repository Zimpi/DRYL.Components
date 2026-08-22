# DrylIcon

## Meta
- **State:** Implemented
- **Source:** code/DRYL.Components/Components/Data/DrylIcon.razor

## User Story

As a Blazor developer, I want to place a line icon by name and have it inherit
the colour of the text around it, so that every icon in my app matches its
context without me shipping an icon font, an SVG sprite or a second package.

## Description

`DrylIcon` renders one inline `svg` from a built-in set of line icons, selected
by `Name`. The set is a static dictionary of path markup, compiled into the
assembly — there is no request, no sprite sheet and no runtime dependency
(`CODE-03`). The paths come from Lucide under the ISC licence, recorded in
`THIRD_PARTY_NOTICES.md`; the DRYL-side name is the public identifier and the
upstream name is noted beside each entry.

Two properties make it composable everywhere else in the library. It is stroked
in `currentColor`, so an icon in a danger button is red and the same icon in a
muted caption is muted, with no parameter passed. And its accessibility is
opt-in the right way round: an icon is **decorative by default** and hidden from
assistive technology, and becomes an announced image only when the consumer
gives it an `AriaLabel`. That default is what makes it safe for the dozens of
icons the library places inside components that already have their own label.

The set itself is public: `DrylIcon.Icons` can be enumerated, which is what the
docs site's icon gallery does, and what a consumer does to offer an icon picker.

## Public API

| Member | Type | Default | Purpose |
|---|---|---|---|
| `Name` | `string` | `""` (`EditorRequired`) | Icon name; a key of `Icons`. |
| `Size` | `int` | `16` | Rendered width and height in pixels. |
| `StrokeWidth` | `string` | `"2"` | SVG stroke width. |
| `AriaLabel` | `string?` | `null` | Accessible label. `null` makes the icon decorative. |
| `Class` | `string?` | `null` | CSS class(es) applied to the `svg` root. |
| `AdditionalAttributes` | `IDictionary<string, object>?` | `null` | Pass-through attributes on the `svg` root. |
| `Icons` | `static IReadOnlyDictionary<string, string>` | — | The built-in set, keyed by name. |

`Icons` is `public static readonly` and therefore part of the frozen 1.0 surface:
its type, its keys and the fact that it can be enumerated are all binding. Its
*values* — the path markup — are not a contract and may be replaced when an
upstream icon is redrawn.

The component takes **no** `Ai` and no `Aura` — see "AI mode" below.

## Acceptance Criteria

### Structure

- The component renders exactly one `svg` element and nothing around it.
- The `svg` carries a view box of the icon set's native coordinate space, so
  every icon in the set aligns on the same grid.
- The `svg` renders at `Size` in both dimensions, so an icon is always square.
- The `svg` is unfilled and stroked, which is what makes the set a line set
  rather than a solid one.
- The stroke is `currentColor`, so the icon inherits the colour of its
  surrounding text without a parameter.
- The stroke uses `StrokeWidth`.
- The stroke caps and joins are rounded, so a short path does not read as a
  spike.
- `Name` matching a key of `Icons` renders that entry's path markup inside the
  `svg`.
- `Name` matching no key renders an `svg` with no path markup inside it.
- `Class` is applied to the `svg` root.
- `AdditionalAttributes` are applied to the `svg` root.

### The icon set

- `Icons` is exposed as a read-only dictionary keyed by icon name.
- Every entry of `Icons` has non-empty path markup, so no name in the set
  renders blank.
- Adding a name to `Icons` is the only step needed to make it usable, because
  the component looks the name up rather than switching on it.

### Keyboard and accessibility

- `AriaLabel` left `null` marks the `svg` `aria-hidden`, so a decorative icon
  beside its own label is not announced twice.
- `AriaLabel` left `null` applies no role, so the `svg` does not claim to be an
  image with no name.
- `AriaLabel` set applies `role="img"` and that label to the `svg`.
- `AriaLabel` set does not mark the `svg` `aria-hidden`, so a labelled icon is
  reachable by assistive technology.
- The icon is not focusable and adds no stop to the tab order.

### Appearance

- The component renders no colour of its own and therefore names no literal
  colour (`DESIGN-01`).
- The component paints no surface — no fill, no border, no frost — so it
  inherits whatever ground it is placed on and `DESIGN-06` has nothing to apply
  to.
- The component branches on no color mode and holds no mode-assuming value, so
  the same markup serves light and dark (`DESIGN-02`).
- Any colour, glow or accent an icon appears to have comes from the component
  around it, so `DESIGN-08` is answered where the accent is decided rather than
  here.

### AI mode

- The component takes no `Ai` parameter and renders no aura.
- The decision is deliberate (`AI-05`): the icon is a glyph, not a surface. It
  has nothing to paint an aura on and no state of its own to signal, and every
  component that places one — `DrylAvatar`, `DrylBadge`, `DrylStat`,
  `DrylButton` — already carries the AI state for the surface the icon sits in.

## Recorded gaps

- **An unknown `Name` fails silently — and one is live in the library.** A
  misspelt name renders a correctly sized, correctly stroked, entirely empty
  `svg`: an invisible hole in the layout, with nothing in the browser console,
  no exception and no fallback glyph. `Name` is `EditorRequired`, which catches
  an omitted name at compile time but says nothing about a wrong one.
  `DrylFileUpload` asks for `UploadCloud`, and the set has only `Upload` — so
  the drop zone's 32px leading icon has been rendering as empty space rather
  than as a glyph, and nothing anywhere reported it. This is the component's one
  real defect and the reason it is worth fixing first: the silence is what let
  the wrong name survive.

  Comparing the `Name` values passed to `DrylIcon` anywhere under `code/`
  against the keys of `Icons` finds exactly this one mismatch out of 38 names
  in use.
- **`Size` is an `int` of pixels.** A parameter of raw pixels is the one place
  the component contradicts `DESIGN-01`: every call site picks a number, and the
  library's own call sites picked `11`, `13`, `15`, `16` and `20` for what is
  conceptually one small scale. A size token or a size enum would give the set a
  rhythm; today it has a habit.
- **`StrokeWidth` is a `string`.** It reads as a violation of `CODE-02` and is
  not quite one: the value goes straight into an SVG attribute, and a `double`
  would be formatted by the current culture, turning `1.5` into `1,5` on a
  German machine and silently breaking the attribute. The string is the safe
  form; what is missing is the `FormattableString.Invariant` wrapper that would
  let it be typed.
- **The set is not enumerated by a test.** `tests/DRYL.Components.Tests/DrylIconTests.cs`
  asserts that three specific names exist, which guards the icons one feature
  needed rather than the invariant "every name any component uses is in the
  set". That invariant is deliberately **not** an acceptance criterion of this
  spec — it is a statement about the components that call `DrylIcon`, not about
  `DrylIcon`, and a criterion referring to code outside its own component fails
  INVEST's first letter (`SPEC-06`). Its natural home is a test over the whole
  `code/` tree, which is also the only form in which it could have caught
  `UploadCloud`.

## Cross-cutting evidence (`SPEC-05`)

- **Both color modes** — the component names no colour at all; it inherits
  `currentColor`. `node scripts/check-light-sync.mjs` and
  `node scripts/validate-light-contrast.mjs` have nothing of this component's to
  check, which is the strongest form of `DESIGN-02` compliance available.
- **Enter/exit animation** — none, and this is the written exception
  `DESIGN-11` allows: a glyph that renders its own path markup and nothing else
  has no state to transition between and no surface to move. Icons *are*
  animated in DRYL — in `DrylButton`'s press, in `DrylExpansion`'s caret, in
  `DrylSpinner`'s loop — always by the component that places them, which owns
  the state the motion expresses.
- **Keyboard and a11y** — the "Keyboard and accessibility" criteria above. The
  substantive decision is decorative-by-default: the icon is silent unless the
  consumer says otherwise, which is the correct default for a set placed
  overwhelmingly beside existing labels.
- **AI mode** — explicitly no, with the reason under "AI mode" above.
- **Demo page** — `DRYL.Website/Components/Pages/DemoIcons.razor`, with the
  example `Components/Examples/Icons/All.razor`, which enumerates `Icons`
  rather than listing names by hand.
- **`ComponentCatalog`** — registered as `"Icons"` / `icons` in
  `DRYL.Website/Components/ComponentCatalog.cs`, flagged not AI-capable.
- **Tests** — `tests/DRYL.Components.Tests/DrylIconTests.cs` guards three names
  of the set; see the recorded gap above for what it does not guard.
