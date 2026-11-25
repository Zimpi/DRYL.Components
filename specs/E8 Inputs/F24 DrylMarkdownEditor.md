# DrylMarkdownEditor

## Meta
- **State:** Implemented
- **Source:** code/DRYL.Components/Components/Inputs/DrylMarkdownEditor.razor
              code/DRYL.Components/Components/Inputs/DrylMarkdownEditor.razor.css
              code/DRYL.Components/Components/Inputs/MarkdownEditorView.cs
              code/DRYL.Components/Components/Inputs/MarkdownEditorTexts.cs

## User Story

As a Blazor developer whose users write Markdown, I want one field that binds a
Markdown string, lets the user format it from a toolbar and switch between the
source and the rendered result, and shows when an AI is writing in it, so that
I do not build the text area, the preview, the switch and the selection editing
again in every form.

## Description

`DrylMarkdownEditor` shows one Markdown string in one of two views. The
**editor** view is the source in a text area; the **preview** renders the same
string with `DrylMarkdown`, with that component's guarantees for untrusted
content unchanged. A switch in the header — a `DrylSegmentedControl` with two
segments — changes between them, and the view can be bound with `@bind-View`.

The field is one frame: a header with the switch and a formatting toolbar, and
a body. Both views share the body's box. The source sets its height (through
`Rows` and the user's vertical resize); a preview longer than that scrolls
inside it, so switching the view does not move the page.

The **toolbar** edits the selection in the source. Ten commands, in order:
bold, italic, strikethrough · heading · bullet list, numbered list, task list ·
quote, code, link. A command on text that already carries its formatting takes
it off again. Four of them have a shortcut — Ctrl+B, Ctrl+I, Ctrl+E (code) and
Ctrl+K (link); on Apple platforms the modifier is Cmd. Enter inside a list, a
task list or a quote starts the next item with the same marker, and an item
left empty ends the list. Tab is never captured. Commands, shortcuts and the
Enter key are handled in the browser by `dryl.mdeditor` (see
[`_Interop.md`](_Interop.md)): a command does not wait for a server round trip,
its edit can be taken back with the browser's own undo, and the new text
reaches the binding as an ordinary `input` event.

The component is a form input like `DrylTextarea`: it inherits
`InputBase<string>`, binds with `@bind-Value`, takes part in `EditForm`
validation and renders its own label, helper text and validation message.

**AI.** The component takes `Ai` and `Aura` and follows a surrounding
`DrylAiScope`. One aura frames the whole editor; the buttons, the switch and
the preview inside the frame draw none of their own. While the state is
`AiState.Streaming`, the preview's newest block carries the shared streaming
reveal of `DrylMarkdown`. The source stays in the DOM and enabled in both
views, so the editor can be wrapped in `DrylAiField` from the Agents package
unchanged: the agent writes or rewrites the Markdown, the user can watch it
arrive as source or as a rendered document, and accepts or discards it. The
component itself calls no model and knows no agent.

```razor
<DrylMarkdownEditor @bind-Value="post.Body" Label="Body" />

<DrylMarkdownEditor @bind-Value="post.Body" @bind-View="_view" Ai="_state" />
```

Out of scope: WYSIWYG editing, syntax highlighting in the source, a
side-by-side view, image upload, a table builder, and a toolbar the host
configures or extends.

## Public API

Namespace: `DRYL.Components`. Inherits `InputBase<string>` and implements
`IAsyncDisposable`. Inherited `Value`, `ValueChanged`, `ValueExpression`,
`DisplayName` and `AdditionalAttributes` follow [`_Api.md`](_Api.md);
`AdditionalAttributes` are applied to the source `textarea`.

