# DrylPopover

## Meta
- **State:** Implemented
- **Source:** code/DRYL.Components/Components/Surfaces/DrylPopover.razor
              code/DRYL.Components/Components/Surfaces/DrylPopover.razor.css
              code/DRYL.Components/Components/Surfaces/PopoverPlacement.cs

## User Story

As a Blazor developer building an application on DRYL, I want a panel that opens
next to a trigger I supply and stays where I put it — over a scrolling card, out
of an `overflow: hidden` ancestor, dismissed by a click outside — so that I can
build a filter panel, an info bubble or a dropdown without writing positioning,
portalling and dismissal code myself.

## Description

`DrylPopover` is the library's anchored-panel primitive. It renders an anchor
element, a trigger slot the consumer fills, and a panel whose body is shown
while `Open` is `true`. It owns four things and nothing else: the open state and
its two-way binding, the move of the panel node to `<body>` and its placement
there, dismissal by a click outside, and — under the condition described below —
dismissal by `Escape`.

It is the surface the rest of the library is built on. `DrylMenu`,
`DrylSelect`, `DrylMultiSelect`, `DrylAutocomplete`, `DrylDatePicker`,
`DrylTimePicker`, `DrylNotifications`, `DrylCitation`,
`DrylCanvasWorkspace`'s version history and the agents package's `DrylAiField`
all place one rather than emitting their own floating panel. That is why it is
`F1` of this category: the seven other `E11 Surfaces` components stand beside
it, not on it, while eight components in four other categories stand on this
one. `F2`–`F8` of this category are unassigned and stay free; nothing here
claims them.

**The panel element is always rendered; only its body is conditional.** This is
the shape the whole component turns on, and it is not an optimisation:
`dryl.popover.open` moves *this node* to `<body>` and `dryl.popover.close` moves
it back, so Blazor must never structurally remove it while JS holds it. What is
conditional is the content: `@if (Open) { @PanelContent }`. Measured on
`/components/popover`, seven anchors on the page carry seven panel nodes at all
times, each empty and `visibility: hidden` while closed.

**Visibility is a two-key gate**, `.is-open.is-positioned` in
`DrylPopover.razor.css`. Blazor adds `is-open` with the open render; JS adds
`is-positioned` only after the node has been placed. Before that second key the
panel is present, laid out and invisible — and `focus()` on an invisible element
is silently a no-op. Everything about focus in this component follows from that
one fact.

**Focus: the consumer decides whether, the portal decides when.** The popover
never moves focus on its own. A consumer that wants focus in its panel calls its
own module (`dryl.menu.focusPanel`, `dryl.datepicker.focusDay`,
`dryl.timepicker.focusPanel`), which tries immediately and — because a parent's
`OnAfterRenderAsync` runs before its child's, so the popover has not portalled
or revealed anything yet — usually fails, and parks a one-shot request on the
panel node as `__drylPendingFocus`. `dryl.popover.open` applies that request in
the same breath as it adds `.is-positioned`, and `dryl.popover.close` deletes a
request that was never reached. A consumer that parks nothing keeps its focus
where it was, which is exactly what `DrylSelect` and `DrylAutocomplete` want.
The channel is private to `dryl.js` and is not public API.

**The key policy on the panel node is the consumers', installed on this
component's element.** `drylPanelKeys.install` (private in `dryl.js`, used by
`dryl.datepicker` and `dryl.timepicker`) binds one `keydown` listener to the
panel and marks it `__drylPanelKeys`. It exists in JS for a reason that cannot
be worked around in .NET: `KeyboardEventArgs` carries no target, so a handler
bound to the panel can never tell which descendant a key came from. It cycles
`Tab` inside the panel — necessary precisely because the panel is portalled to
the end of `<body>`, where the next tab stop is the far end of the page — lets
`Enter` and `Space` through to a control that activates itself, and suppresses
browser defaults only for the keys it actually consumes. `DrylPopover` neither
installs nor removes it, and this file describes it rather than promising it,
for the reason `F3 DrylSplitButton` sets out at length: a restatement of a
dependency's behaviour goes stale in both directions, when the dependency breaks
and again when it is repaired. What the criteria below promise is this
component's own half — that the panel is a focusable, key-receiving element with
a stable identity for such a listener to live on.

