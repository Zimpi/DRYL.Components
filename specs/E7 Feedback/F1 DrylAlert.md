# DrylAlert

## Meta
- **State:** Implemented
- **Source:** code/DRYL.Components/Components/Feedback/DrylAlert.razor

## User Story

As a Blazor developer, I want to put a notice on the page that says what kind of
notice it is — informational, successful, a warning, a failure, or something an
AI produced — so that a reader recognises its weight before reading a word of
it, and a screen-reader user is interrupted only when the message is worth an
interruption.

## Description

`DrylAlert` is the in-flow feedback banner: an icon chip, an optional bold
title, a body, and an optional dismiss button. It stays where it is placed —
unlike `DrylToast`, which floats and expires — so it suits a form error, a
policy notice at the top of a page, or a provenance line above AI-generated
content.

It carries **two independent axes** and that is its defining trait. `Kind` says
what the message is; `Ai` says whether something is happening to it. Neither
implies the other: a `Warning` alert can sit at `AiState.Thinking` while the
check that produced it is still running, and an `AlertKind.Ai` alert can sit at
`AiState.None` once its content is final. Both are specified in
[`_Api.md`](_Api.md).

[`I28`](../../ideas/I28%20An%20alert%20that%20is%20alive.md) gives it a life of
its own: glows in the `Kind`'s tone drifting behind the content, an arrival
when it mounts and an exit that shrinks it away when it is dismissed.

The component is also the fallback surface of
[`F7 DrylErrorBoundary`](F7%20DrylErrorBoundary.md), which renders it as
`AlertKind.Danger` and forwards its own `Ai` and `Aura` into it.

## Public API

| Member | Type | Default | Purpose |
|---|---|---|---|
| `Kind` | `DrylAlert.AlertKind` | `AlertKind.Info` | Semantic variant; picks the icon and the accent. |
| `Title` | `string?` | `null` | Bold headline above the body. |
| `Icon` | `string?` | `null` | Icon override by `DrylIcon` name. `""` suppresses the icon. |
| `Dismissible` | `bool` | `false` | Renders a dismiss button. |
| `OnDismiss` | `EventCallback` | — | Raised when the dismiss button is activated. |
| `ChildContent` | `RenderFragment?` | `null` | Body content. |
| `Ai` | `AiState` | `AiState.None` | Ambient AI state. `AiState.None` inherits a surrounding `DrylAiScope`. |
| `Aura` | `AiAura?` | `null` | Pins the aura variant; `null` inherits a surrounding `DrylAiScope`. |
| `Class` | `string?` | `null` | Extra CSS class(es) merged onto the alert's own classes. |
| `AdditionalAttributes` | `IDictionary<string, object>?` | `null` | Pass-through attributes on the alert root. |

`Dismissible` and `OnDismiss` are separate on purpose, and which of them is set
decides **who owns the alert's lifetime**. With a handler, the host owns it:
dismissing is a request, and the host answers it by unmounting the alert. With
no handler, nobody is listening, so the alert answers the press itself. The
button is therefore never inert. In both cases the alert plays its own exit
first — it shrinks and fades where it stands, then the space it held closes —
so a dismissal animates without the host wrapping the alert in anything.

## Acceptance Criteria

### Structure

- The component renders a single root element carrying the alert classes.
- The root carries the modifier class of its `Kind`, one per value.
- `AlertKind.Info` is the class used for any value the variant switch does not
  match, so an unmapped value degrades to the neutral notice.
- The component renders an icon chip when an icon resolves.
- The component renders no icon chip when `Icon` is the empty string.
- The component renders a title element when `Title` is non-empty.
- The component renders no title element when `Title` is `null` or empty.
- The component renders a body element when `ChildContent` is set.
- The component renders no body element when `ChildContent` is `null`.
- The title and the body sit in one content region that takes the space left by
  the icon and the dismiss button.
- The body region shrinks rather than overflowing when its text is longer than
  the available width.
- `Class` is merged onto the root's own classes rather than replacing them.
- `AdditionalAttributes` are applied to the root.

### Icon resolution

- `Icon` set to a non-empty value wins over the icon implied by `Kind`.
- `Icon` set to the empty string suppresses the icon entirely, including for a
  `Kind` that would otherwise supply one.
- `Icon` left `null` resolves the icon from `Kind`.
- Each `AlertKind` value resolves to an icon name, and `AlertKind.Info` supplies
  the fallback for an unmatched value.
