# Replace the View Transition API with FLIP

## Meta
- **State:** Ready

## Problem

Raised by the Product Owner on 2026-08-22, after driving `DrylMorph` and
`DrylRouteTransition` in the browser:

> "Beim Route Transition animiert ALLES auf der Seite… Auch Dinge die sich gar
> nicht ändern… Außerdem flackert auch da die GANZE Website… nicht einfach nur
> das Element, was sich morpht… Ich hatte mir das so vorgestellt wie bei
> PowerPoint der Morph Effekt."

Three complaints, all measured in the browser and all traced to one cause:

1. **The whole page animates.** Everything without a `view-transition-name`
   lands in a single `root` snapshot — measured at 1718 × 1248 px, the entire
   viewport — which the browser cross-fades as one image.
2. **A morph reads as a cross-fade.** `old` and `new` are two flat bitmaps.
   Stretched into one box (`object-fit: fill`, measured at 130px against 59.5px
   of natural height) they also distort, which is what read as a shape
   overshooting its target and dissolving.
3. **The entire page flickers once.** This one survives every CSS fix. When a
   view transition starts, the browser replaces the **live page** with snapshots
   and swaps back at the end. Text antialiasing changes, the five
   `backdrop-filter` surfaces are recomposited, gradients re-raster. It is
   visible everywhere at once, for a change to one card.

Point 3 is not a defect to fix — it is what the API does. A `MutationObserver`
proved the alternative explanation wrong: Blazor performs **33 mutations** for
that click, all inside the example, and never touches the shell.

## The alternative, proven before it was adopted

A FLIP prototype was built beside the existing example on the demo page —
**F**irst (measure the source), **L**ast (measure the destination after render),
**I**nvert (transform it back onto the source), **P**lay (animate that away).

Measured side by side on the same page and the same move:

| | View Transition | FLIP |
|---|---|---|
| Page frozen into snapshots | yes — 10 pseudo-elements, whole viewport | **no** |
| Real elements animated | 0 (only snapshots move) | **2** (the card and its content) |

The page stays live throughout, and the content is counter-scaled so type does
not distort while the shape grows — which a view transition cannot do, because
its content is a flat image. The Product Owner drove both and chose FLIP.

## Solution

**Remove the View Transition API from DRYL entirely** and move every morph in
the library onto FLIP.

- A new `IDrylMorph` engine (scoped): morph targets register with it; it
  measures them before a mutation, waits for the render, measures again, and
  animates the difference on the real elements.
- `DrylMorph` keeps its shape — `Name`, `Style`, `As`, `Active` — but registers
  with the engine instead of rendering a `view-transition-name`.
- `DrylRouteTransition` keeps its contract; only its engine changes.
- Every call site moves over: `DrylCard`, the dialog handoff, `DrylTable`'s row
  reorder (FLIP is the classic technique for a list reorder), `DrylCanvas` and
  `DrylCanvasWorkspace`.
- `dryl.viewTransition` in `dryl.js` and every `::view-transition-*` rule in
  `dryl.css` are deleted.

## Scope

- **In scope:**
  - `IDrylMorph` + its implementation and the `dryl.morph` JS bridge.
  - `DrylMorphStyle` replacing `DrylViewTransitionStyle`, with the `DepthGlass`
    tier rebuilt on FLIP: blur and opacity animated on the real element during
    the move, rather than a merge filter over two snapshots.
  - `DrylMorph` and `DrylRouteTransition` rebuilt on the engine.
  - All call sites migrated; all affected specs updated in the same commits.
  - Deletion of `IDrylViewTransition`, `DrylViewTransition`,
    `DrylViewTransitionStyle`, `ViewTransitionAttributes`,
    `DrylCard.ViewTransitionName` / `ViewTransitionStyle`,
    `DialogOptions.HandoffStyle`, `dryl.viewTransition` and the
    `::view-transition-*` stylesheet block.
  - **MAJOR release: 3.0.0**, with a migration section in `CHANGELOG.md`.
- **Out of scope:**
  - Any new token, duration or easing — FLIP uses `--dur-slow` and
    `--ease-viscous` exactly as before.
  - Cross-document navigation transitions. They were never in.
  - Keeping the old names as obsolete forwarders. Explicitly rejected below.

## Impact

*(Tech Lead, `IDEA-05`.)*

### Harness

- No new token, no new animation vocabulary, no new `AiState`, no new
  dependency. The motion values are the ones already defined. **No blocker.**
- `DESIGN-11`/`DESIGN-12` are unaffected: everything that was animated stays
  animated; only the mechanism changes.

### Specs

Seventeen spec files name the view-transition surface and are rewritten with
their code: `E1 Foundation` (`F1`, `_Api`, `_Interop`), `E3 AI` (`F3/S5`,
`F3/_Component`, `F8`, `_Interop`), `E5 Data` (`F16/S5`, `F16/S7`, `F16/S9`,
`F16/_Component`, `_Interop`), `E6 Dialogs` (`F2`, `_Api`, `_Interop`),
`E9 Layout` (`F17`, `_Interop`).

### Public API

**Breaking, and deliberately so — MAJOR (`REL-01`).** Removed:
`IDrylViewTransition`, `DrylViewTransition`, `DrylViewTransitionStyle`,
`DrylCard.ViewTransitionName`, `DrylCard.ViewTransitionStyle`,
`DialogOptions.HandoffStyle`. Added: `IDrylMorph`, `DrylMorphStyle`.
`DialogOptions.AnimateHandoff` survives — the handoff still exists, it is simply
driven differently.

Sixty call sites across 18 code files were counted before the decision.

### Code

- **Risk — measuring costs layout.** FLIP reads `getBoundingClientRect` for every
  registered target, twice per morph. A table reordering many rows must not read
  and write in an interleaved loop; the bridge measures all targets first, then
  writes all transforms.
- **Risk — the element must exist at both ends.** A view transition kept a
  snapshot of something that had been removed; FLIP cannot animate an element
  that is gone. A target present only before, or only after, gets an enter/exit
  treatment rather than a move.
- **Risk — Blazor Server latency.** FLIP needs a JS round trip before and after
  the mutation. That is two interop calls per morph where there was one.

## Decisions

- 2026-08-22 (Product Owner): **FLIP replaces the View Transition API
  everywhere.** Driven side by side in the browser before deciding.
- 2026-08-22 (Product Owner): **harder cut, DRYL 3.0.0.** No obsolete
  forwarders; the names that describe a mechanism the library no longer uses are
  removed rather than left lying about.
- 2026-08-22 (Product Owner): **`DepthGlass` is rebuilt on FLIP**, not dropped —
  blur and opacity on the real element instead of a merge filter over snapshots.
- 2026-08-22 (Product Owner): the engine is **`IDrylMorph`**, matching the
  `DrylMorph` component and the PowerPoint analogy that prompted all of this.

## Open Points

*(none.)*
