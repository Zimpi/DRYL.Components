# DrylImage

## Meta
- **State:** Implemented
- **Source:** code/DRYL.Components/Components/Data/DrylImage.razor
              code/DRYL.Components/Components/Data/DrylImage.razor.css

## User Story

As a Blazor developer, I want an image that reserves its space before it
arrives, shows something sensible while it loads and something sensible when it
fails, so that a page does not jump, flash or leave a hole because of a picture.

## Description

`DrylImage` is a frame around an `img` that removes the boilerplate a careful
`img` needs. It lazy-loads and decodes asynchronously by default. It takes an
aspect ratio — either named through `Ratio` or derived from `Width` and `Height`
— and holds that box from the first paint, which is what keeps a loading image
from shifting the layout under it. It fades in when it loads, shows a shimmer
placeholder while it does not, and falls back twice on failure: first to
`FallbackSrc` if one was given, then to a stylised tile carrying an icon and the
alt text.

It is also the category's most AI-native surface, and the only one where the
**AI state changes the image itself** rather than only the frame around it.
`AiState.Thinking` drifts a coloured cloud over the picture. `AiState.Streaming`
sharpens it out of blur — driven by `Progress` when the consumer knows how far
along the generation is, and on a timer when they do not. `AiState.Generated`
reveals it with a one-shot scale-in. All three are built from tokens and the
shared aura primitives; no colour, state or animation was invented for them
(`AI-04`).

The frame has two layout modes and picks between them itself. With a ratio, the
frame owns the box and the image fills it absolutely. Without one, the frame
wraps the image's natural height, and the placeholder defines the box until the
image has one.

## Public API

| Member | Type | Default | Purpose |
|---|---|---|---|
| `Src` | `string` | `""` (`EditorRequired`) | Image URL. |
| `Alt` | `string` | `""` (`EditorRequired`) | Alternative text; reused by the error fallback. |
| `Width` | `int?` | `null` | Intrinsic width in px. Caps the rendered width and, with `Height`, sets the ratio. |
| `Height` | `int?` | `null` | Intrinsic height in px. With `Width`, sets the ratio. |
| `Fit` | `DrylImage.ImageFit` | `ImageFit.Cover` | How the image fills its box. |
| `Position` | `DrylImage.ImagePosition` | `ImagePosition.Center` | Focal point when cropped. |
| `Rounded` | `DrylImage.ImageRounded` | `ImageRounded.None` | Corner rounding, from the radius scale. |
| `Ratio` | `DrylImage.ImageRatio` | `ImageRatio.Auto` | Forced aspect ratio; overrides the `Width`/`Height` ratio. |
| `Lazy` | `bool` | `true` | Lazy-loads the image. |
| `FallbackSrc` | `string?` | `null` | Second URL tried before the icon fallback. |
| `FallbackIcon` | `string?` | `null` | Icon of the error fallback. `null` uses a default. |
| `ShowSkeleton` | `bool` | `true` | Shows the shimmer placeholder while loading. |
| `Border` | `bool` | `false` | Outlines the frame. |
| `Shadow` | `bool` | `false` | Lifts the frame. |
| `Ai` | `AiState` | `AiState.None` | Ambient AI state; also drives effects on the image itself. |
| `Aura` | `AiAura?` | `null` | Pins the aura variant; `null` inherits a surrounding `DrylAiScope`. |
| `Progress` | `int?` | `null` | 0–100 sharpen progress for `AiState.Streaming`. `null` runs it on a timer. |
| `BlurDuration` | `int` | `2000` | Duration in ms of the timed sharpen. |
| `Class` | `string?` | `null` | Extra CSS class(es) merged onto the frame's own classes. |
| `AdditionalAttributes` | `IDictionary<string, object>?` | `null` | Pass-through attributes on the frame. |

The four enums are nested in `DrylImage` and are therefore written qualified —
`DrylImage.ImageRatio.Wide`. Their members are listed in [`_Api.md`](_Api.md).

## Acceptance Criteria

### Structure

- The component renders a frame element holding the image and, depending on the
  load phase, a placeholder or a fallback.
- The frame is a block that fills the width available to it.
- `Class` is merged onto the frame's own classes rather than replacing them.
- `AdditionalAttributes` are applied to the frame.
- The frame carries the modifier class of its layout mode, one of the two.
- The frame carries the modifier class of its `Rounded` value, one per value.
- Every child of the frame inherits the frame's corner, so the image, the
  placeholder and the fallback are rounded alike without repeating the value.
- `Border` set outlines the frame with `--line`.
- `Shadow` set lifts the frame with `--shadow-sm`.
- The frame does not clip its children, so the aura's glow can breathe outside
  the box.

