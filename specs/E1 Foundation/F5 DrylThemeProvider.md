# DrylThemeProvider

## Meta
- **State:** Implemented
- **Source:** code/DRYL.Components/Components/Providers/DrylThemeProvider.razor
              code/DRYL.Components/Theming/IDrylThemeService.cs
              code/DRYL.Components/Theming/DrylThemeService.cs
              code/DRYL.Components/Theming/DrylTheme.cs
              code/DRYL.Components/Theming/DrylAccent.cs
              code/DRYL.Components/Theming/DrylSemantic.cs
              code/DRYL.Components/Theming/DrylChartPalette.cs
              code/DRYL.Components/Theming/DrylThemes.cs
              code/DRYL.Components/Theming/DrylColorMode.cs

## User Story

As a Blazor developer, I want to place one component in my root layout that
applies a brand theme and the colour mode — and lets me switch either at runtime
through a service — so that the whole library takes on my accent and the user's
light/dark choice without a flash of the wrong look and without me touching any
component.

## Description

`DrylThemeProvider` is mounted once in the root layout. It renders nothing
visible: an inline `<style>` that writes the active theme's **seed** custom
properties onto `:root`, and a small pre-paint script that restores the colour
mode before first paint.

**Seeds, not palettes.** A `DrylTheme` carries only seeds: the brand accent
(`--accent-a`, `--accent-b`), an optional AI accent (`--ai-a`, `--ai-b`), optional
semantic overrides (`--success`, `--warning`, `--danger`, `--info`) and optional
chart-slot overrides (`--chart-1` … `--chart-6`). Everything else — soft fills,
accent lines, glows, the AI aura, the first two chart series — is derived from
those seeds in `dryl.css` with `color-mix()`, so a theme cannot drift out of
coherence. A seed the theme leaves unset is not emitted, and `dryl.css`'s
default (or, for the AI pair, the brand accent) applies.

**First paint is server-side.** During prerender the inline `<style>` already
carries the seeds, so the first paint is correct without any JS. **Runtime
changes glide.** After the first render, a theme change made through
`IDrylThemeService` is pushed to `document.documentElement` through
`dryl.theme.apply`; because the accent, AI and chart seeds and the neutral
tokens are registered `@property` colours with a `:root` transition on
`--dur-slow` / `--ease-in-out`, the whole derived chain glides — instantly for
reduced-motion users.

**The mode contract is one attribute.** `DrylColorMode.System` leaves
`data-dryl-mode` off the document root, and `dryl.css`'s
`prefers-color-scheme` media query decides — live, with no listener.
`DrylColorMode.Light` / `Dark` set `data-dryl-mode="light"` / `"dark"`, which the
two LIGHT-TOKEN-SET copies in `dryl.css` key on (`DESIGN-02`). An explicit
choice is persisted in `localStorage` under `dryl-color-mode`; choosing
`System` clears it. The pre-paint script restores a persisted choice — or, when
there is none, the `Mode` parameter — before first paint. After the first
render the provider reads the persisted choice back and syncs
`IDrylThemeService.CurrentMode` to it without writing storage again, so a
switch UI such as `DrylColorModeToggle` shows the restored mode.

**One service, two events.** `IDrylThemeService` (scoped, registered by
`AddDrylComponents()`) holds `Current` and `CurrentMode`. `OnThemeChanged` is a
single-subscriber event meant for the one provider; `OnModeChanged` is
multicast — the provider applies the mode, and any number of switch UIs
re-render on it; every subscriber is awaited in turn.

Usage:

```razor
<DrylThemeProvider Theme="DrylThemes.Nebula" Mode="DrylColorMode.System" />
```

## Public API

Namespace: `DRYL.Components` (component), `DRYL.Components.Theming` (types and
service — see `_Api.md`, section *Theming*).

| Member | Type | Default | Purpose |
|---|---|---|---|
| `Theme` | `DrylTheme?` | `null` | The theme to apply on startup. `null` keeps the service's current theme, which starts as `DrylThemes.Default`. |
| `Mode` | `DrylColorMode` | `DrylColorMode.System` | The colour mode to start in. A persisted explicit choice wins over it. |

`IDrylThemeService`:

| Member | Purpose |
|---|---|
| `Current` | The active `DrylTheme`; starts as `DrylThemes.Default`. |
| `SetThemeAsync(DrylTheme theme)` | Sets `Current` and raises `OnThemeChanged`. |
| `SetAccentAsync(string a, string b)` | Replaces only `Current.Accent`. |
| `OnThemeChanged` | `Func<Task>` event; single subscriber. |
| `CurrentMode` | The chosen `DrylColorMode`; starts as `DrylColorMode.System`. |
| `SetModeAsync(DrylColorMode mode)` | Sets `CurrentMode` and raises `OnModeChanged`. |
| `OnModeChanged` | `Func<Task>` event; multicast. |

