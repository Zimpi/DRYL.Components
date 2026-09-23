# Inputs — Interop

The JS interop surface this category uses, the DI services it registers, and the
cleanup duties each imposes (`CODE-05` in
[`../../harness/code.md`](../../harness/code.md)).

This reference covers `DrylInputText`, `DrylTextarea`, `DrylSelect` and
`DrylFileUpload`.
The category's remaining components are still phase-C work.

## Interop

`DrylInputText` and `DrylTextarea` make no direct JavaScript calls. They mark
their native controls with `data-dryl-input` so field integrations can find
them; they do not themselves register an integration or generate field text.

`DrylSelect` uses these shared entry points in
`code/DRYL.Components/wwwroot/js/dryl.js`:

| Entry point | Caller and observable purpose |
|---|---|
| `dryl.keynav.attach(element)` | `OnAfterRenderAsync` on the first interactive render installs navigation-key default prevention on the combobox trigger. |
| `dryl.keynav.detach(element)` | `DisposeAsync` removes the trigger's registered key handler. |
| `dryl.autocomplete.scrollOptionIntoView(panel, index)` | `ScrollHighlightedIntoView` scrolls the indexed descendant with `role="option"` nearest into view during open-list arrow navigation. |

`dryl.keynav` prevents native defaults for `ArrowUp`, `ArrowDown`,
`ArrowLeft`, `ArrowRight`, `Home` and `End`; it leaves `Tab`, `Enter`,
`Escape` and Space untouched. Select itself handles only the up/down arrows,
`Enter`, Space, `Escape` and `Tab`. Suppression is therefore wider than its
implemented navigation, and Space can still scroll the page. The helper uses
a `WeakMap`, replaces an existing handler on repeated attachment, and permits
repeated detachment. The scroll helper is a no-op for a missing panel or
out-of-range index.

Select delegates portal placement, matching trigger width, outside dismissal,
focus return and enter/exit ownership to `DrylPopover`; it owns no second
portal or dismissal listener. See
[`../E11 Surfaces/_Interop.md`](../E11%20Surfaces/_Interop.md) and
[`../E11 Surfaces/F1 DrylPopover.md`](../E11%20Surfaces/F1%20DrylPopover.md).
Its options stay under that popover's panel during the exit window.

`DrylFileUpload` uses one module of the same file:

| Entry point | Caller and observable purpose |
|---|---|
| `dryl.fileupload.attach(element, dotnetRef)` | `OnAfterRenderAsync` on the first interactive render installs drag listeners on the drop zone, which report the drag-active state back through `SetDragActive`. |
| `dryl.fileupload.detach(element)` | `DisposeAsync` removes those listeners. |

The drop itself is handled by the native file input stretched over the zone;
the module only tracks whether a drag is over it, counting nested
enter/leave pairs so a child element does not flicker the state.

## Services

No input-specific DI service is registered by these three components.
`DrylSelect` injects framework `IJSRuntime`. Form validation uses a cascading
`EditContext` when present. AI inheritance consumes cascading `AiScope`
without injecting an AI activity service directly.

## Cleanup

All three fields own an `AuraLifecycle` and dispose it with the component,
cancelling pending aura retirement/exit work (`CODE-05`). Text input and
textarea implement `IDisposable`; Select implements `IAsyncDisposable`.

Select disposes its aura before detaching the key handler. It detaches only
after an attachment was recorded as successful. Teardown tolerates
`JSDisconnectedException` and `JSException`; first attachment catches only
`JSDisconnectedException`. `ScrollHighlightedIntoView` currently catches
neither. Those asymmetries and the absence of an attachment retry after the
first render are recorded limitations, not new guarantees under H05.

The composed popover owns and tears down its portal, outside/scroll/resize
listeners, pending focus request and shared exit completion. I13 H03's repair
of `dryl.motion.onExit`/`clearExit` also applies to this consumer: rapid
close/reopen, reduced-motion preference changes and disposal must not leave
stale completion callbacks or portalled exits. The contract and repair belong
to E1/E11, with a real Select scene providing integration evidence.

`DrylFileUpload` disposes its `AuraLifecycle`, then detaches the drag
listeners only after its first interactive render attached them, tolerating
`JSDisconnectedException` and `JSException`, and finally disposes its
`DotNetObjectReference`.

## Evidence boundary

I13 H05 requires browser execution with both `dryl.css` and the generated CSS
isolation bundle loaded. bUnit can inspect Select markup and requested interop
arguments; it cannot establish scroll placement, forced-colors focus, actual
focus return or the surviving popover surface. Exercise the real popover on
keyboard open, selection, Escape, outside dismissal and Tab/Shift+Tab, including
close/reopen and removal. Forced-colors emulation and a Windows contrast-theme
pass remain separate evidence. No browser pass is claimed by this spec-only
update.
