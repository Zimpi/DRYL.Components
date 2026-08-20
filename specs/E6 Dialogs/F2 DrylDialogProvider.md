# DrylDialogProvider

## Meta
- **State:** Implemented
- **Source:** code/DRYL.Components/Dialogs/DrylDialogProvider.razor
              code/DRYL.Components/Dialogs/DrylDialogService.cs
              code/DRYL.Components/Dialogs/DrylDialogReference.cs
              code/DRYL.Components/Dialogs/IDrylDialogService.cs
              code/DRYL.Components/Dialogs/IDrylDialogReference.cs
              code/DRYL.Components/Dialogs/IDrylDialogInstance.cs

## User Story

As a Blazor developer building an app on DRYL, I want to place one component in
my root layout and then open dialogs from anywhere by calling a service, so that
opening a dialog is a line of C# in the code that needs it, instead of a piece of
markup, a boolean and a conditional block in whichever page happens to be
showing.

## Description

`DrylDialogProvider` is the host. It is mounted **once**, in the root layout,
takes no parameters, and renders nothing at all until someone calls the dialog
service. From then on it owns everything that is not the dialog's own content:
the backdrop, one full-viewport layer per dialog, the cascaded
`IDrylDialogInstance`, the focus trap, the body scroll lock, the `Escape` key,
and the exit animation with the moment of removal at its end.

Its shape follows from one decision: **one shared backdrop, one layer per
dialog.** Two dialogs on screen — a stack, or a handoff mid-swap — must not
double the dark wash and the blur, which is exactly what a backdrop per dialog
would do. So the backdrop is the container, and each dialog gets its own layer
inside it.

The choreography for sequential dialogs is the part worth reading twice. Agent
flows produce chains: close A, immediately open B. The default is a cross-fade —
the backdrop persists and only its opacity transitions, A plays its exit while B
enters a beat later. A caller who sets `DialogOptions.AnimateHandoff` gets
something stronger instead: A is finalized at once, and its removal together
with B's mount is wrapped in a single browser view transition, so the shell
glides to the new size while title, body and footer cross-fade independently.

Removal is deliberately defensive. An entry stays mounted through its exit
animation and leaves when JS reports the animation ended — but a lost report
would leave an invisible full-viewport overlay eating every click on the page.
A C#-side watchdog finalizes the entry regardless, and the exiting layer stops
taking pointer events immediately. That is a scar, not a precaution: see
[`_Interop.md`](_Interop.md).

## Public API

The component takes **no parameters**. Its API is the service it hosts —
`IDrylDialogService`, `IDrylDialogReference` and `IDrylDialogInstance`, all
specified in [`_Api.md`](_Api.md).

Usage is two lines in an app: `AddDrylComponents()` in startup, and
`<DrylDialogProvider />` in the root layout.

## Acceptance Criteria

### Mounting and lifecycle

- The component renders nothing while no dialog is open — no backdrop, no
  layer, no overlay.
- The component subscribes to the service's added, close-requested and updated
  events when it initializes.
- The component unsubscribes from all three when it is disposed.
- Calling `ShowAsync` renders the requested dialog type without the caller
  placing any markup.
- The parameters passed to `ShowAsync` reach the dialog component as its own
  `[Parameter]` values.
- Each dialog is rendered with a key derived from its instance id, so adding or
  removing one never re-uses another's component state.
- A dialog's `IDrylDialogInstance` is cascaded to the rendered component.
- The cascade is not fixed, so a state change on the instance — an AI state, for
  example — reaches the dialog.

### Layering

- All open dialogs share exactly one backdrop.
- Each dialog is rendered in its own layer above that backdrop.
- Two dialogs on screen at once produce one dark wash and one blur, not two.
- The backdrop stays mounted while any dialog is open, including across a
  close-then-open sequence.
- A fullscreen dialog reaches the viewport edges: the inset that centres the
  other sizes is removed for it, on both the backdrop and the layer.

### Dismissal

- A click on a dialog's layer outside the dialog cancels that dialog when
  `DialogOptions.CloseOnBackdropClick` is set.
- A click on the layer does nothing when `CloseOnBackdropClick` is `false`.
- A click inside the dialog never reaches the layer, so interacting with the
  content cannot dismiss it.
- `Escape` cancels the dialog whose layer has focus when
  `DialogOptions.CloseOnEscape` is set.
- `Escape` does nothing when `CloseOnEscape` is `false`.
- `Escape` reaches the dialog the user is in, not merely the topmost one.
- Closing resolves the caller's awaited `Result` exactly once, whether the
  dialog closed itself, was cancelled by `Escape`, by the backdrop or
  programmatically.

### Focus and scroll

- Opening a dialog moves focus into it.
- Opening a dialog focuses its first focusable element, or the layer itself when
  it holds none, so focus is never left behind on the page.
- `Tab` and `Shift+Tab` cycle within the open dialog and do not reach the page
  behind it.
- Focus that has escaped the layer is pulled back into it on the next `Tab`.
- Closing a dialog returns focus to the element that had it before the dialog
  opened.
- Closing a dialog does **not** return focus when a follow-up dialog has already
  taken it, so a handoff does not break the successor's trap.
- Opening a dialog locks scrolling of the page behind it.
- Scrolling is unlocked when the last dialog closes, not when the first one
  does.

