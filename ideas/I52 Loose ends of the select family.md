# Loose ends of the select family

## Meta
- **State:** Adopted
- **Specs:** [`specs/E8 Inputs/F6 DrylMultiSelect.md`](../specs/E8%20Inputs/F6%20DrylMultiSelect.md),
  [`specs/E8 Inputs/F3 DrylSelect.md`](../specs/E8%20Inputs/F3%20DrylSelect.md),
  [`specs/E8 Inputs/_Api.md`](../specs/E8%20Inputs/_Api.md)

## Problem

Bringing the searchable multi-select (I51) into `release/3.17.0` left five
things written down as debt or noticed in review. The Product Owner asked on
2026-11-20 to close them before the release ("Zieh auch diese Dinge gerade"):

1. Without `Searchable`, `Home` and `End` do nothing in `DrylMultiSelect`,
   although `dryl.keynav` suppresses their default.
2. `ArrowUp` before any highlight does nothing in `DrylMultiSelect`.
3. The option-list styles exist twice, once in each component's scoped
   stylesheet, and the two copies have drifted.
4. `DrylMultiSelect` takes no part in an `EditForm`: a validation message for
   the bound list is never shown.
5. `DrylSelect` renders two fixed English texts: the list's accessible name
   (`Options` / `{Label} options`) and the `No options` row.

Target role: the Blazor developer who puts either control into a validated,
translated form; the keyboard user of the multi-select.

## Solution Idea

- **Keyboard.** In `DrylMultiSelect`, `Home` and `End` move the highlight to
  the first and the last option of an open list. `ArrowUp` on a closed control
  opens it with the last option highlighted; on an open list without a
  highlight it starts at the last option. In search mode `Home` and `End` stay
  with the caret, and `ArrowUp` follows the same rule over the listed options.
- **One list surface.** The panel, the option row and the empty row move to
  `dryl.css` as one block; `DrylSelect.razor.css` keeps its accent dot,
  `DrylMultiSelect.razor.css` its picked tint, its checkbox column and the
  accent edge of its panel. No value changes.
- **Form integration without a new base class.** `DrylMultiSelect` stays what
  it is and gains `SelectedValuesExpression`, which `@bind-SelectedValues`
  supplies by itself. With it and a cascaded `EditContext`, every change is
  reported through `NotifyFieldChanged`, and a validation message for the field
  behaves as on `DrylSelect`: `.input-error`, `aria-invalid`, the message in
  place of `HelperText`, precedence over `State`.
- **`SelectTexts`.** `DrylSelect.Texts`, the pattern of `MultiSelectTexts` and
  `CommandPaletteTexts`, with `NoOptions` and `ListLabel`.

## Scope
- **In scope:** the five points above, their specs, tests and a form example
  on the multi-select demo page.
- **Out of scope:** making `DrylMultiSelect` an `InputBase` (it would rename
  the binding and break every consumer); the keyboard limits `DrylSelect`
  records for itself (`Home`/`End`, typeahead); `DrylAutocomplete`'s texts.

## Impact
- **Harness:** none. No new token, keyframe, `AiState` or dependency. The
  shared block in `dryl.css` carries the same declarations the two scoped
  files carried, including their pre-existing literals.
- **Specs:** `F6 DrylMultiSelect` and `F3 DrylSelect` change; `_Api.md` gains
  `SelectTexts` and the multi-select's binding contract.
- **Public API (additive → MINOR, fits 3.17.0):**
  `DrylMultiSelect.SelectedValuesExpression`, `DrylSelect.Texts`, `SelectTexts`.
- **Behaviour a consumer can notice:** a multi-select bound with
  `@bind-SelectedValues` inside an `EditForm` now marks its field as modified
  and shows that field's validation messages. Before, it did neither.

## Decisions
- 2026-11-20: All five are closed for 3.17.0 (Product Owner).
- 2026-11-20: Form integration is additive through the expression parameter,
  not through `InputBase` (Tech Lead). A field without the expression, or
  outside a form, behaves exactly as before.
- 2026-11-20: The shared styles live in `dryl.css` rather than in a shared
  child component, because both lists are markup of their own component and
  only the declarations are common (Tech Lead).
- 2026-11-20: `Home`/`End` on a closed multi-select do nothing; they do not
  open the list (Tech Lead).

## Open Points

*(none)*
