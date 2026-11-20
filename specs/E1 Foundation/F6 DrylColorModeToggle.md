# DrylColorModeToggle

## Meta
- **State:** Implemented
- **Source:** code/DRYL.Components/Components/Providers/DrylColorModeToggle.razor

## User Story

As a Blazor developer, I want a ready-made icon button for my app bar that
cycles the colour mode between System, Light and Dark, so that my users can
choose their mode — and keep it — without me building a switcher.

## Description

`DrylColorModeToggle` renders one icon-only button inside a `DrylTooltip`. A
click cycles `IDrylThemeService.CurrentMode` **System → Light → Dark → System**
through `SetModeAsync`; applying and persisting the mode is the job of
`DrylThemeProvider` (`F5 DrylThemeProvider.md`), which must be mounted for the
choice to reach the page.

**The icon shows the chosen mode, not the rendered one.** One SVG holds a sun
(core and rays) and a moon; a state class on the button cross-fades between
them: `is-light` shows the sun, `is-dark` the moon, `is-system` the moon plus a
small "A" (auto) badge in `--accent-grad`. Whether System currently renders dark
or light is left to `dryl.css`.

**It follows changes it did not make.** The toggle subscribes to the multicast
`OnModeChanged`, so it re-renders when anything else changes the mode — most
importantly the provider's startup sync restoring a persisted choice after the
first render.

The label names the current mode and the action a click takes, and is used three
times: as the tooltip, as the button's `aria-label`, and in a visually hidden
polite live region that announces the new mode after a click.

Usage:

```razor
<DrylColorModeToggle />
```

## Public API

Namespace: `DRYL.Components`. The toggle reads and writes `IDrylThemeService`
(`DRYL.Components.Theming`, see `_Api.md`, section *Theming*).

| Member | Type | Default | Purpose |
|---|---|---|---|
| `Size` | `int` | `18` | Width and height of the icon, in pixels. |
| `Class` | `string?` | `null` | Merged onto the button's own classes. A splatted `class` binds here case-insensitively and is merged rather than clobbering. |
| `AdditionalAttributes` | `IDictionary<string, object>?` | `null` | Captures unmatched attributes; splatted onto the button. |

## Acceptance Criteria

### Structure

- The component renders one `button` of `type="button"` inside a `DrylTooltip`.
- The button carries `color-mode-toggle`.
- `Class` is appended to the button's classes; `color-mode-toggle` and the
  state class stay.
- `AdditionalAttributes` are applied to the button.
- The button holds one `aria-hidden` SVG of width and height `Size`, with a sun
  core (`.cmt-core`), sun rays (`.cmt-rays`) and a moon (`.cmt-moon`).
- The button holds one `aria-hidden` auto badge (`.cmt-auto`) reading "A".
- After the tooltip, the component renders one `span.visually-hidden` with
  `role="status"` and `aria-live="polite"` carrying the label.

### State

- With `CurrentMode` `DrylColorMode.System`, the button carries `is-system`.
- With `CurrentMode` `DrylColorMode.Light`, the button carries `is-light`.
- With `CurrentMode` `DrylColorMode.Dark`, the button carries `is-dark`.
- With `DrylColorMode.System`, the label reads "Color mode: System — switch to
  light".
- With `DrylColorMode.Light`, the label reads "Color mode: Light — switch to
  dark".
- With `DrylColorMode.Dark`, the label reads "Color mode: Dark — follow
  system".
- The tooltip text, the `aria-label` and the live region carry the same label.

### Cycling

- A click in `DrylColorMode.System` calls `SetModeAsync(DrylColorMode.Light)`.
- A click in `DrylColorMode.Light` calls `SetModeAsync(DrylColorMode.Dark)`.
- A click in `DrylColorMode.Dark` calls `SetModeAsync(DrylColorMode.System)`.
- The button is activated by `Enter` and `Space` (native button).
- When `OnModeChanged` is raised by any caller, the toggle re-renders with the
  new state class and label.

### Appearance

