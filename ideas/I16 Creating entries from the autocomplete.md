# Creating entries from the autocomplete

## Meta
- **State:** Adopted
- **Specs:** [`specs/E8 Inputs/F5 DrylAutocomplete.md`](../specs/E8%20Inputs/F5%20DrylAutocomplete.md),
  [`specs/E8 Inputs/_Api.md`](../specs/E8%20Inputs/_Api.md),
  [`specs/E8 Inputs/_Interop.md`](../specs/E8%20Inputs/_Interop.md)

## Problem

`DrylAutocomplete` can only pick what already exists. Many forms offer a list
of known values and still have to accept a new one: a tag, an organisation, a
role, a category the list has not seen yet. Today a host builds that next to
the field — a second text box, a "+ new" button, a dialog — and the person
types the same word twice. Typed text that matches nothing is also thrown away
the moment the list closes, so a form loses input silently.

The component also hard-codes its two status texts, `No results` and
`Searching…`, in English. A German application cannot use it without showing
English in the middle of the form.

Target role: the Blazor developer building a form on DRYL — concretely the
El Destino Hub's contact book, whose chips (family, gang, order, position) are
picked from the values already in use or created on the spot. Secondarily the
end user, who expects the word they typed to be kept.

## Solution Idea

An opt-in creation path on the existing component, no new component.

- `OnCreate` (`Func<string, Task<TItem?>>?`) switches it on. It receives the
  trimmed query and returns the new item, or `null` to abort. The returned
  item becomes the bound value exactly like a picked one.
- While `OnCreate` is set, the query is not empty and no item's `DisplayText`
  equals the query (trimmed, case-insensitive), the list ends with one more
  option: a plus glyph and `CreateLabel(query)`, default `Create "{query}"`.
  It is an ordinary option — arrows, Enter, Tab, pointer — and a real
  `role="option"` with that text as its name. With no other result it stands
  alone instead of `EmptyText`.
- With `OnCreate`, Enter or Tab without a highlighted option commits the
  query: an exact match is picked, otherwise the item is created.
- With `OnCreate`, closing the list by pressing outside commits a non-empty
  query the same way, so a form never loses what was typed. Without it the
  text is reset as today.
- `EmptyText` and `LoadingText` replace the fixed texts; their defaults are
  the old English strings.
- The create option enters and leaves through `DrylPresence`, and its
  highlight glides like every other option.

## Scope

- **In scope:** `OnCreate`, `CreateLabel`, `EmptyText`, `LoadingText`; the
  create option, its keyboard and pointer behaviour, commit on Enter/Tab/outside
  press; a first spec for `DrylAutocomplete` (it had none); making the
  keyboard highlight visible, which the create option depends on.
- **Out of scope:** multi-value creation (chip input), creating from inside a
  dialog, validating the new value (the host's `OnCreate` decides, and may
  return `null`), localising the listbox's accessible name, a visible
  "new" badge on created items, changing behaviour when `OnCreate` is unset.

## Impact

- **Harness:** no new token, duration, easing, `AiState` or dependency. The
  create option uses `DrylPresence` (`PresenceTransition.Fade`,
  `PresenceSpeed.Fast`), the existing `Plus` icon and the option's existing
  hover/highlight tokens.
- **Specs:** new `specs/E8 Inputs/F5 DrylAutocomplete.md` (phase C: the
  component had no spec — the spec records the current contract plus the
  extension); `specs/E8 Inputs/_Api.md` and `_Interop.md` gain the component.
  `scripts/spec-coverage-baseline.json` loses the entry.
- **Public API (additive, MINOR):** `OnCreate`, `CreateLabel`, `EmptyText`,
  `LoadingText`. `OnCreate` is a `Func`, not an `EventCallback`, because it
  must return the created item — the same exception `ItemsProvider` and
  `SearchFunc` already make.
- **Code:** `Components/Inputs/DrylAutocomplete.razor` + `.razor.css`.
  Risks: the filter runs on the thread pool, so result writes and the commit
  must run on the renderer (`InvokeAsync`); with `ItemsProvider` the visible
  results can lag the query, so a commit re-asks the provider before creating
  a duplicate; a double commit (Enter, then an outside press) must create
  once.

## Decisions

- 2026-09-27 — Jan: DRYL may be extended for the contact book ("DRYL darfst du
  auch einbauen"); the public API names and defaults above were given by him
  and are binding for the consumer.
- 2026-09-27 — Jan: the create option is the last option, only while no item
  matches exactly; alone when nothing else matches.
- 2026-09-27 — Jan: an outside press commits a pending query when `OnCreate`
  is set, so typed text is not lost. Rejected: resetting as today.
- 2026-09-27 — Jan: without `OnCreate` the component behaves exactly as
  before. Tech Lead consequence: Enter/Tab picking an exact match without a
  highlight is part of the creation path only, not a change for existing
  consumers.
- 2026-09-27 — Tech Lead: commit on focus loss is driven by the popover's
  outside press and by Tab, not by the input's `blur` — pressing an option in
  the portalled list blurs the input before its click lands, and a `blur`
  commit would create the typed text instead of the option pressed.
- 2026-09-27 — Tech Lead: the keyboard highlight was never drawn (the
  `is-highlighted` class was not applied). A keyboard-selectable create option
  is useless without it, so the fix is part of this idea.
- 2026-09-27 — Jan confirmed the scope through the task brief; the idea is
  `Ready` and was carried into the spec the same day (`Adopted`).

## Open Points
