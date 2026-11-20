# DrylTabs

## Meta
- **State:** Implemented
- **Source:** code/DRYL.Components/Components/Navigation/DrylTabs.razor

## User Story

As a Blazor developer splitting a page into views, I want a horizontal tab strip
that I fill with `DrylTab` declarations and bind to one identifier, so that the
user switches views with a gliding underline and a sliding panel, and I never
write the `tablist`/`tab`/`tabpanel` wiring myself.

## Description

`DrylTabs` is the parent of the tab pair. Its `DrylTab` children render no
markup of their own (see [`F9 DrylTab.md`](F9%20DrylTab.md)): each registers
itself against the parent it receives as a fixed cascading value, and the
parent draws the whole widget — a `div.tabs-root`, inside it one
`role="tablist"` strip of `button role="tab"` headers, and below it one
`role="tabpanel"` that holds only the active tab's `ChildContent`.

**Selection is an identifier, not an index.** `Active` names a tab by its
identifier — the tab's `Id`, or a generated one when the tab has none. Without
an `Active`, or with one that names no enabled tab, the first enabled tab is
shown. That default is not reported back through `ActiveChanged`; only a user
switch, or the removal of the active tab, raises it.

**The underline glides.** With `AnimateIndicator` on (the default) the strip
carries one shared `.tab-ink` element. After every render that changes the
active tab, the component calls `dryl.motion.moveIndicator` on the strip; the
script measures the header marked `data-dryl-ink-active="true"` and moves the
ink to it, and CSS lets it glide on `--dur-med` / `--ease-spring`. The
transition is only enabled (`.is-ink-ready`) after the first placement, so the
ink does not slide in from the left edge on load. With `AnimateIndicator` off,
each active header draws its own underline (`.tab.is-active::after`), which
scales in on `tab-underline-in`.

**The panel slides in the direction of travel.** The panel is keyed by the
active tab, so a switch mounts a new panel and replays its entry animation:
moving to a tab further right slides it in from the right
(`.tab-panel--fwd`), moving left slides it in from the left
(`.tab-panel--back`), and the first panel fades up (`tab-panel-in`). The old
panel leaves without an exit animation — see **Recorded debt**.

**Keyboard.** The headers use a roving `tabindex`: the active header is the one
`Tab` stop of the strip. On a header, `ArrowRight` / `ArrowLeft` activate the
next / previous enabled tab and wrap at the ends, `Home` / `End` activate the
first / last enabled tab, and `Enter` / `Space` activate the header the key was
pressed on. Activation follows the key immediately (automatic activation). The
component moves no DOM focus — see **Recorded debt**.

## Public API

Namespace: `DRYL.Components`. Implements `IAsyncDisposable`.

| Member | Type | Default | Purpose |
|---|---|---|---|
| `Active` | `string?` | `null` | Identifier of the active tab. Two-way bindable through `@bind-Active`. |
| `ActiveChanged` | `EventCallback<string?>` | — | Raised with the new identifier when the user switches tabs, and when the active tab is removed. |
| `AriaLabel` | `string?` | `null` | Accessible name of the `tablist`. |
| `AnimateIndicator` | `bool` | `true` | `true`: one shared underline glides between headers. `false`: each active header draws its own underline in place. |
| `ChildContent` | `RenderFragment?` | `null` | The `DrylTab` declarations. |
| `Class` | `string?` | `null` | Merged onto the root's `tabs-root` class. |
| `AdditionalAttributes` | `IDictionary<string, object>?` | `null` | Captures unmatched attributes; splatted onto the root. |

There is no public tab collection, no navigation method and no orientation
parameter; the strip is horizontal only.

## Acceptance Criteria

### Structure

- The component renders one root `div` carrying `tabs-root`.
- `Class` is appended to the root's classes; `tabs-root` stays.
- `AdditionalAttributes` are applied to the root.
- The root holds one strip element carrying `.tabs` and `role="tablist"`.
- `AriaLabel` is rendered as the strip's `aria-label`.
- Every registered, rendered `DrylTab` produces one `button` header in the
  strip, in registration order.
- Each header is of `type="button"`, so a tab strip inside a form submits
  nothing.
- Each header carries `role="tab"` and the id `dryl-tab-{identifier}`.
- Each header's `aria-controls` names `dryl-tabpanel-{identifier}`.
- Exactly the active header carries `aria-selected="true"`; every other header
  carries `aria-selected="false"`.
- Exactly the active header carries `.is-active`.
- One panel is rendered below the strip while at least one enabled tab exists;
  none is rendered otherwise.
