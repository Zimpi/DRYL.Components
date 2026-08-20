# DrylDialog

## Meta
- **State:** Implemented
- **Source:** code/DRYL.Components/Dialogs/DrylDialog.razor
              code/DRYL.Components/Dialogs/DialogSize.cs

## User Story

As a Blazor developer writing a dialog for my app, I want to wrap my content in
a ready-made frame with a title, a close button and a footer for my buttons, and
close it from my own code with a result, so that I write only what is specific
to my dialog and nothing about modality, layering or dismissal.

## Description

`DrylDialog` is the **frame**, not the modality. It renders the glass panel a
dialog lives in — header with title, icon and close button, a scrolling body, an
optional footer — and it is what a consumer places at the root of their own
dialog component. What is around it, above the page, is
[`F2 DrylDialogProvider`](F2%20DrylDialogProvider.md): backdrop, layer, focus
trap, scroll lock and the `Escape` key are all the provider's, not this
component's.

The two halves meet in one cascading value. The provider cascades an
`IDrylDialogInstance`, and `DrylDialog` reads it for the things the *caller*
decided rather than the dialog author: the title passed to `ShowAsync`, the size
preset, whether the close button is shown, the frame's AI state. Every one of
those has a local parameter that wins over the instance, so the same component
also works standalone, with no provider and no instance at all — useful for a
dialog rendered inline in a demo, and the reason none of its parameters is
required.

Closing is not this component's decision either. The close button calls
`Cancel()` on the instance; a dialog that wants to close with a result calls
`Instance.Close(...)` from its own code.

## Public API

| Member | Type | Default | Purpose |
|---|---|---|---|
| `Title` | `string?` | `null` | Title text. Falls back to the instance's title. |
| `TitleContent` | `RenderFragment?` | `null` | Custom title slot; replaces the plain `Title` string. |
| `Icon` | `string?` | `null` | Leading icon shown before the title, by `DrylIcon` name. |
| `ChildContent` | `RenderFragment?` | `null` | The dialog body. |
| `ActionContent` | `RenderFragment?` | `null` | Footer content, typically buttons. The footer is absent without it. |
| `ShowHeader` | `bool` | `true` | Render the header at all. |
| `ShowCloseButton` | `bool?` | `null` | Overrides `DialogOptions.ShowCloseButton`. `null` defers to the caller. |
| `Ai` | `AiState` | `AiState.None` | AI state of the frame. `AiState.None` defers to the instance. |
| `Aura` | `AiAura?` | `null` | Pins the aura variant; `null` inherits a surrounding `DrylAiScope`. |
| `Class` | `string?` | `null` | Extra CSS class(es) merged onto the dialog's own classes. |
| `AdditionalAttributes` | `IDictionary<string, object>?` | `null` | Pass-through attributes on the dialog root. |
| `Instance` | `IDrylDialogInstance?` | `null` | Cascading. Supplied by `DrylDialogProvider`; absent when the dialog is rendered standalone. |

`DialogSize` is not a parameter of this component. The size is the *caller's*
decision, made in `DialogOptions` at `ShowAsync` time, and the dialog author does
not override it — a dialog that hardcoded its own width would ignore what the
call site asked for. `DialogSize` and `DialogOptions` are specified in
[`_Api.md`](_Api.md).

The component exposes no `EventCallback`: closing goes through the instance, not
through a callback the host would have to wire.

## Acceptance Criteria

### Structure

- The dialog root carries `role="dialog"`.
- The dialog root carries `aria-modal="true"`.
- The component renders a header, a body and — when `ActionContent` is set — a
  footer, in that order.
- The component renders no footer when `ActionContent` is `null`.
- The component renders no header when `ShowHeader` is `false`.
- The body renders `ChildContent`.
- The body scrolls on its own when the content exceeds the dialog's height, so
  the header and footer stay in place.
- `Class` is merged onto the dialog root's own classes.
- `AdditionalAttributes` are applied to the dialog root.
- The extra class from `DialogOptions.Class` is applied to the dialog root as
  well, and independently of `Class`.

### Title and icon

- The header renders `Title` when it is set.
- The header renders the instance's title when `Title` is `null`.
- The header renders `TitleContent` instead of any title string when it is set.
- The header renders a leading `DrylIcon` when `Icon` is set.
- The header renders no icon slot when `Icon` is `null` or empty.

### Closing

- `ShowCloseButton` defaults to deferring to `DialogOptions.ShowCloseButton`.
- The close button is shown when neither `ShowCloseButton` nor the instance's
  option is set, so a dialog is dismissible unless someone says otherwise.
- `ShowCloseButton` set to `false` hides the close button even when the caller's
  options ask for it.
- Activating the close button cancels the dialog through the instance.
- Activating the close button does nothing when no instance is cascaded, rather
  than throwing.
- The component itself never removes itself from the DOM: closing is a request
  to the provider, which owns the exit animation.

### Size

