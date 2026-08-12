# DrylButton

## Meta
- **State:** Implemented
- **Source:** code/DRYL.Components/Components/Actions/DrylButton.razor

## User Story

As a Blazor developer building an application on DRYL, I want one button that
already carries the library's variants, sizes, loading state, icons and AI aura,
so that every action in my app looks and behaves the same without me styling a
`<button>` by hand.

## Description

`DrylButton` is the library's primary action component. It renders a single native
`button` element and adds four visual variants, three sizes, an optional leading or
trailing icon, a loading state that swaps the leading icon for a spinner, an
optional toggle state, and the shared AI aura.

It is also the button the rest of the library composes: `DrylSplitButton`,
`DrylButtonGroup`, `DrylTable`, `DrylCanvas`, `DrylCommandPalette`,
`DrylEmptyState`, `DrylChatComposer` and the canvas node view all place one rather
than emitting their own `button` markup, and `DrylSplitButton` re-exposes this
component's two enums as its own `Variant` and `Size` parameters.

The `ButtonVariant` and `ButtonSize` enums are declared **nested inside**
`DrylButton`. A consumer therefore writes them qualified —
`Variant="DrylButton.ButtonVariant.Ghost"`, `Size="DrylButton.ButtonSize.Small"` —
which is how every call site in the library and in `DRYL.Website` spells them, and
how `DrylSplitButton` types its own parameters
(`public DrylButton.ButtonVariant Variant`). The unqualified spelling appears only
in the components' Razor usage comments, not in compiled call sites.

AI mode is inherited rather than declared here: the component is
`@inherits DrylAiAware`, which supplies the opt-in `Ai` parameter and the `Aura`
variant, and resolves both against a surrounding `DrylAiScope`. The aura's mount
lifecycle is owned by an `AuraLifecycle` the component composes and disposes.

The component has no codebehind and **no `.razor.css`**: all of its styling lives
in the shared `code/DRYL.Components/wwwroot/dryl.css`, under the `.btn` family of
selectors.

## Public API

| Member | Type | Default | Purpose |
|---|---|---|---|
| `Variant` | `DrylButton.ButtonVariant` | `ButtonVariant.Primary` | Visual style. `Primary`, `Secondary`, `Ghost`, `Danger`. |
| `Size` | `DrylButton.ButtonSize` | `ButtonSize.Medium` | Size. `Small`, `Medium`, `Large`. |
| `Loading` | `bool` | `false` | Shows a spinner in place of the leading icon and makes the button inert. |
| `Disabled` | `bool` | `false` | Makes the button inert and renders it as inactive. |
| `IsSubmit` | `bool` | `false` | Selects the rendered `type`: `submit` when set, `button` otherwise. |
| `LeadingIcon` | `string?` | `null` | `DrylIcon` name rendered before the label. |
| `TrailingIcon` | `string?` | `null` | `DrylIcon` name rendered after the label. |
| `AriaLabel` | `string?` | `null` | Accessible name, for the icon-only case where there is no visible label. |
| `Pressed` | `bool?` | `null` | Toggle state. Non-`null` emits `aria-pressed`; `true` also highlights the button. `null` is a plain, non-toggle button. |
| `OnClick` | `EventCallback<MouseEventArgs>` | — | Fires on activation while the button is neither `Disabled` nor `Loading`. |
| `ChildContent` | `RenderFragment?` | `null` | The label. Omitting it puts the button in icon-only mode. |
| `Ai` | `AiState` | `AiState.None` | Inherited from `DrylAiAware`. The AI opt-in (`AI-03`): off by default, engages the shared aura. |
| `Aura` | `AiAura?` | `null` | Inherited from `DrylAiAware`. Pins the aura variant; `null` inherits the scope, ultimately `AiAura.Comet`. |
| `Class` | `string?` | `null` | Extra CSS class(es) merged onto the button's own classes. |
| `AdditionalAttributes` | `IDictionary<string, object>?` | `null` | Pass-through attributes on the `button` element. |

Nested enums: `DrylButton.ButtonVariant` (`Primary`, `Secondary`, `Ghost`,
`Danger`) and `DrylButton.ButtonSize` (`Small`, `Medium`, `Large`). There is no
`IconOnly` parameter — icon-only mode is entered by omitting `ChildContent`.

