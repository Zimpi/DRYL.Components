# Surfaces — Interop

The JS interop surface this category uses, the DI services it registers, and the
cleanup duties each imposes (`CODE-05` in
[`../../harness/code.md`](../../harness/code.md)).

Five of the category's eight components inject `IJSRuntime`: `DrylPopover`,
`DrylCard`, `DrylChat`, `DrylChatComposer` and `DrylDepthGlass`. `DrylMarkdown`,
`DrylMessage` and `DrylToast` inject none. `DrylToast`'s timer and motion are
driven from `dryl.toast` through its provider, documented in
`E1 Foundation/_Interop.md`. Every module named below lives in
`code/DRYL.Components/wwwroot/js/dryl.js`; none is imported as a JS module, and no
component holds an `IJSObjectReference`.

## Interop

### `DrylPopover`

`DrylPopover` injects `IJSRuntime` and calls four entry points of the
`dryl.popover` module in `code/DRYL.Components/wwwroot/js/dryl.js`. There is no
JS module import and no `IJSObjectReference`: the library ships one script that
attaches `window.dryl`, and the component calls into it by name.

| Entry point | Called from | Does |
|---|---|---|
| `dryl.popover.claimTrigger(anchor, role, open)` | `OnAfterRenderAsync` on the **first render only**, and only when `PanelRole` is set | Writes `aria-haspopup` and `aria-expanded` on the trigger, each only where absent, marking each claim on the node. |
| `dryl.popover.open(anchor, panel, dotnetRef, opts)` | `OnAfterRenderAsync`, when the rendered state is open and the portal is not yet up | Moves the panel to `<body>` carrying its content's scroll positions across the move, positions it, reveals it, applies a pending focus request, registers the scroll, resize and outside-press listeners, and re-claims the trigger with `aria-expanded="true"`. |
| `dryl.popover.releaseFocus(anchor)` | On the close request, including an externally changed `Open` after its render | Returns focus only while it still belongs to the closing panel; teardown does not delay the user's focus return. |
| `dryl.popover.close(anchor)` | `OnAfterRenderAsync` when the rendered state is closed, and from `DisposeAsync` | Removes those three listeners, sets a claimed `aria-expanded` back to `false`, drops an unapplied focus request, clears the inline placement styles and `data-dryl-positioned`, and returns the panel node to its anchor, again carrying its content's scroll positions across the move. |

`opts` carries `placement`, `matchWidth`, `closeOnOutside` and `role`.

**The list ink (I38).** `dryl.popover.open` calls `dryl.motion.glideScan(panel)`
after the reveal and any pending focus; `dryl.popover.close` calls
`dryl.motion.glideRelease(panel)`. No component calls either directly.

| Entry point | Does |
|---|---|
| `dryl.motion.glideScan(panel)` | Attaches the ink to every host in the panel — the panel itself when it is a `[role="menu"]` (rows `[role="menuitem"]`), and every element carrying `data-dryl-glide="<row selector>"` (`DrylSelect`, `DrylMultiSelect`, `DrylAutocomplete`, each `DrylTimePicker` column). Watches the panel for hosts that render later. Per host: a `MutationObserver` on `class` and children (the listboxes' `.is-highlighted`, an autocomplete's re-rendered rows), a `ResizeObserver`, and `pointerover`/`pointermove`/`pointerleave`/`focusin` listeners; mouse only. Writes `data-glide`, `data-glide-on`, `data-glide-tone`, `data-glide-spot` and `--glide-x/y/w/h`, `--glide-spot-x` — attributes Blazor never renders. |
| `dryl.motion.glideRelease(panel)` | Disconnects the panel watcher and every host's observers and listeners, cancels pending frames and removes every attribute and property written above (`CODE-05`). |

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

### `DrylCard` and `DrylDepthGlass` — the pointer trackers

Both modules only write custom properties on the element they are given; every
visible effect is CSS in `dryl.css`, so a pointer move costs no Blazor render.

| Entry point | Called from | Does |
|---|---|---|
| `dryl.spotlight.track(el)` | `DrylCard.OnAfterRenderAsync`, first render only, when `Depth` is `null` and `Spotlight` is `true` | Binds one `mousemove` listener that writes the pointer position relative to the element as `--mx` / `--my` (pixels). Marks the element `__drylSpot`; a second call on a marked element does nothing. |
| `dryl.spotlight.untrack(el)` | `DrylCard.DisposeAsync`, when the spotlight was tracked | Removes the listener and the `__drylSpot` marker. The written properties stay. |
| `dryl.depthglass.track(el)` | `DrylCard.OnAfterRenderAsync` (first render, `Depth` set) and `DrylDepthGlass.OnAfterRenderAsync` (first render, `Interactive` `true`) | Binds `pointermove`, writing `--mx` / `--my` (percent of the element) and `--tx` / `--ty` (offset from the centre), and `pointerleave`, resetting `--tx` / `--ty` to zero. Marks the element `__drylDg`; idempotent like the spotlight. |
| `dryl.depthglass.untrack(el)` | `DrylCard.DisposeAsync` and `DrylDepthGlass.DisposeAsync`, when tracked | Removes both listeners and the `__drylDg` marker. |