### The box

- `Ratio` at any value other than `ImageRatio.Auto` sets that ratio on the
  frame.
- `Ratio` at `ImageRatio.Auto` with both `Width` and `Height` positive sets
  their ratio on the frame.
- `Ratio` at `ImageRatio.Auto` with either dimension missing sets no ratio, and
  the frame wraps the image's natural height instead.
- `Ratio` set wins over `Width` and `Height`.
- The ratio is written with invariant formatting, so a German locale does not
  emit a decimal comma into the style attribute.
- `Width` set positive caps the frame's rendered width.
- In ratio mode the image fills the frame absolutely, so the box is held from
  the first paint and the layout never shifts.
- In natural mode a not-yet-loaded image is taken out of the flow, so the
  placeholder alone defines the height until the image has one.
- `Fit` maps to the image's object fit, one value per member.
- `Position` maps to the image's object position, one value per member.
- Any unmapped `Fit` value renders as `ImageFit.Cover` and any unmapped
  `Position` value as `ImagePosition.Center`, so an unknown value still renders
  an image.

### Loading and failing

- The image is lazy-loaded when `Lazy` is `true` and eagerly loaded when it is
  `false`.
- The image is decoded asynchronously, so decoding a large picture does not
  block the frame.
- The component starts in its loading phase whenever `Src` changes.
- An empty `Src` goes straight to the failed phase rather than requesting
  nothing.
- Changing `Src` re-arms the fallback chain, so an image that failed once can
  succeed at a new URL.
- A load error with an unused `FallbackSrc` retries at that URL and returns to
  the loading phase.
- A load error with no `FallbackSrc`, or with one already tried, enters the
  failed phase.
- The failed phase renders no `img` element at all, so a broken URL is not
  requested again on every render.
- `ShowSkeleton` left `true` renders a shimmer placeholder while loading.
- `ShowSkeleton` set to `false` renders no placeholder.
- The placeholder is removed as soon as the image loads or fails.
- The fallback renders an icon and, when `Alt` is not empty, the alt text
  beneath it.
- The fallback's icon is `FallbackIcon` when given and a default one otherwise.
- The fallback is outlined with a dashed border, so a failure reads as a missing
  thing rather than as a deliberate empty tile.

### Keyboard and accessibility

- The image carries `Alt` as its alternative text.
- The fallback tile carries `role="img"` and `Alt` as its accessible label, so a
  failed image is still announced as the picture it was meant to be.
- The placeholder is hidden from assistive technology, so a loading image is not
  announced as a decoration.
- Every aura layer is hidden from assistive technology.
- The frame becomes a polite live region while `Ai` is anything but
  `AiState.None`, and carries no live region at all otherwise.
- The frame is not focusable and adds no stop to the tab order, because it is a
  picture and not a control.

### Appearance

- Every color the component renders comes from a token; the component names no
  literal color (`DESIGN-01`).
- The frame and the image are backed by `--glass-1`, so an image with
  transparency sits on the library's ground rather than on the page's.
- The fallback is backed by `--glass-1`, outlined with `--line-strong` and set
  in `--fg-dim`, with its text in `--fg-muted`.
- Each `ImageRounded` value maps to one radius token; `ImageRounded.Full` maps
  to `--r-pill`.
- The component uses its own isolated stylesheet rather than adding to the
  global one.
- The frame sits in the flow rather than floating, so it carries no frost
  (`DESIGN-06`).
- The accent appears as the aura's ring, comet, glow and wash and as the
  thinking cloud — never as the fill of the frame (`DESIGN-08`).
- The component branches on no color mode and holds no mode-assuming value, so
  the same markup serves light and dark (`DESIGN-02`).

### Motion

- The image fades in when it loads, at `--dur-med` with `--ease-out`.
- The fade is driven by a class the load handler adds, so it runs once per load
  rather than on every render.
- `AiState.Thinking` drifts a two-part coloured cloud over the image,
  continuously and alternating, so it reads as weather rather than as a loop.
- `AiState.Streaming` with `Progress` set eases the image's blur between values
  at `--dur-med`.
- `AiState.Streaming` with `Progress` null animates the blur to zero over
  `BlurDuration` and holds it there.
- `AiState.Generated` reveals the image with a one-shot scale-in at `--dur-slow`
  with `--ease-out`.
- Under `prefers-reduced-motion: reduce` the thinking cloud stops moving.
- Under `prefers-reduced-motion: reduce` the timed sharpen does not run and the
  image is left unblurred rather than stuck blurred.
- Under `prefers-reduced-motion: reduce` the generated reveal does not run.

