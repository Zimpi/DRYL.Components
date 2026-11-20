# I48 — The orb and the handoff lane wear the shared aura: plan

Idea: [`ideas/I48 The orb and the handoff lane wear the shared aura.md`](../../ideas/I48%20The%20orb%20and%20the%20handoff%20lane%20wear%20the%20shared%20aura.md) (orb at rest: the sphere alone; lane that finishes: the one-shot).
Target: `release/3.17.0`. One commit: both components make the same move onto
the same primitives.

## Task 1 — `DrylVoiceOrb`

Files: `code/DRYL.Components.Agents/Voice/DrylVoiceOrb.razor`, `.razor.css`.

- One `AuraLifecycle`, synced from `OnParametersSet` and from the run's
  `OnChange` (marshalled onto the render thread).
- Root classes through `AiAuraCss.Append`; the layers through
  `DrylAuraElements`. The scoped mute rules reach the layers through `::deep`,
  because a child component's elements carry no scope attribute of this file.

## Task 2 — `DrylHandoffTrace`

File: `code/DRYL.Components.Agents/Agents/DrylHandoffTrace.razor`.

- One `AuraLifecycle` per lane, keyed by the step, dropped when `Run` changes
  and on dispose.
- A lane's aura state is its step's `Thinking`, `Streaming` or `Generated`
  without an error; anything else is `None`.
- A lane already at `Generated` when its lifecycle is created is marked
  settled and not synced until it leaves that state.

## Task 3 — tests, specs, changelog

- `tests/DRYL.Components.Tests/Agents/Voice/DrylVoiceOrbTests.cs` — at rest no
  aura; graceful exit.
- `tests/DRYL.Components.Tests/Agents/DrylHandoffTraceTests.cs` — one-shot on
  completion; settled at mount; failing lane fades; one aura per concurrent
  lane.
- Both specs, `CHANGELOG.md` (`Agents 0.25.1` Changed).

Verify: the two test classes, the full evidence list of `CLAUDE.md` stage 5,
and both colour modes by eye. The docs website was locked by a running
instance, so the eye check used a temporary page in `tests/DRYL.BrowserHost`
that drove both components through their states; it is not committed.