**`Escape` is only half this component's, and the half it owns is conditional.**
`CloseOnEscape` defaults to `true`, and the handler that reads it is bound to the
panel. Focus is therefore the precondition: measured on `/components/popover`
with a plain trigger and no consumer focus management, `Escape` pressed while
focus sits on the trigger does **not** close the panel, and the same `Escape`
pressed with focus on a button inside the panel closes it at once. Every library
consumer that implements `Escape` itself passes `CloseOnEscape="false"` —
`DrylMenu`, `DrylSelect`, `DrylMultiSelect`, `DrylAutocomplete` and both pickers
— because closing is not the whole job: focus has to be returned to the trigger
or the input, and this component returns it nowhere. Measured: after an `Escape`
that closed the panel, `document.activeElement` is `<body>`. Whoever sets
`CloseOnEscape="false"` takes on both duties, and a consumer that takes the key
without ensuring focus reaches the element its handler is bound to gets a panel
that cannot be closed by keyboard at all — which is exactly what happened to
both pickers and to `DrylMenu`, and is recorded in
[`../../docs/2026-08-13-popover-portal-focus-plan.md`](../../docs/2026-08-13-popover-portal-focus-plan.md)
and
[`../../docs/2026-08-13-picker-escape-focus-plan.md`](../../docs/2026-08-13-picker-escape-focus-plan.md).

**The trigger's ARIA is claimed from JS, additively, per attribute.** A trigger
that opens a panel should announce it (`aria-haspopup`) and report its state
(`aria-expanded`). Components that build their own trigger markup write both
themselves; the ones composed from a plain button cannot, because the open state
lives in the wrapping component and the trigger fragment renders in the
consumer's context. So `dryl.popover.claimTrigger` writes them — only where the
attribute is absent, marking each claim separately on the node
(`__drylTriggerHasPopup`, `__drylTriggerExpanded`), and only ever rewriting what
it claimed. The docs site's own theme switcher is the case that shows why the
two claims are separate: measured on `/components/menu`, it keeps its own
`aria-haspopup="dialog"` untouched while its `aria-expanded` is claimed and
driven by the popover.

**Four doc comments around this component mislead, and one of them is simply
false.** They are named here rather than quoted as evidence anywhere below,
because a reader of this spec will otherwise meet them in the source and believe
them. The distinction between the two kinds is kept, since it decides what to do
with each: a false comment is corrected, a misleading one is usually a decision
that was written down as a virtue.

**False.** The `dryl.popover` module comment says `open()` "drops a comment
placeholder at the panel's original slot". It does not: `open` is
`document.body.appendChild(panel)`, `close` is `anchor.appendChild(s.panel)`
with the panel restored as the anchor's last child, and nothing in the module
creates a placeholder node of any kind.

**Misleading but true.**

- `DrylPopover.razor.css` says that dropping `.is-open` hides the panel
  "atomically with the content removal, so no empty surface box ever flashes".
  That is accurate — both come out of the same render, and per-frame sampling
  never catches an empty surface — but it presents the `DESIGN-12` violation
  recorded below as a virtue. The flash it prevents is real; the exit animation
  it prevents is the point.
- The same comment says the panel is "positioned with `position: fixed` by
  `dryl.popover` (JS)". The `position: fixed` declaration is the stylesheet's
  own, in the `.popover-panel` rule directly beneath the comment; what JS
  supplies is `top`, `left` and, under `MatchTriggerWidth`, `width`. The
  escape from an ancestor's containing block is therefore CSS's doing, not the
  portal's.
