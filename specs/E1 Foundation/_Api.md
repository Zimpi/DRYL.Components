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

All in namespace `DRYL.Components.Theming`, under
`code/DRYL.Components/Theming/`. Behaviour is pinned in
[`F5 DrylThemeProvider.md`](F5%20DrylThemeProvider.md), which claims these
files; [`F6 DrylColorModeToggle.md`](F6%20DrylColorModeToggle.md) consumes the
service. The consumer guide is
[`../../harness/theming.md`](../../harness/theming.md).

### `DrylTheme` (sealed record)

A theme carries **seeds only**; everything else is derived from them in
`dryl.css` with `color-mix()`.

| Member | Type | Default | Emits |
|---|---|---|---|
| `Accent` | `DrylAccent` | required | `--accent-a`, `--accent-b` |
| `AiAccent` | `DrylAccent?` | `null` | `--ai-a`, `--ai-b`; `null` emits neither, and AI surfaces reuse the brand accent |
| `Semantic` | `DrylSemantic?` | `null` | the non-blank members of `DrylSemantic` |
| `Charts` | `DrylChartPalette?` | `null` | the non-blank members of `DrylChartPalette` |

`ToCssVariables()` is `internal`: the provider's serialisation to
`--key:value;` pairs, not consumer API.

### `DrylAccent` (readonly record struct)

`DrylAccent(string A, string B)` — the two stops of the accent gradient; `A`
maps to `--accent-a` (or `--ai-a`), `B` to `--accent-b` (or `--ai-b`).

### `DrylSemantic` (sealed record)

`Success` → `--success`, `Warning` → `--warning`, `Danger` → `--danger`,
`Info` → `--info`; each `string?`, `null` keeps the `dryl.css` default.

### `DrylChartPalette` (sealed record)

`Series1` … `Series6` → `--chart-1` … `--chart-6`; each `string?`, `null` keeps
the default — slots 1 and 2 derived from the accent seeds, slots 3–6 fixed
anchors. The chart components that read these slots are specified in `E4
Charts`.

### `DrylThemes` (static)

| Member | Notes |
|---|---|
| `Nebula` | Violet → cyan; identical to the `dryl.css` defaults. Sets only `Accent`. |
| `Ember` | Amber → red; overrides `Series3`. |
| `Verdant` | Green → teal; overrides `Series4`. |
| `Mono` | Near-monochrome slate; overrides all six chart slots. |
| `Default` | Returns `Nebula`. |

### `DrylColorMode` (enum)

`System`, `Dark`, `Light` — in this order; `System` is the default value.
`System` follows `prefers-color-scheme`; the other two force a mode through
`data-dryl-mode` on the document root.

### `IDrylThemeService` / `DrylThemeService` (scoped)

| Member | Notes |
|---|---|
| `Current` | `DrylTheme`; starts as `DrylThemes.Default`. |
| `SetThemeAsync(DrylTheme theme)` | Throws on `null`; sets `Current`, raises `OnThemeChanged` on every call. |
| `SetAccentAsync(string a, string b)` | `SetThemeAsync(Current with { Accent = … })`. |
| `OnThemeChanged` | `event Func<Task>?`; designed for one subscriber (the provider). |
| `CurrentMode` | `DrylColorMode`; starts as `System`. |
| `SetModeAsync(DrylColorMode mode)` | No-op for the current mode; otherwise sets `CurrentMode` and raises `OnModeChanged`. |
| `OnModeChanged` | `event Func<Task>?`; multicast — every subscriber awaited in turn. |

### The mode contract

- **`data-dryl-mode`** on `document.documentElement`: absent = follow the
  system, `"light"` / `"dark"` = forced. Both LIGHT-TOKEN-SET copies in
  `dryl.css` key on it (`DESIGN-02`); no component reads it.
- **`localStorage` key `dryl-color-mode`**: holds `"light"` or `"dark"` for an
  explicit choice; absent for System. Written by `dryl.theme.applyMode`, read by
  the provider's pre-paint script and `dryl.theme.storedMode`.

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

### `IDrylAiOperation`

`code/DRYL.Components/Ai/IDrylAiOperation.cs` — the handle
`IDrylAiActivityService.Begin(key, initial)` returns. Each member moves the
operation's key to one state; disposing the handle settles the key back to
`None`.

