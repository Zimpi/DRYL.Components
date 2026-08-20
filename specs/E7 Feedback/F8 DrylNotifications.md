# DrylNotifications

## Meta
- **State:** Implemented
- **Source:** code/DRYL.Components/Components/Feedback/DrylNotifications.razor
              code/DRYL.Components/Components/Feedback/DrylNotifications.razor.css
              code/DRYL.Components/Notifications/DrylNotification.cs
              code/DRYL.Components/Notifications/IDrylNotificationService.cs
              code/DRYL.Components/Notifications/DrylNotificationService.cs

## User Story

As a Blazor developer building an app shell, I want a bell in my header that
shows how many unread notifications there are and opens an inbox when clicked,
fed either by a service I can push to from a background job or by a list I hold
myself, so that an agent finishing a task twenty minutes from now can tell the
user without me building an inbox.

## Description

`DrylNotifications` is two things in one component: a bell trigger with an
unread badge, and a popover panel listing the entries. The panel is a
[`DrylPopover`](../E11%20Surfaces/F1%20DrylPopover.md) — the portal, the
placement, the outside click and `Escape` are that component's, not this one's.

It has **two modes**, and which one is active is decided by a single question:
was `Items` supplied?

- **Service-driven** (`Items` is `null`) — the component resolves
  `IDrylNotificationService`, subscribes to its change event, renders what the
  service holds and mutates the service directly. A background job, an agent
  completion or a SignalR message pushes an entry and the bell updates live.
- **Controlled** (`Items` is set) — the component renders that list and raises
  a callback for every action instead of mutating anything. The service is not
  resolved at all.

The service is resolved leniently rather than required, so a consumer who never
called `AddDrylComponents()` gets an empty inbox rather than an exception at
render time.

Each entry carries its own `AiState`, which is what makes the inbox AI-native:
"your report was generated" arrives with the aura the report was written under.
The state belongs to the entry and is fixed for its lifetime — entries are added
and removed, never transitioned — which is why the component takes `Aura` but
no `Ai` of its own.

## Public API

| Member | Type | Default | Purpose |
|---|---|---|---|
| `Items` | `IReadOnlyList<DrylNotification>?` | `null` | Controlled list. Set it to leave service-driven mode. |
| `OnItemClick` | `EventCallback<DrylNotification>` | — | Raised when a row is activated, after it is marked read. |
| `OnMarkRead` | `EventCallback<DrylNotification>` | — | Controlled mode: a row should be marked read. |
| `OnMarkAllRead` | `EventCallback` | — | Controlled mode: "Mark all read" was pressed. |
| `OnRemove` | `EventCallback<DrylNotification>` | — | Controlled mode: a row was dismissed. |
| `OnClear` | `EventCallback` | — | Controlled mode: "Clear all" was pressed. |
| `Title` | `string` | `"Notifications"` | Panel heading. |
| `EmptyTitle` | `string` | `"All caught up"` | Empty-state heading. |
| `EmptyText` | `string` | `"No new notifications."` | Empty-state description. |
| `Placement` | `PopoverPlacement` | `PopoverPlacement.BottomEnd` | Where the panel opens relative to the bell. |
| `MaxBadgeCount` | `int` | `99` | Counts above this show as "N+". |
| `ShowClearAll` | `bool` | `true` | Shows the "Clear all" action. |
| `MaxHeight` | `string` | `"360px"` | Maximum height of the scrolling list. |
| `AriaLabel` | `string` | `"Notifications"` | Accessible label of the panel. |
| `Aura` | `AiAura?` | `null` | Aura variant for AI entries; `null` inherits a surrounding `DrylAiScope`. |
| `Class` | `string?` | `null` | Extra CSS class(es) merged onto the anchor's own classes. |
| `AdditionalAttributes` | `IDictionary<string, object>?` | `null` | Pass-through attributes on the anchor. |

`DrylNotification` and `IDrylNotificationService` are specified in
[`_Api.md`](_Api.md).

## Acceptance Criteria

### Mode selection

- `Items` left `null` puts the component in service-driven mode.
- `Items` set puts the component in controlled mode.
- Service-driven mode resolves `IDrylNotificationService` and renders its
  entries.
- Service-driven mode renders an empty inbox rather than throwing when no
  service is registered.
- Controlled mode does not resolve the service at all.
- Service-driven mode mutates the service and raises none of the four
  state-changing callbacks.
- Controlled mode raises the callbacks and mutates nothing at all, so the list a
  consumer passed in is never written to by the component.
- The mode is decided per render from `Items`, so a consumer cannot end up in
  both.

