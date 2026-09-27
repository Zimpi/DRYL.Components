# DrylCopyButton

## Meta
- **State:** Implemented
- **Source:** code/DRYL.Components/Components/Actions/DrylCopyButton.razor
              code/DRYL.Components/Components/Actions/DrylCopyButton.razor.css

## User Story

As a Blazor developer, I want a button that copies a given text to the
clipboard and visibly and audibly confirms it — or says that it failed — so
that I can offer "copy this value" anywhere without writing the clipboard call,
the confirmation state, its timer and its announcement again.

## Description

`DrylCopyButton` composes a `DrylButton` with the library's clipboard helper
(`dryl.clipboard.copy`, the one `DrylCodeBlock` uses). Pressing it writes
`Text` to the clipboard. On success the `Copy` glyph crossfades to `Check`, a
visible `Label` crossfades to `CopiedLabel`, `OnCopied` fires and the result is
announced. When the browser refuses, the glyph becomes `Alert` in `--danger`
and the label `FailedLabel`. After a short hold (`ResetDelay`, two seconds) the
button returns to rest.

Without a `Label` the button is icon-only: it is named by `AriaLabel` (falling
back to `Copy`) and wrapped in a `DrylTooltip` with the same text (`UX-05`),
which switches to the confirmation while it is shown. With a `Label`, the label
is the accessible name. The name never changes with the state — the result is
announced through a polite live region instead, so assistive technology hears
it once rather than as a renamed control.

Glyphs and labels of all three states are rendered together and stacked in
one grid cell, so the swap is a crossfade and the button keeps the width of its
longest text.

```razor
<DrylCopyButton Text="@contact.Phone" AriaLabel="Nummer kopieren"
                CopiedLabel="Kopiert" FailedLabel="Kopieren fehlgeschlagen" />
```

## Public API

Namespace: `DRYL.Components`. Implements `IDisposable`.

| Member | Type | Default | Purpose |
|---|---|---|---|
| `Text` | `string` (`EditorRequired`) | `""` | The text written to the clipboard. |
| `Label` | `string?` | `null` | Visible text; `null` or empty renders an icon-only button. |
| `AriaLabel` | `string?` | `null` | Accessible name and tooltip of an icon-only button (fallback `"Copy"`); applied with a `Label` only when set. |
| `CopiedLabel` | `string` | `"Copied"` | Confirmation shown and announced after a successful copy. |
| `FailedLabel` | `string` | `"Copy failed"` | Notice shown and announced after a refused copy. |
| `Variant` | `DrylButton.ButtonVariant` | `DrylButton.ButtonVariant.Ghost` | Variant of the composed button. |
| `Size` | `DrylButton.ButtonSize` | `DrylButton.ButtonSize.Small` | Size of the composed button. |
| `OnCopied` | `EventCallback` | — | Fires after a successful copy. |
| `Class` | `string?` | `null` | Extra classes merged onto the button's own. |
| `AdditionalAttributes` | `IDictionary<string, object>?` | `null` | Unmatched attributes, applied to the button. |

`DrylButton.ButtonVariant` and `DrylButton.ButtonSize` are documented in
[`_Api.md`](_Api.md). The component has no `Disabled` parameter; a `disabled`
attribute passed through `AdditionalAttributes` reaches the button.

## Acceptance Criteria

### Structure and naming

- The component renders one `DrylButton`.
- The button carries `.copy-btn`.
- `Variant` defaults to `DrylButton.ButtonVariant.Ghost`.
- `Size` defaults to `DrylButton.ButtonSize.Small`.
- `Variant` and `Size` are passed to the button.
- `Class` is merged onto the button's classes.
- `AdditionalAttributes` are applied to the button.
- The button has `type="button"`, so it never submits a surrounding form.
- Without a `Label`, the button carries `.btn-icon`.
- Without a `Label`, the button's `aria-label` is `AriaLabel`.
- Without a `Label` and without `AriaLabel`, the button's `aria-label` is `Copy`.
- Without a `Label`, the button is wrapped in a `DrylTooltip` whose text equals
  its `aria-label` at rest.