| Member | Sets the key to |
|---|---|
| `Thinking()` | `Thinking` |
| `Streaming()` | `Streaming` |
| `Generated()` | `Generated`. The key rests there until the handle is disposed or a new operation begins, so the reveal can play. |
| `Proposed()` | `Proposed`. The key rests there until the host resolves it — accept → `Generated()`, dismiss → disposing the handle. Added by `ideas/I31 Proposed wherever the library waits for a decision.md`. |
| `Dispose()` | `None`. A second call does nothing. |

`Proposed()` ships as a **default interface implementation that sets nothing**,
so an implementer written before it keeps compiling (`REL-01`, MINOR); the
library's own operation overrides it.

*(`IDrylAiActivityService` itself, `AiAura`, `AiScope` and `AuraLifecycle`: phase C. The behaviour of
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

## Toasts (`F3 DrylToastProvider`)

All in namespace `DRYL.Components.Toasts`, under `code/DRYL.Components/Toasts/`;
registered by `AddDrylComponents()`. Behaviour is pinned in
[`F3 DrylToastProvider.md`](F3%20DrylToastProvider.md).

### `IDrylToastService` (scoped)

| Member | Notes |
|---|---|
| `Show(string message, ToastOptions? options = null)` | Shows a toast; returns its `IDrylToastReference`. |
| `ShowSuccess` / `ShowWarning` / `ShowError` / `ShowInfo(string message, string? title = null, ToastOptions? options = null)` | Set `Variant` (and `Title` when given) on the options, then `Show`. |
| `Show<TComponent>(ToastParameters? parameters = null, ToastOptions? options = null)` | Renders `TComponent` as the body. |
| `CloseAll()` | Closes every visible toast. |
| `OnToastAdded`, `OnToastCloseRequested`, `OnToastUpdated` | Events the provider subscribes to. |

### `IDrylToastReference` / `IDrylToastInstance`

`Id`, `Close()`, `SetAi(AiState)`; the reference adds `OnClosed` (raised once,
after removal), the cascaded instance adds `Options`, `Message` and `Ai`.

### `ToastOptions`

| Member | Type | Default | Notes |
|---|---|---|---|
| `Variant` | `ToastVariant` | `Default` | See `E11 Surfaces/_Api.md`. |
| `Title` | `string?` | `null` | |
| `Duration` | `int` | `5000` | Milliseconds; `0` never expires. |
| `ShowCloseButton` | `bool` | `true` | |
| `PauseOnHover` | `bool` | `true` | Documented, but not read: hovering always pauses. Pre-existing. |
| `ShowProgress` | `bool` | `true` | |
| `Ai` | `AiState` | `None` | |
| `Icon` | `string?` | `null` | Empty string: no icon. |
| `Class` | `string?` | `null` | |
| `Origin` | `ElementReference?` | `null` | Added in 3.16.0 (I34). The element the toast grows out of; `null` means it comes out of the bell, or unfolds as a capsule. `DrylButton.Element` is the usual value. |
| `KeepInInbox` | `bool` | `true` | Added in 3.16.0 (I34). Lets a bell-born toast that expires return into the inbox. |

### `ToastParameters` and `ToastPosition`

`ToastParameters` is a name-to-value bag for `Show<TComponent>`: indexer,
`Add`, `Contains`, `Count`, `ToDictionary()`; a `null` value removes the name.
`ToastPosition`: `TopRight`, `TopLeft`, `TopCenter`, `BottomRight`,
`BottomLeft`, `BottomCenter`.

## Reconnect modal (`F4 DrylReconnectModal`)

`DrylReconnectModal` defines no enum, model or service; its parameters are all
strings, one `bool` and the `Class` / `AdditionalAttributes` pair, listed in
[`F4 DrylReconnectModal.md`](F4%20DrylReconnectModal.md). Two contracts it
depends on are not its own:

- **The framework's reconnect-display contract.** The root's id
  `components-reconnect-modal`, the state classes `components-reconnect-show`,
  `-hide`, `-retrying`, `-paused`, `-resume-failed`, `-failed` and `-rejected`,
  the `components-reconnect-state-changed` event with its `state` and `remote`
  detail, the globals `Blazor.reconnect()` and `Blazor.resumeCircuit()`, and the span ids
  `components-reconnect-current-attempt`, `components-reconnect-max-retries` and
  `components-seconds-to-next-attempt` are spelled by ASP.NET Core's
  `blazor.web.js`, not by the library (read from the .NET 11.0.0 script).
- **`--z-reconnect`.** Declared in the shared `:root` block of `dryl.css`,
  mode-neutral, the top of the library's stacking order — the overlay has to
  win over every other layer while the circuit is down.

