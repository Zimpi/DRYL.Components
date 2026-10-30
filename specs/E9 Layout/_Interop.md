# Layout — Interop

The JS interop surface this category uses, the DI services it registers, and the
cleanup duties each imposes (`CODE-05` in
[`../../harness/code.md`](../../harness/code.md)).

*Scaffold. Filled in during phase C.*

## Interop

| Entry point | Called by | Purpose |
|---|---|---|
| `dryl.motion.navGlide(drawer)` | `DrylDrawer`, after its first interactive render (and again if it switches between slotted and plain content) | Places the drawer's `.nav-ink` under the active nav row and glides it on navigation; tracks group and rail resizes; drives the hover light through one delegated `pointermove`. Idempotent per drawer. |
| `dryl.motion.disposeNavGlide(drawer)` | `DrylDrawer.DisposeAsync` | Disconnects the observers and listeners and clears the `data-nav-ink*` markers. |
| `dryl.storage.get` / `dryl.storage.set` | `DrylDrawer` (`SidebarMode.Pinnable`) | Restores and persists the collapsed state under `PersistStateKey`. |
| `dryl.modal.attach` / `dryl.modal.detach` | `DrylDrawer` (`SidebarMode.Flyout`) | Focus trap and Escape while the flyout is open. |

*(the rest: phase C)*

## Services

| Service | Lifetime | Registered by | Used by |
|---|---|---|---|
| `IDrylMorph` | scoped | `AddDrylComponents()` | `DrylMorph` — injected, and signalled from every render so the browser learns when the new view reached the DOM. `DrylMorph` never *starts* a transition; that stays with the consumer. |

*(the rest: phase C)*

## Cleanup

`DrylMorph` makes no interop call of its own, holds no `IJSObjectReference` and
registers no listener, so it has nothing to dispose (`CODE-05`). Signalling a
service it does not own imposes no cleanup duty: `SignalRendered()` is a no-op
when no transition is in flight.

`DrylDrawer` calls `dryl.motion.disposeNavGlide` and `dryl.modal.detach` only
when it attached them in an interactive render — a prerendered drawer that
never attached makes no interop call on disposal (`CODE-05`). A disconnected
circuit is tolerated.

*(the rest: phase C)*
