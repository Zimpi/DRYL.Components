# DrylExpansion

## Meta
- **State:** Implemented
- **Source:** code/DRYL.Components/Components/Layout/DrylExpansion.razor

## User Story

As a Blazor developer, I want a collapsible panel that stacks with its neighbours
into one connected list and shows its content with a deliberate movement, so that
I can tuck detail away without writing any disclosure behaviour or motion myself.

## Description

`DrylExpansion` renders a header button and a body that opens and closes under it.
Adjacent panels share their borders and form one list; an open panel detaches from
its siblings with a gap and its own rounded corners. The body stays in the DOM while
closed, so its state survives.

Opening draws an accent seam along the header's lower edge, starting at the point of
the click, and a soft edge uncovers the body downwards from it. Under the mouse a short
light runs along the same edge
([`I42`](../../ideas/I42%20A%20panel%20that%20reveals%20its%20body.md)). A panel can
carry the shared AI vocabulary through `Ai`.

## Public API

Namespace: `DRYL.Components`.

| Member | Type | Default | Meaning |
|---|---|---|---|
| `Title` | `string?` | `null` | Header label. |
| `Icon` | `string?` | `null` | Leading icon name (see `DrylIcon`). |
| `Open` | `bool` | `false` | Whether the panel is expanded; supports `@bind-Open`. |
| `OpenChanged` | `EventCallback<bool>` | Unset | Fires when the user toggles the panel. |
| `Disabled` | `bool` | `false` | The panel cannot be toggled. |
| `ChildContent` | `RenderFragment?` | `null` | Body content. |
| `HeaderTrailingContent` | `RenderFragment?` | `null` | Slot between the title and the chevron, such as a badge. |
| `Ai` | `AiState` | `AiState.None` | AI ambient state on the panel. |
| `Aura` | `AiAura?` | `null` | Aura variant; `null` inherits a surrounding `DrylAiScope`. |
| `Class` | `string?` | `null` | Classes merged onto the root. |
| `AdditionalAttributes` | `IDictionary<string, object>?` | `null` | Captured unmatched attributes applied to the root. |

`DrylExpansion` implements `IDisposable`. `AiScope` is cascaded in from `DrylAiScope`
and used only to resolve the aura variant.

## Acceptance Criteria

### Structure

- The root renders with class `expansion`.
- `Class` is merged onto the root's class list.
- Unmatched attributes are applied to the root.
- The header renders as a `button` with class `expansion-header`.
- The header carries `aria-expanded` matching the open state.
- The header's `aria-controls` names the body's `id`.
- `Title` renders in `.expansion-title`.
- `Icon` renders a leading `.expansion-icon` only when set.
- `HeaderTrailingContent` renders in `.expansion-header-trailing` only when set.
- A click inside `.expansion-header-trailing` does not toggle the panel.
- The body renders as `.expansion-body` with `role="region"`, directly after the header.

### Open and closed

- A panel is closed by default.
- `Open="true"` adds `is-open` to the root.
- Clicking the header toggles the panel and fires `OpenChanged` with the new state.
- `Disabled` adds `is-disabled` to the root and sets `disabled` on the header.
- Clicking a disabled panel neither toggles it nor fires `OpenChanged`.
- A closed body carries `inert` and `aria-hidden="true"`.
- An open body carries neither.
- A closed body keeps its content in the DOM.

### Stacking

- Adjacent closed panels overlap by one border width.
- The first panel of a stack rounds its top corners and the last its bottom corners.
- A standalone panel rounds all four corners.
- An open panel takes `--sp-3` of margin above and below, rounds all four corners and
  carries `--shadow-md`.
- An open panel's border reads `--accent-line`.
- The sibling after an open panel drops its overlap.

### The seam and the reveal — I42

- Opening grows the body's `grid-template-rows` from `0fr` to `1fr` on `--dur-slow` and
  `--ease-out`; closing shrinks it on `--dur-med` and `--ease-out`.
- Opening draws a 1 px accent seam along the top of the body by `transform` on
  `--dur-slow` and `--ease-out`.
