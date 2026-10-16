# Data — Interop

The JS interop surface this category uses, the DI services it registers, and the
cleanup duties each imposes (`CODE-05` in
[`../../harness/code.md`](../../harness/code.md)).

Five of the twenty-two components inject an `IJSRuntime`: `DrylCodeBlock`,
`DrylStat`, `DrylTreeView`, `DrylTable` and `DrylCalendar`. The other seventeen
are markup and
CSS end to end, which is why none of them needs a prerender guard — there is no
`OnAfterRenderAsync` interop to guard.

The distribution is lopsided on purpose. `DrylSparkline` computes an entire
chart on the server and emits it as SVG; `DrylImage` gets its loading, error and
AI behaviour from CSS and DOM events; `DrylCitation` gets a whole floating panel
without touching JS, by composing `DrylPopover`. Interop here is reserved for
what the server genuinely cannot do: reach the clipboard, tween text between
renders, measure a table, suppress a browser default, and write a file.

## Interop

| Entry point | Called by | Purpose |
|---|---|---|
| `dryl.clipboard.copy` | `DrylCodeBlock` | Writes the code to the clipboard. Returns whether it succeeded. |
| `dryl.motion.countUp` | `DrylStat` | Tweens the value span's text toward the rendered value. |
| `dryl.tree.attach` / `dryl.tree.detach` | `DrylTreeView` | Suppresses the browser's default scroll for the navigation keys. |
| `dryl.table.initColumnResize` / `dryl.table.disposeColumnResize` | `DrylTable` | Pointer-driven column resizing; calls back into `OnColumnResized`. |
| `dryl.table.focusGrip` | `DrylTable` | Moves focus onto a row's drag grip after a keyboard reorder. |
| `dryl.table.focusHeader` | `DrylTable` | Moves focus onto a header after a keyboard column move. |
| `dryl.table.focusFirstEditor` | `DrylTable` | Moves focus into the first editor when an inline edit starts. |
| `dryl.table.layoutPinned` | `DrylTable` | Re-measures the sticky offsets of pinned columns. |
| `dryl.calendar.scrollToTime` | `DrylCalendar` | Puts `InitialScrollTime` at the top of the time grid's scroller, once per newly shown grid. |
| `dryl.storage.get` / `dryl.storage.set` | `DrylTable` | Reads and writes the persisted table state. |
| `dryl.download.csv` | `DrylTable` | Hands the built CSV to the browser as a download. |
| `DrylMorphEngine` (wrapping `dryl.morph.capture / dryl.morph.play`) | `DrylTable` | Same-document morphs for row morphs. |
| `dryl.popover.*` | `DrylCitation`, transitively | Portalling and placing the citation panel. |

### `dryl.tree` is an alias, and the name is misleading

`window.dryl.tree` is assigned from `window.dryl.keynav`, which is also used by
`DrylSelect`. It does exactly one thing: it installs a `keydown` listener that
calls `preventDefault` for the six navigation keys — the four arrows, `Home` and
`End` — so a Blazor `@onkeydown` handler can move a roving focus without the
page scrolling underneath it. `Tab`, `Enter` and `Escape` are deliberately left
alone, so focus can still leave the widget and activation still works.

It moves no focus and knows nothing about trees. All of `DrylTreeView`'s
keyboard behaviour is C#; this call only stops the browser from competing with
it.

### `DrylStat`'s tween never changes what the DOM says

`dryl.motion.countUp` rewrites the value span's text *between* renders and
always lands on exactly the string Blazor rendered. The consequence is the
contract worth knowing: the markup is identical with and without `CountUp`, so a
bUnit test, a screen reader or a consumer reading the DOM sees the real value
whether or not JS ever ran. That property is what
`tests/DRYL.Components.Tests/DrylStatCountUpTests.cs` pins.

### `DrylTable`'s calls are all best-effort

Every post-render call the table makes — the four focus and layout calls, the
resize attach, the storage read and write — is wrapped so that a missing element
or a closed circuit is swallowed rather than surfaced. The reasoning is that
none of them is load-bearing: a focus that does not land, a pin offset that is
not re-measured or a state that is not restored degrades the table without
breaking it, and the alternative is an exception during a render triggered by
the DOM being one frame behind.

The resize attach is the exception that proves it: a failed attach explicitly
un-sets the "attached" flag, so a later render retries rather than leaving the
handles inert forever.

### `DrylTable` is the category's only two-way interop

It hands JS a `DotNetObjectReference` to itself so the pointer-driven resize
helper can report a finished drag through the `[JSInvokable]`
`OnColumnResized`. It is the only object reference this category hands out, and
therefore the only one it can leak.

### `DrylCitation` — interop by composition

`DrylCitation` calls no JS. It composes `DrylPopover`, and every interop duty of
its panel — the body portal, the placement, the outside click, `Escape`, the
exit animation — belongs to that component and is specified in
`specs/E11 Surfaces/F1 DrylPopover.md`. The citation inherits both the behaviour
and the recorded debt.

