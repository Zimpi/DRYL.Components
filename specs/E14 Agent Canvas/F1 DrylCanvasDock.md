# DrylCanvasDock

## Meta
- **State:** Implemented
- **Source:** code/DRYL.Components.Agents/Canvas/DrylCanvasDock.razor
              code/DRYL.Components.Agents/Canvas/DrylCanvasDock.razor.css

## User Story

As a Blazor developer building an AI application on DRYL, I want one floating
command bar that takes the user's prompt, says in a single line what the agent is
doing and keeps the transcript out of the way until it is asked for, so that the
artifact on the canvas stays the answer and the conversation does not take half
my screen.

## Description

`DrylCanvasDock` is the prompt dock of the Agent Canvas: a card floating in one
corner of the viewport with a composer, one line of live status and — only on
demand — the transcript. It is a **command bar, not a chat**. The dock brings no
message model of its own: the transcript is passed in through the `Log` slot, so
a host renders it with `DrylChat`/`DrylMessage` exactly as it renders any other
conversation.

Given a `DrylCanvasRun` it derives its status line and its AI state from that
run. Given a `CanvasSelection` it shows a context chip for the selected element
and prefixes every prompt with a reference to it, so a follow-up like "make it a
bar chart" lands on the element the user means. Given a `DrylVoiceRun` it grows a
microphone button, and a live session takes the whole dock over: composer,
suggestions and context chip step aside for the orb and the last spoken line.

Collapsed, the dock is a single floating button. That button is the way into the
assistant from anywhere in the application, so it takes `ButtonVariant.Bold` —
the one variant `DESIGN-08` allows to fill with the accent, for the rare hero
call to action. Nothing competes with it in its corner, and the quiet
`ButtonVariant.Primary` it carried until `2.24.1` did not read as an entry point
against a real application ground; in light mode it was a white pill on a
near-white page. The judgment and its evidence are recorded in
`ideas/I7 Two loose ends from the quiet Primary.md`.

The dock lives in the browser's **top layer** (`popover="manual"`), because a
`position: fixed` element is measured against the nearest ancestor carrying a
transform or a `backdrop-filter` — in a real application almost always a glass
card. Manual, not auto: the dock owns its own collapse, and a light-dismissing
popover would close behind the component's back on the first click elsewhere.

## Public API

| Parameter | Type | Default | Purpose |
|---|---|---|---|
| `Run` | `DrylCanvasRun?` | `null` | The canvas run the status line and the AI state read. Without it the dock is just an input. |
| `Busy` | `bool` | `false` | The host is mid-turn: the composer locks and the dock breathes. |
| `OnSend` | `EventCallback<string>` | — | Raised with the submitted text, prefixed with the selection reference when there is one. |
| `Corner` | `DockCorner` | `BottomRight` | Which corner the dock floats in. |
| `Placeholder` | `string?` | `"Ask for a view…"` | Composer placeholder. |
| `Status` | `string?` | `null` | Overrides the status line derived from `Run` and `Voice`. |
| `Log` | `RenderFragment?` | `null` | The transcript, revealed on demand. Without it the dock offers no disclosure. |
| `Actions` | `RenderFragment?` | `null` | Host controls in the dock head, left of the log toggle. |
| `Suggestions` | `RenderFragment?` | `null` | Prompt chips above the composer. |
| `Selection` | `CanvasSelection?` | `null` | The canvas's selection: drives the context chip and the prompt prefix. |
| `Voice` | `DrylVoiceRun?` | `null` | A voice session for this dock. Without it nothing about the dock changes. |
| `VoiceLabel` | `string` | `"Talk to the assistant"` | Label of the microphone button — tooltip and `aria-label` both. |
| `Collapsed` | `bool` | `false` | Whether the dock is collapsed to a single button. Two-way bindable. |
| `CollapsedChanged` | `EventCallback<bool>` | — | Fires when the dock collapses or expands. |
| `Title` | `string` | `"Assistant"` | Name of the assistant: the collapsed button's label and the composer's `aria-label`. |
| `Class` | `string?` | `null` | Extra classes merged onto the dock root, never replacing its own. |
| `AdditionalAttributes` | `IDictionary<string, object>?` | `null` | Pass-through attributes on the root element. |

`DockCorner` is a category-level enum and is specified in [`_Api.md`](_Api.md).

## Acceptance Criteria

### Collapsed and expanded

- `Collapsed` defaults to `false`, so a dock that was never given the parameter
  renders expanded.
- The root carries the collapsed modifier class exactly while `Collapsed` is
  `true`, and the root narrows to the button's own width in that state.
- Exactly one of the two — the collapsed button or the panel — is visible at a
  time; each is wrapped in its own `DrylPresence`, so the swap is animated in
  both directions rather than cut.
- The collapsed button expands the dock, and the head's collapse button
  collapses it.
- Both raise `CollapsedChanged`, and neither changes `Collapsed` without raising
  it, so a host that binds the parameter stays authoritative.
- The collapsed button uses `ButtonVariant.Bold`: it is a floating entry point
  with nothing competing against it, which is the hero case that variant exists
  for, and its affordance may not rest on the tooltip alone.
- The collapsed button is icon-only and therefore carries both a `DrylTooltip`
  and an `AriaLabel`, each naming `Title` (`UX-05`).

### Status line

- `Status`, when set, wins over every derived line, so a host can write the
  status in its own language.
- A voice session's own phase — connecting, listening, thinking, speaking,
  ending — is reported next, because a live session is what the AI is doing.
- An error on the voice run or on the canvas run is reported as its message, and
  the status line carries the error modifier class while it does.
