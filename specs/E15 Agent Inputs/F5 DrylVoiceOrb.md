# DrylVoiceOrb

## Meta
- **State:** Implemented
- **Source:** code/DRYL.Components.Agents/Voice/DrylVoiceOrb.razor
              code/DRYL.Components.Agents/Voice/DrylVoiceOrb.razor.css

## User Story

As a Blazor developer running a voice session, I want a single round mark that
shows whether the assistant is listening, thinking or speaking and that breathes
with the audio, so that the user sees the conversation without reading a label.

## Description

`DrylVoiceOrb` is a voice made visible: a glass sphere wrapped in the shared
`.ai-aura` ring, comet and halo. Its state classes are the ones every other AI
surface wears, mapped from the `AiState` of the `DrylVoiceRun` it is given — so
a thinking assistant reads the same here as anywhere else in the library.

The one thing the orb owns is the **level**. The voice module measures the
microphone and the model's audio in the browser and writes `--voice-level`
straight onto the orb element; the orb's CSS turns it into a scale. The level
never crosses into .NET — a value shipped over interop sixty times a second
would re-render the circuit for a decoration.

When the user mutes the microphone, the ring and the comet step back and only
the core keeps answering, now moved by the model's voice alone.

`DrylCanvasDock` places one orb in its voice panel; a host can place its own.

```razor
<DrylVoiceOrb Run="_voice" Size="120" />
```

## Public API

Namespace: `DRYL.Components.Agents`. Implements `IAsyncDisposable`.

| Member | Type | Default | Purpose |
|---|---|---|---|
| `Run` | `DrylVoiceRun?` | `null` | The session the orb reflects. |
| `Size` | `int` | `96` | Diameter in pixels. |
| `Class` | `string?` | `null` | Extra classes merged onto the orb. |

`DrylVoiceRun`, its `State` and `IsMuted` are specified in [`_Api.md`](_Api.md);
`attachOrb` in [`_Interop.md`](_Interop.md).

## Acceptance Criteria

### Structure

- The component renders one root element carrying `.voice-orb` and `.ai-aura`.
- The root contains, in order, `.ai-aura-ring`, `.ai-aura-comet`,
  `.ai-aura-glow` and `.voice-orb-core`.
- The root carries `aria-hidden="true"`.
- The root's inline style sets `--orb-size` to `Size` in pixels.
- `--orb-size` is formatted with the invariant culture.
- `Size` defaults to `96`.
- A non-blank `Class` is appended to the root's classes.
- A blank `Class` adds nothing.

### State

- `AiState.Thinking` on the run adds `ai-thinking` to the root.
- `AiState.Streaming` on the run adds `ai-streaming` to the root.
- `AiState.Generated` on the run adds `ai-generated` to the root.
- Any other state, and a `null` `Run`, adds no state class.
- `IsMuted` on the run adds `voice-orb--muted` to the root.
- Every `OnChange` of the run re-renders the orb.
- Passing the same `Run` reference again does not resubscribe.
- Passing a different `Run` unsubscribes from the previous run's `OnChange` and
  subscribes to the new one.

### Level

- After its first render, the orb imports the voice module and calls
  `attachOrb` with its root element, once.
- The orb's scale grows with `--voice-level`, starting from its natural size at
  a level of zero.
- The level reaches the orb as a CSS custom property only; no level value
  passes through .NET and no level change re-renders the component.
- A scale change transitions over `--dur-fast` with `--ease-out`.

### Mute

- While muted, `.ai-aura-ring` and `.ai-aura-comet` fade to transparent over
  `--dur-slow` with `--ease-out`.
- While muted, the ring and the comet become `visibility: hidden` once the fade
  has finished, so the comet stops drawing.
- While muted, `.voice-orb-core` and `.ai-aura-glow` stay visible.
- While muted, the core keeps scaling with `--voice-level`.
- Unmuting makes the ring and the comet visible at once and fades them in over
  `--dur-slow`.

### Accessibility and motion

- The orb is decorative: it has no focusable node, no role and no accessible
  name.
