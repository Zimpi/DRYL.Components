# Surfaces — Interop

The JS interop surface this category uses, the DI services it registers, and the
cleanup duties each imposes (`CODE-05` in
[`../../harness/code.md`](../../harness/code.md)).

**Partial by design.** Of the category's eight components only `DrylPopover` has
a spec (`F1 DrylPopover.md`), and only its interop is documented below.
`DrylCard`, `DrylChat`, `DrylChatComposer` and `DrylDepthGlass` also inject
`IJSRuntime` and call `dryl.*` entry points of their own — `dryl.depthglass.*`,
`dryl.spotlight.*` and `dryl.chat.*` — and none of that is written here yet;
`DrylMarkdown`, `DrylMessage` and `DrylToast` inject no `IJSRuntime` at all, but
`DrylToast`'s lifecycle is driven from `dryl.toast` through its provider. Those
seven are open, not covered, and the sections below are not a complete account of
the category.

## Interop

`DrylPopover` injects `IJSRuntime` and calls three entry points of the
`dryl.popover` module in `code/DRYL.Components/wwwroot/js/dryl.js`. There is no
JS module import and no `IJSObjectReference`: the library ships one script that
attaches `window.dryl`, and the component calls into it by name.

| Entry point | Called from | Does |
|---|---|---|
| `dryl.popover.claimTrigger(anchor, role, open)` | `OnAfterRenderAsync` on the **first render only**, and only when `PanelRole` is set | Writes `aria-haspopup` and `aria-expanded` on the trigger, each only where absent, marking each claim on the node. |
| `dryl.popover.open(anchor, panel, dotnetRef, opts)` | `OnAfterRenderAsync`, when the rendered state is open and the portal is not yet up | Moves the panel to `<body>` carrying its content's scroll positions across the move, positions it, reveals it, applies a pending focus request, registers the scroll, resize and outside-press listeners, and re-claims the trigger with `aria-expanded="true"`. |
| `dryl.popover.close(anchor)` | `OnAfterRenderAsync` when the rendered state is closed, and from `DisposeAsync` | Removes those three listeners, sets a claimed `aria-expanded` back to `false`, drops an unapplied focus request, clears the inline placement styles and `data-dryl-positioned`, and returns the panel node to its anchor, again carrying its content's scroll positions across the move. |

`opts` carries `placement`, `matchWidth`, `closeOnOutside` and `role`.

**Back into .NET.** `DrylPopover` hands `dryl.popover.open` a
`DotNetObjectReference<DrylPopover>`; the outside-press listener calls
`Close()`, the component's one `[JSInvokable]` method. It is public because
interop requires it, not as an invitation to call it from consumer code.

**Two private channels on the panel node**, both established by `dryl.js` and
neither part of the public API. They are named here because they are the reason
the portal behaves the way `F1 DrylPopover.md` describes, and because anything
that changes the close path has to preserve them:

| Marker | Owner | Contract |
|---|---|---|
| `panel.__drylPendingFocus` | written by `drylPanelFocus.into` (used by `dryl.menu`, `dryl.datepicker`, `dryl.timepicker`), applied and deleted by `dryl.popover.open`, deleted unapplied by `dryl.popover.close` | The consumer decides **whether** focus enters the panel; the portal decides **when**. Needed because the panel is `visibility: hidden` until `dryl.popover.open` adds `.is-positioned`, and `focus()` on a hidden element is silently a no-op. |
| `panel.__drylPanelKeys` | written by `drylPanelKeys.install` (used by `dryl.datepicker` and `dryl.timepicker`) | The key policy for a focused panel: `Tab` cycles inside it, `Enter`/`Space` are left to a control that activates itself, and defaults are suppressed only for the keys the panel really consumes. It lives in JS because `KeyboardEventArgs` carries no target, so a .NET handler on the panel cannot tell where a key came from. |

**Two markers on the trigger node**, written by `dryl.popover.claimTrigger` and
read by nothing else: `__drylTriggerHasPopup` and `__drylTriggerExpanded`. They
record which of the two attributes this module claimed, so a trigger that writes
one of them itself is never written over. The rule that picks the node they land
on is the popover's own — an element already carrying `aria-haspopup`, else the
shallowest `button`, link or tab stop, with `tabindex="-1"` excluded and
disabled controls included — and it is deliberately not the selector
`dryl.menu.focusTrigger` uses, because what focus may land on and what ARIA
describes are different questions.

**What `DrylPopover` does not call.** It never focuses anything itself, and it
imports no positioning library: the placement, the viewport flip and the
horizontal clamp are arithmetic in `dryl.popover`'s own `place`, which keeps
`CODE-03`'s zero-runtime-dependency rule intact.

## Services

**None.** No component in this category registers a service, and none injects a
`DRYL.Components` service. `DrylPopover` injects `IJSRuntime` alone. It reads no
cascading value and requires no provider component in the layout, so it works in
an application that never called `AddDrylComponents()`.

## Cleanup

| Component | Contract | Released |
|---|---|---|
| `DrylPopover` | `IAsyncDisposable` | The portal (`dryl.popover.close`) and the `DotNetObjectReference` it handed to JS. |

`DisposeAsync` tears the portal down **before** disposing the object reference,
in that order, because a popover disposed while open has its panel node living
under `<body>`: without the teardown the node and its three document-level
listeners would outlive the component. The teardown is idempotent — a `_portaled`
latch makes a second call a no-op — and it swallows exactly three exceptions,
each for a state in which the call cannot succeed and no cleanup is owed: a
closing circuit (`JSDisconnectedException`), an element already gone
(`JSException`) and a statically prerendered component that never had a portal
(`InvalidOperationException`).

The listeners registered by `dryl.popover.open` — capture-phase `scroll`,
`resize`, and capture-phase `pointerdown` when `CloseOnClickOutside` is set —
are all removed in `dryl.popover.close`, which the component reaches on every
close path because the portal is driven off the rendered open state rather than
off the path that changed it.

**One listener in this category has no teardown path**: the `keydown` handler
`drylPanelKeys.install` adds to the popover's panel node. It is not removed
anywhere in `dryl.js`. The argument is that it captures nothing but the node it
is bound to and is collected with it when Blazor discards the component; that
holds, and it is still the library's first listener without a `detach`
counterpart. `F1 DrylPopover.md` carries it as a deviation.
