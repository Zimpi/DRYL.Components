# I54 — A Markdown editor with a preview switch: implementation plan

Source idea: [`ideas/I54 A Markdown editor with a preview switch.md`](../ideas/I54%20A%20Markdown%20editor%20with%20a%20preview%20switch.md).
Version: `DRYL.Components` 3.18.1 → **3.19.0** (new component). 3.18.1 is on
`origin/main` and therefore shipped (`REL-01`), so T1 bumps the version and
cuts a new changelog block; T2 and T3 join that block. The Agents package gains
no code.

Each task updates its spec in the same commit (`SPEC-01`), carries its own
verification and is one commit.

## T1 — Five icons

- `code/DRYL.Components/Components/Data/DrylIcon.razor` — `Bold`, `Italic`,
  `Strikethrough`, `ListOrdered`, `ListChecks` (lucide `bold`, `italic`,
  `strikethrough`, `list-ordered`, `list-checks`); signed off in I54.
- `specs/E5 Data/F10 DrylIcon.md` — test evidence line.
- `tests/DRYL.Components.Tests/DrylIconTests.cs` — the five names are in the set.
- `code/DRYL.Components/DRYL.Components.csproj` — `<Version>3.19.0</Version>`;
  `CHANGELOG.md` — `[3.19.0]` block.
- Verify: `dotnet test DRYL.slnx -c Release --filter DrylIconTests`.

## T2 — `dryl.mdeditor`, the editing script

- `code/DRYL.Components/wwwroot/js/dryl.js` — `window.dryl.mdeditor`:
  `attach(root)`, `detach(root)`, `sync(root)`, and the two pure helpers
  `transform(value, start, end, command)` and `enter(value, caret)`.
  Toolbar presses and shortcuts are handled in the browser, without a server
  round trip; the edit goes through the browser's own text insertion so it
  stays undoable, and reaches `@bind-Value` as a native `input` event.
- `specs/E8 Inputs/_Interop.md` — the module, its listeners, its cleanup.
- `tests/js/mdeditor.test.mjs` — every command on and off, the list
  continuation, the shortcut table, listener cleanup.
- Verify: `node --test tests/js/mdeditor.test.mjs`.

## T3 — `DrylMarkdownEditor`

- `specs/E8 Inputs/F24 DrylMarkdownEditor.md` (new, `Implemented` with the
  code); `specs/E8 Inputs/_Api.md` — `MarkdownEditorView`,
  `MarkdownEditorTexts`; `harness/requirements.md` `SPEC-02` table (E8 24,
  total 132) and every other place that states the total;
  `specs/E15 Agent Inputs/F1 DrylAiField.md` — the editor named as a wrapped
  input; the idea moves to `Adopted`.
- `code/DRYL.Components/Components/Inputs/DrylMarkdownEditor.razor` +
  `.razor.css`, `MarkdownEditorView.cs`, `MarkdownEditorTexts.cs`.
- `code/DRYL.Components/wwwroot/dryl.css` — the editor joins the selector
  lists of the existing streaming reveal and of the forced-colors focus
  fallback. No new token, keyframe or duration.
- `tests/DRYL.Components.Tests/DrylMarkdownEditorTests.cs`.
- `CHANGELOG.md` — the component's entry in the `[3.19.0]` block.
- Verify: `dotnet build DRYL.slnx -c Release`, `dotnet test DRYL.slnx -c Release`,
  `node --test tests/js/*.test.mjs`, the five `scripts/check-*.mjs` /
  `validate-*.mjs` checks.

## T4 — Website (`DRYL.Website`, its own commit in its own repository)

- `Components/Pages/DemoMarkdownEditor.razor`, examples under
  `Components/Examples/MarkdownEditor/`, the `ComponentCatalog` entry
  (`REL-04`, `CODE-20`), the new icons on the icons page if it lists names by
  hand.
- Verify: the page driven in a real browser — toolbar, shortcuts, list
  continuation, undo, view switch, validation, AI states — in both color
  modes, by eye (`DESIGN-02`).

## T5 — Independent review

- A reviewer without this session's context reads the diff of T1–T4 against
  the spec and the harness; findings are fixed in follow-up commits.