- The orb carries no `aria-live` region; announcing the session is the host's
  duty (the dock's voice line, for one).
- Under `prefers-reduced-motion: reduce`, the orb does not scale with the level
  and has no scale transition.
- Under `prefers-reduced-motion: reduce`, the aura's own animations follow the
  shared `.ai-aura*` reduced-motion rules in `dryl.css`.

### Appearance

- The orb is a circle of radius `--r-pill`.
- The core is glass: a radial highlight of `--glass-2` over `--glass-1`, with a
  border of `--line`.
- The core's backdrop filter is `--glass-fx-float`.
- The accent shows only in the aura ring, comet and halo, never as the core's
  fill (`DESIGN-08`).
- The component branches on no color mode and holds no mode-assuming value
  (`DESIGN-02`).

### Cleanup

- Disposal unsubscribes from the run's `OnChange`.
- Disposal of an attached orb calls `attachOrb` with `null` and disposes the
  module reference.
- Disposal of an orb that never attached makes no JS call, so static prerender
  does not throw.
- A disconnected circuit or an already torn-down module during disposal is
  swallowed.

## Recorded debt

- **Idle reads as `Active`.** The root always carries `.ai-aura`, and the base
  `.ai-aura` is the `Active` look with an orbiting comet. A `null` run, an
  `Idle` run and a run at `AiState.Active` all render the same live aura
  (`AI-06`). Inside `DrylCanvasDock` the orb is mounted only while a session is
  active, so the debt surfaces where a host places the orb standalone.
- **Hand-drawn aura.** The orb writes the `.ai-aura-*` elements and state
  classes itself instead of composing `AiAuraCss`, `DrylAuraElements` and
  `AuraLifecycle` (`AI-02`, `AI-07`): `AiState.Proposed` has no mapping, a
  `Generated` is not retired, and there is no graceful exit.
- **Frost on an in-flow surface.** The core uses `--glass-fx-float`, the
  floating-surface frost, wherever the orb is placed (`DESIGN-06`).
- **Literal geometry.** The level-to-scale factor, the `--orb-size` fallback,
  the core's border width and the radial highlight's size and position are
  literals (`DESIGN-01`).
- **One orb per page.** The voice module holds a single orb element: the last
  orb to attach receives the level, and disposing any attached orb detaches
  whichever orb is currently attached.
- **Mute fade under reduced motion.** The ring and comet opacity transition of
  the mute is not suppressed under `prefers-reduced-motion: reduce` (`UX-06`).

## Cross-cutting evidence (`SPEC-05`)

- **Both color modes** — glass and aura tokens only, no mode branch; **not yet inspected by eye**: the orb mounts only during a live voice session, which the 2026-11-20 session did not start.
- **Enter/exit animation** — the orb itself does not mount conditionally;
  inside `DrylCanvasDock` it arrives and leaves with the dock's voice panel
  through `DrylPresence` (`PresenceTransition.Scale`). Level and mute changes
  are animated as above. A standalone orb has no exit of its own.
- **Keyboard and a11y** — decorative and hidden from assistive technology, as
  stated above.
- **AI mode** — **yes, always on and not an opt-in**: the orb has no `Ai`
  parameter because its state is the run's `AiState`; it exists only to show
  the voice session's AI state.
- **Demo page** — rendered inside `DrylCanvasDock`'s voice panel on
  `/components/voice` (`DRYL.Website/Components/Pages/DemoVoice.razor`) and
  `/components/canvas-dock`, visible while a voice session is live; no example
  places the orb on its own.
- **`ComponentCatalog`** — covered by the `"Voice"` / `voice` entry, whose
  source link names `DrylVoiceRun.cs`.
- **Tests** — `tests/DRYL.Components.Tests/Agents/Voice/DrylVoiceOrbTests.cs`
  (shared aura primitives, state classes, idle run without state class,
  invariant size, `aria-hidden`, mute class). `tests/js/voice.test.mjs` loads
  `attachOrb` but asserts nothing about `--voice-level`; the level, the
  attach/detach and the reduced-motion behaviour have no test.