- `OnOpen` is documented as "Fires after the panel opens" and `OnClose` as
  "Fires after the panel closes". Both are invoked from `SetOpenAsync`, which
  runs **before** the render that opens or closes anything and long before
  `dryl.popover.open` has portalled, placed or revealed the panel. A consumer
  who reads "after" and focuses something in `OnOpen` walks straight into the
  hidden-panel no-op this whole component is shaped around. The `## Public API`
  table above says "when the open state becomes `true`" for that reason.

The false comment and the first misleading one are also recorded in
[`../../ideas/I4 An exit animation for the popover surface.md`](../../ideas/I4%20An%20exit%20animation%20for%20the%20popover%20surface.md).

## Public API

| Member | Type | Default | Purpose |
|---|---|---|---|
| `Open` | `bool` | `false` | Whether the panel is open. Supports `@bind-Open`. |
| `OpenChanged` | `EventCallback<bool>` | — | Fires when the open state changes. |
| `TriggerContent` | `RenderFragment?` | `null` | The clickable trigger. |
| `PanelContent` | `RenderFragment?` | `null` | The panel body; rendered only while `Open` is `true`. |
| `Placement` | `PopoverPlacement` | `PopoverPlacement.BottomStart` | Where the panel opens relative to the trigger. |
| `MatchTriggerWidth` | `bool` | `false` | Gives the panel the trigger's measured width. |
| `Block` | `bool` | `false` | Stretches the anchor to its container's full width. |
| `TriggerTogglesOpen` | `bool` | `true` | Whether a click on the trigger toggles the panel. |
| `CloseOnClickOutside` | `bool` | `true` | Dismiss on a pointer press outside the anchor and the panel. |
| `CloseOnEscape` | `bool` | `true` | Close on `Escape` **received by the panel**. |
| `Surface` | `bool` | `true` | Whether the panel paints the default glass surface. |
| `PanelRole` | `string?` | `null` | ARIA role written on the panel, and the popup type claimed on the trigger. |
| `PanelAriaLabel` | `string?` | `null` | Accessible name of the panel. |
| `OnKeyDown` | `EventCallback<KeyboardEventArgs>` | — | Raised for every keydown the panel receives. |
| `OnOpen` | `EventCallback` | — | Fires when the open state becomes `true`. |
| `OnClose` | `EventCallback` | — | Fires when the open state becomes `false`. |
| `Class` | `string?` | `null` | Extra CSS class(es) merged onto the anchor's own classes. |
| `AdditionalAttributes` | `IDictionary<string, object>?` | `null` | Pass-through attributes on the anchor element. |

Public members beyond the parameters:

| Member | Kind | Purpose |
|---|---|---|
| `PanelElement` | `ElementReference` | The panel node, so a consumer can drive panel-scoped JS of its own. |
| `AnchorElement` | `ElementReference` | The anchor node. |
| `SetOpenAsync(bool)` | `Task` | Opens or closes the panel programmatically, raising the same callbacks a trigger click would. |
| `Close()` | `[JSInvokable] Task` | Called from `dryl.js` on an outside press. Public because interop requires it, not because a consumer is meant to call it. |
| `DisposeAsync()` | `ValueTask` | `IAsyncDisposable`; tears the portal down. |

`PopoverPlacement` is a top-level enum in
`code/DRYL.Components/Components/Surfaces/PopoverPlacement.cs`, not nested in the
component, so a consumer writes `Placement="PopoverPlacement.TopEnd"`. Its
members are listed in [`_Api.md`](_Api.md).

The component takes **no** `Ai` and no `Aura` parameter and does not inherit
`DrylAiAware`; the reasoning is under **Cross-cutting evidence** below. There is
no parameter for the gap between trigger and panel, none for a close delay, and
no way to ask the panel to keep focus.

## Acceptance Criteria

### Structure