| Member | Type | Default | Purpose |
|---|---|---|---|
| `View` | `MarkdownEditorView` | `MarkdownEditorView.Editor` | The view shown. Bindable with `@bind-View`. |
| `ViewChanged` | `EventCallback<MarkdownEditorView>` | unset | Fires when the user changes the view with the switch. |
| `Label` | `string?` | `null` | Visible label above the editor. |
| `Placeholder` | `string?` | `null` | Placeholder of the empty source. |
| `HelperText` | `string?` | `null` | Text below the editor, replaced by validation messages. |
| `Rows` | `int` | `8` | Initial visible row count of the source. |
| `State` | `InputState` | `InputState.Default` | Visual state override. |
| `Disabled` | `bool` | `false` | Stops editing: disables the source and the toolbar. The switch stays usable. |
| `AriaLabel` | `string?` | `null` | Accessible name of the source when `Label` is empty. |
| `Texts` | `MarkdownEditorTexts?` | `null` | The editor's own words; `null` means `MarkdownEditorTexts.Default`. |
| `Ai` | `AiState` | `AiState.None` | AI state, with scope inheritance. |
| `Aura` | `AiAura?` | `null` | Aura variant, with scope inheritance. |
| `Class` | `string?` | `null` | Extra CSS class(es) merged onto the root. |

`MarkdownEditorView` (`Editor`, `Preview`) and `MarkdownEditorTexts` are
described in [`_Api.md`](_Api.md).

## Acceptance Criteria

### Structure

- The component renders one root carrying `.field` and `.mde-field`.
- `Class` is merged onto the root's own classes and never replaces them.
- The root holds exactly one frame carrying `.mde`.
- The frame holds a `.mde-header` followed by a `.mde-body`.
- The body holds exactly one native `textarea` carrying `.mde-source`.
- The source carries `data-dryl-input`.
- The source carries `data-mde-source`.
- The source displays the bound string value.
- `Rows` defaults to `8`.
- The supplied `Rows` value is rendered as the source's `rows` attribute.
- `Placeholder` is passed to the source's placeholder attribute.
- `AdditionalAttributes` are applied to the source.
- A nonempty `Label` renders a label associated with the source's ID.
- An empty `Label` renders no label element.

### Value

- A native `input` event on the source updates the bound value through `ValueChanged`.
- A changed value notifies the associated `EditContext` field when one is present.
- A null parsed input is normalized to `string.Empty`.
- The component binds outside an `EditForm` without a `ValueExpression`.

### View switch

- The header holds a `role="radiogroup"` with exactly two `role="radio"` segments.
- The radio group's accessible name is `Texts.View`.
- The first segment's text is `Texts.Editor`.
- The second segment's text is `Texts.Preview`.
- The segment of the view shown has `aria-checked="true"`.
- `View` defaults to `MarkdownEditorView.Editor`.
- In the editor view the frame carries `.mde--editor`.
- In the editor view the frame carries `data-mde-view="editor"`.
- In the preview the frame carries `.mde--preview`.
- In the preview the frame carries `data-mde-view="preview"`.
- Choosing the other segment changes the view.
- Choosing the other segment fires `ViewChanged` with the new view.
- Choosing the segment of the view already shown fires nothing.
- A changed `View` parameter changes the view.
- A re-render with an unchanged `View` parameter keeps a view the user chose.
- Choosing a segment leaves keyboard focus on the switch.
- The switch stays enabled when `Disabled` is set.

### Preview

- In the editor view no preview and no `DrylMarkdown` is rendered.
- In the preview a `.mde-preview` region renders the bound value through `DrylMarkdown`.
- The preview region has `role="region"`.
- The preview region's accessible name is `Texts.Preview`.
- The preview region is in the tab order, so a keyboard user can scroll it.
- A value that changes while the preview is shown is re-rendered in the preview.
- A null, empty or whitespace-only value previews as `Texts.PreviewEmpty` instead of a document.
- Raw HTML in the value is escaped in the preview, never inserted as markup.
- In the preview the source remains in the DOM.
- In the preview the source is not disabled by the view.
- In the preview the source is neither visible nor reachable by keyboard.
- The frame has the same height in both views.
- A preview taller than the body scrolls inside the body.
- The preview enters and leaves through `DrylPresence`.

### Toolbar

- The header holds a `role="toolbar"` element carrying `data-mde-toolbar`.
- The toolbar's accessible name is `Texts.Toolbar`.
- The toolbar's `aria-controls` names the source's ID.
- The toolbar holds exactly ten buttons.
- The buttons name their commands in `data-mde-cmd`, in the order `bold`,
  `italic`, `strike`, `heading`, `bullet`, `ordered`, `task`, `quote`, `code`,
  `link`.
- Every toolbar button is an icon-only ghost `DrylButton` of `type="button"`.
- Every toolbar button is wrapped in a `DrylTooltip` whose text equals the
  button's `aria-label` (`UX-05`).
