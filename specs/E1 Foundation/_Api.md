# Foundation — Public API

The public surface that belongs to no single component: theming types, the DI
registration, the AI state vocabulary, the motion primitives, and the token
surface consuming apps are allowed to override.

**Source folder:** `code/DRYL.Components/Components/Providers/`

Foundation is the one category whose subject is not a family of widgets but the
library's own footing — and the five components in that folder are exactly that:
the plumbing a consumer mounts once in the layout rather than places on a page
(`DrylThemeProvider`, `DrylToastProvider`, `DrylPresence`, `DrylReconnectModal`,
`DrylColorModeToggle`). They moved here from `Components/Surfaces/` on
2026-08-11; the reasoning is in `ideas/I3 Component folder layout.md`.

Alongside them this category still documents the surface that belongs to no
component at all — which is why the sections below exist. This file carries no
`Meta` block: it is a reference for the specs around it, not a unit of
implementation.

*Scaffold. The shared types below are filled in during phase C, each listed with
the exact spelling used in code. Until then this file claims nothing.*

## Theming

*(phase C)*

## AI state vocabulary

### `AiState`

`code/DRYL.Components/AiState.cs` — the one AI vocabulary of the library
(`AI-01`). Six members, in this order; the order is part of the contract, since
the underlying values are what a stored or serialized state carries.

| Member | Kind | Meaning |
|---|---|---|
| `None` | — | No AI styling; the component renders normally. |
| `Active` | live | Persistent AI mode: the AI is present on this surface. |
| `Thinking` | live | A tool call or reasoning step is in flight. |
| `Streaming` | live | Output is arriving. |
| `Generated` | one-shot | Generation has just completed. Plays its reveal and then removes itself through `AuraLifecycle` (`AI-07`). |
| `Proposed` | resting | The AI is done, and a human now has to accept or dismiss what it produced. Stays until the host changes the state. Added by `ideas/I20 An AiState for work awaiting a human.md`. |

The three live states may only show while the AI is actually working on the
surface (`AI-06`). `Proposed` is the one resting state: it may stay for as long
as a human has not decided, and nothing in it loops.

The library never ends `Proposed` by itself. The host resolves it: accepting
moves the surface to `Generated`, dismissing moves it to `None`. What dismissing
means for the content — an element removed, a previous value restored — is the
host's business; the library offers no accept or dismiss API of its own.

`Proposed` is set only where an accept or dismiss action actually exists. A
surface the AI merely touched once is not a proposal.

`Proposed` was appended after `Generated`, so the underlying values of the five
older members are unchanged. A consumer's exhaustive `switch` over `AiState`
gains an unhandled case — a compiler warning, not a break (`REL-01`, MINOR).

*(`AiAura`, `AiScope` and `AuraLifecycle`: phase C. The behaviour of
`AuraLifecycle` under `Proposed` is specified with the aura it drives, in
`specs/E3 AI/F7 DrylAuraElements.md`.)*

## Motion primitives

*(mostly phase C. The morph surface is documented here because
`DrylRouteTransition` and `DrylMorph` both depend on it and neither owns it.)*

### `DrylMorphStyle`

How much of the morph vocabulary a target gets. Both tiers glide on
`--ease-viscous`; only `DepthGlass` pays for the blur/merge pass.

| Value | Meaning |
|---|---|
| `Glide` | Viscous easing only — the shape glides, no blur/merge pass. Cheap enough for high-frequency interactions. |
| `DepthGlass` | The full choreography — translucency pulse, mercury merge, decoupled clarity. For low-frequency, high-meaning merges. |

### `IDrylMorph`

Animates a state change as a movement of the elements that exist on both sides
of it, using FLIP — measure, re-render, invert, play. Registered scoped by
`AddDrylComponents()`. Targets announce themselves through the DOM
(`data-dryl-morph`), which is what `DrylMorph` renders.

| Member | Purpose |
|---|---|
| `RunAsync(Action mutate)` | Runs `mutate` (which must end in `StateHasChanged()`) inside a morph; completes when the morph has finished. |
| `RunAsync(Func<Task> mutate)` | Async-mutation overload. |
| `SignalRendered()` | Reports that a render reached the DOM. Called unconditionally; a no-op when no transition is in flight. `DrylMorph` calls it for its consumers. |
| `BeginNavigation(TimeSpan timeout)` | Starts a transition that a **coming navigation** completes, rather than one this service mutates itself. Ships as a **default interface implementation that does nothing**, so an existing implementer keeps compiling and simply never morphs a navigation. `timeout` bounds how long the old frame may be held. |

All four fall back to applying the change directly — no measuring, no
movement — during prerender, on a disconnected circuit, and when the user
prefers reduced motion (which the engine checks before it animates anything).

## Token surface

*(phase C — the consumer-overridable custom properties, cross-referenced to
[`../../harness/tokens.md`](../../harness/tokens.md))*

### Motion

Declared once in the shared `:root` block of `wwwroot/dryl.css`; mode-neutral,
so neither LIGHT-TOKEN-SET copy repeats them (`DESIGN-02`). A consumer may
override any of them; the rules that bind their use are `DESIGN-10` and
`harness/tokens.md`.

| Token | Default | Contract |
|---|---|---|
| `--dur-fast` | `140ms` | Transition scale — hover, press. |
| `--dur-med` | `240ms` | Transition scale — most colour and border changes. |
| `--dur-slow` | `420ms` | Transition scale — layout shifts, entries, large reveals. |
| `--dur-choreo` | `900ms` | One-shot, multi-beat choreography only; never on a `transition`. Not a fourth step of the scale. Added by `ideas/I2 Motion tokens for choreography and delay.md`. |
| `--delay-short` | `200ms` | A beat's offset inside an `animation`/`transition` shorthand. Added by I2. |
| `--delay-long` | `800ms` | A hold before something retires itself. Added by I2. |
| `--ease-out` | `cubic-bezier(0.16, 1, 0.3, 1)` | Exits, fade-ins, default reveal. |
| `--ease-in-out` | `cubic-bezier(0.65, 0, 0.35, 1)` | Layout shifts, content swaps. |
| `--ease-spring` | `cubic-bezier(0.34, 1.56, 0.64, 1)` | Toggles, indicator pings. |
| `--ease-viscous` | `cubic-bezier(0.45, 0.05, 0.15, 1)` | Morph settle only (`DrylMorphStyle`). |

There is no `linear` easing in the vocabulary (`DESIGN-10`). A stagger step
multiplied by an index (`--reveal-step`) and `0s` are not delays in the sense of
this table and stay outside it. `check-motion-tokens.mjs` enforces the
duration and delay side.

## Presence and shared reduced motion (I13)

`PresenceTransition` has `Fade`, `Scale`, `SlideUp`, `SlideDown`, `SlideLeft` and
`SlideRight`; `PresenceSpeed` has `Fast`, `Medium` and `Slow`. Their component
contract is [DrylPresence](F2%20DrylPresence.md).

Under reduced motion the shared `.spinner`, `.fade-in`, `.stagger > *` and
ambient `.aurora` have no decorative animation. Reveal utilities leave content
visible at its settled position. The dialog/backdrop and all Comet/Aurora
state/pseudo-element combinations likewise stop moving; static state cues remain
usable. Preference changes during an exit still permit its removal. No new
animation, duration, easing or token is added by I13.
