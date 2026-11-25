# A Markdown editor with a preview switch

## Meta
- **State:** Ready

## Problem

DRYL can show Markdown (`DrylMarkdown`) and it can take multi-line text
(`DrylTextarea`), but a host that lets a person *write* Markdown builds the
pairing itself every time: a text area, a rendered view, a switch between the
two, a formatting toolbar that edits the selection, and the state that keeps
all of it on one string. And where an AI drafts or rewrites the text, the host
also has to make the editor show that — in the source and in the preview.

Target role: the Blazor developer who offers Markdown authoring; secondarily
the end user who writes there, alone or together with an AI.

## Solution Idea

A new input component — working name `DrylMarkdownEditor` — that binds one
Markdown string and shows it in one of two views, chosen by a switch in the
component's header.

**Views.** `MarkdownEditorView.Editor` shows the source in the library's
text-area surface; `MarkdownEditorView.Preview` shows the same string rendered
by `DrylMarkdown`, with its untrusted-content guarantees unchanged. The view
is bindable (`@bind-View`). The enum is cut so that a `Split` member can join
later without a breaking change. The switch is the existing
`DrylSegmentedControl`; the change between the views is animated with the
existing vocabulary (`DESIGN-11`, `DESIGN-12`).

**Form input.** Inherits `InputBase<string>` like `DrylTextarea`: binds with
`@bind-Value`, validates inside an `EditForm`, renders its own label, helper
text and validation message.

**Toolbar.** A formatting toolbar sits beside the switch and edits the
selection in the source: it wraps or unwraps inline marks and sets or removes
line prefixes. It is a `role="toolbar"` with one tab stop and arrow-key
navigation; every button is icon-only with a tooltip and an `aria-label`
(`UX-05`), and all its words come from one `MarkdownEditorTexts` record
(English defaults, after `SelectTexts`). In the preview the toolbar's commands
are not available.

Commands, in order: bold, italic, strikethrough · heading (one button that
cycles H1 → H2 → H3 → none) · bullet list, numbered list, task list · quote ·
code (inline; a fenced block when the selection spans lines) · link (inserts
`[selection](url)` and selects `url`; no dialog). A command on text that
already carries its mark removes it.

**Keyboard and typing.** Ctrl/Cmd+B bold, +I italic, +K link, +E code. Enter
continues a list, a task list or a quote and ends it on an empty item. Tab is
not captured, so the editor is never a keyboard trap (`UX-01`).

**AI-native.** The component takes `Ai` and `Aura` (`AI-03`) and follows a
surrounding `DrylAiScope`. One aura frames the whole editor — header and body
are one surface (`AI-08`). The effective state is handed to the preview, so
while the state is `Streaming` the rendered document carries `DrylMarkdown`'s
streaming reveal. The source stays mounted in both views, so the editor works
unchanged inside `DrylAiField` from the Agents package: the agent writes or
rewrites the Markdown, the person watches it arrive as source or as a rendered
document, and accepts or discards it. The core package itself gains no agent
and no model call.

## Scope

- **In scope:** the component, the view switch, the two views, the formatting
  toolbar with its shortcuts, form binding, `Ai`/`Aura`, the texts record,
  spec, tests, changelog, a demo page in `DRYL.Website` (including one example
  inside `DrylAiField`).
- **Out of scope:** WYSIWYG editing; syntax highlighting inside the source; a
  side-by-side view (the enum leaves room for it); image upload; a table
  builder; a toolbar the host configures or extends; an AI action built into
  the toolbar; any new dependency.

## Impact

- **Harness:**
  - No new token, duration, easing or `AiState`. No new dependency: Markdig is
    the approved exception (`CODE-03`) and is used only through `DrylMarkdown`.
  - **New icons — signed off by the maintainer (2026-11-25).** `DrylIcon` has
    `Heading`, `List`, `Quote`, `Code` and `Link`, but no `Bold`, `Italic`,
    `Strikethrough`, `ListOrdered` or `ListChecks`. They are added from Lucide
    (ISC, the set's existing source, already covered by
    `THIRD_PARTY_NOTICES.md`).
  - New first-party script surface: a `window.dryl.mdeditor` module in
    `dryl.js` (read and replace the selection, shortcuts, list continuation).
    No framework, no package.
