# Layout — Interop

The JS interop surface this category uses, the DI services it registers, and the
cleanup duties each imposes (`CODE-05` in
[`../../harness/code.md`](../../harness/code.md)).

`DrylAppBar`, `DrylAspectRatio`, `DrylContainer`, `DrylDivider`, `DrylGrid`,
`DrylLayout` and `DrylList` make no JavaScript calls: their responsive
behaviour is CSS (container queries for `DrylGrid`, viewport queries for the
app shell), and the app shell coordinates through the cascaded
`DrylLayoutContext` (see [`_Api.md`](_Api.md)), not through script.

## Interop

| Entry point | Called by | Purpose |
|---|---|---|
| `dryl.motion.navGlide(drawer)` | `DrylDrawer`, after its first interactive render (and again if it switches between slotted and plain content) | Places the drawer's `.nav-ink` under the active nav row and glides it on navigation; tracks group and rail resizes; drives the hover light through one delegated `pointermove`. Idempotent per drawer. |
| `dryl.motion.disposeNavGlide(drawer)` | `DrylDrawer.DisposeAsync` | Disconnects the observers and listeners and clears the `data-nav-ink*` markers. |
| `dryl.storage.get` / `dryl.storage.set` | `DrylDrawer` (`SidebarMode.Pinnable`) | Restores and persists the collapsed state under `PersistStateKey`. |
| `dryl.modal.attach` / `dryl.modal.detach` | `DrylDrawer` (`SidebarMode.Flyout`) | Focus trap and Escape while the flyout is open. |
| `dryl.expansion` (delegated document listeners) | `DrylExpansion`, **without calling it** | Writes where the seam starts (`--seam-x` on the body) and where the hover light sits (`--spot-x` and `data-spot` on the header). Nothing to dispose. |
| `dryl.motion.observe(element, null, { stagger, once, threshold })` | `DrylReveal`, once, after its first interactive render | Watches the wrapper — or, with `stagger`, each direct child, including children added later (through a MutationObserver) — with two IntersectionObservers: one fires on the element's visible ratio, the other on the element filling `threshold` of the viewport. Reveals through `.is-revealed` on the wrapper, or `data-revealed` and `--reveal-i` per child. Under reduced motion, or without IntersectionObserver, it adds `.is-revealed` at once. Idempotent per element. |
| `dryl.motion.unobserve(element)` | `DrylReveal.DisposeAsync` | Disconnects the IntersectionObservers and the MutationObserver and cancels a pending batch frame. |

## Services

| Service | Lifetime | Registered by | Used by |
|---|---|---|---|
| `IDrylMorph` | scoped | `AddDrylComponents()` | `DrylMorph` — injected, and signalled from every render so the browser learns when the new view reached the DOM. `DrylMorph` never *starts* a transition; that stays with the consumer. |

No other component of the category injects a library service. `DrylDrawer`
injects framework `IJSRuntime` and `NavigationManager` (a location change closes an open overlay); `DrylReveal` injects `IJSRuntime`.

Cascading values, which are not DI services but carry the same coordination
duty: `DrylLayout` cascades one `DrylLayoutContext` (`IsFixed="true"`) to the
app shell; `DrylList` cascades itself (`IsFixed="true"`) so its
`DrylListItem` children read the marker variant.

## Cleanup

`DrylMorph` makes no interop call of its own, holds no `IJSObjectReference` and
registers no listener, so it has nothing to dispose (`CODE-05`). Signalling a
service it does not own imposes no cleanup duty: `SignalRendered()` is a no-op
when no transition is in flight.

`DrylDrawer` calls `dryl.motion.disposeNavGlide` and `dryl.modal.detach` only
when it attached them in an interactive render — a prerendered drawer that
never attached makes no interop call on disposal (`CODE-05`). A disconnected
circuit is tolerated. On disposal it also unregisters its collapse toggle and
itself from the `DrylLayoutContext`.

`DrylReveal` implements `IAsyncDisposable` and calls `dryl.motion.unobserve`
only when its first-render `observe` succeeded; disposal tolerates
`JSDisconnectedException` and the prerender `InvalidOperationException`. The
first-render `observe` catches only `JSDisconnectedException`.

`DrylAppBar` and `DrylLayout` implement `IDisposable` and unsubscribe from
`DrylLayoutContext.StateChanged`; `DrylAppBar` also moves its subscription when
the cascaded context changes. Both re-render through `InvokeAsync`, so a
`StateChanged` raised off the renderer's thread is safe. `DrylAspectRatio`,
`DrylContainer`, `DrylDivider`, `DrylGrid` and `DrylList` hold nothing to
release.
