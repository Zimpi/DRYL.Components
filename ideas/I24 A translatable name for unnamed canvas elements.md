# A translatable name for unnamed canvas elements

## Meta
- **State:** Adopted
- **Specs:** [`specs/E3 AI/F3 DrylCanvas/S4 Interaction.md`](../specs/E3%20AI/F3%20DrylCanvas/S4%20Interaction.md),
  [`specs/E3 AI/F3 DrylCanvas/S2 AI states.md`](../specs/E3%20AI/F3%20DrylCanvas/S2%20AI%20states.md),
  [`specs/E3 AI/F8 DrylCanvasWorkspace.md`](../specs/E3%20AI/F8%20DrylCanvasWorkspace.md)
- **Origin:** Trello #76 (split off from #75)

## Problem

`CanvasLabel.For` names a node by the first non-blank of `title`, `label`,
`text`, `submitLabel`, `name`, `content`. A node with none of them falls back to
a name built from its catalog type: `lineChart` → "Line chart", `keyValue` →
"Key value", no type → "Element". That fallback is English and cannot be
replaced. It reaches four places:

- the selection announcement (`CanvasTexts.Selected(label, type)`),
- the node's `aria-label` (`CanvasNodeView.WrapperAriaLabel`),
- `CanvasEdit.Label` (via `CanvasTexts.PinEdit` & co.), and with it the
  version memos in `DrylCanvasWorkspace`,
- `CanvasSelection.Label`, which the dock shows in its context chip and writes
  into the prompt to the model
  (`Regarding the artifact element "{id}" ({Type}, "{Label}")`).

Checking the code widened the card's premise in three ways:

1. **The raw type id is spoken, too.** The `aria-label` reads
   `"{label}, {Node.Type}"`, `Selected` receives `Node.Type`, and the caption
   under a streaming node reads `WaitingFor(Node.Type)` — the catalog id
   `lineChart`, not even a readable English word. A German screen-reader user
   hears "Umsatz, lineChart" with every text translated.
2. **`DrylCanvasWorkspace` hard-codes its chrome.** "Undo", "Redo",
   "Version history", the announcements "Undone: …", "Redone: …",
   "last change", "Restored version {n} of {m}", the tab close label
   "Close {title}" and the default commit label "Version {n}" are neither
   parameters nor part of `CanvasTexts`.
3. **The workspace's own body canvas cannot be translated.** Without a `View`
   slot it renders `<DrylCanvas Spec=… EmptyText=…>` and passes no `Texts`.

(`DrylAiCanvas` and `DrylCanvasDock` already expose their words as parameters.)

Target role: a Blazor developer who ships a canvas in a non-English application
and has translated `CanvasTexts` (3.9.0) — and still finds English in the
announcements, the version bar and the dock chip.

## Solution Idea

**One readable type name, everywhere a human meets the type.**
`CanvasTexts.TypeName` (`Func<string?, string>`) turns a catalog type id into
the name a human reads; its default is today's splitting (`lineChart` →
"Line chart", null or blank → "Element"). A host translates it like every other
member and can fall back to `CanvasTexts.Default.TypeName(type)` for ids it does
not map:

```csharp
Texts="@(CanvasTexts.Default with {
    TypeName = t => t switch { "lineChart" => "Liniendiagramm", _ => CanvasTexts.Default.TypeName(t) } })"
```

It is used in five places:

- the fallback of `CanvasLabel.For` — via a new overload
  `CanvasLabel.For(CanvasNode, Func<string?, string>)`; the existing
  `For(CanvasNode)` keeps its behaviour,
- the type part of the node's `aria-label` ("Umsatz, Liniendiagramm"),
- the `type` argument of `CanvasTexts.Selected`,
- the `type` argument of `CanvasTexts.WaitingFor`,
- through the label, `CanvasEdit.Label` and `CanvasSelection.Label` — so the
  dock chip and the prompt to the model carry the translated name. The prompt
  keeps the raw id in `CanvasSelection.Type`, so the model can always identify
  the element.

`DrylCanvas` hands its `Texts.TypeName` to `CanvasSelection` (internal, like the
existing `SetFallback`), so `Select` keeps its public signature.

**The workspace joins `CanvasTexts`.** `DrylCanvasWorkspace` gets a `Texts`
parameter (`CanvasTexts`, default `CanvasTexts.Default`) and passes it to its
own body `DrylCanvas`. `CanvasTexts` gains the workspace's words, with today's
English as default:

| Member | Default |
|---|---|
| `Undo` | "Undo" |
| `Redo` | "Redo" |
| `VersionHistory` | "Version history" (button label, tooltip and panel label) |
| `Undone` — `Func<string?, string>` | "Undone: {label}" / "Undone: last change" |
| `Redone` — `Func<string?, string>` | "Redone: {label}" / "Redone: last change" |
| `Restored` — `Func<int, int, string>` | "Restored version {n} of {m}" |
| `CloseView` — `Func<string, string>` | "Close {title}" |
| `VersionLabel` — `Func<int, string>` | "Version {n}" (the commit label when `RevisionLabel` is null) |

`EmptyTitle`, `EmptyText` and `AriaLabel` stay parameters of the workspace
(API freeze — moving them would duplicate them).

## Scope
- **In scope:** `CanvasTexts.TypeName` and its five uses; the
  `CanvasLabel.For` overload; the workspace's `Texts` parameter, its eight new
  `CanvasTexts` members and the forwarding to its body canvas; the website demo
  page showing a translated canvas, if one exists.
- **Out of scope:** translating the artifact's content (authored by the spec);
  re-labelling history entries that were committed before `Texts` changed
  (they are stored data); refreshing `CanvasSelection.Label` when `Texts`
  changes while a node is selected — it updates on the next selection;
  `DrylAiCanvas` / `DrylCanvasDock` parameters, which are already translatable;
  shipping German defaults.

## Impact
- **Harness:** no new token, animation, `AiState` or dependency.
- **Specs:**
  `specs/E3 AI/F3 DrylCanvas/S4 Interaction.md` (Texts block, selection
  announcement, `aria-label`), `specs/E3 AI/F3 DrylCanvas/_Component.md`
  (`CanvasLabel`, `CanvasTexts` members), the waiting caption wherever it is
  specified under `specs/E3 AI/F3 DrylCanvas/`,
  `specs/E3 AI/F8 DrylCanvasWorkspace.md` (new `Texts` parameter, translatable
  chrome, forwarding), `specs/E14 Agent Canvas/F2 DrylAiCanvas.md` only if it
  restates the label rule.
- **Public API:** nine new `CanvasTexts` members, one `CanvasLabel.For`
  overload, one `DrylCanvasWorkspace.Texts` parameter — additive, MINOR
  (`REL-01`). Behaviour change with English defaults: the `aria-label`, the
  selection announcement and the waiting caption read "Line chart" instead of
  "lineChart". This is an accessibility fix, not a contract break; it is named
  in the changelog.
- **Code:** `Canvas/CanvasTexts.cs`, `Canvas/CanvasLabel.cs`,
  `Canvas/CanvasSelection.cs`, `Components/AI/DrylCanvas.razor`,
  `Canvas/Internal/CanvasNodeView.razor`,
  `Components/AI/DrylCanvasWorkspace.razor`. No change in
  `DRYL.Components.Agents` — the dock reads `CanvasSelection.Label`, which is
  already translated when it gets there. Risk: `CanvasSelection` is created by
  the host and shared with the dock; the type-name function must be in place
  before the first `Select`. `DrylCanvas` makes every `Select` call and sets the
  function in `OnParametersSet`, before any user interaction, so this holds.

## Decisions
- 2026-10-17: Product Owner chose **one translated name everywhere**, including
  the prompt to the model, over two names (translated display, English prompt).
  The chip and the prompt say the same word the user sees, and the raw id
  travels in `CanvasSelection.Type`, so the model loses nothing.
- 2026-10-17: Product Owner chose to replace the **raw type id** in the
  `aria-label` and in `Selected` with the readable `TypeName`, over keeping it
  or dropping the type. The kind of element stays audible, the id never reaches
  a human.
- 2026-10-17: Product Owner chose to bring the **workspace chrome into this
  idea**, over a separate card — same kind of work, one release, and afterwards
  the canvas chrome is translatable end to end.
- 2026-10-17: Product Owner chose to put the workspace's words **into
  `CanvasTexts`**, over a separate `CanvasWorkspaceTexts` record or single
  parameters: one record to translate, handed to every canvas surface.
- 2026-10-17: Product Owner chose to give **`WaitingFor`** the readable
  `TypeName` too, for the same reason as the `aria-label`.
- 2026-10-17: Product Owner confirmed the final version.
- 2026-10-17 (Tech Lead, implementation): a blank result from a host's
  `TypeName` falls back to the default name, so `CanvasLabel.For` keeps its
  "never empty" promise.

## Open Points
