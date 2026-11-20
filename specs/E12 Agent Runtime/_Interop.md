# Agent Runtime — Interop

The JS interop surface this category uses, the DI services it registers, and the
cleanup duties each imposes (`CODE-05` in
[`../../harness/code.md`](../../harness/code.md)).

## Interop

none — no component or type under `code/DRYL.Components.Agents/Agents/` or
`code/DRYL.Components.Agents/Display/` calls JavaScript. Motion comes from
scoped CSS and the composed core components (`DrylPresence`, `DrylAlert`,
`DrylBadge`, `DrylAiIndicator`, the chart family).

## Services

- **`DrylAgentRunner`** — registered **scoped** (one per circuit) by
  `AddDrylAgents()` in
  `code/DRYL.Components.Agents/Extensions/ServiceCollectionExtensions.cs`,
  called alongside the core `AddDrylComponents()`. (`AddDrylAgents()` also
  registers `DrylVoiceRunner`, which belongs to `E15 Agent Inputs`.)
- **`IDrylAiActivityService`** — optional, from the core package. When it is
  registered, the runner mirrors a run's state onto the run's `aiKey`, so a
  surrounding `DrylAiScope Key="…"` lights up; without it, `aiKey` has no
  effect.

The run components (`DrylAgentToolCalls`, `DrylAgentError`, `DrylAgentUsage`,
`DrylHandoffTrace`, `DrylAgentAttachments`) inject nothing; they receive a run
through their `Run` parameter.

## Cleanup

- **Run subscription.** Every component of the category subscribes to its
  run's `OnChange`. It unsubscribes from the previous run when `Run` changes to
  another instance, and in `Dispose`.
- **Reveal queue.** `DrylAgentAttachments` cancels and disposes its pending
  stagger delay when `Run` changes and in `Dispose`.
- **Runs are the consumer's.** A run is `IAsyncDisposable`; the consumer that
  started it disposes it. `DisposeAsync` cancels the run's processing and any
  pending approval wait, and completes `TextStream` and
  `WaitForCompletionAsync`. `DrylMultiAgentRun.DisposeAsync` additionally
  disposes every started child run. The components never dispose the run they
  render.
