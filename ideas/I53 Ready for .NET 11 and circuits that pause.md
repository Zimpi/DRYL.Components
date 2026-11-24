# Ready for .NET 11 and circuits that pause

## Meta
- **State:** Draft

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

Not settled. Candidate building blocks, to be cut down in the dialogue:

- **A. Target framework.** Add `net11.0` to both library projects with the
  matching `Microsoft.AspNetCore.Components.Web` reference; decide what
  happens to `net8.0` (out of support since 2026-11-10) and `net9.0`.
- **B. A reconnect modal that knows pause.** A paused state that is not the
  reconnecting spinner (or shows nothing while resume is automatic), and a
  resume-failed state with a reload/retry action.
- **C. State that survives a resume.** Decide per service and component what
  is persisted, and what is deliberately lost.
- **D. Analyzer clean-up.** Triage `BL0012`–`BL0016` on the `net11.0` build.
- **E. The website as the proof.** `DRYL.Website` moves to `net11.0`, turns
  auto-pause on, and documents the setup for consumers.

## Scope
- **In scope:** to be decided — see Open Points.
- **Out of scope:** to be decided. Proposed: `Microsoft.AspNetCore.Components.AI`
  (prerelease for all of .NET 11), client-side validation, `CacheView`.

## Impact
- **Harness:** B may need markup and motion for one or two new state blocks;
  no new token or keyframe is foreseen (the existing `reconnect-rise` and
  state-block pattern cover it) — to be confirmed. E, and any library helper
  for it, would touch `CODE-03`: the AutoPause package must stay a dependency
  of the host, never of the library.
- **Specs:** `specs/E1 Foundation/F4 DrylReconnectModal.md` (B); the specs of
  every service or component chosen under C; `harness/releasing.md` if the
  supported-framework policy changes (A).
- **Public API:** B adds text parameters (`PausedText`, `ResumeFailedText`,
  a button label) — additive. Dropping `net8.0` under A is a MAJOR change
  (`REL-01`). C may add opt-in parameters; nothing may be renamed.
- **Code:** `DRYL.Components.csproj`, `DRYL.Components.Agents.csproj`,
  `DrylReconnectModal.razor` and its stylesheet, the JS interop call sites
  (D), the scoped services (C). Risks: `[PersistentState]` exists only from
  `net10.0`, so C needs `#if` per target or stays `net10.0`+; conversation
  and canvas state can be large and persisted state is size-limited; the
  class names and the resume behaviour of `blazor.web.js` in the .NET 11 GA
  build are **not yet verified** — neither the SDK nor the Learn docs were
  reachable when this draft was written.

## Decisions
- 2026-11-24: `DRYL.Website` moves to `net11.0` and turns auto-pause on as part of this idea (Product Owner) — it is the only place the modal's pause behaviour can be seen.

## Open Points
- What exactly does the returning user see with auto-pause on: does
  `blazor.web.js` resume by itself when the tab becomes visible, and which
  classes does it set on the way? To be verified against the .NET 11 SDK.
- A: is `net8.0` dropped (MAJOR, 4.0.0) or kept alongside `net11.0` (MINOR)?
- C: which state must survive a resume, and which is acceptable to lose?
- C, sharpened: state the consumer passes in is the consumer's to persist; only state the library holds internally (unbound table sort/page, unbound active tab, theme, chat, notifications) is in question. Tech Lead proposes theme + table + tabs, the rest deliberately lost.
- The working environment has no .NET SDK and cannot download one; stage 5
  evidence (`dotnet build`, `dotnet test`) needs that solved first.