- The dialog carries the class of `DialogOptions.Size` from its instance.
- The dialog falls back to the `DialogSize.Medium` class when no instance is
  cascaded.
- Each of the four `DialogSize` values maps to its own class, so the four widths
  are selectable and none shares a rule with another.
- A fullscreen dialog fills the viewport, and the padding that insets the other
  sizes is removed for it.

### Standalone use

- The component renders without an `Instance`, with no exception thrown.
- Every value the component reads from the instance has a local parameter or a
  documented fallback, so a standalone dialog is fully configurable.

### Motion

- The dialog enters with a scale-and-lift over `--dur-med` with `--ease-spring`.
- The exit animation belongs to the provider's layer, over `--dur-med` with
  `--ease-in-out`, and mirrors the entrance.
- Both are switched off under `prefers-reduced-motion: reduce`, leaving the
  dialog fully usable.

### Keyboard and accessibility

- The dialog root is labelled by its own title element through
  `aria-labelledby`, so a screen reader announces the dialog by name.
- The title element's id is unique per component instance, so two dialogs on
  screen at once do not label each other.
- The close button carries an accessible label of its own, since it renders an
  icon and no text (`UX-05`).
- The header icon is decorative and adds no second announcement of the title.
- The focus trap, the `Escape` key and the return of focus on close are the
  provider's (`F2`); this component adds no key handling of its own.

### Appearance

- Every color the component renders comes from a token; the component names no
  literal color (`DESIGN-01`).
- The dialog is a floating surface: `--panel-grad` fill with `--glass-fx-float`
  frost, the pairing `DESIGN-06` requires for something that floats above the
  page.
- The border is `--line-strong` and the corner radius `--r-lg`.
- The elevation is `--shadow-lg`, with an accent-derived glow rather than an
  accent fill (`DESIGN-08`).
- The footer is separated from the body by a `--line` border, not by a change of
  background.
- The component branches on no color mode and holds no mode-assuming value, so
  the same markup serves light and dark (`DESIGN-02`).
- Below the mobile breakpoint a non-fullscreen dialog docks to the bottom edge
  as a sheet — full width, rounded at the top only — and a fullscreen dialog is
  unaffected.
- A dialog that reaches the bottom edge keeps its footer clear of the device's
  safe area, so the confirm button is never under a home indicator.

### AI mode

- `Ai` defaults to `AiState.None`.
- An explicit `Ai` value wins over the instance's state.
- `Ai` left at `AiState.None` renders the instance's state, so the caller can
  drive the frame through `DialogOptions.Ai` and `IDrylDialogInstance.SetAi`.
- The aura variant follows `Aura` when set and a surrounding `DrylAiScope`
  otherwise.
- The frame renders the shared aura vocabulary — ring, glow, wash — rather than
  a dialog-specific AI treatment (`AI-02`).
- Leaving AI mode keeps the aura mounted for one `--dur-slow` beat so it
  dissolves rather than snapping away.
- Entering `AiState.Generated` replays the one-shot completion wash, every time
  it is entered.
- The AI state changes nothing about the dialog's layout, so content does not
  reflow when an operation starts or ends.

## Recorded debt

- The dialog widths, the maximum height and the mobile breakpoint are written as
  literals in `code/DRYL.Components/wwwroot/dryl.css`. `DESIGN-01` covers colors,
  radii, shadows, durations and easings, which are tokens here; the four widths
  are not covered by a token today. Recorded as debt, not as compliance.
- `AdditionalAttributes` and `Class` reach the same root element, and a `class`
  entry splatted through `AdditionalAttributes` would clobber the component's
  own classes. `Class` is the supported way to add one.

## Cross-cutting evidence (`SPEC-05`)

- **Both color modes** — token-only colors, verified by
  `node scripts/check-light-sync.mjs` and
  `node scripts/validate-light-contrast.mjs`. The frost and the panel fill are
  the mode-dependent tokens; the component defines no mode-specific rule.
- **Enter/exit animation** — the entrance is this component's; the exit runs on
  the provider's layer and is specified there, because the provider owns the
  moment of removal (`DESIGN-12`).
- **Keyboard and a11y** — the "Keyboard and accessibility" criteria above, plus
  the provider's focus trap and `Escape` handling in `F2`.
- **AI mode** — yes. A dialog is where an agent's work is most often waited on,
  so the frame carries the aura and the instance can change it mid-flight
  through `SetAi`.
- **Demo page** — `DRYL.Website/Components/Pages/DemoDialog.razor`, with the
  examples `Components/Examples/Dialog/Sizes.razor`, `.../CustomTyped.razor`,
  `.../ConfirmAlert.razor`, `.../Sequential.razor` and `.../HumanInMiddle.razor`.
- **`ComponentCatalog`** — registered as `"Dialog"` / `dialog` in
  `DRYL.Website/Components/ComponentCatalog.cs`, with an explicit source-URL
  override because the component sits outside `Components/`.