- **Specs:** new `specs/E8 Inputs/F24 DrylMarkdownEditor.md`; `_Api.md` of E8
  gains the component, `MarkdownEditorView` and `MarkdownEditorTexts`;
  `_Interop.md` of E8 gains the script module; `specs/E5 Data` icon list gains
  the new names; the component count in `harness/requirements.md` (`SPEC-02`)
  and in the coverage check rises by one. `DrylAiField`'s spec gains the
  editor as a supported wrapped input.
- **Public API (new component, MINOR):** `Value` (+ `ValueChanged`,
  `ValueExpression`), `View` (+ `ViewChanged`), `Label`, `Placeholder`,
  `HelperText`, `Rows`, `State`, `Disabled`, `AriaLabel`, `Texts`, `Ai`,
  `Aura`, `Class`, `AdditionalAttributes`; enum `MarkdownEditorView
  { Editor, Preview }`; record `MarkdownEditorTexts`.
- **Code:** `Components/Inputs/DrylMarkdownEditor.razor` (+ `.razor.css`),
  `MarkdownEditorView.cs`, `MarkdownEditorTexts.cs`, the script module, the
  icons in `DrylIcon`. Risks:
  - A scripted edit must stay undoable with Ctrl+Z; the script inserts through
    the browser's own text-insertion path and falls back to a plain range
    replacement.
  - A scripted edit must reach `@bind-Value` (a native `input` event) and must
    not be reverted by a late server echo (`window.dryl.inputs` guard).
  - The source must stay mounted and enabled in the preview, or `DrylAiField`
    finds no field; hidden content must still leave the tab order.
  - Focus after a switch and after a toolbar command returns to the source
    with the selection intact.
  - The two views differ in height; the change must not make the page jump.
  - JS interop must be guarded against static prerender and disposal
    (`CODE-05`).

## Decisions

- 2026-11-25 — Jan: DRYL gets a Markdown editor with a switch between editor
  and preview.
- 2026-11-25 — Jan: the editor is AI-native — it takes part in the shared AI
  vocabulary from its first version.
- 2026-11-25 — Jan: the first version carries a formatting toolbar; the bare
  "source + preview + switch" cut proposed by the Tech Lead was rejected.
- 2026-11-25 — Jan (on the Tech Lead's recommendation): two views only; the
  view enum is cut so `Split` can be added later without a breaking change.
- 2026-11-25 — Tech Lead: the source stays mounted in both views, so that
  `DrylAiField` can stream into the editor while the preview is showing.
- 2026-11-25 — Jan: "AI-native" in the core package means `Ai`/`Aura`, one
  aura around the whole editor, the streaming reveal in the preview and
  guaranteed composition with `DrylAiField`; the toolbar carries no AI action
  of its own, because the core package knows no agents.
- 2026-11-25 — Jan: the toolbar's command set as listed under *Solution Idea*;
  image and table stay out.
- 2026-11-25 — Jan: sign-off for the five new icons `Bold`, `Italic`,
  `Strikethrough`, `ListOrdered`, `ListChecks`, sourced from Lucide.
- 2026-11-25 — Jan: shortcuts, list continuation on Enter, no Tab capture and
  the dialog-free link insertion as listed under *Solution Idea*.
- 2026-11-25 — Jan: confirmed the final version as summarized by the Tech Lead
  ("Ja fang an"); the idea is `Ready`.
- 2026-11-25 — Tech Lead, refining the link command for the empty case: with
  no selection it inserts `[](url)` and puts the caret between the brackets,
  because there is no text to keep and the text is what is typed first. With
  a selection it stays as decided: `[selection](url)` with `url` selected.
- 2026-11-25 — Tech Lead: both views share one box whose height the source
  sets; a longer preview scrolls inside it. That is what keeps the page from
  jumping on a switch.
- 2026-11-25 — Tech Lead: the view switch stays usable on a disabled editor —
  `Disabled` stops editing, not reading.
- 2026-11-25 — Tech Lead: on Apple platforms the shortcuts use Cmd only.
  Ctrl+E and Ctrl+B are native caret movements in a macOS text field and are
  left alone.

## Open Points

None.
