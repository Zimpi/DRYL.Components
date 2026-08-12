# AI states

## Meta
- **State:** Implemented

What `Ai` does on this component — and, just as much a decision, what it does
not do.

## Acceptance Criteria

### The opt-in

- `Ai` defaults to `AiState.None`.
- `Ai` accepts exactly the five values of `AiState`.
- The artifact tree renders unchanged when `Ai` is `AiState.None`.
- The header and the empty state render unchanged when `Ai` is `AiState.None` —
  the parameter is a switch, not a precondition, which is why `AI-03` requires it
  to be called `Ai`.
- Setting the obsolete `State` alias sets `Ai` to the same value.
- Reading the obsolete `State` alias returns the current value of `Ai`.
- The value is passed into the shared `CanvasContext`, so the node views resolve
  their own AI behaviour from one place rather than from a cascade of their own.

### The build line

- The build line is visible exactly while the canvas is busy.
- The canvas is busy when `Ai` is `AiState.Streaming` or `AiState.Thinking`.
- The canvas is also busy while the data binder is loading, independently of
  `Ai` — a data load is an author at work too (see `S3`).
- The build line is rendered as an indeterminate `DrylProgress`, not as a
  hand-rolled bar (`DESIGN-13`).
- The build line enters and exits through `DrylPresence`, so it also animates
  out (`DESIGN-12`).
- The build line carries `aria-hidden="true"`: it is decoration, and the state
  it signals is already announced through the `aria-live` region.

### Not-yet-valid nodes

- A node that fails validation renders as a skeleton with a "waiting for
  {type}…" note while `Ai` is `AiState.Streaming` or `AiState.Thinking`.
- The same node renders its validation error once `Ai` has settled — a
  finished-broken node reads as broken, not as loading.
- A node whose binding has not delivered its first value renders as a skeleton
  regardless of `Ai`: a first load is a first load whoever triggered it.

### No aura here

- The canvas carries no `.ai-aura*` classes and takes no `Aura` parameter.
- The canvas takes no aura from a surrounding `DrylAiScope`.

  This is a decision, not an omission. An artifact tree is a large surface, and
  `DESIGN-08` reserves the accent for gradients, hairlines, glow rings and small
  indicators rather than the fill or frame of something that size. The state is
  already legible from the build line and the waiting skeletons. The AI-facing
  wrapper `DrylAiCanvas` adds the aura where it wants one, on its own frame.

- `AiState.Generated` therefore fires no one-shot reveal on this component; the
  wrapper owns that choreography (`AI-07`).
