# DrylFormField

## Meta
- **State:** Implemented
- **Source:** code/DRYL.Components/Components/Inputs/DrylFormField.razor
              code/DRYL.Components/Components/Inputs/DrylFormField.razor.css

## User Story

As a Blazor developer laying out a form, I want one wrapper that gives any
input a consistent label, a required marker and a hint that steps aside while
the field is invalid, so that my form fields look alike without each input
carrying its own label and hint.

## Description

`DrylFormField<TValue>` wraps arbitrary input content in a `.field` column: a
label (with an optional required asterisk) above, the `ChildContent` in the
middle, a hint below. It renders no validation message itself — DRYL inputs
render their own — but with `For` bound inside an `EditForm` it hides the hint
while that field has validation messages, so hint and error never show at once.

`TValue` is the type of the field `For` points at; it is usually inferred from
the expression.

## Public API

Namespace: `DRYL.Components`. Generic `TValue`.

| Member | Type | Default | Purpose |
|---|---|---|---|
| `Label` | `string?` | `null` | Label above the content. |
| `Hint` | `string?` | `null` | Helper text below the content; hidden while `For` has validation messages. |
| `Required` | `bool` | `false` | Appends a required marker to the label. |
| `For` | `Expression<Func<TValue>>?` | `null` | Identifies the bound field for the hint's error check. |
| `ChildContent` | `RenderFragment?` | `null` | The input. |
| `AdditionalAttributes` | `IDictionary<string, object>?` | `null` | Unmatched attributes, applied to the root. |
| `Class` | `string?` | `null` | Extra classes merged onto the `.field` root. |

## Acceptance Criteria

### Structure

- The root is a `div` carrying `.field` and `Class`.
- A `class` attribute is merged into the root's classes through `Class`, keeping `.field`.
- `AdditionalAttributes` are applied to the root.
- A nonempty `Label` renders a `label` carrying `.field-label` with the label text.
- An empty `Label` renders no label element.
- `ChildContent` renders between the label and the hint.

### Required marker

- With `Required` and a nonempty `Label`, the label ends in a `.field-required` span reading `*`.
- The required marker carries `aria-hidden="true"`.
- The required marker is colored `--accent-a`.
- Without `Required`, no marker is rendered.

### Hint

- A nonempty `Hint` renders an `.input-helper` after `ChildContent`.
- Inside an `EditContext`, the hint is not rendered while the field identified by `For` has validation messages.
- Without `For`, the hint is rendered regardless of validation state.
- Without a cascading `EditContext`, the hint is rendered regardless of `For`.
- The component renders no validation message of its own.

## Recorded debt

- **The label is not associated with the input.** The `label` has no `for`, so
  the wrapped input gets no accessible name from it unless the consumer adds one
  (`UX-01`).
- **`Required` is visual only.** The asterisk is hidden from assistive
  technology and no `aria-required` reaches the input.
- **The hint is not linked** to the input through `aria-describedby`.
- **No validation subscription.** The error check runs only when the wrapper
  renders; it does not listen to `OnValidationStateChanged`.
- **The hint snaps in and out** — it mounts and unmounts without `DrylPresence`
  (`DESIGN-12`).
- **Literal margin** — the marker's left margin in the `.field-required` rule of
  `DrylFormField.razor.css` (`DESIGN-01`).

## Cross-cutting evidence (`SPEC-05`)

- **Both color modes** — mode tokens only; inspected on `/components/form-field` in dark and light (2026-11-20)
- **Enter/exit animation** — **no** enter or exit of its own: a static layout
  wrapper its host mounts. The snapping hint is recorded above.
- **Keyboard and a11y** — adds no interactive element; keyboard behaviour is the
  wrapped input's. The missing label association and `aria-required` are
  recorded above.
- **AI mode** — **no**: the wrapper holds no content of its own; the wrapped
  input carries its own `Ai` (`AI-05`).
- **Demo page** — `/components/form-field`
  (`DRYL.Website/Components/Pages/DemoFormField.razor`), with the example
  `Components/Examples/FormField/ValidatedForm.razor`.
- **`ComponentCatalog`** — `"Form Field"` / `form-field`, group `Inputs`,
  `ClassName` `"DrylFormField"`, AI flag `false` (`REL-04`).
- **Tests** — `tests/DRYL.Components.Tests/ClassMergeTests.cs`
  (`FormField_merges_consumer_class_without_clobbering`) covers the class merge;
  the hint criteria have no test of their own.