## Acceptance Criteria

### Content and composition

- The component renders exactly one `button` element as its root.
- The component renders `ChildContent` as the button's label.
- The component renders a leading `DrylIcon` when `LeadingIcon` is set and
  `Loading` is `false`.
- The component renders no leading icon when `LeadingIcon` is empty or `null`.
- The component renders a trailing `DrylIcon` when `TrailingIcon` is set and
  `Loading` is `false`.
- The component renders no trailing icon while `Loading` is `true`.
- The leading and trailing icons carry distinct marker classes, so the stylesheet
  can animate them independently.
- The component renders `DrylAuraElements` as the first child of the button, which
  emits the aura's layers only while the aura's lifecycle is present.

### Variants and sizes

- `Variant` defaults to `ButtonVariant.Primary`.
- `Variant` accepts exactly the four values of `DrylButton.ButtonVariant`.
- Each of the four variants puts a distinct variant class on the button.
- `Size` defaults to `ButtonSize.Medium`.
- `Size` accepts exactly the three values of `DrylButton.ButtonSize`.
- `ButtonSize.Medium` is the unmodified size and adds no size class.
- `ButtonSize.Small` and `ButtonSize.Large` each add their own size class.

### Interaction

- The `OnClick` callback fires when the button is activated and neither `Disabled`
  nor `Loading` is set.
- The `OnClick` callback does not fire while `Disabled` is `true`.
- The `OnClick` callback does not fire while `Loading` is `true`.
- The rendered `button` carries the native `disabled` attribute exactly when
  `Disabled` or `Loading` is `true`; the component does not use `aria-disabled`.
- The component renders a spinner in place of the leading icon exactly while
  `Loading` is `true`.
- The rendered `button` carries `type="submit"` when `IsSubmit` is `true`, so it
  submits its enclosing form.
- The rendered `button` carries `type="button"` when `IsSubmit` is `false`, so a
  button inside a form never submits it by accident.

### Toggle state

- `Pressed` defaults to `null`.
- The rendered `button` carries no `aria-pressed` attribute while `Pressed` is
  `null`, so a plain button is not exposed as a toggle.
- The rendered `button` carries `aria-pressed="true"` while `Pressed` is `true`.
- The rendered `button` carries `aria-pressed="false"` while `Pressed` is `false`.
- The button carries the active modifier class exactly while `Pressed` is `true`.
- The component never changes `Pressed` itself: the toggle is controlled by the
  consumer through `OnClick`.

### Class and attribute merging

- `Class` is merged onto the button's own classes and never replaces them.
- A consumer-supplied `class` attribute is merged the same way rather than
  clobbering the component's identity classes, because Blazor matches parameter
  names case-insensitively and binds it to `Class`. `ClassMergeTests` in
  `tests/DRYL.Components.Tests/ClassMergeTests.cs` pins both paths: the typed
  parameter and the unmatched attribute each leave `btn` and the variant class in
  place.
- `AdditionalAttributes` are applied to the `button` element, so `data-*` and other
  pass-through attributes reach the DOM.
- `AriaLabel` is rendered as the button's `aria-label`, and the attribute is
  omitted when `AriaLabel` is `null`.

### AI mode

- `Ai` defaults to `AiState.None`, so a button that was never given the parameter
  renders as an ordinary button (`AI-03`).
- `Ai` is an opt-in in the sense `AI-03` defines: it is a switch that turns AI
  styling on for a component that otherwise renders as an ordinary button, so its
  name and its `AiState.None` default are the ones the rule requires.
- An explicit `Ai` other than `AiState.None` wins over a surrounding `DrylAiScope`.
- An `Ai` of `AiState.None` inherits the surrounding `DrylAiScope`'s state.
- The effective state is `AiState.None` when `Ai` is `AiState.None` and there is no
  surrounding scope.
- An explicit `Aura` wins over a surrounding `DrylAiScope`'s variant.
- The effective variant is `AiAura.Comet` when neither the component nor a
  surrounding scope pins one.