- The icon chip is hidden from assistive technology, because the `Kind` it
  depicts is already carried by the alert's role.

### Dismissal

- The dismiss button is rendered only when `Dismissible` is `true`.
- Activating the dismiss button raises `OnDismiss` when a handler is attached.
- Activating the dismiss button plays the alert's exit before anything else
  happens, in every configuration (`dryl.motion.collapse`).
- With a handler attached, `OnDismiss` is raised once the exit has played.
- With a handler attached the component does not remove itself: unmounting the
  alert is the host's decision, taken in response to `OnDismiss`.
- A host that keeps the alert after `OnDismiss` gets it back: the exit plays in
  reverse (`dryl.motion.restore`).
- With no handler attached the component removes itself once the exit has
  played, so the button is never a control that does nothing.
- Further presses while the exit is playing are ignored, so `OnDismiss` is
  raised once per dismissal.
- Setting `Dismissible` to `false` restores a self-dismissed alert, so a host
  without a handler can still bring it back without remounting it.
- No configuration adds an element around the alert: the exit plays on the
  alert's own root.
- The dismiss button is a `type="button"`, so an alert inside a form cannot
  submit it.
- A dismissible alert reserves the space its button occupies, so adding
  `Dismissible` does not reflow the text.

### Keyboard and accessibility

- An alert of `AlertKind.Danger` or `AlertKind.Warning` carries `role="alert"`.
- Every other `Kind` carries `role="status"`.
- An alert of `AlertKind.Danger` or `AlertKind.Warning` carries
  `aria-live="assertive"`, so a failure interrupts what the screen reader is
  saying.
- Every other `Kind` carries `aria-live="polite"`, so a routine notice waits its
  turn.
- The dismiss button carries an accessible label of its own, since it renders an
  icon and no text (`UX-05`).
- The dismiss button is reachable by `Tab` and activated by `Enter` and `Space`,
  because it is a native button and the component adds no key handling.
- The dismiss button shows a visible focus ring under `:focus-visible`.

### Appearance

- Every color the component renders comes from a token; the component names no
  literal color (`DESIGN-01`).
- The alert sits in the flow and therefore pairs `--glass-1` with
  `--glass-fx-flow`, the pairing `DESIGN-06` requires for an in-flow surface.
- The border is `--line-strong` tinted toward the `Kind`'s tone, and the corner
  radius `--r-md`.
- Each `Kind` tints its icon chip, its border and the ember layer; the text
  stays neutral.
- The ember layer is two soft radial glows in the `Kind`'s tone, clipped to the
  alert and drawn behind its content. They are gradients at low strength, so
  the surface stays glass and the accent never fills it (`DESIGN-08`).
- `AlertKind.Ai`'s second glow is `--accent-b`; every other `Kind` uses its
  tone for both.
- The ember layer is hidden from assistive technology and takes no pointer
  events.
- `AlertKind.Success`, `Warning` and `Danger` derive their chip from the
  matching semantic token.
- `AlertKind.Info` and `AlertKind.Ai` derive their chip from `--accent-soft` and
  `--accent-ico`.
- The body text is `--fg-muted`, so the title reads as the louder of the two.
- The component branches on no color mode and holds no mode-assuming value, so
  the same markup serves light and dark (`DESIGN-02`).

### Motion

- The dismiss button transitions its background and its text color over
  `--dur-fast` with `--ease-out` on hover.
- The ember glows drift continuously on `transform` alone, `--ease-in-out`,
  `infinite alternate` (`alert-ember-a`, `alert-ember-b`).
- The drift's tempo and strength scale with the `Kind`'s weight: `Info`,
  `Success` and `Ai` drift calmest, `Warning` faster and brighter, `Danger`
  fastest.
- On mount the alert arrives once: a band of its tone sweeps from the icon
  across the alert over `--dur-choreo` (`alert-wave`), and the icon chip
  springs in over `--dur-slow` with `--ease-spring` (`alert-ico-in`).
- A re-render does not replay the arrival, because the alert is patched in
  place.
- The exit is two beats over `--dur-choreo`: the alert shrinks, blurs and fades
  where it stands, then its height, padding, border and the gap of a flex or
  grid parent close, so the content below glides up instead of jumping.
- Under `prefers-reduced-motion: reduce` the glows stand still, the arrival is
  skipped and the exit settles at once; the alert remains fully legible and
  fully dismissible.

### AI mode

- `Ai` defaults to `AiState.None`, so AI styling is opt-in.
- `Ai` is independent of `Kind`: every combination of the two renders.
- The aura variant follows `Aura` when set and a surrounding `DrylAiScope`
  otherwise.
