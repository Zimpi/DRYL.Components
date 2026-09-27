# A copy button

## Meta
- **State:** Adopted
- **Specs:** [`specs/E2 Actions/F4 DrylCopyButton.md`](../specs/E2%20Actions/F4%20DrylCopyButton.md),
  [`specs/E2 Actions/_Api.md`](../specs/E2%20Actions/_Api.md),
  [`specs/E2 Actions/_Interop.md`](../specs/E2%20Actions/_Interop.md)

## Problem

Copying a value — a phone number, an id, a link — is a small action that every
application builds again: a button, a call into the clipboard, a "copied"
state, a timer that resets it, a failure path when the browser refuses. DRYL
already does all of that, but only inside `DrylCodeBlock`, where it is welded
to the code surface and its English labels. A host that wants the same for a
single value writes it again, usually without the failure path and without an
announcement for screen readers.

Target role: the Blazor developer — concretely the El Destino Hub's contact
book, where each phone number carries a copy button. Secondarily the end user,
who needs to see and hear that the copy worked.

## Solution Idea

A new action component, `DrylCopyButton`, that wraps `DrylButton` and the
existing `dryl.clipboard.copy` helper.

- `Text` is what is copied. `Label` is the visible text; without it the button
  is icon-only, named by `AriaLabel` (fallback `"Copy"`) and wrapped in a
  `DrylTooltip` (`UX-05`).
- On success the `Copy` glyph crossfades to `Check`; a visible label
  crossfades to `CopiedLabel`. On failure the glyph becomes `Alert` in
  `--danger` and the label `FailedLabel`. After about two seconds it returns.
- The result is announced through a polite live region, so the accessible name
  of the button itself stays stable.
- `OnCopied` fires after a successful copy.
- `Variant` and `Size` reuse `DrylButton.ButtonVariant` / `ButtonSize`,
  defaulting to `Ghost` / `Small`.

## Scope

- **In scope:** the component, its crossfade, announcement, failure path,
  spec, tests, changelog.
- **Out of scope:** copying rich content (HTML, images); a configurable reset
  delay; replacing the copy button inside `DrylCodeBlock` (possible later, but
  it would change that component's labels); an `Ai` parameter.

## Impact

- **Harness:** no new token, duration, easing, `AiState` or dependency. Icons
  `Copy`, `Check` and `Alert` exist; the clipboard helper exists in `dryl.js`.
  The crossfade uses `--dur-med`, `--ease-out` and `--ease-spring`.
- **Specs:** new `specs/E2 Actions/F4 DrylCopyButton.md`; `_Api.md` and
  `_Interop.md` of E2 gain the component; `harness/requirements.md` SPEC-02
  table: E2 3 → 4, total 129 → 130.
- **Public API (new component, MINOR):** `Text`, `Label`, `AriaLabel`,
  `CopiedLabel`, `FailedLabel`, `Variant`, `Size`, `OnCopied`, `Class`,
  `AdditionalAttributes`.
- **Code:** `Components/Actions/DrylCopyButton.razor` + `.razor.css`. First
  component of the Actions category that uses JS interop. Risks: the reset
  timer must be cancelled on disposal and on a second press (`CODE-05`); the
  tooltip bubble does not re-read its text while it is shown, so the live
  region — not the tooltip — carries the announcement.

## Decisions

- 2026-09-27 — Jan: a copy button is added to DRYL ("DRYL darfst du auch
  einbauen"); the public API names and defaults were given by him.
- 2026-09-27 — Tech Lead: the component composes `DrylButton` rather than
  emitting its own `button`, so variants, sizes, press spring and focus ring
  are the library's.
- 2026-09-27 — Tech Lead: `AriaLabel` defaults to `null` and falls back to
  `"Copy"` only when there is no `Label`. A default of `"Copy"` would override
  a localised visible label as the accessible name.
- 2026-09-27 — Tech Lead: no `Ai` parameter (`AI-05`). The copy is the user's
  own action on content; the surface that shows AI-authored content carries
  the aura, not the button beside it.
- 2026-09-27 — Jan confirmed the scope through the task brief; the idea is
  `Ready` and was carried into the spec the same day (`Adopted`).

## Open Points
