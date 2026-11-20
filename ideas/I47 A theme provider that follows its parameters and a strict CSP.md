# A theme provider that follows its parameters and a strict CSP

## Meta
- **State:** Adopted
- **Specs:** `specs/E1 Foundation/F5 DrylThemeProvider.md`; the consumer guide
  `harness/theming.md` (sections *Color mode* and *Content Security Policy*)

## Problem

Two findings from the `DrylThemeProvider` spec (`Recorded debt`), independent
of each other:

1. **Parameters are read once.** `Theme` and `Mode` are evaluated in
   `OnInitializedAsync` only. `<DrylThemeProvider Theme="@_theme" />` looks
   like a binding; changing `_theme` later does nothing, with no error. Runtime
   changes work only through `IDrylThemeService`.
2. **Inline script and style.** The provider emits an inline `<script>` (the
   pre-paint mode restore) and an inline `<style>` (the theme seeds on
   `:root`). An app with a Content Security Policy without `'unsafe-inline'`
   blocks both: no theme seeds at first paint, and the stored colour mode
   flashes in late.

## Solution Idea

### Part 1 — parameters

- **A — `Theme` follows, `Mode` stays a startup value.** A changed `Theme`
  reference calls `SetThemeAsync`, so the parameter is a real one-way binding.
  `Mode` keeps its meaning — "the mode to start in when the user has not
  chosen" — because the user's persisted choice wins by design, and a reactive
  `Mode` would fight `DrylColorModeToggle`. Its doc comment says so.
- **B — both follow.** `Mode` changes call `SetModeAsync` too. Then a parent
  re-render with the same `Mode` must not override the user's toggle, which
  needs change tracking and still leaves "who wins" unclear to the consumer.
- **C — neither follows; say so.** Doc comments and the theming guide state
  "startup only". No behaviour change.

Chosen: **A**.

The parameter is compared with the value last passed, not with the service's
current theme: a theme picked at runtime through `IDrylThemeService` survives
the next parent render that passes the unchanged parameter. `DrylTheme` is a
record, so the comparison is by value and an equal theme built anew on every
render is not applied again.

### Part 2 — CSP

- **A — a `Nonce` parameter.** The provider stamps `nonce="…"` on its `<script>`
  and `<style>`. The app passes the nonce its CSP middleware already creates.
  Standard, small, additive.
- **B — no inline code.** The pre-paint script becomes a static file the app
  references in `<head>`; the seeds are applied by JS after load. Works under
  any CSP, but a non-default theme then flashes the default look on first
  paint, and the app has one more tag to remember.
- **C — document only.** The theming guide names the two hashes or
  `'unsafe-inline'` as the requirement.

Chosen: **A**, plus one section in the theming guide.

## Scope
- **In scope:** `DrylThemeProvider` — `Theme` reactivity, the meaning of
  `Mode`, CSP compatibility of its two inline elements.
- **Out of scope:** the other recorded debt of the provider (semantic colours
  that do not glide, the single-subscriber event, the pre-paint guarantee
  without prerender, the storage contract written twice); other components'
  inline output.

## Impact
- **Harness:** no new token, animation, `AiState` or dependency.
- **Specs:** `specs/E1 Foundation/F5 DrylThemeProvider.md`.
- **Public API:** with CSP way A, a `Nonce` parameter (`string?`) — additive.
  Part 1 changes behaviour of an existing parameter, no signature.
- **Code:** `DrylThemeProvider.razor` (`OnParametersSetAsync`, the two inline
  elements); `harness/theming.md`. Risk in part 1: a consumer who passes a new
  `DrylTheme` instance on every render (`Theme="@(new DrylTheme { … })"`) would
  re-apply the theme on every parent render — equality is by reference.

## Decisions
- 2026-11-20: Part 1 way **A** — `Theme` follows its value, `Mode` stays a
  startup value (Product Owner, on the Tech Lead's recommendation).
- 2026-11-20: Part 2 way **A** — a `Nonce` parameter (Product Owner, on the
  Tech Lead's recommendation). Whether a consumer runs a strict CSP today was
  left unanswered; the parameter is additive and costs nothing when unset.
- 2026-11-20: Themes are compared by value — `DrylTheme` is a sealed record,
  checked in `code/` — as recommended for that case (Product Owner). With all
  three recommendations accepted and the feasibility check raising nothing
  new, the Tech Lead took that answer as the confirmation of the final
  version.
- 2026-11-20: Detail settled by the Tech Lead during implementation: the
  comparison is against the parameter last passed, so a runtime theme is not
  reverted by a parent render.

## Open Points
