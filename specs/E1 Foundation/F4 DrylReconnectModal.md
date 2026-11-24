# DrylReconnectModal

## Meta
- **State:** Implemented
- **Source:** code/DRYL.Components/Components/Providers/DrylReconnectModal.razor
              code/DRYL.Components/Components/Providers/DrylReconnectModal.razor.css
              code/DRYL.Components/wwwroot/js/dryl.js

## User Story

As a Blazor developer shipping a Blazor Server (interactive server) app, I want
to place one component in my layout that replaces the framework's default
reconnect overlay with one in the library's look, so that a dropped connection
reads as part of my app — with a working retry and reload — instead of a grey
default box.

## Description

`DrylReconnectModal` renders the one element the Blazor framework looks for by
id, `components-reconnect-modal`. The framework's own script then toggles state
classes on it as the circuit drops and recovers, and fills the attempt counter
spans it recognises by id. **Everything visible is pure CSS keyed on those
classes.** There is no Blazor state in the component, because while the circuit
is down no C# runs and no re-render can happen.

| Framework class | What shows |
|---|---|
| none, or `components-reconnect-hide` | nothing — the overlay is hidden and takes no pointer events |
| `components-reconnect-show`, `-retrying` | the **reconnecting** block: spinner, message, optional attempt counter |
| `components-reconnect-failed` | the **failed** block: warning glyph, message, a retry button |
| `components-reconnect-paused` | the **paused** block: pause glyph, message, a resume button — or nothing, when the browser paused the circuit itself (see below) |
| `components-reconnect-resume-failed` | the **resume-failed** block: warning glyph, message, a reload button |
| `components-reconnect-rejected` | the **rejected** block: danger glyph, message, a reload button |

**A pause the browser asked for shows nothing.** Since .NET 10 a circuit can
be paused and resumed; .NET 11 auto-pause (`AddAutoPause` in the host) does it
for a tab that has been hidden for a while and resumes when the tab is looked
at again. The framework sets `components-reconnect-paused` for that pause
exactly as for one the server requested; only its
`components-reconnect-state-changed` event tells them apart (`remote`).
`dryl.js` mirrors that onto the root as `data-pause-origin` — `client` or
`server` — and the stylesheet keeps the overlay hidden for `client`: nobody
was looking at the tab, and the resume needs no decision. A pause the server
requested, a pause on a page without `dryl.js`, and a resume that failed are
shown.

**The buttons are native on purpose.** They are plain `button` elements
with an inline `onclick` — `Blazor.reconnect()`, `Blazor.resumeCircuit()` and
`location.reload()` — not `DrylButton`s, because an `EventCallback` would never reach a dead circuit. They
borrow the `.btn` / `.btn-primary` look from `dryl.css`.

**It enters and leaves.** The overlay fades its `opacity` and `visibility` on
`--dur-med`, so it fades out as well as in; the card glides up and settles on
`--dur-slow` / `--ease-spring`; each state block rises in on its own.

**A consumer can force a state.** Because the state is just a class, a
`Class="components-reconnect-failed"` shows that state without any outage —
which is how the demo page shows its states side by side.

The component is mounted once, near the end of the layout. It has no effect
under Blazor WebAssembly, where there is no circuit to lose.

## Public API

Namespace: `DRYL.Components`.

| Member | Type | Default | Purpose |
|---|---|---|---|
| `ReconnectingText` | `string` | `"Reconnecting to the server…"` | Message of the reconnecting state. |
| `FailedText` | `string` | `"We couldn't reconnect. The server may be unavailable."` | Message of the failed state. |
| `RejectedText` | `string` | `"Your session has expired."` | Message of the rejected state. |
| `PausedText` | `string` | `"Your session was paused."` | Message of the paused state. |
| `ResumeFailedText` | `string` | `"We couldn't resume your session."` | Message of the resume-failed state. |
| `ResumeButtonText` | `string` | `"Resume"` | Label of the paused state's resume button. |
| `RetryButtonText` | `string` | `"Retry now"` | Label of the failed state's retry button. |
| `ReloadButtonText` | `string` | `"Reload page"` | Label of the reload button of the rejected and resume-failed states. |
| `ShowAttemptCounter` | `bool` | `true` | Shows the "Attempt X / Y" and "next try in Ns" lines in the reconnecting state. |
| `AttemptLabel` | `string` | `"Attempt"` | Text before the current attempt number. |
| `NextAttemptLabel` | `string` | `"Next try in"` | Text before the seconds value. |
| `SecondsSuffix` | `string` | `"s"` | Text after the seconds value. |
| `Class` | `string?` | `null` | Merged onto the overlay's `reconnect-overlay` class. |
| `AdditionalAttributes` | `IDictionary<string, object>?` | `null` | Captures unmatched attributes; splatted onto the overlay. |

## Acceptance Criteria

### Structure

- The component renders one root `div` with the id
  `components-reconnect-modal`.
