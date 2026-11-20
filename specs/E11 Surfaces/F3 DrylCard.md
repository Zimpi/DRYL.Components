# DrylCard

## Meta
- **State:** Implemented
- **Source:** code/DRYL.Components/Components/Surfaces/DrylCard.razor

## User Story

As a Blazor developer laying out a dashboard or a settings page, I want one glass
card that I can drop any content into — with a light that follows the pointer, an
optional 3D tilt and an AI state that frames it — so that every panel in my app
shares the library's surface without me writing its border, fill, padding or
pointer tracking.

## Description

`DrylCard` is the library's in-flow surface. It renders one `div` carrying
`.glass-card` and a padding preset class, and places `ChildContent` inside it. The
surface itself — fill, border, radius, shadow, hover glow — is the global
`.glass-card` rule in `dryl.css`; the component owns no isolated stylesheet.

It has three mutually exclusive pointer modes, decided by two parameters:

- **Depth** (`Depth` set) — the card becomes the same 3D depth-warp surface as
  [`F6 DrylDepthGlass.md`](F6%20DrylDepthGlass.md): it tilts toward the pointer,
  a sheen and a specular highlight parallax above it, and the content floats. The
  card keeps its own `.glass-card` look; only the motion (`.dg-warp`) and the
  intensity class are shared. `Depth` supersedes `Spotlight`.
- **Spotlight** (`Spotlight` is `true`, the default, and `Depth` is `null`) — a
  radial light and an edge light follow the pointer while it hovers the card.
- **Flat** (`Spotlight="false"`, `Depth` `null`) — `ChildContent` is rendered
  directly inside the card with no extra layer.

Pointer tracking is done by `dryl.spotlight` or `dryl.depthglass` in `dryl.js`,
which write custom properties on the card element; all motion is CSS. The tracker
is wired once, after the first render, and released on disposal.

The card is AI-aware: it inherits `DrylAiAware` and composes the shared aura
(`DrylAuraElements` as its first child, `AiAuraCss.Append` on its root), so an
`Ai` state frames the card with the library's one AI vocabulary.

## Public API

Namespace: `DRYL.Components`. Inherits `DrylAiAware`; implements
`IAsyncDisposable`.

| Member | Type | Default | Purpose |
|---|---|---|---|
| `Spotlight` | `bool` | `true` | Renders the pointer-following spotlight. Ignored when `Depth` is set. |
| `Depth` | `DepthGlassIntensity?` | `null` | Turns the card into a 3D depth-warp surface of that intensity; supersedes `Spotlight`. |
| `Padding` | `DrylCard.CardPadding` | `DrylCard.CardPadding.Default` | Padding preset. |
| `ChildContent` | `RenderFragment?` | `null` | Card body. |
| `Class` | `string?` | `null` | Extra CSS class(es) merged onto the card's own classes. |
| `AdditionalAttributes` | `IDictionary<string, object>?` | `null` | Pass-through attributes on the card element (captures unmatched values). |
| `Ai` | `AiState` | `AiState.None` | Inherited from `DrylAiAware`. AI ambient state of the card. |
| `Aura` | `AiAura?` | `null` | Inherited from `DrylAiAware`. Aura variant; `null` inherits a surrounding `DrylAiScope`. |

`CardPadding` is nested in the component (`DrylCard.CardPadding`); its members are
listed in [`_Api.md`](_Api.md). `DepthGlassIntensity` is top-level and owned by
`F6 DrylDepthGlass.md`.

Public members beyond the parameters:

| Member | Kind | Purpose |
|---|---|---|
| `DisposeAsync()` | `ValueTask` | Releases the aura lifecycle and the pointer tracker. |

## Acceptance Criteria

### Structure

- The component renders exactly one `div` as its root.
- The root carries the `glass-card` class.
- The root carries exactly one padding class: `p-0` for `DrylCard.CardPadding.None`,
  `p-tight` for `DrylCard.CardPadding.Tight`, `p-default` for `DrylCard.CardPadding.Default` and
  `p-loose` for `DrylCard.CardPadding.Loose`.
