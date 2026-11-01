# DrylToastProvider

## Meta
- **State:** Implemented
- **Source:** code/DRYL.Components/Components/Providers/DrylToastProvider.razor
              code/DRYL.Components/Toasts/IDrylToastService.cs
              code/DRYL.Components/Toasts/DrylToastService.cs
              code/DRYL.Components/Toasts/IDrylToastReference.cs
              code/DRYL.Components/Toasts/IDrylToastInstance.cs
              code/DRYL.Components/Toasts/DrylToastReference.cs
              code/DRYL.Components/Toasts/ToastOptions.cs
              code/DRYL.Components/Toasts/ToastParameters.cs
              code/DRYL.Components/Toasts/ToastPosition.cs

## User Story

As a Blazor developer, I want to place one component in my root layout and then
raise a toast from anywhere with one line of C# — from a button's handler or
from a background job — so that feedback appears where the user can follow it:
out of the button they pressed, or out of the notification bell when nobody
pressed anything, and back into the bell when they missed it.

## Description

`DrylToastProvider` is the host. It is mounted **once**, in the root layout,
subscribes to `IDrylToastService` and renders every active toast as a
[`DrylToast`](../E11%20Surfaces/F2%20DrylToast.md) in a fixed stack at one of
six corners. It cascades `IDrylToastInstance` to a custom body, so the body can
close its own toast or change its AI state.

Each toast is handed to `dryl.toast`, which owns three things the server cannot
do well: the auto-dismiss timer (paused while the pointer is over the toast, in
lockstep with the CSS pause of the countdown bar), the entry and the leave.

**Where a toast comes from** (idea I34). A toast is part of a story: it answers
something. When it answers a click, it grows out of the element that was
clicked — `ToastOptions.Origin`, typically a `DrylButton.Element`. When nobody
clicked — a background job finished, the server pushed something — it comes out
of the notification bell, where such news lives. Only when neither exists does
it unfold in place, from a capsule around its icon.

**Where it goes.** "What came from the bell goes back to the bell": a toast that
came out of the bell and expires unread folds into its capsule, flies back into
the bell and becomes an unread entry in `IDrylNotificationService`, so it is not
lost. A toast that answered a click is direct feedback; filing every "Saved" in
the inbox would flood it, so it simply leaves. A toast the user or code closed
always simply leaves.

The bell is found in the DOM: `DrylNotifications` marks its bell with
`data-dryl-inbox` in service-driven mode — the one mode in which an entry this
provider adds to the service actually appears behind that bell.

Removal is defensive in both directions. A toast is hidden until `dryl.toast`
has taken it over, so an entry morph never starts from a flash of the finished
toast; when that hand-off fails, C# reveals the toast without motion and runs
its timer itself. A leave that JS never answers is cut off by a C# watchdog, so
a dead toast can never stay on screen.

## Public API

| Member | Type | Default | Purpose |
|---|---|---|---|
| `Position` | `ToastPosition` | `ToastPosition.BottomRight` | Corner of the stack: `TopRight`, `TopLeft`, `TopCenter`, `BottomRight`, `BottomLeft`, `BottomCenter`. |

The rest of the API is the service it hosts — `IDrylToastService`,
`IDrylToastReference`, `IDrylToastInstance`, `ToastOptions` and
`ToastParameters` — written out in [`_Api.md`](_Api.md). Usage is two lines in
an app: `AddDrylComponents()` in startup and `<DrylToastProvider />` in the
root layout.

## Acceptance Criteria

### Mounting and lifecycle

- The provider subscribes to the service's added, close-requested and updated
  events when it initializes, and unsubscribes from all three on dispose.
- Every `Show*` call renders one toast without the caller placing any markup.
- `Show<TComponent>` renders the component as the toast's body with the given
  `ToastParameters` as its parameters, and `ToastOptions.Title` above it.
- Each toast's `IDrylToastInstance` is cascaded to its body, and the cascade is
  not fixed, so `SetAi` reaches the card.
- Each toast is keyed by its id at the top level of the rendered list, so
  removing one never rebuilds another: the remaining toasts keep their DOM
  elements, their countdown bars and their timers.