**`DrylTable` deliberately does not do this**, and the cost is recorded in
`F16 DrylTable/S2` and `S5`: its per-column filter surface and its
column-visibility menu are hand-built, rendered in place rather than portalled,
and therefore clipped inside a scrolling table — and each answers `Escape` only
if the user has focused it first, which nothing does when it opens.

### `DrylCalendar` — one call, and a timer

In stage one the calendar needs JS for exactly one thing the server cannot do:
setting the time grid's scroll position so that `InitialScrollTime` is in view,
once, after the render that first shows a time grid. The call is best-effort —
a missing element, a prerender or a closed circuit is swallowed — and hands out
no object reference; `dryl.calendar.scrollToTime` keeps no listener and no
timer, which `tests/js/calendar.test.mjs` pins. Everything else — the
layout of events, the narrow layouts, the motion — is markup and CSS.

It also holds a timer, which advances the current-time line while `Now` is
`null`. No timer exists while `Now` is set.

**Stage two adds an editing bridge** (`F22 DrylCalendar/S6` and `S8`;
specified, not yet implemented). Only while `Editable` is set, the calendar
attaches one delegated `pointerdown` listener to its root — the shape of
`dryl.table.initColumnResize` — that runs every move, resize and create in the
browser: pointer capture, the preview, snapping, edge scrolling, the touch hold
and the conflict check against the intervals the server rendered into the
markup. It reports to .NET once, on completion, through a `DotNetObjectReference`;
nothing is sent per pointer move. Keyboard editing (`S7`) needs no bridge: it
runs on the server, one round trip per key press, with `dryl.keynav` keeping the
arrow keys from scrolling the page. The bridge is detached, and the object
reference disposed, when `Editable` is switched off and when the calendar is
disposed; both are guarded against prerender and a closed circuit.

## Services

**None.** No component in this category injects a DRYL service, registers one,
or is registered by `AddDrylComponents()`. `DrylTable`'s state persistence goes
through the browser's storage via `dryl.storage` rather than through a service,
which is why it is scoped to a `PersistStateKey` and not to a user.

## Cleanup

For I13, `dryl.table.disposeColumnResize` cancels any active resize as well as
the delegated root listener. Cancellation restores each cell's pre-gesture
inline width, clears `tbl-resizing`, releases capture/window handlers and never
reports or persists the preview. Pointer identity isolates concurrent input;
stale events after disposal cannot change cells or call `OnColumnResized`.

Nine components implement a disposal.

| Component | Disposes |
|---|---|
| `DrylAvatar` | Unregisters itself from a surrounding `DrylAvatarGroup`. |
| `DrylTreeNode` | Unregisters itself from its `DrylTreeView`. |
| `DrylCodeBlock` | The aura lifecycle's timer, and the cancellation source behind the copy confirmation. |
| `DrylImage` | The aura lifecycle's timer. |
| `DrylStat` | The aura lifecycle's timer. |
| `DrylTimelineItem` | The aura lifecycle's timer. |
| `DrylTreeView` | Detaches the key-suppression listener. `IAsyncDisposable`. |
| `DrylCalendar` | The timer that advances the current-time line while `Now` is `null`. Nothing on the JS side: its one call registers nothing. |
| `DrylTable` | The aura lifecycle's timer, the search debounce, the in-flight data-provider request, the view-transition helper, the resize listener and the object reference behind it. `IAsyncDisposable`. |

Three patterns are worth naming because getting them wrong has broken this
repository before.

**The aura lifecycle holds a timer.** `AuraLifecycle` keeps the AI aura mounted
for one `--dur-slow` beat after the state drops, so it dissolves rather than
snapping away, and the timer that does it must be disposed with the component.
Every component in this category that takes `Ai` disposes one.

**Detaching is guarded by whether attaching happened.** `DrylTreeView` records
whether its first render actually attached, and skips the detach entirely when
it did not. Without that guard, a statically prerendered component throws
`InvalidOperationException` while being torn down, because there is no JS to
call. `DrylTable` guards its resize detach the same way.

**A cancelled delay must not outlive its component.** `DrylCodeBlock`'s copy
confirmation and `DrylTable`'s search debounce both wait on a `Task.Delay` and
then call `StateHasChanged`. Both hold a cancellation source that is cancelled
*and* disposed on disposal, so neither can call into a component that is gone.
`DrylCodeBlock`'s is also cancelled by a second copy, so a rapid double press
does not let the first timer revert the second confirmation early.

`DrylCodeBlock` and `DrylStat` additionally catch `JSDisconnectedException` —
and `DrylStat` `InvalidOperationException` as well, for the prerender pass —
around their own calls, because both can be triggered by a user action or a
render that races the circuit closing.
