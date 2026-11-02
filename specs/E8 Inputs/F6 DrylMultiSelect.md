# DrylMultiSelect

## Meta
- **State:** Modified
- **Source:** code/DRYL.Components/Components/Inputs/DrylMultiSelect.razor
              code/DRYL.Components/Components/Inputs/DrylMultiSelect.razor.css

## User Story

As a Blazor developer building a form on DRYL, I want a dropdown that lets the
user pick any number of options, shows what is picked as removable chips and
stays open while they pick — and, on a long list, lets them type to find an
option — so that I can bind a list of values without building a listbox, its
keyboard model, its chip row or a search beside it myself.

## Description

`DrylMultiSelect` is a `combobox` trigger that opens a `listbox` with
`aria-multiselectable="true"`. It binds a list of option values through
`SelectedValues` / `SelectedValuesChanged`; it is not an `InputBase`, so it
takes part in no `EditForm` validation.

**It is a consumer of `E11 Surfaces`/`F1 DrylPopover`**, and the panel's
portal, placement, click-outside dismissal and entrance and exit motion are
promised there, not here. `DrylMultiSelect` opens the popover with
`Surface="false"`, `MatchTriggerWidth="true"`, `TriggerTogglesOpen="false"` and
`CloseOnEscape="false"`: it draws its own list surface, toggles on its own
trigger and handles `Escape` itself.

**Choosing toggles and keeps the list open.** A click or `Enter`/`Space` on the
highlighted option adds or removes its value; the panel stays open so several
values can be picked in a row. `Escape` and `Tab` close it.

**The highlight is the component's, the ink is the library's.** The highlighted
option carries `.is-highlighted` and is announced through the trigger's
`aria-activedescendant`. The listbox carries `data-dryl-glide="[role=option]"`,
so once the popover reveals it, `dryl.motion.glideScan` lays the list ink
(idea I38) under the option the user acted on last and lets it glide to the
next — see `E11 Surfaces/_Interop.md`. Without script the highlighted option
keeps its static `--glass-2` fill.

**Search is opt-in** (`ideas/I43 A multi-select that searches.md`). With
`Searchable` set, the trigger becomes a chip input: the chips, then a text
field that takes the rest of the line. The field, not the surrounding `div`,
is then the `combobox`: it carries the id the label points to and every
`aria-*` the trigger carries without search. Typing opens the list and
filters it over the options' labels — case-insensitive `Contains`, or a
consumer `SearchFunc` — and highlights the first match. `Enter` toggles the
highlighted option and `Space` types. The query stays after a toggle, so
several matches of one search can be picked in a row, and clears when the
list closes. `Backspace` in an empty field first marks the last picked value,
a second press removes it. `Escape` first clears a query, a second press
closes the list.

**Filtered rows stay mounted.** In search mode each option sits in a
`role="none"` track wrapper; a row the query does not match collapses its
track and fades, and is `inert` and `aria-hidden` until it matches again, so
the list never mounts or unmounts an option while the user types. The part of
a label that matches the query is marked with `<mark>`. With a `SearchFunc`
that matches on something else, a row whose label does not contain the query
is shown unmarked.

## Public API

| Member | Type | Default | Meaning |
|---|---|---|---|
| `Items` | `IEnumerable<SelectItem>?` | `null` | The options, in order. |
| `SelectedValues` | `IReadOnlyList<string>` | `[]` | The picked option values. |
| `SelectedValuesChanged` | `EventCallback<IReadOnlyList<string>>` | — | Raised with the new list after a toggle or a chip removal. |
| `Label` | `string?` | `null` | Visible label above the trigger. |
| `Placeholder` | `string?` | `null` | Shown while nothing is picked; falls back to `Select options…`. |
| `HelperText` | `string?` | `null` | Text below the control, linked through `aria-describedby`. |
| `MaxVisibleChips` | `int` | `3` | Chips shown before the rest collapse into `+N`. |
| `State` | `InputState` | `Default` | `Default`, `Error` or `Success` border treatment. |
| `Disabled` | `bool` | `false` | Greys the trigger out and ignores clicks. |
| `AriaLabel` | `string?` | `null` | Accessible name when `Label` is not set. |
| `Ai` | `AiState` | `None` | AI ambient state of the field. |
| `Aura` | `AiAura?` | `null` | Aura variant; inherits a surrounding `DrylAiScope`. |
| `Searchable` | `bool` | `false` | Turns the trigger into a chip input that filters the list as the user types. |
| `SearchPlaceholder` | `string?` | `null` | Placeholder of the search field while values are picked; falls back to `Search…`. While nothing is picked the field shows `Placeholder`. |
| `SearchFunc` | `Func<string, IEnumerable<SelectItem>, IEnumerable<SelectItem>>?` | `null` | Filter replacing the default case-insensitive `Contains` on `Label`. Receives the trimmed query and all items. |
| `EmptyText` | `string` | `"No results"` | Text of an open list whose query matches no option. |

