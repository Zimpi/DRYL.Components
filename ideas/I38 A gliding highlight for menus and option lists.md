# A gliding highlight for menus and option lists

## Meta
- **State:** Draft
- **Specs:** none yet for `DrylMenu`, `DrylMenuItem`, `DrylSelect`, `DrylMultiSelect`, `DrylAutocomplete`, `DrylTimePicker` (phase C); the rework writes them

## Problem

Raised by the Product Owner on 2026-11-02, for release 3.16.0:

> "Komponente um die es geht: DrylMenu / DrylMenuItem, dazu die Optionslisten
> von Select, MultiSelect, Autocomplete, TimePicker. Eine mögliche Idee: Eine
> gemeinsame Highlight-Pill, die zur Zeile unter Maus oder Tastatur gleitet,
> wie die Drawer-Pill aus 3.13 (navGlide), plus Hover-Light."

Since the 3.13 visual refresh the drawer's active row glides on a spring and a
soft light follows the pointer (I29). The menu and the four option lists still
highlight the old way: each row snaps to a `--glass-2` fill, with nothing
moving between rows. Five floating lists, five slightly different copies of
the same rule:

- `.menu-item:hover` in `dryl.css`, rows full-bleed, panel padding `--sp-1 0`.
- `.select-option` in `DrylSelect.razor.css`, duplicated in
  `DrylMultiSelect.razor.css` because scoped CSS does not reach across
  components.
- `.autocomplete-option` in `DrylAutocomplete.razor.css`.
- `.time-cell` in `DrylTimePicker.razor.css`, which has no keyboard highlight
  per column at all (cells are buttons reached by Tab).

Target role: the end user moving through a menu or list by pointer or
keyboard; the Blazor developer, who gets the motion with no new parameter.

## Solution Idea

One highlight per list that glides to the row under the pointer or the
keyboard cursor, shared by all five surfaces. Three variants in the prototype
artifact `https://claude.ai/artifact/E9JxxxNr8rggPP1coeBTjc` (with today's look
as a fourth tab for comparison):

- **A — Ink.** The drawer's `.nav-ink` carried over: accent gradient, 1 px
  `--accent-line` edge, small glowing bar; spring travel (`--dur-slow` /
  `--ease-spring`), the radial hover light from `.nav-item::after`. Menu and
  navigation speak one language. *Tech Lead recommendation.*
- **B — Liquid.** A glass drop (`--glass-3`, `--sheen-grad`,
  `--edge-hi-strong`) whose leading edge runs ahead on `--dur-fast` while the
  trailing edge follows on `--dur-slow` / `--ease-in-out`; a `--shimmer`
  highlight follows the pointer inside the drop; accent only as a 1 px
  hairline.
- **C — Rail.** No fill: a 3 px accent rail that stretches between rows, the
  label shifts by `--sp-1`, and an accent wash lights the current row under the
  pointer.

Shared by all three: one highlight per list; pointer and keyboard drive the same
highlight (the `aria-activedescendant` / `_highlightedIndex` state stays the
source of truth); on open the highlight is placed without flight, then glides
(navGlide's place → glide); danger rows recolour it; headers, separators and
disabled rows are skipped; the light is mouse-only; reduced motion places
without gliding; without script the static look stays.

## Scope
- **In scope:** the highlight and hover light in the five lists; panel inset
  (`--sp-1` all round, so the highlight sits inside the radius); one shared
  option base style; keyboard highlight per column in `DrylTimePicker`.
- **Out of scope:** panel open/close motion (the popover owns it, I30);
  sub-menus; type-ahead; AI styling; `DrylCommandPalette` (its own list,
  candidate for a follow-up).

## Impact
- **Harness:** every variant uses existing tokens and the three durations /
  easings; no new token, no new easing, no new dependency. B and C use a
  direction-dependent transition (two durations on `top`/`bottom`), a new
  pattern but no new value.
- **Specs:** none of the six components has a spec yet; the rework writes
  them (raises the coverage meter) together with `_Api.md` / `_Interop.md`
  entries for the shared engine.
- **Public API:** none planned — the glide is the default look. Open: whether a
  consumer may opt out.
- **Code:** a generalised `navGlide` engine in `wwwroot/js/dryl.js` bound to a
  list host; rules in `wwwroot/dryl.css`; option markup in the four input
  components gains a host wrapper; `DrylTimePicker` gains a highlighted index
  per column. Risk: Blazor owns `class`, so state lives in `data-*` and inline
  style as in navGlide; the Autocomplete re-renders its rows on every keystroke.
- **Website:** demos for the five components; `ComponentCatalog` entries.

## Decisions

## Open Points
1. Which variant (A, B or C)? — Product Owner.
2. Opt-out parameter, or the glide everywhere?
3. `DrylCommandPalette` in this release or a follow-up idea?