- The panel carries `.tab-panel`, `role="tabpanel"`, the id
  `dryl-tabpanel-{identifier}` of the active tab and an `aria-labelledby`
  naming the active header.
- The panel renders the active tab's `ChildContent` and no other tab's.
- `ChildContent` is rendered inside a fixed `CascadingValue` of the component
  itself, so `DrylTab` declarations register without consumer wiring.

### Selection

- With `Active` `null`, the first registered enabled tab is active.
- With `Active` naming an enabled tab's identifier, that tab is active.
- With `Active` naming a disabled or unknown tab, the first enabled tab is
  active.
- Selecting the default tab raises no `ActiveChanged`.
- Clicking an enabled, inactive header makes its tab active.
- Clicking a header raises `ActiveChanged` with that tab's identifier.
- Clicking the already active header raises no `ActiveChanged`.
- A disabled tab can never become active by click or key.
- Removing the active tab makes the first remaining enabled tab active.
- Removing the active tab raises `ActiveChanged` with the new identifier, or
  with `null` when no enabled tab remains.

### Keyboard

- Only the active header has `tabindex="0"`; every other header has
  `tabindex="-1"`.
- `ArrowRight` on a header activates the next enabled tab.
- `ArrowRight` on the last enabled tab activates the first enabled tab.
- `ArrowLeft` on a header activates the previous enabled tab.
- `ArrowLeft` on the first enabled tab activates the last enabled tab.
- `Home` on a header activates the first enabled tab.
- `End` on a header activates the last enabled tab.
- `Enter` on a header activates that header's tab.
- `Space` on a header activates that header's tab.
- Arrow navigation skips disabled tabs.
- Any other key does nothing.

### Indicator

- `AnimateIndicator` defaults to `true`.
- While `AnimateIndicator` is `true`, the strip carries `.tabs--glide` and holds
  exactly one `.tab-ink` element (`tests/DRYL.Components.Tests/DrylTabsGlideTests.cs`).
- The `.tab-ink` element carries `aria-hidden="true"` and `data-dryl-ink`.
- Exactly the active header carries `data-dryl-ink-active="true"`.
- After the first render, and after every render that changes the active tab,
  the component calls `dryl.motion.moveIndicator` with the strip.
- The ink takes the active header's width and horizontal offset.
- The ink is drawn with `--accent-grad` and glows with `--accent-a`.
- The ink glides its position and width on `--dur-med` / `--ease-spring` and
  fades on `--dur-fast` / `--ease-out`.
- The ink does not glide into its first position.
- While `.tabs--glide` is set, an active header draws no underline of its own.
- While `AnimateIndicator` is `false`, the strip carries neither `.tabs--glide`
  nor a `.tab-ink`, and no `dryl.motion.moveIndicator` call is made.
- While `AnimateIndicator` is `false`, the active header draws its own
  `--accent-grad` underline, which scales in on `--dur-med` / `--ease-spring`.

### Panel motion

- The panel is keyed by the active tab, so every switch mounts a new panel.
- The first panel enters on `tab-panel-in`, `--dur-med` / `--ease-out`.
- After a switch to a tab further along the strip, the panel carries
  `.tab-panel--fwd` and enters on `presence-in-left`.
- After a switch to a tab earlier in the strip, the panel carries
  `.tab-panel--back` and enters on `presence-in-right`.

### Appearance

- The strip's bottom border is `--line`.
- The strip scrolls sideways when the headers overflow it, with no visible
  scrollbar.
- The strip never shrinks to make room for a tall panel inside a
  height-constrained root; the panel is the part that shrinks.
- An inactive header's text is `--fg-muted`; hovering it turns the text `--fg`
  over a `--glass-1` fill, on `--dur-fast` / `--ease-out`.
- The active header's text is `--fg`.
- A header's keyboard focus shows a `--accent-soft` ring (`:focus-visible`).
- A disabled header reads `--fg-faint`, shows a `not-allowed` cursor and takes
  no pointer events.
- The panel's top padding is `--sp-5`.
- The component branches on no colour mode and writes no mode-assuming value;
  both modes come from the token set (`DESIGN-02`).

### Reduced motion

- Under `prefers-reduced-motion: reduce`, the ink moves without a transition.
- Under `prefers-reduced-motion: reduce`, the per-header underline and the
  panel have no animation.

### Disposal

- `DisposeAsync` clears the registered tabs.
- When the indicator was attached, `DisposeAsync` calls
  `dryl.motion.disposeIndicator` with the strip.
