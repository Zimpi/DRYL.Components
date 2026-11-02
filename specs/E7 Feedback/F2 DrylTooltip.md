# DrylTooltip

## Meta
- **State:** Implemented
- **Source:** code/DRYL.Components/Components/Feedback/DrylTooltip.razor
- **Idea:** [`ideas/I37 A morphing tooltip with an open delay.md`](../../ideas/I37%20A%20morphing%20tooltip%20with%20an%20open%20delay.md)

## User Story

As a Blazor developer, I want to wrap any trigger in a tooltip and get a bubble
that appears on hover and on keyboard focus, never gets clipped by the glass
card it sits in, and costs nothing per instance, so that I can label a toolbar
full of icon buttons without thinking about portals, positioning or interop —
and, with a delay, without every bubble flashing up as the pointer passes.

## Description

`DrylTooltip` is a wrapper, not a bubble. It renders one inline element around
`ChildContent` and states up to three facts on it as data attributes: the text,
the preferred side and, when set, the open delay. Everything else — creating the bubble, measuring it, flipping
it, revealing it, hiding it — is done by delegated document listeners that are
installed once per page, and are specified in [`_Interop.md`](_Interop.md).

That split is the reason the component has no lifecycle at all. It obtains no
`IJSRuntime`, registers nothing and disposes nothing, so a page may hold
hundreds of tooltips without hundreds of interop calls, and a tooltip works
unchanged during prerender.

The bubble is **decorative** and hidden from assistive technology. It repeats
text the trigger is expected to carry itself: an icon-only button labels itself
with `AriaLabel`, and the tooltip shows the same words to people using a
pointer. A tooltip is therefore never the only place a label exists.

## Public API

| Member | Type | Default | Purpose |
|---|---|---|---|
| `Text` | `string` | `string.Empty` | Bubble text. `[EditorRequired]`. |
| `Placement` | `DrylTooltip.TooltipPlacement` | `TooltipPlacement.Top` | Preferred side of the trigger. |
| `Delay` | `TimeSpan` | `TimeSpan.Zero` | How long pointer or focus stays on the trigger before the bubble opens. Skipped while the bubble is warm. |
| `ChildContent` | `RenderFragment?` | `null` | The trigger. |
| `Class` | `string?` | `null` | Extra CSS class(es) merged onto the wrapper's own classes. |
| `AdditionalAttributes` | `IDictionary<string, object>?` | `null` | Pass-through attributes on the wrapper. |

The component takes no `Ai` and no `Aura`, and that is a decision rather than an
omission — see the cross-cutting evidence below.

## Acceptance Criteria

### Structure

- The component renders one inline wrapper element around `ChildContent`.
- The wrapper carries `Text` in its tooltip data attribute.
- The wrapper carries the string form of `Placement` in its placement data
  attribute.
- Each `TooltipPlacement` value maps to its own placement string.
- `TooltipPlacement.Top` supplies the placement string for any value the switch
  does not match.
- The wrapper carries `Delay` in whole milliseconds, culture-invariant, in its
  delay data attribute when `Delay` is greater than zero.
- The wrapper carries no delay attribute when `Delay` is zero or negative.
- The wrapper displays inline, so wrapping a trigger does not change the
  trigger's position in a row.
- `Class` is merged onto the wrapper's own classes rather than replacing them.
- `AdditionalAttributes` are applied to the wrapper.
- The component adds no element inside the wrapper: the trigger's own markup is
  what renders.

### The bubble

- A bubble appears when the pointer enters the wrapper or anything inside it,
  once the wrapper's delay has passed.
- A bubble appears when focus moves into the wrapper, once the wrapper's delay
  has passed, so a keyboard user reaches the same information as a pointer user.
- No bubble appears when pointer or focus leaves the wrapper before its delay
  has passed.
- A wrapper without a delay opens its bubble at once.
- The bubble is **warm** while it is open and for `--delay-short` after it
  closed: a wrapper reached while the bubble is warm skips its delay.
- When pointer or focus moves to another wrapper while the bubble is open, the
  same bubble glides to the new wrapper and takes its size there, over
  `--dur-med` with `--ease-in-out`; the text cross-fades instead of the bubble
  closing and reopening.
- After pointer or focus leaves a wrapper the bubble stays for `--dur-fast`
  before it closes, so the gap between two neighbouring triggers does not close
  it.
- The bubble shows the wrapper's current `Text`.
- No bubble appears when `Text` is empty.
- The bubble hides when the pointer leaves the wrapper and reaches no other.
- The bubble hides when focus leaves the wrapper and reaches no other.
- The bubble hides at once on pointer-down, so it does not linger over the thing the
  user just clicked.
- The bubble hides at once on scroll and on viewport resize, rather than staying
  at a stale position.
- A hide on pointer-down, scroll or resize leaves the bubble cold: the next
  wrapper waits its delay again.
- A scroll or resize while a delay is still running does not cancel it: the
  bubble opens when the delay ends, if pointer or focus is still on the wrapper
  then, placed from the wrapper's position at that moment.
- The bubble hides when its trigger is removed from the DOM while hovered.
- No bubble opens for a trigger inside a `[data-dryl-morph]` target while that
  target's morph is running: the trigger slid under a cursor that stood still,
  and its rect is the stretched FLIP geometry. The next pointer movement after
  the morph shows it as usual.
- One bubble exists per page, and a second tooltip reuses it rather than adding
  another.
- The bubble is rendered outside every component's subtree, so no ancestor's
  clipping or frost can cut it off.

### Placement

- The bubble is placed on the side named by `Placement` when the viewport has
  room for it there.