- With a `Label`, the label is rendered as the button's visible text.
- With a `Label` and without `AriaLabel`, the button has no `aria-label`.
- With a `Label`, no tooltip is rendered.
- The glyphs are hidden from assistive technology.
- The copied and failed label texts are hidden from assistive technology.
- The glyphs are the `Copy`, `Check` and `Alert` icons of `DrylIcon.Icons`.

### Copying

- Pressing the button calls `dryl.clipboard.copy` with `Text`.
- A successful copy shows the `Check` glyph.
- A successful copy shows `CopiedLabel` in place of a visible `Label`.
- A successful copy switches an icon-only button's tooltip text to `CopiedLabel`.
- A successful copy writes `CopiedLabel` into the live region.
- A successful copy fires `OnCopied`.
- A copy the helper reports as failed shows the `Alert` glyph.
- A failed copy shows `FailedLabel` in place of a visible `Label`.
- A failed copy switches an icon-only button's tooltip text to `FailedLabel`.
- A failed copy writes `FailedLabel` into the live region.
- A failed copy does not fire `OnCopied`.
- A clipboard helper that throws a `JSException` counts as a failed copy.
- A disconnected circuit during the copy changes nothing.
- After `ResetDelay` the button returns to rest: `Copy` glyph, `Label`, empty
  live region.
- A second press during the hold restarts the hold.
- The button's accessible name does not change with the state.

### Keyboard and accessibility

- The button is reachable by Tab and pressed with Enter or Space, as a native
  `button`.
- The button keeps `DrylButton`'s visible focus ring (`UX-02`).
- The live region has `role="status"` and `aria-live="polite"`.
- The live region is present, empty, before the first press, so the first
  result is announced.
- The live region is visually hidden.

### Appearance and motion

- The glyphs crossfade over `--dur-med`: opacity with `--ease-out`, scale with
  `--ease-spring`.
- A visible label crossfades over `--dur-med` with a rise of `--sp-1`.
- The failed glyph uses `--danger`.
- Swapping state does not change the button's width.
- The glyph springs on hover with `DrylButton`'s leading-icon motion.
- Under `prefers-reduced-motion: reduce` the swap is an instant switch without
  transform.
- Every color comes from a token; the component branches on no color mode
  (`DESIGN-02`).

### Cleanup

- Disposal cancels a pending return to rest.
- A return to rest never renders after disposal.

## Recorded limits

- `DrylTooltip`'s bubble reads its text when it opens; while it is already open
  under keyboard focus it keeps showing the resting text. The live region, not
  the tooltip, is the reliable confirmation.
- A second copy within the hold writes the same text into the live region;
  some screen readers do not repeat an unchanged announcement.
- `ResetDelay` is fixed.
- `DrylCodeBlock` keeps its own copy button (`ideas/I17 A copy button.md`,
  out of scope).

## Cross-cutting evidence (`SPEC-05`)

- **Both modes:** colors come from `DrylButton`'s variant tokens and
  `--danger`; no mode branch. Not checked by eye in this pass.
- **Motion:** crossfade of glyph and label on state change, `DrylButton`'s
  press and hover spring; no conditional mount (all states stay mounted), so
  `DrylPresence` is not needed.
- **Keyboard/a11y:** native button, tooltip for the icon-only form, stable
  name, polite status region.
- **AI decision:** no `Ai` parameter. The copy is the user's own action on
  content; the surface that shows AI-authored content carries the aura, not
  the button beside it (`AI-05`).
- **Demo:** not yet — `DRYL.Website` is a separate repository and was not
  changed in this pass (`CODE-20`, open).
- **Catalog:** not yet registered in `DRYL.Website/Components/ComponentCatalog.cs`
  (`REL-04`, open).
- **Tests:** `tests/DRYL.Components.Tests/DrylCopyButtonTests.cs`.