- A disconnected circuit or a prerender during disposal throws nothing.

## Recorded debt

- **Arrow keys do not move focus (`UX-01`, WAI-ARIA tabs pattern).** The
  component calls no `focus()` and no script does it for it. Read from the code:
  an arrow key activates the neighbouring tab and moves the roving
  `tabindex="0"` to it, but DOM focus stays on the header the key was pressed
  on, which now carries `tabindex="-1"`. The next arrow press is handled for
  that same, still-focused header and targets the tab that is already active,
  so it does nothing — arrow navigation advances one step per focus.
  *To be confirmed in a browser.*
- **`aria-controls` points at panels that do not exist.** Only the active
  tab's panel is rendered, so every inactive header's `aria-controls` names an
  id that is not in the document.
- **The panel is not focusable.** It carries no `tabindex="0"`; a panel
  without focusable content cannot be reached with `Tab` as the WAI-ARIA tabs
  pattern recommends.
- **Disabled tabs leave the focus order.** A disabled header carries the native
  `disabled` attribute rather than `aria-disabled`, so it is neither focusable
  nor discoverable by keyboard.
- **No exit animation for the old panel (`DESIGN-11`, `DESIGN-12`).** A switch
  replaces the keyed panel; the old content disappears at once while the new
  one slides in.
- **DOM ids are per identifier, not per instance.** The header and panel ids
  are built from the tab identifier only, so two `DrylTabs` on one page whose
  tabs share an `Id` (e.g. `"overview"`) render duplicate ids, and their
  `aria-controls` / `aria-labelledby` cross-wire.
- **Order follows registration, not markup.** A `DrylTab` that mounts later —
  one inside an `@if` that turns true after the first render — is appended to
  the end of the strip, wherever it stands in the markup.
- **The default selection is not reported.** A consumer bound through
  `@bind-Active` reads `null` until the user switches, although a tab is shown.
- **The component writes its own `Active` parameter** inside its activation
  path, rather than leaving the value to the binding.
- **Literal geometry and type.** `.tabs` `gap: 4px`; `.tab` `gap: 8px`,
  `height: 40px`, `padding: 0 14px`, `font-size: 13px`, `letter-spacing`, the
  2 px underline and ink heights, the 8 px underline insets, the 12 px glow
  blur on `.tab-ink` and `.tab.is-active::after`, and the 2 px focus ring are
  literals in `dryl.css` (`DESIGN-01`).

## Cross-cutting evidence (`SPEC-05`)

- **Both color modes** — EYE-CHECK-PENDING on /components/tabs
- **Enter/exit animation** — the component itself does not mount
  conditionally. Its moving parts are animated: the ink glides between headers,
  the fallback underline scales in, and each new panel enters directionally.
  The panel's missing exit is recorded above.
- **Keyboard and a11y** — `tablist` / `tab` / `tabpanel` roles,
  `aria-selected`, `aria-controls`, `aria-labelledby`, roving `tabindex`, arrow /
  `Home` / `End` / `Enter` / `Space` handling, the ink `aria-hidden` (`UX-07`).
  The focus-follow gap, the dangling `aria-controls` and the unfocusable panel
  are recorded above.
- **AI mode** — **yes, per tab, not on the strip.** `DrylTabs` takes no `Ai`
  parameter; each `DrylTab` carries `Ai` of type `AiState`, and `DrylTabs`
  renders it on that tab's header (see [`F9 DrylTab.md`](F9%20DrylTab.md)). A
  strip as a whole is navigation, not a surface an AI works on; a single view
  behind one tab can be.
- **Demo page** — `/components/tabs`, examples `Tabs/Default`, `Tabs/Icons`,
  `Tabs/Trailing`, `Tabs/Disabled`, `Tabs/TwoWayBinding` and `Tabs/AiSingle`
  under `DRYL.Website/Components/Examples/Tabs/`. No example sets
  `AnimateIndicator="false"`.
- **`ComponentCatalog`** — `"Tabs"` / `tabs` in
  `DRYL.Website/Components/ComponentCatalog.cs`, group `Layout`, `ClassName`
  `"DrylTabs"`, folder `Navigation`, AI flag `true` (`REL-04`).
- **Tests** — `tests/DRYL.Components.Tests/DrylTabsGlideTests.cs` (ink present by
  default, active header marked, fallback without ink) and
  `tests/DRYL.Components.Tests/AiProposedTests.cs`
  (`A_tab_shows_the_proposal_and_names_it_for_assistive_technology`). Selection,
  keyboard and ARIA wiring have no test.