- The component renders exactly one `div` as its root, the anchor.
- The anchor carries the `popover-anchor` identity class.
- The anchor holds exactly two children: the trigger slot and the panel.
- The trigger slot is a `div` carrying the `popover-trigger` class.
- The component renders `TriggerContent` inside the trigger slot.
- The panel is a `div` carrying the `popover-panel` class.
- The panel element is rendered whether the popover is open or closed.
- The panel carries `tabindex="-1"`, so it can receive focus programmatically
  without becoming a tab stop.
- The component renders `PanelContent` exactly while `Open` is `true`.
- The panel element is empty while `Open` is `false`.
- The panel carries the `is-open` class exactly while `Open` is `true`.
- The panel carries the `popover-panel--surface` class exactly while `Surface`
  is `true`.
- The panel carries the `popover-panel--match` class exactly while
  `MatchTriggerWidth` is `true`.
- The panel carries exactly one placement modifier class, in every state.
- The anchor carries the block modifier class exactly while `Block` is `true`.

### Open state

- `Open` defaults to `false`.
- `Open` supports two-way binding through `OpenChanged`.
- A click on the trigger slot toggles `Open` while `TriggerTogglesOpen` is
  `true`.
- A click on the trigger slot changes nothing while `TriggerTogglesOpen` is
  `false`.
- `SetOpenAsync` opens or closes the panel from consumer code.
- `SetOpenAsync` returns without raising anything when the requested state is
  the current one.
- `OpenChanged` fires before `OnOpen` and before `OnClose`.
- `OnOpen` fires when the state becomes `true`.
- `OnClose` fires when the state becomes `false`.
- Neither `OnOpen` nor `OnClose` fires when `Open` is set to the value it
  already has.
- A popover given no binding for `Open` still opens and closes from its own
  trigger, because the component assigns its own parameter.
- The portal is driven from the rendered open state rather than from the path
  that changed it, so trigger toggle, `Escape`, an outside click and a consumer
  flipping the bound parameter all reach the same open and close code.

### The portal

- The panel node is moved to `<body>` while the popover is open.
- The panel node is returned to its anchor when the popover closes.
- The panel is positioned relative to the viewport while portalled, so no
  ancestor's `overflow`, `transform` or `backdrop-filter` can clip or re-anchor
  it.
- The panel is painted at the `--z-popover` layer.
- The panel becomes visible only once it has been positioned, so no
  mis-positioned frame is ever shown.
- The panel is repositioned when any ancestor scrolls.
- The panel is repositioned when the window is resized.
- Scrolling does not close the panel.
- The panel keeps its distance from the trigger across a reposition.
- The inline placement styles the portal applied are cleared when the popover
  closes.
- A popover disposed while open tears its portal down, so neither the node nor
  its listeners outlive the component.
- Tearing the portal down survives a closing circuit, a missing element and a
  statically rendered component without throwing.

### Placement

- `Placement` defaults to `PopoverPlacement.BottomStart`.
- `Placement` accepts exactly the four values of `PopoverPlacement`.
- A `bottom` placement puts the panel below the trigger.
- A `top` placement puts the panel above the trigger.
- A `start` placement aligns the panel's leading edge with the trigger's.
- An `end` placement aligns the panel's trailing edge with the trigger's.
- The panel is separated from the trigger by a fixed gap.
- A panel that would overflow the viewport on its preferred side flips to the
  opposite side, but only when the opposite side has room for it.
- A panel that would overflow horizontally is clamped inside the viewport rather
  than flipped.
- The panel never exceeds the viewport's width, so a phone-width screen shows it
  whole.
- The placement modifier class on the panel is a styling hook only: the
  position itself is computed against the viewport, so the class alone moves
  nothing.

### Width

- `MatchTriggerWidth` defaults to `false`.
- The panel is given the trigger's measured width while `MatchTriggerWidth` is
  `true`.
- The surface's minimum width is dropped while `MatchTriggerWidth` is `true`, so
  the panel can shrink to a narrow trigger.
