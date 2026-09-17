# DrylSelect

## Meta
- **State:** Implemented
- **Source:** code/DRYL.Components/Components/Inputs/DrylSelect.razor
              code/DRYL.Components/Components/Inputs/DrylSelect.razor.css

## User Story

As a Blazor developer, I want a single-choice field with model binding,
validation and keyboard selection, so that users can choose a labelled option
without leaving the shared form and AI styling system.

## Description

`DrylSelect` shows one selected label in a combobox trigger and its choices in
an anchored floating list. The trigger retains keyboard focus while arrows
move the option highlight. Selecting an option commits its string value and
closes the list. The field integrates with `EditForm` validation and also
supports simple binding outside a form.

The list uses the real `DrylPopover` surface lifecycle, including positioning,
outside dismissal and focus return. Select supplies the combobox/listbox
semantics and option keyboard behavior. AI styling belongs to the field
trigger, where AI-assisted selection is visible to the user.

This newly documented contract adds I13 H05's forced-colors focus requirement
and integration evidence for I13 H03's shared exit repair. These repairs are
implemented; unrelated existing gaps and native evidence limits remain visible.

## Public API

Namespace: `DRYL.Components`. Inherits `InputBase<string>` and implements
`IAsyncDisposable`. Inherited `Value`, `ValueChanged`, `ValueExpression`,
`DisplayName` and `AdditionalAttributes` follow [`_Api.md`](_Api.md).

| Member | Type | Default | Purpose |
|---|---|---|---|
| `Label` | `string?` | `null` | Visible label above the trigger. |
| `Placeholder` | `string?` | `null` | Fallback trigger text when no matching item exists. |
| `HelperText` | `string?` | `null` | Text below the field, replaced by validation messages. |
| `State` | `InputState` | `InputState.Default` | Visual state override. |
| `Disabled` | `bool` | `false` | Disabled trigger styling and removal from the tab order. |
| `AriaLabel` | `string?` | `null` | Accessible name applied when `Label` is empty. |
| `Ai` | `AiState` | `AiState.None` | AI state, with scope inheritance. |
| `Aura` | `AiAura?` | `null` | Aura variant, with scope inheritance. |
| `Items` | `IEnumerable<SelectItem>?` | `null` | Ordered options whose values can be selected. |

`SelectItem` and `InputState` are defined in [`_Api.md`](_Api.md). There is no
public open-state parameter, item template, multiple-selection mode, search,
clear button, content slot or separate `Class` parameter. Additional attributes
target the trigger `div`, not the outer field or listbox. This is a custom
combobox, not a native `select` element.

## Acceptance Criteria

### Options, value and structure

- The field renders a trigger carrying `.select`.
- A nonempty `Label` is displayed above the trigger.
- An empty `Label` renders no visible label.
- Options preserve the enumeration order of `Items`.
- Updating `Items` refreshes the available option list on parameter application.
- Null `Items` is treated as an empty list.
- An open empty list displays `No options`.
- Each option displays its `SelectItem.Label` as text.
- The trigger displays the first item label whose value matches the bound value.
- Without a matching item, the trigger displays `Placeholder` when supplied.
- Without a matching item or placeholder, the trigger text is empty.
- A null or empty bound value applies the trigger's placeholder class.
- A nonempty unmatched bound value does not apply the placeholder class.
- Clicking an enabled trigger toggles the list's open state.
- Clicking an option sets the bound value to its `SelectItem.Value`.
- Selecting a different value invokes `ValueChanged` with that `SelectItem.Value`.
- Committing a changed value notifies the associated form field when present.
- Committing an option closes the list.
- Committing an option clears the highlighted index.
- String parsing normalizes a null parsed value to `string.Empty`.
- `AdditionalAttributes` are applied to the trigger.

### Validation and disabled state

- A field validation message applies `.input-error` regardless of `State`.
- Without a validation message, `InputState.Error` applies `.input-error`.
- Without a validation message, `InputState.Success` applies `.input-success`.
- `InputState.Default` adds no state modifier without a validation message.
- Field validation messages replace `HelperText`.
- Multiple field validation messages are displayed joined by a space.
- With no validation message, nonempty `HelperText` is displayed below the trigger.
- Without a validation message or helper text, no helper element is rendered.
- `Disabled` adds `.select--disabled` to the trigger.
- A disabled trigger is removed from sequential keyboard focus.
- Clicking the disabled trigger does not toggle the list.

### Keyboard selection

- An enabled trigger is reachable through Tab and Shift+Tab in DOM order.
- ArrowDown on a closed nonempty list opens it at the selected option when one exists.
- ArrowDown on a closed nonempty list without a selection highlights its first option.
- ArrowUp on a closed nonempty list opens it at the selected option when one exists.
- ArrowUp on a closed nonempty list without a selection highlights its last option.
- ArrowDown in an open list advances the highlight without wrapping past its end.
- ArrowUp in an open list moves the highlight backward without wrapping past its start.
- Arrow navigation in an already open list requests the highlighted option be
  scrolled nearest into view.
- Enter on a closed nonempty list opens it at the selected option, or the first
  option if there is no selection.