- `Padding` defaults to `DrylCard.CardPadding.Default`.
- With `Depth` set, the root carries `dg-warp`.
- With `Depth` set, the root carries `dg--subtle`, `dg--medium` or `dg--strong`
  for `DepthGlassIntensity.Subtle`, `.Medium` and `.Strong` respectively.
- With `Depth` set, the component renders a `.dg-sheen` layer, a `.dg-specular`
  layer and `ChildContent` inside a `.dg-content` element, in that order.
- With `Depth` set, the component renders no `.spotlight-fx` layer and the root
  does not carry `spotlight-card`, whatever `Spotlight` says.
- With `Depth` `null` and `Spotlight` `true`, the root carries `spotlight-card`.
- With `Depth` `null` and `Spotlight` `true`, the component renders a
  `.spotlight-fx` layer and `ChildContent` inside a `.spotlight-content` element.
- With `Depth` `null` and `Spotlight` `false`, the component renders
  `ChildContent` directly inside the root, with no spotlight or depth layer.
- `Spotlight` defaults to `true`.
- `Depth` defaults to `null`.

### Class merging and attributes

- The root always carries `glass-card`.
- `Class` is merged onto the root's own classes and never replaces them.
- A consumer-supplied `class` attribute is merged the same way, because Blazor
  binds it to the `Class` parameter.
- `AdditionalAttributes` is splatted onto the root.

### Surface

- The card's fill is `--sheen-grad` over `--panel-tint`.
- The card's border is `--line`.
- The card's frost is `--glass-fx-flow`, the in-flow tier (`DESIGN-06`).
- The card's corner radius is `--r-lg`.
- The card's shadow is an `--edge-hi` inset highlight over `--shadow-md`.
- The padding presets read `--sp-4` (`Tight`), `--sp-5` (`Default`) and `--sp-6`
  (`Loose`); `None` applies no padding.
- The root carries `overflow-wrap: anywhere`, so a long unbroken string wraps
  inside the card instead of widening it.
- On hover the border changes to `--line-strong`.
- On hover a radial glow tinted from `--accent-a` fades in behind the card.
- The border, transform and shadow transition on `--dur-med` / `--ease-out`.
- The component branches on no colour mode: both modes come from the token set
  alone (`DESIGN-02`).

### Spotlight

- After the first render in the browser, a spotlight card calls
  `dryl.spotlight.track` on its root.
- The tracker writes the pointer position relative to the card as `--mx` and
  `--my` on the root while the mouse moves over it.
- While the card is hovered, the `.spotlight-fx` radial light, tinted from
  `--accent-a` and `--accent-b`, fades in at the pointer position on `--dur-med`
  / `--ease-out`.
- While the card is hovered, an edge light tinted from `--accent-a` fades in on
  the card's border at the pointer position.
- Both lights fade out when the pointer leaves the card.
- Under an ancestor carrying `data-spotlights="off"`, neither light becomes
  visible on hover.
- The content sits above the spotlight light, so the light never covers text.

### Depth

- After the first render in the browser, a depth card calls
  `dryl.depthglass.track` on its root, and does not call `dryl.spotlight.track`.
- The depth card's tilt, parallax, specular highlight, hover lift and reduced
  motion behaviour are those of `.dg-warp`, specified in
  [`F6 DrylDepthGlass.md`](F6%20DrylDepthGlass.md).
- The depth card keeps the `.glass-card` surface rather than the standalone
  `.dg-surface` look.

### Disposal

- Disposing a card whose tracker was wired calls `dryl.depthglass.untrack` (depth)
  or `dryl.spotlight.untrack` (spotlight) on its root (`CODE-05`).
- Disposing a card whose tracker was never wired — a flat card, or one that never
  rendered in the browser — makes no JS call.
- A failing untrack call (closed circuit, element gone) raises nothing.
- Disposing the card disposes its aura lifecycle.

### AI mode

- `Ai` defaults to `AiState.None`, so a card never given the parameter renders
  without any aura markup or aura class (`AI-03`).
- An explicit `Ai` other than `AiState.None` wins over a surrounding
  `DrylAiScope`; an `Ai` of `AiState.None` inherits the scope's state.
- An explicit `Aura` wins over a surrounding `DrylAiScope`'s variant; with
  neither, the variant is `AiAura.Comet`.
