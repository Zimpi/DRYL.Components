# Ready for .NET 11 and circuits that pause

## Meta
- **State:** Adopted
- **Specs:** [`specs/E1 Foundation/F4 DrylReconnectModal.md`](../specs/E1%20Foundation/F4%20DrylReconnectModal.md),
  [`specs/E1 Foundation/_Api.md`](../specs/E1%20Foundation/_Api.md),
  [`specs/E1 Foundation/_Interop.md`](../specs/E1%20Foundation/_Interop.md)

## Problem

The Product Owner asked on 2026-11-24 to make the library ".NET 11 ready",
with the circuit behaviour that arrived in .NET 11 as the main interest, and
the expectation that the reconnect modal no longer appears after a tab was
left alone for a longer time.

What happens today: a Blazor Server tab that stays hidden long enough loses
its circuit. On return the user gets `DrylReconnectModal` in its reconnecting
state and then, once the server has dropped the circuit, the rejected state
with a reload button.

What .NET 11 actually adds (read from the Preview 7 and RC1 release notes in
`dotnet/core`; the Learn pages were not reachable from the working session):

- There is no "auto" circuit mode. There is **auto-pause**: an opt-in package,
  `Microsoft.AspNetCore.Components.Server.AutoPause`, that the **host app**
  turns on through `WithBrowserOptions(o => o.AddAutoPause(...))` on
  `AddInteractiveServerRenderMode()`. A tab hidden for `HiddenDelay` (default
  two minutes) pauses its circuit; the pause is deferred while a text input is
  dirty, media plays, or a JS interop call, stream or render is in flight.
- It rides on the pause/resume mechanism of .NET 10. A resumed circuit is
  **rebuilt from persisted state**; anything a component or a scoped service
  holds only in memory starts from scratch.
- `Circuit.RequestCircuitPauseAsync` lets the server pause on demand, and a
  client-side `onCircuitPausing` circuit handler lets a script defer a pause.
- Analyzers `BL0012`–`BL0016` are warnings by default. `BL0016` flags JS
  interop calls outside `try`/`catch`, which a component library has many of.

So the library cannot switch the behaviour on, and three things in it stand
in the way of a host that does:

1. `DrylReconnectModal` shows the reconnecting spinner for
   `components-reconnect-paused`, offers no resume, and has no rule for
   `components-reconnect-resume-failed` — a failed resume leaves a dead page
   with nothing on screen. Both are already recorded debt in
   `specs/E1 Foundation/F4 DrylReconnectModal.md`.
2. The library persists nothing. Its scoped services (`DrylThemeService`,
   `DrylToastService`, `DrylNotificationService`, `DrylDialogService`,
   `DrylAiActivityService`, `CommandRegistry`, `DrylAgentRunner`,
   `DrylVoiceRunner`) and its stateful components (`DrylChat`, `DrylCanvas`,
   `DrylTable` sort/page, `DrylTabs`, …) come back empty after a resume.
3. Both projects target `net8.0;net9.0;net10.0`; there is no `net11.0` asset.

Target role: the Blazor developer who runs DRYL in a Blazor Server app on
.NET 11 and turns auto-pause on; and that app's user returning to a tab.

## Solution Idea

- **A. Target framework.** `net11.0` joins `net8.0;net9.0;net10.0` in both
  library projects, with `Microsoft.AspNetCore.Components.Web` 11.0.0.
- **B. A reconnect modal that knows pause.** A pause the browser started
  (auto-pause) shows nothing; a pause the server requested shows a paused
  block with a resume button; a failed resume shows a block with a reload
  button.
- **C. State that survives a resume.** No new library mechanism. What the
  library already persists client-side comes back by itself (`DrylTable` with
  `PersistStateKey`, the colour mode of `DrylThemeProvider`); everything
  bindable is the consumer's to mark `[PersistentState]`. A guide on the
  showcase says so.