- The button carries the shared aura classes produced from its lifecycle and
  effective variant, so it composes the `.ai-aura*` primitives and defines no
  button-specific ring (`AI-02`).
- The one-shot `Generated` wash is re-keyed on each entry into `AiState.Generated`,
  so a repeated generation replays the bloom (`AI-07`).
- The aura stays mounted and fades after the effective state drops to
  `AiState.None`, rather than being removed instantly (`DESIGN-12`).
- The component disposes its aura lifecycle with itself, cancelling the pending
  exit or retire timer (`CODE-05`).

### Keyboard and accessibility

- The button is a native `button` element, so it is reachable with `Tab` and
  activated with `Enter` and `Space` without the component adding key handling of
  its own (`UX-01`).
- The button shows the library's shared `:focus-visible` ring, drawn in
  `--accent-b`; the `.btn` rules override no outline, so the ring is not suppressed
  (`UX-02`).
- A `Disabled` or `Loading` button is removed from the tab order and is not
  operable, because the native `disabled` attribute is used rather than
  `aria-disabled`. The consequence is deliberate and worth knowing: a button that
  becomes `Loading` while focused loses focus, and assistive technology skips it
  entirely instead of announcing it as an unavailable control.
- The loading spinner is `aria-hidden`, so it adds no announcement and no text to
  the accessible name.
- The component makes no announcement when `Loading` changes: it renders no live
  region and sets no `aria-busy`. A consumer that needs the transition announced
  provides its own live region.
- Icon-only mode is entered by omitting `ChildContent`; the button then carries the
  icon-only class regardless of whether a `LeadingIcon` was given.
- An icon-only button has no visible text, so its accessible name comes from
  `AriaLabel` alone.
- The component renders no `DrylTooltip` of its own. `UX-05` requires an icon-only
  button to be wrapped in one and to carry an `aria-label` saying the same thing;
  the wrapper is an ancestor element, so that duty sits at the call site, and this
  component's part of it is to accept `AriaLabel`. The demo page's icon-only Ghost
  button in `DRYL.Website/Components/Examples/Button/Icons.razor` sets `AriaLabel`
  but is not wrapped — a call-site gap in the website, not in the component.
- The aura layers add no tab stop and are hidden from assistive technology, so AI
  mode changes neither the focus order nor the accessible name of the button
  (`UX-07`).

### Motion

- Hover, press, focus and disabled are transitions rather than steps: background,
  border color, box-shadow, color and transform each animate.
- The transform transition uses `--ease-spring`, so a press settles back rather
  than snapping.
- The color and surface transitions use `--ease-out`.
- All of those transitions run for `--dur-med`.
- Pressing the button drops and shrinks it while the pointer is down, and only
  while the button is not disabled.
- A glass sheen sweeps across the surface on hover, with its opacity on `--dur-fast`
  and its travel on `--dur-slow`, both with `--ease-out`.
- The sheen is not rendered on `ButtonVariant.Ghost`, which is chromeless, and not
  while the button is disabled.
- The trailing icon slides forward on hover and the leading icon pops, each with
  `--ease-spring` over `--dur-med`.
- The icon of an icon-only button scales on hover instead of sliding.
- The hover sheen is suppressed under `prefers-reduced-motion: reduce` (`UX-06`).
- The icon springs are suppressed under `prefers-reduced-motion: reduce`, both
  their transitions and their hover transforms (`UX-06`).
- The loading spinner rotates continuously and linearly, which `DESIGN-10`
  explicitly allows for a rotating, `infinite` animation.
- The button has no enter or exit animation of its own. This is the explicit
  exception `DESIGN-11` allows: a button is a persistent control that its host
  places once, and its motion budget is spent on state changes — hover, press,
  focus, the sheen, the icon springs and the AI aura — rather than on an entrance.
  A consumer that mounts a button conditionally wraps it in `DrylPresence` on its
  own side (`DESIGN-12`).

  Two things the reduced-motion mirror does not cover are recorded here rather than
  claimed as compliance. The `.btn` base transitions and the press transform stay
  active under `prefers-reduced-motion: reduce`; they are short state changes, not
  travel. And the `.spinner` rule carries no reduced-motion mirror at all, so the
  loading spinner keeps rotating — unlike `.progress--indeterminate` and
  `.ai-indicator`, which are muted. `.spinner` is shared with `DrylTable`, so
  muting it is a decision of its own rather than a side effect of writing this
  spec. Documented debt, not compliance.

