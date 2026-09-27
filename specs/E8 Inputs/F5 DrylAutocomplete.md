# DrylAutocomplete

## Meta
- **State:** Implemented
- **Source:** code/DRYL.Components/Components/Inputs/DrylAutocomplete.razor
              code/DRYL.Components/Components/Inputs/DrylAutocomplete.razor.css

## User Story

As a Blazor developer, I want a typed combobox that filters a list as the user
types, can search a server, and — when I allow it — creates the entry the user
typed if it does not exist yet, so that a form offers the known values without
losing a new one and without a second field beside it.

## Description

`DrylAutocomplete<TItem>` is a text input with an anchored suggestion list. It
binds a single `TItem` (a reference type) through `InputBase<TItem?>`. Typing
clears the bound value and filters: client-side over `Items` (with the default
case-insensitive `Contains` on `ToString()`, or a consumer `SearchFunc`), or
server-side through `ItemsProvider`, debounced and cancelled on the next
keystroke. Picking an option binds it and writes its `DisplayText` into the
input. The list is a `DrylPopover` surface that matches the input's width.

The creation path is opt-in (`ideas/I16 Creating entries from the
autocomplete.md`). With `OnCreate` set, a query that no item matches exactly
gets one extra, last option — a plus glyph and `CreateLabel(query)` — and
committing the query (Enter, Tab or pressing outside the field) picks the exact
match or creates the item. The created item is bound like a picked one. The
host is responsible for adding it to `Items` or its provider if later searches
should find it.

Without `OnCreate` the component behaves as it did before the creation path
existed. The two status texts are parameters, so a host localises them.

Typical use with strings:

```razor
<DrylAutocomplete TItem="string" @bind-Value="_family"
                  Items="_knownFamilies" DisplayText="s => s"
                  OnCreate="q => Task.FromResult<string?>(q)"
                  CreateLabel='q => $"„{q}“ anlegen"'
                  EmptyText="Keine Treffer" LoadingText="Suche…" />
```

## Public API

Namespace: `DRYL.Components`. `@typeparam TItem where TItem : class`. Inherits
`InputBase<TItem?>` and implements `IDisposable`. Inherited `Value`,
`ValueChanged`, `ValueExpression`, `DisplayName` and `AdditionalAttributes`
follow [`_Api.md`](_Api.md).

| Member | Type | Default | Purpose |
|---|---|---|---|
| `Label` | `string?` | `null` | Visible label above the input. |
| `Placeholder` | `string?` | `null` | Placeholder of the input. |
| `HelperText` | `string?` | `null` | Text below the field, replaced by validation messages. |
| `State` | `InputState` | `InputState.Default` | Visual state override. |
| `Disabled` | `bool` | `false` | Disables the input. |
| `AriaLabel` | `string?` | `null` | Accessible name when `Label` is empty. |
| `Ai` | `AiState` | `AiState.None` | AI state, with scope inheritance. |
| `Aura` | `AiAura?` | `null` | Aura variant, with scope inheritance. |
| `Items` | `IEnumerable<TItem>?` | `null` | Static items for client-side filtering. |
| `SearchFunc` | `Func<string, IEnumerable<TItem>, IEnumerable<TItem>>?` | `null` | Client-side filter replacing the default. |
| `ItemsProvider` | `Func<string, CancellationToken, Task<IEnumerable<TItem>>>?` | `null` | Server-side search; takes precedence over `Items`. |
| `ItemTemplate` | `RenderFragment<TItem>?` | `null` | Custom option content. |
| `DisplayText` | `Func<TItem, string>?` | `null` | Item-to-text conversion; `null` uses `ToString()`. |
| `OnCreate` | `Func<string, Task<TItem?>>?` | `null` | Enables creation. Receives the trimmed query; returns the new item, or `null` to abort. |
| `CreateLabel` | `Func<string, string>?` | `null` | Text of the create option from the query; `null` gives `Create "{query}"`. |
| `EmptyText` | `string` | `"No results"` | Text of an open list without options. |
| `LoadingText` | `string` | `"Searching…"` | Text while a provider search runs and nothing is listed. |