## Acceptance Criteria

### Seeds on first render

- The component renders one `<style>` element whose content is a `:root` rule.
- The `:root` rule carries the custom properties of `IDrylThemeService.Current`.
- With `Theme` omitted, the rule carries the seeds of `DrylThemes.Default`.
- With `Theme` set to a theme other than `IDrylThemeService.Current`, the
  provider sets it on the service during initialisation.
- With `Theme` set, the rule carries that theme's seeds.
- The rule always carries `--accent-a` and `--accent-b` from `DrylTheme.Accent`.
- With `DrylTheme.AiAccent` set, the rule carries `--ai-a` and `--ai-b`.
- With `DrylTheme.AiAccent` `null`, the rule carries neither `--ai-a` nor
  `--ai-b`.
- Each non-blank member of `DrylTheme.Semantic` emits its token (`--success`,
  `--warning`, `--danger`, `--info`); a `null` or blank member emits nothing.
- Each non-blank member of `DrylTheme.Charts` emits its slot (`--chart-1` …
  `--chart-6`); a `null` or blank member emits nothing.
- The first paint carries the seeds without any JS call.

### Runtime theme change

- After `IDrylThemeService.SetThemeAsync`, the provider re-renders its `<style>`
  with the new theme's seeds.
- After the first render, a theme change also calls `dryl.theme.apply` with the
  new seed string.
- `dryl.theme.apply` first removes every seed a theme may emit from the
  document root's inline style, then sets the new ones — a seed the new theme
  leaves unset falls back to its `dryl.css` default.
- Before the first render, a theme change makes no JS call.
- A disconnected circuit during `dryl.theme.apply` is swallowed.
- Without reduced motion, `--accent-a`, `--accent-b`, `--ai-a`, `--ai-b` and
  `--chart-1` … `--chart-6` transition on `--dur-slow` / `--ease-in-out`.
- Under `prefers-reduced-motion: reduce`, a theme change applies instantly.

### Colour mode

- The component renders one inline pre-paint `<script>`.
- The script reads `dryl-color-mode` from `localStorage`.
- A stored `light` or `dark` sets `data-dryl-mode` to that value on the
  document root.
- Without a stored `light` or `dark`, `Mode` `DrylColorMode.Light` or
  `DrylColorMode.Dark` sets `data-dryl-mode` to `light` or `dark`.
- Without a stored choice and with `Mode` `DrylColorMode.System`, the script
  sets no attribute.
- A `localStorage` failure in the script is swallowed.
- After the first render, the provider reads the stored choice through
  `dryl.theme.storedMode`.
- When the stored choice (or, without one, `Mode`) differs from
  `IDrylThemeService.CurrentMode`, the provider sets it on the service.
- That startup sync calls `dryl.theme.applyMode` with persistence off — it does
  not write `localStorage`.
- After the first render, `IDrylThemeService.SetModeAsync` makes the provider
  call `dryl.theme.applyMode` with `"light"`, `"dark"` or `"system"` and
  persistence on.
- `"light"` / `"dark"` set `data-dryl-mode` on the document root and, with
  persistence on, store the value under `dryl-color-mode`.
- `"system"` removes `data-dryl-mode` and, with persistence on, removes
  `dryl-color-mode` from storage.
- `data-dryl-mode` is set even when `localStorage` is unavailable.
- With no `data-dryl-mode`, the rendition follows `prefers-color-scheme` live.
- Without reduced motion, a mode switch glides the registered neutral tokens on
  `--dur-slow` / `--ease-in-out`.

### Service

- `IDrylThemeService` is registered scoped by `AddDrylComponents()`.
- `Current` starts as `DrylThemes.Default`.
- `SetThemeAsync(null)` throws `ArgumentNullException`.
- `SetThemeAsync` raises `OnThemeChanged` on every call.
- `SetAccentAsync(a, b)` replaces `Current.Accent` and keeps `AiAccent`,
  `Semantic` and `Charts`.
- `CurrentMode` starts as `DrylColorMode.System`.
- `SetModeAsync` with the current mode does nothing and raises no event.
- `SetModeAsync` with another mode sets `CurrentMode`, then raises
  `OnModeChanged`.
- `OnModeChanged` awaits every subscriber, one after the other.

### Presets

- `DrylThemes.Default` is `DrylThemes.Nebula`.
- `DrylThemes.Nebula` sets only `Accent` — applying it changes nothing
  visually against `dryl.css`'s defaults.
- `DrylThemes.Ember`, `Verdant` and `Mono` each set a different `Accent`.
- `DrylThemes.Ember` and `Verdant` override the one chart slot their derived
  series would shadow; `DrylThemes.Mono` overrides all six slots.