- A run that has not yet streamed, produced or failed anything, with no `Busy`,
  reads `Idle` rather than claiming work: `DrylRunBase` starts at
  `AiState.Thinking`, so a freshly created run would otherwise make an untouched
  page claim the assistant is working.
- A streaming run reports its node count; a settled run holding an artifact
  reports the same count as ready, because no generation is coming to resolve a
  "working" line that was never true.
- The status line replaces itself with a movement rather than a jump: it is
  re-keyed on its own text inside a `DrylPresence`, so the old line fades out and
  the new one in.
- The status line is a live region (`aria-live="polite"`), so the state it
  reports reaches a screen reader without stealing focus.

### Prompt and selection

- An empty or whitespace-only prompt raises nothing.
- With no selection, `OnSend` receives exactly what the user typed.
- With a selection, `OnSend` receives the text prefixed by a reference naming the
  element's id, type and label, so the model can patch the right node.
- That prefix is composed with `InvariantCulture`, so a host running under a
  German locale sends the same string as one running under an English locale.
- The context chip is shown exactly while there is a selection and no live voice
  session, and it arrives and leaves with a movement.
- The chip's clear button clears the selection and is icon-only, so it carries a
  tooltip and an `AriaLabel`.
- The selection survives a send: a follow-up almost always concerns the same
  element.
- A "prompt about this element" request from the canvas expands a collapsed dock
  and moves focus into the composer, so the sentence can be typed where it was
  asked for.

### Transcript

- The log toggle exists exactly while `Log` is set: a dock with no transcript
  offers no disclosure.
- The log stays in the DOM while closed and animates open on the grid-row
  disclosure, so its content is never squeezed while it moves.
- The log carries `role="log"`, and `aria-hidden` exactly while it is closed.
- The log toggle is a toggle in the accessibility tree: it carries `Pressed`, so
  it reports `aria-pressed` and takes the button's active modifier.
- Sending a prompt, and opening the log, scroll it to the end.

### Voice

- The microphone button exists exactly while `Voice` is set and no session is
  live: once the session runs, the whole dock is the voice and the way out is the
  stop button.
- A live session hides the composer, the suggestions and the context chip and
  shows the orb, the last spoken line and the stop button — each swap animated.
- The last spoken line is re-keyed on itself, so a new sentence fades in rather
  than replacing the old one in place.
- The dock subscribes to `Run`, `Selection` and `Voice` by reference and
  re-subscribes only when the reference changes, so a host that hands over a
  stable instance is not re-wired on every render.

### Top layer

- The dock renders `popover="manual"` and is promoted with `dryl.topLayer.show`,
  so it is measured against the viewport and not against whichever ancestor
  happens to carry a `transform` or a `backdrop-filter`.
- Promotion is a two-step render: the attribute must be in the DOM before the
  call is made, so the first render goes out without it and asks for a second.
- Under static prerender the dock stays a plain fixed element and renders without
  a JS call, so the first paint never throws.
- The dock unsubscribes from all three runs and hides its popover on dispose, and
  it disposes only the subscription — the runs belong to the host and outlive it.
- Every interop call tolerates a dead circuit, a missing element and a prerender
  pass without failing the component (`CODE-05`).

## Cross-cutting evidence (`SPEC-05`)

- **Both color modes** — token-only colors throughout the scoped stylesheet; the
  component branches on no mode and holds no mode-assuming value (`DESIGN-02`).
  The collapsed FAB was photographed in both modes against a real application
  ground on 2026-08-18 —
  `docs/screenshots/2026-08-18-canvas-dock-fab-dark.png` and
  `docs/screenshots/2026-08-18-canvas-dock-fab-light.png` — which is the evidence
  that produced the `Bold` change, and again afterwards as
  `docs/screenshots/2026-08-18-canvas-dock-fab-bold-dark.png` and
  `docs/screenshots/2026-08-18-canvas-dock-fab-bold-light.png`.
- **Enter/exit animation** — present throughout and central to the component: the
  collapsed button and the panel each own a `DrylPresence` on the scale
  transition, and the context chip, the suggestions, the composer and the voice
  takeover each own one of their own, so every row of the dock arrives and leaves
  with a movement (`DESIGN-11`, `DESIGN-12`). The status line and the spoken line
  animate by re-keying their presence on their own text.
- **Keyboard and a11y** — the dock is a card of ordinary buttons and one
  composer, so it is reachable in document order with no key handling of its own;
  every icon-only button carries a `DrylTooltip` and an `AriaLabel` (`UX-05`);
  the status line is a polite live region; the log is a `role="log"` that is
  `aria-hidden` while closed, and its toggle reports `aria-pressed`. The dock is
  `popover="manual"`, so `Escape` does not dismiss it — the collapse button is
  the way out, and the dock never traps focus.
- **AI mode** — yes, and the dock is one of the components the vocabulary was
  built for. It derives one `AiState` for the whole dock and hands it to the
  `DrylAiIndicator`, the composer and the collapsed button, so the assistant's
  state is felt in the corner without a label. A live voice session outranks the
  canvas run, because what the AI is doing then *is* what the voice is doing
  (`AI-05`).
- **Demo page** — `Components/Pages/DemoCanvasDock.razor` in `DRYL.Website`, with
  the `Components/Examples/CanvasWorkspace/` examples beside it (`CODE-20`).
- **`ComponentCatalog` entry** — "Canvas Dock" (`canvas-dock`, category
  "Agents") in `DRYL.Website`'s `Components/ComponentCatalog.cs` (`REL-04`).
