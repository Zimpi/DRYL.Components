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
| `Ai` | `AiState` | `AiState.None` | Ambient AI state. |
| `Aura` | `AiAura?` | `null` | Pins the aura variant; `null` inherits a surrounding `DrylAiScope`. |
| `Class` | `string?` | `null` | Extra CSS class(es) merged onto the alert's own classes. |
| `AdditionalAttributes` | `IDictionary<string, object>?` | `null` | Pass-through attributes on the alert root. |

`Dismissible` and `OnDismiss` are separate on purpose, and which of them is set
decides **who owns the alert's lifetime**. With a handler, the host owns it:
dismissing is a request, and the host answers it by unmounting the alert. With
no handler, nobody is listening, so the alert answers the press itself and
animates away. The button is therefore never inert, and an alert that worked
before behaves exactly as it did.

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
- With a handler attached the component does not remove itself: unmounting the
  alert is the host's decision, taken in response to `OnDismiss`.
- With no handler attached the component dismisses itself, so the button is
  never a control that does nothing.
- A self-dismissing alert animates out rather than disappearing instantly
  (`DESIGN-12`).
- Setting `Dismissible` to `false` restores a self-dismissed alert, so a host
  without a handler can still bring it back without remounting it.
- The extra wrapper element the self-dismissing configuration needs is present
  only in that configuration, so an alert with a handler and a non-dismissible
  alert render exactly the markup they did before.
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
- The border is `--line-strong` and the corner radius `--r-md`.
- Each `Kind` tints only its icon chip — background and text stay neutral — so
  the accent is an indicator rather than the fill of a large surface
  (`DESIGN-08`).
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
- The alert has no mount animation of its own: it is an in-flow element the host
  places, and a host that wants it to animate in wraps it in `DrylPresence`
  (`DESIGN-12`).
- A self-dismissing alert fades out over the shared presence vocabulary rather
  than a treatment of its own (`DESIGN-13`).
- Under `prefers-reduced-motion: reduce` the alert remains fully legible and
  fully dismissible.

### AI mode

- `Ai` defaults to `AiState.None`, so AI styling is opt-in.
- `Ai` is independent of `Kind`: every combination of the two renders.
- The aura variant follows `Aura` when set and a surrounding `DrylAiScope`
  otherwise.
- The alert renders the shared aura vocabulary — ring, comet, glow, wash —
  rather than an alert-specific AI treatment (`AI-02`).
- While an aura is present the alert's own border recedes to `--accent-line`, so
  the rotating gradient ring dominates instead of competing with a second edge.
- Leaving AI mode keeps the aura mounted for one `--dur-slow` beat, so it
  dissolves rather than snapping away.
- Entering `AiState.Generated` replays the one-shot completion wash, every time
  it is entered, including on a second entry after leaving.
- The AI state changes nothing about the alert's layout, so text does not reflow
  when an operation starts or ends.

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
- **The self-dismissal completes outside bUnit's reach.** The removal finishes
  when the presence exit animation ends, which is driven by JS, so the test
  suite can only assert that the alert began to leave. That it actually goes was
  measured in the browser instead.
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
- **Enter/exit animation** — no enter animation of its own, and that is the
  written exception `DESIGN-11` allows: an in-flow banner is mounted by its host,
  which wraps it in `DrylPresence` when it should animate in. The one unmount the
  component owns — a dismissal nobody is listening to — does animate out, through
  `DrylPresence` and the shared presence vocabulary. Its other motion is the
  dismiss button's hover transition and the AI aura.
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