- The panel keeps its own content width while `MatchTriggerWidth` is `false`.
- `Block` defaults to `false`.
- The anchor is inline while `Block` is `false`.
- The anchor fills its container's width while `Block` is `true`.
- The trigger slot passes the block width through to whatever the consumer put
  inside it.
- `Block` and `MatchTriggerWidth` are independent: either can be set without the
  other.

### Dismissal

- `CloseOnClickOutside` defaults to `true`.
- A pointer press outside both the anchor and the portalled panel closes the
  popover while `CloseOnClickOutside` is `true`.
- A pointer press inside the portalled panel does not close the popover, even
  though the panel is no longer a descendant of the anchor.
- No outside-press listener is registered while `CloseOnClickOutside` is
  `false`.
- The outside-press listener is removed when the popover closes.
- `CloseOnEscape` defaults to `true`.
- `Escape` closes the popover while `CloseOnEscape` is `true` **and** the key
  reaches the panel — that is, while focus is inside the panel.
- `Escape` does nothing while `CloseOnEscape` is `false`, leaving the key
  entirely to the consumer.
- The component moves focus nowhere when the panel closes, by any path.

### Keyboard and accessibility

- The trigger's own keyboard behaviour is whatever the consumer put in the
  trigger slot: the component adds no key handling to it and no `tabindex` of
  its own.
- `OnKeyDown` is raised for every keydown the panel receives.
- `OnKeyDown` is raised before the component's own `Escape` handling, so a
  consumer sees the key first.
- The panel is focusable programmatically and is not a tab stop, so nothing
  about it changes the page's tab order until something focuses into it.
- `PanelRole` is rendered as the panel's `role`.
- The panel carries no role when `PanelRole` is `null`.
- `PanelAriaLabel` is rendered as the panel's `aria-label`.
- The panel is unnamed when `PanelAriaLabel` is `null`.
- The component writes no ARIA attribute of its own on the trigger or the
  anchor: the two the trigger carries are written from JS, on the terms below.

### The trigger's ARIA claim

- The trigger is claimed at the popover's first render, not only on open, so a
  user is told the control opens something before they open it.
- Nothing is claimed when `PanelRole` is `null`.
- Nothing is claimed when `PanelRole` is not one of the roles `aria-haspopup`
  defines, so no untrue popup type is ever announced.
- The claim targets an element inside the trigger slot that already carries
  `aria-haspopup`, if there is one.
- The claim otherwise targets the shallowest button, link or tab stop in the
  trigger slot, so a control nested deeper inside the trigger is treated as part
  of it rather than as the trigger.
- An element carrying `tabindex="-1"` is excluded from that choice, being the
  marker a decorative, programmatically focused node carries.
- A **disabled** control is included in that choice, because `aria-haspopup`
  describes what the control is, not whether it can be used right now.
- `aria-haspopup` is written only where it is absent, and its value is
  `PanelRole`.
- `aria-expanded` is claimed only where it is absent.
- The two attributes are claimed independently, so a trigger that writes one of
  them itself keeps that one and receives the other.
- Only a claimed `aria-expanded` is ever written again.
- A claimed `aria-expanded` reads `true` while the panel is open.
- A claimed `aria-expanded` reads `false` when the panel closes.
- `aria-haspopup` is left standing when the panel closes, because the trigger
  still opens a panel while closed.
- Opening re-runs the claim, so a trigger rebuilt between two opens is claimed
  again.

### Class merging and attribute precedence

- The anchor always carries the `popover-anchor` identity class.
- `Class` is merged onto the anchor's own classes and never replaces them.
- A consumer-supplied `class` attribute is merged the same way, because Blazor
  binds it to the `Class` parameter.
- `AdditionalAttributes` is splatted onto the anchor only, and reaches neither
  the trigger slot nor the panel.
- Neither `Class` nor `AdditionalAttributes` reaches `TriggerContent` or
  `PanelContent`.

