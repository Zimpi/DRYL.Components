# DrylCanvas

## Meta
- **State:** Implemented
- **Source:** code/DRYL.Components/Components/AI/DrylCanvas.razor
              code/DRYL.Components/Components/AI/DrylCanvas.razor.css

## Description

`DrylCanvas` renders a `CanvasSpec` as a live tree of curated DRYL components —
charts, tables, forms, KPIs. It is the surface an AI writes onto, but it is
deliberately dumb about that: a spec goes in, interactions come out, and the
component knows nothing about where the spec came from. Code, a database, a saved
document and an AI generation are all the same to it. The AI-facing wrapper is
`DrylAiCanvas` in the agents package, which adds the run, the aura and the
streaming choreography on top.

The component owns the spec while it is mounted. That ownership is what makes it
the only place a patch may be applied, the only place that can resolve "one step
up" or "duplicate this" into a node, and the only place that sees the whole tree
— which is why direct manipulation and the reorder gesture live here rather than
in the node views.

Five aspects, each with its own acceptance criteria:

| Aspect | Subject |
|---|---|
| [`S1 Rendering.md`](S1%20Rendering.md) | The header, the body, the spec tree, the empty and error states, purging |
| [`S2 AI states.md`](S2%20AI%20states.md) | `Ai`, the build line, and what the state does *not* do here |
| [`S3 Data binding.md`](S3%20Data%20binding.md) | Bound data sources, refresh, the pulse tracker |
| [`S4 Interaction.md`](S4%20Interaction.md) | Selection, node commands, reorder, the callbacks |
| [`S5 Layout and expand.md`](S5%20Layout%20and%20expand.md) | Fullscreen, the morph, width reporting |

This component is split under `SPEC-02`, which names it as one of the library's
three split candidates. `Source` stays here at the component level; the `S{n}`
files carry a `State` and no `Source`.

## Public API

| Member | Type | Default | Purpose |
|---|---|---|---|
| `Spec` | `CanvasSpec?` | `null` | The artifact to render. May be mutated in place by its owner. |
| `Ai` | `AiState` | `AiState.None` | The opt-in (`AI-03`). Shows the build line and renders not-yet-valid nodes as waiting skeletons. |
| `State` | `AiState` | — | **Obsolete.** Delegating alias for `Ai`; removed in `3.0.0`. See `_Api.md`. |
| `Error` | `string?` | `null` | A fatal error for the whole artifact; rendered instead of the tree. |
| `Announcement` | `string?` | `null` | Text announced through the canvas's `aria-live` region. |
| `EmptyText` | `string?` | `"Nothing to show yet."` | Message shown when there is no artifact. |
| `Epoch` | `int` | `0` | Bump to reset interactive form state when a fresh artifact recycles field names. |
| `Pulse` | `CanvasPulseTracker?` | `null` | The change-pulse stamps to render. Without one the canvas owns its own. |
| `Selection` | `CanvasSelection?` | `null` | Opt-in for direct manipulation. Without it nothing about the canvas changes. |
| `AllowExpand` | `bool` | `true` | Whether the header offers expand-to-fullscreen. |
| `HeaderTools` | `RenderFragment?` | `null` | Extra controls in the header's tool row. |
| `Overlay` | `RenderFragment?` | `null` | Absolutely-positioned decoration as the first child of the root. |
| `OnInteraction` | `EventCallback<CanvasInteraction>` | — | A button intent fired inside the artifact. |
| `OnAction` | `EventCallback<CanvasActionOutcome>` | — | Raised after every completed action button, successful or not. |
| `OnEdit` | `EventCallback<CanvasEdit>` | — | Raised after every completed direct manipulation. |
| `OnPurge` | `EventCallback<string>` | — | Raised with a node id once its exit animation finished. |
| `OnWidthChanged` | `Action<int>?` | `null` | The measured usable body width in CSS px, past a deadband. |
| `Class` | `string?` | `null` | Extra CSS class(es) merged onto the root. |
| `AdditionalAttributes` | `IDictionary<string, object>?` | `null` | Pass-through attributes on the root. |

`Context` (`CanvasContext`) is exposed as a public property so a wrapper such as
`DrylAiCanvas` can read the live field values.

`OnWidthChanged` is a plain `Action<int>` rather than an `EventCallback` on
purpose: an `EventCallback` bound to a component re-renders it, and dragging a
window must never re-render the artifact tree. Nothing on screen depends on the
value.

## Cross-cutting evidence (`SPEC-05`)

- **Both color modes** — token-only styling, verified by
  `node scripts/check-light-sync.mjs` and
  `node scripts/validate-light-contrast.mjs`; the component defines no
  mode-specific rule.
- **Enter/exit animation** — the build line through `DrylPresence`, node reflow
  through `dryl.motion.autoFlip`, and the expand morph through
  `IDrylMorph`. See `S2` and `S5`.
- **Keyboard and a11y** — two `aria-live` regions, <kbd>Escape</kbd> to collapse,
  and the selection's keyboard model. See `S1`, `S4` and `S5`.
- **AI mode** — yes, and it is an opt-in: the parameter is a switch on a tree
  that renders in full without it, so `AI-03` requires it to be called `Ai`. Note
  that the canvas carries no aura — see `S2` for why.
- **Demo page** — `DRYL.Website/Components/Examples/Canvas/CatalogTypes.razor`,
  `.../Canvas/DataBinding.razor` and `.../Canvas/Actions.razor`.
- **`ComponentCatalog`** — registered as `"Canvas"` / `canvas` in
  `DRYL.Website/Components/ComponentCatalog.cs`.