- The root carries `reconnect-overlay`.
- `Class` is appended to the root's classes; `reconnect-overlay` stays.
- `AdditionalAttributes` are applied to the root.
- The root carries `role="alertdialog"` and `aria-modal="true"`.
- The root's `aria-labelledby` names the reconnecting message
  (`dryl-reconnect-label`).
- The root holds one `.reconnect-card` with five state blocks:
  `.reconnect-reconnecting`, `.reconnect-failed`, `.reconnect-paused`,
  `.reconnect-resume-failed` and `.reconnect-rejected`.
- All five blocks are always in the markup; CSS decides which one shows.

### Reconnecting state

- The reconnecting block carries `aria-live="polite"`.
- It renders an `aria-hidden` spinner, then `ReconnectingText`.
- With `ShowAttemptCounter` `true` (the default), it renders an attempt line —
  `AttemptLabel`, the `components-reconnect-current-attempt` span, a separator,
  the `components-reconnect-max-retries` span.
- With `ShowAttemptCounter` `true`, it renders a next-attempt line —
  `NextAttemptLabel`, the `components-seconds-to-next-attempt` span, then
  `SecondsSuffix`.
- With `ShowAttemptCounter` `false`, neither line is rendered.
- The attempt line is hidden while the current-attempt span is empty.
- The next-attempt line is hidden while the seconds span is empty.

### Failed and rejected states

- The failed block carries `role="alert"` and renders an `aria-hidden` warning
  glyph (`Alert` icon), `FailedText` and one button labelled `RetryButtonText`.
- The retry button is of `type="button"` and calls `Blazor.reconnect()` through
  a native `onclick`.
- The rejected block carries `role="alert"` and renders an `aria-hidden` danger
  glyph (`Server` icon), `RejectedText` and one button labelled
  `ReloadButtonText`.
- The reload button is of `type="button"` and calls `location.reload()` through
  a native `onclick`.
- Both buttons carry `.btn` and `.btn-primary`.
- The warning glyph is drawn in `--warning`, the danger glyph in `--danger`,
  each over a tint of its own colour.

### Paused and resume-failed states

- The paused block carries `role="alert"` and renders an `aria-hidden` pause
  glyph (`Pause` icon), `PausedText` and one button labelled
  `ResumeButtonText`.
- The resume button is of `type="button"` and calls `Blazor.resumeCircuit()`
  through a native `onclick`.
- When `Blazor.resumeCircuit()` resolves `false`, the page reloads.
- When `Blazor.resumeCircuit()` rejects, the root's
  `components-reconnect-paused` is replaced by
  `components-reconnect-resume-failed`.
- The resume-failed block carries `role="alert"` and renders an `aria-hidden`
  warning glyph (`Alert` icon), `ResumeFailedText` and one button labelled
  `ReloadButtonText` that calls `location.reload()` through a native `onclick`.
- Both buttons carry `.btn` and `.btn-primary`.
- The pause glyph is drawn in `--accent-a` over `--accent-soft`, bordered
  `--line-strong`.
- On a `components-reconnect-state-changed` event with state `paused`,
  `dryl.js` sets `data-pause-origin` on the event's target: `server` when the
  event's `remote` is true, `client` otherwise.
- On any other state of that event, `dryl.js` removes `data-pause-origin`.

### State visibility

- Without a state class, and with `components-reconnect-hide`, the overlay is
  invisible and takes no pointer events.
- With `components-reconnect-show` or `components-reconnect-retrying`, the
  overlay is visible and shows only the reconnecting block.
- With `components-reconnect-paused` and `data-pause-origin="client"`, the
  overlay is invisible and takes no pointer events.
- With `components-reconnect-paused` and any other or no `data-pause-origin`,
  the overlay is visible and shows only the paused block.
- With `components-reconnect-resume-failed`, the overlay is visible and shows
  only the resume-failed block.
- With `components-reconnect-failed`, the overlay is visible and shows only the
  failed block.
- With `components-reconnect-rejected`, the overlay is visible and shows only
  the rejected block.
- A state class supplied through `Class` shows that state without the
  framework.

### Appearance

- The overlay covers the viewport and centres the card.
- The overlay stacks on `--z-reconnect`.
- The overlay's fill is `--backdrop`.
- The overlay's `backdrop-filter` is `--glass-fx-float`: it lies over the whole page, so it takes the floating glass tier.
- The card's fill is `--panel-grad`, its border `--line-strong`, its radius
  `--r-lg` and its shadow `--shadow-lg` plus an `--accent-a` glow.
- The card's text is `--fg`, centred.
- The spinner ring is drawn in `--accent-soft` with `--accent-a` and
  `--accent-b` leading edges; its core is `--accent-grad` with `--glow-accent`.
- The messages read `--fg`; the attempt line reads `--fg-muted`; the
  next-attempt line reads `--fg-dim`; both use tabular numerals.
- The component branches on no colour mode and writes no mode-assuming value;
  both modes come from the token set (`DESIGN-02`).

### Motion

- The overlay fades `opacity` and `visibility` on `--dur-med` / `--ease-out`,
  entering and leaving.
