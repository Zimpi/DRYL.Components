# DrylTooltip

## Meta
- **State:** Implemented
- **Source:** code/DRYL.Components/Components/Feedback/DrylTooltip.razor

## User Story

As a Blazor developer, I want to wrap any trigger in a tooltip and get a bubble
that appears on hover and on keyboard focus, never gets clipped by the glass
card it sits in, and costs nothing per instance, so that I can label a toolbar
full of icon buttons without thinking about portals, positioning or interop.

## Description

`DrylTooltip` is a wrapper, not a bubble. It renders one inline element around
`ChildContent` and states two facts on it as data attributes: the text, and the
preferred side. Everything else — creating the bubble, measuring it, flipping
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
- The wrapper displays inline, so wrapping a trigger does not change the
  trigger's position in a row.
- `Class` is merged onto the wrapper's own classes rather than replacing them.
- `AdditionalAttributes` are applied to the wrapper.
- The component adds no element inside the wrapper: the trigger's own markup is
  what renders.

### The bubble

- A bubble appears when the pointer enters the wrapper or anything inside it.
- A bubble appears when focus moves into the wrapper, so a keyboard user reaches
  the same information as a pointer user.
- The bubble shows the wrapper's current `Text`.
- No bubble appears when `Text` is empty.
- The bubble hides when the pointer leaves the wrapper.
- The bubble hides when focus leaves the wrapper.
- The bubble hides on pointer-down, so it does not linger over the thing the
  user just clicked.
- The bubble hides on scroll and on viewport resize, rather than staying at a
  stale position.
- The bubble hides when its trigger is removed from the DOM while hovered.
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
- The bubble's border is `--line-strong` and its elevation is `--shadow-md`.
- The bubble's text is `--fg` and does not wrap, so a short label stays on one
  line.
- The bubble branches on no color mode and holds no mode-assuming value, so the
  same bubble serves light and dark (`DESIGN-02`).

### Motion

- The bubble fades and lifts in over `--dur-fast` with `--ease-out`.
- The bubble lifts from below when it is placed above the trigger, and from
  above when it is placed below, so the motion points away from what it labels.
- The bubble fades out over the same duration when it hides, rather than
  disappearing instantly.
- Both transitions are switched off under `prefers-reduced-motion: reduce`,
  leaving the bubble fully readable.

## Recorded gaps

- **The bubble is not the trigger's accessible name.** A consumer who wraps an
  icon-only control and gives it no label of its own ships a control that is
  unnamed for screen-reader and touch users. The component cannot detect this;
  it is documented in the component's usage comment and stated here.
- **No open delay.** The bubble appears on the first `pointerover`, so moving
  the pointer across a toolbar flashes every bubble on the way. A delay would be
  a new motion value and therefore a maintainer decision (`DESIGN-10`).
- **No touch affordance.** Touch fires no `pointerover` that stays, so a tooltip
  is effectively pointer- and keyboard-only.
- **`Text` is `[EditorRequired]` but defaults to `string.Empty`**, so omitting
  it is a compiler warning rather than an error, and the component silently
  renders no bubble.
- **The bubble's padding, radius and font size are literals** in
  `code/DRYL.Components/wwwroot/dryl.css`, not tokens. Colors, shadows,
  durations and easings are tokens. Recorded as debt, not as compliance.
- **No tests of its own.** None of the criteria above is guarded by a test; the
  flip, the clamp and the hide-on-removal path are verified in the browser only.

## Cross-cutting evidence (`SPEC-05`)

- **Both color modes** — token-only colors, verified by
  `node scripts/check-light-sync.mjs` and
  `node scripts/validate-light-contrast.mjs`. `--panel-solid` and
  `--line-strong` are the mode-dependent tokens; the component defines no
  mode-specific rule.
- **Enter/exit animation** — both present, on the bubble: a fade-and-lift in,
  and a fade out driven by the same transition. The wrapper itself has nothing
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
