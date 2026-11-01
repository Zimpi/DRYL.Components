# DrylSpinner

## Meta
- **State:** Implemented
- **Source:** code/DRYL.Components/Components/Feedback/DrylSpinner.razor
              code/DRYL.Components/Components/Feedback/DrylSpinner.razor.css

## User Story

As a Blazor developer, I want a compact loading indicator that fits inline in a
button, a row or a panel, and that changes its rhythm when the work behind it is
an AI thinking rather than a request loading, so that a user can feel what kind
of wait they are in without reading a label.

## Description

`DrylSpinner` is the smallest of the category's indicators: an inline-flex box
sized by one token, holding one of three organic animations (I35). `Ring` is
**Liquid**, a torus that turns at constant speed while its outline wobbles and its
wall breathes. `Dots` is **Goo**, three droplets that split from a core and fuse
back into it. `Pulse` is **Bloom**, three translucent membranes that morph at
unrelated periods around a breathing nucleus. The enum keeps its names, so the
redesign changes no public API.

Its distinguishing behaviour is that **the AI state changes the spinner's own
rhythm, not only its aura.** Every other component in the category treats `Ai`
as a decoration applied around unchanged content. Here the loop itself
retimes — slower while a model is thinking, faster while it is streaming, and
stopped once it is done — so the same spinner communicates a different kind of
waiting in the same space.

The size system is one custom property per size on the wrapper. Every child
dimension derives from it, which is why a variant can change shape without any
size value being written twice. The rate system works the same way: every loop
duration is a base value multiplied by one tempo property on the wrapper, which
the AI state sets once.

## Public API

| Member | Type | Default | Purpose |
|---|---|---|---|
| `Variant` | `DrylSpinner.SpinnerVariant` | `SpinnerVariant.Ring` | Visual style. |
| `Size` | `DrylSpinner.SpinnerSize` | `SpinnerSize.Medium` | Physical size. |
| `Label` | `string?` | `null` | Accessible label. `null` uses a state-aware default. |
| `Ai` | `AiState` | `AiState.None` | Ambient AI state; also retimes the spinner's own loop. `AiState.None` inherits a surrounding `DrylAiScope`. |
| `Aura` | `AiAura?` | `null` | Pins the aura variant; `null` inherits a surrounding `DrylAiScope`. |
| `Class` | `string?` | `null` | Extra CSS class(es) merged onto the spinner's own classes. |
| `AdditionalAttributes` | `IDictionary<string, object>?` | `null` | Pass-through attributes on the root element. |

## Acceptance Criteria

### Structure

- The component renders a single root element carrying the wrapper classes.
- The root carries the modifier class of its `Size`, one per value.
- The root carries the modifier class of its `Variant`, one per value.
- `SpinnerVariant.Ring` supplies the class for any value the switch does not
  match, so an unmapped value still renders a spinner.
- `SpinnerVariant.Ring` renders one torus element inside a turning layer.
- `SpinnerVariant.Dots` renders an inline SVG with one core and exactly three
  droplet circles, grouped under a goo filter.
- The goo filter is defined inside the spinner with an id unique to the
  instance, so two spinners on one page never share or clash over a filter.
- `SpinnerVariant.Pulse` renders three membrane elements and one core element.
- The root is inline-flex, so a spinner sits on the text baseline row of a
  button or a label without breaking the line.
- The root does not shrink when placed in a flex row that runs out of space.
- `Class` is merged onto the root's own classes rather than replacing them.
- `AdditionalAttributes` are applied to the root.

### Size

- Each `SpinnerSize` value sets its own size custom property on the root.
- Every child dimension derives from that property, so no variant carries a
  second copy of a size value.
- The ring's wall thickness is a share of its own size, so a small ring stays
  proportionate rather than looking heavy.
- Every variant renders inside a square box with a circular outline, so the
  aura ring wraps each of them the same way.

### Keyboard and accessibility

- The root carries `role="status"`.
- The root carries `aria-live="polite"`, so a spinner appearing mid-page does
  not interrupt what the screen reader is saying.
- The root carries an accessible label.
- `Label` set wins over the state-aware default.
- `Label` left `null` yields a label describing the current `Ai` state, so a
  screen-reader user learns that a model is thinking rather than only that
  something is loading.
- `Label` left `null` with `Ai` at `AiState.None` yields the plain loading
  label.
- The spinner is not focusable and adds no stop to the tab order, because it is
  an indicator and not a control.
- The spinner's moving parts are hidden from assistive technology: the state is
  carried by the label, not by the animation.

### Appearance

- Every color the component renders comes from a token; the component names no
  literal color (`DESIGN-01`).
- The spinner paints no surface of its own — no fill, no border, no frost — so
  it inherits whatever ground it is placed on and `DESIGN-06` has nothing to
  apply to.
- `SpinnerVariant.Ring` draws its torus from `--accent-a` and `--accent-b`,
  fading to a translucent tail, with an off-centre hole masked out; a small
  specular head in `--ai-core` rides the bright end.
- `SpinnerVariant.Dots` fills its core from `--accent-a` and its droplets from
  `--accent-a`, `--accent-b` and a mix of the two; a spark in `--ai-core` sits
  on the core, outside the goo filter so it stays crisp.
- `SpinnerVariant.Pulse` draws its membranes from `--accent-a`, `--accent-b`
  and a mix of the two, its nucleus from `--ai-core` and its halo from a
  translucent `--accent-a`.
- The accent appears as a small indicator at most `--sp-6` across — never as
  the fill of a large surface (`DESIGN-08`).
