# Changelog

All notable changes to DRYL are documented in this file.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).
Versioning follows [Semantic Versioning](https://semver.org/).

Version bump guide:
- **MAJOR** (1.x.x) — Breaking changes to the public API
- **MINOR** (x.1.x) — New components or features, backwards-compatible
- **PATCH** (x.x.1) — Bug fixes, docs, visual tweaks with no API change

---

## [Unreleased]

## [2.23.0] — 2026-08-12

### Changed
- **Release note on the version bump** — This release's only fix is a bug fix, which `REL-01` would bump as a PATCH. It is a MINOR because a public parameter arrives in the same unreleased version: `DrylSplitButton` gains `Aura`, the pin for the AI aura variant that every other AI-aware component already exposes. The two changes were written as one unit and nothing is published between them, so one bump covers both rather than cutting two releases for one release's worth of work. Recorded here because the `Fixed`-only block below does not show it

### Fixed
- `DrylSplitButton` — The caret segment now shows a tooltip on hover and on keyboard focus. It is an icon-only button, and `UX-05` requires every button that renders only an icon to be wrapped in a `DrylTooltip` naming its action; the caret had an `aria-label` but no tooltip, so a sighted mouse user got no hint at all what the chevron opened. The bubble's text is the component's existing `MenuAriaLabel` parameter (default `More actions`), so there is no new parameter to set and the two cannot drift apart — set `MenuAriaLabel` and you set both. **The accessible name is unchanged**: the caret keeps the same `aria-label` it had, and the tooltip bubble is a decorative `aria-hidden` portal, so screen readers announce exactly what they announced before. The joined segment look is unchanged too — the caret's rules in `dryl.css` reach it through a descendant selector, so the tooltip's wrapper `span` sits between them harmlessly, and focus still returns to the caret when the menu closes. One small new behaviour comes with it: the tooltip is attached to the wrapper rather than to the button, so a **disabled** caret can now show the bubble too, in browsers that retarget pointer events over a disabled control to its parent. Where a disabled caret previously gave no hint at all, hovering one may now say what it would open

## [2.22.1] — 2026-08-12

### Fixed
- `DrylAuraElements` — The AI aura layers (`.ai-aura-ring`, `.ai-aura-comet`, `.ai-aura-glow`, `.ai-aura-wash`) now carry `aria-hidden="true"`. `UX-07` requires a purely decorative moving indicator to be hidden from assistive technology and names the AI aura as one of its examples; the gliding indicators `.tab-ink` and `.ws-ink` already complied, the aura did not. Every component that embeds `DrylAuraElements` benefits, since the markup is shared. Nothing usable leaves the accessibility tree: the layers are empty, not focusable, take no pointer events, and the component accepts no `ChildContent`, so no host can place content inside them
- `DrylCard`, `DrylMarkdown`, `DrylImage`, `DrylCommandPalette`, `DrylNotifications` — Same fix applied to their aura layers. These five emit the `.ai-aura*` markup inline instead of embedding `DrylAuraElements`, so the shared fix above did not reach them; they were found by inspecting the rendered page rather than the component source. Their layers are likewise empty and decorative

### Changed
- `DrylCommandPalette`, `DrylAlert` — **User-visible labels are now English.** `REL-02` requires every consumer-facing artefact to be in English; several strings had been left in German. Changed: the palette's AI pill (`Denkt…` → `Thinking…`), its back button tooltip and `aria-label` (`Zurück` → `Back`), its argument-fill confirm button (`Ausführen` → `Run`) and its destructive-command confirmation dialog (`Aktion bestätigen` / `Ausführen` / `Abbrechen` → `Confirm action` / `Run` / `Cancel`); and the alert's dismiss button `aria-label` (`Benachrichtigung schließen` → `Dismiss notification`). **If your app relied on these German labels, they change with this release** — the palette dialog and the alert dismiss button are the visible ones, and the two `aria-label`s change what screen readers announce
- XML doc comments and source comments — `DrylAlert`'s parameter documentation was written entirely in German and shipped that way as IntelliSense; it is now English, as are the German comments in `dryl.css`, `dryl.js`, `DrylTooltip`'s usage block, `DrylToastProvider` and the `AddDrylCanvasAction` code sample. No behaviour change

## [2.22.0] — 2026-08-12

### Added
- `DrylCanvas` — New `Ai` parameter (`AiState`, defaults to `AiState.None`), the opt-in that turns AI styling on, replacing `State` (see Deprecated). An artifact tree renders in full with `AiState.None` — the parameter only adds the build line and the waiting skeletons — so it is a switch, and `AI-03` requires it to be called `Ai`
- `DrylToolCallGroup` — New `Ai` parameter (`AiState`, defaults to `AiState.None`), the opt-in that turns AI styling on, replacing `State` (see Deprecated). Same reasoning as `DrylToolCall` below: a collapsed group still shows its count and its status with `AiState.None`, so the parameter is a switch and `AI-03` requires it to be called `Ai`
- `DrylToolCall` — New `Ai` parameter (`AiState`, defaults to `AiState.None`), the opt-in that turns AI styling on. It replaces `State`, which stays as an obsolete alias so nothing breaks — see Deprecated. `AI-03` requires the opt-in parameter to be called `Ai` wherever it is a switch on a component that would otherwise render as an ordinary one, and a tool-call card is exactly that: it shows the tool name, its status and its arguments whether or not AI styling is on. The reasoning, and why five other AI components keep their own parameter names, is in `ideas/I1 AI parameter naming for AI-native components.md`

### Deprecated
- `DrylCanvas.State` — Renamed to `Ai`, on the same terms as the two below: the alias delegates, still works, warns at the call site, and goes away in `3.0.0`
- `DrylToolCallGroup.State` — Renamed to `Ai`, on the same terms as `DrylToolCall.State` below: the alias delegates, still works, warns at the call site, and goes away in `3.0.0`
- `DrylToolCall.State` — Renamed to `Ai`. Setting `State` still works and still does exactly what it did; it delegates to `Ai` and raises `CS0618` at the call site naming the replacement. The alias is removed in the next planned `3.0.0`. Note that the compiler reports the generated `*_razor.g.cs` path rather than the `.razor` line, so search for the component name in your own markup

### Changed
- **Release note on the version bump** — `2.22.0` was bumped in the first of three commits that each add one of the new `Ai` parameters, rather than once per commit as `REL-01` reads literally. The three were written and pushed as one unit and nothing was published in between, so the released package contains all three parameters; recorded here because the individual commits do not show it
- `AI-03` — The rule now governs the **opt-in** parameter rather than every `AiState` parameter, and its test asks what the parameter is: a switch that turns AI styling on for a component that would otherwise render as an ordinary one, or the component's own content, settle state or broadcast override? A switch is named `Ai` and defaults to `AiState.None`; everything else keeps its own name and default. Five components that the rule previously counted as violations — `DrylAiIndicator`, `DrylAiScope`, `DrylAiStream`, `DrylAiGenerate`, `DrylAiBuild` — are named individually with their reason and are no longer in breach. `Enforced` moves from `grep` to `grep` + `review`, since "is an opt-in" is not greppable. No consumer-facing behaviour changes; this entry is here because the rule decides what the specs record
- **First three component specs** — `specs/E3 AI/` gains `F1 DrylToolCall`, `F2 DrylToolCallGroup` and the split `F3 DrylCanvas/`, plus the category's `_Api.md` parameter contract. Phase C stands at 3 of 127 components covered
- `DrylAgentToolCalls` (Agents 0.17.4) — Sets `Ai` instead of `State` on the `DrylToolCallGroup` it renders and on the `DrylToolCall` cards inside it. Internal call sites only, no API of this package changes; the library does not use its own deprecated alias
- `DrylAiCanvas` (Agents 0.17.4) — Sets `Ai` instead of `State` on the `DrylCanvas` it wraps. Internal call site only; the `State` it sets on its own `DrylAiIndicator` is unchanged, because that parameter is the displayed value and not an opt-in

### Fixed
- `DrylToolCall`, `DrylToolCallGroup`, `DrylExpansion` — The collapsed body of all three kept its content in the tab order. Each animates on a `grid-template-rows` track and therefore leaves its body in the DOM; `aria-hidden` marked it hidden for assistive tech, but neither that nor `overflow: hidden` removes an element from the tab order. A keyboard user tabbing out of the header landed on invisible controls — the copy button of a collapsed code block, or the header of a card inside a collapsed group — and a screen reader had focus on an element its accessibility tree did not contain (WCAG 4.1.2). All three now carry `inert` while collapsed, which is plain HTML and needs no JavaScript. The gap existed in `DrylToolCallGroup` and `DrylExpansion` before this release and was widened in `DrylToolCall` by the disclosure fix below; found by review
- `DrylToolCall` — The collapsible body appeared and vanished with no transition: it sat behind a bare `@if`, which `DESIGN-12` does not allow for a visible surface. It now animates open and closed on the `grid-template-rows` `0fr` → `1fr` disclosure its own neighbour `DrylToolCallGroup` already used, honours `prefers-reduced-motion`, and stays in the DOM while collapsed — which preserves the scroll position of a long code block across a collapse. Found while writing the component's spec

## [2.21.0] — 2026-08-11

The motion vocabulary gains the words it was missing. `DESIGN-10` fixes three durations and three easings, but six one-shot choreographies ran past its 600 ms ceiling and three delays were bare literals — so the rule forbade the value and offered no token, and every author wrote the literal anyway.

### Added
- **Three motion tokens** — `--dur-choreo` (900ms) for multi-step one-shot choreography, deliberately outside the transition scale and carrying a scope comment, as `--ease-viscous` already does; `--delay-short` (200ms) for a beat's offset so two things do not land at once; `--delay-long` (800ms) for a hold before something retires itself. All three are mode-neutral and consumer-overridable like the rest of the token surface — see `harness/theming.md`

### Changed
- **Motion literals replaced by tokens across `dryl.css`** — Eleven call sites and five bare `ease-in-out` keywords. Five choreographies converge on `--dur-choreo` and their timing shifts visibly: the AI table-row flash is 700ms quicker (1600 → 900), the toast shine 400ms quicker, the progress bar 180ms quicker, `ai-comet-retire` 200ms quicker, and `ai-generated-lift` 180ms *slower*. `ai-aura-bloom` was already at 900ms and is unchanged; `.fade-in`, `.stagger` and the toast icon pop shift by under 100ms. The five ambient animations (`drift-a/b/c`, `shimmer`, `skel`) keep their free rhythm and only swap the bare `ease-in-out` keyword for `var(--ease-in-out)` — not the same curve. Consumers who override the motion tokens in their own theme control all of this
- `DrylImage` — Dropped the unreachable `2000ms` fallback in `var(--img-blur-dur, 2000ms)`. The variable is set inline under exactly the condition that adds the class consuming it (`Ai == AiState.Streaming && !Progress.HasValue`), so the fallback could never apply. The public `BlurDuration` parameter keeps its `2000` default and its behaviour

## [2.20.2] — 2026-08-11

### Added
- `DrylVoiceRun` (Agents 0.17.0) — New `ShouldContinue` and `MaxAutoContinuations` parameters: a voice session can now carry on working by itself. A realtime session has no agent loop — the protocol only continues a turn that carried a tool call, so a turn that was *only* speech ended the model's part and nothing ever started another one. An assistant working through a plan would announce "let me go and check", stop talking, and sit there until the user asked whether it was still working. `ShouldContinue` is asked after every tool-less turn and, returning true, sends the model back to work; wire it to whatever knows there is work left (an open task list, a queue). It is null by default, so a plain conversation is unchanged. `MaxAutoContinuations` (6) caps consecutive *fruitless* turns — running a tool or the user speaking resets the budget, so it bounds a model talking to itself without limiting one that is making progress.

### Fixed
- `DrylVoiceRun` (Agents 0.17.1) — A voice session went mute mid-plan whenever the realtime API rate-limited a turn. A refused turn arrives as `response.done` with `status: "failed"`, an empty output and an all-zero usage block: nothing ran, no audio, no tokens, no tool. It was nevertheless counted as a finished turn, so the next continuation was requested immediately — with no wait at all, against a limit measured per minute. Every attempt hit the same refusal, `MaxAutoContinuations` was spent in milliseconds on turns that never happened, and the assistant fell silent with its plan half-done until the user prompted it again. A refused turn is now re-requested after the delay the API itself states ("Please try again in 1.911s"), up to five attempts, and no longer consumes the continuation budget. Failures that will not pass by waiting are reported instead of retried, and the floor returns to the user once the attempts are used up. Interrupting cancels a pending retry.
- `DrylVoiceRun` (Agents 0.17.1) — Every continuation decision can now be traced: set `window.__drylVoiceDebug = true` before starting a session and the data channel reports each turn's outcome and why it did or did not continue. Off by default and free when off. Diagnosing a stalled session previously meant reading a channel that reported nothing at all.
- `DrylVoiceRun` (Agents 0.17.0) — An `error` event on the data channel tore down the whole session, including for complaints a live session survives. The common one is a `response.create` that raced the user starting to speak — recoverable, and indistinguishable from a dead session at the point it was handled, so a working conversation was thrown away for it. Non-fatal errors are now logged and the session continues; a session that really is gone still closes, because the transport reports that separately through `oniceconnectionstatechange`. Tool results and continuation requests additionally carry a turn guard, so neither is sent on top of a response the user's own speech already started.

### Changed
- `DRYL.Components.Agents` (Agents 0.16.1) — Minimum `Microsoft.Agents.AI` raised from 1.13.0 to 1.15.0. No API of this package changes; consumers pinned to 1.13.x need to move up with it.
- `DESIGN-01`, `DESIGN-10`, `CODE-01` — Rule scope corrected after triaging the harness's pre-existing violations. `DESIGN-01` exempts alpha-channel contexts (`mask`, `clip-path`), where a color literal is a stencil rather than a design choice. `DESIGN-10` now governs transitions and one-shot animations only; continuous `infinite` motion (spinners, shimmers, ambient drift) keeps its own rhythm and may use `linear`, while the easing tokens still bind. `CODE-01` applies to public components; internal building blocks under `Internal/` deliberately carry no `Dryl` prefix. No component code changed
- `prototype/` and `samples/` — Removed. The prototype was a proof-of-concept reference the library has long outgrown; `samples/` held no tracked demo pages. Demos live in `DRYL.Website` and are surfaced through its `ComponentCatalog`. Consumers are unaffected — neither directory shipped in either package
- Repository layout — Library projects moved to `code/`, the rules split out of `CLAUDE.md` into `harness/` with stable rule IDs, and `specs/` + `ideas/` added for spec-driven development. Consumers are unaffected: package IDs, assembly names and the `_content/DRYL.Components/…` asset paths are unchanged
- Spec structure — The fifteen spec categories are fixed in `SPEC-02` and scaffolded under `specs/`, and `scripts/check-spec-coverage.mjs` now enforces that every `Dryl*.razor` is claimed by exactly one spec. It reports `x/127 components covered`; the specs themselves are written per category in the phase that follows. Documentation and tooling only — no library code is touched, so no version is bumped
- Component folder layout — Eighteen files moved so that a component's folder matches what it is. `DrylNavGroup`, `DrylNavLink`, `DrylTabs`, `DrylTab`, `DrylStepper` and `DrylStep` left `Components/Layout/` for `Components/Navigation/`, where someone looking for tabs looks first. The five pieces of mount-once plumbing — `DrylThemeProvider`, `DrylToastProvider`, `DrylPresence`, `DrylReconnectModal`, `DrylColorModeToggle` — left `Components/Surfaces/` for a new `Components/Providers/`; they were never surfaces. `DrylDialog` and `DrylDialogProvider` joined `DrylAlertDialog` and `DrylConfirmDialog` in `Dialogs/`, so one feature no longer lives in two folders. **Consumers are unaffected:** every component declares its own `@namespace`, so no `using` changes, no type moves, no renamed parameters — the folder was never part of the public API. The timing is deliberate: the per-component specs land next and record concrete source paths, which would otherwise have to be rewritten afterwards

## [2.20.1] — 2026-07-30

Two bugs breaking the same promise: a surface belongs to the theme, and a panel belongs over the page — not inside the card it was born in.

### Fixed
- `DrylMultiSelect` — The listbox panel was clipped by the surrounding card. It was the last dropdown in the library still sitting on its own `position: absolute` panel, while `DrylSelect` / `DrylAutocomplete` / `DrylDatePicker` / `DrylTimePicker` had long since moved to `DrylPopover`. So any ancestor with `overflow` or `backdrop-filter` clipped it — on the docs site, the `overflow: hidden` example card. It now sits on `DrylPopover`, is portaled to `<body>` and positions itself against the viewport. It inherits the primitive's entrance animation along the way, plus `aria-activedescendant` and `dryl.keynav` (arrow keys no longer scroll the page while you walk the options).
- `DrylPopover` — `Block="true"` stretched the anchor but not the trigger's content. Whatever the consumer puts in `TriggerContent` is a flex item at `flex: 0 1 auto`, so it sat at its intrinsic width: a block dropdown rendered narrower than the column it was placed in. Affected `DrylAutocomplete`, `DrylDatePicker`, `DrylTimePicker` — and would have hit `DrylMultiSelect` on its way over. The rule needs `::deep`, because the trigger's children carry the *consumer's* scope attribute, not the popover's.
- **Theme awareness** — Replaced 46 hardcoded accent and semantic colours across 15 components: they were nailed down as `rgba(124, 92, 255, …)` / `rgba(248, 113, 113, …)` and stayed violet or red while `DrylThemeProvider` recoloured the rest of the surface. Now uniformly `color-mix(in srgb, var(--accent-a|--accent-b|--ai-a|--ai-b|--danger|--success|--warning) N%, transparent)`. Affected: `DrylMultiSelect`, `DrylSelect`, `DrylRating`, `DrylChipInput`, `DrylInputOtp`, `DrylInputPassword`, `DrylSlider`, `DrylValidationSummary`, `DrylSkeleton`, `DrylSpinner`, `DrylNotifications`, `DrylImage`, `DrylTimelineItem`, `DrylStepper`, `DrylCommandPalette`. AI surfaces read `--ai-a` / `--ai-b` rather than the accent tokens — the same values, but the contract the `.ai-aura` primitives use.

## [2.20.0] — 2026-07-29

The assistant gets a voice. A dock that used to be typed into can now be spoken to — same tools, same history, and without a single audio frame touching the server.

### Added
- `DrylVoiceRunner` / `DrylVoiceRun` / `DrylVoiceOptions` (Agents 0.16.0) — **New:** voice sessions over the OpenAI Realtime API. The browser holds a WebRTC connection straight to the model (microphone in, voice out, one data channel for the events); the server only mints the short-lived `ek_…` token and executes every tool call. The API key never leaves the server, and because the whole session is baked into the token, the browser can change neither the instructions nor the model nor the tool list. Configuration is C#-only — `Model`, `Instructions`, `Voice`, `Speed`, `TurnDetection`, `ReasoningEffort`, `TranscriptionModel`, `Language`, `Tools`, `IdleTimeout`, `MaxDuration`, `BaseUrl`, `SafetyIdentifier`. There is deliberately no settings UI.
- `DrylVoiceOrb` (Agents 0.16.0) — **New component:** the visible voice. Built from the existing `.ai-aura` primitives, so without a single new colour or duration; the five `AiState` values carry it like any other AI surface. The volume level stays in the browser and writes a CSS variable directly onto the element — a level travelling through interop would be sixty render passes per second for a decoration.
- `DrylCanvasDock` (Agents 0.16.0) — New `Voice` and `VoiceLabel` parameters. Given a voice run, the head grows a microphone button; while the session is live the dock becomes the voice panel: composer, suggestions and context chip step aside, and the orb, the last spoken line and an end button take their place. Without `Voice` nothing about the dock changes.
- `DrylIcon` — New `Microphone` icon (lucide: mic).

### Fixed
- `DrylCanvasDock` (Agents 0.16.0) — The dock surface was see-through. It inherited a 4% white gradient with `backdrop-filter: none` from `.glass-card` — the treatment for surfaces *in* the flow — but the dock floats over the canvas, and you could read the page straight through it. It now carries `--panel-float` and a real `--glass-fx-float` (rule 2.3). Only noticed once the voice takeover made the surface large and mostly empty.

## [2.19.0] — 2026-07-27

Two things invisible on fast machines and painful on every other one: typed characters vanished back out of input fields, and the library charged GPU work for effects nobody could see.

### Changed
- **Glass surfaces** — Frost is now charged only where it can be seen. A surface that floats over scrolling content (topbar, sidebar, popover, menu, tooltip, toast, dialog) keeps its real `backdrop-filter`; a surface in the flow (`.glass`, `.glass-card`, `.expansion`, `.alert`, `.btn-secondary`) keeps the translucent fill and drops the blur, because the page's own smooth background behind it is what the blur was blurring. Measured on the component overview: 95 frosted surfaces down to 5, GPU draw per frame 2.77 ms → 1.11 ms, with an average pixel difference of 0.84 of 255. Opt back in per app with `--glass-fx-flow`.
- **Opaque surfaces** — Removed the `backdrop-filter` from surfaces whose own background is opaque and which therefore could never show it: the table's bulk bar (on `--bg-2`) and the sticky table header (`--panel-sticky`, 0.92). A wide table charged one wasted backdrop sample per header cell; the tables page went 1.98 ms → 1.17 ms per frame with no visual change at all.
- **Floating panels are glass again** — Dialog, command palette, menu, popover, select, multi-select, autocomplete, date and time panels were opaque or near-opaque (0.95 / 0.97 / fully solid). The frost above them had 3-5% to work with, so they read as solid blocks — the dialog carried no `backdrop-filter` at all. They now sit on genuinely translucent fills (`--panel-grad`, `--panel-grad-strong`, the new `--panel-float`) with a real `--glass-fx-float` behind them. This is the tier that earns its cost: a dialog open over a page measures 0.76 → 0.97 ms per frame, and only while it is open.
- `AiState.Generated` — Now a genuine one-shot: the aura plays its bloom, holds long enough to be read, then takes itself off the surface. It used to be a resting place, so a surface that had been generated into kept a breathing halo alive for the life of the page, and a host had to remember to hand back to `None` to stop it. Hosts no longer need to — setting `Generated` and walking away is the whole contract now.

### Added
- `--glass-fx-flow` — New token controlling the frosting of in-flow surfaces. Defaults to `none`; set it to `blur(var(--glass-blur)) saturate(140%)` to give every card its backdrop blur back.
- `--glass-fx-float` — New token carrying the frosting recipe for floating surfaces (`blur(24px) saturate(160%)`), so every panel that floats over content frosts identically.
- `--panel-float` — New token: the translucent fill for floating flat panels (menu, popover, select, date and time panels).

### Fixed
- **Every live-bound field** — Characters typed while a keystroke was in flight were swallowed. A live-bound field renders as `value` + `oninput`, so the server writes its own copy of the text back on every keystroke; over a real network that copy arrives stale and overwrites what was typed in the meantime, and the next keystroke builds on the damaged text. Measured over a 300 ms round trip, "generiere mir folgende View" arrived as "geneilew". The browser now keeps a short history of the values a field locally held and drops a write that repeats an older one — a late echo of a keystroke already superseded. Programmatic writes (clearing a composer after send, filling a field from a picked suggestion, a server-side correction) are not echoes and still apply. Affects `DrylInputText`, `DrylTextarea`, `DrylInputPassword`, `DrylInputNumber`, `DrylInputMask`, `DrylInputOtp`, `DrylSlider`, `DrylAutocomplete`, `DrylChipInput`, `DrylChatComposer`, `DrylCommandPalette` and `DrylTable`'s search and column filters.
- `DrylInputOtp` — Digits landed in the wrong boxes. Moving the caret to the next box was a server round trip, so every digit typed before the answer came back overwrote the previous one: `123456` arrived as `16`. Caret movement between boxes (advance, backspace walk-back, arrow keys) now happens in the browser.

### Added
- `data-dryl-input` — Opt-in marker for the echo guard above. DRYL's own fields carry it; an app with its own live-bound `<input>` or `<textarea>` can add the attribute to get the same protection.

## [2.18.0] — 2026-07-26

Two gaps that showed up while building a real control centre: a form could not hold long text, and the dock belonged exclusively to itself.

### Added
- `DrylCanvas` — New catalog node type `textarea`: the multi-line sibling of `inputText`, with `rows` (2..20, default 4). A form in an artifact can now hold a Markdown body or a long description instead of squeezing it into a single line.
- `CanvasPrompt.SchemaText` (Agents 0.15.0) — One line for the new `textarea` type, plus the guidance that decides which of the two a generation reaches for.
- `DrylCanvasDock` (Agents 0.15.0) — New `Actions` slot: host controls in the dock head, left of the log toggle. The dock brought stop and reset for nobody, so every host that wanted them had to put them somewhere else on the page — which is exactly the chrome the dock exists to remove.
- `DrylCanvasDock` (Agents 0.15.0) — New `Suggestions` slot above the composer, for ready-made prompt chips. On a workspace page there is no empty state left to carry them.
- `AddDrylCanvasDocumentStore<TStore>` — New optional `ServiceLifetime`. A store that separates documents per user has to read the signed-in user, which a singleton cannot do — so the only lifetime on offer was the one a real host cannot use.

### Fixed
- `DrylCanvasRun` — Started at `Thinking`. Right for an agent run, which starts because someone asked; wrong for a canvas, which is created empty and may never be generated into. A restored document showed an aura claiming to think, and nothing was ever coming to settle it.
- `DrylCanvasDock` — The status line said `Working…` for an idle run that holds a spec. An artifact nobody is working on is `Ready`, whether a generation or a document store put it there.
- `DrylCanvasDock` — A long host `Status` squeezed the AI indicator and the log/collapse buttons out of the head instead of truncating itself: flexbox shrinks every sibling proportionally, and the indicator and the tooltip-wrapped buttons are child-component output, so only a `::deep` rule can reach them.
- `DrylCanvasDock` — The selection chip's type badge wrapped mid-word (`butto/n`) because label and badge shared the row with no flex rules.
- `DrylCanvas` — A `list` or `keyValue` node carrying a data binding was rejected for having no literal entries, although the data prompt offers both as `rows` targets and the source supplies the entries at runtime. `dataGrid` had it right all along.
- `CanvasDataPrompt` — The block never said that `kpi` cannot bind. It is the obvious node for a row of figures, so a model honouring A3 built an empty one with no way out.

## [2.17.1] — 2026-07-26

Sidequest R — **Responsive**. An artifact now knows how wide it really is: the canvas body is a container context, no widget measures against the viewport any more, and the donut fits its slot instead of overflowing it.

### Fixed
- `DrylDonutChart` — The wheel was always as wide as it was tall (`Height`, default 260 px) regardless of its slot, and overflowed both sides of any narrower container (measured: a 220 px wheel in a 200 px slot, 10 px over each edge). It now takes `min(height, available width)` — segments, tooltip anchors and the centre scale with it.
- `DrylCanvas` — The canvas body is a named container context (`canvas`): nodes size themselves against the canvas's width, not the viewport's — the same spec sits in a narrow chat column one moment and full-screen a morph later.
- `DrylCanvas` — No more sideways scrolling: chart tooltips, which are laid out even while hidden, had pushed the body's scroll width past its own width (330 vs 329 at 375 px, 322 vs 318 in a 320 px slot) and offered a scroll to a bubble the canvas clips at its edge anyway. A widget with genuine horizontal needs still scrolls inside itself.

## [2.17.0] — 2026-07-26

Canvas Platform, phase 6 — **Direct Manipulation**. Describing a change is not always the shortest way to make one. Hand the canvas a `CanvasSelection` and its elements become pickable — by click or by keyboard, at the cost of exactly one tab stop for the whole artifact. The selected element carries a toolbar: prompt about it, pin it, duplicate it, remove it, drag it into another slot. A pin is an instruction to the AI author, not a freeze of the widget: the patcher refuses every AI op on a pinned node and hands the model a sentence saying why, while everything the user triggers — a data refresh, an action result, the toolbar itself — still goes through. And because every edit is one ordinary `CanvasOp`, the presence, FLIP and pulse layers animate a user's change exactly like the AI's: one op, one movement.

With this the Agents package has everything it needs for 1.0.

### Added
- **`CanvasSelection`** — The selected node of one canvas surface, shared by the renderer and the prompt dock so the user can point at an element and then talk about it: `Select`, `Clear`, `RequestPrompt`, plus `Id`, `Type`, `Label`, `Locked`, `HasSelection`, `RovingId` and the `OnChange` / `OnPromptRequested` events. Ships with `CanvasNav`, `CanvasNodeCommand` and `CanvasEdit`.
- `DrylCanvas` — New `Selection` parameter. Supplying one **is** the opt-in for direct manipulation; without it not a single attribute of the rendered markup changes. Nodes become clickable and keyboard-reachable (arrow keys walk the tree, `Home`/`End` jump, `Alt`+`↑`/`↓` reorders, `Enter` asks the dock, `Delete` removes, `Escape` deselects), the selected one shows an accent ring and a toolbar, and every selection change is announced through its own `aria-live` region.
- `DrylCanvas` — New `OnEdit` callback (`CanvasEdit`) raised after every completed direct manipulation, carrying a ready-made history label — bump your workspace's `Revision` from it and a user's edit becomes a version exactly like an AI round.
- **`CanvasNode.Locked`** (`"locked": true`) — A node the user pinned. `CanvasPatcher` refuses `setProps`, `remove` and `move` on it, plus `insert`/`move` into or out of it when it is a container, and returns a corrective model-facing reason; its descendants stay editable. The flag travels into a saved document without a schema change.
- `CanvasPatcher.Apply` — New optional `CanvasPatchAuthor` parameter (`User` by default, `Ai` from the AI patch path) — pins bind the AI author only, so a data refresh and an action result the user triggered are never blocked.
- **`CanvasLabel`** — Turns a node into the short, speakable name the toolbar, the dock's chip and every announcement all use for it.
- **`CanvasNodeClone`** — Deep-copies a subtree for "duplicate" with fresh ids and fresh interactive field names, keeping data and action bindings; a copy starts unpinned.
- `DrylChatComposer` — New `FocusAsync()` method.
- `DrylAiCanvas` — New `Selection` and `OnEdit` parameters, forwarded to `DrylCanvas`.
- `DrylCanvasDock` — New `Selection` parameter: a context chip naming the selected element above the input, and one reference line (`id`, type, label) prefixed onto every prompt so "make it a bar chart" patches the right node. "Prompt about this element" opens a collapsed dock and focuses its composer.

### Changed
- `CanvasPrompt` — The generator contract now documents `"locked": true` in both the schema and the update-op block, so the model skips a pinned node instead of being corrected after the fact.

### Fixed
- `DrylCanvas` — A removed node is now dropped from the spec even when the host handles no `OnPurge`; it used to linger forever as an invisible, removing-flagged element.

## [2.16.1] — 2026-07-26

### Fixed
- `DrylChatComposer` — The textarea collapsed to zero height when the composer was attached inside a host that was still hidden (a `DrylCanvasDock` before its popover is shown): the auto-grow measurement read a `scrollHeight` of `0` and pinned that height until the first keystroke. The measurement is now ignored while the element has no layout, and re-run once the host reveals it.

## [2.16.0] — 2026-07-26

Canvas Platform, phase 5 — **Document**. A workspace that only lives in memory is a demo, not an application. A canvas document now serializes the whole workspace — every named view, its artifact, and which one was open — so a dashboard survives a reload, a user switch and a deployment. And because the canvas has no op log (patches mutate in place, a fresh generation replaces the tree), the version history is a snapshot ring per view: undo, redo, and "back to version N", each one morphing through the same view-transition layer a view switch uses. Persistence itself stays yours — DRYL ships the contract and an in-memory implementation and no database code at all.

### Added
- **`CanvasDocument`** — Serializes a whole `CanvasWorkspace` with a schema version: `Capture` (a deep copy that skips views already animating away, and optionally folds live field values into the nodes' `value` props), `Restore`, `ToJson`, `TryFromJson` and `AsTemplate`. Data bindings travel with the document, the numbers do not — a restored document asks the host's registered sources for fresh values. A document written by a newer build is refused with a message that names both schema versions rather than being half-read.
- **`CanvasHistory`** — The per-view snapshot ring behind undo and redo: `Record` (a snapshot identical to the current one is dropped, so a round that changed nothing never fills the ring), `Undo`, `Redo`, `Restore(index)`, `Clear`, plus `Entries`, `Position`, `CanUndo` and `CanRedo`. Recording after an undo truncates the redo branch. Reachable per view as `CanvasView.History`.
- `CanvasWorkspace` — New `Commit`, `Undo`, `Redo`, `RestoreVersion`, `CanUndo` and `CanRedo`, all about the active view; each raises `OnChange` exactly when it actually changed something.
- **`ICanvasDocumentStore`** + `InMemoryCanvasDocumentStore` + `CanvasDocumentInfo` — the persistence contract (`SaveAsync` / `LoadAsync` / `ListAsync` / `DeleteAsync`), task-based so the same interface works over HTTP or `localStorage` on WebAssembly. Register with `AddDrylCanvasDocumentStore()`, or `AddDrylCanvasDocumentStore<TStore>()` for your own — neither ever overwrites an existing registration.
- `DrylCanvasWorkspace` — New `ShowHistory`, `Revision` and `RevisionLabel` parameters: undo, redo and a version-history popover in the view bar. A changed `Revision` commits a version of the active view; every history step runs through `IDrylViewTransition`, so the artifact morphs instead of blinking, and announces itself over `aria-live`. With history on, the bar also shows for a single view — one artifact still deserves an undo.
- `DrylCanvasWorkspace` — New `AutoSave`, `AutoSaveDelayMs`, `DocumentId`, `DocumentIdChanged`, `DocumentTitle` and `OnSaved` parameters: debounced saving against the registered store. A store that throws is swallowed, never surfaced into the circuit — a broken store must not take a running dashboard down.
- `DrylIcon` — New `Undo`, `Redo` and `History` icons.

### Changed
- `DrylCanvasWorkspace` — The view bar no longer scrolls as a whole: the chips scroll inside a new `.ws-chips` element while the tool group stays put, so undo and redo remain reachable at 375 px. Custom CSS that targeted `.ws-bar` for the scrolling row (or `.ws-bar.is-ink-ready`) must move to `.ws-chips`.

## [2.15.0] — 2026-07-26

Canvas Platform, phase 4 — **Catalog**. Nine new node types, and with them the shapes a real line-of-business dashboard used to miss: a sortable, searchable, paged data grid; a form that bundles its fields into one command; a KPI row, lists, key/value pairs, collapsible sections, images, code and an empty state. Every one of them renders through components DRYL already ships — no new component, no new token, no new animation.

### Added
- Canvas catalog — **`dataGrid`**: the interactive big brother of `table`, rendered as a `DrylTable`. `columns` (1–12), optional literal `rows` (max 100), plus `sortable` (default true), `filterable`, `searchable` and `pageSize` (default 10, max 100; paging only appears once there is more than one page). Bind it to a rows source and it takes up to 1000 rows. On a canvas narrower than the grid it scrolls sideways inside its own container rather than squeezing cells to one glyph per line.
- Canvas catalog — **`form`**: a container whose `action` binding sits on the container itself. The interactive nodes inside it become one command with one rendered submit button; `required` field names are checked before the handler is called, and a field that failed shows an inline hint that clears the moment the user edits it. The A4 guarantee is untouched — submitting is still something only a person does.
- Canvas catalog — **`kpi`** (a row of 1–6 compact stats with count-up), **`list`** (up to 50 title/text/icon entries), **`keyValue`** (up to 20 label/value pairs, one or two columns), **`accordion`** (collapsible sections, exactly one child per label, optional `open` index), **`image`** (with required `alt`; `src` must start with `https://`, `/` or `data:image/`), **`code`** and **`emptyState`**.
- `CanvasData` rows sources now fill `dataGrid`, `list` and `keyValue` as well as `table` — one source, four presentations. A `keyValue` needs exactly two columns and says so at the node when it does not get them. Per-type ceilings: table 30, dataGrid 1000, list 50, keyValue 20, each with the existing truncation notice.
- `CanvasCatalog.KnownTypes` — **New property.** The generator's schema is checked against it by a test, so a type can never exist in the catalog without the model being told about it, or vice versa.

### Changed
- An `action` binding may now sit on a `form` as well as on a `button` — the widening phase 2 announced. Anything else is still rejected with a corrective sentence.
- `CanvasPrompt.SchemaText` (Agents 0.13.0) — One line per new type, plus the guidance that decides the expensive cases: `table` for small static tables and `dataGrid` for bound or larger data, one `form` instead of a button per field. A test now caps the schema at 4500 characters; the next type to be added has to negotiate with that limit rather than quietly widen every generation.
- `CanvasPrompt.LayoutBudget` (Agents 0.13.0) — Constrains `dataGrid` columns and `kpi` tiles per width step, the way it already constrained `table` and `stat`.
- Canvas data prompt — The shape map now reads `rows -> table|dataGrid|list|keyValue`, including the keyValue two-column rule.

## [2.14.1] — 2026-07-25

### Fixed
- `DrylCanvasWorkspace` — Opening/closing views in quick succession could kill the circuit: the view bar lives inside a `DrylPresence`, so the gliding-indicator interop could reach an element that had already left the DOM mid-exit, and `ResizeObserver.observe` threw. `dryl.motion.moveIndicator` now ignores anything that is not a live element, and the workspace tolerates the race and re-attaches on the next render.

## [2.14.0] — 2026-07-25

Canvas Platform, phase 3 — **Workspace + Prompt Dock**. A page is no longer one canvas and a chat column next to it: a `DrylCanvasWorkspace` holds named views with exactly one visible, and a `DrylCanvasDock` sits in the corner as a command bar. The AI can open a view for a new subject instead of overwriting the artifact the user is looking at — which is the difference between a tool and a chat toy.

### Added
- `DrylCanvasWorkspace` — **New component.** Named canvas views, exactly one of them large; the view bar carries the same gliding gradient indicator as `DrylTabs`, and switching runs through `IDrylViewTransition`, so the surface morphs instead of snapping. A single view gets no bar at all. Chips close with an exit animation, keyboard is ←/→/Home/End plus `Delete`; the `View` slot decides what renders (typically a `DrylAiCanvas`), otherwise the workspace renders a plain `DrylCanvas`.
- `CanvasWorkspace` / `CanvasView` — **New state types** (`DRYL.Components.Canvas`). `Open` / `Activate` / `Close` / `Remove` / `Clear` plus `OnChange`; a view's id is the slug of its title, so re-opening "Overview" finds the view that already exists rather than adding a second one. Plain observable state a host may pre-fill from code.
- `DrylCanvasDock` — **New component** (`DRYL.Components.Agents`). The prompt dock: a `DrylChatComposer`, one line of live status derived from the run (`Building · 7 elements`, `Ready`, or the error), and the transcript only on demand — the host supplies it through the `Log` slot, so there is no second message model next to `DrylMessage`. Collapses to a single button, corners via the new `DockCorner` enum, full width below 640px. Lives in the browser's top layer (`popover="manual"`), because a `position: fixed` dock would otherwise measure itself against the nearest glass card instead of the viewport.
- `DrylCanvasRun.UseWorkspace(workspace)` — **New method.** From then on `Spec` reads and writes the active view's spec: a generation always fills the view the user is looking at, and switching views resets the interactive form state so a different artifact never inherits the previous one's field values.
- `DrylCanvasTools.Create` / `CreateReplay` — **New optional `workspace` argument.** With it the model also gets `open_view(name, brief)`, which opens (or re-opens) a named view and runs the same create generation into it; the receipt names the view. Without a workspace nothing changes — the tool is not registered and costs no prompt budget.

### Changed
- `create_artifact` — Its description now says explicitly that it replaces whatever is currently shown, and (with a workspace) points at `open_view` for a subject that should stay reachable.

## [2.13.0] — 2026-07-25

Canvas Platform, phase 2 — **Canvas Actions**. A button in an artifact triggers a typed host command instead of a chat message. The AI builds, labels and pre-fills the button; only a human press ever runs the handler — there is no tool and no code path from a model output to a command. That is what makes it safe for an artifact to offer "Release order" at all.

### Added
- `AddDrylCanvasAction(name, description, handler)` — **New DI extension.** Registers a named host command. Same shape as `AddDrylCanvasDataSource`, deliberately: the arguments are declared as a C# record and the model-facing schema is derived from it, an unsupported argument type throws at registration, a duplicate name throws, and tenant and user come from the handler's DI scope (`ctx.Services`) rather than from the spec. A parameterless overload covers commands that take no arguments.
- `CanvasNode.Action` — **New binding**, next to `data`: `"action": { "name": "order.approve", "args": { "orderId": { "$field": "order" } }, "confirm": "Auftrag wirklich freigeben?" }`. An argument is a literal or a reference to an interactive node of the same artifact — the same `$field` syntax a data parameter uses. `confirm` puts a `DrylDialog` in front of the handler. `action` is optional and only valid on a `button`; a button without one behaves exactly as it always has.
- `CanvasActionResult` — **New result type.** `Ok(message)` / `Fail(message)`, plus a fluent `Refresh(sources…)`, `Refresh(source, parameters)`, `Patch(ops…)` and `AskAi(message)`. Success is a toast, failure stays inline at the button, refreshes run through the existing data binder, and patch ops go through the existing patcher and change-pulse. `AskAi` is off by default.
- `ICanvasActionService` — **New scoped service** (`Descriptors` + the infrastructure invoker). Registered by `AddDrylCanvasAction`.
- `CanvasActionRunner` — **New per-canvas runner.** Owns each button's busy/error state, resolves the arguments, gates on the confirmation dialog, invokes the handler inside `try/catch`, and applies the result. A second press while one is running is discarded.
- `DrylCanvas` / `DrylAiCanvas` — **New `OnAction` parameter** reporting every completed action as a `CanvasActionOutcome` (action, node id, success, message). Optional: the canvas is fully functional without it.
- `CanvasInteraction.Message` — **New optional property.** `ToPromptMessage()` returns it verbatim when set, which is how an action's `AskAi(…)` reaches an existing `OnInteraction="i => _chat.Send(i.ToPromptMessage())"` wiring without a single line of host change.
- Canvas catalog — `button` accepts `"kind": "danger"`, and may omit `intent` when it carries an `action`.
- `ICanvasDataService.Invalidate(CanvasInvalidation)` — New overload that republishes a ready-made notice (an action result's refresh list, whose canonical key is already computed).

### Changed
- `CanvasCatalog.Validate(node, context)` now also checks action bindings: the action exists, it sits on a button, its arguments are complete, known and correctly typed, every `$field` resolves within the artifact, and `confirm` is a real question. Findings come back as corrective sentences in the `create_artifact` / `update_artifact` receipt, never as a hard stop.
- `DrylCanvasTools.Create` / `CreateReplay` — (Agents 0.11.0) New optional `ICanvasActionService` argument. When supplied, the registered actions are described to the generator in both prompts, including the line that it may place a button but never trigger one. With nothing registered, neither prompt nor receipt mentions actions and existing chat artifacts are untouched.

### Fixed
- `DrylCanvas` — A node's memoized validation subject dropped its `data` and `action` bindings, so a validity rule that depends on a binding could reject a perfectly good node.

## [2.12.0] — 2026-07-25

Canvas Platform, phase 1 — **Canvas Data Binding**. A canvas node reads its values from a registered host data source instead of from prompt text. Numbers stop hallucinating, stop going stale the second after they were generated, and the token cost decouples from the amount of data. Ships together with the A1 move that puts the whole renderer in this package.

### Added
- `DrylCanvas` — **New component.** Renders a `CanvasSpec` — a titled tree of curated DRYL components — as a live surface: glass card, header (title · `HeaderTools` slot · refresh · expand), body, empty state, error alert, fullscreen through the top layer. Deliberately dumb: a spec goes in, interactions come out, and where the spec came from (code, a database, a saved document, an AI generation) is the host's business. A line-of-business app can now render an artifact without referencing `DRYL.Components.Agents` at all.
- `AddDrylCanvasDataSource(name, description, handler)` — **New DI extension.** Registers a named data source. Its parameters are declared as a C# record and the model-facing schema is derived from that record's primary constructor, so there is one place of truth and IntelliSense in the handler; an unsupported parameter type throws at registration rather than when the model first uses it. A parameterless overload covers sources that take no arguments. Tenant and user come from the handler's DI scope (`ctx.Services`) and never travel through the spec.
- `CanvasData` — **New result shapes.** `Scalar` (→ `stat`, `badge`, `progress`), `Series` (→ `lineChart`, `areaChart`, `barChart`), `Segments` (→ `donutChart`) and `Rows` (→ `table`). The host thinks in data; the binder maps the shape onto whichever node type the artifact happens to use. A `Rows` result beyond the catalog's 30-row ceiling is cut and the node says so.
- `CanvasNode.Data` — **New binding.** `"data": { "source": "sales.byMonth", "params": { "year": 2026, "region": { "$field": "region" } }, "refresh": "interval:30s" }`. A parameter is a literal or a reference to an interactive node of the same artifact — one select at the top, and the dependent nodes follow without an AI turn. `data` is optional; a node without it behaves exactly as it always has.
- `ICanvasDataService` — **New scoped service.** `Descriptors` (what exists), `Invalidate(source)` and `Invalidate(source, parameters)` (the host says something moved). Registered by `AddDrylComponents()`.
- `CanvasDataBinder` — **New per-canvas binder.** Keys bindings by source plus resolved parameters, so three stat nodes on `orders.open` cost one call. Four triggers: first render, a debounced change to a referenced field, one interval timer per canvas (five-second floor), and `Invalidate`. Per key at most one load runs, a newer one cancels the older, and a late straggler is dropped.
- `CanvasPulseTracker` — **New.** The single source of truth for "this node just changed", stamped by both the AI patcher and the data binder, so a data refresh looks exactly like an AI change.
- `CanvasCatalog.Validate(node, context)` — **New additive overload.** Checks a binding's source, result shape, parameters, `$field` targets and `refresh` syntax, and returns one corrective sentence for the model's receipt. The parameterless signature is unchanged.

### Changed
- **The canvas renderer moved into this package** (roadmap A1). `CanvasSpec`, `CanvasNode`, `CanvasJson`, `CanvasCatalog`, `CanvasNodeView`, `CanvasPatch`, `CanvasPatcher`, `CanvasFormState` and `CanvasInteraction` now live in `DRYL.Components` under the namespace **`DRYL.Components.Canvas`** (they were `DRYL.Components.Agents`). **Migration:** add `using DRYL.Components.Canvas;` — or `@using DRYL.Components.Canvas` in `_Imports.razor`. Nothing else changes. See the Agents 0.10.0 note below.
- `CanvasNode.Version` is now public. It is the mutation stamp renderers memoize on; a patcher outside this package needs to bump it.
- `wwwroot/js/dryl-canvas.js` now ships from `_content/DRYL.Components/js/` (was `_content/DRYL.Components.Agents/js/`). Nothing to wire up — the component imports it itself.
- **Refresh is a movement, not a rebuild** (roadmap A8). A bound node's first load shows a `DrylSkeleton`; every later refresh keeps the node's identity and lands as the existing change-pulse, with `DrylStat.CountUp` tweening the number. A refresh that changes nothing renders nothing at all, so a dashboard on a 30-second interval never blinks. A failure after a good value keeps the value and adds a small marker whose tooltip names the source; a failure without one shows a compact inline error at that node only — a broken widget must never blow up the dashboard around it.
- `DrylCanvasRun` — (Agents 0.10.0) New `Pulse` property (`CanvasPulseTracker`) replacing the internal change-stamp table. `DrylAiCanvas` hands it to `DrylCanvas`, so an AI patch and a data refresh stamp the same tracker.
- `DrylCanvasTools.Create` / `CreateReplay` — (Agents 0.10.0) New optional `ICanvasDataService` argument. When supplied, the registered sources are described to the generator in `create_artifact` **and** `update_artifact`, and every produced binding is validated against them; findings come back as corrective sentences in the receipt, never as a hard stop. With no registered sources neither the prompt nor the receipt mentions data at all, so existing chat artifacts are untouched (roadmap A2).
- `DrylAiCanvas` — (Agents 0.10.0) Now wraps `DrylCanvas` and keeps only what is specific to an AI being the author: the run subscription, the aura, the status indicator, the announcements and the artifact-swap morph. Its public parameters are unchanged.

### Removed
- **⚠ Breaking (Agents 0.10.0).** The canvas renderer types listed under *Changed* no longer live in `DRYL.Components.Agents`. They moved to `DRYL.Components` / namespace `DRYL.Components.Canvas`; `[Obsolete]` aliases were deliberately not added. Add `using DRYL.Components.Canvas;` and the code compiles again. `DrylAiCanvas`, `DrylCanvasRun`, `DrylCanvasTools`, `CanvasPrompt` and `CanvasStreamReveal` stay in the Agents package where they were.
- `DrylCanvasRun.ChangeTickOf(id)` — (Agents 0.10.0) removed; use `run.Pulse.TickOf(id)`.

## [2.11.0] — 2026-07-25

### Added
- `DrylStat` — New `CountUp` parameter: the headline value tweens up to its new number instead of snapping — from 0 on first render, from the previous number on every change. The first number in `Value` is animated while any prefix/suffix (currency symbol, unit, `%`) rides along unchanged, and the tween always lands on exactly the string you passed. Off by default; honours reduced motion.
- `DrylIcon` — New `Maximize` and `Minimize` icons (Lucide `maximize-2` / `minimize-2`).
- `dryl.motion.countUp(el, text)` — New shared motion primitive backing `DrylStat.CountUp`. Duration reads `--dur-slow`; the easing mirrors `--ease-out`.
- `dryl.topLayer.show/hide(el)` — New helper that promotes an element to the browser's top layer via the Popover API. `position: fixed` is measured against the nearest transformed/filtered ancestor, so a "fullscreen" overlay built on it quietly fills a card instead of the viewport; the top layer has no containing block at all. Browsers without the Popover API fall back to plain fixed positioning.
- `DrylAiCanvas` — (Agents 0.9.0) New `AllowExpand` parameter (default `true`): the header offers an expand-to-fullscreen button, and the artifact *grows* into the overlay through a view transition rather than jumping. The expanded canvas really covers the viewport (top layer, see above), scrolls its body while the header stays put, and contains overscroll so the page behind stays still. `Escape` collapses it. It is an overlay, not a modal — it neither traps focus nor blocks the page. Set `false` for canvases inside a surface that owns its own layering.
- `DrylCanvasRun` — (Agents 0.9.0) New `NodeCount` property: the number of nodes in the live tree.
- `DrylCanvasRun` — (Agents 0.9.0) New `AvailableWidth` property and `ReportWidth(int)` method: the measured inline width of the surface the artifact renders into. `DrylAiCanvas` keeps it current on its own; a consumer only needs this when hosting the artifact tree outside the canvas.

### Changed
- `DrylAiCanvas` — (Agents 0.9.0) **Change-pulse:** a `setProps` patch now flashes a one-shot accent ring over the node it changed. Every patch op finally has exactly one visual — an insert enters, a move glides, a remove exits, and a content change pulses. Compositor-only (opacity + transform), reduced-motion aware.
- `DrylAiCanvas` — (Agents 0.9.0) **Live build counter:** while an artifact streams, the header status reads "Building · 14 elements" and a thin indeterminate progress line runs under the header, so the artifact visibly grows even between revealed nodes.
- `DrylAiCanvas` — (Agents 0.9.0) Canvas `stat` nodes count up: AI-authored numbers now tween on every `setProps` instead of snapping.
- `DrylAiCanvas` — (Agents 0.9.0) **The artifact generator now knows how much room it has.** The canvas measures its own body width and hands every `create_artifact` / `update_artifact` generation a matching layout budget (grid column ceiling, stat label and table column limits, chart series/label limits). The artifact generator is stateless and never sees the page, so it previously authored a three-column dashboard for a 360px phone column just as readily as for a desktop panel. The width is the canvas element's, not the viewport's — a canvas in a narrow side panel on a wide screen gets the narrow budget — and it is re-read on every tool call, so resizing, rotating or expanding to fullscreen is reflected in the next generation.

### Fixed
- `DrylGrid` — `Columns` never actually stepped down on narrow slots. The step-down rules are container queries, and a container query resolves against the nearest *ancestor* container — the grid carried `.cq` on itself, which can never match. Fixed columns now get a container-query wrapper (the same shape `DrylStack.CollapseBelow` already used), so `Columns="3"` really becomes 2 columns below 768px and 1 below 480px of available width. This was why a canvas artifact's three stats stayed side by side on a phone, one glyph per line.
- `DrylAiCanvas` — (Agents 0.9.0) A second `create_artifact` no longer replaces the artifact with a hard cut: the old tree morphs into the new one through a view transition (the "Depth Glass" mercury merge), giving the replaced artifact the exit animation it previously had no way to play. Browsers without the View Transition API — and users who prefer reduced motion — get the plain, morph-free swap.
- `DrylLineChart` / `DrylBarChart` / `DrylAreaChart` / `DrylDonutChart` — A `ValueFormat` .NET does not recognise (e.g. `"K"`, or an inner `"{value:Q}"`) now falls back to the default number format instead of throwing mid-render and taking the circuit down.

## [2.10.2] — 2026-07-25

### Changed
- Canvas prompt — (Agents 0.8.2) `valueFormat` is now documented as a `{value}` display template with examples, and duplicate node ids in a generated artifact are reported back to the model in the create receipt so it can repair them via `update_artifact`.

### Fixed
- `DrylLineChart` / `DrylBarChart` / `DrylAreaChart` / `DrylDonutChart` — `ValueFormat` now supports `{value}` templates (e.g. `"€{value} Tsd"`, `"{value}%"`, optional inner .NET format `"{value:0.0}"`) in addition to plain .NET format strings; AI-authored templates no longer render the literal `{value}` placeholder into axes and tooltips.
- `DrylDonutChart` — The tooltip/aria-label percentage is always plain-formatted, so a percent-style `ValueFormat` no longer produces duplicated `%` signs.
- `DrylAiCanvas` — (Agents 0.8.2) Nodes that stay invalid after a generation settles now show a compact error placeholder with the catalog's corrective message instead of an endless "waiting…" skeleton (the skeleton remains while streaming).
- `DrylAiCanvas` — (Agents 0.8.2) Cancelling a generation mid-stream settles the run back to idle instead of leaving the canvas stuck in "Building".
- `DrylAiCanvas` — (Agents 0.8.2) A fresh `create_artifact` resets the interactive form state, so a recycled field name no longer shows the previous artifact's user input over the new AI-provided value.
- `DrylAiCanvas` — (Agents 0.8.2) A container revealed as a streaming shell (and the root) now has its props final-synced when the stream completes — e.g. a card title that finished late now lands.

## [2.10.1] — 2026-07-24

### Added
- `DrylAiGenerate<T>` — (Agents 0.8.0) New `ThrottleMs` parameter (default 66 ≈ 15fps): fast token bursts are coalesced to this budget so structured streaming no longer fires one Blazor Server re-render (and full re-parse) per token, bounding circuit/SignalR load; a slow stream still renders every token and the final snapshot always renders. Set `0` to render on every token.
- `PartialJsonReader<T>` — (Agents 0.8.0) New `AppendRaw` / `Snapshot` split so buffering (cheap, per token) is decoupled from repair+deserialize (per render frame); `Snapshot` is memoized to skip re-parsing an unchanged buffer. `Append` still works as before.

### Changed
- `DrylAiCanvas` — (Agents 0.8.1) Canvas nodes memoize catalog validation and props deserialization on a new internal mutation stamp (`CanvasNode.Version`, bumped by patcher/reveal/purge): JSON parsing and validation now run only when a node actually changes, not on every render — during create streaming this removes O(nodes × deltas) re-parses.
- `DrylMarkdown` — Unchanged `Content` no longer re-parses through Markdig on every parent render; the parse runs only when the content actually changes.

### Fixed
- `DrylAiCanvas` — (Agents 0.8.1) A second `create_artifact` whose tree reuses node ids from the previous artifact no longer renders the old artifact's content: the canvas node's parse/validation memo now keys on node instance identity in addition to the `CanvasNode.Version` stamp, so Blazor's component reuse across whole-tree replacements cannot hit a stale memo.
- `PartialJsonReader<T>` — (Agents 0.8.0) An incomplete trailing number no longer flickers its field value → `null` → value mid-stream (e.g. while `12.5` streams as `12` / `12.` / `12.5`): the repair keeps the longest complete numeric prefix instead of dropping the field.
- `DrylAiCanvas` — (Agents 0.8.0) `create_artifact` streaming no longer flickers as the artifact fills in. Instead of replacing the whole spec and re-rendering the half-parsed tree on every token, the canvas now reveals one **complete** node at a time (recursively, leaf level): within each container the still-streaming last child is withheld — a leaf until it finishes, a container as an empty shell that fills — and already-revealed nodes keep their instance (frozen) so settled parts don't re-render or replay their reveal animation. Each finished element fades in on its own, matching the `update_artifact` choreography.

## [2.10.0] — 2026-07-22

### Added
- `DrylToolCallGroup` — New core component that collapses a run of agent tool calls into one quiet, gently-pulsing summary row instead of a heavy stack of cards; while a call runs it live-tickers the active tool name (calm Aurora aura), once settled it reads "N tool calls · Done", and it expands via a grid-rows disclosure to the individual `DrylToolCall` cards. Auto-expands when a tool errors so failures are never hidden.
- `DrylAiCanvas` — (Agents 0.6.0) Interactive chat artifacts: the AI builds and iterates a live, fully interactive composition of DRYL components next to the chat — streaming in node by node, morphing in place on updates (staged patch ops + FLIP glide), with button intents and input values routed back as structured `CanvasInteraction` events via `OnInteraction`
- `DrylCanvasTools` — (Agents) `create_artifact` / `update_artifact` chat-agent tools; each call runs a dedicated structured-streaming sub-generation (raw JSON deltas → progressive `CanvasSpec` snapshots, patch ops applied one per beat) against a consumer-supplied generator agent, plus a `CreateReplay` seam for demos/tests without a live model
- `DrylCanvasRun` — (Agents) Observable canvas handle atop `DrylRunBase`: live `Spec`, completed `Round` count, `ChangedIds` of the latest ops; a failed generation surfaces via `Error` without killing the canvas
- `CanvasInteraction` / `CanvasFormState` — (Agents) A click inside the artifact carries its intent plus a snapshot of every input value; `ToPromptMessage()` turns it into the next chat turn
- Canvas catalog — (Agents) Curated 19-type node vocabulary (stack/grid/card/tabs, markdown, stat, badge, progress, table, timeline, four chart types, four inputs, button, divider) with per-node validate-and-fallback: invalid nodes render as skeleton placeholders and the model gets a corrective tool result

### Changed
- `DrylMessage` — Composite assistant bubbles (tool calls + text + usage) now carry a consistent vertical rhythm between their stacked blocks; single-block text/markdown bubbles are visually unchanged.
- `DrylAgentToolCalls` — (Agents 0.7.0) No longer stacks every tool call as a full card; wraps them in the new `DrylToolCallGroup` so a long agent turn reads as one calm, expandable summary that tickers the running tool.

## [2.9.0] — 2026-07-18

### Added
- `dryl.motion` — New `autoFlip`/`stopAutoFlip` primitive: FLIP position-glide
  for `[data-cid]` children (powers `DrylAiCanvas` move ops); reduced-motion
  aware, transform-only.

## [2.8.3] — 2026-07-16

### Fixed
- `DrylMarkdown` — Leaving AI mode no longer snaps the layout: the glass panel
  (padding + background) that AI mode adds now glides in and out over
  `--dur-slow`, so after the aura's `--out` dissolve the text settles back into
  the bare layout instead of jumping. Honours `prefers-reduced-motion`.

## [2.8.2] — 2026-07-16

### Fixed
- **AI aura performance** — The comet no longer animates the registered
  `--ai-aura-angle` custom property (which repainted the conic gradient and
  re-ran both drop-shadows on the main thread every frame, for every visible
  aura — the main cause of the "laggy" feel on mid-range machines). The comet
  is now a dedicated `.ai-aura-comet` element: a static masked + bloom-filtered
  wrapper whose oversized conic square rotates via a compositor `transform`.
  Same vocabulary, same states, zero per-frame paint. Measured on the docs AI
  page under 8× CPU throttling: main-thread frame time freed almost entirely;
  frame rate up ~50 % overall. Side effect: the specular head's bloom (always
  intended per the design comment) now actually renders past the 1px border —
  previously the mask clipped most of it away.
- `DrylTabs` — The per-tab AI ring uses the same compositor-driven comet
  (new internal `.tab-comet` child) instead of the angle-animated `::before`.
- **Streaming sheen, Aurora drift, `.skel` shimmer** — All remaining
  `background-position` loop animations (paint per frame) replaced with
  oversized gradient strips sliding via `transform`/`translate` on the
  compositor: `.ai-aura-glow::before` (sheen), the Aurora variant's edge field
  (now two counter-phased strips on the ring's pseudos), and the skeleton
  shimmer (now on `.skel::before`; `DrylSkeleton`'s state mutations follow).
- **Retired comets stop costing** — After the Generated afterglow retires the
  comet, it now ends `visibility: hidden` so the compositor stops drawing the
  (invisible) spinning surface for the rest of the surface's life.
- **Page aurora** — `blur(70px) saturate(140%)` moved from the `.aurora`
  container onto the individual orbs, so the GPU re-filters three small
  cached orb layers instead of one oversized container surface every frame
  of the drift.

### Changed
- `--ai-aura-angle` is now legacy: still registered (consumer CSS reading it
  keeps resolving) but no longer animated by the library.

## [2.8.1] — 2026-07-13

### Fixed
- `DrylAutocomplete` — The async-search loading spinner no longer sinks to the
  bottom-right corner of the field. Two-part fix: absolutely-positioned input
  icons now center vertically (`.input-wrapper .input-icon` gains `top: 50%` +
  `translateY(-50%)`), and the spinner's positioning classes moved off the
  `DrylSpinner` root onto a neutral wrapper `<span>` — the spinner's scoped
  `.spinner-wrap` rule (`position: relative`, equal specificity, later-loading
  scoped bundle) was overriding the global `position: absolute`, pushing the
  spinner out of the field regardless of the centering fix.
- `DrylCard` — The 3D `Depth` warp no longer shows hard rectangular seams inside
  the card when the pointer nears a corner. The gloss (`.dg-sheen`) is now
  overscanned past its parallax travel and clipped back to the rounded card, so
  its own edge never slides into view.
- `DrylButton` — AI mode now uses the shared aura vocabulary (`.ai-aura-ring` /
  `-glow` / `-wash` via `DrylAuraElements` + `AuraLifecycle`) like every other
  host, replacing the old bespoke rotating-ring. Buttons now match the redesigned
  AI aura, gain graceful enter/exit and the `Aura` variant, and re-key the
  one-shot Generated wash.
- `DrylSegmentedControl` — The selected-segment glow is now theme-aware. The
  hardcoded violet `rgba(124, 92, 255, …)` halo was replaced with the themed
  `var(--accent-a)`, so it follows `DrylThemeProvider` accents and both color modes.

## [2.8.0] — 2026-07-13

### Added
- `DrylReconnectModal` — Drop-in, on-brand replacement for Blazor Server's default
  reconnect overlay. Renders the framework's `#components-reconnect-modal` element and
  drives all three states (reconnecting / failed / rejected, incl. .NET 9 `retrying` &
  `paused`) with pure CSS keyed on the framework state classes — so it works while the
  circuit is down. Glassy, animated (enter + exit, spring-glide card, live spinner),
  correct in both color modes, honours `prefers-reduced-motion`. Every message is an
  overridable parameter (`ReconnectingText`, `FailedText`, `RejectedText`,
  `RetryButtonText`, `ReloadButtonText`, `AttemptLabel`, `NextAttemptLabel`,
  `SecondsSuffix`) plus `ShowAttemptCounter` for the live `Attempt X / Y` counter.
- `dryl.css` — New `--z-reconnect` layering token (above every other layer) so the
  reconnect overlay always wins.

## [2.7.1] — 2026-07-13

### Fixed
- `DrylMainContent` — The app-shell scroll region no longer parks its vertical
  scrollbar "in the middle of the page" on wide viewports. `.main` was both the
  scroll container **and** width-capped (`max-width`) + centred, so on viewports
  wider than the cap its scrollbar floated inward from the right edge. The scroll
  container now spans the full grid track and the reading-width cap moved to a new
  inner `.main-inner` wrapper, so the scrollbar sits flush at the viewport edge on
  every page. No API change

## [2.7.0] — 2026-07-12

### Changed
- `DrylTable` — Under `Ai=Streaming`/`Generated` on a plain client-side list, a
  streaming insert now wraps its view rebuild in a same-document view transition:
  the surrounding rows **glide** down to make room for the newly-arrived rows
  instead of snapping, while the new rows keep their accent-flash entry. Reuses the
  existing `RowIdSelector` for the per-row morph identity and the shared
  `::view-transition-group` glide vocabulary — no new API. Rapid successive inserts
  coalesce (an in-flight transition is not interrupted). Falls back to the previous
  flash-only entry without the View Transition API, under `prefers-reduced-motion`,
  during prerender, and when `Virtualize`/`GroupBy`/`DataProvider` is set

### Fixed
- `DrylTable` — Row `view-transition-name`s are now scoped per table instance, so two
  morph-enabled tables on the same page (e.g. `AnimateReorder` + AI streaming) no longer
  collide on a shared row id and abort the transition with a duplicate-name error

## [2.6.0] — 2026-07-12

### Added
- `AiAura` enum (`Comet` / `Aurora`) and a shared **`Aura`** parameter on
  AI-aware components and `DrylAiScope`. Selects the aura variant and is
  scope-propagated, so a whole subtree can switch to the calmer **Aurora** for
  dense AI pages. Defaults to `Comet` (bold travelling comet); leaving it unset
  inherits the surrounding scope
- `DrylAuraElements` — shared child markup for an AI aura (comet ring, breathing
  glow, one-shot Generated wash), driven by the new `AuraLifecycle`
- New CSS tokens `--ai-core` (the comet's theme-aware specular head) and
  `--ai-strength` (light-mode aura-presence multiplier)
- `Aura` parameter added across the remaining AI-aware hosts (see Changed) —
  `DrylInputText`, `DrylInputNumber`, `DrylInputPassword`, `DrylInputMask`,
  `DrylInputOtp`, `DrylTextarea`, `DrylSelect`, `DrylMultiSelect`,
  `DrylAutocomplete`, `DrylChipInput`, `DrylDatePicker`, `DrylTimePicker`,
  `DrylSlider`, `DrylRadioGroup`, `DrylRating`, `DrylFileUpload`, `DrylDialog`,
  `DrylChatComposer`, `DrylTable`, `DrylStat`, `DrylImage`, `DrylCodeBlock`,
  `DrylTimelineItem`, `DrylDonutChart`, `DrylLineChart`, `DrylBarChart`,
  `DrylAreaChart`, `DrylExpansion`, `DrylStepper`/`DrylStep`,
  `DrylCommandPalette`, `DrylAlert`, `DrylEmptyState`, `DrylProgress`,
  `DrylSpinner`, `DrylSkeleton`, `DrylNotifications`, `DrylErrorBoundary`

### Changed
- **AI aura, redesigned** — the shared `.ai-aura*` primitive now reads by state
  *character*, not just speed. Comet (default): an even, aspect-independent base
  saum (fixing the patchy long edges wide surfaces used to show) plus a luminous
  travelling comet, with per-state colour weighting (Thinking violet-dominant,
  Streaming cyan) and a directional Streaming sheen. Aurora: a soft flowing edge
  field. `Generated` now rests as a calm afterglow (the comet retires) so a
  surface can hold an "AI was here" trace without dropping to `None`; leaving AI
  mode dissolves the aura over `--dur-slow` instead of snapping. Colour comes
  only from the AI accent tokens; `prefers-reduced-motion` keeps it static.
  Applies to every AI-aware surface
- **`Aura` variant + graceful exit now on every AI-aware host** — completes the
  redesign rollout: each host drives its aura through the shared `AuraLifecycle`
  (so leaving AI mode dissolves instead of snapping) and honours the `Aura`
  variant from an explicit parameter or a surrounding `DrylAiScope`. `DrylRating`
  now renders the full ring/glow (a new `.rating-wrapper.ai-aura` radius traces
  the stars as a pill). Off by default (`Ai="AiState.None"`) — existing call
  sites are unchanged. Special cases kept intentionally: `DrylButton` / `DrylTab`
  keep their compact conic ring and ignore `Aura` (Aurora is meaningless at that
  size); `DrylNotifications` scopes the variant per AI-provenance item (its state
  is fixed per item, so there is no per-item exit); `DrylImage` keeps its bespoke
  per-state treatment while gaining the variant + graceful exit
- `DrylMarkdown` — now inherits the shared AI-aware base, so it resolves `Ai`
  and `Aura` from a surrounding `DrylAiScope` (it previously honoured only an
  explicit `Ai`)

## [2.5.0] — 2026-07-12

### Changed
- Streaming Markdown reveal — the newest-block treatment is now a **hot→cold cooldown** instead of a travelling gradient sweep, bringing the per-word `.stream-token` cooldown to block level. The newest top-level block carries a vertical temperature gradient anchored to its box (`0deg`): its bottom line — at the writing head — glows hot in `--ai-a`, cooling upward through `--ai-b` to settled `--fg`. As the block grows, older lines rise out of the hot zone, so the text visibly "cools with the writing head"; a short one-line block reads warm throughout. The hot zone breathes gently in brightness (`stream-cooldown`, `--dur-slow`/`--ease-in-out`) to stay alive between token bursts. Because the gradient covers the block 1:1 and `background-position` is never animated, the invisibility/caret-drift trap of the old oversized swept image (2.4.1) cannot arise. Colour comes only from the AI accent tokens + `--fg`; `prefers-reduced-motion` keeps it static and readable. Applies to `DrylMarkdown` and Markdown `DrylMessage` bubbles; no API change

## [2.4.3] — 2026-07-12

### Fixed
- `DrylMarkdown` — streaming Markdown no longer crashes the circuit when a token-stream chunk boundary splits a UTF-16 surrogate pair (e.g. mid-emoji). The accumulated text is re-parsed on every chunk, and a lone surrogate at the tail made Markdig's AutoIdentifier (advanced extensions) throw `ArgumentException: String contains invalid Unicode code points` during ICU normalization of heading slugs. `DrylMarkdown` now strips unpaired surrogates before parsing (allocation-free when the text is already well-formed); the next chunk delivers the pair and the character renders normally

## [2.4.2] — 2026-07-12

### Changed
- Plain-text streaming reveal — freshly arrived words now ignite with an Aurora **cooldown glow**: each word lights up in the theme's start accent (`--ai-a`, "hot"), shifts to the end accent (`--ai-b`) as it sharpens, then the glow dies to nothing. Because words mount one after another behind the moving caret, it reads as a short glowing trail cooling in the cursor's wake. Colour comes only from the AI accent tokens, so it follows whatever the active theme derives; timing reuses the existing `--dur-slow` / `--ease-out` reveal (no new motion), and `prefers-reduced-motion` keeps it glowless. Layered onto the existing `.stream-token` blur reveal — no API change

## [2.4.1] — 2026-07-12

### Fixed
- Streaming Markdown sheen — the newest-block reveal no longer makes trailing text vanish during fast streaming. The 260%-wide gradient was `no-repeat`, so as the sheen swept, `background-position` pushed the oversized image off the block and left the trailing text uncovered; with `background-clip:text` + transparent fill those glyphs rendered fully invisible, and the tip caret (anchored after the full text) floated out past the visible edge. The gradient now tiles (`background-repeat: repeat`, seamless since both ends are `--fg`) and the keyframes travel exactly one tile, so text stays fully visible and the caret sits at the true writing head throughout the sweep

## [2.4.0] — 2026-07-12

### Added
- Streaming reveal, reworked — freshly streamed AI text no longer pops in at full opacity; it comes alive, with a small glowing violet→cyan accent dot riding the tip of the stream. Two Blazor-safe reveals share the AI motion vocabulary, both new primitives in `dryl.css` and honouring `prefers-reduced-motion`:
  - **Plain text** (`DrylMessage Text="…"` without `Markdown`) — each freshly arrived word blur-fades to crisp, staggered per word (`.stream-token`, rendered fully server-side; already-shown words never re-animate)
  - **Markdown** (`DrylMarkdown`, and Markdown `DrylMessage` bubbles) — the newest top-level block (the one being written) is filled with the moving AI gradient — a "being written right now" sheen that advances with the writing head as blocks complete; settled blocks relax to normal text
- `DrylMarkdown` — shows the newest-block streaming sheen automatically while `Ai="AiState.Streaming"`; no API change
- `DrylMessage` — plain-text bubbles show the per-word materialize while streaming, Markdown bubbles the block sheen; no API change

### Fixed
- `DrylMessage` — Streaming Markdown bubbles no longer double up the AI aura: `DrylMessage` was forwarding its `Ai` state into the nested `DrylMarkdown`, which independently drew its own ring/glow/glass surface inside the bubble that already hosts the aura, reading as a nested duplicate box. `DrylMarkdown` inside a message bubble now renders plain (`Ai` stays `AiState.None`); only the bubble carries the aura

## [2.3.0] — 2026-07-11

### Added
- `DialogOptions.AnimateHandoff` — Opt-in per dialog call: when a dialog opens while its predecessor is still closing (the sequential "agent handoff" pattern), the swap morphs through the browser's View Transition API (`IDrylViewTransition`) instead of the default CSS cross-fade. `DrylDialog`'s shell, header, body and footer each carry a matching `view-transition-name` so the outer size/position glides while the title/content/buttons cross-fade independently. `DrylDialogProvider` finalizes the outgoing dialog immediately and wraps the swap in one view transition. Off by default; falls back to the existing cross-fade automatically without View Transition support, during prerender, and under `prefers-reduced-motion`
- `DialogOptions.HandoffStyle` — Morph tier for `AnimateHandoff`, defaults to `DrylViewTransitionStyle.DepthGlass`: every named dialog element also gets `view-transition-class="dryl-depth"`, so the mercury-merge + translucency pulse plays on the swap — making the content change read clearly even when two dialogs happen to be nearly the same size. Set to `DrylViewTransitionStyle.Glide` for the cheaper shape-only morph

### Fixed
- `dryl-depth-clarify` (the DepthGlass view-transition tier) — the keyframes only animated `filter`, so the `animation` shorthand on `::view-transition-new(*.dryl-depth)` silently dropped the browser's default cross-fade-in; the new snapshot popped in at full opacity on frame one and instantly occluded the still-fading-out old one, reading as a flicker instead of a merge. Added an `opacity` ramp to the same keyframes — first hit on `DrylDialog`'s handoff morph, but the bug affected every `DepthGlass` shared-element morph (e.g. `DrylCard.ViewTransitionStyle`)

## [2.2.1] — 2026-07-11

### Changed
- `DrylTabs` — Tab content entry is now direction-aware: switching to a tab on the right slides the panel in from the right, switching back slides in from the left (reuses the shared presence keyframes; the previous subtle fade+rise remains for the first render). Honours `prefers-reduced-motion`

## [2.2.0] — 2026-07-11

### Added
- `IDrylViewTransition` / `dryl.viewTransition` — Same-document View Transition bridge: `RunAsync(mutate)` snapshots the DOM, applies the state change once the render is committed, and morphs old → new (FLIP-style position/size/opacity). Direct, morph-free fallback for unsupported browsers, prerender and `prefers-reduced-motion`; registered by `AddDrylComponents()`
- `DrylViewTransitionStyle` — `Glide / DepthGlass` morph tiers. New `--ease-viscous` easing token (view-transition pseudo-elements only) plus the `dryl-depth` "Depth Glass" CSS choreography: mercury merge filter + translucency pulse with crystalline clarity landing on `--dur-med`, before the `--dur-slow` shape settle
- `DrylCard` — New `ViewTransitionName` / `ViewTransitionStyle` parameters mark a card as a shared-element morph endpoint
- `DrylAiField` (`DRYL.Components.Agents` **0.5.0**) — Wrap any existing text-like DRYL input to give it a ✨ AI affordance: empty field → the agent generates the value, selected text → only the selection is transformed (instruction-driven or free mini-prompt). The result streams live into the field through a DOM value bridge (the inner `@bind-Value` keeps working untouched), the shared ai-aura plays via `DrylAiScope`, and the user accepts or rejects (Esc) the suggestion. One line, zero wiring: `<DrylAiField Agent="agent" Instruction="…"><DrylTextarea @bind-Value="…" /></DrylAiField>`

### Changed
- `DrylTable` — New `AnimateReorder` + `RowIdSelector` parameters: row drag-reorder (and click-to-sort) morphs rows to their new position via a view transition instead of snapping. Off by default; requires a client-side list

## [2.1.0] — 2026-07-10

### Added
- `--bar-bg` — Dedicated token for the `DrylAppBar` (flat) surface, themeable independently of the modal scrim (`--backdrop-soft`)

### Changed
- `DrylAppBar` — Flat bar now reads from `--bar-bg` instead of `--backdrop-soft`; the light-mode default is a cleaner, cool frosted-white glass (was a murky dark tint). Dark mode is unchanged

## [2.0.0] — 2026-07-10

### Added
- `DrylColorMode` — `System / Dark / Light`; `IDrylThemeService.SetModeAsync`, `CurrentMode` and the multicast `OnModeChanged` event
- `DrylThemeProvider` — New `Mode` parameter (startup value); persists explicit choices in localStorage (`dryl-color-mode`) and restores them before first paint (prerender-safe inline script, no flash)
- `DrylColorModeToggle` — Animated System / Light / Dark switcher (sun–moon morph, auto badge); reflects mode changes it didn't trigger
- Light color rendition of the full token system ("Aurora Light"): tinted near-white ground, white-glass surfaces, light-validated semantic and chart palettes (contrast-checked ≥ 3:1, scripted)
- New effect tokens for every per-mode optical detail: `--edge-hi(-strong)`, `--sheen-grad(-soft)`, `--shimmer(-strong)`, `--hover-wash`, `--press-wash`, `--line-hover`, `--backdrop(-soft)`, `--scrollbar-thumb(-hover)`, `--on-accent(-line/-hi)`, `--knob`, `--accent-fg`, `--accent-ico`, `--danger-fg`, `--success/warning/danger/info-hi`, `--depth-edge(-strong)`, `--depth-shadow`, `--panel-*`, `--code-bg`, `--code-fg`, `--grain-opacity`, `--aurora-opacity`
- `scripts/check-light-sync.mjs` and `scripts/validate-light-contrast.mjs` — dev-time guards for the light token set

### Changed
- **BREAKING:** the default color mode now follows the operating system (`prefers-color-scheme`). Apps that must stay dark regardless of the OS pin `<DrylThemeProvider Mode="DrylColorMode.Dark" />`
- All remaining hardcoded color literals in `dryl.css` and scoped styles lifted onto semantic tokens; danger alpha washes now derive from the `--danger` seed via `color-mix`
- `DrylTooltip` — The bubble is now a body-level fixed portal driven by one delegated listener in `dryl.js`: it can no longer be clipped by card overflow/backdrop-filter, and it flips to the opposite side when the preferred placement has no viewport room (app-bar tooltips open downward). The `.tt`/`.tt-bottom/left/right` CSS classes are replaced by `.tt-portal`
- App shell — `.main` centers its capped column inside the grid track instead of piling leftover space onto the right edge
- Markdown/chat code fences keep a dark surface in both modes (`--code-bg`/`--code-fg`); `DrylCodeBlock` follows the mode via its token-mapped syntax colors

## [1.5.0] — 2026-07-09

### Added
- `DrylTheme` — New optional `Charts` override (`DrylChartPalette`, `Series1`–`Series6`): themes can pin individual chart series tokens (`--chart-1`…`--chart-6`); unset slots keep the derived/default colors
- `DrylThemes` — Presets now ship curated chart palettes where their accent hue collides with a fixed series anchor: Ember swaps the amber anchor (slot 3) for cyan, Verdant swaps the green anchor (slot 4) for violet, Mono pins the full validated default palette (its slate seeds carry no usable hue)

### Changed
- Charts — `--chart-1` / `--chart-2` now follow the active theme: hue from the accent seeds, lightness/chroma normalized into the dark-validated band via relative color syntax. All six chart tokens are registered `@property` colors, so theme changes glide and engines without relative color syntax fall back to the previous fixed palette
- `DrylThemeProvider` — Runtime theme application now clears all theme-managed variables before applying the new theme, so optional overrides (AI accent, semantics, chart slots) from the previous theme no longer linger after a switch

### Fixed
- `DrylDonutChart` — Hover/focus tooltip is now anchored at the hovered slice's mid-point; previously its percentage anchors were resolved against the full-width wrapper instead of the square donut area, flinging tooltips far off the chart in wide containers and out of it in small ones (the flip-side variant was additionally dead CSS)

## [1.4.0] — 2026-07-09

### Added
- `DrylAiStream` — New `Smooth` parameter: reveals incoming text at a steady, backlog-adaptive pace instead of rendering each chunk as it arrives. Providers that buffer parts of a response (e.g. Ollama withholds a generation while parsing tool-call syntax and then delivers it in one burst) still read as a live stream; genuinely live token streams are not slowed. Off by default

### Changed
- `DrylAgentAttachments` — (Agents) When several display-tool calls arrive in one burst, the attachments now reveal staggered (first immediately, the rest cascading) so each chart/card gets its own entrance instead of all popping in a single frame

## [1.3.0] — 2026-07-09

### Added
- `DrylPresence` — New `Speed` parameter (`PresenceSpeed`: Medium / Fast / Slow) remaps the enter/exit animation onto the fixed duration tokens; default Medium is pixel-identical to before
- `DrylDisplayTools` — (Agents) Factory for six ready-made display `AIFunction` tools (`show_line_chart`, `show_area_chart`, `show_bar_chart`, `show_donut_chart`, `show_stats`, `show_timeline`); tools validate against small typed schemas and return corrective, model-facing errors so the model can retry
- `DrylAgentAttachments` — (Agents) Renders a run's display-tool calls as live DRYL components (charts, KPI stat row, timeline) inline in the chat; each validated attachment glides in via `DrylPresence` (Slow) with the shared Generated reveal

## [1.2.0] — 2026-07-08

### Added
- `DrylIcon` — New `Clock` icon
- `GenerationSnapshot<T>.Raw` — (Agents) Every snapshot now carries the raw accumulated model output (the JSON buffer so far), so UIs can show the live token stream that drives the typed value

## [1.1.0] — 2026-07-07

### Added
- `DrylLineChart` — Multi-series line chart; axes, gridlines, legend, pure-CSS hover tooltips, `Smooth` splines, `ShowMarkers`; AI-Mode
- `DrylBarChart` — Grouped or `Stacked` column chart; ≤ 24px bars, rounded data-ends, surface gaps; AI-Mode
- `DrylAreaChart` — Line chart with same-hue gradient fill to the zero baseline; `Smooth`; AI-Mode
- `DrylDonutChart` — Donut/pie with `InnerRadius`, `CenterContent` slot, per-segment CSS hover; AI-Mode
- `ChartSeries` / `ChartSegment` — Typed data records for the chart family; optional `ColorSlot` pins a series to a palette slot so filters never repaint survivors
- CSS tokens `--chart-1` … `--chart-6` — Categorical series palette, CVD-validated for the dark surface (adjacent ΔE ≥ 12, contrast ≥ 3:1)
- Responsive foundation — `Breakpoint` scale (Sm/Md/Lg/Xl), `.cq` container-query utility, and a global safety layer (media `max-width:100%`, flex `min-width:0`, word-wrap) so DRYL UIs resist horizontal overflow on small screens
- `DrylGrid` — Responsive column grid; auto-fit by default (`MinItemWidth`) or fixed `Columns` with automatic step-down; token-driven `Gap`
- `DrylContainer` — Centers content at a readable max width (`Size`) with responsive side padding so pages are never edge-to-edge on mobile
- `DrylStack` — New `CollapseBelow` (`Breakpoint?`) flips a horizontal stack to vertical below the chosen container width; off by default, no change to existing usage
- `DrylSpacer` — Layout spacer; grows to fill by default or a fixed `Size` from the spacing scale
- `DrylAspectRatio` — Holds a fixed ratio (Square / Video / Photo / Wide / Custom) for media and embeds; never exceeds its slot
- `DrylThemeProvider` — Root provider that applies a customizable color theme; place once in the root layout; renders `:root { <seeds> }` inline `<style>` for no-flash first paint (incl. Blazor Server prerender); subscribes to `IDrylThemeService.OnThemeChanged` for animated runtime switches
- `IDrylThemeService` — Runtime theme switching (`SetThemeAsync` / `SetAccentAsync`) with an animated transition; change glides over `--dur-slow` (instant under `prefers-reduced-motion`)
- `DrylTheme` / `DrylThemes` — Strongly-typed themes and curated presets (Nebula default, Ember, Verdant, Mono); set a few seed hues, DRYL derives the rest via `color-mix()`; `DrylTheme` is a composable record (`Accent`, optional `AiAccent`, optional `Semantic`)
- `--ai-a` / `--ai-b` CSS tokens — Optional separate AI accent seeds; default to the brand accent (`--accent-a` / `--accent-b`) for a unified look; set them to diverge AI surfaces from the UI accent (opt-in)
- `DRYL.Components.Agents` — New companion package integrating the Microsoft Agent Framework (`Microsoft.Agents.AI`). Experimental, independently versioned (0.1.0), decoupled from core. The core stays dependency-free
- `AddDrylAgents()` — DI extension registering `DrylAgentRunner` (scoped); call alongside `AddDrylComponents()`
- `DrylAgentRunner` — Starts agent runs and bridges them to DRYL's AI vocabulary; `Start(...)` returns an observable run, `GenerateStreamingAsync<T>(...)` streams typed structured output, `Replay(...)` drives a run from a pre-built update sequence (recorded runs / demos / tests)
- `DrylAgentRun` — Observable run handle (`State`, `Text`, `ToolCalls`, `TextStream`, `OnChange`); drives `AiState` automatically and feeds `DrylAiScope`
- `DrylToolInvocation` — Captured tool/function call with lifecycle-derived `AiState`; maps 1:1 onto the core `DrylToolCall`
- `DrylAgentToolCalls` — Renders an agent run's tool calls via the core `DrylToolCall` (full trace, or `ActiveOnly`)
- `PartialJsonReader<T>` / `JsonPartialRepair` — Tolerant partial-JSON snapshot engine for structured streaming (hold-last-good on parse failure)
- `DrylAiGenerate<T>` / `GenerationSnapshot<T>` — Streams a typed object from raw JSON tokens and renders progressive partial snapshots; mirrors `DrylAiStream`
- `DrylUiTools` — Factory for four human-in-the-loop `AIFunction` tools (`AskChoice`, `AskMultiChoice`, `RequestPermission`, `AskText`) backed by DRYL dialogs, plus an `All` collection
- `DrylAskChoiceDialog` / `DrylAskMultiChoiceDialog` / `DrylAskTextDialog` — Agent-question dialogs (Agents package) composed from core components; `RequestPermission` reuses the core `DrylConfirmDialog`
- `DrylAgentRunner.StartBuild<T>` — Starts a collaborative, iterative artifact build; framework-owned iteration guidance prompt + auto-injected `update_<T>` merge tool drive the model to refine `T` round-by-round via `DrylArtifactRun<T>`
- `DrylAgentRunner.CreateUpdateTool<T>` — Internal factory that generates the typed `update_<T>` (or custom-named) `AIFunction` tool, embedding `T`'s JSON schema in the description so the model knows the artifact shape
- `DrylArtifactRun<T>` — Observable handle for a collaborative build; live progressively-merged `Artifact` + `Round` counter atop the shared run surface
- `DrylBuildOptions` — `MaxRounds` safety cap (default 12), overridable `Guidance` prompt, custom `UpdateToolName`, and `RevealDuration` (per-round progressive-reveal target, default 1.2 s; `TimeSpan.Zero` = atomic merge)
- `DrylAiBuild<T>` / `ArtifactSnapshot<T>` — Renders the live artifact; each `update_<T>` round materializes progressively (a guided, type-as-you-go reveal) over `DrylBuildOptions.RevealDuration` — the round's new/changed fields type in while earlier fields stay stable, with the `Streaming` aura shown during the reveal (parallel to `DrylAiGenerate<T>`)
- `JsonMerge` — Deep-merge engine for partial artifact patches (objects merge recursively, arrays/scalars replace, null/absent leaves existing)
- `DrylRunBase` — Shared run plumbing (text channel, completion, stable `TextStream`, `OnChange`) extracted from `DrylAgentRun`; base for `DrylAgentRun` and `DrylArtifactRun<T>` (public surface of `DrylAgentRun` unchanged)
- `DrylPresence` — New motion primitive; defers a child's unmount until its exit animation finishes (motion.dev-style AnimatePresence). `Transition`: Fade / Scale / SlideUp / SlideDown / SlideLeft / SlideRight; `Appear`, `OnExited`
- `DrylReveal` — New motion primitive; scroll-triggered staggered entrance via IntersectionObserver. `Transition`: Fade / Rise / ScaleIn; `Stagger`, `Once`, `Threshold`
- `dryl.motion` — New JS module (`onExit`, `moveIndicator`, `observe`) powering the motion primitives; reduced-motion aware
- `--reveal-step` — New motion token (60 ms) controlling `DrylReveal`'s per-child stagger step
- `DrylTabs` — New `AnimateIndicator` parameter (default true) to opt out of the gliding underline
- `DrylDepthGlass` — New experimental glass surface that warps in 3D toward the pointer; perspective tilt + layered content/gloss parallax + travelling specular highlight + hover lift (pure CSS transforms); `Intensity` (Subtle / Medium / Strong), `Interactive`; reduced-motion aware
- `DrylCard` — New `Depth` parameter (`DepthGlassIntensity?`) turns a card into a 3D depth-warp surface (same effect as `DrylDepthGlass`); supersedes `Spotlight` when set
- `DrylCommand` / `DrylCommandArgument` — Declarative command + typed arguments hosted in `DrylCommandPalette`; one `OnRun(CommandContext)` serves click, keyboard and AI. Self-register into `ICommandRegistry`
- `ICommandRegistry` / `CommandRegistry` — Scoped registry (registered by `AddDrylComponents()`) feeding the palette from both declarative `DrylCommand`s and consumer code; de-duplicated by `Id`
- `CommandContext` / `CommandArgType` — Execution payload (typed `GetArgument<T>`, cancellation) and argument input/schema types (`Text` / `Number` / `Boolean` / `Choice`)
- `ICommandResolver` / `CommandResolution` — Narrow, AI-free seam: a resolver turns a natural-language query into one command + filled arguments, surfaced as a confirmable top suggestion (human-in-the-loop, never auto-fired)
- `DrylCommandPalette` — New `Resolver`, `HotKey`, `EmptyText`, `MaxResults` and `ChildContent` parameters; hosts `DrylCommand`s, fuzzy-matches the registry alongside the existing `Items`/`SearchProvider`, fills arguments inline, and wears the shared AI aura while a resolver thinks. Existing `Items`/`SearchProvider`/`Ai` API unchanged
- `DrylAiCommandResolver` — (Agents) `ICommandResolver` that exposes each registered command to an agent as an `AIFunction` and resolves one structured tool call with filled arguments — execution deferred to confirmation; destructive commands gated by `DrylConfirmDialog`
- `DrylAiCommandPalette` — (Agents) Convenience wrapper pre-wired with the DI-registered `ICommandResolver`
- `AddDrylCommandResolver(...)` — (Agents) DI helper registering a `DrylAiCommandResolver` from a consumer-supplied `AIAgent`
- `DrylRunBase.Error` / `DrylRunError` — (Agents) A faulted run now surfaces its terminal error (message, exception type, failing step as `Source`) instead of swallowing it; the run settles at `AiState.None` with `Error` set — the same failed-state convention `DrylToolInvocation` uses — and `OnChange` fires with the error in place
- `DrylAgentError` — (Agents) Danger alert for a run's terminal error with an optional `OnRetry` callback; slides in via `DrylPresence` the moment `Run.Error` is set and renders nothing while the run is healthy
- `DrylRunBase.Usage` / `DrylRunUsage` — (Agents) Token usage (prompt / completion / total) accumulated from every `UsageContent` update on the stream; stays null when a provider never reports numbers
- `DrylAgentUsage` — (Agents) Compact badge row showing a run's token usage; fades in on the first report, culture-invariant compact formatting (`1.2k` / `3.4M`)
- `DrylAgentRunner.StartSequential` / `StartConcurrent` — (Agents) Multi-agent flows bundled into one observable run: a sequential handoff chain feeds each agent the previous answer (the flow's `TextStream` carries the final agent's answer) while a concurrent fan-out runs every agent on the same input; per-step usage aggregates onto the flow, and a failing step settles it with the step name as `Error.Source`
- `DrylMultiAgentRun` / `DrylAgentHandoff` / `DrylAgentStep` / `DrylAgentFlow` — (Agents) Observable multi-agent flow handle atop `DrylRunBase`: named steps, each with its own child `DrylAgentRun` (text, tool calls, usage, error) plus `ActiveIndex` for the running step
- `DrylHandoffTrace` — (Agents) Living timeline of a multi-agent run: one lane per agent speaking the shared AI vocabulary (the active lane wears the ai-aura, status via `DrylAiIndicator`, the connector fills as the baton is handed on), per-lane usage badges and error alerts, and an optional `StepContent` slot for each lane's answer

### Changed
- `DrylDialogProvider` — Sequential dialogs (the AI-agent pattern of "close one, immediately open the next") now hand off smoothly: all service dialogs share one persistent backdrop whose fade is interruption-safe (opacity transition instead of a restarting animation), the outgoing dialog cross-fades out while the incoming one enters after a `--dur-fast` beat — no more double dark/blur pulse between dialogs. Each dialog renders in its own `.dialog-layer` above the shared `.dialog-backdrop`
- `dryl.css` — Accent-derived tokens (`--accent-soft`, `--accent-line`, `--glow-accent`, `--glow-soft`, body ambient glow, AI aura) now derive from seed variables via `color-mix()`; the default theme is visually unchanged
- `DrylTabs` — The active underline now glides between tabs on a spring instead of fading in per-tab (set `AnimateIndicator="false"` for the old behaviour)

### Fixed
- `DrylDialogProvider` — A rapidly closed-and-replaced dialog could get "swallowed": its exit `animationend` was lost (keyed rendering was missing, and the removal listener attached after an async interop roundtrip), leaving an invisible full-screen backdrop that ate every click and leaked the body scroll lock. Entries are now keyed by dialog id, a C#-side watchdog finalizes any exit whose animation event never arrives, and exiting overlays are `pointer-events: none` as defense in depth
- `dryl.js` — Closing a dialog no longer steals focus back from a follow-up dialog that already opened (focus is only restored if it still sits inside the closing dialog)
- `DrylDialog` — Non-fullscreen dialogs are now usable on phones: below 640px they dock to the bottom edge as a full-width sheet (top-rounded, height capped via `dvh`); dialog heights use `dvh` with a `vh` fallback so iOS Safari no longer clips the footer behind its toolbar; the footer wraps its actions instead of overflowing and respects the safe-area inset (fullscreen too)
- `DrylAspectRatio` — Fixed the ratio box collapsing to its child's content height inside a `DrylGrid`/flex row (the default `align-items: stretch` silently overrode `aspect-ratio`). The box now opts out of stretch (`align-self: start`) and any direct child fills it edge to edge
- `DrylPopover` / `DrylMenu` — Dropdown panels are now capped to the viewport width (`calc(100vw - …)`) so they can't push off the right edge of a phone screen (`DrylDialog` and `DrylToast` were already constrained)
- `DrylStepper` — A horizontal stepper now scrolls its step track on a narrow slot (each step keeps a readable min width) instead of crushing every label to an ellipsis
- `DrylTabs` — Tabs keep their size while the strip scrolls on a narrow slot (no longer squeezed)
- `DrylDescriptionList` — Multi-column lists collapse to a single column on a narrow slot (container-query driven) so values no longer squeeze on phones
- `DrylPagination` — The numbered page buttons collapse on a narrow slot, leaving first/prev/next/last and the result summary, so the bar no longer overflows on phones
- `DrylAppBar` — The top bar no longer overflows on phones: it tightens its padding and the fixed-width search shrinks to fit instead of clipping off the right edge
- `DrylCard` — Card content now wraps instead of clipping on narrow screens (rows inside a card wrap; children may shrink via the responsive safety layer)
- `DrylLayout` — The app shell now pins the sidebar and top bar in place and scrolls only the main content area. Previously the whole document scrolled, so on pages taller than the viewport the `DrylDrawer` sidebar and `DrylAppBar` scrolled away with the content instead of staying fixed
- `DrylThemeProvider` — Custom themes now also recolor element glows, focus rings, the aurora background, selection states and the dialog glow (previously only buttons/borders followed the theme)
- `DrylDialog` — Dialogs now animate out (scale + fade) on close instead of disappearing instantly; honours `prefers-reduced-motion`

## [1.0.0] — 2026-06-24

First stable release. The public API is now frozen: after 1.0.0, any rename of a
public parameter, event, enum or slot on an existing component — the surface
defined by `CONVENTIONS.md` — is a breaking change (MAJOR bump). 1.0.0 ships the
content of `1.0.0-rc.1` unchanged: the API-freeze event-name audit (board #39),
the JS-interop render-mode audit (#40) and the first wave of behavioural test
coverage for the complex/stateful surfaces (#41). A browsable per-component API
reference (#42) is published alongside it on the docs site.

### Added
- `DrylButton` — New `Class` parameter that merges extra CSS class(es) onto the button's own classes. This is also the fix for a class-clobber bug (see Fixed); a consumer's `class="..."` now binds to `Class` and is merged instead of overriding the button's identity classes. Establishes the library-wide convention (see `CONVENTIONS.md` §2) being rolled out to the remaining components
- **NuGet packaging** — `DRYL.Components` is now a publishable NuGet package with full metadata (id, description, tags, MIT license expression, project/repository URLs, icon, package README and release notes), a symbol package (`.snupkg`), XML documentation and SourceLink-enabled deterministic builds
- **Multi-target framework support** — the library now targets **net8.0, net9.0 and net10.0** (was net10.0 only), with the `Microsoft.AspNetCore.Components.Web` reference pinned per target framework
- `IDrylAiActivityService` / `DrylAiActivityService` — New scoped service (registered by `AddDrylComponents()`) that coordinates `AiState` across components keyed by operation, turning the shared AI vocabulary from a per-component visual into real orchestration. `Begin(key)` returns a disposable `IDrylAiOperation` handle (`Thinking()` / `Streaming()` / `Generated()`; `Dispose()` settles the key back to `None`); `GetState` / `Set` / `Clear` / `OnChanged` round it out. `StreamAsync(key, tokens, onToken, ct)` drives an `IAsyncEnumerable<string>` end-to-end (Thinking → Streaming on first token → Generated on completion) and always settles in a `finally`. Zero external dependency — you bring the token stream (e.g. from `Microsoft.Extensions.AI`); DRYL maps it to the existing `.ai-aura*` primitives. No new `AiState` values, colours, or `dryl.css` changes
- `IDrylAiOperation` — Disposable handle for an in-flight AI operation returned by `IDrylAiActivityService.Begin`
- `DrylAiScope` — New Intelligence component: wrap a region in `<DrylAiScope Key="...">` and every AI-aware component inside it inherits that operation's `AiState` automatically — a button, card and input light up in lockstep while the model works. Tracks `IDrylAiActivityService` by `Key`, or takes an explicit `State` override that needs no service. A component's own `Ai` parameter always wins over the scope
- `DrylAiStream` — New Intelligence component: binds an `IAsyncEnumerable<string>` token stream straight to the UI via a `RenderFragment<AiStreamContext>` (exposing `Text` + `State`), driving `AiState` automatically and settling to `SettleTo` (default `None`) after the `Generated` reveal. Optional `Key` pushes state to `IDrylAiActivityService` so a surrounding `DrylAiScope` reacts; `OnCompleted` fires with the full text. Cancels and restarts cleanly when `Source` changes (CancellationTokenSource disposed on teardown)
- `AiScope` — New cascaded context (Key + State) supplied by `DrylAiScope`, with a static `Resolve(explicitAi, scope)` that defines the one resolution rule (explicit `Ai` wins, otherwise inherit the scope) shared by every consumer
- `AiStreamContext` — New render context for `DrylAiStream`'s child content (`Text`, `State`)
- `DrylAiAware` — New base class (`@inherits DrylAiAware`) giving non-`InputBase` components the opt-in `Ai` parameter plus a cascaded `EffectiveAi` that resolves against a surrounding `DrylAiScope`
- `DrylImage` — New Data component: an intelligent, responsive image surface. Smart defaults remove the usual boilerplate — `loading="lazy"` + `decoding="async"`, an automatic `aspect-ratio` (from `Width`+`Height` or the `Ratio` enum: `Square`/`Video`/`Portrait`/`Wide`) that kills layout shift, `object-fit` cover, a shimmer skeleton while loading (reuses `DrylSkeleton`) and a stylised icon + alt fallback on error. Parameters: `Src`/`Alt` (required), `Width`/`Height`, `Fit` (`Cover`/`Contain`/`Fill`/`None`/`ScaleDown`), `Position` (`Center`/`Top`/`Bottom`/`Left`/`Right`), `Rounded` (`None`/`Sm`/`Md`/`Lg`/`Full`), `Ratio`, `Lazy`, `FallbackSrc`, `FallbackIcon`, `ShowSkeleton`, `Border`, `Shadow`. AI-native: with `Ai` set the **image area itself** reacts — `Active` washes a faint accent over it, `Thinking` drifts a violet→cyan cloud, `Streaming` sharpens from blur (drive it with `@bind`-style `Progress` 0–100 or let it run on a timer via `BlurDuration`), `Generated` reveals with a scale-in — all built only on the shared `.ai-aura*` primitives, no new colours/states/animations. `aria-live="polite"` announces state changes. Scoped CSS only
- `ImageFit` / `ImagePosition` / `ImageRounded` / `ImageRatio` enums — for `DrylImage`
- `DrylList` / `DrylListItem` — New Layout components: a token-driven replacement for ad-hoc `<ul>`/`<ol>` markup. `DrylList` chooses `Ordered` (ol vs ul), a marker `Variant` (`Default` DRYL dot / `Disc` / `Decimal` / `None` / `Dash`), a `Density` (`Compact`/`Default`/`Comfortable`) mapped to the spacing scale, and optional hairline `Dividers` between rows. `DrylListItem` takes an `Icon` (in place of the marker), `Start` / `End` slots (avatar/checkbox · badge/action), `Selected` (accent line + glass tint) and `Disabled` states, and becomes a keyboard-focusable button when given an `OnClick`. Nest a `DrylList` inside an item's content for indented sub-lists with a connector rail. Not AI-aware (a structural primitive, per CLAUDE.md §2.10)
- `ListVariant` / `ListDensity` enums — for `DrylList`
- `dryl.css` — New `.list` / `.list-item` primitives (markers via CSS counters for correct Decimal numbering across nested lists, density custom props, hairline dividers, selected / interactive / disabled states)
- `DrylIcon` — Two new icons: `Image` (lucide: image) and `ImageOff` (lucide: image-off, default `DrylImage` error fallback)
- `DrylAppBar` — New `Elevation` parameter (`Flat` default / `Raised`; Raised lifts the bar with `var(--shadow-md)` + a denser glass tint) and three optional layout slots `Start` / `Center` / `End` that switch the bar from a single flex row to a balanced three-region layout (Start and End flex equally so Center stays optically centred). New `ShowSidebarToggle` renders a desktop-visible button that collapses / expands the sidebar via the shared layout context. Fully backwards-compatible — plain `ChildContent` and the existing mobile hamburger are unchanged
- `DrylDrawer` — Upgraded to a full sidebar. New `Mode` (`SidebarMode`): `Auto` (default, the historical desktop-column / mobile-overlay behaviour), `Static` (always an in-flow column), `Collapsible` (desktop icon-rail collapse via `@bind-Collapsed`), `Pinnable` (collapse state persisted to `localStorage` via `PersistStateKey`) and `Flyout` (always an overlay; closes on `Esc` / backdrop and traps focus, reusing `dryl.modal`). The collapse is a desktop affordance — every non-`Static` mode still becomes the hamburger overlay (backdrop + focus, full labels) below 1024px. New `@bind-Collapsed`, `Width` / `CollapsedWidth` (CSS-length overrides) and pinned `Header` / `Content` / `Footer` slots (a scrolling nav area between a fixed header and footer). Backwards-compatible — `@bind-Open` + plain `ChildContent` keep working
- `SidebarMode` enum — `Auto` / `Static` / `Collapsible` / `Pinnable` / `Flyout` for `DrylDrawer.Mode`
- `AppBarElevation` enum — `Flat` / `Raised` for `DrylAppBar.Elevation`
- `DrylLayout` — New `SidebarWidth` / `SidebarCollapsedWidth` parameters (override the `--sidebar-w` / `--sidebar-collapsed-w` grid-column widths per layout) and app-shell-wide collapse coordination: it reflects the registered drawer's collapsed state onto the grid (`.is-sidebar-collapsed`) so the body reflows in step with the sidebar's icon-rail animation
- `DrylLayoutContext` — New `SidebarCollapsed`, `CanCollapseSidebar` and `ToggleSidebarAsync()` so a `DrylAppBar` button (or any consumer) can collapse the sidebar without wiring state by hand
- `dryl.css` — New app-chrome dimension tokens `--appbar-h` (60px), `--sidebar-w` (260px) and `--sidebar-collapsed-w` (56px), now consumed by `.topbar` / `.app-shell` / `.sidebar`. New `.topbar.is-raised` + `.topbar-start` / `-center` / `-end` slot primitives, `.app-shell.is-sidebar-collapsed`, and sidebar primitives `.sidebar--static` / `--flyout`, `.sidebar.is-collapsed` (icon rail), `.sidebar-header` / `-content` / `-footer`, `.sidebar-backdrop--flyout` and `.sidebar-toggle`
- `DrylNotifications` — New Feedback component: a bell trigger with an unread-count badge plus a popover inbox panel (built on `DrylPopover`) listing notifications with a leading icon chip, title, message, relative "x ago" time, unread dot, per-item dismiss, "Mark all read" and "Clear all". Empty state via `DrylEmptyState`. Works **service-driven** (bind to the new `IDrylNotificationService` — push entries from background jobs / AI completions and the badge updates live) or **controlled** (pass `Items` + `OnMarkRead` / `OnMarkAllRead` / `OnRemove` / `OnClear`). AI-aware per entry: a `DrylNotification` with `Ai != None` carries the shared `.ai-aura` (ideal for "Your report was generated" / "Agent task finished"). Scoped CSS only. Accessible bell (`aria-haspopup` / `aria-expanded` / live unread count) and `role="dialog"` panel
- `IDrylNotificationService` / `DrylNotificationService` — New scoped service (registered by `AddDrylComponents()`): `Add` / `MarkRead` / `MarkAllRead` / `Remove` / `Clear`, `Notifications`, `UnreadCount`, `OnChanged`
- `DrylNotification` — New model: `Id`, `Title`, `Message`, `Icon`, `Timestamp`, `Read`, `Ai`
- `DrylIcon` — Two new icons: `BellOff` (lucide: bell-off, notifications empty state) and `CheckCheck` (lucide: check-check, mark-all-read)
- `DrylTable` — Resizable, reorderable & pinned columns: new `ResizableColumns` adds a drag handle to each header's right edge (pointer-driven, widths reported back to .NET and persisted); `ReorderableColumns` lets users drag a header onto another — or focus it and press `Alt`+`Arrow Left`/`Right` — to reorder, with focus following the moved column. New `DrylColumn.Pinned` (`ColumnPin.Start`/`End`) freezes a column to an edge during horizontal scroll (sticky, opaque backing, edge rule), and per-column `Resizable` / `Reorderable` opt-outs (pinned columns never reorder). Reordering is confined to a pin group. Widths and order persist via `PersistStateKey`. New `dryl.table` helpers (`initColumnResize` / `disposeColumnResize` / `layoutPinned` / `focusHeader`) — no npm
- `ColumnPin` enum — `None` / `Start` / `End` for `DrylColumn.Pinned`
- `DrylColumn<TItem>` — New `Pinned`, `Resizable` and `Reorderable` parameters
- `dryl.css` — New `.tbl-pin` (+ `-start` / `-end`), `.tbl-col-resize`, `.tbl-resizing` and `.tbl-th--col-dragging` / `--col-drop-target` primitives for frozen columns, resize grips and column drag/drop
- `DrylTable` — Inline editing: new `Editable` (bool) plus a per-column `EditTemplate` (`RenderFragment<TItem>`) turn rows into inline editors that reuse the existing DRYL inputs. `EditMode` (`Row` default / `Cell`) chooses whether the whole row or a single cell edits; double-clicking a row/cell or pressing the pencil affordance starts editing, **Enter** commits and **Escape** cancels (handled on the row). Commits raise `OnRowCommitted` (`EventCallback<RowEditEventArgs<TItem>>`); `OnRowCancelled` carries the original row. An optional `CloneRow` (`Func<TItem,TItem>`) edits an isolated working copy so cancel reverts cleanly. Editable cells get the first editor auto-focused via a tiny `dryl.table.focusFirstEditor` helper (no npm); commit/cancel/pencil buttons carry `aria-label`s. Client-only — ignored (with a console warning) under `DataProvider`
- `TableEditMode` enum — `Row` / `Cell` granularity for `DrylTable` inline editing
- `RowEditEventArgs<TItem>` — New record (`Item` original / `EditedItem` working copy) carrying a committed `DrylTable` inline edit
- `DrylColumn<TItem>` — New `EditTemplate` parameter supplying the inline editor for a column
- `dryl.css` — New `.tbl-row--editing`, `.tbl-td-editing` and `.tbl-edit-btn` (+ `--commit` / `--cancel`) primitives for the inline-editing row, editor cell and commit/cancel/pencil affordances
- `dryl.js` — `dryl.table` gains `focusFirstEditor` (focuses + selects the first control in the editing row)
- `DrylSegmentedControl<TValue>` / `DrylSegment<TValue>` — New Inputs components: a compact iOS-style segmented switcher for exclusive view / mode selection in toolbars and headers (List / Board / Calendar, Day / Week / Month). A glass track holds equal-width segments (CSS grid, so widths stay equal even when the track shrink-wraps) with a single accent indicator that glides between them on `--ease-spring` — pure CSS, no JS. Lightweight `@bind-Value` (generic `TValue`, not an `EditForm` input) and carries no panel, unlike `DrylTabs`. `Size` (`Small`/`Medium`/`Large`), `Block`, per-segment `Icon` / `Label` / custom `ChildContent` / `Disabled`. Accessible `role="radiogroup"` + `role="radio"` with roving tabindex, Arrow/Home/End keyboard navigation (skips disabled), programmatic focus move, and a `:focus-visible` accent ring. Not AI-aware (a neutral mode switch, per CLAUDE.md §2.10). Scoped CSS only — no `dryl.css` changes
- `SegmentedSize` enum — `Small` / `Medium` / `Large` for `DrylSegmentedControl`
- `DrylTypo` — New Layout component: a strongly-typed typography primitive. `Variant` (`H1`/`H2`/`H3`/`H4`/`Lead`/`Body`/`Caption`/`Eyebrow`) drives the look while `As` independently chooses the rendered HTML tag, so an H2-styled heading can be a semantic `<h1>`. `Color` maps to the `--fg*` tokens, plus `Align` and a `Gradient` flag (reuses the shared `.gradient-text` primitive). Not AI-aware. New scoped `.typo-*` classes mirror the dryl.css type scale so the look rides a class, independent of the tag
- `DrylStack` — New Layout component: a flex layout primitive replacing ad-hoc `.row`/`.col`/`.between` markup. `Direction` (`Vertical`/`Horizontal`), token-driven `Gap` (`None`…`Xxl` → `--sp-*`), `Align`, `Justify` and `Wrap`. Token-only inline styling, no CSS. Not AI-aware
- `DrylDivider` — New Layout component: a thin separating rule. `Orientation` (`Horizontal`/`Vertical`, reusing the global `.divider`/`.divider-v` primitives) plus an optional centred label via `ChildContent` for the "— or —" pattern; `role="separator"`. Not AI-aware. New scoped `.divider-labelled*` classes for the labelled variant
- `dryl.css` — New `--z-popover: 150` layering token (between `--z-modal` and `--z-toast`) so portaled `DrylPopover` panels render above page content and modals, but below toasts
- `dryl.js` — New `dryl.popover` namespace (`open` / `close`): portals a popover panel to `<body>`, positions it with `position: fixed` against the viewport (placement, flip/clamp, reposition on scroll/resize) and handles click-outside accounting for the portaled panel. Replaces `DrylPopover`'s use of `dryl.menu.attach`
- `DrylButtonGroup` — New Actions component: visually joins related `DrylButton`s into one segmented control (flattened inner corners, merged 1px borders, outer radius preserved across sizes). Works as a clustered toolbar or, with each button's `Pressed`, an exclusive toggle group (reuses the shared `btn--active` surface — no new toggle state invented). `AriaLabel`, `Block`; `role="group"`
- `DrylSplitButton` — New Actions component: a primary action joined to a caret that opens a `DrylMenu` of secondary variants (the "Save ▾ / Save & new / Save & close" pattern), composed from `DrylButton` + `DrylMenu`. `Variant` / `Size` (shared by both segments), `LeadingIcon`, `OnClick`, `MenuItems` slot, `MenuPlacement`, `MenuLabel`, `MenuAriaLabel`, `Block`, AI-aware (`Ai` on the main button); the caret is a labelled icon button
- `dryl.css` — New `.btn-group` / `.btn-group--block` and `.split-btn` / `.split-btn--block` primitives that connect adjacent buttons into a segmented outline
- `DrylErrorBoundary` — New Feedback component: a glass error-fallback surface around Blazor's built-in `ErrorBoundary`. When the protected content throws during render/lifecycle, the default unstyled markup is replaced by a danger `DrylAlert` with `Title`, `Description`, an optional dev-only collapsible stack-trace toggle (`ShowDetails`) and a retry button that recovers the boundary (`ShowRetry` / `RetryText` / `OnRetry`). AI-aware via `Ai` (the fallback carries the shared aura — ideal for failed AI blocks); `FallbackContent` fully overrides the surface and receives the caught `Exception`; public `Recover()` for programmatic recovery
- `DrylIcon` — New `Refresh` icon (lucide: rotate-ccw) for the error-boundary retry action
- `DrylTable` — New `Reorderable` (bool, default `false`) and `OnRowReordered` (`EventCallback<RowReorderEventArgs>`) enable manual row reordering via a leading grip-handle column. Drag a handle to move a row, or focus it and press `Alt`+`Arrow Up`/`Arrow Down` for a keyboard-accessible move (focus follows the moved row). The table updates its displayed order immediately and raises `OnRowReordered` so consumers can persist the new order. Requires a plain client list — ignored (with a console warning) under `Virtualize`, `GroupBy` or `DataProvider`, and the handle is disabled while a sort is active. No npm — native HTML5 drag events plus a tiny `dryl.table.focusGrip` helper
- `RowReorderEventArgs` — New record (`OldIndex` / `NewIndex`, view-relative) carrying a `DrylTable` row move
- `DrylIcon` — New `GripVertical` icon (lucide: grip-vertical) for the table reorder handle
- `dryl.css` — New `.tbl-col-grip` / `.tbl-grip` primitives and `.tbl-row--dragging` / `.tbl-row--drop-target` row states for the reorder handle and drag affordance
- `dryl.js` — New `dryl.table` namespace (`focusGrip`) restores focus to the reorder handle after a keyboard row-move
- `DrylToolCall` — New AI component: visualises a single agent tool / function call — tool name, a live status pill (`DrylAiIndicator`) and a collapsible body holding arguments / result as JSON (`DrylCodeBlock`). Status uses the shared `AiState` vocabulary (`Thinking`=running, `Streaming`, `Generated`=done); `Error` shows a danger `DrylAlert`. Stack inside a `DrylTimeline` for a full agent trace
- `DrylCitation` — New Data component: inline source-attribution chip (`[n]`) that reveals title / URL / snippet in a `DrylPopover`; for verifiable RAG answers. `Index`, `Title`, `Url`, `Snippet`; accessible `<button>` trigger
- `DrylCitationList` / `DrylCitationListItem` — New Data components: the numbered source list that complements the inline chips (`<ol>` semantics, external links)
- `DrylMarkdown` — New Surfaces component: renders Markdown (CommonMark + GFM via Markdig) into the DRYL glass aesthetic. Fenced code blocks are delegated to `DrylCodeBlock`; all other content is rendered with **raw HTML disabled** so model-authored markup is escaped rather than executed. Re-renders as tokens arrive (streaming). `Content`, `Ai`. New global `.md` / `.md-content` CSS primitives
- `Markdig` — New (and only) external runtime dependency, added to power `DrylMarkdown`. Documented exception to CLAUDE.md rule 2.8; see `THIRD_PARTY_NOTICES.md` (BSD-2-Clause)
- `DrylCodeBlock` — New Data component: glass code surface with a language label and copy-to-clipboard button (`Code`, `Language`, `ShowLineNumbers`, `Ai`). Code is rendered text-only (HTML-encoded). Consumed by `DrylMarkdown` for fenced code blocks. AI-aware (Streaming glow)
- `dryl.js` — New `dryl.clipboard` namespace (`copy`): writes text to the clipboard via the async Clipboard API with an `execCommand` fallback; returns success so callers can show copied/failed feedback
- `DrylIcon` — Three new icons: `Link` (lucide: link), `Quote` (lucide: quote), `Wrench` (lucide: wrench) for the new AI components
- `dryl.js` — New `dryl.keynav` namespace (`attach` / `detach`): suppresses default page-scroll for navigation keys on a host element. `dryl.tree` is now a backwards-compatible alias of it; `DrylSelect` reuses it for its combobox trigger
- `DrylScrollArea` — New Layout component: a container-scoped scrollable region with the DRYL thin accent scrollbar (`MaxHeight`, `MaxWidth`, `Horizontal`). Pure CSS, no JS — for sidebars, log viewers, code blocks and long lists
- `DrylKbd` — New Data component for keyboard-shortcut display: renders semantic `<kbd>` chips; single key via content (`<DrylKbd>⌘K</DrylKbd>`) or a chord via `Keys` (`Keys="@(new[]{"Ctrl","K"})"`) joined by `Separator`. Token-based styling, no JS
- `DrylTable` — New `ShowExport` (bool, default `false`) and `ExportFileName` (string, default `"export.csv"`) parameters add a CSV export button to the toolbar. The export honours the active search, filters and sort, includes only the visible columns, and (in client mode) covers the full filtered result set across all pages. No npm — download is produced via a Blob URL (`dryl.download`); a UTF-8 BOM and `InvariantCulture` number formatting keep it Excel- and locale-safe
- `dryl.js` — New `dryl.download` namespace (`text` / `csv`) triggers a client-side file download via a transient Blob URL
- `dryl.css` — New `.tbl-toolbar-action` / `.tbl-toolbar-action--auto` primitives position toolbar action buttons (e.g. CSV export)
- `DrylEmptyState` — Now AI-aware: new `Ai` parameter (`AiState`, default `None`) drives the shared aura (ring / glow / Generated wash); in AI mode the placeholder gains a glass surface so the ring frames it. Demo page extended with Thinking / Streaming / Generated examples
- `DrylButton` — New `Pressed` parameter (`bool?`, default `null`) for toggle buttons (mute / bold / filter on-off): emits `aria-pressed` and highlights the button via the new `.btn--active` surface while pressed
- `dryl.css` — New `.btn--active` primitive (accent border + glow) for pressed/toggled buttons
- `DrylPopover` — Anchored floating-panel primitive; `@bind-Open`; `TriggerContent` / `PanelContent` slots; `Placement` (BottomStart / BottomEnd / TopStart / TopEnd); `MatchTriggerWidth`; `Block`; `CloseOnClickOutside` / `CloseOnEscape`; optional glass `Surface`; exposes `PanelElement` / `AnchorElement` for panel-scoped interop
- `DrylEmptyState` — "No data" placeholder; `Icon`, `Title`, `Description`, `ActionContent` slot; `Size` (Small / Medium)
- `DrylDescriptionList` — Semantic `<dl>` key/value view; `Layout` (Stacked / Inline); `Columns`
- `DrylDescriptionItem` — Single term/value pair; `Term`, `Icon`, value content
- `DrylFormField` — Generic `<TValue>` label + required marker + hint + inline validation wrapper for any input; `For` expression binds validation messages within an `EditForm`
- `DrylValidationSummary` — Glass-styled summary of all `EditContext` validation errors; subscribes to validation-state changes
- `PopoverPlacement` enum — `BottomStart` / `BottomEnd` / `TopStart` / `TopEnd` for `DrylPopover`
- `EmptyStateSize` enum — `Small` / `Medium` for `DrylEmptyState`
- `DescriptionLayout` enum — `Stacked` / `Inline` for `DrylDescriptionList`
- `DrylSparkline` — Tiny inline-SVG trend chart (zero JS); `Line` / `Area` / `Bar`; `Width` / `Height`; `ShowLastDot`; accent-gradient stroke/fill; all coordinates formatted with `InvariantCulture`
- `DrylStat` — KPI / metric card on a glass surface; `Label`, `Value`, `Icon`, `Delta` + `Direction` (Up / Down / Neutral) chip; `Sparkline` slot; AI-aware
- `DrylTimeline` — Vertical event sequence; draws the connecting rail for child `DrylTimelineItem`s; `role="list"`
- `DrylTimelineItem` — Single event; variant-tinted marker (Default / Accent / Success / Warning / Danger), `Title`, `Timestamp`, `Icon`, body; AI-aware marker (agent step traces)
- `DrylTreeView` — Hierarchical tree; declarative `DrylTreeNode` children; `@bind-SelectedValue`; roving-tabindex focus; full WAI-ARIA tree keyboard nav (arrows expand/collapse/move, Home/End, Enter/Space); `role="tree"`
- `DrylTreeNode` — Tree node; `Text`, `Icon`, `Value`, `@bind-Expanded`, `Disabled`; chevron toggle; nests further nodes
- `SparklineKind` enum — `Line` / `Area` / `Bar` for `DrylSparkline`
- `DeltaDirection` enum — `None` / `Up` / `Down` / `Neutral` for `DrylStat`
- `TimelineVariant` enum — `Default` / `Accent` / `Success` / `Warning` / `Danger` for `DrylTimelineItem`
- `dryl.js` — New `dryl.tree` namespace: `attach` / `detach` prevent default page-scroll for tree navigation keys (Tab left untouched)
- `DrylAvatar` — User / entity face; image with initials/icon/generic fallback; `Size` (Small / Medium / Large); `Shape` (Circle / Square); presence `Status` dot (Online / Busy / Away / Offline); initials derived from `Name`
- `DrylAvatarGroup` — Overlapping avatar stack; cascades `Size` to children; `Max` collapses overflow into a `+N` tile
- `DrylBreadcrumbs` — Hierarchical navigation trail; child `DrylBreadcrumbItem` registration; custom `Separator`; `MaxItems` collapses the middle into an ellipsis; `<nav>/<ol>` semantics, last crumb `aria-current="page"`
- `DrylBreadcrumbItem` — Single crumb; `Href` (link) or plain text; optional leading `Icon`
- `DrylProgress` — Linear progress bar; determinate or `Indeterminate` sweep; `Variant` (Accent / Success / Warning / Danger); `Size` (Small / Medium / Large); `ShowLabel` percentage; `role="progressbar"` ARIA; AI-aware
- `DrylChat` — Conversation surface; scrollable message log + pinned `Footer` composer slot; `Height`; `AutoScroll` via `dryl.chat.scrollToEnd`; `role="log"` + `aria-live="polite"`; AI-aware
- `DrylMessage` — Chat bubble; `Role` (User / Assistant / System) drives alignment & styling; `Author`, `Timestamp`, avatar slot, `Typing` dots; AI-aware
- `DrylChatComposer` — Chat input; `@bind-Value`; `OnSend`; Enter sends, Shift+Enter newline, auto-grow textarea via `dryl.chat.attachComposer`; AI-aware
- `AvatarSize` / `AvatarShape` / `AvatarStatus` enums for `DrylAvatar`
- `ProgressVariant` / `ProgressSize` enums for `DrylProgress`
- `MessageRole` enum — `User` / `Assistant` / `System` for `DrylMessage`
- `dryl.js` — New `dryl.chat` namespace: `scrollToEnd`, `attachComposer` (Enter-to-send + auto-grow), `detachComposer`, `resize`
- `DrylChipInput` — Free-text tag field; chips created on Enter / comma; Backspace removes last chip; `@bind-Tags` (`IReadOnlyList<string>`); `MaxTags`; AI-aware
- `DrylRating` — Star rating input inheriting `InputBase<int?>`; configurable `MaxStars`; hover preview; `AllowClear`; `ReadOnly`; keyboard navigation (arrows, Home, End); EditForm / DataAnnotations validation; AI-aware
- `DrylInputOtp` — Fixed-box OTP/2FA code entry inheriting `InputBase<string>`; configurable `Digits` (default 6); auto-focus advance; paste-to-fill via `dryl.otp` JS helper; AI-aware
- `DrylTimePicker` — Time-only picker inheriting `InputBase<TimeOnly?>`; scrollable hour/minute panel; `Min`/`Max`; `MinuteStep` (1, 5, 10, 15, 30…); Escape/Enter keyboard support; AI-aware
- `DrylInputMask` — Masked input inheriting `InputBase<string>`; predefined `MaskType` (Phone / Iban / PostalCode / CreditCard) or `CustomPattern` (`#` = digit, `A` = letter); formatting enforced via `dryl.inputmask` JS helper (input + paste); `LeadingIcon` slot; AI-aware
- `MaskType` enum — `Phone` / `Iban` / `PostalCode` / `CreditCard` / `Custom` for `DrylInputMask`
- `dryl.js` — Three new namespaces: `dryl.otp` (focusNext, focusPrev, attach/paste), `dryl.timepicker` (click-outside attach/detach, scrollToActive), `dryl.inputmask` (format-on-input attach/detach, paste)
- `DrylIcon` — Sechs neue Icons: `Circle` (lucide: circle), `Command` (lucide: command), `Hash` (lucide: hash), `List` (lucide: list), `Sliders` (lucide: sliders-horizontal), `Upload` (lucide: upload); werden in der Demo-Navigationsleiste verwendet

### Changed
- **BREAKING (API freeze)** `DrylExpansion` — Renamed `IsOpen` / `IsOpenChanged` → `Open` / `OpenChanged` to follow the no-`Is` boolean convention (`CONVENTIONS.md` §2/§3). Update `@bind-IsOpen` → `@bind-Open`. Resolves the last of the board #39 event-name deviations
- **BREAKING (API freeze)** `DrylPagination` — Renamed the page/size events to the bindable `<Property>Changed` form (`CONVENTIONS.md` §3): `OnPageChanged` → `CurrentPageChanged` and `OnPageSizeChanged` → `PageSizeChanged`. Both pair with their property for `@bind-CurrentPage` / `@bind-PageSize`
- **BREAKING (API freeze)** `DrylTable` — Normalised pagination to match `PageSize`/`PageSizeChanged`: the current page is now the two-way bindable `Page` / `PageChanged` (replaces the notification-style `OnPageChanged`). Use `@bind-Page` to control or observe the page; `PageSize`/`PageSizeChanged` are unchanged
- `DrylButton`, `DrylCard`, `DrylMessage`, `DrylChat`, `DrylInputText`, `DrylTextarea`, `DrylAutocomplete`, `DrylSelect` — These AI-aware components now inherit their `AiState` from a surrounding `DrylAiScope` when no explicit `Ai` is set, so a single operation can light them up together. An explicit `Ai` still wins, and with no scope present behaviour is unchanged. The four non-`InputBase` ones (`DrylButton`, `DrylCard`, `DrylMessage`, `DrylChat`) now derive from the new `DrylAiAware` base class. **No public API change**
- `DrylCodeBlock` — Now syntax-highlights code server-side via a tiny dependency-free C# tokenizer (no JS, no npm — CLAUDE.md rules 2.1 / 2.8). Token colors map only onto existing DRYL tokens (keyword→`--accent-a`, type→`--accent-b`, string→`--success`, number→`--warning`, comment→`--fg-faint`, punctuation→`--fg-muted`). Languages: `csharp`, `javascript`/`typescript`, `json`, `html`/`xml`, `css`, `bash`, `sql`, `python` (with common aliases); unknown languages fall back to plain text. Every token is HTML-encoded before wrapping, so model-authored code stays injection-safe. New `Highlight` parameter (bool, default `true`) opts out to verbatim plain text. Highlighting also flows automatically through `DrylMarkdown` fenced code blocks. **No breaking change**
- `DrylMessage` — New optional `Text` (string) and `Markdown` (bool) parameters: when `Text` is set it takes precedence over `ChildContent`, and with `Markdown="true"` it is rendered through `DrylMarkdown` (formatted Markdown + code blocks) — ideal for streaming LLM output. Defaults keep existing `ChildContent` usages unchanged. **No breaking change**
- `DrylButton` — Tactile "Sheen & Spring" interaction polish: a soft light reflection sweeps across the surface on hover (all variants except Ghost), the press now drops-and-shrinks with a spring-back release (`--ease-spring`), and icons animate on hover (trailing slides forward, leading pops, icon-only scales). Leading/trailing icons gain marker classes `btn-ico-lead` / `btn-ico-trail`. All token-driven; honours `prefers-reduced-motion`. **No public API change**
- `DrylMenu`, `DrylSelect`, `DrylAutocomplete`, `DrylTimePicker`, `DrylDatePicker` — Refactored onto the shared `DrylPopover` primitive for anchoring, positioning and click-outside dismissal; the duplicated panel-positioning CSS (`position:absolute; top:calc(100% + var(--sp-1)); …`) and per-component `dryl.menu.attach` boilerplate were removed. **No public API change** — parameters, keyboard navigation, ARIA and visuals are unchanged
- `dryl.js` — `dryl.menu.focusTrigger` now also matches a trigger inside `.popover-trigger` (used by the refactored dropdowns)
- `DrylSelect` — Replaced native `<select>` element with a fully custom dropdown; API changed from `ChildContent` (`<option>` elements) to `Items` (`IEnumerable<SelectItem>`); panel and option styling now matches `DrylAutocomplete` (glass background, accent scrollbar, selected-item dot); `Placeholder` parameter added; click-outside detection via `dryl.menu.attach`; keyboard navigation (ArrowDown/Up, Enter, Space, Escape, Tab)
- `DrylNavGroup` — New `Collapsible` parameter (bool, default `false`) enables accordion-style sub-menus with CSS grid animate-in/out; `DefaultExpanded` (bool, default `true`) sets initial state; `Href` parameter makes the header a `NavLink` while a separate chevron button controls collapse; `Icon` parameter adds a leading icon to the collapsible header
- `DrylNavLink` — New `Sub` parameter (bool, default `false`) renders the item indented (`.nav-item--sub`) for use inside collapsible `DrylNavGroup` children
- `dryl.css` — New primitives for collapsible nav: `.nav-scroll` (scrollable sidebar middle area), `.nav-section-toggle`, `.nav-section-header`, `.nav-section-link`, `.nav-section-chevron-btn`, `.nav-section-chevron`, `.nav-children`, `.nav-children-inner`, `.nav-item--sub`

### Fixed
- `DrylFileUpload`, `DrylMultiSelect` — `DisposeAsync` no longer throws a `JSDisconnectedException` when the Blazor Server circuit is already gone at teardown. Both guarded the prerender case (`_jsReady`) but called `dryl.*.detach` without catching a disconnected circuit; the detach is now wrapped in `try { … } catch (JSDisconnectedException) catch (JSException)` like the other interop components. Closes the render-mode audit (board #40): all 20 JS-interop components are now verified prerender-safe (no JS before first interactive render) with a guarded, disconnect-tolerant `DisposeAsync`
- Surfaces (`DrylCard`, `DrylChat`, `DrylDialog`, `DrylMarkdown`, `DrylMessage`, `DrylPopover`, `DrylToast`) — Same consumer-`class` clobber fix: each now exposes a merged `Class` parameter. With this, **all 61 components that accept pass-through attributes now merge a consumer `class` instead of clobbering their identity classes** — the library-wide convention (`CONVENTIONS.md` §2) is complete
- Navigation (`DrylBreadcrumbs`, `DrylMenu`, `DrylMenuItem`) — Same consumer-`class` clobber fix: each now exposes a merged `Class` parameter (`DrylMenu` forwards it to its `DrylPopover`)
- Layout (`DrylDivider`, `DrylExpansion`, `DrylList`, `DrylListItem`, `DrylScrollArea`, `DrylStack`, `DrylTypo`) — Same consumer-`class` clobber fix: each now exposes a merged `Class` parameter (`DrylListItem` additionally now splats its `AdditionalAttributes`, previously captured but never rendered)
- Inputs (`DrylChipInput`, `DrylFormField`, `DrylValidationSummary`) — Same consumer-`class` clobber fix: each now exposes a merged `Class` parameter applied to its field root
- Feedback components (`DrylAlert`, `DrylEmptyState`, `DrylErrorBoundary`, `DrylNotifications`, `DrylProgress`, `DrylSkeleton`, `DrylSpinner`, `DrylTooltip`) — Same consumer-`class` clobber fix: each now exposes a merged `Class` parameter (`DrylErrorBoundary` forwards it to its fallback `DrylAlert`)
- Data components (`DrylAvatar`, `DrylAvatarGroup`, `DrylBadge`, `DrylCitation`, `DrylCitationList`, `DrylCitationListItem`, `DrylCodeBlock`, `DrylDescriptionList`, `DrylImage`, `DrylKbd`, `DrylPagination`, `DrylSparkline`, `DrylStat`, `DrylTable`, `DrylTableKpi`, `DrylTimeline`, `DrylTimelineItem`, `DrylTreeView`) — Same consumer-`class` clobber fix: each now exposes a merged `Class` parameter so a consumer's `class="..."` is folded into the component's own classes instead of overriding them. (`DrylTableKpi` additionally now splats its `AdditionalAttributes`, which were previously captured but never rendered)
- `DrylAiIndicator`, `DrylToolCall` — Same consumer-`class` clobber fix: both now expose a merged `Class` parameter
- `DrylButtonGroup`, `DrylSplitButton` — Same consumer-`class` clobber fix as `DrylButton`: both now expose a merged `Class` parameter so a consumer's `class="..."` is folded into the group/split-button classes instead of overriding them
- `DrylButton` — A consumer-supplied `class="..."` no longer wipes the button's own classes. Passing a class through the `@attributes` splat overrode the explicit `class="btn btn-primary …"` (Blazor's splat clobbers a same-element `class`, it does not merge), so `<DrylButton class="mt-4">` rendered `class="mt-4"` and lost all button styling. A new merged `Class` parameter now captures it (Blazor matches `class`→`Class` case-insensitively) and folds it in. The same convention is being rolled out to the other components that expose `AdditionalAttributes` without a `Class` parameter
- `DrylTable` — Persisted column widths are now formatted with `InvariantCulture`. A resized column wider than 999px previously rendered an invalid CSS width (e.g. `1.200px`) under group-separator locales such as German, collapsing the column; widths now always use a `.`-free integer pixel value
- `DrylButton` — A disabled button is now visually distinct from an enabled one. There was no `.btn:disabled` rule at all, so a disabled primary kept its full accent gradient and glow and read as active (WCAG 1.4.1 — state not conveyed). Disabled buttons are now dimmed (`opacity: 0.45`), desaturated, stripped of the accent glow, flattened (no lift) and show a `not-allowed` cursor, across all variants
- `DrylFileUpload` — AI mode (`Ai != None`) no longer draws a square-cornered ring/glow around the rounded drop zone. The wrapper had no radius, so the `.ai-aura-ring`/`.ai-aura-glow` `border-radius:inherit` resolved to `0`; the wrapper now carries `border-radius: var(--r-lg)` (matching `.file-drop`) via a stable `.file-upload-wrapper` class, so the rotating ring traces the rounded corners — mirroring the existing input/textarea/radio-group wrapper rules
- `DrylTable` — Global search bar no longer renders the magnifier icon floating outside a left-padded box (icon, gap, then placeholder text starting far inside). The `.input-icon` absolute-positioning rule was scoped to `.input-wrapper` only, so in the table toolbar's `.tbl-search` host the icon stayed inline while `.has-leading-icon` still reserved 38px of padding. `.tbl-search` now shares the same icon positioning, so the search reads as one cohesive field
- Inputs (`DrylInputText`, `DrylTextarea`, `DrylSelect`, `DrylChipInput`, `DrylInputOtp` and every component using the shared `.input`/`.textarea`/`.select` primitives) — Focused fields no longer snap their corners from `var(--r-md)` (14px) to a near-square 4px. The global `:focus-visible` rule was clobbering each component's `border-radius` with a forced `4px` (same specificity, declared later in the cascade); that declaration is removed and the outline now follows the element's own radius. The focus glow also gains a flush `0 0 0 1px var(--accent-line)` ring that fuses with the border into one crisp accent edge instead of a thin line with a detached halo
- `DrylPopover` — Panel is no longer clipped by an ancestor's `overflow: hidden` or `backdrop-filter` (e.g. a `DrylCard`/glass surface). It now portals to `<body>` and positions itself with `position: fixed` against the viewport (placement maths, viewport flip/clamp, reposition on scroll/resize in `dryl.popover`), so e.g. a `DrylCitation` chip inside a card opens fully visible above the prose. Fixes the same clipping for every consumer (`DrylMenu`, `DrylSelect`, `DrylAutocomplete`, `DrylDatePicker`, `DrylTimePicker`) and lets a popover opened inside a `DrylDialog` render above the modal. The panel wrapper is now always present (content still gated by open state) so Blazor never removes a portaled node — avoiding a Blazor-Server crash on consumer-driven close. **No public API change**
- `DrylMarkdown` — AI mode (`Ai != None`) no longer draws a sharp-cornered rectangle hugging the text. The `.md` host had no radius/surface, so the rotating ring's `border-radius:inherit` resolved to `0`; it now gets `background: var(--glass-1)`, `border-radius: var(--r-lg)` and padding so the ring frames a proper glass panel, mirroring `DrylRating` / `DrylEmptyState`
- `DrylTable` — CSV export in server mode (`DataProvider`) now exports the **full filtered result set** across all pages instead of only the currently loaded page. The export issues a dedicated `DataProvider` call carrying the active search / sort / filters with `Skip=0` / `Take=int.MaxValue`; client mode is unchanged
- `DrylSelect` — Navigation keys (Arrow Up/Down, Home/End) no longer scroll the page behind the combobox while moving the option highlight. The trigger now attaches the shared `dryl.keynav` helper (preventDefault for nav keys only; Tab / Enter / Escape stay live so focus can leave the control). Mirrors the `DrylTreeView` behaviour
- `DrylPagination` — Active page button now carries `aria-current="page"` so screen readers can identify the current page (previously only `aria-label="Page N"`, indistinguishable from the others)
- `DrylEmptyState` — Root element is now a labelled `role="region"` (`aria-label` derived from `Title`, falling back to "Empty") so assistive tech can identify and navigate to the empty state
- `DrylSelect` — `ArrowUp` now opens the dropdown when the combobox is focused but closed (highlighting the selected option, or the last option if none), matching `ArrowDown` and the ARIA combobox pattern; previously `ArrowUp` did nothing while closed
- `DrylStepper` — Step headers are now real `<button>` elements: keyboard-focusable and activatable with Enter/Space (previously plain `<div @onclick>`, unreachable by keyboard — WCAG 2.1.1). Active header carries `aria-current="step"` and a visually-hidden `aria-live="polite"` region announces the active step
- `DrylCard` — Spotlight `mousemove` listener is now removed on dispose (`dryl.spotlight.untrack`); previously the handle was always `null` so the listener leaked on every navigation away from a page using a card
- `DrylInputNumber` — No longer emits spurious `min`/`max`/`step="0"` attributes for non-nullable value types (e.g. `TValue="int"`), which made the browser reject any positive value ("Value must be 0") and the stepper increment by 0. The native constraints are now emitted only when `Min`/`Max`/`Step` are explicitly set
- `DrylFormField` — No longer renders its own validation message (the wrapped DRYL input already shows one), so errors no longer appear twice; its hint is hidden while the field is invalid
- `DrylTimePicker` — Time panel rendered outside the `.ai-aura` (`isolation:isolate`) wrapper so `backdrop-filter` blurs the page correctly instead of the parent's AI glow effects
- `DrylInputOtp` — AI aura now wraps each digit box individually (rotating gradient ring per box, box border hidden in AI mode) instead of spanning the entire group
- `DrylRating` — AI mode wrapper gets `background: var(--glass-1)` so the gradient ring frames a proper glass surface instead of floating around bare stars
- All `InputBase<T>`-derived components (`DrylInputText`, `DrylInputPassword`, `DrylTextarea`, `DrylInputNumber`, `DrylRating`, `DrylTimePicker`, `DrylInputOtp`, `DrylInputMask`, `DrylSlider`, `DrylToggle`, `DrylCheckbox`, `DrylRadioGroup`, `DrylSelect`, `DrylAutocomplete`) — overrode `SetParametersAsync` to supply a fallback `ValueExpression` when the component is used with one-way `Value="..."` or no value outside an `EditForm`; previously threw `InvalidOperationException: requires a value for the 'ValueExpression' parameter`
- `DrylIcon` — Added missing `ChevronUp` icon (lucide: chevron-up); was silently rendering an empty SVG when used in `DrylInputNumber`'s stepper
- `DrylInputNumber` — Stepper buttons are now flush with the input: wrapper uses `align-items: stretch` via `.has-stepper`, input squares off its right edge (`border-radius: var(--r-md) 0 0 var(--r-md); border-right: none`), stepper closes the shape with right-side radius; separator border syncs to input hover/focus state; buttons gain `:active` (glass-3 + accent-a) and `:focus-visible` ring; removed erroneous `has-trailing-icon` padding from the stepper mode
- `DrylDatePicker` — Empty calendar cells (leading/trailing padding days) no longer show a hover highlight; hover selector now excludes `.date-cell--empty`
- `DrylDrawer` — Sidebar navigation area is now scrollable when content overflows the viewport height; brand and Project footer remain pinned outside the scroll region

### Added
- `DrylInputPassword` — Password input with show/hide eye toggle; inherits `InputBase<string>`; EditForm / DataAnnotations validation; AI-aware
- `DrylInputNumber<TValue>` — Generic numeric input for `int`, `long`, `float`, `double`, `decimal` and nullable variants; optional `Min` / `Max` / `Step`; optional ± stepper buttons (`ShowStepper`); `inputmode="decimal"` for mobile keyboards; AI-aware; native spinners hidden in favour of custom stepper
- `DrylRadioGroup<TValue>` — Radio button group inheriting `InputBase<TValue>`; `Orientation` (`Vertical` / `Horizontal`); cascades `RadioGroupContext<TValue>` to children; EditForm validation; AI-aware (ring wraps the group)
- `DrylRadio<TValue>` — Single radio option inside `DrylRadioGroup`; receives group context via `[CascadingParameter]`; individual `Disabled` override; accessible `<label>` + visually-hidden `<input type="radio">` pattern
- `DrylMultiSelect` — Multi-selection dropdown; chip display for selected items with `MaxVisibleChips` overflow count; `@bind-SelectedValues` (`IReadOnlyList<string>`); same JS click-outside / keyboard pattern as `DrylSelect`; panel stays open on selection; AI-aware
- `DrylSlider` — Range slider inheriting `InputBase<double>`; `Min` / `Max` / `Step`; accent gradient fill tracks thumb via CSS custom property `--pct` (no JS); `ShowValue` label; AI-aware
- `DrylFileUpload` — Drag-and-drop / click-to-browse file picker built on Blazor `InputFile`; `Multiple` / `Accept` / `MaxFileSizeBytes`; drag-active glow via `dryl.fileupload.attach` JS helper; removable file list; `FilesChanged` event callback; AI-aware
- `RadioGroupOrientation` enum — `Vertical` / `Horizontal` for `DrylRadioGroup`
- `RadioGroupContext<TValue>` — Internal cascading context record used by `DrylRadioGroup` / `DrylRadio`
- `dryl.js` — `window.dryl.fileupload`: `attach` / `detach` for drag-enter/leave/over/drop event management with counter-based tracking to avoid false "drag leave" on child elements
- `dryl.css` — New primitives: `.radio-group` / `.radio-group--vertical` / `.radio-group--horizontal` / `.radio` / `.radio-input` / `.radio-control` / `.radio-label` / `.radio--disabled`; `.chip` / `.chip-text` / `.chip-remove` / `.chip-overflow` / `.multiselect-chips`; `.num-stepper` / `.num-step-btn`; `.file-drop` / `.file-drop--active` / `.file-drop--disabled` / `.file-drop-icon` / `.file-drop-title` / `.file-drop-sub` / `.file-list` / `.file-item` / `.file-item-icon` / `.file-item-name` / `.file-item-size` / `.file-item-remove`; `.slider-wrap` / `.slider-header` / `.slider-value`; native number spinner suppression (`input[type=number]::-webkit-inner-spin-button`)
- `DrylCommandPalette` — Full-screen command launcher overlay; accepts static `Items` or async `SearchProvider` (250 ms debounce); Ctrl+K / Cmd+K global hotkey; category grouping with `CommandItem.Category` (named categories alpha-sorted, ungrouped last); keyboard navigation (Arrow Up/Down, Enter, Escape); three item types: `Navigate` (router), `Action` (callback, closes palette), `AiIntent` (callback, keeps palette open); AI result panel via `Ai` parameter and `AiContent` slot; `@bind-Open` two-way binding; ARIA combobox + listbox pattern with `aria-activedescendant`, `aria-live` AI panel
- `CommandItem` / `CommandItemType` — Model classes for command palette entries (`Label`, `Description`, `Icon`, `Category`, `Type`, `Href`, `Action`, `AiAction`)
- `dryl.js` — `window.dryl.commandpalette`: `attachGlobal` / `detachGlobal` for per-instance Ctrl+K document listener (WeakMap-keyed, no leaks), `focusInput`, `scrollItemIntoView`
- `DrylAutocomplete<TItem>` — Generic combobox; `ItemsProvider` for server-side async search, `SearchFunc` for client-side filtering, `ItemTemplate` for custom option rendering, `DisplayText` converter; ARIA combobox pattern; AI-aware (`Ai` parameter signals model pre-filling the value)
- `DrylDatePicker` — Calendar panel bound to `DateOnly?`; keyboard-navigable ARIA grid (Arrow keys, PageUp/Down, Home/End, Enter/Escape); `Min` / `Max` constraints; optional date range mode via `Range` + `@bind-RangeStart` / `@bind-RangeEnd`; AI-aware
- `DrylStepper` — Multi-step wizard container (mirrors `DrylTabs` cascading pattern); variants: Horizontal / Vertical; `@bind-ActiveStep` two-way binding; compound with `DrylStep`
- `DrylStep` — Single step declaration inside `DrylStepper`; states: Pending / Active / Completed / Error; optional `Description`, `Icon` override; AI-aware (`Ai` parameter wraps the step header in the shared ai-aura ring vocabulary)
- `dryl.js` — `window.dryl.autocomplete.scrollOptionIntoView` and `window.dryl.datepicker.focusDay` helpers
- `StepperOrientation` enum — `Horizontal` / `Vertical` for `DrylStepper`
- `StepState` enum — `Pending` / `Active` / `Completed` / `Error` for `DrylStep`

### Changed
- `DrylSelect` — Now AI-aware: added `Ai` parameter (`AiState`, default `AiState.None`); native `<select>` is wrapped in `.input-wrapper` with the shared ai-aura ring, glow, and wash primitives

- `DrylSkeleton` — AI-native content placeholder; variants: Line / Text / Avatar / Card / Image / Custom; sizes: Small / Medium / Large; `Lines` and `Width` parameters; `AiState.Streaming` shifts shimmer to violet-cyan gradient to signal AI writing into placeholder blocks; `AiState.Generated` fades blocks out to reveal real content
- `DrylIcon` — new `Blocks` icon (Lucide `layout-template`) for navigation / skeleton-related UI
- `DrylMenu` + `DrylMenuItem` — Dropdown action menu anchored to any trigger; `MenuPlacement` (BottomStart / BottomEnd / TopStart / TopEnd), `Block` mode; `DrylMenuItem` supports icons, keyboard-shortcut hints, `Danger` variant, separators and section headers; fully keyboard-navigable (Arrow keys, Home/End, ESC, Tab)
- `dryl.css` — Menu primitives: `.menu-anchor`, `.menu-panel` (+ `--end`, `--top` placement modifiers), `.menu-item` (+ `--danger`), `.menu-item-shortcut`, `.menu-separator`, `.menu-header`
- `dryl.js` — `window.dryl.menu` — click-outside detection via capture-phase `pointerdown`, `focusPanel`, `navigate`, `focusTrigger`
- `DrylSpinner` — New loading indicator; variants: Ring / Dots / Pulse; sizes: Small / Medium / Large; AI-aware (`Ai` parameter drives shared AI vocabulary — spinning rate responds to Thinking/Streaming states)
- `DrylTable` — `GroupBy` parameter clusters rows under collapsible mono-styled group headers with a per-group count badge
- `DrylTable` — `DetailTemplate` slot adds a leading expand-chevron column; clicking it toggles a glass detail panel under each row
- `DrylTable` — `RowActions` slot appends a trailing per-row actions column whose clicks don't propagate to the row click handler
- `DrylTable` — `BulkActions` slot renders a floating glass action bar above the toolbar while any row is selected, with a count chip and clear-selection close
- `DrylTable` — `Virtualize` + `VirtualizeItemSize` parameters render only visible rows via `Microsoft.AspNetCore.Components.Web.Virtualization.Virtualize`; suppresses pagination, ignores grouping with a console warning
- `DrylTable` — `Height` parameter constrains the scrollable body (required for `Virtualize`)
- `DrylTable` — `StickyHeader` parameter (default `true`) keeps the header anchored to the top while the body scrolls
- `DrylTable` — `AllowColumnVisibility` parameter exposes a Settings-icon toolbar menu that toggles individual columns on and off
- `DrylTable` — `PersistStateKey` parameter persists sort, filters, page, page-size and column visibility in `localStorage` across reloads
- `DrylColumn` — `Hidden` parameter sets a column's initial visibility for the visibility menu
- `dryl.css` — Phase 3/4 primitives: `.tbl-group-header`, `.tbl-group-toggle`, `.tbl-group-chevron`, `.tbl-group-count`, `.tbl-col-expand`, `.tbl-expand-btn`, `.tbl-row-detail`, `.tbl-row-detail-inner`, `.tbl-col-actions`, `.tbl-row-actions`, `.tbl-bulk-bar` (+ `-info` / `-count` / `-label` / `-actions` / `-close`), `.tbl-wrap--scroll`, `.tbl-no-sticky`, `.tbl-col-menu-wrap`, `.tbl-col-menu-trigger`, `.tbl-col-menu` (+ `-header` / `-title` / `-close` / `-body` / `-option`)
- `dryl.js` — `window.dryl.storage` helper (`get` / `set` / `remove`) wrapping `localStorage` with graceful failure

### Changed
- `DrylTable` — Now implements `IAsyncDisposable` and uses `IJSRuntime` for the new state-persistence path

### Deprecated
<!-- Features that still work but will be removed in a future MAJOR go here -->

### Removed
<!-- Removed features go here -->

### Fixed
<!-- Bug fixes go here -->

---

## [0.1.0] — 2026-05-27

First documented state of the library. All components are in early-development status.

### Added

#### Design System
- `dryl.css` — Complete token system: colors, spacing, radii, shadows, transitions, typography
- AI-mode primitives: `.ai-aura`, `.ai-aura-ring`, `.ai-aura-glow`, `.ai-aura-wash`, `.ai-indicator`
- `AiState` enum — Shared AI state (`None / Active / Thinking / Streaming / Generated`)
- `DESIGN_TOKENS.md` — Full token reference
- `COMPONENT_PATTERNS.md` — Component anatomy and folder conventions
- `CLAUDE.md` — Contribution rules for AI agents and human contributors

#### Actions
- `DrylButton` — Primary interaction surface; variants: Primary / Secondary / Ghost / Danger; sizes: Small / Medium / Large; states: Loading, Disabled; leading and trailing icon slots; AI-Mode

#### Surfaces
- `DrylCard` — Glass surface with optional cursor spotlight; AI-Mode with rotating gradient border
- `DrylDialog` — Service-driven glass dialog; focus trap; sizes: Small / Medium / Large / FullScreen; AI-Mode (Human in the Middle)
- `DrylDialogProvider` — Root provider; placed once in `App.razor`
- `DrylToast` — Service-driven toast stack; variants: Info / Success / Warning / Danger / Ai; 6 positions; auto-dismiss with progress bar; hover-pause; AI-Mode

#### Intelligence (AI)
- `DrylAiIndicator` — Pulsing status pill; label and pulse speed adapt to `AiState`

#### Data
- `DrylBadge` — Inline status label; variants: Neutral / Accent / Success / Warning / Danger; optional dot
- `DrylIcon` — Lucide-based icon set; used by Button, Badge and others
- `DrylTable<TItem>` — Declarative data grid; global search, sort (multi-sort via Shift-click), column filters (Text / Select), pagination, row selection, KPI summary bar; optional `DataProvider` for server-side loading; AI-Mode
- `DrylColumn<TItem>` — Declarative column for `DrylTable`; `Sortable`, `Searchable`, `Filterable`; custom `CellTemplate` / `HeaderTemplate`; alignment; width
- `DrylTableKpi` — KPI summary bar for `DrylTable`
- `DrylPagination` — Standalone page navigator; First / Prev / numbers (smart-ellipsis) / Next / Last; page-size selector; "Showing X–Y of Z"

#### Inputs
- `DrylInputText` — Form-bound text input; leading and trailing icon slots; AI-Mode
- `DrylTextarea` — Auto-resizable textarea; AI-Mode
- `DrylCheckbox` — Accessible checkbox with label
- `DrylSelect` — Styled select bound to `EditForm`
- `DrylToggle` — On/off toggle switch

#### Layout
- `DrylLayout` — Root shell; CSS grid with sidebar and topbar slots; cascades layout context
- `DrylAppBar` — Sticky top bar; optional responsive drawer-toggle hamburger
- `DrylDrawer` — Sidebar; always-visible column on desktop, overlay on mobile (`@bind-Open`)
- `DrylMainContent` — Main content slot inside `DrylLayout`; handles scroll and padding
- `DrylNavGroup` — Labelled group of nav links inside `DrylDrawer`
- `DrylNavLink` — Single nav row with icon and active highlighting; supports external links
- `DrylExpansion` — Collapsible glass panel; stacked panels share borders and detach on open; AI-Mode
- `DrylTab` / `DrylTabs` — Tab bar with glass panel content

#### Feedback
- `DrylAlert` — Feedback banner; variants: Info / Success / Warning / Danger / Ai; optional title; dismissible; AI-Mode
- `DrylTooltip` — CSS-only hover tooltip; 4 placements: Top / Bottom / Left / Right

#### Services & Extensions
- `IDrylDialogService` / `DrylDialogService` — Service-driven dialog control; `ShowAsync<T>`, `ShowConfirmAsync`, `ShowAlertAsync`
- `IDrylToastService` / `DrylToastService` — Service-driven toast control
- `AddDrylComponents()` — `IServiceCollection` extension method

#### Data Models
- `SortDescriptor`, `FilterDescriptor`, `DataRequest`, `DataResult<TItem>` — Models for `DrylTable` DataProvider
- `ColumnAlign`, `ColumnFilterType` — Enums for `DrylColumn`
- `DialogOptions`, `DialogParameters`, `DialogResult`, `DialogSize` — Models for `DrylDialog`
- `ToastOptions`, `ToastParameters`, `ToastVariant`, `ToastPosition` — Models for `DrylToast`
- `InputState` — Shared state enum for input components

---

[Unreleased]: https://github.com/Zimpi/DRYL.Components/compare/v2.0.0...HEAD
[2.0.0]: https://github.com/Zimpi/DRYL.Components/compare/v1.5.0...v2.0.0
[1.1.0]: https://github.com/Zimpi/DRYL.Components/compare/v1.0.0...v1.1.0
[1.0.0]: https://github.com/Zimpi/DRYL.Components/compare/v0.1.0...v1.0.0
[0.1.0]: https://github.com/Zimpi/DRYL.Components/releases/tag/v0.1.0
