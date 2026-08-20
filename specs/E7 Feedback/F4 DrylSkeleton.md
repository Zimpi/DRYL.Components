# DrylSkeleton

## Meta
- **State:** Implemented
- **Source:** code/DRYL.Components/Components/Feedback/DrylSkeleton.razor
              code/DRYL.Components/Components/Feedback/DrylSkeleton.razor.css

## User Story

As a Blazor developer, I want a placeholder that has the shape of the content it
is standing in for, and that shows me the difference between "still fetching"
and "an AI is writing into this space right now", so that a user waiting on a
model sees the answer taking shape rather than a generic loading block.

## Description

`DrylSkeleton` renders shimmer blocks in the rough shape of the content that is
coming: one bar, a paragraph of bars, a circle, an image rectangle, or a
composite card that combines all of them. The shimmer itself is the library's
`skel` primitive; this component adds the size system, the shapes, and the AI
behaviour.

The AI behaviour is what makes it AI-native rather than AI-decorated. The three
states each change the shimmer, not just the frame around it: `Thinking`
accelerates it, `Streaming` recolors it from a neutral sweep to a violet-cyan
one — the placeholder itself signals that model output is flowing into it — and
`Generated` fades the blocks out so real content can take their place.

`SkeletonVariant.Custom` makes the component's block classes part of its public
contract: a consumer builds their own layout out of `skel`, `skel-circle` and
`skel-rect`, and inherits the shimmer, the sizes and every AI mutation above.

## Public API

| Member | Type | Default | Purpose |
|---|---|---|---|
| `Variant` | `DrylSkeleton.SkeletonVariant` | `SkeletonVariant.Line` | Shape of the placeholder. |
| `Size` | `DrylSkeleton.SkeletonSize` | `SkeletonSize.Medium` | Size of the blocks. |
| `Lines` | `int` | `3` | Bars rendered by `Text`, and in the body block of `Card`. |
| `Width` | `string?` | `null` | CSS width for `Line`. `null` fills the container. |
| `Label` | `string?` | `null` | Accessible label. `null` uses a state-aware default. |
| `ChildContent` | `RenderFragment?` | `null` | Layout for `SkeletonVariant.Custom`. |
| `Ai` | `AiState` | `AiState.None` | Ambient AI state; also mutates the shimmer. |
| `Aura` | `AiAura?` | `null` | Pins the aura variant; `null` inherits a surrounding `DrylAiScope`. |
| `Class` | `string?` | `null` | Extra CSS class(es) merged onto the skeleton's own classes. |
| `AdditionalAttributes` | `IDictionary<string, object>?` | `null` | Pass-through attributes on the root element. |

`Width` is a raw CSS value rather than a typed size, deliberately: a placeholder
is matched to the content it replaces, and that content's width is the
consumer's, not the library's.

## Acceptance Criteria

### Structure

- The component renders a single root element carrying the wrapper classes.
- The root carries the modifier class of its `Size`, one per value.
- The root carries the modifier class of its `Variant`, one per value.
- `SkeletonVariant.Line` supplies the class for any value the switch does not
  match.
- `SkeletonVariant.Line` renders one bar.
- `SkeletonVariant.Text` renders exactly `Lines` bars.
- `SkeletonVariant.Avatar` renders one circle.
- `SkeletonVariant.Image` renders one rectangle.
- `SkeletonVariant.Card` renders a header row, a rectangle and a body block, in
  that order.
- `SkeletonVariant.Card` renders an avatar and two bars in its header row.
- `SkeletonVariant.Card` caps its body block at five bars however large `Lines`
  is, so a card placeholder cannot grow unbounded.
- `SkeletonVariant.Custom` renders `ChildContent` and no built-in shape.
- `Lines` set to zero or below renders no bars and throws nothing.
- `Class` is merged onto the root's own classes rather than replacing them.
- `AdditionalAttributes` are applied to the root.

### Shape and size

