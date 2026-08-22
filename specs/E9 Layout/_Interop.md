# Layout — Interop

The JS interop surface this category uses, the DI services it registers, and the
cleanup duties each imposes (`CODE-05` in
[`../../harness/code.md`](../../harness/code.md)).

*Scaffold. Filled in during phase C.*

## Interop

none *(phase C)*

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

*(the rest: phase C)*