- The seam starts at the horizontal point of the click.
- A panel toggled from the keyboard starts its seam in the middle.
- The seam rests at an opacity of `0.75` with an accent glow.
- Closing withdraws the seam on `--dur-fast` and `--ease-out`.
- Opening uncovers the body downwards behind a soft edge `--sp-7` tall, on `--dur-slow`
  and `--ease-in-out`.
- Closing covers the body on `--dur-fast` and `--ease-out`.
- An open panel at rest has no mask, so glass nested in the body keeps its frost.
- A panel rendered open starts with no mask and plays no reveal.
- A browser without `transition-behavior: allow-discrete` shows the track's reveal
  without the soft edge.

### Chevron — I42

- The open chevron is rotated by 180° on `--dur-slow` and `--ease-spring`.
- The open chevron reads `--accent-fg` with an accent glow.
- Hovering a closed header's chevron reads `--fg-muted`.

### Hover light — I42

- Moving a mouse pointer over a header lights a 1 px line on its lower edge, centred
  on the pointer.
- The line follows the pointer by `transform` only.
- The line fades in and out with `--dur-med` and `--ease-out`.
- The line stays inside the header.
- Touch and pen input never light a header.
- Leaving the header clears its `data-spot` marker.
- Under `prefers-reduced-motion: reduce` no header is lit.

### Reduced motion

- Under `prefers-reduced-motion: reduce` the panel, body, seam, mask and chevron change
  state without a transition.

### AI

- An `Ai` state other than `AiState.None` adds the shared aura classes and renders
  `DrylAuraElements` in the root.
- A move into `AiState.Generated` plays the bloom once.
- A `null` `Aura` resolves through a surrounding `DrylAiScope`.

## Cross-cutting evidence (`SPEC-05`)

- **Both color modes:** every color comes from tokens (`--accent-a`, `--accent-b`,
  `--accent-line`, `--accent-fg`, `--line`, `--fg-muted`) with no mode selector.
  Verified in the browser on 2026-11-02 on the docs website in dark and light: seam,
  hover light, open border and chevron.
- **Enter/exit:** the body and seam animate in and out as above; the body stays
  mounted, so no `DrylPresence` applies. Verified in the browser on 2026-11-02: the
  seam's origin at the click and in the middle from the keyboard, the mask leaving
  after the reveal, and the closing state.
- **Keyboard and a11y:** the header is a native button, reached by Tab and toggled by
  Enter or Space, with `aria-expanded` and `aria-controls`. A closed body is `inert`
  (`UX-07`). Seam and light are decorative pseudo-elements.
- **AI decision:** yes. `Ai` and `Aura` drive the shared vocabulary; `DrylAuraElements`
  draws it. Verified on `/components/ai` on 2026-11-02 in the light mode: the aura wraps
  the open panel and a body growing while it streams is not cut off.
- **Demo:** `DRYL.Website/Components/Pages/DemoExpansion.razor`, route
  `/components/expansion`, with the examples under `Expansion/`.
- **Catalog:** `DRYL.Website/Components/ComponentCatalog.cs` — `Expansion`, slug
  `expansion`, group `Layout`, category `Layout`, AI flag `false` (`REL-04`).

## Recorded debt and scope limits

- **Scope of `Ai`:** `DrylExpansion` takes only the aura variant from a surrounding
  `DrylAiScope`, not its state. An explicit `Ai` is what lights a panel.
- **Stale seam origin:** a panel opened through its bound `Open` starts its seam from
  the last click on it, or the middle if there was none.
- **Header clip:** `overflow: clip` on the header keeps the light inside it, so
  `HeaderTrailingContent` that draws outside the header without portaling is cut off.
  DRYL's own floating surfaces portal and are not affected.
- **Literal value (`DESIGN-01`):** the seam's rest opacity of `0.75` is a literal.
- **Verification gaps:** the reveal is verified in the browser, not by an automated
  browser test. `tests/js/expansion.test.mjs` covers the pointer script and bUnit the
  render side (`tests/DRYL.Components.Tests/DrylExpansionTests.cs`).
