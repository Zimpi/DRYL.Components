# DrylDepthGlass

## Meta
- **State:** Implemented
- **Source:** code/DRYL.Components/Components/Surfaces/DrylDepthGlass.razor
              code/DRYL.Components/DepthGlassIntensity.cs

## User Story

As a Blazor developer building a showcase or a hero section, I want a glass pane
that tilts toward the pointer while its gloss and content parallax in separate
layers, so that a feature tile reads as a sheet of glass floating above the page
without me writing pointer maths or 3D transforms.

## Description

`DrylDepthGlass` renders one `div` carrying `.dg-surface`, `.dg-warp` and an
intensity class, with three children: a `.dg-sheen` gloss, a `.dg-specular`
highlight and a `.dg-content` element holding `ChildContent`.

The effect is entirely CSS, driven by four custom properties that
`dryl.depthglass` in `dryl.js` writes on the root as the pointer moves: `--tx` and
`--ty` (the pointer's offset from the centre, used for the tilt and the parallax)
and `--mx` and `--my` (the pointer's position, used for the travelling highlight).
`.dg-warp` tilts the whole pane in perspective; the sheen parallaxes against the
tilt, the content with it; the pane lifts on hover. Leaving the pane springs the
tilt back to flat. The intensity class sets the maximum tilt angle and parallax
shift through two local custom properties, `--dg-tilt` and `--dg-par`.

The motion half (`.dg-warp`, `.dg-sheen`, `.dg-specular`, `.dg-content` and the
intensity classes) is shared with `DrylCard`'s `Depth` parameter; the standalone
glass look (`.dg-surface`) is this component's alone. All of it is global CSS in
`dryl.css`, in the "Depth Glass" block; the component has no isolated stylesheet.

`DepthGlassIntensity` is owned here and also used by `DrylCard.Depth`.

## Public API

Namespace: `DRYL.Components`. Implements `IAsyncDisposable`.

| Member | Type | Default | Purpose |
|---|---|---|---|
| `ChildContent` | `RenderFragment?` | `null` | The surface content. |
| `Intensity` | `DepthGlassIntensity` | `DepthGlassIntensity.Medium` | Strength of the tilt and parallax. |
| `Interactive` | `bool` | `true` | Whether the surface tracks the pointer. |
| `Class` | `string?` | `null` | Extra CSS class(es) merged onto the surface's own classes. |
| `AdditionalAttributes` | `IDictionary<string, object>?` | `null` | Pass-through attributes on the surface element (captures unmatched values). |

Public members beyond the parameters:

| Member | Kind | Purpose |
|---|---|---|
| `DisposeAsync()` | `ValueTask` | Releases the pointer tracker. |

The component takes no `Ai` and no `Aura` parameter; see **Cross-cutting
evidence**.

## Acceptance Criteria

### Structure

- The component renders exactly one `div` as its root.
- The root carries `dg-surface` and `dg-warp`.
- The root carries `dg--subtle`, `dg--medium` or `dg--strong` for
  `DepthGlassIntensity.Subtle`, `.Medium` and `.Strong` respectively.
- `Intensity` defaults to `DepthGlassIntensity.Medium`.
- The root carries `dg--static` while `Interactive` is `false`.
- The root holds a `.dg-sheen` element, a `.dg-specular` element and a
  `.dg-content` element, in that order.
- The component renders `ChildContent` inside `.dg-content`.
- `Class` is merged onto the root's own classes and never replaces them.
- `AdditionalAttributes` is splatted onto the root.

### Pointer tracking

- `Interactive` defaults to `true`.
- After the first render in the browser, an interactive surface calls
  `dryl.depthglass.track` on its root.
- A surface with `Interactive` `false` makes no JS call.
- While the pointer moves over the surface, the tracker writes `--tx` and `--ty`
  as the pointer's offset from the centre, and `--mx` and `--my` as its position,
  on the root.
- When the pointer leaves the surface, the tracker resets `--tx` and `--ty`, so
  the pane springs back to flat.
- Disposing a surface whose tracker was wired calls `dryl.depthglass.untrack` on
  its root (`CODE-05`).
- Disposing a surface whose tracker was never wired makes no JS call.
- Disposal on a closed circuit, or of a statically prerendered surface, raises
  nothing.

### Motion

- The pane tilts in perspective toward the pointer, up to the intensity's maximum
  angle.
- The `.dg-sheen` gloss parallaxes against the tilt.
- The `.dg-content` layer parallaxes with the tilt, up to the intensity's maximum
  shift, so the content reads as raised above the glass.
- The `.dg-specular` highlight follows the pointer, resting at the top centre
  before the pointer has entered.
- The pane lifts on hover, and its shadow deepens.
- The pane's tilt and lift transition on `--dur-med` / `--ease-spring`; its
  shadow on `--dur-med` / `--ease-out`.
- The sheen and the content layer transition on `--dur-med` / `--ease-out`.
- Under `prefers-reduced-motion: reduce`, the pane, the sheen and the content
  layer carry no transform and no transition, and the pane does not lift on hover.
- The sheen, the highlight and the content are clipped to the pane's rounded
  shape.

### Surface

- The pane's fill is `--glass-1`.
- The pane's corner radius is `--r-lg`.
- The pane's border and inset edge highlights read `--depth-edge` and
  `--depth-edge-strong`.
- The pane's drop shadow reads `--depth-shadow`.
- The gloss and the highlight are tinted from `--depth-edge-strong`.
- The content is padded by `--sp-5` and coloured `--fg`.
- All mode-dependent colours come from tokens that both LIGHT-TOKEN-SET copies
  redefine; the component branches on no mode (`DESIGN-02`).

### Keyboard and accessibility

- The surface adds no tab stop, role or key handling of its own.
- The gloss and highlight layers are empty, take no pointer events and carry no
  text.

## Recorded debt

- **Hand-written frost on an in-flow surface.** `.dg-surface` in `dryl.css`
  declares `backdrop-filter: blur(8px) saturate(1.5)` instead of `--glass-fx-flow`
  (`DESIGN-06`, `DESIGN-07`); the `DESIGN-07` check line names it. Moving it
  onto `--glass-fx-flow` was tried on 2026-11-20 and withdrawn: with the
  default tier the ground shows sharp through the pane and the glass reads as
  a tinted outline. Which tier the pane belongs to is an open design question.
- **Literal shadow geometry.** The `.dg-surface` shadow and the `.dg-warp:hover`
  shadow write their offsets, blur radii and inset widths as literals around the
  `--depth-*` colour tokens (`DESIGN-01`: every shadow references a variable).
- **Literal motion geometry.** The perspective distance, the hover lift, the
  per-intensity tilt angles and parallax shifts, the sheen overscan and the
  highlight's radius are literals. None of them falls under a token category
  today; named here, not counted as a violation.
- **`dg--static` is a hook without a rule.** `Interactive="false"` adds the class
  and skips the tracker, so the pane does not tilt — but no rule in `dryl.css`
  targets `.dg--static`, so the pane still lifts and deepens its shadow on hover.
  "Not interactive" therefore means "no tilt", not "no hover response".
- **Tracking is decided on the first render only.** Switching `Interactive` from
  `false` to `true` later wires no tracker.

## Cross-cutting evidence (`SPEC-05`)

- **Both color modes** — mode tokens only; inspected on `/components/depth-glass` in dark and light (2026-11-20)
- **Enter/exit animation** — **no** enter or exit: the surface does not mount
  conditionally. Its motion is the pointer-driven tilt, parallax, highlight and
  hover lift above, all on the fixed duration and easing tokens, and all stilled
  under reduced motion (`DESIGN-11`, `UX-06`).
- **Keyboard and a11y** — a non-interactive container; the effect is pointer-only
  and purely decorative, so a keyboard user loses nothing. Content placed inside
  keeps its own tab order (`UX-01`).
- **AI mode** — **no**. `DrylDepthGlass` declares no `Ai` parameter and does not
  inherit `DrylAiAware` (`AI-05`). It is a decorative showcase surface with no
  work of its own for an AI to be doing; a consumer who needs a depth surface that
  shows an AI state uses `DrylCard` with `Depth` and `Ai`, which composes the aura.
  The `ComponentCatalog` row carries the AI flag `false`.
- **Demo page** — `/components/depth-glass`, built from
  `DRYL.Website/Components/Pages/DemoDepthGlass.razor` with the examples
  `DepthGlass/Default` and `DepthGlass/Intensities`. `Interactive="false"` has no
  example.
- **`ComponentCatalog`** — `"Depth Glass"` / `depth-glass`, category `Surfaces`,
  `ClassName` `"DrylDepthGlass"`, AI flag `false`, in
  `DRYL.Website/Components/ComponentCatalog.cs`.
- **Tests** — `tests/DRYL.Components.Tests/DrylDepthGlassTests.cs` (layers,
  intensity classes, tracking on and off, `dg--static`).
