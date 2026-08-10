# Contributing to DRYL

Thanks for your interest in DRYL! This is a small, opinionated Blazor component
library with a strong design system. The bar for contributions is **visual and
API consistency** — please read this before opening a PR.

## Ground rules

DRYL has a single source of truth for *how* components are built:
[`CLAUDE.md`](CLAUDE.md). It applies to humans and AI agents alike. The short version:

- **Tokens, not literals.** Every color, spacing, radius, shadow, duration and
  easing references a CSS variable from [`code/DRYL.Components/wwwroot/dryl.css`](code/DRYL.Components/wwwroot/dryl.css).
  See [`harness/tokens.md`](harness/tokens.md).
- **Dark only.** No light theme, no `prefers-color-scheme` overrides.
- **Glass surfaces, accents glow.** Translucent layers; accent colors only as
  gradients, 1px borders, glow rings or small indicators.
- **One AI vocabulary.** AI-aware components use the shared `AiState`
  (`None / Active / Thinking / Streaming / Generated`) and the `.ai-aura*`
  primitives — never a per-component AI state.
- **Strongly-typed parameters.** `enum` for variants, never `string`.
- **Follow the API conventions.** Public parameter / event / enum / slot naming
  must match [`harness/conventions.md`](harness/conventions.md). These are frozen at 1.0.
- **Accessibility is not optional.** Keyboard-reachable, ARIA-labeled, visible
  focus rings; icon-only buttons always get a `DrylTooltip`.

If a value you need isn't in the design tokens, **don't invent it** — open an
issue proposing it as a new token.

## Development setup

Requires the **.NET SDK** (8, 9 and 10 SDKs for the full multi-target build;
the latest SDK is enough for day-to-day work).

```bash
# Restore + build everything
dotnet build DRYL.slnx -c Release

# Run the test suite (bUnit + xUnit)
dotnet test DRYL.slnx -c Release

# Run the demo / showcase app
dotnet run --project samples/DRYL.Components.Demo
```

## Workflow

1. **Open an issue first** for anything non-trivial (new component, API change).
2. Fork & branch from `main` (`feature/...` or `fix/...`).
3. Follow the component checklist in [`CLAUDE.md`](CLAUDE.md) §3 and the patterns
   in [`harness/patterns.md`](harness/patterns.md).
4. Add/extend tests under [`tests/DRYL.Components.Tests`](tests/DRYL.Components.Tests).
5. **Update the docs** — every change to library code updates
   [`CHANGELOG.md`](CHANGELOG.md) (`[Unreleased]`) and, if a component is new or
   its public API changed, the component table in [`README.md`](README.md).
   This is mandatory (see `CLAUDE.md` §7).
6. Ensure `dotnet build` and `dotnet test` pass. CI runs both on every PR.
7. Open the PR using the template; describe the change and link the issue.

## Commit messages

Conventional-commit style is appreciated: `feat:`, `fix:`, `docs:`, `refactor:`,
`test:`, `chore:`.

## Versioning

DRYL follows [Semantic Versioning](https://semver.org/). Maintainers set the
version and cut releases; contributors only write into the `[Unreleased]`
section of the changelog — never create a version section yourself. The full
release flow is documented in [`harness/releasing.md`](harness/releasing.md).

## License

By contributing, you agree that your contributions are licensed under the
project's [MIT License](LICENSE).
