# DrylTab

## Meta
- **State:** Implemented
- **Source:** code/DRYL.Components/Components/Navigation/DrylTab.razor

## User Story

As a Blazor developer building a tabbed view, I want to declare each tab — its
identifier, label, icon, badge, AI state and content — as one child element of
`DrylTabs`, so that the strip and the panel follow my declarations without my
keeping a separate list in sync.

## Description

`DrylTab` is a declaration, not a widget. It renders **no markup of its own**:
on initialisation it registers with the `DrylTabs` it receives as a cascading
value, and `DrylTabs` draws its header in the strip and, while it is active, its
`ChildContent` in the panel ([`F8 DrylTabs.md`](F8%20DrylTabs.md)). Placed
anywhere else it throws.

**The identifier is fixed at birth.** A non-empty `Id` becomes the tab's
identifier — the value `DrylTabs.Active` and `ActiveChanged` speak. Without one,
the tab gets a generated identifier. Either is read once, in `OnInitialized`.

**AI state lights one header.** `Ai` marks this tab's header — not the strip,
not the panel — with an accent tint and a rotating gradient ring drawn by a
dedicated `.tab-comet` child, kept inside the header's rounded bounds. Each
state sets the ring's tempo; `AiState.Proposed` stops it and adds the visually
hidden proposal description.

## Public API

Namespace: `DRYL.Components`. Implements `IDisposable`.

| Member | Type | Default | Purpose |
|---|---|---|---|
| `Id` | `string?` | `null` | Stable identifier; the value `@bind-Active` on the parent uses. |
| `Label` | `string?` | `null` | Header text. |
| `Icon` | `string?` | `null` | `DrylIcon` name rendered before the label. |
| `Disabled` | `bool` | `false` | The header is shown but cannot be activated, and keyboard navigation skips it. |
| `Ai` | `AiState` | `AiState.None` | AI state shown on this tab's header. |
| `TrailingContent` | `RenderFragment?` | `null` | Slot after the label, e.g. a counter badge. |
| `ChildContent` | `RenderFragment?` | `null` | The panel body, rendered by the parent while this tab is active. |

The parent reference is a private `[CascadingParameter]` and not part of the
public API.

## Acceptance Criteria

### Registration

- `DrylTab` renders no element of its own.
- A `DrylTab` outside a `DrylTabs` throws `InvalidOperationException` on
  initialisation.
- On initialisation the tab registers with its parent once.
- On disposal the tab unregisters from its parent.
- A non-empty `Id` is the tab's identifier.
- With `Id` `null` or empty, the tab has a generated identifier.
- A change of `Id` after initialisation does not change the identifier.
- A change of `Ai`, `Disabled`, `Label` or `Icon` after initialisation
  re-renders the parent.

### Header content

- The header renders `Label` in a `span`.
- With `Icon` set, the header renders a `DrylIcon` carrying `.tab-icon` before
  the label.
- With `Icon` `null` or empty, the header renders no icon.
- With `TrailingContent` set, the header renders it after the label inside a
  `span.tab-trailing`.
- With `TrailingContent` `null`, no `.tab-trailing` element is rendered.
- `ChildContent` is rendered in the parent's panel while this tab is active,
  and nowhere otherwise.
- An icon reads at reduced opacity and reaches full opacity on hover and while
  the tab is active, on `--dur-fast` / `--ease-out`.

### Disabled

- `Disabled` defaults to `false`.
- A disabled tab's header carries the native `disabled` attribute and
  `.is-disabled`.
- A disabled tab is never the active tab.

### AI state

- `Ai` defaults to `AiState.None`.
- With `Ai` `AiState.None`, the header carries no `ai-*` class and no
  `.tab-comet`.
- With any other `Ai`, the header carries `ai-aura` and renders one
  `span.tab-comet` carrying `aria-hidden="true"`.
- `AiState.Thinking`, `AiState.Streaming`, `AiState.Generated` and
  `AiState.Proposed` add `ai-thinking`, `ai-streaming`, `ai-generated` and
  `ai-proposed` respectively; `AiState.Active` adds no state class.
- An AI header's text is `--accent-fg` over a `--accent-soft` fill.
- The ring is drawn from `--accent-a` and `--accent-b` and rotates on the
  shared `ai-comet-spin` keyframes.