### Appearance and motion

- The panel's fill is `--panel-float` and its frost is `--glass-fx-float`, which
  is the library's floating-surface pair (`DESIGN-06`).
- The panel's border colour is `--line-strong`.
- The panel's corner radius is `--r-md`.
- The panel's shadow is `--shadow-lg`.
- The panel's padding is `--sp-2`.
- The panel paints none of the above while `Surface` is `false`, so a consumer
  can supply its own surface — which is what every input dropdown in the library
  does.
- The component branches on no colour mode and writes no mode-assuming value:
  both modes come from the token set alone (`DESIGN-02`).
- The panel animates in with `popover-in`, over `--dur-fast` with `--ease-out`.
- The entrance animation is bound to the reveal state rather than to the node
  mounting, so it replays on every open although the node is reused.
- The entrance animation is suppressed under `prefers-reduced-motion: reduce`.
- The panel has **no** exit animation: it is removed within a single frame of
  the close. See **Deviations**.

## Cross-cutting evidence (`SPEC-05`)

- **Both color modes** — the panel's whole surface is tokens: `--panel-float`,
  `--glass-fx-float`, `--line-strong`, `--r-md`, `--shadow-lg`, `--sp-2`,
  `--z-popover`. `DrylPopover.razor.css` contains no `data-dryl-mode` selector
  and no colour literal, so there is no mode branch to check. Measured on the
  open panel on `/components/popover` by flipping `data-dryl-mode` on `<html>`:
  the fill and the shadow swap to the light set and the border resolves to that
  mode's `--line-strong`, with the frost unchanged in both — one identity, two
  palettes.
- **Enter/exit animation** — enter: **yes**, `popover-in` bound to
  `.is-open.is-positioned` so it replays on every open, with
  `prefers-reduced-motion` honoured. Exit: **no**, and this is recorded as a
  `DESIGN-12` deviation below rather than as an exception. It is not the
  "nothing to animate" case `DESIGN-11` allows; there is a surface, it is
  visible, and it vanishes in one frame.
- **Keyboard and a11y** — the "Keyboard and accessibility" and "The trigger's
  ARIA claim" criteria above: `PanelRole` and `PanelAriaLabel` on the panel, the
  additive per-attribute claim of `aria-haspopup` and `aria-expanded` on the
  trigger from the first render onwards, `OnKeyDown` handed to the consumer
  before the component's own `Escape`, and a panel that is focusable without
  being a tab stop. And the two things this component does **not** do, both
  measured and both carried as deviations below: it moves focus nowhere on open
  or close, and its portalled panel sits at the end of `<body>`, so `Tab` from
  the trigger leaves the popover behind. A bare `DrylPopover` is therefore
  operable by keyboard only in as much as its consumer makes it so — which every
  library consumer does, in its own module, and which a direct consumer of this
  primitive has to do for themselves (`UX-01`).
- **AI mode** — **no**, deliberately, and the decision is the one `AI-05` asks
  to be written down. `DrylPopover` declares no `Ai` parameter and does not
  inherit `DrylAiAware`. It has no action of its own for a model to be working
  on: it is a container that paints a surface around content it does not own,
  and the state a user needs to feel belongs to the control that opens it. That
  is where the library puts it — `DrylSelect`, `DrylMultiSelect`,
  `DrylAutocomplete` and both pickers each render `DrylAuraElements` in their
  *trigger*, inside this component's trigger slot, and light the field rather
  than the panel. An aura on the panel would double that signal and would
  outlive nothing, since the panel is only on screen while it is open. The
  `ComponentCatalog` row agrees: `Popover` is registered with its AI flag
  `false`.
