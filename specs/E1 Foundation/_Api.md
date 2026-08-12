# Foundation — Public API

The public surface that belongs to no single component: theming types, the DI
registration, the AI state vocabulary, the motion primitives, and the token
surface consuming apps are allowed to override.

**Source folder:** `code/DRYL.Components/Components/Providers/`

Foundation is the one category whose subject is not a family of widgets but the
library's own footing — and the five components in that folder are exactly that:
the plumbing a consumer mounts once in the layout rather than places on a page
(`DrylThemeProvider`, `DrylToastProvider`, `DrylPresence`, `DrylReconnectModal`,
`DrylColorModeToggle`). They moved here from `Components/Surfaces/` on
2026-08-11; the reasoning is in `ideas/I3 Component folder layout.md`.

Alongside them this category still documents the surface that belongs to no
component at all — which is why the sections below exist. This file carries no
`Meta` block: it is a reference for the specs around it, not a unit of
implementation.

*Scaffold. The shared types below are filled in during phase C, each listed with
the exact spelling used in code. Until then this file claims nothing.*

## Theming

*(phase C)*

## AI state vocabulary

*(phase C)*

## Motion primitives

*(phase C)*

## Token surface

*(phase C — the consumer-overridable custom properties, cross-referenced to
[`../../harness/tokens.md`](../../harness/tokens.md))*
