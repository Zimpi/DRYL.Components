# Inputs — Interop

The JS interop surface this category uses, the DI services it registers, and the
cleanup duties each imposes (`CODE-05` in
[`../../harness/code.md`](../../harness/code.md)).

This reference covers `DrylInputText`, `DrylTextarea`, `DrylSelect`,
`DrylFileUpload`, `DrylAutocomplete`, `DrylMultiSelect`, `DrylTimePicker`,
`DrylInputMask`, `DrylDatePicker`, `DrylChipInput`,
`DrylSegmentedControl` and `DrylMarkdownEditor`. The list ink of the four list inputs (I38) is driven by
`DrylPopover`, not by the components; it is documented in
`../E11 Surfaces/_Interop.md`. `DrylInputNumber`, `DrylInputPassword`,
`DrylSegment`, `DrylFormField` and `DrylValidationSummary` make no JavaScript
calls.

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
`Escape` and Space untouched. An optional second argument, an array of key
names, replaces that set for the element it is attached to. Select itself handles only the up/down arrows,
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

`DrylAutocomplete` uses one entry point of the same file:

| Entry point | Caller and observable purpose |
|---|---|
| `dryl.autocomplete.scrollOptionIntoView(panel, index)` | `ScrollHighlightedIntoView` scrolls the indexed `role="option"` descendant — the create option included, as the last one — nearest into view during arrow navigation. |

Like Select it delegates portal, width, outside dismissal and the list's
enter/exit to `DrylPopover`; the popover's outside press reaches the
component as `OnClose`, which is where a pending query is committed when
`OnCreate` is set. The create option's enter/exit belongs to `DrylPresence`
(`dryl.motion.onExit`, E1). The search runs on the thread pool; its results
are applied through `InvokeAsync`, never written from the pool thread.

`DrylMultiSelect` uses the same three entry points as Select:
`dryl.keynav.attach` on its trigger after the first render, `dryl.keynav.detach`
from `DisposeAsync`, and `dryl.autocomplete.scrollOptionIntoView` while the
highlight moves. With `Searchable` the focusable element is the search text
field, and it attaches with a key list:
`dryl.keynav.attach(field, ['ArrowUp', 'ArrowDown', 'Enter'])`. The list
replaces the default navigation keys for that element only, so the field keeps
its caret keys and `Enter` never submits a surrounding form. Since every option
stays mounted, the scroll helper's index is the option's index in `Items`.
While a filtered row's `grid-template-rows` track transitions, the list ink
(`E11 Surfaces/_Interop.md`) re-places itself every frame, so it stays on the
row it covers instead of landing where the row was before the list moved.

`DrylTimePicker` uses three entry points of `dryl.timepicker`:

| Entry point | Caller and observable purpose |
|---|---|
| `dryl.timepicker.scrollToActive(panel)` | After an open, scrolls each column to its staged cell. |
| `dryl.timepicker.focusPanel(panel)` | After an open, moves focus into the panel once the popover reveals it. |
| `dryl.timepicker.restoreFocus(panel, input)` | After a close, hands focus back to the field while it is still inside the closing panel; answers whether it moved focus. |

`DrylInputMask` uses `dryl.inputmask`:

| Entry point | Caller and observable purpose |
|---|---|
| `dryl.inputmask.attach(element, pattern, dotnetRef)` | `OnAfterRenderAsync`, after the first interactive render and again whenever the active pattern changes to a different nonempty one, installs `input` and `paste` listeners on the native input. Each event re-masks the text, puts the caret at its end and calls the `[JSInvokable]` `OnMaskedValue` with the masked string. Attaching first detaches any previous listeners of that element. |
| `dryl.inputmask.detach(element)` | `DisposeAsync` removes the listeners; a no-op for an element without them. |

The listeners are kept in a `WeakMap` keyed by the element.

`DrylDatePicker` uses two entry points of `dryl.datepicker`:

| Entry point | Caller and observable purpose |
|---|---|
| `dryl.datepicker.focusDay(panel, dayNumber)` | After an open or a keyboard move, installs the shared panel key policy (`drylPanelKeys`, navigation keys suppressed) and focuses the day button carrying `data-day="{dayNumber}"`, or the panel when there is none — deferred through `drylPanelFocus` until the popover reveals the panel. |
| `dryl.datepicker.restoreFocus(panel, input)` | After `Escape` or a selection closes the panel, hands focus back to the field while it is still on `<body>` or inside the panel; answers whether it moved focus, so the one-shot open suppression is dropped when it did not. |

Like `DrylTimePicker`, it delegates portal, placement, outside dismissal and
enter/exit to `DrylPopover`; see `../E11 Surfaces/_Interop.md` for
`drylPanelFocus` and `drylPanelKeys`.

`DrylChipInput` and `DrylSegmentedControl` call no `dryl.js` entry point. They
move focus with Blazor's built-in `ElementReference.FocusAsync` — the chip
field when its box is clicked, the segmented control onto the segment an arrow
key selected.

`DrylMarkdownEditor` uses one module of the same file, `dryl.mdeditor`:

| Entry point | Caller and observable purpose |
|---|---|
| `dryl.mdeditor.attach(root)` | `OnAfterRenderAsync` on the first interactive render. Finds the source (`[data-mde-source]`) and the toolbar (`[data-mde-toolbar]`) under the editor's frame and installs five listeners: `keydown` on the source (shortcuts, list continuation) and `click`, `mousedown`, `keydown` and `focusin` on the toolbar. Attaching again replaces them. |
| `dryl.mdeditor.sync(root)` | `OnAfterRenderAsync` after a render that changed which toolbar buttons are enabled. Leaves exactly one enabled `button[data-mde-cmd]` with `tabindex="0"` and every other one with `-1`; with none enabled, none is a tab stop. |
| `dryl.mdeditor.detach(root)` | `DisposeAsync` removes the five listeners. Repeated detachment is a no-op. |
| `dryl.mdeditor.run(root, command)` | Not called from .NET. Applies one command to the source's selection; refuses when the source is disabled or read-only or the frame carries `data-mde-view="preview"`. |
| `dryl.mdeditor.transform(value, start, end, command)`, `dryl.mdeditor.enter(value, caret)` | Not called from .NET. The pure text edits behind `run` and the Enter key; each returns `{ start, end, text, selStart, selEnd }` or `null`. |

A toolbar press is handled by the `click` listener in the browser and never
travels to the server: the button names its command in `data-mde-cmd` (`bold`,
`italic`, `strike`, `heading`, `bullet`, `ordered`, `task`, `quote`, `code`,
`link`). The edit is made with `document.execCommand('insertText' | 'delete')`
so that it joins the browser's undo stack and raises a native `input` event;
where the browser refuses, the module falls back to `setRangeText` and
dispatches a bubbling `input` event itself. Either way the value reaches the
component through its ordinary `input` handler, and the echo guard of
`dryl.inputs` records it like typed text. `mousedown` on a toolbar button is
default-prevented so that a press does not move focus, and with it the
selection, out of the source. The shortcut modifier is Ctrl, or Cmd alone on
Apple platforms (read from `navigator.platform`), where Ctrl+B and Ctrl+E are
native caret movements. Tab is never handled.

The module starts no timer and no animation frame.

## Services

No input-specific DI service is registered by any component of this category.
`DrylSelect`, `DrylInputMask`, `DrylDatePicker` and `DrylMarkdownEditor` inject framework
`IJSRuntime`. Form validation uses a cascading `EditContext` when present;
`DrylFormField` and `DrylValidationSummary` read it without binding a value.
`DrylSegmentedControl` cascades itself (`IsFixed="true"`) so that its
`DrylSegment` children can register. AI inheritance consumes cascading `AiScope`
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

`DrylAutocomplete` implements `IDisposable`: it disposes its `AuraLifecycle`,
cancels and disposes the running search's `CancellationTokenSource`, and marks
itself disposed so a search or a create that finishes later neither writes
state nor renders.

`DrylFileUpload` disposes its `AuraLifecycle`, then detaches the drag
listeners only after its first interactive render attached them, tolerating
`JSDisconnectedException` and `JSException`, and finally disposes its
`DotNetObjectReference`.

`DrylInputMask` implements `IAsyncDisposable`: it disposes its
`AuraLifecycle`, then — only after an interactive render — calls
`dryl.inputmask.detach`, swallowing any exception, and finally disposes its
`DotNetObjectReference`. Its attach calls are not guarded.

`DrylInputNumber`, `DrylInputPassword`, `DrylDatePicker` and `DrylChipInput`
implement `IDisposable` and dispose their `AuraLifecycle`. `DrylDatePicker`
holds no listener of its own: the panel key listener installed by
`dryl.datepicker.focusDay` lives on the popover's panel node and goes with it
(see `../E11 Surfaces/_Interop.md`).

`DrylMarkdownEditor` implements `IAsyncDisposable`. It disposes its
`AuraLifecycle` through `InputBase`'s `Dispose(bool)`, which `DisposeAsync`
invokes first so that the base's `EditContext` subscription is released too —
the renderer calls only `DisposeAsync` on a component that has both. It then
calls `dryl.mdeditor.detach` once, only after `attach` succeeded on an
interactive render, tolerating `JSDisconnectedException`, `JSException` and
`OperationCanceledException`. `attach` and `sync` tolerate
`JSDisconnectedException` only. The composed `DrylPresence` owns the preview's
exit and its own cleanup (E1).

`DrylValidationSummary` implements `IDisposable` and unsubscribes from
`EditContext.OnValidationStateChanged`; it also moves the subscription when the
cascading `EditContext` changes. `DrylSegmentedControl` clears its segment
registrations on dispose, and each `DrylSegment` unregisters itself from its
control on dispose. `DrylFormField` holds nothing to release.

## Evidence boundary

I13 H05 requires browser execution with both `dryl.css` and the generated CSS
isolation bundle loaded. bUnit can inspect Select markup and requested interop
arguments; it cannot establish scroll placement, forced-colors focus, actual
focus return or the surviving popover surface. Exercise the real popover on
keyboard open, selection, Escape, outside dismissal and Tab/Shift+Tab, including
close/reopen and removal. Forced-colors emulation and a Windows contrast-theme
pass remain separate evidence. No browser pass is claimed by this spec-only
update.