- Space on a closed nonempty list opens it at the selected option, or the first
  option if there is no selection.
- Enter in an open list commits a valid highlighted option.
- Space in an open list commits a valid highlighted option.
- Escape closes the list without changing the bound value.
- Tab in an open list commits a valid highlighted option before closing it.
- Shift+Tab in an open list follows the same commit rule as Tab.
- Tab and Shift+Tab retain the browser's ordinary movement to adjacent controls.
- Hovering an option moves the highlight to that option.
- An option's pointer press does not take keyboard focus from the trigger.

### Accessibility and focus

- The trigger has `role="combobox"`.
- The trigger has `aria-haspopup="listbox"`.
- `aria-expanded` reflects the requested open state.
- `aria-controls` names the generated listbox ID.
- The open option list has `role="listbox"`.
- The listbox name is `Options` without a visible `Label`.
- With a visible `Label`, the listbox name is that label followed by ` options`.
- Every option has `role="option"`.
- An option whose value equals the bound value has `aria-selected="true"`.
- A valid highlighted option is named by `aria-activedescendant`.
- When `Label` is empty, `AriaLabel` is applied to the trigger as `aria-label`.
- When `Label` is nonempty, the component's `AriaLabel` is omitted.
- A field validation message sets `aria-invalid="true"`.
- A manual `InputState.Error` alone does not set `aria-invalid`.
- A displayed helper or validation message is referenced by `aria-describedby`.
- Opening the list leaves keyboard focus on the trigger during normal selection.
- Escape or option selection leaves focus on the trigger during normal selection.
- Outside dismissal does not pull focus away from the control the user clicked.
- A popover close returns focus from inside its panel to the saved trigger when
  that trigger is still connected, following the shared popover focus contract.
- Under `forced-colors: active`, an enabled `.select:focus-visible` has a
  visible outline when box shadows are removed (I13 H05, `UX-02`).
- The surviving outline is verified on the real non-native trigger with its
  popover open as well as closed.
- The forced-colors fallback preserves the ordinary keyboard focus order.
- The focus fallback introduces no new design token or focus dimension.

### Appearance and motion

- The trigger uses shared `.select` styling without a component color-mode branch.
- The base trigger surface uses `--glass-1`.
- Trigger text uses `--fg`.
- Placeholder text uses `--fg-dim`.
- The default border uses `--line-strong`.
- Normal focus retains the existing accent border and glow treatment.
- Manual and validation error states retain their existing semantic focus treatment.
- The open trigger has `.select--open`.
- The open panel matches the trigger width through `DrylPopover`.
- The floating list surface uses `--panel-float` with `--glass-fx-float`.
- The list's border, radius and shadow use `--line-strong`, `--r-md` and `--shadow-lg`.
- Highlighted option text uses `--fg` over `--glass-2`.
- A selected option displays the existing small accent dot.
- Option hover/highlight transitions use `--dur-fast` with `--ease-out`.
- The panel enters through the shared popover's entrance behavior.
- The panel content remains mounted for the shared popover's exit window.
- An exiting panel does not intercept pointer actions aimed through it.
- Closing under reduced motion removes the panel through the shared exit path.
- Reopening during exit prevents a stale exit completion from closing the new
  open list (I13 H03).
- Disposal removes the composed popover's portal and owned listeners.

### AI mode and cleanup

- `Ai` defaults to `AiState.None` (`AI-03`).
- An explicit non-`None` `Ai` overrides an inherited scope state.
- With `AiState.None`, the field inherits a surrounding scope state when present.
- Without an active scope or explicit AI state, a settled field has no aura.
- An explicit `Aura` overrides the inherited aura variant.
- The aura variant defaults to `AiAura.Comet` when neither field nor scope sets it.
- The shared aura surrounds the trigger wrapper rather than the floating list.
- Effective `Generated` entry replays the shared one-shot wash.
- A generated aura retires without requiring the consumer to reset `Ai`.
- Re-rendering a spent `Generated` state does not replay it.
- Returning to live AI work cancels a pending aura exit.
- Aura decoration does not change the bound value or keyboard order.
- Aura elements are hidden from assistive technology (`UX-07`).
- Reduced motion leaves selection usable with the shared aura at a static end
  state (I13 H03, `UX-06`).
- Disposal cancels pending aura lifecycle work.
- Disposal detaches a successfully attached trigger navigation-key listener.
- Navigation-key teardown tolerates a disconnected circuit or already-removed listener.

## Recorded debt and limits

- The visible label uses `for` pointing to a `div`, which is not a native
  labelable element. There is no `aria-labelledby` linking that label to the
  combobox, and `AriaLabel` is suppressed when `Label` is present. A visible
  label therefore does not establish the trigger's accessible name (`UX-01`).
  This pre-existing naming repair is outside H05's outline scope.
- Disabled styling uses `tabindex="-1"` and pointer suppression but sets no
  `aria-disabled`. `HandleKeyDown` and `SelectItem` do not guard `Disabled`;
  disabling an already-focused/open Select does not fully block keyboard or
  option selection (`UX-01`).
