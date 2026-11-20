# An AI command palette that speaks your language and can be seen

## Meta
- **State:** Draft

## Problem

`DrylAiCommandPalette` wraps `DrylCommandPalette` with the DI-registered
resolver. Three gaps remain after its German defaults were removed:

1. **Its words cannot be translated.** The core palette has `Texts`
   (`CommandPaletteTexts`) for every word of its chrome — the thinking pill,
   the back and run buttons, the labels. The wrapper does not pass it on, so an
   app in another language can set only `Placeholder` and `EmptyText`.
2. **It forwards a fraction of the core palette.** `Items`, `SearchProvider`,
   `Debounce`, `GroupOrder`, `Fallback`, `OnItemRun`, `AriaLabel`, `Ai`, `Aura`
   and `AiContent` are not reachable; a consumer who needs one must drop the
   wrapper and wire `Resolver` by hand.
3. **Nobody can see it.** It has no demo and no `ComponentCatalog` entry; the
   resolver example on `/components/command-palette` uses the core palette with
   a demo resolver.

## Solution Idea

- **A — `Texts` only, plus demo and catalog.** Add `Texts` to the wrapper, show
  the wrapper in an example on the command-palette page, register it in the
  catalog. Smallest step that closes gaps 1 and 3.
- **B — the wrapper forwards everything.** `Texts` and all ten parameters of
  gap 2, so the wrapper is the core palette plus the resolver and never has to
  be abandoned. Plus demo and catalog. More API surface on the wrapper, kept in
  step with the core palette by a test that compares the two parameter lists.
- **C — demo and catalog only.** The wrapper stays a convenience for the simple
  case; whoever needs more uses the core palette. Gap 1 stays.

Recommendation: **B**. A wrapper that forwards part of its inner component
always meets a consumer one parameter short; the comparison test keeps it from
drifting again.

## Scope
- **In scope:** `DrylAiCommandPalette` parameters; an example and a catalog
  entry in `DRYL.Website`.
- **Out of scope:** the session factory of `AddDrylCommandResolver`; the
  constant `Confidence`; any change to `DrylCommandPalette` itself.

## Impact
- **Harness:** no new token, animation, `AiState` or dependency.
- **Specs:** `specs/E15 Agent Inputs/F2 DrylAiCommandPalette.md`.
- **Public API:** new parameters on `DrylAiCommandPalette` — one with A, eleven
  with B. Additive; `Agents 0.25.1` is unpublished, but new parameters make it
  a MINOR: the version becomes `0.26.0` (`REL-01`).
- **Code:** `DrylAiCommandPalette.razor`; a bUnit test. The demo and the
  catalog entry live in the `DRYL.Website` repository and ship in its own pull
  request — they need the new package or the project reference.

## Decisions

## Open Points
- A, B or C?
