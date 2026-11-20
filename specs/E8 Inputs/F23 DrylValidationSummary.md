# DrylValidationSummary

## Meta
- **State:** Implemented
- **Source:** code/DRYL.Components/Components/Inputs/DrylValidationSummary.razor
              code/DRYL.Components/Components/Inputs/DrylValidationSummary.razor.css

## User Story

As a Blazor developer, I want one banner near the top of an `EditForm` that
lists every current validation message, so that a user who submits an invalid
form sees at a glance everything that needs fixing.

## Description

`DrylValidationSummary` reads the cascading `EditContext` and subscribes to its
`OnValidationStateChanged`. While the form has validation messages, it renders
a danger-tinted glass banner with an alert icon, an optional title and a list of
the distinct messages; with none, it renders nothing. Outside an `EditForm` it
never renders.

## Public API

Namespace: `DRYL.Components`. Implements `IDisposable`.

| Member | Type | Default | Purpose |
|---|---|---|---|
| `Title` | `string?` | `"Please fix the following:"` | Heading above the list; empty hides it. |
| `AdditionalAttributes` | `IDictionary<string, object>?` | `null` | Unmatched attributes, applied to the banner. |
| `Class` | `string?` | `null` | Extra classes merged onto the banner. |

## Acceptance Criteria

### Messages

- Without a cascading `EditContext`, the component renders nothing.
- While the `EditContext` has no validation messages, the component renders nothing.
- While it has validation messages, the component renders one `div` carrying `.validation-summary`.
- The banner lists every validation message of the `EditContext` as one `li` in `.validation-summary-list`.
- A message that occurs more than once is listed once.
- A change of the validation state re-reads the messages and re-renders.
- A new cascading `EditContext` moves the subscription to it and re-reads the messages.

### Structure

- The banner carries `role="alert"`.
- The banner starts with an `.validation-summary-icon` holding the decorative `Alert` icon.
- A nonempty `Title` renders a `.validation-summary-title` above the list.
- An empty `Title` renders no title element.
- `AdditionalAttributes` are applied to the banner.
- A `class` attribute is merged into the banner's classes through `Class`.

### Appearance

- The banner has a border mixed from `--danger`.
- The banner's background is a tint mixed from `--danger`.
- The banner's `backdrop-filter` is `--glass-fx-flow`, the in-flow glass tier.
- The banner has the `--r-md` radius.
- The banner has `--sp-4` padding.
- The icon reads `--danger`.
- The title reads `--fg`.
- Messages read `--fg-muted`.
- The stylesheet branches on no colour mode (`DESIGN-02`).

### Cleanup

- Disposing the component unsubscribes from `OnValidationStateChanged`.

## Recorded debt

- **The banner snaps in and out.** It mounts on the first message and unmounts
  on the last without `DrylPresence` (`DESIGN-11`, `DESIGN-12`).
- **Messages are not linked to their fields**; the list offers no way to jump to
  the invalid input.
- **Literal values** — the type sizes and the small gaps and margins of
  `.validation-summary-icon`, `.validation-summary-title` and
  `.validation-summary-list`; the icon size passed to `DrylIcon` (`DESIGN-01`).
- **Fixed English default** — `Please fix the following:`.

## Cross-cutting evidence (`SPEC-05`)

- **Both color modes** — mode tokens only; inspected on `/components/form-field` in dark and light (2026-11-20)
- **Enter/exit animation** — **missing**: the banner mounts and unmounts
  conditionally without `DrylPresence`; recorded above as debt, not claimed as
  an exception.
- **Keyboard and a11y** — no interactive element; `role="alert"` announces the
  banner when it appears, and the icon is decorative.
- **AI mode** — **no**: it reports form validation, not content a model writes
  (`AI-05`).
- **Demo page** — `/components/form-field`
  (`DRYL.Website/Components/Pages/DemoFormField.razor`), in the example
  `Components/Examples/FormField/ValidatedForm.razor`, which sets `Title`.
- **`ComponentCatalog`** — no entry of its own; shown on the `"Form Field"` /
  `form-field` page, whose entry names only `DrylFormField` (`REL-04`).
- **Tests** — none.
