# AI — Interop

The JS interop surface this category uses, the DI services it registers, and the
cleanup duties each imposes (`CODE-05` in
[`../../harness/code.md`](../../harness/code.md)).

Five of the category's eight components touch neither JS nor DI: `DrylAiIndicator`
and `DrylAuraElements` are pure markup, and `DrylToolCall`, `DrylToolCallGroup` and
`DrylAiScope`/`DrylAiStream` reach at most for an optional service. The interop
below belongs almost entirely to `DrylCanvas` and `DrylCanvasWorkspace`.

## Interop

Two shapes are used, and they are not interchangeable. The `dryl.*` entry points
are globals from the always-loaded `dryl.js`; `dryl-canvas.js` is a module imported
on demand, which is why only it needs a `DotNetObjectReference` and an explicit
release.

| Entry point | Used by | Purpose |
|---|---|---|
| `dryl.motion.moveIndicator` | `DrylCanvasWorkspace` | Measures the active chip and glides the shared `[data-dryl-ink]` indicator onto it. Also sets `is-ink-ready`, deferred, so the ink does not slide in from x=0 on the first placement. |
| `dryl.motion.disposeIndicator` | `DrylCanvasWorkspace` | Releases the observer the placement attached. |
| `dryl.motion.autoFlip` | `DrylCanvas` | Animates node reflow in the artifact body. |
| `dryl.motion.stopAutoFlip` | `DrylCanvas` | Stops that observation. |
| `dryl.topLayer.show` / `dryl.topLayer.hide` | `DrylCanvas` | Promotes the canvas root to the top layer while it is expanded to fullscreen, and takes it back down. |

### The `dryl-canvas.js` module

Imported by `DrylCanvas` from
`./_content/DRYL.Components/js/dryl-canvas.js`. It exports four functions in two
pairs, each pair attach/release:

| Export | Purpose |
|---|---|
| `observe(el, dotnet)` | Watches the artifact body's usable width and reports it back. |
| `unobserve(el)` | Releases that observation. |
| `initReorder(root, dotnet)` | Attaches the pointer-driven node reorder gesture. |
| `disposeReorder(root)` | Releases it. |

Both take a `DotNetObjectReference<DrylCanvas>` and call back into two `[JSInvokable]`
methods on the component: `OnWidthMeasured(int width)` and
`OnNodeReorder(string id, int index)`.

`initReorder` is attached only when the canvas was given a `CanvasSelection` — the
opt-in for direct manipulation. `disposeReorder` is therefore called only when it
was attached; see `F3 DrylCanvas/S4 Interaction.md`.

## Services

None of these is registered by this category. `AddDrylComponents()` registers what
is registered, and every one of them is resolved **optionally**, so a component in
this category works in an application that never called it.

| Service | Consumed by | Resolution |
|---|---|---|
| `IDrylAiActivityService` | `DrylAiScope`, `DrylAiStream` | Optional (`GetService`). Without it, `DrylAiScope` falls back to its explicit `State` and `DrylAiStream` pushes nothing. Scoped — one per Blazor circuit. |
| `IDrylViewTransition` | `DrylCanvas`, `DrylCanvasWorkspace` | Injected. Drives the morph on a view switch, a history step and the expand-to-fullscreen. |
| `ICanvasDocumentStore` | `DrylCanvasWorkspace` | Optional (`GetService`). Without it, `AutoSave` does nothing rather than failing. |
| `ICanvasDataService` | `DrylCanvas` | Optional. Without it, bound data sources stay unresolved. |
| `ICanvasActionService` | `DrylCanvas` | Optional. Without it, action buttons have nothing to run. |
| `ILogger<CanvasDataBinder>`, `ILogger<CanvasActionRunner>` | `DrylCanvas` | Optional. Binding and action failures are logged when a logger exists and swallowed when it does not. |

`IDrylAiActivityService` is the only one of these the category also *drives*:
`DrylAiStream` writes each of its states to the service under its `Key`, which is
what makes a surrounding `DrylAiScope Key="…"` light up in lockstep. The interface
is documented in [`_Api.md`](_Api.md) and its type lives in `E1 Foundation`.

## Cleanup

Every component in this category that takes on a duty releases it. The two async
ones exist because JS interop cannot be released from a synchronous `Dispose`.

| Component | Contract | Released |
|---|---|---|
| `DrylToolCall` | `IDisposable` | Its `AuraLifecycle`, which holds a pending exit or `Generated` retirement timer. |
| `DrylToolCallGroup` | `IDisposable` | The same. |
| `DrylAiScope` | `IDisposable` | Its `IDrylAiActivityService.OnChanged` subscription. |
| `DrylAiStream` | `IDisposable` | The `CancellationTokenSource` of the running stream, which stops the producer, the reveal loop and the pending settle delay. |
| `DrylCanvasWorkspace` | `IAsyncDisposable` | The pending autosave, the `CanvasWorkspace.OnChange` subscription, and `dryl.motion.disposeIndicator` — the last only when an indicator was ever attached. |
| `DrylCanvas` | `IAsyncDisposable` | `dryl.motion.stopAutoFlip`, `disposeReorder` (only when attached), `unobserve`, the imported module reference, and the `DotNetObjectReference` it handed to it. |

`DrylCanvasWorkspace` also unsubscribes from `CanvasWorkspace.OnChange` when the
`Workspace` **parameter is replaced**, not only on disposal — a subscription to a
workspace the component no longer renders is the same leak one step earlier.

Every interop call in this category — attach and release alike — tolerates two
failure modes without surfacing an error, because neither is a fault: a
`JSDisconnectedException` when the circuit is gone, and an
`InvalidOperationException` during prerender, when there is no JS at all.

`DrylCanvasWorkspace` tolerates a third that `DrylCanvas` does not: a `JSException`
raised when the element left the DOM between the render and the interop call. Its
bar is wrapped in a `DrylPresence`, so an exit animation can take the element away
underneath a queued `moveIndicator`; the canvas body has no equivalent race.