- `IDrylToastReference.OnClosed` is raised once, after the toast's leave has
  finished and it has been removed.
- `CloseAll()` closes every visible toast.

### Timer

- A toast with `Duration` above zero expires after that many milliseconds.
- Hovering a toast pauses its timer and its countdown bar together; leaving it
  resumes both.
- `Duration = 0` never expires.
- A detached toast's timer never fires, even when its callback was retained.

### Entry

- A toast is hidden until `dryl.toast` has taken it over and set
  `data-dryl-entered` on its slot.
- A toast whose `Origin` is connected and on screen starts in that element's
  shape and place and morphs into its own place in the stack, its text fading
  in as it arrives.
- A toast without an origin — or whose origin is no longer connected or on
  screen — comes out of the first visible `[data-dryl-inbox]` bell: a capsule
  around its icon leaves the bell, flies to its place in one continuous motion
  and unfolds while it lands; the bell answers with a nudge.
- With neither origin nor bell, the toast unfolds from a capsule around its icon
  in place, arriving from the side of the screen its stack sits on.
- When `dryl.toast` cannot take the toast over, the toast is shown without
  motion and expires on a C# timer instead.

### Leave

- A toast that came out of the bell, expired, carries a title or a message and
  has `KeepInInbox` set folds into its capsule, flies into the bell and is then
  added to `IDrylNotificationService` as an unread entry.
- That entry takes the toast's title as its title and the message as its
  message; a toast without a title gives its message as the entry's title. It
  keeps the toast's icon (or its variant's) and its AI state.
- No entry is added when the bell is gone by the time the toast leaves; the
  toast then shrinks and fades in place.
- When `dryl.toast` does not answer the leave in time, a toast that would have
  returned into the bell is filed anyway — losing the message is the worse
  failure.
- A toast with an origin, a toast that unfolded as a capsule, a toast closed by
  the user or by code, and a toast with `KeepInInbox = false` never add an
  entry; they shrink, blur and fade in place.
- After either leave, the space the toast held closes, so its neighbours glide
  into place instead of jumping.
- A leaving toast takes no pointer input.
- A toast is removed at the latest after the watchdog (3 s), whether or not
  `dryl.toast` answered.

### Reduced motion

- With `prefers-reduced-motion: reduce`, every entry and every leave is a plain
  fade; the decision where a toast came from and whether it is filed in the
  inbox is the same.

### Appearance

- The stack sits in a fixed host at the chosen corner, above page content
  (`--z-toast`), with `--sp-3` between toasts and `--sp-5` padding.
- Clicks pass through the host's empty space; only the toasts take pointer
  input.

## Recorded gaps

- The host is a polite live region around toasts that are live regions
  themselves — a toast can be announced twice (see the `DrylToast` spec).
- The origin must be passed by hand; there is no automatic capture of the last
  activated element. Deliberate (I34): after a slow server call the last click
  can be somewhere else entirely.
- A custom-body toast (`Show<TComponent>`) without a `Title` has no text to file
  and never enters the inbox.
- `ToastOptions.PauseOnHover` is documented but read nowhere: hovering always
  pauses. Pre-existing.

## Cross-cutting evidence (`SPEC-05`)

- **Both color modes** — the provider adds no colour; the toasts are
  `DrylToast`'s. Entry from a button and from the bell, the return into the bell
  and the capsule were driven with Playwright in a Blazor Server host on
  2026-11-01 in dark and light mode.
- **Enter/exit animation** — the entry and leave above, on `--dur-*`,
  `--ease-out`, `--ease-spring` and `--ease-viscous` (`DESIGN-10`, sign-off in
  I34); neighbours glide on removal.
- **Keyboard and a11y** — toasts keep their own close button and live region;
  the motion changes no focus order or semantics (`UX-07`).
- **AI mode** — the toasts' (`SetAi`, cascaded); an inbox entry keeps the AI
  state.
- **Tests** — `tests/DRYL.Components.Tests/DrylToastProviderTests.cs`,
  `tests/js/toast.test.mjs`.
- **Demo page** — on `DRYL.Website`; `ComponentCatalog` is maintained there
  (`REL-04`).