- **Demo page** — its own page at `/components/popover`, built from
  `DRYL.Website/Components/Pages/DemoPopover.razor` with three examples under
  `DRYL.Website/Components/Examples/Popover/`: `Basic`, `Placements` (all four
  `PopoverPlacement` values) and `MatchWidth` (`Block` and `MatchTriggerWidth`
  together). Verified in the running site. What the page does **not**
  demonstrate is most of the API: `Surface="false"`, `TriggerTogglesOpen`,
  `CloseOnClickOutside`, `CloseOnEscape`, `PanelRole`, `PanelAriaLabel`,
  `OnKeyDown`, `OnOpen`, `OnClose` and `SetOpenAsync` have no example, so the
  criteria about them rest on the code and on the measurements recorded here
  rather than on a rendered example. The page lives in `DRYL.Website`, a
  different repository; no acceptance criterion above is about it.
- **`ComponentCatalog`** — registered in
  `DRYL.Website/Components/ComponentCatalog.cs` as `"Popover"` / `popover`,
  category `Surfaces`, `ClassName` `"DrylPopover"`, AI flag `false`. Checked in
  the file, so the component reaches the sidebar, the Ctrl+K search and the
  `/components` overview under its own name (`REL-04`).

## Deviations (`State: Implemented`)

`State` records whether spec and code agree, and they do: every criterion above
was read off this code or measured in the running application today. What
follows is the component's debt against the harness rules and against what a
consumer would reasonably expect — written into the criteria as behaviour rather
than hidden, and listed here so it is not mistaken for a finished job.

- **No exit animation (`DESIGN-12`).** The panel body sits behind a bare
  `@if (Open)` with no `DrylPresence`, and the visibility gate drops in the same
  render. Measured on `/components/popover`, sampling computed style per frame
  around an outside-click close: `.is-open` and the panel content go together in
  one frame, `.is-positioned` and the return to the anchor follow in the next,
  and the panel's opacity never leaves `1` — no `*-out` animation runs at any
  point. The obvious repair does not work and was built and measured rather than
  argued: `DrylPresence` around the panel *content* animates inside the surface,
  leaving a fully opaque, empty glass box standing for roughly 115 ms before it
  jumps away — worse than the jump. It also puts a generic element between a
  container role and its owned elements, which breaks `menu`/`menuitem` and
  `listbox`/`option`, and it breaks an existing test, because library code
  relies on the content being gone the moment `Open` is `false`. Only an exit on
  the **surface itself** can look right, and both routes to that are a new
  animation needing maintainer sign-off (`DESIGN-13`, `CLAUDE.md` stage 1). The
  decision is open and is held in
  [`../../ideas/I4 An exit animation for the popover surface.md`](../../ideas/I4%20An%20exit%20animation%20for%20the%20popover%20surface.md).
- **`Escape` is inert for a popover nobody focused into.** `CloseOnEscape`
  defaults to `true` and reads as a promise, but the handler is on the panel.
  Measured: focus on the trigger, `Escape`, panel still open. A direct consumer
  of this primitive who does not move focus into the panel has no keyboard
  dismissal at all, and nothing in the API tells them so — the parameter's own
  doc comment says "when Escape is pressed inside it", which is accurate and
  easy to read past.
- **The portalled panel drops out of the tab order.** Measured: with the panel
  open, `Tab` from the trigger moves to the next control on the page and the
  panel stays open behind it. The panel is the last child of `<body>` while
  portalled, so its content sits at the very end of the tab order. Every library
  consumer covers this — the pickers by cycling `Tab` inside the panel, the menu
  by moving focus in on open — but the primitive alone does not, and `UX-01` is
  satisfied only by what a consumer adds.
- **Focus is returned nowhere on close.** Measured: after an `Escape` that
  closed the panel from inside it, `document.activeElement` is `<body>`. Focus
  restoration lives in each consumer (`dryl.menu.focusTrigger`,
  `dryl.timepicker.restoreFocus`), which is defensible — this component does not
  know which element deserves it — but it means the pairing "the consumer takes
  `Escape`, so the consumer owes the focus return" is a convention, not a
  mechanism, and both pickers once got it wrong.
