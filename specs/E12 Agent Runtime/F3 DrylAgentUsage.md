# DrylAgentUsage

## Meta
- **State:** Implemented
- **Source:** code/DRYL.Components.Agents/Agents/DrylAgentUsage.razor
              code/DRYL.Components.Agents/Agents/DrylAgentUsage.razor.css

## User Story

As a Blazor developer running an agent through `DrylAgentRunner`, I want the
run's token usage to appear as a quiet row of badges as soon as the provider
reports it, so that I can show cost and size of an answer without parsing the
update stream myself.

## Description

`DrylAgentUsage` watches a `DrylRunBase` and renders its `Usage` — a
`DrylRunUsage` summed from every `UsageContent` the update stream delivers (see
[`_Api.md`](_Api.md)) — as up to three `DrylBadge`s: prompt, completion and
total. The row fades in through `DrylPresence` once the first numbers arrive;
a provider that never reports usage leaves the component invisible.

Numbers are formatted compactly and culture-invariantly: below one thousand as
an integer, then with one optional decimal and a `k` or `M` suffix, so a German
locale cannot turn `1.2k` into `1,2k`.

`DrylHandoffTrace` composes it once per lane; a `DrylMultiAgentRun` sums its
steps' usage into its own `Usage`, so the same component also shows a flow's
total.

## Public API

Namespace: `DRYL.Components.Agents`.

| Member | Type | Default | Purpose |
|---|---|---|---|
| `Run` | `DrylRunBase?` | `null` | The run whose token usage to display. Nothing renders while it is `null` or its `Usage` is `null`. |

## Acceptance Criteria

### Rendering

- The component renders no row while `Run` is `null`.
- The component renders no row while `Run.Usage` is `null`.
- Once `Run.Usage` is set, the component renders one `.agent-usage` row.
- The row renders a "Prompt" badge exactly when `InputTokens` has a value.
- The row renders a "Completion" badge exactly when `OutputTokens` has a value.
- The row renders a "Total" badge exactly when `TotalTokens` has a value.
- The badges appear in the order prompt, completion, total.
- The "Total" badge uses `DrylBadge.BadgeKind.Accent`; the other two use the
  default badge kind.
- Each badge renders its label in `.agent-usage-label` before the number.

### Number format

- A value below 1 000 renders as an integer without separators (`999`).
- A value from 1 000 below 1 000 000 renders in thousands with at most one
  decimal and a `k` suffix (`1500` → `1.5k`, `2000` → `2k`).
- A value from 1 000 000 renders in millions with at most one decimal and an
  `M` suffix (`3400000` → `3.4M`).
- The decimal separator is a point under every UI culture.

### Live updates

- The component re-renders when the run raises `OnChange`, so usage arriving
  on the final chunk appears without a parent render.
- The component unsubscribes from the previous run's `OnChange` when `Run`
  changes to another instance.
- The component unsubscribes from the run's `OnChange` when it is disposed.

### Motion

- The row enters through `DrylPresence` with `PresenceTransition.Fade`,
  including on first render (`Appear`).

### Keyboard and accessibility

- The row carries `role="status"` and `aria-live="polite"`, so arriving and
  growing numbers are announced without interrupting.
- The row carries `aria-label="Token usage"`.
- The component renders no focusable element.

### Appearance

- The row wraps, centres its badges vertically and spaces them by `--sp-2`.
- Each label reads in `--fg-dim`, separated from its number by `--sp-1`.

## Recorded debt

- **Labels are fixed English.** "Prompt", "Completion", "Total" and the row's
  `aria-label` "Token usage" are not parameters.
- **The exit is empty.** As in
  [`F2 DrylAgentError.md`](F2%20DrylAgentError.md), the row sits under an
  `@if` inside the `DrylPresence`, so passing a new run without usage removes
  it at once and the fade-out plays on an empty wrapper (`DESIGN-12`).
- **Every usage update re-announces the row.** Usage reported in several
  chunks makes the live region speak once per chunk.

## Cross-cutting evidence (`SPEC-05`)

- **Both color modes** — token-only colors, composed of `DrylBadge`; inspected on `/components/run-health`, with usage badges shown, in dark and light (2026-11-20).
- **Enter/exit animation** — enter yes: `DrylPresence` with
  `PresenceTransition.Fade`; the exit runs on an empty wrapper, recorded above.
- **Keyboard and a11y** — the "Keyboard and accessibility" criteria above.
- **AI mode** — **no** `Ai` parameter (`AI-05`). Usage is bookkeeping read off
  a run, not a surface the AI works on; the run's state is shown by the
  components that render its text and tool calls.
- **Demo page** — `/components/run-health`
  (`DRYL.Website/Components/Pages/DemoRunHealth.razor`), example
  `Agents/UsageTracking`.
- **`ComponentCatalog`** — the `"Run Health"` / `run-health` entry in
  `DRYL.Website/Components/ComponentCatalog.cs`.
- **Tests** — `tests/DRYL.Components.Tests/Agents/DrylAgentUsageTests.cs`.
