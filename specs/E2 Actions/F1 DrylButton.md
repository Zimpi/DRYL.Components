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
`DrylTable`, `DrylCanvas`, `DrylCommandPalette`, `DrylChatComposer` and the canvas
node view all place one rather than emitting their own `button` markup, and
`DrylSplitButton` re-exposes this component's two enums as its own `Variant` and
`Size` parameters. `DrylButtonGroup` and `DrylEmptyState` are not on that list:
each renders a slot the consumer fills with buttons of their own.

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
- Both icons are rendered at one fixed pixel size, whatever the button's `Size`.
- The component renders `DrylAuraElements` as the first child of the button, which
  emits the aura's layers only while the aura's lifecycle is present.

### Variants and sizes

- `Variant` defaults to `ButtonVariant.Primary`.
- `Variant` accepts exactly the four values of `DrylButton.ButtonVariant`.
- Each of the four variants puts a distinct variant class on the button.
- A `Variant` outside the declared four — reachable only by casting an
  out-of-range integer — renders as `ButtonVariant.Primary` rather than failing.
- `Size` defaults to `ButtonSize.Medium`.
- `Size` accepts exactly the three values of `DrylButton.ButtonSize`.
- `ButtonSize.Medium` is the unmodified size and adds no size class.
- `ButtonSize.Small` and `ButtonSize.Large` each add their own size class.
- A `Size` outside the declared three renders as `ButtonSize.Medium` rather than
  failing.
- Icon-only mode is square at every size: the label padding is dropped and the
  width matches the height at `ButtonSize.Small`, at `ButtonSize.Medium` and at
  `ButtonSize.Large`. The size override and the icon-only rule are two separate
  declarations of the same padding, so each size carries its own icon-only reset;
  without it the size's horizontal padding survives, and — since the icon-only
  width is the *whole* button width rather than a content width — the icon is left
  no room at all and collapses to zero.

### Interaction

- The `OnClick` callback fires when the button is activated and neither `Disabled`
  nor `Loading` is set.
- The `OnClick` callback does not fire while `Disabled` is `true`.
- The `OnClick` callback does not fire while `Loading` is `true`.
- The rendered `button` carries the native `disabled` attribute when `Disabled` is
  `true`; the component does not use `aria-disabled`.
- The rendered `button` carries the native `disabled` attribute when `Loading` is
  `true`.
- The component renders a spinner in place of the leading icon exactly while
  `Loading` is `true`.
- The rendered `button` carries `type="submit"` when `IsSubmit` is `true`, so it
  submits its enclosing form.
- The rendered `button` carries `type="button"` when `IsSubmit` is `false`, so a
  button inside a form never submits it by accident.

### Attribute precedence

- `AdditionalAttributes` is splatted onto the `button` after every attribute the
  component writes itself, so a pass-through attribute of the same name wins.
- A consumer-supplied `disabled` attribute therefore disables the button even
  though `Disabled` and `Loading` are both `false`.
- A consumer-supplied `type` attribute therefore overrides the one `IsSubmit`
  selected.
- A consumer-supplied `aria-pressed` attribute therefore overrides the one
  `Pressed` produced.
- A consumer-supplied `aria-label` attribute therefore overrides `AriaLabel`.
- `class` is the exception to that precedence: it binds to the `Class` parameter
  and is merged rather than splatted, so it never reaches `AdditionalAttributes`
  and never clobbers the component's own classes.

### Toggle state

- `Pressed` defaults to `null`.
- The rendered `button` carries no `aria-pressed` attribute while `Pressed` is
  `null`, so a plain button is not exposed as a toggle.
- The rendered `button` carries `aria-pressed="true"` while `Pressed` is `true`.
- The rendered `button` carries `aria-pressed="false"` while `Pressed` is `false`.
- The button carries the active modifier class exactly while `Pressed` is `true`.
- The component never changes `Pressed` itself: the toggle is controlled by the
  consumer through `OnClick`.

### Class merging

- `Class` is merged onto the button's own classes and never replaces them.
- A consumer-supplied `class` attribute is merged the same way, because Blazor
  matches parameter names case-insensitively and binds it to `Class`.
- The identity class and the variant class survive both forms of merging, since
  the component composes its class list unconditionally.

  Both paths are pinned by tests. `Button_merges_consumer_class_without_clobbering_identity_classes`
  in `tests/DRYL.Components.Tests/ClassMergeTests.cs` renders with an unmatched
  `class` attribute and asserts the identity class, the variant class and the
  consumer's class are all present; `Button_typed_Class_parameter_is_merged`
  covers the typed parameter and asserts the identity class and the consumer's
  class. Neither test asserts the variant class on the typed path — that half of
  the criterion above rests on the class list being built the same way for both.

### AI mode