`OnCreate`, `CreateLabel`, `SearchFunc`, `ItemsProvider` and `DisplayText` are
`Func`s rather than `EventCallback`s because they return a value the component
needs (conventions §3 governs notification events only).

There is no separate `Class` parameter; additional attributes target the
native `input`.

## Acceptance Criteria

### Filtering and selection

- Focusing an empty, closed input runs the filter with an empty query.
- Typing sets the input text to the typed value.
- Typing clears the bound value until an option is committed.
- Typing opens the list.
- Without `ItemsProvider` and `SearchFunc`, the list shows the `Items` whose
  `ToString()` contains the query, case-insensitively.
- An empty query without `ItemsProvider` and `SearchFunc` lists all `Items`.
- With `SearchFunc`, the list shows what `SearchFunc` returns for the query and
  `Items`.
- With `ItemsProvider`, the provider is asked for the query after a debounce.
- A newer keystroke cancels a provider search that has not finished.
- Result writes and re-renders from the background filter run on the
  renderer's dispatcher.
- Each option renders `ItemTemplate` for its item when supplied.
- Without `ItemTemplate`, each option renders the item's display text.
- The display text is `DisplayText(item)` when supplied, otherwise
  `item.ToString()`.
- Clicking an option binds its item.
- Committing an option writes its display text into the input.
- Committing an option closes the list.
- An externally changed bound value replaces the input text with its display
  text.
- Clearing the input binds `null`.

### Status texts

- An open list without options and without a pending provider search shows
  `EmptyText`.
- `EmptyText` defaults to `No results`.
- An open list without options while a provider search runs shows
  `LoadingText`.
- `LoadingText` defaults to `Searching…`.
- A running provider search shows a small spinner at the input's trailing edge.

### Creating an entry

- Without `OnCreate`, no create option is rendered.
- With `OnCreate`, a non-empty trimmed query that no listed item matches
  exactly renders a create option.
- An exact match compares the item's display text and the query, both
  trimmed, ignoring case.
- Without `ItemsProvider`, `Items` count as listed for the exact-match test.
- A whitespace-only query renders no create option.
- The create option is the last option of the list.
- With no other option, the create option replaces `EmptyText`.
- The create option shows the `Plus` icon.
- The create option shows `CreateLabel(query)` as its text.
- `CreateLabel` left `null` renders `Create "{query}"`.
- The create option has `role="option"`.
- The create option's accessible name is its label text; the icon is hidden
  from assistive technology.
- Clicking the create option calls `OnCreate` with the trimmed query.
- An item returned by `OnCreate` becomes the bound value.
- An item returned by `OnCreate` writes its display text into the input.
- Creating closes the list.
- `OnCreate` returning `null` binds nothing.
- `OnCreate` returning `null` after Enter keeps the typed text.
- `OnCreate` returning `null` after Tab or an outside press resets the input
  to the bound value's text.
- A commit that is already creating does not call `OnCreate` a second time.
- `OnCreate` runs on the renderer's dispatcher.

### Committing a typed query

- With `OnCreate`, Enter without a highlighted option binds the item whose
  display text matches the query exactly.
- With `OnCreate`, Enter without a highlighted option and without an exact
  match calls `OnCreate`.
- With `OnCreate`, Tab without a highlighted option follows the same rule as
  Enter.
- With `OnCreate`, closing the list by pressing outside with a pending query
  follows the same rule as Enter.
- With `ItemsProvider`, a commit whose listed results belong to an older query
  asks the provider for the current query before deciding to create.
- A query equal to the bound value's display text is not committed again.
- Without `OnCreate`, Enter without a highlighted option binds nothing.
- Without `OnCreate`, closing the list by pressing outside resets the input to
  the bound value's text.
- Escape closes the list and resets the input to the bound value's text.

### Keyboard

- ArrowDown on a closed list opens it and runs the filter for the input text.
- ArrowDown in an open list moves the highlight to the next option, stopping at
  the last.
- ArrowUp moves the highlight to the previous option, stopping at the first.
- Arrow navigation includes the create option.
- Arrow navigation requests the highlighted option be scrolled into view.
- Enter in an open list commits the highlighted option.
- Tab in an open list commits the highlighted option before closing the list.
- Enter or Tab on a highlighted create option creates.
- Hovering an option moves the highlight to it.
- Keyboard focus stays on the input while the list is open.