- **D. Analyzers.** `BL0016` is switched off for the two library projects,
  with the reason in the project file.
- **E. The website as the proof.** `DRYL.Website` runs on `net11.0` with
  auto-pause on, and its reconnect-modal page carries the guide.

## Scope
- **In scope:** A–E above.
- **Out of scope:** dropping `net8.0` (a MAJOR change, left for the next
  major); a consumer-facing persistence helper; `[PersistentState]` inside
  library components (an unbound active tab, a chat transcript, a canvas,
  toasts and open dialogs start fresh after a resume); the six `BL0012`
  findings ("`StateHasChanged` is unnecessary") in `DrylCalendar`,
  `DrylCodeBlock`, `DrylTable`, `DrylCommandPalette` and `DrylMenu`, which
  stay visible as warnings until each is judged against its spec;
  `Microsoft.AspNetCore.Components.AI`, client-side validation, `CacheView`.

## Impact
- **Harness:** none. No new token, keyframe, `AiState` or dependency; the two
  new blocks reuse the state-block pattern, `reconnect-rise` and the glyph
  shape. The AutoPause package is a dependency of the showcase only
  (`CODE-03` untouched).
- **Specs:** `F4 DrylReconnectModal`, and the reconnect sections of
  `_Api.md` and `_Interop.md` in `specs/E1 Foundation/`.
- **Public API:** additive — `PausedText`, `ResumeFailedText`,
  `ResumeButtonText` on `DrylReconnectModal`. MINOR (`REL-01`).
- **Code:** both project files, `DrylReconnectModal.razor` and its
  stylesheet, one capture listener in `dryl.js`, the two CI workflows.

## Evidence gathered in the dialogue (2026-11-24, .NET SDK 11.0.100)

- Read from `blazor.web.js` 11.0.0: every graceful pause calls the custom
  display's `show()` and then sets `components-reconnect-paused`; the
  `components-reconnect-state-changed` event carries `remote`. A failed
  resume sets `components-reconnect-resume-failed`.
- Read from the AutoPause package's script: it calls `Blazor.pauseCircuit()`
  after the hidden delay and `Blazor.resumeCircuit()` when the tab is visible
  again.
- Measured on the showcase with a 3 s delay: before the change the overlay
  was at full opacity when the tab came back; after it, the overlay's opacity
  stayed 0 from `paused` to `hide` (about 30 ms), and the page took a click
  afterwards. A tab selected before the pause was back on its first tab after
  the resume — unbound component state does not survive.
- A resume after a server restart succeeded: a browser-side pause keeps its
  state in the browser.

## Decisions
- 2026-11-24: `DRYL.Website` moves to `net11.0` and turns auto-pause on as part of this idea (Product Owner) — it is the only place the modal's pause behaviour can be seen.
- 2026-11-24: the library supports .NET 11 — `net11.0` joins the target frameworks of both projects (Product Owner).
- 2026-11-24: the library part stays small; persistence is wanted as "made easier", not as a large feature (Product Owner). What that means concretely is open — see Open Points.
- 2026-11-24: `net8.0` stays; `net11.0` is added as a MINOR release (Product Owner, on the Tech Lead's recommendation).
- 2026-11-24: persistence is "guide plus the library's own internal state" (Product Owner). Finding during implementation: the table and the colour mode already persist client-side and survive a resume; the Tech Lead did not add `[PersistentState]` to library components, because its keys are derived from the component's position and two instances under one parent were not shown to be safe. The unbound active tab is therefore not restored — a deviation from the agreed scope, reported to the Product Owner.
- 2026-11-24: a browser-side pause shows no overlay (Tech Lead, following the Product Owner's stated goal that the modal should not appear after a long absence).
- 2026-11-24: `BL0016` is suppressed in the library; `BL0012` is left as warnings (Tech Lead).
- 2026-11-24: the Product Owner released the work for implementation ("Setz das alles um").

## Open Points
