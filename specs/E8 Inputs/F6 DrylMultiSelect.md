# DrylMultiSelect

## Meta
- **State:** Implemented
- **Source:** code/DRYL.Components/Components/Inputs/DrylMultiSelect.razor
              code/DRYL.Components/Components/Inputs/DrylMultiSelect.razor.css

## User Story

As a Blazor developer building a form on DRYL, I want a dropdown that lets the
user pick any number of options, shows what is picked as removable chips and
stays open while they pick, so that I can bind a list of values without
building a listbox, its keyboard model and its chip row myself.

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
- **Keyboard and a11y** — the "Keyboard" criteria above; read off
  `HandleKeyDown`. The ink follows `.is-highlighted`, so it moves with the
  keyboard exactly as `aria-activedescendant` does.
- **AI mode** — **yes**: a model may propose or fill a multi-value field, so
  the field carries `Ai` and `Aura` like the other inputs (`AI-05`).
- **Demo page** — `/components/multiselect` in `DRYL.Website`, built from the
  `Basics`, `Overflow`, `States` and `AiMode` examples under
  `Components/Examples/MultiSelect/`.
- **`ComponentCatalog`** — registered as `"Multi Select"` / `multiselect`,
  group `Inputs`, `ClassName` `"DrylMultiSelect"`, AI flag `true` (`REL-04`).
- **Tests** — `tests/DRYL.Components.Tests/ListGlideTests.cs` covers the ink
  host marker; the ink's behaviour is covered in `tests/js/glide.test.mjs`.

## Recorded debt (`State: Implemented`)

**Deviations from the acceptance criteria above: none.**

- **`Home` and `End` do nothing**, although `dryl.keynav` suppresses their
  default; `DrylSelect` handles both.
- **`ArrowUp` before any highlight does nothing** rather than starting at the
  last option.
- **Fixed English strings** — `Select options…`, `No options`, `Options` /
  `{Label} options` and `Remove {label}` are not translatable.
- **Option styles are a copy of `DrylSelect`'s**, because scoped CSS does not
  cross components; the two copies have already drifted (the picked tint exists
  only here). A shared rule in `dryl.css` would end that.
- **No `EditForm` integration** — the control is not an `InputBase`, so a
  validation message for the bound list is never shown.
