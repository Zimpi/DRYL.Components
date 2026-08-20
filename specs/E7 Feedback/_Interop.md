# Feedback — Interop

The JS interop surface this category uses, the DI services it registers, and the
cleanup duties each imposes (`CODE-05` in
[`../../harness/code.md`](../../harness/code.md)).

The category is unusually quiet here. **No component in it calls JS from C#** —
there is no `IJSRuntime` injection anywhere under
`code/DRYL.Components/Components/Feedback/`. `DrylSpinner`, `DrylSkeleton`,
`DrylProgress`, `DrylAlert`, `DrylEmptyState` and `DrylErrorBoundary` are CSS and
markup end to end. That is why none of them needs a prerender guard: there is no
`OnAfterRenderAsync` interop to guard.

Two components are the exceptions, and neither is an exception in the usual way.

## Interop

| Entry point | Reached by | Purpose |
|---|---|---|
| `dryl.tooltip` (delegated document listeners) | `DrylTooltip`, **without calling it** | Shows, positions and hides the tooltip bubble. |
| `dryl.tooltip.hide()` | any consumer, optional | Hides the current bubble immediately. |
| `dryl.popover.*` | `DrylNotifications`, transitively | Opening, portalling and positioning the inbox panel. |

### `DrylTooltip` — an interop surface with no interop call

`DrylTooltip` renders a wrapper carrying `data-tt` and `data-tt-placement` and
nothing else. It never obtains an `IJSRuntime`, never registers, never
unregisters. A single set of **delegated document listeners**, installed once by
`dryl.tooltip` in `code/DRYL.Components/wwwroot/js/dryl.js` and guarded by
`window.__drylTooltipBound`, drives every tooltip on the page by finding the
nearest `[data-tt]` ancestor of the event target.

The consequences are the point of the design:

- A tooltip costs **no** per-instance interop, which is what makes it usable in
  a table cell or a toolbar with dozens of triggers.
- A tooltip works during prerender, because there is nothing to attach.
- A trigger removed from the DOM mid-hover cannot leak a listener, because it
  never had one. `pointerout` and the `isConnected` guard in the placement
  routine cover the bubble.

The bubble itself is a **body-level portal**: one lazily created `div.tt-portal`
appended to `document.body`, `position: fixed`, reused by every tooltip and
never removed. It escapes ancestor `overflow` and `backdrop-filter` clipping —
the reason a tooltip inside a glass card or an app bar is visible at all.

Placement is resolved in JS, not in CSS: the routine measures the bubble
off-screen, flips to the opposite side when the preferred one has no viewport
room, and finally clamps the result into the viewport on both axes. The bubble
is revealed on the next animation frame so its enter transition runs rather than
being skipped.

`dryl.tooltip` exposes exactly one function, `hide()`. Nothing in the library
calls it; it exists so an application that moves content under a stationary
pointer can dismiss a bubble that would otherwise linger.

### `DrylNotifications` — interop by composition

`DrylNotifications` calls no JS either. It composes `DrylPopover`, and every
interop duty of the inbox panel — the body portal, the placement, the outside
click, `Escape`, the exit animation — belongs to that component and is specified
in `specs/E11 Surfaces/F1 DrylPopover.md`. The inbox inherits both the
behaviour and the recorded debt.

## Services

| Service | Lifetime | Registered by |
|---|---|---|
| `IDrylNotificationService` | scoped | `AddDrylComponents()` |

Scoped means one per Blazor circuit, which is what makes an inbox per-user
rather than per-server.

`DrylNotifications` resolves it in an unusual way, and deliberately: it injects
`IServiceProvider` and asks for `IDrylNotificationService` with `GetService`,
tolerating `null`. A required-service injection would make the component
unusable for anyone who did not call `AddDrylComponents()`, and unusable in
controlled mode, where the service is genuinely not wanted. The service is also
not resolved at all when `Items` is set — controlled mode never touches it.

No other component in the category injects anything.

## Cleanup

Six components implement `IDisposable` for the same single duty: `AuraLifecycle`
keeps the AI aura mounted for one `--dur-slow` beat after the state drops so it
dissolves rather than snapping away, and it holds a timer that must be disposed
with the component. `DrylAlert`, `DrylSpinner`, `DrylSkeleton`, `DrylProgress`,
`DrylEmptyState` and `DrylNotifications` each dispose theirs.

`DrylNotifications` carries one duty more, and it is the one that matters:
**it unsubscribes from `IDrylNotificationService.OnChanged`.** The service is
scoped and outlives any single component; a bell disposed without unsubscribing
would be kept alive by the service for the rest of the circuit and would call
`StateHasChanged` on a dead component on the next push. The subscription is
taken in `OnInitialized` only in service-driven mode, and the disposal is
null-safe for controlled mode where it was never taken.

`DrylTooltip` and `DrylErrorBoundary` implement no disposal and need none:
neither holds a timer, a subscription or a JS reference. `DrylErrorBoundary`
forwards `Ai` and `Aura` to the `DrylAlert` it renders, and that alert disposes
its own aura lifecycle.

Nothing in the category hands a `DotNetObjectReference` to JS, so nothing in the
category can leak one.