- The ten labels come from `Texts.Bold`, `Texts.Italic`,
  `Texts.Strikethrough`, `Texts.Heading`, `Texts.BulletList`,
  `Texts.NumberedList`, `Texts.TaskList`, `Texts.Quote`, `Texts.Code` and
  `Texts.Link`, in that order.
- The buttons of the bold, italic, code and link commands declare their
  shortcut in `aria-keyshortcuts`, for both the Control and the Meta modifier.
- In the editor view of an enabled editor every toolbar button is enabled.
- In the preview every toolbar button is disabled.
- With `Disabled` every toolbar button is disabled.
- The toolbar is one tab stop: exactly one enabled button has `tabindex="0"`.
- With every button disabled the toolbar is not a tab stop.
- ArrowRight moves focus to the next enabled button and wraps at the end.
- ArrowLeft moves focus to the previous enabled button and wraps at the start.
- Home moves focus to the first enabled button.
- End moves focus to the last enabled button.
- The button that last had focus is the toolbar's tab stop when focus returns.
- Pressing a toolbar button with the pointer leaves focus and the selection in the source.
- Activating a toolbar button from the keyboard applies its command and moves focus to the source.

### Commands

Each criterion describes the source after the command; "selection" is the
source's selection when the command runs.

- Bold wraps the selection in `**`.
- Italic wraps the selection in `_`.
- Strikethrough wraps the selection in `~~`.
- Code wraps a selection within one line in `` ` ``.
- After an inline command the wrapped text is selected.
- Whitespace at the ends of the selection stays outside the marks.
- An inline command with an empty selection inserts the pair of marks and
  places the caret between them.
- An inline command on a selection that is directly surrounded by its marks
  removes them.
- An inline command on a selection that itself starts and ends with its marks
  removes them.
- Code on a selection that spans lines puts it into a fenced block on lines of
  its own.
- Code on a selection that is directly surrounded by a fence removes the fence.
- Link on a selection inserts `[selection](url)` and selects `url`.
- Link with an empty selection inserts `[](url)` and places the caret between
  the brackets.
- Heading on a line without a heading makes it a level-one heading.
- Heading on a level-one heading makes it level two.
- Heading on a level-two heading makes it level three.
- Heading on a heading of level three or deeper removes the heading.
- Bullet list prefixes every selected line with `- `.
- Numbered list prefixes the selected lines with `1. `, `2. `, … in order.
- Task list prefixes every selected line with `- [ ] `.
- Quote prefixes every selected line with `> `.
- A list command on lines that all carry its marker removes the marker.
- Quote on lines that are all quoted removes the quote.
- A list command on lines that carry another list marker replaces that marker.
- A list command keeps each line's indentation.
- A list command leaves blank lines inside the selection unchanged.
- Quote marks blank lines inside the selection with `>`, so the quote stays one block.
- A line command with an empty selection applies to the caret's line.
- After a line command with an empty selection the caret is on the character it was on.
- After a line command on a selection the changed lines are selected.
- A selection that ends directly after a line break does not include the next line.
- A command changes nothing while the source is read-only.
- A command raises exactly one `input` event on the source.
- A command can be taken back with the browser's undo.

### Keyboard in the source

- Ctrl+B applies bold.
- Ctrl+I applies italic.
- Ctrl+E applies code.
- Ctrl+K applies link.
- A shortcut that applied its command suppresses the browser's default for that key.
- On Apple platforms the shortcut modifier is Cmd.
- On Apple platforms Ctrl+B, Ctrl+I, Ctrl+E and Ctrl+K keep their native behaviour.
- A shortcut combined with Shift or Alt is left to the browser.
- Enter at the end of a bullet item starts a new item with the same bullet character.
- Enter in a numbered item starts a new item with the next number.
- Enter in a task item starts a new, unchecked task item.
- Enter in a quoted line starts a new quoted line.
- A continued item keeps the indentation of the item before it.
- Enter in an item that holds only its marker removes the marker instead of
  adding a line.
- Enter with the caret before or inside the marker is left to the browser.
- Enter during text composition is left to the browser.
- Shift+Enter is left to the browser.
- Tab and Shift+Tab move focus and are never captured (`UX-01`).
- The source is reachable through Tab and Shift+Tab in DOM order, after the
  switch and the toolbar.

### Validation and disabled state

- A field validation message applies `.mde--error` to the frame regardless of `State`.
- Without a validation message, `InputState.Error` applies `.mde--error`.
- Without a validation message, `InputState.Success` applies `.mde--success`.
- `InputState.Default` adds no state modifier without a validation message.
- Field validation messages replace `HelperText`.
- Multiple field validation messages are displayed joined by a space.
- With no validation message, nonempty `HelperText` is displayed below the frame.
- Without a validation message or helper text, no helper element is rendered.
- A field validation message sets `aria-invalid="true"` on the source.
- A manual `InputState.Error` alone does not set `aria-invalid`.
- A displayed helper or validation message is referenced by the source's `aria-describedby`.
- When `Label` is empty, `AriaLabel` is applied to the source as `aria-label`.
- When `Label` is nonempty, `AriaLabel` is omitted.
- `Disabled` applies the native disabled attribute to the source.
- `Disabled` applies `.mde--disabled` to the frame.
- A disabled editor still shows its value in the preview.

### Texts

- `Texts` defaults to `MarkdownEditorTexts.Default`, whose members are English.
- Every word the component renders of its own comes from `Texts`.

### Appearance, focus and motion

- The frame reads its colors from tokens without a color-mode branch (`DESIGN-02`).
- The frame's surface uses `--glass-1`.
- The frame's border uses `--line-strong`.
- The frame's hover border uses `--line-hover`.
- The frame's corner radius uses `--r-md`.
- The header is separated from the body by a `--line` hairline.
- The source's text uses `--fg` in `--font-mono`.
- While the source has focus the frame shows the accent border `--accent-line`
  and an accent glow.
- While the source has focus in the error state the frame shows the `--danger` treatment.
- While the source has focus in the success state the frame shows the `--success` treatment.
- A focused toolbar button or switch segment shows its own focus ring and
  leaves the frame at rest.
- Frame border, surface and glow changes use `--dur-med` with `--ease-out`.
- The source fades out and in with `--dur-med` and `--ease-out` when the view changes.
- Toolbar buttons glide into and out of their disabled look with `--dur-med`
  and `--ease-out`.
- The switch's indicator glides between the segments.
- Under `forced-colors: active` the focused source shows a visible outline (`UX-02`).
- Under `prefers-reduced-motion: reduce` the frame, the source and the toolbar
  buttons change without a transition (`UX-06`).
- The stylesheet names no literal color, radius, spacing, duration or easing;
  the glow's spread and blur read `--sp-1` and `--sp-5` (`DESIGN-01`).

### AI mode

- `Ai` defaults to `AiState.None` (`AI-03`).
- Without an active scope or explicit AI state the editor renders no aura.
- A non-`None` effective state puts the shared aura classes on the frame.
- The aura is drawn by `DrylAuraElements` as the frame's first child.
- An explicit non-`None` `Ai` overrides an inherited scope state.
- With `AiState.None` the editor inherits a surrounding scope state.
- An explicit `Aura` overrides the inherited aura variant.
- The aura variant defaults to `AiAura.Comet` when neither editor nor scope sets it.
- Inside a non-`None` scope, the toolbar buttons, the switch and the preview's
  `DrylMarkdown` render no aura of their own.
- While the effective state is `AiState.Streaming`, the newest block of the
  preview carries the shared streaming reveal of `DrylMarkdown`.
- Effective `Generated` entry replays the shared one-shot wash.
- A generated aura retires without requiring the consumer to reset `Ai`.
- `AiState.Proposed` renders the shared resting proposal look of `DrylAuraElements`.
- Leaving AI mode fades the aura out before it is removed.
- Aura elements are hidden from assistive technology (`UX-07`).
- Aura decoration does not change the bound text or keyboard order.

### Composition with `DrylAiField`

- The editor renders exactly one `textarea` and no other text-like `input`.
- The source is never marked `data-aifield-ui`.
- A value written to the source by script, followed by a native `input` event,
  updates the bound value in either view.

### Lifecycle

- No JavaScript is called before the first interactive render.
- The first interactive render calls `dryl.mdeditor.attach` once with the frame.
- A render that changes whether the commands are enabled calls `dryl.mdeditor.sync`.
- Disposal calls `dryl.mdeditor.detach` once, and only after a successful attach.
- Disposal of a statically prerendered editor makes no JavaScript call.
- Disposal tolerates a disconnected circuit.
- Disposal cancels the component's pending aura lifecycle work (`CODE-05`).
- Disposal releases the `EditContext` subscription of `InputBase`.

## Recorded limits

- **The preview does not grow with its content.** Both views share the height
  the source sets, and a long document scrolls inside it. This is deliberate —
  it is what keeps a view change from moving the page — but a host that wants a
  full-height reading view renders `DrylMarkdown` itself.
- **Without script the toolbar and the shortcuts do nothing.** They are wired
  on the first interactive render; a statically prerendered editor is a plain
  text area with a working view switch only after it becomes interactive.
- **Undo depends on the browser's text insertion.** Where
  `document.execCommand('insertText')` is refused, the edit is still made, but
  it replaces the source's undo history instead of joining it.
- **The source is found by a wrapper through its selector.** `DrylAiField`
  writes into the hidden source while the preview is shown; it places its
  trigger in the top-right corner of what it wraps, which is the right end of
  the editor's header. On a width where the toolbar wraps into that corner, or
  with a `Label` above the editor, the trigger can sit over the header's edge.
  That placement belongs to `DrylAiField`
  ([`../E15 Agent Inputs/F1 DrylAiField.md`](../E15%20Agent%20Inputs/F1%20DrylAiField.md)).
- **AI changes carry no built-in live announcement** (`UX-04`). The editor
  shows the state; a host announces it with `DrylAiIndicator`, and
  `DrylAiField` announces its own runs.
- **A changed `View` parameter moves focus nowhere.** If the host switches to
  the preview while the source has focus, focus falls back to the document.
- **Heading, list and quote detection is line-based.** A marker inside a fenced
  code block is treated like any other line.
- `Rows` is passed through without range validation.

## Cross-cutting evidence (`SPEC-05`)

- **Both color modes:** the isolated stylesheet names tokens only and branches
  on no mode. The editor was driven and inspected in Chromium on
  `/components/markdown-editor` in dark and in light mode on 2026-11-25: rest,
  source focus, error, success, disabled, preview, `Streaming` in the preview,
  `Proposed` inside `DrylAiField`, and a 390-pixel-wide viewport.
- **Enter and exit animation:** the preview mounts and unmounts through
  `DrylPresence` (`DESIGN-12`); the source cross-fades; the switch indicator
  glides; toolbar buttons glide into their disabled look; the frame's border and
  glow transition; the aura enters and leaves through `AuraLifecycle`. The label
  and the helper line mount without a transition, as in `DrylTextarea`.
- **Keyboard and a11y:** the criteria under "View switch", "Toolbar" and
  "Keyboard in the source". Driven in Chromium on the same date: Shift+Tab from
  the source lands on the toolbar's tab stop and then on the switch; arrows,
  Home and End move within the toolbar; a keyboard-activated command returns
  focus to the source; Tab leaves the editor.
- **AI decision:** explicitly yes — the text of a Markdown field is exactly
  what an assistant drafts and rewrites. The state frames the editor as a whole
  (`AI-08`), and the preview shows a stream arriving.
- **Demo:** `DRYL.Website/Components/Pages/DemoMarkdownEditor.razor`, route
  `/components/markdown-editor`, with the examples under
  `DRYL.Website/Components/Examples/MarkdownEditor/` (`Basic`, `BoundView`,
  `Form`, `States`, `AiStates`, `AiField`, `Texts`).
- **Catalog:** registered as `"Markdown Editor"` / `markdown-editor` in
  `DRYL.Website/Components/ComponentCatalog.cs`, category Inputs, flagged
  AI-capable (`REL-04`).
- **Tests:** `tests/DRYL.Components.Tests/DrylMarkdownEditorTests.cs` covers
  structure, binding, the view, the toolbar's markup, validation, texts, the AI
  classes and scope isolation, and the interop calls.
  `tests/js/mdeditor.test.mjs` covers every command on and off, the Enter key,
  the shortcut table on both platform families, the toolbar's arrow keys and
  tab stop, and listener cleanup. bUnit does not run the script and the script
  tests do not run a browser: undo, real focus movement, the shared height and
  the rendered colors rest on the browser pass named above, which is not an
  automated test.