- Each `SkeletonSize` value sets its own bar height, circle diameter, rectangle
  height and header-avatar diameter on the root.
- Every block derives its dimensions from those properties, so a size change
  never means editing a second value.
- `Width` set applies to the bar of `SkeletonVariant.Line`.
- `Width` left `null` makes that bar fill its container.
- The bars of `SkeletonVariant.Text` vary in width, so a paragraph placeholder
  does not read as a stack of identical rectangles.
- The last bar of a multi-bar `Text` block is visibly shorter than the ones
  above it, the way a real last line of a paragraph is.
- A single-bar `Text` block is not shortened, because there is no paragraph for
  it to end.
- `SkeletonVariant.Avatar` sizes its root to the circle, so it can sit in a flex
  row beside text without stretching.
- `SkeletonVariant.Card` and `SkeletonVariant.Image` round their root, so the
  AI ring traces the shape of the content rather than a rectangle around it.

### Keyboard and accessibility

- The root carries `role="status"`.
- The root carries `aria-live="polite"`, so a placeholder appearing mid-page
  does not interrupt what the screen reader is saying.
- The root carries an accessible label.
- `Label` set wins over the state-aware default.
- `Label` left `null` yields a label describing the current `Ai` state, so a
  screen-reader user learns that a model is writing rather than only that
  something is loading.
- `Label` left `null` with `Ai` at `AiState.None` yields the plain loading
  label.
- The skeleton is not focusable and adds no stop to the tab order.
- The shimmer blocks carry no text, so nothing is announced twice.

### Appearance

- Every color the component renders comes from a token; the component names no
  literal color (`DESIGN-01`).
- The blocks paint `--glass-1` and sweep through `--glass-3`, so a placeholder
  reads as an absence of content rather than as content.
- The component paints no surface of its own around the blocks while `Ai` is
  `AiState.None` — no fill, no border, no frost — so it inherits whatever ground
  it is placed on and `DESIGN-06` has nothing to apply to.
- The rectangle and the card use `--r-md` and `--r-lg`; the circles are fully
  round.
- The component branches on no color mode and holds no mode-assuming value, so
  the same markup serves light and dark (`DESIGN-02`).

### Motion

- Every block shimmers continuously while mounted.
- The shimmer is carried by a sliding strip inside each block rather than by an
  animated background position, so it stays on the compositor.
- Consecutive bars in a text block and in a card header shimmer at staggered
  offsets, so the group reads as a wave rather than as one flash.
- The looping duration is chosen for the rhythm the shimmer needs and is
  deliberately not `--dur-*`: that scale governs transitions and one-shots,
  while continuous motion is free of it (`DESIGN-10`).
- Wherever an easing is applied, it is an easing token rather than a bare
  keyword.
- Under `prefers-reduced-motion: reduce` the staggered offsets are dropped, so
  every bar shimmers in phase and nothing chases anything.

### AI mode

- `Ai` defaults to `AiState.None`, so AI styling is opt-in.
- The aura variant follows `Aura` when set and a surrounding `DrylAiScope`
  otherwise.
- The skeleton renders the shared aura vocabulary — ring, comet, glow, wash —
  rather than a skeleton-specific AI treatment (`AI-02`).
- The root gains a glass frame while an aura is present, so the rotating ring
  has a shape to trace instead of wrapping loose blocks.
- `AiState.Thinking` accelerates the shimmer relative to its default.
- `AiState.Streaming` recolors the shimmer from the neutral sweep to a gradient
  built from `--ai-a` and `--ai-b`, so the placeholder itself says that model
  output is arriving.
- `AiState.Streaming` also tints the blocks' resting fill, so the part of a
  block the sweep has not reached is not left neutral.
- `AiState.Generated` fades the blocks out over `--dur-slow`, so real content
  replaces a dissolving placeholder rather than a disappearing one.
- The faded-out blocks stay in the layout, so the swap to real content does not
  jump.
- Every shimmer mutation targets the sliding strip rather than the block itself,
  so an AI state change never repaints a large surface.