- In an AI state the component renders `DrylAuraElements` as the root's first
  child and the root carries the shared aura classes from `AiAuraCss.Append`
  (`AI-02`).
- The one-shot `Generated` wash is re-keyed on each entry into
  `AiState.Generated`, so a repeated generation replays the bloom (`AI-07`).
- The aura stays mounted and fades out after the effective state drops to
  `AiState.None`, rather than being removed instantly.
- `AiState.Proposed` renders the shared resting proposal look and its visually
  hidden description.
- A change from `AiState.Proposed` straight to `AiState.Generated` adds
  `ai-from-proposed` to the root and plays the shared accept wave.

### Keyboard and accessibility

- The card adds no tab stop, role or key handling of its own; its keyboard
  behaviour is that of the content placed in it.
- A `role` or ARIA attribute supplied by the consumer reaches the root through
  `AdditionalAttributes`.
- The spotlight and depth layers are empty decorative elements that take no
  pointer events.

## Recorded debt

- **Tracking is wired on the first render only.** `Depth` and `Spotlight` are read
  in `OnAfterRenderAsync(firstRender)` alone. Setting `Depth` on a card that first
  rendered flat or as a spotlight adds the depth classes and layers but no tracker,
  so the card never tilts; turning `Spotlight` on later shows a light that sits at
  the card's centre. Changing either at runtime is not supported, and nothing says
  so.
- **The spotlight follows the mouse only.** `dryl.spotlight` listens to
  `mousemove`, not `pointermove`, so pen input gets no light. The light is
  decorative, so no function is lost (`UX-01` is not engaged).
- **The aura may be clipped on the default card.** `.spotlight-card` and
  `.dg-warp` both set `overflow: hidden`, while the aura's halo
  (`.ai-aura-glow`) draws its glow as an outward `box-shadow`. Unlike `.toast` and
  `.tbl-root`, `dryl.css` carries no rule pulling the aura inside these hosts.
  Not verified in a browser; recorded so the eye check looks for it.
- **The `data-spotlights="off"` switch is a hook nobody sets.** The selector
  exists in `dryl.css`; no library component and no service writes the attribute.

## Cross-cutting evidence (`SPEC-05`)

- **Both color modes** — mode tokens only; inspected on `/components/cards` in dark and light (2026-11-20)
- **Enter/exit animation** — **no** enter or exit of its own: the card does not
  mount conditionally, it is placed in the flow. State changes are animated — the
  hover border and glow, the spotlight fade, the depth tilt and lift — and the AI
  aura animates in and out through `AuraLifecycle` (`DESIGN-11`, `DESIGN-12`).
- **Keyboard and a11y** — the card is a non-interactive container: no tab stop,
  no role, decorative layers only; whatever is interactive is the consumer's
  content, reachable in its own order (`UX-01`).
- **AI mode** — **yes**. `DrylCard` inherits `DrylAiAware` and composes
  `DrylAuraElements` and `AiAuraCss.Append`; a card is a bounded surface at rest,
  so the full frame is right (`AI-08`). The `ComponentCatalog` row carries the AI
  flag `true`.
- **Demo page** — `/components/cards`, built from
  `DRYL.Website/Components/Pages/DemoCard.razor` with the examples `Card/Spotlight`,
  `Card/NoSpotlight`, `Card/Padding` and `Card/Depth`. No example shows the `Ai`
  parameter.
- **`ComponentCatalog`** — `"Card"` / `cards`, category `Surfaces`, `ClassName`
  `"DrylCard"`, AI flag `true`, in `DRYL.Website/Components/ComponentCatalog.cs`.
- **Tests** — `tests/DRYL.Components.Tests/DrylDepthGlassTests.cs`
  (`DrylCard_Depth_adds_warp_layers_and_supersedes_spotlight`),
  `tests/DRYL.Components.Tests/ClassMergeTests.cs`
  (`Card_merges_consumer_class_without_clobbering`),
  `tests/DRYL.Components.Tests/AiProposedTests.cs` and
  `tests/DRYL.Components.Tests/AiProposedSweepTests.cs` (proposal and accept wave).
  Padding classes, the flat mode and disposal have no dedicated test.
