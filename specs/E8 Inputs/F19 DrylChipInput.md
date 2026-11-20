# DrylChipInput

## Meta
- **State:** Implemented
- **Source:** code/DRYL.Components/Components/Inputs/DrylChipInput.razor
              code/DRYL.Components/Components/Inputs/DrylChipInput.razor.css

## User Story

As a Blazor developer, I want a free-text tag field that turns what the user
types into removable chips and hands me the list, so that I can collect labels
or keywords with `@bind-Tags` without building chip entry myself.

## Description

`DrylChipInput` is a plain component, not an `InputBase`: it binds a list of
strings through `Tags` / `TagsChanged`. The chips and a borderless text input
sit together in one field-styled box. `Enter`, or a trailing comma, commits
the typed text as a tag; `Backspace` on an empty input removes the last tag;
each chip carries a small remove button. Whitespace-only text and duplicates
(compared case-insensitively) are dropped, and `MaxTags` caps the list.

The component never changes `Tags` itself — every change is reported as a new
list through `TagsChanged`, and the consumer's binding writes it back.

The box can show the shared AI aura. Only the explicit `Ai` drives it; the
aura variant follows a surrounding `DrylAiScope`, the state does not.

## Public API

Namespace: `DRYL.Components`. Implements `IDisposable`. Binding contract in
[`_Api.md`](_Api.md).

| Member | Type | Default | Purpose |
|---|---|---|---|
| `Tags` | `IReadOnlyList<string>` | `[]` | The current tags; bind with `@bind-Tags`. |
| `TagsChanged` | `EventCallback<IReadOnlyList<string>>` | — | Raised with the new list on every change. |
| `Label` | `string?` | `null` | Visible label above the box. |
| `Placeholder` | `string?` | `null` | Placeholder while there are no tags; falls back to `Add tag…`. |
| `HelperText` | `string?` | `null` | Text below the box. |
| `State` | `InputState` | `InputState.Default` | Visual state; `Error` also styles the helper as an error. |
| `Disabled` | `bool` | `false` | No tags can be added or removed. |
| `MaxTags` | `int` | `0` | Maximum number of tags; `0` means unlimited. |
| `AriaLabel` | `string?` | `null` | Accessible name when `Label` is empty. |
| `Ai` | `AiState` | `AiState.None` | AI state of the box's aura. |
| `Aura` | `AiAura?` | `null` | Aura variant; inherits a surrounding `DrylAiScope`. |
| `AdditionalAttributes` | `IDictionary<string, object>?` | `null` | Unmatched attributes, applied to the inner text input. |
| `Class` | `string?` | `null` | Extra classes merged onto the `.field` root. |

## Acceptance Criteria

### Structure

- The root is a `div` carrying `.field` and `Class`.
- A nonempty `Label` renders a `.field-label` associated with the inner input's generated `id`.
- The box is a `.chip-input-inner` inside an `.input-wrapper`.
- Each tag renders as a `.chip` holding a `.chip-text` span with the tag text.
- Tags render in list order, before the text input.
- The text input is an `input type="text"` carrying `.chip-input-field`.
- The text input carries `data-dryl-input`.
- `AdditionalAttributes` are applied to the text input.
- A `class` attribute is merged into the root's classes through `Class`.

### Placeholder

- While `Tags` is empty, the input shows `Placeholder`.
- While `Tags` is empty and `Placeholder` is unset, the input shows `Add tag…`.
- While at least one tag exists, the input has no placeholder.

### Adding tags

- `Enter` commits the trimmed typed text as a new tag.
- Typed text ending in a comma commits the text before the comma as a new tag.
- A commit invokes `TagsChanged` with the current tags plus the new one appended.
- A commit clears the typed text.
- Whitespace-only text is discarded without invoking `TagsChanged`.
- Text equal to an existing tag, ignoring case, is discarded without invoking `TagsChanged`.
- With `MaxTags` greater than 0 and reached, text is discarded without invoking `TagsChanged`.
- With `MaxTags` greater than 0 and reached, the text input is disabled.

### Removing tags

- While enabled, each chip renders a `button type="button"` carrying `.chip-remove`.
- The remove button carries `aria-label="Remove {tag}"`.
- The remove button carries `tabindex="-1"`.
- Activating a remove button invokes `TagsChanged` with every tag except that one.
- A click on a remove button does not propagate to the box.
- `Backspace` on an empty text input invokes `TagsChanged` without the last tag.
- `Backspace` with typed text leaves the tags unchanged.

### Disabled and state