`SearchFunc` is a `Func` rather than an `EventCallback` because it returns a
value the component needs (conventions §3 governs notification events only).
Its shape is `DrylAutocomplete.SearchFunc` with `SelectItem` for `TItem`.

## Acceptance Criteria

### Structure and value

- The trigger carries `role="combobox"` and `aria-haspopup="listbox"`.
- The trigger's `aria-expanded` is `true` exactly while the list is open.
- The trigger's `aria-controls` names the listbox.
- The listbox carries `role="listbox"` and `aria-multiselectable="true"`.
- Each item of `Items` renders as one option carrying `role="option"`, in order.
- An option's `aria-selected` is `true` exactly while its value is in `SelectedValues`.
- A list without items shows a `No options` row instead of options.
- Each picked value renders as a chip labelled with its option's label.
- A picked value without a matching option renders its raw value as the chip label.
- No more than `MaxVisibleChips` chips are shown.
- Picked values beyond `MaxVisibleChips` collapse into one `+N` marker.
- The placeholder is shown exactly while nothing is picked.

### Choosing

- A click on the trigger opens a closed list and closes an open one.
- A click on an option adds its value when it was not picked.
- A click on an option removes its value when it was picked.
- Choosing an option leaves the list open.
- A chip's remove button removes its value without opening or closing the list.
- Every change raises `SelectedValuesChanged` once, with the whole new list.
- A disabled control does not open.

### Keyboard

- `ArrowDown` on a closed control opens it with the first option highlighted.
- `ArrowDown` moves the highlight to the next option and stops at the last.
- `ArrowUp` moves the highlight to the previous option and stops at the first.
- `Enter` and `Space` on an open list toggle the highlighted option.
- `Enter` and `Space` on a closed control open it.
- `Escape` and `Tab` close the list.
- The trigger's `aria-activedescendant` names the highlighted option while the list is open.
- Moving the highlight scrolls it into view.
- Hovering an option makes it the highlighted option.

### Search

These criteria hold while `Searchable` is `true`; without it the control
behaves as described above.

- The trigger contains a text field after the chips.
- The text field carries `role="combobox"`, `aria-autocomplete="list"`, `aria-expanded`, `aria-controls` and `aria-activedescendant`.
- The surrounding trigger carries no `role` and no `tabindex`.
- The label's `for` names the text field.
- The text field shows `Placeholder` while nothing is picked, and `SearchPlaceholder` while something is.
- Typing into the text field opens a closed list.
- An option whose label contains the trimmed query, ignoring case, is listed.
- An option whose label does not contain the query is filtered out.
- A `SearchFunc` decides alone which options are listed.
- An empty or whitespace query lists every option.
- A filtered-out option is `inert` and `aria-hidden="true"` and stays in the DOM.
- A query change highlights the first listed option.
- A query that lists no option shows `EmptyText` and highlights nothing.
- `ArrowDown` and `ArrowUp` move the highlight across listed options only.
- `Enter` on an open list toggles the highlighted option.
- `Enter` on a closed control opens it.
- `Space` types a space and toggles nothing.
- Toggling an option keeps the query and the filtered list.
- Closing the list clears the query.
- A click on the trigger focuses the text field and opens a closed list.
- A click on the trigger leaves an open list open.
- `Backspace` in an empty text field marks the last picked value without removing it.
- A second `Backspace` while the last value is marked removes it and raises `SelectedValuesChanged` once.
- Any other key clears the mark.
- `Backspace` in a text field that holds text edits the text and removes nothing.
- `Escape` with a query clears the query and leaves the list open.
- `Escape` without a query closes the list.
- The matching part of a listed option's label is wrapped in a `<mark>`.
- A label that does not contain the query renders without a `<mark>`.
- The text field's caret keys (`ArrowLeft`, `ArrowRight`, `Home`, `End`) keep their native behaviour.
- `Enter` in the text field does not submit a surrounding form.
- A disabled control's text field is disabled.

### Appearance and motion