### Lifecycle

- The provider subscribes to `OnThemeChanged` and `OnModeChanged` on
  initialisation.
- On dispose it unsubscribes from both.
- The component branches on no colour mode in its own output; both renditions
  come from the token set (`DESIGN-02`).

## Recorded debt

- **Parameters are read once.** `Theme` and `Mode` are evaluated in
  `OnInitializedAsync` only; changing either on a mounted provider has no
  effect. The theming guide calls `Mode` a startup value, but neither parameter
  says so in code.
- **Semantic overrides do not glide.** `--success`, `--warning`, `--danger` and
  `--info` are not registered `@property` colours and are absent from the
  `:root` transition list, so a theme change that alters them snaps while the
  accent, AI and chart seeds glide. `IDrylThemeService.SetThemeAsync` documents
  "Animates if motion is allowed" for the whole theme.
- **`OnThemeChanged` is single-subscriber by convention only.** It is a plain
  multicast delegate invoked with `handler.Invoke()`: a second subscriber —
  e.g. a second provider — is called, but only the last delegate's task is
  awaited. The interface remarks call this unsupported; nothing prevents it.
- **The pre-paint guarantee needs server-rendered markup.** The restore script
  is emitted as a `MarkupString`; it runs when the provider's HTML is
  prerendered. Rendered only interactively (WebAssembly without prerender),
  the browser does not execute an inserted script, and the mode is applied only
  by the startup sync after the first render — a flash of the system mode.
- **The storage contract is written twice.** The pre-paint script repeats the
  key `dryl-color-mode` and the `light`/`dark` filter of `dryl.theme.storedMode`
  in `dryl.js`; they are kept in sync by a comment only.
- **Inline script and style vs. a strict CSP.** The pre-paint `<script>` and the
  seed `<style>` are inline; an app with a Content Security Policy without
  `'unsafe-inline'` (or a nonce/hash) blocks both.
- **Theme equality is by reference.** `Theme` is applied when it is not
  `ReferenceEquals` to `Current`; a structurally equal new instance still
  raises `OnThemeChanged` (harmless, but a redundant apply).
- **`SetThemeAsync` always raises.** Setting the current theme again raises
  `OnThemeChanged` and re-applies it; `SetModeAsync`, by contrast, is a no-op
  for the current mode.

## Cross-cutting evidence (`SPEC-05`)

- **Both color modes** — the provider *is* the mode switch: it writes no colour
  of its own and only toggles `data-dryl-mode`, which both LIGHT-TOKEN-SET
  copies key on; inspected on `/components/theming` in dark and light (2026-11-20)
- **Enter/exit animation** — **exception (`DESIGN-11`)**: the provider renders
  no visible surface and is mounted once with the layout, never conditionally,
  so there is nothing of its own to enter or leave. What it changes moves: a
  theme or mode switch glides the registered seed and neutral tokens on
  `--dur-slow` / `--ease-in-out`, instantly under reduced motion. The snapping
  semantic overrides are recorded above.
- **Keyboard and a11y** — not applicable to the provider itself: it renders no
  interactive or readable content. The mode it applies is always one of the two
  contrast-validated token sets (`scripts/validate-light-contrast.mjs`).
- **AI mode** — **no**, deliberately: the component has no `Ai` parameter. It
  supplies the AI seeds (`--ai-a`, `--ai-b`, through `DrylTheme.AiAccent`) that
  every AI surface derives from, but shows no AI work of its own (`AI-05`). The
  catalog row agrees (AI flag `false`).
- **Demo page** — `/components/theming`
  (`DRYL.Website/Components/Pages/DemoTheming.razor`): derived-token preview,
  provider setup, presets, runtime switching, AI accent and semantics. The site
  mounts the real provider in `DRYL.Website/Components/Layout/MainLayout.razor`
  and `LandingLayout.razor`; `ThemeSwitcher.razor` in the top bar drives
  `IDrylThemeService` at runtime. The mode half is shown on `/components/color-mode`
  (see `F6 DrylColorModeToggle.md`).
- **`ComponentCatalog`** — `"Theming"` / `theming` in
  `DRYL.Website/Components/ComponentCatalog.cs`, group `Customization`,
  `ClassName` `"DrylThemeProvider"`, folder `Providers`, AI flag `false`
  (`REL-04`).
- **Tests** — `tests/DRYL.Components.Tests/Theming/DrylThemeProviderTests.cs`,
  `DrylThemeServiceTests.cs`, `DrylThemeServiceModeTests.cs`,
  `DrylThemeTests.cs`, `DrylCssDerivationTests.cs`; service registration in
  `tests/DRYL.Components.Tests/ServiceRegistrationTests.cs`.