### Appearance

- `ButtonVariant.Primary` is filled with `--accent-grad`, labelled in
  `--on-accent` and bordered in `--on-accent-line`.
- `ButtonVariant.Primary` carries an accent glow derived from `--accent-a` and
  `--accent-b`, which intensifies on hover.
- `ButtonVariant.Secondary` uses `--glass-2` with a `--line-strong` border, and
  moves to `--glass-3` with an `--accent-line` border on hover.
- `ButtonVariant.Ghost` has no background and is labelled in `--fg-muted`, taking
  `--glass-2` and `--fg` only on hover.
- `ButtonVariant.Danger` derives its background and border from `--danger` and is
  labelled in `--danger-fg`.
- The default label color is `--fg` and the type is set in `--font-sans`.
- The corner radius is `--r-md`, and `ButtonSize.Small` uses `--r-sm`.
- The hover sheen derives from `--shimmer`.
- The active modifier reads as a toggled-on secondary: an `--accent-line` border
  and an `--accent-a` glow.
- A disabled button is dimmed, desaturated and stripped of its glow, so it reads as
  inert in every variant.
- The accent appears as a gradient fill on the single primary action, as hairline
  borders and as glow rings — never as a saturated fill of a large surface
  (`DESIGN-08`).
- The component branches on no color mode and holds no mode-assuming value, so the
  same markup and the same rules serve light and dark (`DESIGN-02`).

  The button's `height`, `padding`, `gap`, `font-size`, `font-weight` and
  `letter-spacing` are written as literals, as are the icon-only width, the two
  size overrides, the disabled `opacity` and `grayscale` amounts, and the spinner's
  size, border width and period. `DESIGN-01` governs the lengths; its check greps
  colors in `*.razor.css` only, and this component has no isolated stylesheet at
  all — its rules live in the shared
  `code/DRYL.Components/wwwroot/dryl.css` — so the green that check reports says
  nothing whatsoever about `DrylButton`. Recorded here as documented debt, not as
  compliance. The colors themselves are clean: every one resolves to a token or to
  a `color-mix` over tokens.

## Cross-cutting evidence (`SPEC-05`)

- **Both color modes** — token-only colors across all four variants, the toggled
  state and the disabled state; the component defines no mode-specific rule and no
  mode-assuming literal. Verified by `node scripts/check-light-sync.mjs` and
  `node scripts/validate-light-contrast.mjs`, which stay green for the tokens the
  button consumes.
- **Enter/exit animation** — none of its own, and the exception is written out
  under "Motion" above on the terms `DESIGN-11` sets. The AI aura does animate out,
  through the lifecycle the component composes (`DESIGN-12`).
- **Keyboard and a11y** — the "Keyboard and accessibility" criteria above: a native
  `button` with the shared focus ring, `aria-pressed` only for real toggles,
  `aria-label` for the icon-only case, an `aria-hidden` spinner, and the two things
  the component does *not* do — no `aria-disabled`, no loading announcement, no
  tooltip of its own.
- **AI mode** — yes, and as an opt-in in the exact sense `AI-03` requires: the
  parameter is inherited from `DrylAiAware`, is named `Ai`, defaults to
  `AiState.None`, and is a switch on a component that renders as an ordinary button
  without it. It is none of the three non-opt-in shapes the rule names — not
  content, not a settle state, not a broadcast override.
- **Demo page** — `DRYL.Website/Components/Pages/DemoButton.razor`, routed at
  `/components/buttons`, composing
  `DRYL.Website/Components/Examples/Button/Variants.razor`, `.../Button/Sizes.razor`,
  `.../Button/Icons.razor` and `.../Button/States.razor`.
- **`ComponentCatalog`** — registered as `"Button"` / `buttons` with `ClassName`
  `"DrylButton"` in `DRYL.Website/Components/ComponentCatalog.cs`, in the
  `"Actions"` category.