- The bubble flips to the opposite side when the preferred side has no room.
- The bubble is clamped into the viewport on both axes after placement, so it is
  never partly off-screen.
- The bubble is centred on the trigger along the axis it is not offset on.
- The bubble sits `--sp-3` away from the trigger.
- The bubble's anchor — the point on its edge facing the trigger — is aimed at
  the trigger's centre, also when the bubble was clamped.
- The bubble is measured before it is revealed, so it is never seen at an
  intermediate position.

### Keyboard and accessibility

- The bubble is hidden from assistive technology and adds no second announcement
  of the trigger's own label.
- The bubble does not receive pointer events, so it can never intercept a click
  meant for what is underneath it.
- The wrapper adds no `tabindex`, so the tooltip does not insert a stop into the
  tab order; the trigger keeps whatever focusability it had.
- The component installs no key handler, so `Escape` inside the trigger reaches
  whatever the trigger does with it.
- A trigger that conveys information only through its tooltip is a consumer
  error: the trigger carries its own accessible name, and the tooltip repeats it
  (`UX-05`).

### Appearance

- Every color the bubble renders comes from a token; the component names no
  literal color (`DESIGN-01`).
- The bubble is a floating surface on an opaque fill — `--panel-solid` — and
  therefore carries no frost, the case `DESIGN-07` reserves for an opaque ground.
- The bubble's border is `--line-strong`, its elevation is `--shadow-md` and its
  open radius is `--r-sm`.
- The bubble's padding is `--sp-2` by `--sp-3`.
- The bubble's text is `--fg` and does not wrap, so a short label stays on one
  line.
- A thread — a 1px line in `--accent-grad` with `--glow-accent` — connects the
  trigger to the bubble's anchor, across the `--sp-3` gap.
- A 1px `--accent-grad` edge runs along the bubble's bottom edge, or along its
  top edge when the bubble sits below the trigger.
  Both accents are lines, never a fill (`DESIGN-08`).
- The bubble branches on no color mode and holds no mode-assuming value, so the
  same bubble serves light and dark (`DESIGN-02`).

### Motion

- The bubble opens as a thread ("Faden"), in three beats:
  1. the thread grows out of the trigger towards the anchor over `--dur-fast`
     with `--ease-out`;
  2. after `--delay-short`, the bubble unfolds from the anchor — from a small
     pill to its full size, its radius settling from `--r-pill` to `--r-sm` —
     over `--dur-med` with `--ease-spring`, and its text fades in;
  3. the accent edge draws out from the centre over `--dur-slow`.
- The bubble is measured and placed before it is revealed, without transitions,
  so it never travels in from a previous position on a first open.
- The bubble and the thread fold back over `--dur-fast` when the bubble hides,
  rather than disappearing instantly.
- Every transition — the open, the close and the glide — is switched off under
  `prefers-reduced-motion: reduce`, leaving the bubble fully readable. The delay
  still applies.

## Recorded gaps

- **The bubble is not the trigger's accessible name.** A consumer who wraps an
  icon-only control and gives it no label of its own ships a control that is
  unnamed for screen-reader and touch users. The component cannot detect this;
  it is documented in the component's usage comment and stated here.
- **No touch affordance.** Touch fires no `pointerover` that stays, so a tooltip
  is effectively pointer- and keyboard-only.
- **`Text` is `[EditorRequired]` but defaults to `string.Empty`**, so omitting
  it is a compiler warning rather than an error, and the component silently
  renders no bubble.
- **The bubble's font size and weight are literals** in
  `code/DRYL.Components/wwwroot/dryl.css` — DRYL has no type-scale tokens.
  Padding, radius, colors, shadows, durations and easings are tokens.
- **The bubble is tested in the browser only.** `DrylTooltipTests` guards the
  wrapper's structure criteria; the delay timer, the warm window, the glide,
  the flip, the clamp and the hide-on-removal path live in `dryl.tooltip` and
  are verified in the browser.

## Cross-cutting evidence (`SPEC-05`)

- **Both color modes** — token-only colors, verified by
  `node scripts/check-light-sync.mjs` and
  `node scripts/validate-light-contrast.mjs`. `--panel-solid` and
  `--line-strong` are the mode-dependent tokens; the component defines no
  mode-specific rule.
- **Enter/exit animation** — both present, on the bubble: the thread choreography
  in, the fold back out, and a glide between triggers. The wrapper itself has nothing
  to animate.
- **Keyboard and a11y** — the "Keyboard and accessibility" criteria above. The
  substantive decision is that the tooltip opens on focus as well as on hover,
  and that the bubble is decorative rather than an accessible name.
- **AI mode** — **no**, deliberately. The component renders no surface of its
  own: its wrapper is a transparent inline box and its bubble is a shared,
  page-level element that would have to carry the aura for every tooltip at
  once. An aura here would either be invisible or would attach to the wrong
  thing. A trigger that needs to show AI activity carries `Ai` itself — the
  button, the field or the card it sits on (`AI-05`).
- **Demo page** — `DRYL.Website/Components/Pages/DemoTooltip.razor`, with the
  examples `Components/Examples/Tooltip/Placements.razor`,
  `.../IconOnly.razor` and `.../LongText.razor`.
- **`ComponentCatalog`** — registered as `"Tooltip"` / `tooltip` in
  `DRYL.Website/Components/ComponentCatalog.cs`, flagged as not AI-capable,
  which matches the decision above. Its one-line description there still reads
  "CSS-only hover tooltip", which has not been true since the bubble became a
  JS-driven body portal; a correction belongs to `DRYL.Website`, not here.