- A component that leaves `Ai` at `AiState.None` shows the surrounding
  `DrylAiScope`'s state, including the one-shot `AiState.Generated` reveal on an
  inherited move into it; an explicit `Ai` wins over the scope.
- The alert renders the shared aura vocabulary — ring, comet, glow, wash —
  rather than an alert-specific AI treatment (`AI-02`).
- While an aura is present the alert's own border recedes to `--accent-line`, so
  the rotating gradient ring dominates instead of competing with a second edge.
- Leaving AI mode keeps the aura mounted for one `--dur-slow` beat, so it
  dissolves rather than snapping away.
- Entering `AiState.Generated` replays the one-shot completion wash, every time
  it is entered, including on a second entry after leaving.
- With an aura present, the ring and the comet stay the dominant AI signal; the
  ember layer is the `Kind`'s identity, not an AI visual.
- The AI state changes nothing about the alert's layout, so text does not reflow
  when an operation starts or ends.
- `AiState.Proposed` renders the shared resting proposal look of `DrylAuraElements`:
  the frozen aurora edge, with no comet and no halo.
- A change from `AiState.Proposed` straight to `AiState.Generated` plays the shared
  accept wave of `DrylAuraElements` before the completion wash.

## Recorded gaps

- **The dismiss button's label is fixed English** (`"Dismiss notification"`),
  with no parameter to change it. Every other string on the component comes from
  the consumer.
- **Most of its criteria are unguarded.** Tested today, in
  `tests/DRYL.Components.Tests/DrylAlertTests.cs`: both halves of the dismissal
  contract, the wrapper's absence in the other two configurations, the
  role/`aria-live` split by `Kind` and the empty-string icon; plus the class
  merge in `tests/DRYL.Components.Tests/ClassMergeTests.cs`. The title and body
  slots, the icon resolution per `Kind` and the whole AI section are not.
- **The exit itself plays outside bUnit's reach.** It is a Web Animations call in
  `dryl.motion.collapse`; bUnit's loose interop completes it at once, so the
  suite asserts the order (exit, then handler or removal; restore when the host
  keeps the alert). That the alert shrinks, the space closes and a kept alert
  comes back whole was measured in the browser.
- **A parent's gap is closed only for flex columns and grids.** The exit reads
  the parent's `row-gap`; a gap from a horizontal flex row or a sibling's own
  margin is not compensated.
- **Literal type sizes and paddings.** The alert's font sizes, the icon chip's
  dimensions and the banner's padding are literals in
  `code/DRYL.Components/wwwroot/dryl.css`. `DESIGN-01` covers colors, radii,
  shadows, durations and easings, which are tokens here; type scale is not
  covered by a token today. Recorded as debt, not as compliance.
- **`AdditionalAttributes` and `Class` reach the same root element.** A `class`
  entry splatted through `AdditionalAttributes` would clobber the component's
  own classes; `Class` is the supported way to add one.

## Cross-cutting evidence (`SPEC-05`)

- **Both color modes** — token-only colors, verified by
  `node scripts/check-light-sync.mjs` and
  `node scripts/validate-light-contrast.mjs`. The glass fill, the frost and the
  semantic chips are the mode-dependent tokens; the component defines no
  mode-specific rule.
- **Enter/exit animation** — the alert arrives with its one-shot wave and chip
  spring, and every dismissal plays its own exit before the alert is removed.
  Its continuous motion is the ember drift; its other motion is the dismiss
  button's hover transition and the AI aura.
- **Keyboard and a11y** — the "Keyboard and accessibility" criteria above. The
  role/`aria-live` split by `Kind` is the substantive decision: only a failure
  or a warning interrupts.
- **AI mode** — yes, on its own axis. An alert is where AI provenance is stated
  in words, so it carries both the semantic `AlertKind.Ai` and the ambient `Ai`
  state, and neither is derived from the other.
- **Demo page** — `DRYL.Website/Components/Pages/DemoAlert.razor`, with the
  examples `Components/Examples/Alert/Varianten.razor`, `.../NoTitle.razor`,
  `.../NoIcon.razor`, `.../Dismissible.razor`, `.../AiStates.razor` and
  `.../Lifecycle.razor`.
- **`ComponentCatalog`** — registered as `"Alert"` / `alerts` in
  `DRYL.Website/Components/ComponentCatalog.cs`, flagged AI-capable.
