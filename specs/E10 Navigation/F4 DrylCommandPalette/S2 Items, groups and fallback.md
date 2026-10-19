# Items, groups and fallback

## Meta
- **State:** Implemented

## Acceptance Criteria

### Sources

- Without `SearchProvider`, `Items` is filtered case-insensitively by
  `CommandItem.Label`, `CommandItem.Description` and `CommandItem.Category`.
- With `SearchProvider`, `Items` is ignored.
- The provider's items are shown in the order the provider returns them within
  their group; the palette does not rank or filter them.
- Registry commands are matched by the palette and rendered after the items,
  capped at `MaxResults`.
- Opening the palette asks for the empty query.

### Debounce

- A non-empty query reaches `SearchProvider` only after it has rested for
  `Debounce`.
- `Debounce` defaults to 250 milliseconds.
- The empty query reaches `SearchProvider` at once.
- A query superseded while it waits or loads never replaces the rows of the
  newer query.

### Keys

- A row is keyed by `CommandItem.Id` when it is set.
- An item without `Id` is keyed by its `Category` and `Label`.
- Two rows with the same key in one result set both render, each with its own
  key.
- A registry command is keyed by its `DrylCommand.ResolvedId`.

### Row content

- A row renders its `Icon`, `Label` and `Description`.
- A row with `Meta` renders it muted at the trailing end, in `--fg-dim`, before
  the trailing arrow.
- A `CommandItemType.Navigate` row ends in an arrow.
- A `CommandItemType.AiIntent` row ends in a sparkle in `--accent-a`.

### Group order

- `GroupOrder` defaults to `CommandGroupOrder.Alphabetical`.
- Under `CommandGroupOrder.Alphabetical` groups are sorted by name,
  case-insensitively, and rows without a `Category` come last without a header.
- Under `CommandGroupOrder.AsProvided` groups appear in the order their first row
  arrives.
- Under `CommandGroupOrder.AsProvided` rows without a `Category` form a group
  without a header at the place their first row arrives.

### Fallback

- For a non-blank query the palette asks `Fallback` for one entry.
- A `null` from `Fallback` shows no fallback for that query.
- The empty query shows no fallback.
- While other rows match, the fallback is the last row.
- When no other row matches, the fallback is the first row and is focused.
- The fallback follows the query as it is typed, without waiting for
  `Debounce` or the provider.
- The fallback keeps one key across queries, so its row stays and only its words
  change.
- A fallback with a `Category` renders under that header.

### Running items

- Running a `CommandItemType.Navigate` item closes the palette and navigates to
  its `Href`.
- Running a `CommandItemType.Action` item closes the palette and invokes its
  `Action`.
- Running a `CommandItemType.AiIntent` item keeps the palette open and invokes
  its `AiAction`.
- `OnItemRun` fires with the item before its navigation or action, for provider
  items, static items and the fallback alike.
- `OnItemRun` does not fire for registry commands, which report through their
  own `OnRun`.
- The palette stores no recent items of its own.