- `Disabled` disables the text input.
- `Disabled` renders no remove buttons.
- `Disabled` adds `.chip-input-inner--disabled`.
- `InputState.Error` adds `.chip-input-inner--error`.
- `InputState.Success` adds `.chip-input-inner--success`.
- A nonempty `HelperText` renders an `.input-helper` below the box.
- With `InputState.Error`, the helper also carries `.input-helper-error`.
- Without `HelperText`, no helper is rendered.
- A click anywhere in the box focuses the text input, unless the input is disabled.

### Appearance and motion

- The box reads `--glass-1` at rest.
- The box has a `--line-strong` border at rest.
- The box has the `--r-md` radius.
- A hovered, enabled box turns its border `--line-hover`.
- A box with focus inside turns its border `--accent-line`.
- A box with focus inside reads `--glass-2`.
- A box with focus inside carries a glow ring mixed from `--accent-line` and `--accent-a`.
- An error box has a `--danger` border.
- A success box has a `--success` border.
- Border, background and shadow transition on `--dur-med` / `--ease-out`.
- The text input's caret uses `--accent-a`.
- The placeholder reads `--fg-dim`.
- Chip text reads `--fg-muted` on `--glass-2`.
- Chips have a `--line` border and the `--r-sm` radius.
- The stylesheet branches on no colour mode (`DESIGN-02`).

### Keyboard and accessibility

- The text input is reachable through Tab while enabled.
- When `Label` is empty, `AriaLabel` is applied as the text input's `aria-label`.
- When `Label` is nonempty, `AriaLabel` is omitted.

### AI mode

- `Ai` defaults to `AiState.None`.
- A non-`None` `Ai` renders the shared aura on the `.input-wrapper` through `DrylAuraElements`.
- An unset `Aura` inherits the surrounding `DrylAiScope`'s variant, falling back to `AiAura.Comet`.
- Disposing the component disposes its `AuraLifecycle`.

## Recorded debt

- **`Ai` does not follow a surrounding `DrylAiScope`'s state.** Only the aura
  variant is resolved from the scope; listed as an I32 follow-up in
  `tests/DRYL.Components.Tests/AiScopeInheritanceTests.cs`.
- **Chips appear and disappear instantly.** Each chip mounts and unmounts
  without `DrylPresence` (`DESIGN-11`, `DESIGN-12`).
- **Remove buttons are outside the tab order and have no `DrylTooltip`.** Only
  the last tag is removable by keyboard, through `Backspace` (`UX-01`, `UX-05`).
- **No form integration.** Not an `InputBase`: no `EditContext` field
  notification, no validation messages, no `aria-invalid`; `InputState.Error`
  is visual only.
- **The helper is not linked** — no `aria-describedby` from the input; the
  chips are not exposed as a list.
- **`Enter` keeps its browser default**, so inside a form it can also trigger
  implicit submission.
- **Removal is case-sensitive while the duplicate check is not**, and removing
  a tag removes every exactly equal entry.
- **Literal values** — the box's minimum height, the text input's minimum width,
  height and type, the focus/error/success glow geometry and the disabled
  opacity in `DrylChipInput.razor.css`; the `.chip` rule's padding, type size
  and maximum width in `dryl.css`; the icon size passed to `DrylIcon`
  (`DESIGN-01`).
- **Fixed English strings** — `Add tag…`, `Remove {tag}`.

## Cross-cutting evidence (`SPEC-05`)

- **Both color modes** — mode tokens only; inspected on `/components/chip-input` in dark and light (2026-11-20)
- **Enter/exit animation** — **no** enter or exit of its own: an in-flow field
  its host mounts. Border, background and focus glow are transitioned; the
  aura has its own lifecycle. The instant chip mount/unmount is recorded above.
- **Keyboard and a11y** — the text input is a native tab stop; `Enter`, comma
  and `Backspace` edit the list. The unreachable remove buttons and missing
  helper link are recorded above.
- **AI mode** — **yes**: a model may suggest tags, so the box carries `Ai` and
  `Aura` (`AI-05`).
- **Demo page** — `/components/chip-input`
  (`DRYL.Website/Components/Pages/DemoChipInput.razor`), with the examples
  `Components/Examples/ChipInput/Basics.razor`, `MaxTags.razor`,
  `States.razor`, `AiMode.razor` and `LivePreview.razor`.
- **`ComponentCatalog`** — `"Chip Input"` / `chip-input`, group `Inputs`,
  `ClassName` `"DrylChipInput"`, AI flag `true` (`REL-04`).
- **Tests** — no dedicated bUnit suite; the component appears only in the
  aura-only allowlist of `tests/DRYL.Components.Tests/AiScopeInheritanceTests.cs`.