- `Ai` defaults to `AiState.None`, so a button that was never given the parameter
  renders as an ordinary button (`AI-03`).
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
- A `Disabled` or `Loading` button is not keyboard-operable and holds no tab stop,
  because the native `disabled` attribute is used rather than `aria-disabled`.
- A button that becomes `Loading` while focused loses focus, for the same reason.
- The loading spinner is `aria-hidden`, so it adds no announcement and no text to
  the accessible name.
- The component renders no live region for the loading state.
- The component sets no `aria-busy` for the loading state.
- The leading and trailing icons are decorative: the component passes them no
  accessible label, so they are rendered `aria-hidden` and contribute nothing to
  the button's name (`UX-07`).
- Icon-only mode is entered by omitting `ChildContent`.
- The icon-only class is applied whether or not a `LeadingIcon` was given, since
  the absence of a label is what decides it.
- An icon-only button's accessible name comes from `AriaLabel` alone.
- `AriaLabel` is rendered as the button's `aria-label`.
- The `aria-label` attribute is omitted when `AriaLabel` is `null`.
- The component renders no `DrylTooltip` of its own.
- The aura layers add no tab stop and are hidden from assistive technology, so AI
  mode changes neither the focus order nor the accessible name of the button
  (`UX-07`).

  Two consequences of the criteria above are worth stating plainly for a consumer.
  First, native `disabled` does **not** remove the button from the accessibility
  tree: a screen reader still reaches it with its browse cursor and announces it as
  unavailable. What is lost is the tab stop, the keyboard operability and — since
  there is no live region and no `aria-busy` — any announcement that the button
  entered or left its loading state. A consumer who needs that transition announced
  provides their own live region.

  Second, `UX-05` requires an icon-only button to be wrapped in a `DrylTooltip`
  naming its action, with the tooltip text and the `aria-label` saying the same
  thing. The wrapper is an ancestor element, so that duty sits at the call site;
  this component's part of it is to accept `AriaLabel`, which it does. The demo
  page's icon-only Ghost button in
  `DRYL.Website/Components/Examples/Button/Icons.razor` sets `AriaLabel` but is not
  wrapped — a call-site gap in the website, not in the component.

### Motion

- Hovering the button animates its background, border color, box-shadow, color and
  transform, rather than stepping them.
- The `ButtonVariant.Primary` variant lifts on hover.
- Pressing the button drops and shrinks it while the pointer is down, and only
  while the button is not disabled.
- The transform transition uses `--ease-spring`, so a press settles back rather
  than snapping.
- The color and surface transitions use `--ease-out`.
- All of those transitions run for `--dur-med`.
- The focus ring is **not** animated: `outline` is absent from the button's
  transition list, so the ring steps in and out.
- The disabled look is **not** animated either: the dim and the desaturation that
  make it read as inert are absent from the transition list and step, while the
  shadow removal and the transform reset animate with the rest.
- A glass sheen sweeps across the surface on hover, with its opacity on `--dur-fast`
  and its travel on `--dur-slow`.
- Both halves of the sheen use `--ease-out`.
- The sheen is not rendered at all on `ButtonVariant.Ghost`, which is chromeless.
- The sheen is never revealed while the button is disabled: it is generated on the
  other three variants but its reveal is gated on the button not being disabled.
- The trailing icon slides forward on hover, so it reads as an affordance to move
  on.
- The leading icon pops on hover.
- The icon of an icon-only button scales on hover instead of sliding.
- Both icon movements use `--ease-spring` over `--dur-med`.
- The hover sheen is suppressed under `prefers-reduced-motion: reduce` (`UX-06`).
- The icon springs are suppressed under `prefers-reduced-motion: reduce`, both
  their transitions and their hover transforms (`UX-06`).
- The loading spinner rotates continuously, which `DESIGN-10` allows to sit outside
  the transition duration scale.
- The button has no enter or exit animation of its own. This is the explicit
  exception `DESIGN-11` allows: a button is a persistent control that its host
  places once, and its motion budget is spent on state changes — hover, press, the
  sheen, the icon springs and the AI aura — rather than on an entrance. A consumer
  that mounts a button conditionally wraps it in `DrylPresence` on its own side
  (`DESIGN-12`).

  Three things are recorded here rather than claimed as compliance. The spinner's
  rotation is written with the bare `linear` keyword and a literal period;
  `DESIGN-10` makes both legitimate for a rotating, `infinite` animation — it
  requires `linear` for rotation and leaves a continuous period free — but the
  easing is therefore a keyword rather than a token. The `.btn` base transitions
  and the `:active` press transform stay active under
  `prefers-reduced-motion: reduce`; they are short state changes rather than
  travel. And `.spinner` carries no reduced-motion mirror at all, so the loading
  spinner keeps rotating — unlike `.progress--indeterminate`, `.ai-indicator` and
  the aura comet, which are all muted. `.spinner` is shared with `DrylTable`, so
  muting it is a decision of its own rather than a side effect of writing this
  spec. Documented debt, not compliance.