### AI mode

- `Ai` defaults to `AiState.None`, so AI styling is opt-in.
- The aura variant follows `Aura` when set and a surrounding `DrylAiScope`
  otherwise.
- The component renders the shared aura vocabulary — ring, comet, glow, wash —
  rather than an image-specific aura (`AI-02`).
- The aura's markup is written out by the component rather than delegated to the
  shared helper, because the image adds effects of its own on top of it.
- `AiState.Active` and `AiState.Generated` render the wash layer; the other
  states do not.
- Leaving AI mode keeps the aura mounted for one `--dur-slow` beat, so it
  dissolves rather than snapping away.
- The wash is not rendered while the aura is exiting, so a dissolving aura does
  not flash a wash on its way out.
- Entering `AiState.Generated` replays the one-shot wash, every time it is
  entered.
- The aura lifecycle's timer is disposed with the component.
- `Progress` is clamped into 0–100 before it is used, so an out-of-range value
  cannot produce a negative blur.

## Recorded gaps

- **The state label is announced by nobody.** `Ai` set makes the frame a polite
  live region and puts the state into the frame's `aria-label`. A live region
  announces changes to its *content*, not to its label, and the frame's content
  does not change when the state does — so "Generating image… 40%" is written
  into the DOM and read by no screen reader. The percentage in particular
  changes only in the label.
- **A dead class.** `AiState.Active` adds an image-specific modifier class to
  the frame for which no rule exists anywhere in the library. The `Active` wash
  a reader sees comes from the shared wash layer; the extra class does nothing
  and has never done anything.
- **The blur radius is written twice, in two languages.** The timed sharpen's
  starting blur lives in a keyframe in `DrylImage.razor.css` and the
  progress-driven blur is computed against the same number in C#. Changing one
  and not the other makes the two streaming modes disagree about how blurry
  "0 %" is.
- **`BlurDuration` is a duration outside the scale.** It is a one-shot
  animation's duration expressed as an `int` of milliseconds with a hand-picked
  default, rather than one of the three motion tokens (`DESIGN-10`). It is
  consumer-facing, so it cannot simply become a token — but nothing relates its
  default to the rest of the library's motion.
- **The thinking cloud blends toward white.** It is composited with a screen
  blend, which lightens whatever is under it. Over a dark image that reads as a
  coloured drift; over a light image in light mode it mostly reads as a wash of
  white. The component branches on no mode, so this is not a `DESIGN-02`
  violation — but it is a mode-dependent *result* that needs checking by eye in
  both modes rather than being assumed from the tokens.
- **`FallbackSrc` is only tried once, and only per `Src`.** Changing
  `FallbackSrc` after a failure does not retry, because only a change of `Src`
  re-arms the chain.
- **The fallback's geometry is literal.** Its minimum height and its text size
  are written into `DrylImage.razor.css` with no token behind them
  (`DESIGN-01`).
- **No tests of its own.** None of the criteria above is guarded by a test —
  not the ratio derivation, not the two-step fallback chain, not the clamp on
  `Progress`, and not the invariant formatting, which is the one criterion with
  a known-recurring failure mode in this repository.

## Cross-cutting evidence (`SPEC-05`)

- **Both color modes** — token-only colors, verified by
  `node scripts/check-light-sync.mjs` and
  `node scripts/validate-light-contrast.mjs`. `--glass-1`, `--line`,
  `--line-strong`, `--fg-dim`, `--fg-muted`, `--accent-soft` and `--accent-b`
  are the mode-dependent tokens; the component defines no mode-specific rule.
  The screen-blended thinking cloud is the one effect whose *result* differs per
  mode, recorded above.
- **Enter/exit animation** — the load fade is the enter animation and it is the
  component's own. There is no exit: an image's removal is its host's decision,
  and a host that wants it animated wraps the frame in `DrylPresence`.
- **Keyboard and a11y** — the "Keyboard and accessibility" criteria above. The
  substantive decision is that the error fallback keeps `role="img"` and the alt
  text, so a failure degrades rather than disappears; the substantive omission
  is the unannounced state label, recorded above.
- **AI mode** — yes, and further than anywhere else in the category: the image
  itself blurs, drifts and reveals per state, which is the cue a viewer reads
  before they read the aura around the frame.
- **Demo page** — `DRYL.Website/Components/Pages/DemoImage.razor`, with the
  examples `Components/Examples/Image/Basics.razor`, `.../Fallback.razor` and
  `.../AiStates.razor`.
- **`ComponentCatalog`** — registered as `"Image"` / `image` in
  `DRYL.Website/Components/ComponentCatalog.cs`, flagged AI-capable.