- The ring rotates faster under `AiState.Thinking` than under
  `AiState.Streaming`, and slower under `AiState.Generated` than under
  `AiState.Streaming`.
- Under `AiState.Proposed` the ring stands still.
- Under `AiState.Proposed` the header contains the visually hidden text
  "AI suggestion, awaiting your decision"; no other header does
  (`tests/DRYL.Components.Tests/AiProposedTests.cs`).
- The ring takes no pointer events.
- An active AI tab still shows the active underline above the ring.
- Under `prefers-reduced-motion: reduce`, the ring does not rotate and reads
  at reduced opacity.

## Recorded debt

- **A hand-drawn aura (`AI-02`, `AI-04`).** The header composes neither
  `DrylAuraElements` nor `AiAuraCss`: `.tab-comet` is a tab-only ring with its
  own literal tempos (`6s`, `1.8s`, `3s`, `4s` on `.tab-comet::before`) and
  opacities, instead of the shared `.ai-aura-*` layers and the
  `--aura-comet-dur` periods. The `AiProposedSweepTests` allow-list accounts
  for `DrylTab.razor` as "rendered by DrylTabs, which maps Proposed".
- **`Generated` is never retired (`AI-07`).** No `AuraLifecycle` is composed,
  so a tab set to `AiState.Generated` keeps its ring until the host changes the
  state, and there is no graceful exit from any state — the ring disappears at
  once when `Ai` returns to `None`.
- **No announcement of AI activity (`UX-04`).** A change of `Ai` on a tab is
  not announced through any `aria-live` region.
- **No `Aura` parameter and no `AiScope`.** The tab does not consume the
  surrounding `AiScope` and offers no Comet/Aurora choice, unlike the
  library's shared-aura hosts.
- **Order follows registration, not markup.** A tab mounted after the first
  render is appended to the end of the strip (see `F8 DrylTabs`).
- **`TrailingContent` and `ChildContent` changes do not notify the parent.**
  Only `Ai`, `Disabled`, `Label` and `Icon` are compared in
  `OnParametersSet`; the two fragments rely on the parent re-rendering for
  other reasons.
- **No accessible name for an icon-only tab (`UX-05`).** The icon is
  decorative (`aria-hidden`) and the tab offers no `AriaLabel`, so a tab with an
  `Icon` and no `Label` is announced without a name.
- **Literal sizes.** The icon is rendered at a literal `Size="14"`;
  `.tab-trailing` has a literal `margin-left: 2px`; `.tab-comet` has literal
  `2px` insets and a `1px` ring width (`DESIGN-01`).

## Cross-cutting evidence (`SPEC-05`)

- **Both color modes** — EYE-CHECK-PENDING on /components/tabs
- **Enter/exit animation** — `DrylTab` renders nothing, so it has nothing to
  mount or animate itself. Its header's state changes — hover, active, AI ring
  — are animated by `DrylTabs`' styles; switching to it slides its panel in.
  The ring's missing graceful exit is recorded above.
- **Keyboard and a11y** — the header is a native `button role="tab"` rendered
  by `DrylTabs`, keyboard model there; the ring is `aria-hidden`, and a
  proposal is described in visually hidden text. Gaps recorded above and in
  `F8 DrylTabs`.
- **AI mode** — **yes.** `Ai` of type `AiState`, defaulting to `AiState.None`
  (`AI-03`). One view behind one tab is a place an AI can be working — a
  "Copilot" tab while its answer streams — and the header is a bounded surface
  of its own that the ring can trace (`AI-08`).
- **Demo page** — `/components/tabs`; `Tabs/AiSingle` cycles one tab through
  all six `AiState` values, `Tabs/Icons`, `Tabs/Trailing` and `Tabs/Disabled`
  show the other parameters.
- **`ComponentCatalog`** — no row of its own, being usable only inside
  `DrylTabs`; covered by the `"Tabs"` / `tabs` entry (AI flag `true`).
- **Tests** — `tests/DRYL.Components.Tests/AiProposedTests.cs`,
  `tests/DRYL.Components.Tests/DrylTabsGlideTests.cs`; the registration
  lifecycle has no test.
