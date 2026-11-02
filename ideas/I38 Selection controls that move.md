# Selection controls that move

## Meta
- **State:** Draft

## Problem

Raised by the Product Owner on 2026-11-02, for release 3.16.0, as the next
component in the rework:

> "Komponente um die es geht: DrylCheckbox / DrylRadio / DrylToggle. Eine
> mögliche Idee: Häkchen, das sich zeichnet; der Knopf staucht sich beim
> Drücken und dehnt sich beim Gleiten; ein kurzer Glow-Puls beim Einschalten.
> Nebenbei: .checkbox animiert transition: all"

What the three controls do today (3.15):

1. **The check pops, it does not draw.** `.checkbox::after` scales an L-shaped
   border in with `--ease-spring`.
2. **The accent fill snaps.** `.checkbox:checked`, `.toggle:checked` and the
   checked `.radio-control` set `background: var(--accent-grad)`; a gradient is
   a `background-image` and does not interpolate, so the fill switches in one
   frame while only the glow fades.
3. **`.checkbox` animates `transition: all`** — against `DESIGN-10`'s explicit
   vocabulary and a needless cost on every property change.
4. **The toggle knob is rigid.** It translates; there is no press and no
   travel deformation.
5. **Inconsistencies.** Labels: Checkbox and Toggle use inline `font-size: 13px;
   color: var(--fg-muted)`, Radio uses `.radio-label` at 14px / `--fg`. The
   checkbox radius is the literal `5px`. `prefers-reduced-motion` is handled for
   the radio only.
6. **No specs.** None of `DrylCheckbox`, `DrylRadioGroup`, `DrylRadio`,
   `DrylToggle` has a spec under `specs/E8 Inputs/`.

Target role: a Blazor developer building forms and settings pages, and the end
user toggling options.

## Solution Idea

Three variants, shown side by side with the 3.15 reference in a prototype
artifact (`https://claude.ai/artifact/Rggv2GKQV6eqrp817ELA7c`):

- **A — Tinte.** The PO's idea as given. The gradient fill springs up from the
  centre on its own layer, then the white check draws left to right
  (`clip-path` wipe over a check mask). The toggle knob is two edges: the
  leading edge runs ahead on `--dur-fast`, the trailing one follows on
  `--dur-slow` / `--ease-spring`, so the knob stretches in travel; pressed it
  goes wide and flat. One glow-pulse ring (`--dur-slow`) when a control turns on.
- **B — Flut.** The gradient floods the checkbox as a circle from the centre,
  the check draws after it. The toggle pours its track full behind the knob.
  The radio dot is one shared bead per group that glides to the chosen option
  and stretches in its direction of travel (`dryl.motion.moveIndicator`).
- **C — Funke.** Restrained, like the quiet Primary (I6). Glass at rest, no
  fill: the checked state is an accent hairline, a soft accent tint and a check
  drawn in the gradient. The toggle knob grows from a grey seed into a glowing
  gradient bead (`filter: grayscale()` so nothing snaps); a spark ring when it
  lands. The label brightens from `--fg-muted` to `--fg` with the control.

Shared by all three: `transition: all` replaced by an explicit list; the fill on
its own layer so it fades instead of snapping; a press (`:active`) that squashes
and springs back; pulse/spark only after a user change (a `data-touched` flag
set by the component), never on page load; one label style; `--r-xs` radius;
reduced motion for all three. Because the check is CSS on `.checkbox`, the raw
`.checkbox` inputs in `DrylTable` and `DrylMultiSelect` get it too.

## Scope
- **In scope:** look and motion of `.checkbox`, `.toggle`, `.radio-control`;
  the `DrylCheckbox`, `DrylToggle`, `DrylRadio`, `DrylRadioGroup` markup as far
  as the chosen variant needs it; first specs for the four components.
- **Out of scope:** an indeterminate checkbox state (proposed as its own idea);
  AI styling of the single controls (the group keeps its aura); new sizes.

## Impact
- **Harness:** no new token, duration or easing in any variant
  (`--dur-fast/med/slow`, `--ease-out/in-out/spring`). The pulse/spark is a new
  one-shot `@keyframes` on the existing vocabulary (`DESIGN-10`, `DESIGN-13`
  review). The check is a `mask` with an inline SVG data URI — a shape, not a
  colour. Variant C's checked state carries no fill and needs a hand-measured
  non-text contrast check in light mode (WCAG 1.4.11).
- **Specs:** new `specs/E8 Inputs/` features for `DrylCheckbox`,
  `DrylRadioGroup` (+ `DrylRadio`) and `DrylToggle`; `E8 Inputs/_Api.md`.
- **Public API:** none planned. `data-touched` is internal markup.
- **Code:** `DrylCheckbox.razor`, `DrylToggle.razor`, `DrylRadio.razor`
  (touched flag, label class), `DrylRadioGroup.razor` (B only: glider element
  and JS interop), `wwwroot/dryl.css` (Checkbox, Toggle, Radio group blocks,
  reduced-motion), `wwwroot/js/dryl.js` (B only). Risk in B: glider on wrap,
  resize and prerender without JS.

## Decisions
- 2026-11-02: The rework covers Checkbox, Radio and Toggle together (Product Owner).

## Open Points
- Which variant: A, B, C, or a mix (e.g. A with B's gliding radio bead)?
- Indeterminate state: in this rework or its own idea? (Tech Lead: its own idea.)