- **The panel key listener is never detached.** `drylPanelKeys.install` adds a
  `keydown` listener to this component's panel node and marks it
  `__drylPanelKeys`; there is no `detach` counterpart anywhere in `dryl.js`. The
  argument is that it captures nothing but the node it lives on and dies with
  it — which holds, since the node is Blazor's and is discarded with the
  component — but it is the library's first listener with no teardown path, and
  `CODE-05`'s habit of pairing every handle with its release is worth keeping
  rather than eroding.
- **Two literals in `dryl.popover` duplicate a token.** The module's `GAP` and
  `EDGE` constants are both `4`, and `GAP`'s comment says it "matches `--sp-1`".
  It does today. Nothing keeps it matching: the value is a copy of a token in a
  file no token check reads (`DESIGN-01`'s check greps `*.razor.css`), so a
  change to `--sp-1` moves every spacing in the library except the gap between a
  trigger and its panel.
- **`.popover-panel--surface` carries a raw `min-width`** and a raw `1px`
  border width in `DrylPopover.razor.css`. Neither is in `DESIGN-01`'s
  enumeration of colour, padding, radius, shadow, duration and easing, and no
  token expresses either, so this is named rather than counted as a violation —
  in the same spirit as `F3 DrylSplitButton`'s zero radii.
- **No tests of its own.** `tests/DRYL.Components.Tests/` holds no
  `DrylPopoverTests`. The component is touched by exactly one test that names
  it — the anchor's class merge in `ClassMergeTests` — and otherwise only
  through `DrylSplitButtonTests`, which asserts the arguments reaching
  `dryl.popover.claimTrigger` and `dryl.popover.open` from a composed menu.
  Everything in this file about the portal, placement, dismissal, focus and ARIA
  rests on reading the code and on measurement in a browser, because bUnit
  executes no `dryl.js` and manages no real focus. A test suite that claimed
  otherwise would be lying, which is why there is none — but it does mean the
  behaviour has no regression net.

### Recorded gaps — boundaries of the mechanism, not observed defects

- **A trigger node replaced under a live popover keeps no ARIA until the next
  open.** The claim runs at first render and again on open; a node swapped in
  between carries neither attribute. No library component produces this today,
  and closing it would cost an interop call per popover per render.
- **A popover nested inside another popover's trigger takes its host's ARIA.**
  The target rule prefers an element that already carries `aria-haspopup`, and
  Blazor renders children before parents, so the inner popover claims its
  trigger first and the outer one then writes its own open state onto that same
  node. The precedence rule is still right — it is what recognises
  `DrylSelect`'s and `DrylMultiSelect`'s own trigger containers — and nothing in
  the library nests a popover inside a popover's trigger.
- **A popover given no `PanelRole` announces nothing on its trigger.**
  Deliberate: `aria-haspopup` takes one of a fixed set of popup types, and a
  panel with no role gives no true value to write. Measured on
  `/components/popover`, where none of the demo triggers carries either
  attribute — which is correct for the demo and worth knowing for a consumer who
  expects the primitive to announce itself for free.
- **`dryl.timepicker.scrollToActive` is a no-op against the hidden panel**, and
  the cause is in this component: the call runs from the picker's
  `OnAfterRenderAsync`, before the popover has portalled or revealed anything,
  and `scrollIntoView` on an invisible node does nothing — after which
  `document.body.appendChild` resets the scroll offset of the columns anyway.
  The defect and its fix belong to `DrylTimePicker` and to `E8 Inputs`, which is
  still a scaffold; it is named here because the mechanism is the popover's.
- **`F3 DrylSplitButton` currently carries the written account of this
  component's focus, `Escape` and ARIA behaviour**, because `E11` had no spec
  when it was written. This file is now that account. The nested-popover and
  replaced-trigger boundaries above are stated there as well; they are this
  component's mechanism and belong here, and `F3`'s copies should become
  references in a commit of their own.