### The bell

- The bell renders a badge when at least one entry is unread.
- The bell renders no badge when every entry is read.
- The badge shows the unread count.
- The badge shows the count followed by a plus sign when the count exceeds
  `MaxBadgeCount`.
- The bell reflects the panel's open state with a modifier class.
- Activating the bell opens the panel, and activating it again closes it.

### The panel

- The panel renders `Title` as its heading.
- The panel renders the entries newest first.
- The panel renders a "Mark all read" action when at least one entry is unread.
- The panel renders no "Mark all read" action when every entry is read.
- The panel renders a "Clear all" action when `ShowClearAll` is `true` and at
  least one entry exists.
- The panel renders no "Clear all" action when `ShowClearAll` is `false`.
- The panel renders an empty state when there are no entries, using
  `EmptyTitle` and `EmptyText`.
- The panel renders the small size of the empty state, so an in-panel placeholder
  does not fill the popover.
- The list scrolls within `MaxHeight` rather than growing the panel past it.
- The panel opens at `Placement` relative to the bell.
- Each row is keyed by its notification's identity, so re-rendering the list
  does not reuse one row's state for another entry.

### A row

- A row renders its notification's title.
- A row renders its notification's message when the message is non-empty.
- A row renders no message element when the message is `null` or empty.
- A row renders a relative time derived from its notification's timestamp.
- A row's relative time falls back to an absolute date once the entry is more
  than a week old.
- A row's relative time is formatted with the invariant culture, so the month
  abbreviation does not change with the host's locale.
- A future timestamp renders as the present rather than as a negative age.
- A row renders its notification's icon when one is set.
- A row with no icon and no AI provenance renders the bell icon.
- A row with no icon and any AI provenance renders the sparkle icon.
- An unread row carries an unread modifier class and an unread dot.
- A read row carries neither.

### Actions

- Activating an unread row in service-driven mode marks it read in the service.
- Activating an unread row in controlled mode raises `OnMarkRead` with that row
  and leaves its read state to the consumer.
- Activating a row that is already read raises neither `OnMarkRead` nor a
  service call.
- Activating a row raises `OnItemClick` whether or not it was already read.
- Activating a row settles the read state before `OnItemClick` is raised, so a
  handler runs after the mode's own bookkeeping.
- Activating "Mark all read" marks every entry read.
- Activating a row's dismiss control removes that entry.
- Activating "Clear all" removes every entry.
- Every action works in both modes, differing only in whether it mutates the
  service or raises a callback.

### Keyboard and accessibility

- The bell carries an accessible label naming the unread count when there is
  one, so a screen-reader user hears the badge rather than only seeing it.
- The bell carries a plain accessible label when nothing is unread.
- The bell carries `aria-haspopup` and `aria-expanded`, and the expanded state
  tracks the panel.
- The badge is hidden from assistive technology, because its number is already
  in the bell's label.
- The panel is announced as a dialog labelled by `AriaLabel`.
- Every row is a native button, so it is reached by `Tab` and activated by
  `Enter` and `Space`.
- Each dismiss control carries an accessible label naming the entry it
  dismisses, so a screen-reader user is not offered a list of identical
  "Dismiss" buttons.
- An unread row carries the word "Unread" as visually-hidden text inside its own
  button, so the read state is part of the row's accessible name rather than
  being conveyed by color alone.
- The unread dot is decorative, so the state is announced once and not twice.
- Every control in the panel shows a visible focus indicator under
  `:focus-visible`.
- `Escape` and the outside click that close the panel are the popover's (`F1` in
  `E11 Surfaces`); this component adds no key handling of its own.

### Appearance

- Every color the component renders comes from a token; the component names no
  literal color (`DESIGN-01`).
- The bell paints `--glass-1` with a `--line` border and `--r-md`.
- The bell's border becomes `--accent-line` while the panel is open, so the
  trigger shows that it is the source of what is on screen (`DESIGN-08`).
- The badge paints `--accent-grad` and is ringed in `--bg-1`, so it stays
  readable over the bell's own border.
- The badge's count is set in `--font-mono`, so a rising number does not shift
  the badge's width digit by digit.
- An unread row paints `--accent-soft` and keeps it on hover, so hovering does
  not make an unread row look read.
- A read row paints nothing at rest and `--glass-1` on hover.
- The panel's own fill and frost are the popover's, not this component's.
- The component branches on no color mode and holds no mode-assuming value, so
  the same markup serves light and dark (`DESIGN-02`).

### Motion

- The bell transitions its border, its color and its background over
  `--dur-fast` with `--ease-out` on hover.
