# A morphing tooltip with an open delay

## Meta
- **State:** Draft

## Problem

Raised by the Product Owner on 2026-11-02, for release 3.16.0:

> "Die Tooltips sind ein bisschen langweilig. Ich würde gerne etwas Morph dort
> reinbringen. […] Ich möchte auch, dass man einen Delay einbauen kann […],
> dass die Tooltips nicht alle instant kommen, sondern zum Beispiel erst nach
> 500 Millisekunden."

Two problems, one component:

1. **The bubble does not look like DRYL.** It is an opaque rectangle with a
   fade and a 4 px lift. Every other floating surface in 3.13–3.16 gained a
   signature motion (organic spinners, glass progress cells, morphing panels);
   the tooltip is the one floating surface that still looks generic.
2. **No open delay.** The `F2 DrylTooltip` spec already records this as a gap:
   the bubble opens on the first `pointerover`, so moving the pointer across a
   toolbar flashes every bubble on the way.

Target role: a Blazor developer labelling icon-only controls, and the end user
moving a pointer across them.

## Solution Idea

Three visual variants, shown side by side in a prototype artifact
(`https://claude.ai/artifact/49HQV3aHs8Sygq5iNYMq6w`). All three share one
behaviour change and differ only in the open morph:

- **Shared — glide between triggers.** The library keeps exactly one bubble per
  page. While it is open (or within a short warm-up window after it closed),
  hovering the next trigger moves that same bubble there and morphs its size,
  instead of fading out and in again. The text cross-fades.
- **Shared — `Delay`.** A first open waits `Delay`; a neighbour reached while
  the bubble is warm opens immediately.
- **A — Tropfen.** An accent droplet appears at the trigger edge and the bubble
  grows out of it via `clip-path`, from a pill to a rounded card, on
  `--ease-viscous` / `--dur-slow`. Opaque `--panel-solid`.
- **B — Linse.** Floating glass (`--panel-float` + `--glass-fx-float`), revealed
  by a circular lens opening at the trigger, text blurs into focus, 1 px
  accent-gradient hairline.
- **C — Faden.** A 1 px accent thread grows out of the trigger; the bubble
  unfolds from its tip with `--ease-spring`, and an accent edge draws last.

## Scope
- **In scope:** the bubble's look and its open/close/glide motion; a `Delay`
  parameter; a warm-up window; placement, flip and clamp unchanged.
- **Out of scope:** AI styling (that is I9's question), rich/interactive
  tooltip content, touch affordance.

## Impact
- **Harness:** no new token and no new easing — every variant uses
  `--dur-fast/med/slow` and `--ease-out/in-out/spring/viscous`. The open delay
  is a consumer parameter, not a token; the spec's recorded gap names a delay
  as a maintainer decision (`DESIGN-10`), which this idea puts in front of the
  maintainer. The warm-up window (300 ms) and the glide grace (≈ 90 ms) are
  engine constants in `dryl.js` — to be confirmed as acceptable literals, or
  mapped to `--delay-short`.
- **Specs:** `specs/E7 Feedback/F2 DrylTooltip.md` (Public API, The bubble,
  Appearance, Motion; the "No open delay" gap is closed) and
  `specs/E7 Feedback/_Interop.md` (delay timer, warm-up, glide).
- **Public API:** new `Delay` parameter on `DrylTooltip`, rendered as
  `data-tt-delay` on the wrapper. Proposed type `TimeSpan` (newer pattern, as
  `DrylCommandPalette.Debounce`); alternative `int DelayMs`.
- **Code:** `DrylTooltip.razor`, `window.dryl.tooltip` in
  `wwwroot/js/dryl.js` (timer, warm-up, glide; bubble markup gains inner
  parts), `.tt-portal` rules in `wwwroot/dryl.css` (the padding/radius/font
  literals recorded as debt become tokens). Risk: the glide must respect the
  existing `inFlight` morph guard and the hide-on-removal path.
- **Website:** `DemoTooltip.razor` gains a delay example;
  `ComponentCatalog` description ("CSS-only hover tooltip") is corrected.

## Decisions
- 2026-11-02: A delay becomes configurable per tooltip (Product Owner).

## Open Points
- Which variant — A Tropfen, B Linse or C Faden? Tech Lead recommends **B**:
  closest to DRYL's glass identity and within `DESIGN-06` without an exception.
- Default `Delay`: `0` (3.15-compatible) or 400–500 ms (better default, but
  changes every existing page)?
- Warm-up: always on as an engine behaviour (recommended), or a parameter?
- `Delay` type: `TimeSpan` (recommended) or `int` milliseconds?