- The floating list surface uses `--panel-float` with `--glass-fx-float`, `--line-strong`, `--r-md` and `--shadow-lg`.
- The list is inset by `--sp-1` on every side, so the ink sits inside its radius.
- Options are rounded with `--r-sm`.
- The listbox carries `data-dryl-glide="[role=option]"`, which makes it a list-ink host.
- While the ink is active, the option under the mouse, the highlighted option, or neither is covered by the ink — the one the user acted on last.
- While the ink is active, options draw no hover or highlight fill of their own; the covered option's text brightens to `--fg`.
- The ink glides between options on `--dur-slow` / `--ease-spring` and appears without travel when it was hidden.
- A soft accent light follows the mouse inside the covered option.
- Without the ink (no script), the hovered and the highlighted option keep a `--glass-2` fill.
- A picked option keeps its accent tint and its checked box whether or not it is covered.
- Under `prefers-reduced-motion` the ink moves without a transition.
- A filtered-out row collapses its track on `--dur-med` / `--ease-out` and fades on `--dur-fast` / `--ease-out`; a row that matches again opens the same way.
- While a row's track moves, the ink stays on the row it covers.
- Under `prefers-reduced-motion` rows collapse and open without a transition.
- The marked part of a label reads `--accent-fg` at weight 600 on no fill of its own.
- The marked last chip carries `.chip--armed`: an `--accent-line` ring and a stronger accent tint.
- The search text field draws no border, fill or focus ring of its own; the trigger's focus treatment shows while it has focus.
- The component branches on no colour mode (`DESIGN-02`).

### AI mode

- `Ai` drives the shared aura on the trigger's wrapper.
- An unset `Aura` inherits the surrounding `DrylAiScope`, falling back to `AiAura.Comet`.

## Cross-cutting evidence (`SPEC-05`)

- **Both color modes** — measured on `/components/multiselect` in dark and
  light with the `Tags` list open and the highlight moved by `ArrowDown`: the ink
  covers the highlighted option, picked rows keep their tint, and no option
  paints a fill of its own. The component's stylesheet has no mode selector.
- **Enter/exit animation** — **yes**, the popover's; the ink appears with an
  opacity fade on first placement and glides afterwards.
- **Keyboard and a11y** — the "Keyboard" and "Search" criteria above; read off
  `HandleKeyDown` and `HandleSearchKeyDown`. In search mode focus never leaves
  the text field; `aria-activedescendant` names the highlighted option and
  filtered rows leave the accessibility tree. The ink follows `.is-highlighted`, so it moves with the
  keyboard exactly as `aria-activedescendant` does.
- **AI mode** — **yes**: a model may propose or fill a multi-value field, so
  the field carries `Ai` and `Aura` like the other inputs (`AI-05`).
- **Demo page** — `/components/multiselect` in `DRYL.Website`, built from the
  `Basics`, `Search`, `Overflow`, `States` and `AiMode` examples under
  `Components/Examples/MultiSelect/`.
- **`ComponentCatalog`** — registered as `"Multi Select"` / `multiselect`,
  group `Inputs`, `ClassName` `"DrylMultiSelect"`, AI flag `true` (`REL-04`).
- **Tests** — `tests/DRYL.Components.Tests/ListGlideTests.cs` covers the ink
  host marker; the ink's behaviour, including following a collapsing row, is
  covered in `tests/js/glide.test.mjs`. The "Search" criteria are covered in
  `tests/DRYL.Components.Tests/DrylMultiSelectSearchTests.cs`, the key list of
  `dryl.keynav` in `tests/js/keynav.test.mjs`.

## Recorded debt (`State: Implemented`)

**Deviations from the acceptance criteria above: none.**

- **Without `Searchable`, `Home` and `End` do nothing**, although
  `dryl.keynav` suppresses their default; `DrylSelect` handles both. In search
  mode they move the caret.
- **`ArrowUp` before any highlight does nothing** rather than starting at the
  last option.
- **Fixed English strings** — `No options`, `Options` / `{Label} options` and
  `Remove {label}` are not translatable; `Select options…`, `Search…` and
  `No results` are, through `Placeholder`, `SearchPlaceholder` and `EmptyText`.
- **No announcement of the result count** — a screen reader hears the
  highlighted option through `aria-activedescendant`, not how many options a
  query left.
- **Option styles are a copy of `DrylSelect`'s**, because scoped CSS does not
  cross components; the two copies have already drifted (the picked tint exists
  only here). A shared rule in `dryl.css` would end that.
- **No `EditForm` integration** — the control is not an `InputBase`, so a
  validation message for the bound list is never shown.
