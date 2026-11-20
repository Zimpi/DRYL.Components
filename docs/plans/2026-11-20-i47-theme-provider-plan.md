# I47 — A theme provider that follows its parameters and a strict CSP: plan

Idea: [`ideas/I47 A theme provider that follows its parameters and a strict CSP.md`](../../ideas/I47%20A%20theme%20provider%20that%20follows%20its%20parameters%20and%20a%20strict%20CSP.md) (part 1 way A, part 2 way A).
Target: `release/3.17.0`. One task, one commit — one component.

## Task

Files:
- `code/DRYL.Components/Components/Providers/DrylThemeProvider.razor`
  - `OnParametersSetAsync`: a `Theme` that differs (by value) from the
    parameter last passed is set on the service. The last parameter is kept in
    `_appliedParameter`, so a runtime theme is not reverted by a parent render.
  - `Nonce` parameter: `nonce` attribute on the `<style>`; HTML-encoded into
    the pre-paint `<script>` tag.
  - `Mode` doc comment states "startup value".
- `tests/DRYL.Components.Tests/Theming/DrylThemeProviderTests.cs` — changed
  parameter applied; runtime theme survives; equal new instance not re-applied;
  changed `Mode` ignored; nonce absent, present, and encoded.
- `specs/E1 Foundation/F5 DrylThemeProvider.md`, `harness/theming.md`,
  `CHANGELOG.md` (`3.17.0` Added and Changed).

Verify: `dotnet test DRYL.slnx -c Release --filter "FullyQualifiedName~DrylThemeProvider"`,
then the full evidence list of `CLAUDE.md` stage 5.

Not verified by this plan: a real browser under an enforced CSP header.