### Appearance

- `ButtonVariant.Primary` is filled with `--accent-grad`.
- `ButtonVariant.Primary` is labelled in `--on-accent`.
- `ButtonVariant.Primary` is bordered in `--on-accent-line`.
- `ButtonVariant.Primary` carries an inset top highlight in `--on-accent-hi`.
- `ButtonVariant.Primary` carries an accent glow derived from `--accent-a` and
  `--accent-b`, which intensifies on hover.
- `ButtonVariant.Secondary` uses `--glass-2` with a `--line-strong` border.
- `ButtonVariant.Secondary` moves to `--glass-3` with an `--accent-line` border on
  hover.
- `ButtonVariant.Secondary` blurs what is behind it with `--glass-fx-flow`, so it
  reads as glass over whatever surface it sits on.
- `ButtonVariant.Ghost` has no background of its own.
- `ButtonVariant.Ghost` is labelled in `--fg-muted`, taking `--glass-2` and `--fg`
  only on hover.
- `ButtonVariant.Danger` derives its background and border from `--danger`.
- `ButtonVariant.Danger` is labelled in `--danger-fg`.
- The default label color is `--fg`.
- The type is set in `--font-sans`.
- The corner radius is `--r-md`.
- `ButtonSize.Small` overrides that radius with `--r-sm`.
- The hover sheen derives from `--shimmer`.
- The active modifier carries an `--accent-line` border.
- The active modifier carries a glow derived from `--accent-a`, so a toggled-on
  button reads like a secondary that is switched on.
- A disabled button is dimmed.
- A disabled button is desaturated.
- A disabled button is stripped of its glow, so it reads as inert in every variant.
- The accent appears as a gradient fill on the single primary action, as hairline
  borders and as glow rings — never as a saturated fill of a large surface
  (`DESIGN-08`).
- The component branches on no color mode and holds no mode-assuming value, so the
  same markup and the same rules serve light and dark (`DESIGN-02`).

  The literals are worth naming precisely, because the check that would normally
  catch them cannot see this component at all: `DrylButton` has no isolated
  stylesheet, and `DESIGN-01`'s check greps colors in `*.razor.css` only, so the
  green it reports says nothing whatsoever about these rules.

  `DESIGN-01` enumerates color, padding, radius, shadow, duration and easing.
  Of those it governs, the button writes as literals: its `padding` (base and both
  size overrides) and **every shadow it has** — the primary's four-layer
  `box-shadow` and the heavier hover variant, the secondary's hover glow, the
  danger variant's hover glow and the active modifier's ring-plus-glow are all
  literal offsets, blur radii and spreads, with only their colors tokenised. The
  shadows are the largest untokenised group in the component and are not covered by
  any token today.

  Outside `DESIGN-01`'s enumeration, and therefore debt of a lesser kind: the
  `height` (base and both size overrides), the `gap`, the icon-only `width` at all
  three sizes, the
  `font-size`, `font-weight` and `letter-spacing`, the disabled `opacity` and
  `grayscale` amounts, the transform distances of the hover lift, the press, the
  two icon slides and the icon-only scale, the sheen's gradient angle, color stops,
  `background-size` and travel positions, and the spinner's size and border width.

  The colors are otherwise clean — every one resolves to a token or to a
  `color-mix` over tokens — with one qualification: the base button's border and
  background, the ghost variant's background and the sheen's outer gradient stops
  are the bare `transparent` keyword. `DESIGN-01`'s alpha-context exemption is
  written for `mask` and `clip-path` and does not cover these, so they are named
  here rather than counted as compliant; no token expresses "no paint", and the
  keyword is identical in both color modes.

## Cross-cutting evidence (`SPEC-05`)

- **Both color modes** — token-only colors across all four variants, the toggled
  state and the disabled state, apart from the `transparent` keywords named under
  "Appearance"; the component defines no mode-specific rule and no mode-assuming
  literal. `node scripts/check-light-sync.mjs` and
  `node scripts/validate-light-contrast.mjs` are green.
- **Enter/exit animation** — none of its own, and the exception is written out
  under "Motion" above on the terms `DESIGN-11` sets. The AI aura does animate out,
  through the lifecycle the component composes (`DESIGN-12`).
- **Keyboard and a11y** — the "Keyboard and accessibility" criteria above: a native
  `button` with the shared focus ring, `aria-pressed` only for real toggles,
  `aria-label` for the icon-only case, `aria-hidden` icons and spinner, and the
  three things the component does *not* do — no `aria-disabled`, no loading
  announcement, no tooltip of its own.
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