- The card glides from a lowered, slightly smaller position to rest on
  `--dur-slow` / `--ease-spring` as the overlay is revealed.
- A state block rises in on `reconnect-rise`, `--dur-med` / `--ease-spring`,
  when it is shown.
- The spinner ring rotates continuously (`reconnect-spin`).
- The spinner core breathes continuously on `--ease-in-out`
  (`reconnect-breathe`).
- Under `prefers-reduced-motion: reduce`, the card does not glide, the state
  blocks do not rise and the core does not breathe.
- Under `prefers-reduced-motion: reduce`, the spinner ring keeps rotating, more
  slowly, as the one signal that work is still going on.
- Under `prefers-reduced-motion: reduce`, the overlay transitions `opacity`
  only.

## Recorded debt

- **The floating card is not floating glass (`DESIGN-06`).** `.reconnect-card`
  floats over the page but carries `--panel-grad` with no
  `--glass-fx-float`; the frost sits on the full-screen overlay instead.
- **A browser-side resume in flight shows nothing.** Between the tab becoming
  visible and the framework's `hide`, the page does not respond and nothing
  says so. Measured on the showcase at about 30 ms; a slow resume would be a
  silent wait.
- **The accessible name never changes.** `aria-labelledby` always names the
  reconnecting message, so the dialog is announced as "Reconnecting…" in the
  failed and rejected states too.
- **No focus handling (`UX-01`).** Nothing moves focus into the dialog or traps
  it there; `aria-modal="true"` does not make the page behind inert, so `Tab`
  continues through the dead page before reaching the retry or reload button.
- **Its own spinner (`DESIGN-13`).** The reconnecting spinner is a one-off
  (`.reconnect-spinner-ring`, `.reconnect-spinner-core`) rather than the shared
  `.spinner` / `DrylSpinner`, and unlike the shared one it keeps rotating under
  reduced motion.
- **No exit for the overlay under reduced motion.** With only `opacity`
  transitioned, `visibility` flips at once on `hide`, so the overlay vanishes
  without fading.
- **Literal values in `DrylReconnectModal.razor.css` (`DESIGN-01`).** The card's
  `400px` width, `1px` border and `80px` glow at a 25 % mix; the `14px` / `0.97`
  entry offset and the `8px` rise; the `46px` spinner and glyph, `3px` ring and
  `10px` core; the `12 %` / `32 %` glyph tints; `font-size` 14 px / 12 px,
  `font-weight: 600`, `line-height: 1.5`; the separator's `0.5` opacity and
  `2px` margin; and the continuous tempos `0.9s`, `1.6s` and `1.8s`.
- **Duplicate ids if mounted twice.** The root and counter spans use fixed ids,
  so a second instance — as on the demo page — repeats them.
- **Visibility is untested in bUnit.** Which block shows is CSS; the tests pin
  the markup only. The pause behaviour was measured in a browser (below).

## Cross-cutting evidence (`SPEC-05`)

- **Both color modes** — mode tokens only; inspected on `/components/reconnect-modal` in dark and light (2026-11-20)
- **Enter/exit animation** — **yes, without `DrylPresence`.** The component
  never mounts or unmounts conditionally — it is always in the DOM — and the
  framework's class toggling drives a CSS fade in *and* out (`visibility`
  transitioned alongside `opacity`), a card glide and a per-state rise.
  A browser-side pause and resume was driven on the showcase under .NET 11
  auto-pause (2026-11-24): the overlay's computed `opacity` stayed `0` from
  `paused` through `hide`.
  `DrylPresence` could not drive it anyway: no C# runs while the circuit is down.
  The reduced-motion exit gap is recorded above.
- **Keyboard and a11y** — `role="alertdialog"`, `aria-modal`, a polite live
  region for the reconnecting state, `role="alert"` on the failed and rejected
  blocks, decorative glyphs and spinner `aria-hidden`, native buttons reachable
  by `Tab` and activated by `Enter` / `Space`. The missing focus handling and
  the static accessible name are recorded above.
- **AI mode** — **no**, deliberately: the component has no `Ai` parameter. It
  reports the state of the connection to the server, not work an AI is doing,
  and while it shows no C# runs that could set an AI state (`AI-05`). The
  catalog row agrees (AI flag `false`).
- **Demo page** — `/components/reconnect-modal`
  (`DRYL.Website/Components/Pages/DemoReconnectModal.razor`), a hand-built
  page rather than `DemoExample`s: it renders three extra instances inside
  frames with `Class` forcing `components-reconnect-show`, `-failed`,
  `-paused`, `-resume-failed` and `-rejected`. The site itself mounts the real one in
  `DRYL.Website/Components/App.razor`.
- **`ComponentCatalog`** — `"Reconnect Modal"` / `reconnect-modal` in
  `DRYL.Website/Components/ComponentCatalog.cs`, group `Surfaces`, `ClassName`
  `"DrylReconnectModal"`, folder `Providers`, AI flag `false` (`REL-04`).
- **Tests** — `tests/DRYL.Components.Tests/DrylReconnectModalTests.cs`.
