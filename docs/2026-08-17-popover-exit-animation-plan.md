# Plan: an exit animation for the `DrylPopover` surface (I4, route 2)

**Idea:** [`../ideas/I4 An exit animation for the popover surface.md`](../ideas/I4%20An%20exit%20animation%20for%20the%20popover%20surface.md) — `Ready`, route 2 signed off 2026-08-17.
**Spec:** [`../specs/E11 Surfaces/F1 DrylPopover.md`](../specs/E11%20Surfaces/F1%20DrylPopover.md)
**Rules in play:** `DESIGN-12` (everything that mounts conditionally animates out), `DESIGN-13` (reuse the vocabulary), `SPEC-01`, `REL-02`.

## The shape

Today the close is one render: `.is-open` drops, `PanelContent` unmounts, and
`OnAfterRenderAsync` tears the portal down. Nothing can be seen leaving.

Route 2 puts one state between the close and the teardown. While that state is
on, the panel keeps everything it had — content mounted, both visibility keys,
its place under `<body>` — and gains `.is-exiting`, which swaps its
`animation-name` from `popover-in` to the existing `presence-out-fade`. When
the animation ends, the portal is torn down as it is today.

This is the `DrylDialogProvider` mechanism, applied to a component that owns a
single surface instead of a list of them: `IsExiting` + `ExitAttached` +
`ExitWatchdog` there become `_exiting` + `_exitAttached` + `_exitWatchdog` here.
Using the same shape is the point — the failure modes are already known and
already paid for.

## Tasks

Each task carries its own verification and lands as one commit.

### Task 1 — the exit state in `DrylPopover.razor`

- `_exiting`, `_exitAttached`, `_exitWatchdog` (`CancellationTokenSource`),
  `_exitRef` (`DotNetObjectReference<ExitCallback>`).
- `SetOpenAsync(false)` starts the exit **only while `_portaled`**. A popover
  that was never on screen has nothing to animate and closes as it does today —
  this is also what keeps a statically prerendered component unchanged.
- The content gate becomes `Open || _exiting`; `is-open` follows the same
  condition; `is-exiting` is added while `_exiting`.
- `OnAfterRenderAsync` releases the portal only while **not** exiting, and
  attaches `dryl.motion.onExit(_panel, _exitRef, { name: "presence-out" })`
  once per exit.
- `OnExitFinished` (from JS) and the watchdog both land on one idempotent
  `FinishExitAsync`.
- **Re-open during an exit** cancels it: watchdog cancelled, `clearExit` called,
  `_exiting` false. The node never left `<body>`, so no second portal is opened;
  dropping `.is-exiting` moves `animation-name` back to `popover-in`, which
  restarts the entrance for free.
- `DisposeAsync` cancels the watchdog and disposes both refs before releasing.

**Verify:** `dotnet build DRYL.slnx -c Release`.

### Task 2 — the binding in `DrylPopover.razor.css`

```css
.popover-panel.is-open.is-positioned.is-exiting {
    animation: presence-out-fade var(--dur-fast) var(--ease-out) forwards;
    pointer-events: none;
}
```

`forwards` holds opacity at 0 between the animation ending and the render that
drops the visibility keys. `pointer-events: none` is not decoration: for 140 ms
the panel is a full-size element on the `--z-popover` layer that the user
believes is gone, and an overlay that eats clicks it should not is the exact
failure the dialog watchdog exists to prevent.

The CSS comment claiming the atomic drop means "no empty surface box ever
flashes" is rewritten — it described the `DESIGN-12` violation as a virtue, and
after this task it is also simply untrue.

**Verify:** `node scripts/check-motion-tokens.mjs`, `node scripts/check-light-sync.mjs`.

### Task 3 — the false doc comment in `dryl.js`

`dryl.popover`'s module comment says `open()` "drops a comment placeholder at
the panel's original slot". It does not, and the spec already records that.
Corrected here because this plan touches the close path it describes.

**Verify:** `grep` the module comment against `open`/`close`; no behaviour to test.

### Task 4 — the spec, the changelog, the tests

- `F1 DrylPopover.md`: the motion criteria, the `Enter/exit animation` line
  under cross-cutting evidence, and the retirement of the `DESIGN-12` entry
  under `Recorded debt`. New criteria for the exit, the re-open, the watchdog
  and `prefers-reduced-motion`. `State` stays `Implemented` — spec and code
  change in one session (`SPEC-04`).
- `CHANGELOG.md`: the entry joins the unreleased `2.23.0` block; `<Version>`
  stays put, because 2.23.0 has not shipped.
- Tests: the exit state is observable in bUnit without any JS — the panel keeps
  `is-open` and gains `is-exiting` on close, and the watchdog finishes it. That
  is worth a first `DrylPopoverTests`, which the spec currently records the
  absence of.

**Verify:** `dotnet test DRYL.slnx -c Release`, `node scripts/check-spec-coverage.mjs`,
`node scripts/check-harness-links.mjs`.

### Task 5 — measure it in the browser

Both colour modes, `/components/popover`. Sample computed style per frame
around a close, as the original finding did, and confirm: opacity leaves 1,
the surface is still painted while it does, the node returns to its anchor only
afterwards, and a click during the exit reaches the page rather than the panel.
Then the same under `prefers-reduced-motion: reduce`.

## The one contract this changes

`PanelContent` currently unmounts in the same render that sets `Open` to
`false`. After task 1 it stays mounted for the length of the exit. Library code
relies on the old timing — `DrylAiFieldTests.Prompt_enter_starts_run_with_typed_instruction`
is the known case, found when the rejected `DrylPresence` repair was measured.

Under bUnit no `animationend` ever arrives, so the watchdog is what closes the
gap there, and any test that asserts "content gone" immediately after a close
has to wait for it. Such a test is asserting a timing detail rather than
behaviour; it is fixed in the test, not by weakening the animation. If more
than a handful turn red, that is the signal to stop and reconsider the shape
rather than to keep patching tests.

## Risks, in the order they are likely to bite

1. A second open arriving mid-exit. Covered explicitly in task 1; verified in
   the browser in task 5 by clicking the trigger twice quickly.
2. The panel eating a click while invisible. `pointer-events: none`, task 2.
3. A popover disposed mid-exit leaking its portal. `DisposeAsync` runs the
   release regardless of `_exiting`.
4. `animationend` never arriving on a live circuit (DOM churn, interop race).
   The watchdog, at 400 ms against a 140 ms animation — the same ratio the
   dialog provider uses.