- Under `prefers-reduced-motion: reduce` the accelerated and the recolored
  shimmer both fall back to the base rate, so an AI state no longer speeds
  anything up.
- Under `prefers-reduced-motion: reduce` `AiState.Generated` settles the blocks
  at a dimmed opacity instead of animating them out.
- Leaving AI mode keeps the aura mounted for one `--dur-slow` beat, so it
  dissolves rather than snapping away.
- Entering `AiState.Generated` replays the one-shot completion wash, every time
  it is entered.
- `AiState.Active` is signalled by an extra class the shared aura helper
  deliberately omits, so the skeleton can respond to the idle-but-engaged state
  as well.

## Recorded gaps

- **The size values are literals** in
  `code/DRYL.Components/Components/Feedback/DrylSkeleton.razor.css` — four
  dimensions times three sizes. `DESIGN-01` covers colors, radii, shadows,
  durations and easings, which are tokens here; a placeholder's block heights
  are not covered by a token today. Recorded as debt, not as compliance.
- **The bar widths are a hardcoded eight-element cycle** in `DrylSkeleton.razor`.
  A `Text` block of nine bars repeats the first width at position nine, and the
  sequence is identical for every skeleton on the page — two paragraphs side by
  side shimmer in exactly the same shape.
- **`Lines` is unbounded for `SkeletonVariant.Text`.** Only the card body caps
  it at five; a `Text` skeleton renders as many bars as it is given.
- **The shimmer stagger covers six bars.** A text block's seventh bar and beyond
  shimmer in phase with the first, because the offsets are written per
  `nth-child` rather than derived from an index.
- **`SkeletonVariant.Custom` freezes three CSS class names** — `skel`,
  `skel-circle` and `skel-rect` — into the public contract. They are as bound by
  the 1.0 freeze as the parameters, and nothing in the build enforces that.
- **The base shimmer keeps running under `prefers-reduced-motion: reduce`.** The
  reduced-motion block calms the AI mutations and drops the stagger, but the
  `skel` primitive's own sweep is untouched, so a user who asked for less motion
  still gets a continuously moving placeholder. The primitive is shared and its
  rule lives in `dryl.css`, so the fix is not this component's alone — recorded
  here because this is where the shimmer is most of the screen (`UX-06`).
- **No tests of its own.** None of the criteria above is guarded by a test.

## Cross-cutting evidence (`SPEC-05`)

- **Both color modes** — token-only colors, verified by
  `node scripts/check-light-sync.mjs` and
  `node scripts/validate-light-contrast.mjs`. `--glass-1`, `--glass-3`,
  `--ai-a` and `--ai-b` are the mode-dependent tokens; the component defines no
  mode-specific rule.
- **Enter/exit animation** — the exit is the component's own and is the point of
  it: `AiState.Generated` fades the blocks out over `--dur-slow` instead of the
  host yanking them. There is no enter animation, the written exception
  `DESIGN-11` allows for a placeholder whose whole body is already a continuous
  animation; a host that wants it to appear gradually wraps it in
  `DrylPresence`.
- **Keyboard and a11y** — the "Keyboard and accessibility" criteria above. The
  substantive decision is the state-aware default label: "AI streaming content"
  is announced in words, not only in the shimmer's color.
- **AI mode** — yes, and it drives the primitive rather than decorating it. The
  violet-cyan streaming shimmer is the component's reason to exist as a DRYL
  component rather than a generic placeholder.
- **Demo page** — `DRYL.Website/Components/Pages/DemoSkeleton.razor`, with the
  examples `Components/Examples/Skeleton/Variants.razor`, `.../Sizes.razor`,
  `.../CardAvatar.razor`, `.../Custom.razor`, `.../AiMode.razor` and
  `.../Lifecycle.razor`.
- **`ComponentCatalog`** — registered as `"Skeleton"` / `skeleton` in
  `DRYL.Website/Components/ComponentCatalog.cs`, flagged AI-capable.