- The component branches on no color mode and holds no mode-assuming value, so
  the same markup serves light and dark (`DESIGN-02`).

### Motion

- Every variant animates continuously while mounted.
- `SpinnerVariant.Ring` turns on a layer of its own, separate from the morph,
  so the rotation never brakes while the outline and the wall keep changing.
- The ring's outline morph and its wall breathing swing back and forth at
  periods unrelated to the rotation, so the motion does not visibly repeat.
- `SpinnerVariant.Dots` staggers its three droplets by a third of the loop each,
  so one is always leaving while another is merging back.
- `SpinnerVariant.Pulse` gives its three membranes unrelated periods, so the
  silhouette never repeats exactly.
- Shape morphs (`border-radius`) are eased with `--ease-in-out` or
  `--ease-viscous` and swing between two shapes rather than stepping through
  several, so no morph stalls at a keyframe.
- The looping durations are chosen for the rhythm each variant needs and are
  deliberately not `--dur-*`: that scale governs transitions and one-shots,
  while continuous motion is free of it (`DESIGN-10`).
- The rotating variant uses `linear` timing, which `DESIGN-10` requires for
  anything that rotates — an eased rotation stutters once per revolution.
- Wherever an easing is applied at all, it is an easing token rather than a bare
  keyword (`DESIGN-10`).
- Under `prefers-reduced-motion: reduce` every loop stops.
- Under `prefers-reduced-motion: reduce` each variant is left in a legible
  resting state rather than blank, so the indicator still reads as present:
  the ring keeps its torus, the droplets rest as a three-lobed drop, the
  membranes rest as a layered blob.

### AI mode

- `Ai` defaults to `AiState.None`, so AI styling is opt-in.
- The aura variant follows `Aura` when set and a surrounding `DrylAiScope`
  otherwise.
- A component that leaves `Ai` at `AiState.None` shows the surrounding
  `DrylAiScope`'s state, including the one-shot `AiState.Generated` reveal on an
  inherited move into it; an explicit `Ai` wins over the scope.
- The spinner renders the shared aura vocabulary — ring, comet, glow, wash —
  rather than a spinner-specific AI treatment (`AI-02`).
- `AiState.Thinking` slows the spinner's own loop relative to its default.
- `AiState.Streaming` speeds the spinner's own loop relative to its default.
- `AiState.Active` sets a rate between the two, so an idle-but-engaged model
  reads as different from both.
- `AiState.Generated` stops the loop and leaves the spinner in a settled state,
  so a finished operation stops asking for attention.
- Each of the three variants implements all four of those rate changes, so the
  signal does not depend on which variant a consumer picked.
- The rate change is carried by the animation's duration alone, so the spinner's
  geometry does not shift when the state changes.
- The rate change is one tempo multiplier per state, set once on the root, so
  the ratio between states is the same for every variant.
- Leaving AI mode keeps the aura mounted for one `--dur-slow` beat, so it
  dissolves rather than snapping away.
- Entering `AiState.Generated` replays the one-shot completion wash, every time
  it is entered.
- `AiState.Active` is signalled by an extra class the shared aura helper
  deliberately omits, because the generic aura treats `Active` as ring-only and
  the spinner needs the rate cue as well.
- `AiState.Proposed` renders exactly like `AiState.None`: a spinner
  reports work in progress, and nothing on it waits for a human decision.

## Recorded gaps

- **Base loop durations are still hand-picked.** Since I35 the AI states share
  one tempo multiplier, so a fifth `AiState` adds one value, not three. The base
  periods of each variant (turn, morph, breathe, orbit, membranes) remain
  chosen by eye, which `DESIGN-10` allows for continuous motion.
- **Liquid's breathing wall needs `@property`.** A browser without it keeps the
  hole still; the ring still turns and morphs.
- **`AiState.Active` needs an extra class the shared helper does not emit.** The
  component appends `ai-active` itself, so the AI-state contract of this
  component is one class wider than the shared vocabulary. Anything reusing the
  helper alone would miss the `Active` rate cue.
- **Only structure is tested.** `DrylSpinnerTests` covers the elements per
  variant, the unique filter id, the hidden moving parts and the size classes;
  motion, rate and reduced-motion criteria remain a reviewer check.

## Cross-cutting evidence (`SPEC-05`)

- **Both color modes** — token-only colors, verified by
  `node scripts/check-light-sync.mjs` and
  `node scripts/validate-light-contrast.mjs`. `--accent-a`, `--accent-b` and
  `--ai-core` are the mode-dependent tokens (`--ai-core` is near-white on dark,
  a deep accent on light, so the nucleus reads on both); the component defines
  no mode-specific rule.
- **Enter/exit animation** — none of its own, and that is the written exception
  `DESIGN-11` allows: a spinner *is* an animation, mounted and unmounted by its
  host, which wraps it in `DrylPresence` when its appearance should be animated
  too.
- **Keyboard and a11y** — the "Keyboard and accessibility" criteria above. The
  substantive decision is the state-aware default label: the AI state is
  announced in words, not only in motion.
- **AI mode** — yes, and further than anywhere else in the category: the
  component's own loop retimes per state, which is the cue a user reads before
  they read the aura.
- **Demo page** — `DRYL.Website/Components/Pages/DemoSpinner.razor`, with the
  examples `Components/Examples/Spinner/Variants.razor`, `.../Sizes.razor`,
  `.../AiMode.razor`, `.../InContext.razor` and `.../Live.razor`.
- **`ComponentCatalog`** — registered as `"Spinner"` / `spinners` in
  `DRYL.Website/Components/ComponentCatalog.cs`, flagged AI-capable.
