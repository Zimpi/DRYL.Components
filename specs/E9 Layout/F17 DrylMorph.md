# DrylMorph

## Meta
- **State:** Implemented
- **Source:** code/DRYL.Components/Components/Layout/DrylMorph.razor

## User Story

As a Blazor developer, I want to give a piece of content a transition ID and
have it morph into its counterpart in another view, so that moving from an
overview to a detail reads as one continuous object instead of two unrelated
screens — without me writing animation or timing code.

## Description

`DrylMorph` marks a piece of content as a **shared element**: content that
exists in two views and should travel between them rather than disappear and
reappear. Wrapping the same `Name` around a card in an overview and around the
heading of the detail view is the whole contract — when the switch between the
two runs inside a morph, the browser morphs position, size and opacity
from one to the other.

It is the generic form of something the library already does in two places:
`DrylCard` accepts a `ViewTransitionName`, and a dialog opened from a card takes
over that card's shape. `DrylMorph` makes the same treatment available to
anything — a list row, an image, a heading, a plain `div` — and is what those
call sites express their own morph with.

The component supplies the **naming and the reporting** halves of the morph. The
**starting** half stays with `IDrylMorph`, which the consumer calls to
run the state change: the hull cannot see when other components have finished
rendering, so it never claims to own the moment a transition begins. What it
does own is the half that is easy to forget — reporting its render back, so the
browser knows when the new view has reached the DOM. A consumer who uses
`DrylMorph` never writes `SignalRendered()`.

The component renders one element and nothing else: no wrapper of its own
around the content, no styling, no color. `data-dryl-morph` has no effect
on a `display: contents` box, so the element is real and participates in its
parent's layout; `As` exists so it can be the *right* element in a list, a table
or an article rather than always a `div`.

Everything the morph looks like — its duration, its easing, the `DepthGlass`
merge — is the shared view-transition vocabulary in `dryl.css`. This component
adds no visual of its own and cannot be styled into a second one.

## Public API

| Member | Type | Default | Purpose |
|---|---|---|---|
| `Name` | `string?` | `null` | The transition ID. Two elements sharing it in the old and the new view are morphed into one another. |
| `Style` | `DrylMorphStyle` | `Glide` | How much of the morph vocabulary the element gets — `Glide` or `DepthGlass`. |
| `As` | `string` | `"div"` | The HTML tag rendered as the component's root. |
| `Active` | `bool` | `true` | Whether this instance currently claims `Name`. Set `false` on the entries of an overview that are not the morph target. |
| `ChildContent` | `RenderFragment?` | `null` | The content that morphs. |
| `Class` | `string?` | `null` | CSS class(es) on the rendered element. The hull renders no class of its own, so this is the only one it carries. |
| `AdditionalAttributes` | `IDictionary<string, object>?` | `null` | Pass-through attributes on the root element. |

`DrylMorphStyle` and `IDrylMorph` belong to no single
component and no single category — they are Foundation surface and are due to be
documented in [`../E1 Foundation/_Api.md`](../E1%20Foundation/_Api.md) when that
scaffold is filled. This category's use of the service is recorded in
[`_Interop.md`](_Interop.md).

The component takes **no** `Ai` and no `Aura` — see "AI mode" below.

## Acceptance Criteria

### Rendering the element

- The component renders exactly one element and nothing around it.
- The rendered element's tag is the value of `As`.
- `As` defaults to `"div"`.
- `ChildContent` is rendered inside that element.
- `Class` is rendered as the element's `class`; the component has no class of its own for it to be merged with or to override.
- A consumer's splatted `class` attribute binds to `Class` and is rendered, rather than being dropped.
- `AdditionalAttributes` are applied to the rendered element.
- The component renders no class, color, spacing or border of its own, so it
  changes nothing about how its content looks.

### Claiming a transition name

- A `Name` holding a non-whitespace value renders `data-dryl-morph` with
  that value on the element.
- A `Name` that is `null`, empty or whitespace renders no
  `data-dryl-morph`, so an unnamed hull is inert.
- `Active` set to `false` renders no `data-dryl-morph`, whatever `Name`
  holds.
- `Active` defaults to `true`.
- Toggling `Active` from `false` to `true` on an already-rendered instance
  renders the `data-dryl-morph` without the element being recreated.

### The morph tiers

- `Style` defaults to `DrylMorphStyle.Glide`.
- `Style` accepts exactly the two values of `DrylMorphStyle`.
- `Style` set to `DepthGlass` renders `data-dryl-morph-depth` on the
  element in addition to the name.
- `Style` set to `DepthGlass` renders the `data-dryl-morph-depth` marker attribute, so
  the JS bridge injects the merge filter the tier needs.
- `Style` set to `Glide` renders neither `view-transition-class` nor
  `data-dryl-morph-depth`.
- Neither marker is rendered while the element claims no name, so an inert hull
  costs nothing.

### Reporting the render

