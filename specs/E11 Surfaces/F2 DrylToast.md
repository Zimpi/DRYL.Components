# DrylToast

## Meta
- **State:** Implemented
- **Source:** code/DRYL.Components/Components/Surfaces/DrylToast.razor
              code/DRYL.Components/Toasts/ToastVariant.cs

## User Story

As a Blazor developer, I want one glass card that shows a short message with an
icon, an optional title, a close button and a countdown, in the semantic colour
of what happened, so that every toast in my app looks and reads the same
whether the library's provider renders it or I place it myself.

## Description

`DrylToast` is the visual shell of a toast. Almost every consumer meets it only
through [`DrylToastProvider`](../E1%20Foundation/F3%20DrylToastProvider.md),
which renders one for every toast raised through `IDrylToastService`; placing it
directly is possible and supported, but rare.

It has three render modes: a message alone, a title above a message, and a title
above a custom body (`ChildContent`, which then replaces the message). The
variant decides the icon, the accent and the live-region politeness. The
countdown bar is drawn by CSS; the toast itself owns no timer — the provider's
`dryl.toast` does.

The motion depends on who owns the toast. Standalone, it enters with its own CSS
entrance. Inside the provider's host it carries none: `dryl.toast` brings it in
(out of its origin, the bell, or an unfolding capsule) and plays its leave —
see the provider's spec.

AI follows the dominance rule: with `Ai` other than `None` the shared aura takes
over the card, and the variant accent survives only on the icon chip.

## Public API

| Member | Type | Default | Purpose |
|---|---|---|---|
| `Variant` | `ToastVariant` | `ToastVariant.Default` | Semantic flavour: icon, accent, glow and live-region politeness. |
| `Title` | `string?` | `null` | Stronger line above the message or body. |
| `Message` | `string?` | `null` | Plain-text body. Ignored when `ChildContent` is set. |
| `ChildContent` | `RenderFragment?` | `null` | Custom body; replaces `Message`. |
| `Icon` | `string?` | `null` | `DrylIcon` name overriding the variant's icon; an empty string shows none. |
| `ShowCloseButton` | `bool` | `true` | Renders the close button. |
| `ShowProgress` | `bool` | `true` | Shows the countdown bar while `Duration` is above zero. |
| `Duration` | `int` | `5000` | Countdown length in milliseconds; `0` renders no bar. |
| `Ai` | `AiState` | `AiState.None` | AI state; when `None`, the cascaded `IDrylToastInstance`'s state applies. |
| `Aura` | `AiAura` | `AiAura.Comet` | Aura variant. Non-nullable: a toast renders at layout root, outside any `DrylAiScope`. |
| `IsLeaving` | `bool` | `false` | Plays the shrink-and-fade exit inside a `.toast-host`. The provider does not set it. |
| `OnClose` | `EventCallback` | — | Raised by the close button; without a delegate the button closes the cascaded instance. |
| `Class` | `string?` | `null` | Extra classes merged onto the card. |
| `AdditionalAttributes` | `IDictionary<string, object>?` | `null` | Pass-through attributes on the card. |

`ToastVariant`, top-level in `code/DRYL.Components/Toasts/ToastVariant.cs`:
`Default`, `Success`, `Warning`, `Error`, `Info` — see [`_Api.md`](_Api.md).

## Acceptance Criteria

### Content

- The toast renders `Message` as its body when no `ChildContent` is given.
- The toast renders `ChildContent` as its body instead of `Message` when both are
  given.
- The toast renders `Title` above the body when it is set, and no title element
  when it is not.
- The toast renders the variant's icon in an icon chip: `Check` for `Success`,
  `Alert` for `Warning` and `Error`, `Info` for `Info`, none for `Default`.
- An `Icon` overrides the variant's icon; an empty `Icon` renders no chip.

### Close and countdown

- The close button is rendered while `ShowCloseButton` is true, labelled
  "Dismiss notification".
- Activating the close button raises `OnClose` when it has a delegate, and
  otherwise closes the cascaded `IDrylToastInstance`.
- A `Duration` above zero renders a countdown bar whose CSS animation runs for
  exactly that duration.
- `ShowProgress = false` keeps the bar in the markup but invisible, so the
  provider's timer still finds its duration.
- `Duration = 0` renders no bar.
- Hovering the toast pauses the bar's animation.

### Accessibility

- `Error` and `Warning` render with `role="alert"` and `aria-live="assertive"`.
- Every other variant renders with `role="status"` and `aria-live="polite"`.
- The close button is a real `button`, reachable by keyboard, with a visible
  focus ring.

### Appearance

- The card is glass on `--panel-soft`, frosted, 1px `--line-strong` border and
  `--r-md` radius.
- Each non-default variant tints the border, adds a left rail and a soft glow in
  its semantic colour, and colours the icon chip and the countdown bar.
- The colours come from tokens only (`--success`, `--warning`, `--danger`,
  `--info` and their `-hi` partners), so both colour modes follow.

### Motion

- Standalone, the toast enters with a spring from the side, the icon chip pops a
  beat later and a single shine sweeps across.
- Inside a `.toast-host` the toast has no CSS entrance; it stays hidden until the
  provider's `dryl.toast` takes it over (`data-dryl-entered` on its slot).
- `IsLeaving` plays the shrink, blur and fade exit inside a `.toast-host`.
- Hovering lifts the card slightly.
- With `prefers-reduced-motion: reduce`, no entrance, shine, chip pop or hover
  lift plays.

### AI mode

- `Ai` other than `None` renders the shared aura; the left rail and tinted
  border step back and the variant accent stays on the icon chip.
- With `Ai` at `None`, the toast shows the AI state of its cascaded
  `IDrylToastInstance`, so `SetAi` on a reference reaches the card.
- A move into `Generated` plays the bloom once.

## Recorded gaps

- The provider's host is an `aria-live` region and every toast is a live region
  of its own; nested regions can announce a toast twice. Candidate for its own
  idea (I34, out of scope).
- `Error` and `Warning` count down like every other variant, so a failure
  disappears on its own after `Duration`. Candidate for its own idea.
- The countdown bar runs on `linear`, which `DESIGN-10` reserves for continuous
  motion; a timer drawn eased would misstate the time left. Not yet ruled on.
- The card's frost is a hand-written `backdrop-filter` rather than
  `--glass-fx-float` (`DESIGN-06`, `DESIGN-07`) — pre-existing debt.

## Cross-cutting evidence (`SPEC-05`)

- **Both color modes** — token-only colours; `node scripts/check-light-sync.mjs`
  and `node scripts/validate-light-contrast.mjs`; checked by eye in both modes
  through the provider on 2026-11-01.
- **Enter/exit animation** — standalone CSS entrance and `IsLeaving` exit; inside
  the provider, `dryl.toast` (provider spec).
- **Keyboard and a11y** — the close button and the variant-dependent live
  region, above.
- **AI mode** — yes, the shared aura with the dominance rule.
- **Demo page** — on `DRYL.Website`, through the provider's demo.
- **`ComponentCatalog`** — `DRYL.Website` is not part of this repository; the
  entry is maintained there (`REL-04`).
