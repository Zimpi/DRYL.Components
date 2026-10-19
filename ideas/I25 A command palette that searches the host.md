# A command palette that searches the host

## Meta
- **State:** Adopted
- **Specs:** [`specs/E10 Navigation/F4 DrylCommandPalette/_Component.md`](../specs/E10%20Navigation/F4%20DrylCommandPalette/_Component.md),
  [`specs/E10 Navigation/F5 DrylCommand.md`](../specs/E10%20Navigation/F5%20DrylCommand.md)

## Problem

`DrylCommandPalette` already opens on Ctrl+K / ⌘K, groups results, walks them
with ↑↓ / Enter / Esc, takes an async `SearchProvider`, hosts declarative
`<DrylCommand>`s with arguments and offers the `ICommandResolver` AI seam. What
it does not do well is the job most hosts want from it: **one field that finds
anything in the application** — records from the host's own data, pages,
commands — and hands the query to an assistant when nothing fits.

Checking the code against that job, and against the library's own rules:

1. **It pops.** The overlay is a bare `@if (Open)`. It enters with a CSS
   keyframe (`cmd-in`) and leaves with nothing — the node is gone in the same
   frame (`DESIGN-12`). Result rows neither enter nor leave; the AI result panel
   is the only part with an entrance.
2. **Rows are keyed by position** (`@key="rIdx"`). Every keystroke maps
   different results onto the same keys, so a row cannot animate in or out on
   its own and Blazor patches text into foreign rows instead of moving them.
3. **Loading is a spinner** (`DrylSpinner` in the search row) and the list
   collapses to its new height when results arrive — the layout jumps.
4. **The chrome is fixed English:** "Back", "Run", "Thinking…",
   "Confirm action", "Cancel", the default `Placeholder` ("Search commands…")
   and the default `AriaLabel` ("Command palette"). `EmptyText` defaults to
   German ("Keine Treffer") — the only non-English default in the component.
5. **Groups are sorted alphabetically**, ungrouped rows always last. A host
   cannot say "people first, pages last", and cannot keep an entry like
   "Ask the assistant: …" at a fixed place.
6. **No movement between groups.** With many groups, ↓ is the only way down.
   Tab is swallowed by the modal focus trap (`dryl.modal`), so it does nothing.
7. **No room for secondary data.** A row has label, description, icon and a
   shortcut — nothing for a short trailing fact ("12 in stock", "yesterday").
8. **The debounce is fixed at 250 ms** and also applies to the empty query, so
   the start state appears a quarter second late on every open.
9. **There is no spec.** `DrylCommandPalette`, `DrylCommand`,
   `DrylCommandArgument`, `ICommandRegistry`, `ICommandResolver`, `CommandItem`
   are not covered under `specs/E10 Navigation/`. The comment in the component
   calls the `CommandItem` / `SearchProvider` path "legacy", although it is the
   only path that can carry search results (a `DrylCommand` is a component, not
   a data row).

Target role: a Blazor developer whose application holds its own records and
wants one keyboard-first entry point to all of them, with an assistant as the
fallback — without the palette knowing anything about the application.

## Solution Idea

The palette stays a **host-agnostic shell**: the host supplies results, order,
the fallback entry and what "recent" means; the palette renders, moves and
handles the keyboard. Nothing below names a domain, a permission or an AI
provider.

**Motion.**
- Backdrop and panel mount through `DrylPresence` (fade for the backdrop, scale
  for the panel). Closing plays the exit before the node leaves; the modal is
  detached and focus returns to the element that had it before opening.
- `CommandItem` gains a stable `Id`. Rows are keyed by it: a row that survives a
  keystroke stays in place, a new row fades and rises in, a dropped row fades
  out. On open, the first set of rows enters staggered (the `.stagger` steps of
  `harness/tokens.md`, at most eight).
- Under `prefers-reduced-motion` all of this is instant; function is unchanged.

**Loading without jumping.**
- The first load after opening shows `DrylSkeleton` rows in the shape of result
  rows (icon, label, description line) instead of a spinner.
- A later query keeps the previous rows visible but quieted until the new ones
  arrive; the list height changes through `DrylMorph`, not by snapping.

**Words.** A `CommandPaletteTexts` record — the pattern of `CanvasTexts` —
carries every human word: placeholder, accessible name, empty text, back, run,
thinking, confirm title / confirm / cancel, and the group-navigation hint.
English defaults, including `EmptyText` ("No results"), so the component has one
default language. The existing `Placeholder`, `EmptyText` and `AriaLabel`
parameters stay (API freeze) and win over `Texts` when set, so a host that set
them today sees no change.

**Order.**
- A palette parameter `GroupOrder` (`CommandGroupOrder.Alphabetical` — today's
  behaviour and the default — or `CommandGroupOrder.AsProvided`, the order in
  which the provider returns them).
- A palette parameter `Fallback` (`Func<string, CommandItem?>`): for a non-blank
  query the host may return one entry that is always present. It sits **last**
  while other rows match and moves **first, focused** when nothing else does. The
  palette does not know what it does — "ask the assistant", "search the web" and
  "create '…'" are all the same to it.

**Recent items.** The palette remembers nothing. A new `OnItemRun`
(`EventCallback<CommandItem>`) fires whenever a provider item, the fallback or a
static item runs; the host records what it wants and returns its recent items as
the result of the empty query, in a group it names. Only the host knows whether
an item still exists and whether the viewer may still see it.

**Keyboard.** Tab / Shift+Tab jump to the first row of the next / previous group
(wrapping), without leaving the field. The group headers become reachable names
for the listbox (`role="group"` + `aria-labelledby`), so a screen reader hears
the group when focus enters it.