### Accessibility

- The input has `role="combobox"` and `aria-autocomplete="list"`.
- `aria-expanded` reflects the open state.
- `aria-controls` names the listbox.
- The list has `role="listbox"`.
- Every option, including the create option, has `role="option"`.
- The highlighted option is named by `aria-activedescendant`.
- The option whose item is the bound value has `aria-selected="true"`.
- `EmptyText` and `LoadingText` are rendered in a polite live region.
- When `Label` is empty, `AriaLabel` is the input's `aria-label`.
- A validation message sets `aria-invalid="true"` and is referenced by
  `aria-describedby`.

### Appearance and motion

- The list surface uses `--panel-float` with `--glass-fx-float`.
- The list enters and leaves through the shared popover lifecycle.
- The highlighted option carries `.is-highlighted`.
- A highlighted or hovered option uses `--fg` over `--glass-2`.
- Option highlight changes over `--dur-fast` with `--ease-out`.
- The create option is separated from the options above it by a `--line`
  hairline.
- The create option enters and leaves through `DrylPresence` with
  `PresenceTransition.Fade` at `PresenceSpeed.Fast`.
- The create option's icon uses `--fg-dim` and turns `--fg` when the option is
  highlighted or hovered, over `--dur-fast`.
- The highlighted create option's icon springs slightly larger over
  `--dur-med` with `--ease-spring`.
- Under `prefers-reduced-motion: reduce` the icon does not scale and the
  presence enters and leaves without animation.
- The component branches on no color mode (`DESIGN-02`).

### AI mode

- `Ai` defaults to `AiState.None`.
- An explicit non-`None` `Ai` overrides an inherited scope state.
- The aura surrounds the input wrapper, not the list.
- Entering `AiState.Generated` replays the one-shot wash.
- Disposal cancels pending aura work and any running search.

## Recorded debt and limits

- The listbox's accessible name (`Suggestions`, `{Label} suggestions`) is
  English and not a parameter.
- Enter in an `EditForm` is not prevented: a browser may submit the form in
  the same keystroke that commits or creates.
- The empty and loading texts swap without `DrylPresence` (`DESIGN-12`); the
  helper and validation lines likewise.
- The default filter does not trim the query: with surrounding spaces it lists
  nothing, although the create option stays hidden for an exact match and a
  commit picks that match.
- The default filter and the selected marker (`ReferenceEquals`) use item
  identity and `ToString()`; two equal strings from different sources are not
  marked selected.
- Pressing an option moves focus off the input (the options are not
  `mousedown`-prevented); the popover keeps the list open until the click
  lands.
- `.autocomplete-option` and the status texts set a literal font size
  (`DESIGN-01`, pre-existing).
- An exiting create option stays in the DOM for its exit window and can still
  be pressed during it.
- There is no `Class` parameter; attribute splatting on the input can replace
  its classes.

## Cross-cutting evidence (`SPEC-05`)

- **Both modes:** all colors come from tokens (`--panel-float`, `--line`,
  `--line-strong`, `--glass-2`, `--fg`, `--fg-muted`, `--fg-dim`,
  `--accent-a`); no mode branch. Not checked by eye in this pass.
- **Motion:** popover entrance/exit for the list, `DrylPresence` for the
  create option, token transitions for highlight and icon, aura lifecycle.
- **Keyboard/a11y:** combobox/listbox/option roles, arrows, Enter, Tab,
  Escape, `aria-activedescendant`; bUnit tests cover the keyboard commit paths.
- **AI decision:** yes — a model may fill the field; the aura sits on the
  input like on the other fields.
- **Demo:** `DRYL.Website/Components/Examples/Autocomplete/` (static list,
  async provider, custom search, template, disabled, validation, AI). A
  creation example is still to be added there.
- **Catalog:** `DRYL.Website/Components/ComponentCatalog.cs` registers
  `DrylAutocomplete` (slug `autocomplete`, category Inputs).
- **Tests:** `tests/DRYL.Components.Tests/DrylAutocompleteTests.cs`.