Neither component re-evaluates the choice after the first render; see the
recorded debt in `F3 DrylCard.md` and `F6 DrylDepthGlass.md`.

### `DrylChat` and `DrylChatComposer` — `dryl.chat`

| Entry point | Called from | Does |
|---|---|---|
| `dryl.chat.scrollToEnd(el)` | `DrylChat.OnAfterRenderAsync`, after **every** render while `AutoScroll` is `true` | Sets the log's `scrollTop` to its `scrollHeight`. Stateless; registers nothing. |
| `dryl.chat.attachComposer(el, dotnetRef)` | `DrylChatComposer.OnAfterRenderAsync`, first render only | Detaches any earlier registration for the element, then binds `keydown` (`Enter` without `Shift` and outside an IME composition: `preventDefault` and `SubmitFromJs`) and `input` (grow the textarea to its content). If the element cannot be measured yet — hidden, `scrollHeight` zero — it keeps its CSS height and a `ResizeObserver` measures it once, then disconnects. Handlers are held in a module-private `WeakMap` keyed by the element. |
| `dryl.chat.resize(el)` | `DrylChatComposer`, after each send | Re-measures the textarea, so it shrinks after the draft is cleared. |
| `dryl.chat.detachComposer(el)` | `DrylChatComposer.DisposeAsync`, when attached | Removes both listeners, disconnects a pending `ResizeObserver` and drops the `WeakMap` entry. |

**Back into .NET.** `DrylChatComposer` hands `attachComposer` a
`DotNetObjectReference<DrylChatComposer>`; the `keydown` listener calls
`SubmitFromJs()`, the component's one `[JSInvokable]` method, which takes the same
path as the send button.

**The input echo guard.** The composer's `textarea` carries `data-dryl-input`. The
component makes no call for it: `dryl.inputs` installs itself on the element at
its first focus through a document-level `focusin` listener and protects the field
from late server echoes of older keystrokes. The guard belongs to the input
family and is documented with it (`E8 Inputs/_Interop.md`).

## Services

**None.** No component in this category registers a service, and none injects a
`DRYL.Components` service. `DrylPopover`, `DrylCard`, `DrylChat`,
`DrylChatComposer` and `DrylDepthGlass` inject `IJSRuntime` alone. `DrylPopover`
reads no cascading value and requires no provider component in the layout, so it
works in an application that never called `AddDrylComponents()`. The AI-aware
components (`DrylCard`, `DrylChat`, `DrylChatComposer`, `DrylMarkdown`,
`DrylMessage`) read the optional cascading `AiScope` that `DrylAiScope` supplies,
and work without one.

## Cleanup

### Exit ownership (I13 H03)

`DrylPopover` shares `dryl.motion.onExit(panel, exitRef, { name:
"presence-out" })` and `dryl.motion.clearExit(panel)` with the foundation's
presence primitive. Each exit supplies its own private `ExitCallback` reference.
The callback and watchdog complete only that exit, with ownership checked on
the renderer after any await or queued dispatch. Reopening, completion and
disposal release that reference; queued callbacks from an older reference are
inert. `clearExit` remains cancellation-only and never reports completion.

Disposal invalidates .NET callbacks before awaiting browser cleanup. Clearing
the exit and releasing the portal are independent cleanup attempts so a rejected
exit cleanup cannot strand the portalled panel. Registration failure keeps the
existing watchdog fallback. This ownership is internal; no public parameter,
callback signature or motion timing is added.

### Per component

| Component | Contract | Released |
|---|---|---|
| `DrylPopover` | `IAsyncDisposable` | The portal (`dryl.popover.close`) and the `DotNetObjectReference` it handed to JS. |
| `DrylCard` | `IAsyncDisposable` | Its `AuraLifecycle`, then the pointer tracker (`dryl.depthglass.untrack` or `dryl.spotlight.untrack`) if one was attached. Any exception from the untrack call is swallowed. |
| `DrylDepthGlass` | `IAsyncDisposable` | The pointer tracker (`dryl.depthglass.untrack`) if attached; swallows `JSDisconnectedException` and `InvalidOperationException` (prerender). |
| `DrylChat` | `IDisposable` | Its `AuraLifecycle`. `dryl.chat.scrollToEnd` registers nothing, so there is no JS to release. |
| `DrylChatComposer` | `IAsyncDisposable` | Its `AuraLifecycle`, the composer listeners (`dryl.chat.detachComposer`) if attached — swallowing `JSDisconnectedException`, `JSException` and `InvalidOperationException` — and then its `DotNetObjectReference`. |
| `DrylMarkdown` | `IDisposable` | Its `AuraLifecycle`. |
| `DrylMessage` | `IDisposable` | Its `AuraLifecycle`. |

Each `AuraLifecycle` disposal cancels a pending aura exit or `Generated` retire
timer, so no re-render is requested of a disposed component (`CODE-05`).
Every JS-attaching component guards its teardown with a flag set only after the
first-render attach succeeded, so a statically prerendered instance makes no JS
call on disposal.

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
