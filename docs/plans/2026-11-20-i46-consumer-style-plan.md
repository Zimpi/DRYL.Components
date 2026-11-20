# I46 — A consumer style that adds to the layout: plan

Idea: [`ideas/I46 A consumer style that adds to the layout.md`](../../ideas/I46%20A%20consumer%20style%20that%20adds%20to%20the%20layout.md) (way B — a typed `Style` parameter).
Target: `release/3.17.0`, worked directly on the release branch. One task, one
commit: the eleven components share one helper and one contract, and a partial
rollout would leave the convention with known exceptions.

## How it works

`InlineStyle.Merge(own, consumer)` (internal, `code/DRYL.Components/InlineStyle.cs`)
trims both sides, drops trailing semicolons and joins them — the component's
declarations first, the consumer's last. It returns `null` when both are empty,
so no empty `style` attribute is written.

Each component gains `[Parameter] public string? Style`. Blazor matches
parameter names case-insensitively, so a `style` attribute binds to it and no
longer lands in `AdditionalAttributes`, where the splat would replace the
component's own style.

## Task — library, tests, specs, harness

Files:
- `code/DRYL.Components/InlineStyle.cs` — new.
- `Components/Layout/DrylStack.razor`, `DrylGrid.razor`, `DrylContainer.razor`,
  `DrylAspectRatio.razor`, `DrylSpacer.razor`, `DrylScrollArea.razor`,
  `DrylTypo.razor` — the private `Style` becomes `OwnStyle`; the root writes
  `InlineStyle.Merge(OwnStyle, Style)`.
- `Components/Layout/DrylDrawer.razor`, `DrylLayout.razor` — merge with
  `StyleVars`. `Components/Data/DrylCalendar.razor` — with `RootStyle`.
  `Components/Data/DrylImage.razor` — with `WrapStyle`.
- `tests/DRYL.Components.Tests/ConsumerStyleMergeTests.cs` — the helper's
  cases; per component: own declarations survive and the consumer's come last;
  the attribute binds to `Style` and never reaches the splat.
- The eleven specs: `Style` row, three criteria, the debt line removed.
- `harness/conventions.md` — the convention beside the merged `Class`.
- `CHANGELOG.md` — `3.17.0` Added and Fixed.

Verify: `dotnet test DRYL.slnx -c Release --filter "FullyQualifiedName~ConsumerStyleMerge"`,
then the full evidence list of `CLAUDE.md` stage 5; in the browser, a
`DrylStack` with `style="margin-top: …"` keeps `display: flex`.