- `is-light` shows the sun core and rays and hides the moon.
- `is-dark` shows the moon and hides the sun.
- `is-system` shows the moon and the auto badge and hides the sun.
- The auto badge is hidden outside `is-system`.
- The button's fill is `--glass-1`, its border `--line`, its radius `--r-sm`,
  its icon colour `--fg-muted`.
- On hover the border becomes `--line-hover`, the icon `--fg`, and the button
  gains `--shadow-sm`.
- The auto badge's fill is `--accent-grad`, its text `--on-accent`, its radius
  `--r-xs`.
- Keyboard focus shows the shared `:focus-visible` ring.
- The component branches on no colour mode and writes no mode-assuming value;
  both renditions come from the token set (`DESIGN-02`).

### Motion

- A hidden sun or moon part is rotated and scaled down as it fades.
- The sun, the moon and the auto badge fade on `--dur-med` / `--ease-out` and
  rotate or scale on `--dur-med` / `--ease-spring` when the state changes.
- Hover transitions border, colour and shadow on `--dur-fast` / `--ease-out`.
- Under `prefers-reduced-motion: reduce`, the sun, the moon and the badge swap
  without transition.

### Lifecycle

- The toggle subscribes to `OnModeChanged` on initialisation.
- On dispose it unsubscribes.

## Recorded debt

- **Labels are not localisable.** The three labels are fixed English strings;
  there is no parameter or texts object to replace them.
- **Prerender shows System.** During prerender `CurrentMode` is still
  `DrylColorMode.System`, so a user with a persisted Light or Dark choice sees
  the System icon until the provider's startup sync after the first render.
- **Silent without a provider.** Without a mounted `DrylThemeProvider`, a click
  changes `CurrentMode` and the icon, but nothing applies or persists the mode.
- **Literal values in `dryl.css` (`DESIGN-01`).** `.color-mode-toggle`'s `34px`
  box and `1px` border; `.cmt-auto`'s `-3px` offsets, `8px` font size,
  `font-weight: 700` and `2px 3px` padding; the `12px 12px` transform origin and
  the `rotate(±40deg)` / `scale(0.5|0.6)` hide transforms; the SVG's
  `stroke-width` in the markup.
- **Live region on every render.** The visually hidden status span carries the
  label at all times, so a mode change made elsewhere (e.g. the startup sync) is
  announced too, not only a click on this toggle.

## Cross-cutting evidence (`SPEC-05`)

- **Both color modes** — token-only styling in `.color-mode-toggle`; inspected on `/components/color-mode` and in the website top bar in dark and light (2026-11-20)
- **Enter/exit animation** — **state animation, no enter/exit of its own.** The
  toggle is mounted with the app bar, never conditionally, so `DrylPresence` has
  nothing to wrap (`DESIGN-12`); every state change is animated — the sun and
  moon cross-fade and turn, the auto badge pops in on `--ease-spring`
  (`DESIGN-11`). The tooltip's motion is `DrylTooltip`'s.
- **Keyboard and a11y** — a native `button`, reachable by `Tab` and activated by
  `Enter` / `Space`; icon-only, so wrapped in a `DrylTooltip` whose text matches
  the `aria-label` (`UX-05`); the label names the current mode and the next
  action; the decorative SVG and badge are `aria-hidden`; a polite live region
  announces the new mode. Unlocalisable labels are recorded above.
- **AI mode** — **no**, deliberately: the component has no `Ai` parameter. It
  switches the colour mode, a user preference no AI works on (`AI-05`). The
  catalog row agrees (AI flag `false`).
- **Demo page** — `/components/color-mode`
  (`DRYL.Website/Components/Pages/DemoColorMode.razor`): a live card with the
  toggle beside token-driven components, the follow-the-system default, runtime
  switching and modes vs. themes. The site's own toggle sits in
  `DRYL.Website/Components/Layout/TopBar.razor`.
- **`ComponentCatalog`** — `"Color Mode"` / `color-mode` in
  `DRYL.Website/Components/ComponentCatalog.cs`, group `Customization`,
  `ClassName` `"DrylColorModeToggle"`, folder `Providers`, AI flag `false`
  (`REL-04`).
- **Tests** — `tests/DRYL.Components.Tests/Theming/DrylColorModeToggleTests.cs`.