**Secondary data.** `CommandItem.Meta` — a short muted text at the trailing end
of the row, before the arrow.

**Timing.** A `Debounce` parameter (`TimeSpan`, default 250 ms). The empty query
is never debounced.

**Spec.** `DrylCommandPalette` and its satellites get their spec under
`specs/E10 Navigation/`, written from the code as it stands plus this idea, so
coverage rises by the components it names. Code comments and spec call the
`CommandItem` / `SearchProvider` path **items** (search results and static
entries) and the `DrylCommand` path **commands** — the word "legacy" goes.

## Scope
- **In scope:** everything under *Solution Idea*; the spec for the palette, its
  models and its services; tests for keying, group order, fallback placement,
  Tab navigation, debounce and texts; the website demo updated to a palette
  with a provider, a fallback and two groups.
- **Out of scope:** any data source, ranking or fuzzy matching of provider
  results (the provider ranks — the palette shows what it gets); permissions;
  semantic or vector search; storing recent items inside the palette; a mobile
  trigger button (the host binds `Open` to its own button); changing the
  `DrylCommand` / registry / resolver behaviour; changing the hotkey rule that
  Ctrl+K is ignored inside text fields.

## Impact
- **Harness:** none. Checked against `harness/tokens.md`: `DrylPresence`,
  `DrylMorph`, `DrylSkeleton`, the `.stagger` steps and `--dur-fast|med|slow` /
  `--ease-out|spring` cover all motion. `Meta` takes the size and `--fg-dim` of
  `.cmd-item-desc`; the library has no font-size tokens, so no new one is needed.
  The literal `px` values already in `DrylCommandPalette.razor.css` stay as they
  are and are not multiplied.
- **Specs:** new `specs/E10 Navigation/F4 DrylCommandPalette.md` (or a folder
  with stories for motion, keyboard, provider, AI seam), entries in
  `specs/E10 Navigation/_Api.md` for `CommandItem`, `CommandItemType`,
  `CommandGroupOrder`, `CommandPaletteTexts`, `ICommandRegistry`,
  `ICommandResolver`, `CommandResolution`, `CommandContext`, and in
  `_Interop.md` for `dryl.commandpalette`. Check `specs/E15 Agent Inputs/_Api.md`,
  which already mentions the palette.
- **Public API:** additive, MINOR (`REL-01`): `CommandItem.Id`,
  `CommandItem.Meta`, palette parameters `Texts`, `GroupOrder`, `Fallback`,
  `Debounce`, `OnItemRun`, new enum `CommandGroupOrder`, new record
  `CommandPaletteTexts`. Behaviour changes, named in the changelog: `EmptyText`
  defaults to English; the overlay animates out; Tab moves between groups.
  `Placeholder`, `EmptyText` and `AriaLabel` stay and win over `Texts`.
- **Code:** `Components/Navigation/DrylCommandPalette.razor` and `.razor.css`,
  `Models/CommandItem.cs`, `wwwroot/js/dryl.js` (`dryl.commandpalette`: Tab
  interception inside the field, since Blazor cannot prevent default
  conditionally), `DrylAiCommandPalette` in the Agents package if it forwards
  texts. Risks: the backdrop is **portaled to `<body>`** — wrapping it in
  `DrylPresence` must keep the portal working while the exit plays; an item
  without `Id` must fall back to a key that stays stable for its label and group,
  so existing hosts keep working; the async search writes `_displayRows` from a
  background task — row identity must not race with the exit animation.

## Decisions
- 2026-10-18: Product Owner chose to **extend `DrylCommandPalette`** over a new
  component — the shell carries, two palettes would be one too many.
- 2026-10-18: Product Owner set the bar: **generic, no trace of any host** in
  the library. The host owns data, order, fallback meaning and recent items.
- 2026-10-18: Product Owner chose **host-supplied recent items** with an
  `OnItemRun` event, over the palette storing ids under a `RecentKey` — only the
  host knows whether an item still exists and may still be seen.
- 2026-10-18: Product Owner chose a palette-level **`GroupOrder`** enum with
  `Alphabetical` as default, over a per-item rank — a provider already returns
  its groups in the order it wants, and existing hosts see no change.
- 2026-10-18: Product Owner chose a dedicated **`Fallback`** parameter, over a
  general `Pin` on any item — "last while others match, first and focused when
  none does" is its own behaviour, not a position.
- 2026-10-18: Product Owner delegated the remaining points to the Tech Lead's
  recommendation: existing `Placeholder` / `EmptyText` / `AriaLabel` **win over
  `Texts`** (no change for current hosts), and the **"legacy" wording goes** —
  items and commands are two equal paths.
- 2026-10-18: Product Owner confirmed the final version.
- 2026-10-19 (Tech Lead, implementation): the list height does not run through
  `DrylMorph`. Every row sits in a one-track grid whose track animates between
  `0fr` and `1fr`, so rows grow in and collapse out at their own height and the
  list resizes with them. A FLIP morph per keystroke would compete with the
  host's own morphs (one per service) and scale text while it measures.
- 2026-10-19 (Tech Lead, implementation): the overlay's `DrylPresence` wrapper
  animates nothing visible itself — opacity on an ancestor would make it a
  backdrop root and drop the blur for the whole fade; backdrop and panel animate
  off its `presence-enter` / `presence-exit` classes.
- 2026-10-19 (Tech Lead, implementation): `DrylCommand` gets its own spec
  (`F5`) rather than riding in the palette's `Source`; a footer with key hints
  carries the group-navigation hint the texts name.

## Open Points