### Exit and removal

- A closing dialog plays its exit animation before it is removed from the render
  tree.
- The entry is removed as soon as the exit animation ends.
- The entry is removed even if the animation's end is never reported, through a
  watchdog on the component's own side.
- An exiting layer takes no pointer events, so a fading or stuck overlay can
  never swallow a click.
- The backdrop fades out only once every remaining dialog is exiting.
- The backdrop's exit is a transition rather than an animation, so a fade
  interrupted by a new dialog reverses from its current opacity instead of
  restarting.
- Removing an entry detaches its JS listeners and disposes the object references
  it handed to JS.

### Sequential dialogs

- A dialog opened while a sibling is still exiting enters with a short delay, so
  the swap reads as a sequence rather than a collision.
- That delay is `--dur-fast`, and the incoming dialog holds its start state until
  it elapses instead of flashing at full opacity first.
- A dialog opened with `AnimateHandoff` while a predecessor is exiting morphs
  into it through a single view transition instead of the cross-fade.
- The predecessor is finalized immediately in that case, and never plays its own
  exit animation.
- The predecessor's removal and the successor's mount are applied as one render,
  so the transition captures one before-state and one after-state.
- A dialog opened with `AnimateHandoff` while no predecessor is exiting opens
  normally.
- The morph falls back to the cross-fade in browsers without view-transition
  support, during prerender, and under reduced motion.
- `DialogOptions.HandoffStyle` selects the morph tier, and defaults to the
  glass-merge tier rather than the shape-only one.
- A handoff transition uses a transition instance of the provider's own, so it
  is unaffected by any other view transition running in the host application.

### Robustness

- Every JS call is guarded against a disconnected circuit, so a client that has
  gone away cannot throw out of a render or a disposal.
- Disposal detaches the interop of every layer still mounted, cancels every
  watchdog and disposes every object reference and the transition instance.
- A second close request for a dialog that is already exiting is ignored rather
  than starting a second exit.

### Keyboard and accessibility

- The dialog's `role="dialog"` and `aria-modal` come from `DrylDialog` (`F1`);
  the provider adds the behaviour that makes them true — the trap, the scroll
  lock and `Escape`.
- Each layer is focusable programmatically but takes no tab stop of its own, so
  it can receive focus when a dialog has no focusable content without adding a
  stop when it does.
- The page behind an open dialog is unreachable by keyboard.

### Appearance

- Every color the component renders comes from a token; the component names no
  literal color (`DESIGN-01`).
- The backdrop derives from `--backdrop` and frosts what is behind it, which is
  the one place in the library where a full-viewport blur is warranted
  (`DESIGN-07`).
- The backdrop sits at `--z-modal`.
- The component branches on no color mode and holds no mode-assuming value, so
  the same markup serves light and dark (`DESIGN-02`).

### AI mode

- The component takes **no `Ai` parameter**, deliberately (`AI-05`). It is
  chrome with no content of its own: the AI state belongs to the dialog frame
  inside it, where `DialogOptions.Ai` and `IDrylDialogInstance.SetAi` put it, and
  an aura on a full-viewport backdrop would ring the entire screen rather than
  the surface doing the work.
- The provider re-renders when an instance's AI state changes, so `SetAi` from
  inside a dialog reaches the frame.

## Recorded debt

- The backdrop's blur radius is written as a literal in
  `code/DRYL.Components/wwwroot/dryl.css` rather than as `--glass-fx-float`. The
  backdrop is not a glass panel — it is the wash behind one — and no token covers
  it today. Recorded as debt, not as compliance.
- The exit watchdog's grace period is a literal in the component. It is a
  robustness timeout rather than an animation duration, so `DESIGN-10` does not
  bind it, but it must stay longer than the exit animation it backs up: a
  shortened `--dur-med` would not break it, a lengthened one would.
- The view-transition name used for a handoff is a fixed string, so only one
  handoff chain may be mid-transition per provider. That matches the sequential,
  non-stacked pattern the option is for; a second simultaneous chain voids its
  own morph rather than misbehaving.

## Cross-cutting evidence (`SPEC-05`)

- **Both color modes** — token-only colors, verified by
  `node scripts/check-light-sync.mjs` and
  `node scripts/validate-light-contrast.mjs`. `--backdrop` is mode-dependent and
  defined in both LIGHT-TOKEN-SET copies; the component defines no mode-specific
  rule.
- **Enter/exit animation** — the backdrop fades in and out, and the provider is
  the component that makes the dialog's *exit* possible at all by holding the
  entry mounted until the animation has finished (`DESIGN-12`).
- **Keyboard and a11y** — the "Keyboard and accessibility" and "Focus and
  scroll" criteria above. This component is where the library's modal keyboard
  contract is implemented.
- **AI mode** — an explicit **no**, with its reason under "AI mode" above.
- **Demo page** — `DRYL.Website/Components/Pages/DemoDialog.razor`; every
  example on that page runs through this provider, and
  `Components/Examples/Dialog/Sequential.razor` is the handoff case.
- **`ComponentCatalog`** — covered by the `"Dialog"` / `dialog` entry in
  `DRYL.Website/Components/ComponentCatalog.cs`. The provider has no entry of its
  own and should not: it is not a component a reader browses for and places on a
  page, it is the one-line mount that page documents.