- The component reports every one of its renders to
  `IDrylMorph.SignalRendered()`.
- The report is made unconditionally, without the component checking whether a
  transition is in flight.
- The component reports its render even while it claims no name, so an instance
  that is only the *destination* of a morph still closes the timing loop.
- A consumer using `DrylMorph` never calls `SignalRendered()` itself.

### Behaviour where the morph cannot run

- The component renders its element and its content unchanged during prerender.
- The component makes no JS interop call of its own, so it has nothing to
  dispose and nothing that can fail on a disconnected circuit.
- The component behaves identically when the browser has no morph engine:
  the markup is inert rather than broken, and the state change still happens
  (`IDrylMorph` falls back to applying it directly).
- The component renders the same markup under `prefers-reduced-motion`; the
  reduced-motion opt-out lives in the shared vocabulary in `dryl.css`, not in
  the component.

### Keyboard and accessibility

- The component renders no `role` and no ARIA attribute of its own, so it never
  changes what its content is announced as.
- The component is not focusable and adds no stop to the tab order.
- `As` lets the hull be the element its context requires (`li` inside a list,
  `article` in a feed), so wrapping content never produces invalid or
  meaningless structure.
- The component binds no key handler and intercepts no key event.

### Appearance

- The component names no color, length, duration or easing (`DESIGN-01`); the
  morph's duration and easing come from `--dur-slow` and `--ease-viscous` in the
  shared `the morph engine` rule.
- The component adds no stylesheet of its own — the entire morph vocabulary is
  the existing `the morph engine` rules in `dryl.css`.
- The element's content is counter-scaled for the length of the move, so type
  and iconography keep their proportions while the shape changes size.
- The component paints no frost, being a transparent hull rather than a surface
  (`DESIGN-06`).
- The component renders no accent, so `DESIGN-08` has nothing to apply to.
- The component branches on no color mode and holds no mode-assuming value, so
  the same markup serves light and dark (`DESIGN-02`).

### AI mode

- The component takes no `Ai` parameter and renders no aura.
- The decision is deliberate (`AI-05`): the hull is transparent by contract — it
  renders no surface of its own, so there is nothing for an aura to sit on. A
  surface that *is* AI-driven carries its own `Ai` inside the hull, where the
  aura belongs, and morphs with it.

## Recorded gaps

- **A morph needs the element to exist at both ends.** The engine measures a
  target before the change and again after it; a name present on only one side
  is treated as an arrival (it fades in) or simply as content that left. There
  is no way to animate an element that is no longer rendered — the snapshots
  that used to make that possible are gone with the View Transition API, and
  with them the whole-page flicker they cost.
- **A morph that ends under a resting pointer lights up once more.** When the
  revealed element sits where the pointer already is — the normal case, because
  the pointer is still on the control that was pressed — its hover transitions
  start as the move finishes, adding a `--dur-med` colour settle after it. This
  is correct browser behaviour and is recorded so it is recognised rather than
  re-investigated.
- **`SignalRendered` needs someone to render.** The hull closes the timing loop
  from its own `OnAfterRender`, so a mutation that renders no `DrylMorph` at all
  leaves the engine waiting for geometry it will never measure. Every shape the
  component is built for renders at least one hull in the same batch.
- **Two live hulls must not share a name.** The engine matches targets by name;
  a duplicate makes it measure one element and move another. That is what
  `Active` is for, and it is guarded by a test rather than by the browser.

## Cross-cutting evidence (`SPEC-05`)

- **Both color modes** — the component renders no color at all; what moves is
  the consumer's own content, in whatever colors it already had. Verified by
  `node scripts/check-light-sync.mjs` and
  `node scripts/validate-light-contrast.mjs` remaining unaffected, and by eye in
  both modes on the demo page.
- **Enter/exit animation** — the component *is* the motion: its entire purpose
  is the morph between two views. It deliberately has no enter or exit animation
  of its own, because an animated hull would fight the morph it exists to
  enable — content that also needs to enter or leave composes `DrylPresence`
  inside it. This is the written exception `DESIGN-11` allows.
- **Keyboard and a11y** — the "Keyboard and accessibility" criteria above. The
  substantive decision is that the hull is semantically invisible: it adds no
  role, no focus stop and no announcement, and `As` exists so it does not force
  a `div` into places where a `div` is wrong.
- **AI mode** — explicitly no, with the reason under "AI mode" above.
- **Demo page** — `DRYL.Website/Components/Pages/DemoMorph.razor`, with the
  examples `Components/Examples/Morph/OverviewToDetail.razor` (cards growing
  into a detail panel, with a Glide/DepthGlass switch) and
  `.../Morph/LongList.razor` (one name claimed by one row of twenty). Demos live
  in the `DRYL.Website` repository (`CODE-20`).
- **`ComponentCatalog`** — registered as `"Morph"` / `morph` in
  `DRYL.Website/Components/ComponentCatalog.cs`, flagged not AI-capable.
