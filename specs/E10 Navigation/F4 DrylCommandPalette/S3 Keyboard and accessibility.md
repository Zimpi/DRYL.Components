# Keyboard and accessibility

## Meta
- **State:** Implemented

## Acceptance Criteria

### Opening and closing

- With `HotKey` `true`, Ctrl+K / ⌘K opens the palette from anywhere outside a
  text field (`UX-01`).
- With `HotKey` `false`, the shortcut does nothing.
- Opening moves focus into the search field.
- Escape closes the palette.
- A click on the backdrop closes the palette.
- A click inside the panel does not close it.
- Closing returns focus to the element that had it before opening, unless focus
  has meanwhile moved somewhere the palette does not own.
- While open, focus is trapped inside the panel and page scroll is locked.

### Moving through results

- The first row is focused when a result set arrives.
- Arrow Down moves focus to the next row, wrapping from the last to the first.
- Arrow Up moves focus to the previous row, wrapping from the first to the last.
- Arrow Up and Arrow Down do not move the caret of the search field.
- Tab moves focus to the first row of the next group, wrapping from the last
  group to the first (`UX-01`).
- Shift+Tab moves focus to the first row of the previous group, wrapping.
- Tab and Shift+Tab keep focus in the search field.
- In the argument view Tab moves between its fields as usual.
- The focused row scrolls into view.
- Hovering a row focuses it.
- Enter runs the focused row.
- A disabled row cannot be run.
- The focused row is marked with `--accent-line` and `--glass-2`; the change
  transitions at `--dur-fast`.

### Roles and names

- The panel is a `dialog` with `aria-modal="true"`, named by `AriaLabel` or
  `CommandPaletteTexts.AriaLabel`.
- The search field is a `combobox` that controls the result `listbox` and points
  at the focused row through `aria-activedescendant`.
- Every live row is an `option` with `aria-selected` reflecting focus.
- Every group with a header is a `group` named by its header through
  `aria-labelledby`.
- The empty state is announced politely.
- The footer hints for ↑ ↓, ↵, Tab and Esc are hidden from assistive technology
  and hidden on narrow screens.