- `ActiveDescendant` is based only on a nonnegative highlight. Escape/outside
  close can leave it naming an unmounted option; an empty list or changed
  `Items` can leave it out of range. Click-open with no selection leaves no
  highlight, so Enter requires an arrow or pointer highlight before committing.
- Duplicate values are not rejected: every matching option reports selected,
  while the trigger displays only the first matching label. An empty-string
  option can have a displayed label but retain placeholder styling.
- `dryl.keynav` suppresses Home/End and horizontal arrows although Select
  implements no action for them. Space can activate the field and still scroll
  the page. There is no typeahead. These are existing keyboard limits, not
  behavior added to I13.
- `.select:focus-visible` suppresses the ordinary outline in favor of its glow; H05 now
  restores the outline under forced colors. Native inspection limits are below.
- `DrylSelect.razor.css` retains literal open-state shadow geometry and
  scrollbar radius; shared `.select` styling retains literal padding and a
  mode-assuming SVG arrow color (`DESIGN-01`, `DESIGN-02`). I13 changes no
  arrow or token design.
- The field root has no enter/exit animation. Conditional labels and helper
  messages have no `DrylPresence` (`DESIGN-11`, `DESIGN-12`). The panel already
  uses the shared popover's surface lifecycle; it is not a second missing exit.
- Option color/background transitions have no isolated reduced-motion override.
  I13 H03 repairs the named shared aura/exit selectors, not all input transitions.
- AI state has no built-in live announcement (`UX-04`). Additional attributes
  can replace the generated ID, classes or ARIA relationships; they are not
  merged as a separate `Class` parameter would be.
- First key-handler attachment catches only `JSDisconnectedException`, has no
  later retry, and option scrolling has no JS exception guard. See
  [`_Interop.md`](_Interop.md); H05 does not broaden these lifecycle contracts.

## Cross-cutting evidence (`SPEC-05`)

- **Both modes:** source inspection covers shared `.select` and isolated
  `.select-panel`/`.select-option` token use. The arrow literal is recorded
  above. Actual open and closed dark/light rendering remains to be checked;
  token synchronization does not establish the composed control's appearance.
- **Motion:** `DrylPopover` owns the real surface entrance and exit; the
  isolated option rules own hover/highlight transitions; `AuraLifecycle` and
  `DrylAuraElements` own the trigger aura. The root/helper gaps are explicit.
  Normal/reduced-motion browser evidence must cover actual portal removal,
  rapid close/reopen and shared exit completion, including preference changes.
- **Keyboard/a11y:** `HandleKeyDown`, trigger/listbox/option attributes and
  `dryl.keynav` were read. H05's live scene must use the real popover, exercise
  Tab/Shift+Tab, arrows, Enter, Space, Escape, outside dismissal and focus
  return in both modes, then assert the surviving forced-colors outline.
  A Windows contrast-theme pass supplements emulation. These are pending
  checks; this contract does not claim a complete accessibility audit.
- **AI decision:** explicitly yes, because a model may propose or populate a
  field choice. It uses the same optional state/variant vocabulary as the text
  fields; the list itself does not gain an independent aura.
- **Demo:** source verified in `DRYL.Website/Components/Pages/DemoForm.razor`,
  route `/components/forms`, with a bound Environment Select in
  `DRYL.Website/Components/Examples/Form/Controls.razor`. Further use exists in
  `Components/Pages/Overview.razor` and `Components/Examples/Table/Editing.razor`.
  These examples do not demonstrate the complete disabled, empty, validation
  and AI matrix (`CODE-20`).
- **Catalog:** source verified in `DRYL.Website/Components/ComponentCatalog.cs`
  through the shared `"Form Controls"` / `forms` entry. Its description names
  select, but `ClassName` is null and its AI flag is `false`; it is shared
  navigation rather than an individual `DrylSelect` registration (`REL-04`).
- **Tests:** no dedicated `DrylSelect` unit-test suite was found in
  `tests/DRYL.Components.Tests` during this contract pass. Existing popover
  unit tests do not execute `dryl.js` or prove Select focus return. I13's browser
  host must load actual global and generated isolated CSS; bUnit-only or static
  markup substitutes cannot prove the forced-colors/portal result.

## I13 focus verification — 2026-09-17

`tests/DRYL.BrowserTests/FocusTests.cs` exercises Tab/Shift+Tab, Stepper
activation, text and multiline input, the actual Select popover with arrows,
Enter/Escape and focus return, and dialog close/return in both modes.
Ordinary keyboard cases pass in Chromium, Firefox and WebKit; Chromium and
installed Chrome/Edge also pass forced-colors outline assertions. Shared
controls and Stepper headers use existing `--accent-b` with browser-defined
outline geometry. Normal focus styles remain unchanged. Visual inspection
and final counts are recorded in `docs/2026-09-15-i13-implementation-plan.md`.

This supersedes the earlier pending-I13 verification wording. A native Windows
contrast-theme inspection and native Safari/Firefox smoke remain outstanding;
forced-colors emulation is not claimed as that native evidence. No full-library
accessibility audit or repair of the unrelated recorded debt is claimed.
