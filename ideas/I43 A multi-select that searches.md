# A multi-select that searches

## Meta
- **State:** Draft

## Problem

Raised by the Product Owner on 2026-11-02, for release 3.16.0:

> "Ich würde gerne das Multiselect erweitern und verbessern. Und zwar soll es
> die Möglichkeit dort geben, wenn das Dropdown aufklappt, dass man da auch
> suchen kann. Nach Einträgen. Also quasi so ein bisschen wie ein Autocomplete.
> Nur, dass man halt mehrere Dinge auswählen kann."

`DrylMultiSelect` lists every `SelectItem` and offers only the arrow keys to
reach one. With a dozen options that is fine; with fifty tags, countries or
team members the user scrolls a 240 px panel and reads every row. The library
already has a search-as-you-type list, `DrylAutocomplete`, but it binds one
item. A host that needs several values from a long list today either accepts
the scrolling or builds its own chip input next to an autocomplete.

Target role: the end user picking several entries from a long list; the Blazor
developer, who should get this from one parameter rather than by composing two
components.

## Solution Idea

An opt-in search on the existing component, no new component. The list filters
as the user types; toggling an entry works exactly as today (the panel stays
open, the selection is a list of values).

Where the user types is the main open question — three options. A and B are
prototyped side by side in the artifact
`https://claude.ai/artifact/UtU9G388EjcmX2LBYCReL1`, with two switches for the
open points on row motion (collapse and fade, or instant) and matched-text
highlighting:

- **A — Search in the panel.** Opening the list puts a search field at the top
  of the panel and moves focus into it. The trigger keeps its chips unchanged.
  Matches the brief's wording ("wenn das Dropdown aufklappt"). Risk: focus
  leaves the trigger for a portaled panel, so keyboard handling, `aria-*` and
  the click-outside dismissal have to follow it; the trigger is no longer the
  element announcing the active option.
- **B — Search in the trigger.** The trigger becomes a chip input: the chips,
  then a text field that takes the rest of the line. Typing opens and filters
  the list; Backspace in an empty field removes the last chip. This is the
  ARIA editable-combobox pattern `DrylAutocomplete` already follows: focus never
  leaves the field, `aria-activedescendant` keeps working. Risk: the
  `MaxVisibleChips` collapse to `+N` has to leave room for the text field, and
  the trigger's height can change while typing. *Tech Lead recommendation.*
- **C — Both, chosen by a parameter.** More surface, two behaviours to specify,
  test and keep in sync. Not recommended for the first step.

## Scope
- **In scope (proposed):** an opt-in switch; client-side filtering over the
  `Label`s (case-insensitive `Contains`, overridable); keyboard and pointer on
  the filtered list; selected entries stay listed and checked while they match;
  an empty-result text; the placeholder of the search field; a first spec for
  `DrylMultiSelect` (it has none).
- **Out of scope (proposed):** server-side search (`ItemsProvider`), creating
  new entries from the query (the I16 path for the multi-select), grouping,
  "select all matches". Each can follow as its own idea.
- **Moved into scope by decision (2026-11-02):** highlighting the matched part
  of each label.

## Impact
- **Harness:** none. The text field reuses the input tokens, the rows reuse the
  I38 gliding ink and the I39 checkbox. Filtered rows collapse on a
  `grid-template-rows` track with `--dur-med` / `--ease-out` and fade on
  `--dur-fast`, as in the prototype. Rows stay mounted and are hidden by a class,
  so nothing mounts conditionally and `DrylPresence` is not needed for them
  (`DESIGN-12`). The matched text is a `<mark>` in `--accent-fg` at weight 600,
  a small indicator in the sense of `DESIGN-08`. No new token, keyframe or
  dependency.
- **Specs:** `DrylMultiSelect` has no spec. It gets its first under `E8 Inputs`
  (next free feature number), and its line in
  `scripts/spec-coverage-baseline.json` is removed. `E8 Inputs/_Api.md` gains
  the new parameters.
- **Public API (proposed, additive → MINOR, fits 3.16.0):** `Searchable`
  (`bool`, default `false`), `SearchPlaceholder` (`string?`), `SearchFunc`
  (`Func<string, IEnumerable<SelectItem>, IEnumerable<SelectItem>>?`, same shape
  as `DrylAutocomplete.SearchFunc`), `EmptyText` (`string`, default
  `"No results"`). Today's fixed `"No options"` and `"Options"` texts are
  candidates to become parameters in the same step.
- **Code:** `Components/Inputs/DrylMultiSelect.razor` and `.razor.css`; the
  `dryl.keynav` attach target moves to whichever element holds focus; the
  `data-dryl-glide` ink must re-measure when rows are filtered out. Website: the
  multi-select demo gets a searchable example.

## Decisions
- 2026-11-02: Variant **B — search in the trigger** (Product Owner, after the
  prototype: "B weich zuklappen, highlighted"). A and C are dropped.
- 2026-11-02: Rows the filter removes **collapse and fade**; rows it brings back
  open the same way (Product Owner).
- 2026-11-02: The matched part of a label is **highlighted** (Product Owner).
  This moves it from out of scope into scope.
- 2026-11-02: The highlight marks the first case-insensitive occurrence of the
  trimmed query in the label, whatever filter ran. With a custom `SearchFunc`
  that matches on something else, a row without such an occurrence is shown
  unmarked (Tech Lead).

## Open Points
- Opt-in `Searchable` (default `false`) rather than searchable by default
  (Tech Lead recommendation) — awaiting Product Owner confirmation.
- Keyboard details as prototyped: the query stays after a toggle and clears on
  close; Backspace in an empty field first marks the last chip, then removes
  it; Escape first clears the query, then closes — awaiting confirmation.
- Remaining out-of-scope list (server search, creating entries, grouping,
  "select all matches") — awaiting confirmation that none belongs in 3.16.0.