- A row transitions its background over `--dur-fast` with `--ease-out` on hover.
- A row's dismiss control is invisible at rest and fades in over `--dur-fast`
  when the row is hovered or the control itself is focused, so the list is calm
  until it is being worked on.
- The panel's own enter and exit animation is the popover's (`F1` in
  `E11 Surfaces`).

### AI mode

- A row whose notification's `Ai` is `AiState.None` renders no aura.
- A row whose notification's `Ai` is anything else renders the shared aura
  vocabulary — ring, comet, glow (`AI-02`).
- `AiState.Thinking`, `AiState.Streaming` and `AiState.Generated` each map to
  their own aura state class.
- `AiState.Active` renders the base aura with no state class, matching how the
  shared vocabulary treats the idle-but-engaged state.
- The aura variant follows `Aura` when set and a surrounding `DrylAiScope`
  otherwise.
- A row's aura ring and glow are drawn inside the row's rounded bounds, so the
  scrolling list's straight edges cannot clip a glow into a square around a
  rounded card.
- The component itself takes no `Ai`: the state belongs to each entry, and the
  inbox around them is not doing the work.

## Recorded gaps

- **AI rows have no aura lifecycle.** Every other AI-capable component in the
  category keeps the aura mounted for one `--dur-slow` beat after the state
  drops. Here the aura classes are computed straight from the entry's own state,
  which never transitions, so there is nothing to fade — but an entry whose `Ai`
  a consumer mutates in place snaps instead of dissolving.
- **No completion wash on a row.** The one-shot `AiState.Generated` wash is
  driven by a re-anchoring tick the shared helper supplies; the per-row aura
  markup is hand-written and carries no such tick, so a `Generated` row shows
  the aura state without the wash that announces it elsewhere.
- **The relative time never re-renders on its own.** "just now" stays "just now"
  until something else causes a render; there is no timer.
- **The relative time is computed from local now against the entry's own
  offset**, so a timestamp taken in another time zone is aged correctly but an
  entry created with a wrong offset silently reads wrong.
- **Six labels are fixed English** — "Mark all read", "Clear all
  notifications", "Unread", "Dismiss {title}", "Notifications, N unread" and the
  bell's plain label — with no parameters to change them, while `Title`,
  `EmptyTitle`, `EmptyText` and `AriaLabel` are overridable.
- **The panel's width, the bell's size and the type sizes are literals** in
  `code/DRYL.Components/Components/Feedback/DrylNotifications.razor.css`.
  `DESIGN-01` covers colors, radii, shadows, durations and easings, which are
  tokens here. Recorded as debt, not as compliance.
- **`MaxHeight` is a raw CSS string**, unvalidated, passed straight through to
  `DrylScrollArea`.
- **The list has no virtualisation.** Every entry the service holds is rendered,
  and the service never trims: a long-lived circuit that pushes on every agent
  completion grows the inbox without bound.
- **Most of its criteria are unguarded.** Tested today, in
  `tests/DRYL.Components.Tests/DrylNotificationsTests.cs`: the two-mode split for
  a row click, the empty inbox without a registered service, and the unread
  state's text alternative. The bell's badge, the panel's header actions, the
  relative time and the per-entry aura are not.

## Cross-cutting evidence (`SPEC-05`)

- **Both color modes** — token-only colors, verified by
  `node scripts/check-light-sync.mjs` and
  `node scripts/validate-light-contrast.mjs`. `--glass-1`, `--glass-2`,
  `--accent-soft`, `--accent-grad` and `--bg-1` are the mode-dependent tokens;
  the component defines no mode-specific rule.
- **Enter/exit animation** — the panel's are the popover's, specified in
  `E11 Surfaces/F1`. The component's own motion is the bell's hover transition,
  the row hover and the dismiss control's fade-in. Individual rows do not
  animate in or out, which is recorded above.
- **Keyboard and a11y** — the "Keyboard and accessibility" criteria above. The
  substantive decisions are the count in the bell's own label and the per-entry
  dismiss labels.
- **AI mode** — yes, per entry rather than per component. An inbox is where
  asynchronous AI work lands, and provenance belongs to the individual
  notification.
- **Demo page** — `DRYL.Website/Components/Pages/DemoNotifications.razor`, with
  the examples `Components/Examples/Notifications/ServiceDriven.razor`,
  `.../Controlled.razor` and `.../AiEntries.razor`.
- **`ComponentCatalog`** — registered as `"Notifications"` / `notifications` in
  `DRYL.Website/Components/ComponentCatalog.cs`, flagged AI-capable.
