# DrylReveal

## Meta
- **State:** Implemented
- **Source:** code/DRYL.Components/Components/Layout/DrylReveal.razor
              code/DRYL.Components/RevealTransition.cs

## User Story

As a Blazor developer building a content-heavy or marketing page, I want to wrap
a block of content so that it eases in the first time it scrolls into view —
optionally child by child — so that the page feels alive without my writing an
IntersectionObserver.

## Description

`DrylReveal` is the library's scroll-entrance motion primitive (`DESIGN-11`,
`DESIGN-13`). It renders one `div.reveal` around its content; its **direct
children** are the animated units. They start hidden, and after the first
interactive render the component hands the wrapper to
`dryl.motion.observe`, which watches it with IntersectionObserver and shows the
content once it is in view.

"In view" means either that `Threshold` of the element is visible, or — for an
element too tall ever to show that much of itself — that it fills `Threshold`
of the viewport. Without `Stagger` the wrapper is the watched unit and all
children arrive together. With `Stagger` every direct child is watched on its
own, so a long list lands row by row as it scrolls; children that arrive in the
same frame are delayed one after another in document order, and children added
later are picked up too. `Once` (the default) reveals a unit a single time;
`Once = false` hides it again whenever it leaves the view.

`Transition` selects one of three entrances from `RevealTransition`.

## Public API

Namespace: `DRYL.Components`. Implements `IAsyncDisposable`.

| Member | Type | Default | Purpose |
|---|---|---|---|
| `ChildContent` | `RenderFragment?` | `null` | Content to reveal; direct children are the animated units. |
| `Transition` | `RevealTransition` | `RevealTransition.Rise` | Entrance the children play. |
| `Stagger` | `bool` | `false` | Watches and reveals each direct child on its own, one after another. |
| `Once` | `bool` | `true` | Reveal once; `false` re-hides content scrolled out of view. |
| `Threshold` | `double` | `0.15` | Fraction of the element that must be visible — or of the viewport it must fill — to count as in view. |
| `Class` | `string?` | `null` | Classes merged onto the wrapper. |
| `AdditionalAttributes` | `IDictionary<string, object>?` | `null` | Captured unmatched attributes applied to the wrapper. |

`RevealTransition` members: `Fade`, `Rise`, `ScaleIn` (owned by this spec,
`code/DRYL.Components/RevealTransition.cs`).

## Acceptance Criteria

### Structure

- The component renders one `div` carrying `.reveal`.
- `ChildContent` renders directly inside the wrapper.
- `Class` is appended to the wrapper's class list.
- Unmatched attributes are applied to the wrapper.

### Transition

- `Transition` defaults to `RevealTransition.Rise`.
- `RevealTransition.Rise` adds `.reveal--rise` to the wrapper.
- `RevealTransition.Fade` adds `.reveal--fade` to the wrapper.
- `RevealTransition.ScaleIn` adds `.reveal--scale-in` to the wrapper.
- Under `.reveal--fade`, a hidden child differs from its shown state in opacity
  only.
- Under `.reveal--rise`, a hidden child also sits lower than its resting place.
- Under `.reveal--scale-in`, a hidden child is also slightly scaled down.

### Observation

- After the first interactive render the component calls
  `dryl.motion.observe` with the wrapper and the options `stagger`, `once` and
  `threshold`.
- The component calls `dryl.motion.observe` once per instance, never on later
  renders.
- `Threshold` defaults to `0.15`.
- A `Threshold` outside 0–1 is clamped into that range.
- An element counts as in view once `Threshold` of it is visible.
- An element taller than the viewport counts as in view once it fills
  `Threshold` of the viewport.

### Reveal without stagger

- Before it is in view, every direct child of the wrapper is fully transparent.
- Once the wrapper is in view, the wrapper gains `.is-revealed`.
- Under `.is-revealed`, every direct child is fully opaque and untransformed.
- With `Once`, the wrapper is no longer watched after its first reveal.
- With `Once = false`, the wrapper loses `.is-revealed` when it leaves the view.

### Reveal with stagger

- `Stagger` defaults to `false`.
- With `Stagger`, each direct child is watched on its own.
- With `Stagger`, a child that comes into view gains the `data-revealed`
  attribute and is shown.
- Children that come into view in the same frame are revealed in document
  order.
- Each child of such a batch carries `--reveal-i`, its place in the batch.
- A child's transition is delayed by `--reveal-i` times `--reveal-step`.
- `--reveal-i` is capped, so a large batch still settles quickly.
- A direct child added after the first render is watched and revealed in the
  same way.
- A direct child removed from the wrapper stops being watched.
- With `Once = false`, a child loses `data-revealed` when it leaves the view.

### Motion

- A revealing child transitions `opacity` and `transform` on `--dur-slow` /
  `--ease-out`.
- Only `opacity` and `transform` are animated.

### Reduced motion and fallback

- Under `prefers-reduced-motion: reduce`, the wrapper gains `.is-revealed`
  immediately, without observing.
- Under `prefers-reduced-motion: reduce`, children have no transition, no delay
  and no transform.
- Without IntersectionObserver support, the wrapper gains `.is-revealed`
  immediately.

### Disposal

- On disposal after a successful `observe`, the component calls
  `dryl.motion.unobserve` with the wrapper.
- `dryl.motion.unobserve` disconnects every observer for the wrapper and
  cancels a pending batch.
- A component that never attached (prerender) makes no interop call on
  disposal.
- A disconnected circuit during attach or disposal raises no error.

### Keyboard and accessibility

- Hidden children stay in the accessibility tree and in the tab order; the
  reveal changes only opacity and transform (`UX-07`).

## Recorded debt

- **Hidden without JavaScript.** Children are transparent from the first paint
  and are only shown by `dryl.motion.observe`; content rendered statically
  (static server rendering without interactivity, or with script blocked) stays
  invisible.
- **The non-stagger state lives in `class`.** `.is-revealed` is added by script
  to the `class` attribute Blazor owns; a re-render that changes the class
  string (a new `Transition` or `Class`) drops it, and with `Once` the wrapper
  is no longer watched, so the content stays hidden. The stagger path already
  uses the `data-revealed` attribute instead.
- **Options are read once.** `Stagger`, `Once` and `Threshold` are passed on the
  first render only; later changes have no effect.
- **Literal geometry (`DESIGN-01`).** The rise offset and the scale-in factor
  are literals; the stagger cap is a script constant.

## Cross-cutting evidence (`SPEC-05`)

- **Both color modes** — mode tokens only; inspected on `/components/reveal` in dark and light (2026-11-20)
- **Enter/exit animation** — the component *is* an entrance primitive: its
  children enter on scroll as above. It has no exit of its own; with
  `Once = false` the reverse transition plays when content leaves the view.
- **Keyboard and a11y** — the wrapper adds no role and no focus stop; content
  keeps its semantics and order while hidden; reduced motion shows everything
  at once.
- **AI mode** — **no** (`AI-05`): a motion wrapper carries no content of its
  own; the AI state belongs to what it wraps.
- **Demo page** — `/components/reveal`
  (`DRYL.Website/Components/Pages/DemoReveal.razor`), examples
  `Reveal/Default`, `Reveal/Stagger` and `Reveal/Transitions`; also used on the
  website's `Landing.razor`.
- **`ComponentCatalog`** — registered as `"Reveal"` / `reveal`, flagged not
  AI-capable (`REL-04`).
- **Tests** — `tests/DRYL.Components.Tests/DrylRevealTests.cs` (default
  transition, transition-to-class mapping, `observe` invoked on render).
