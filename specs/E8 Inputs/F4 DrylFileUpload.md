# DrylFileUpload

## Meta
- **State:** Implemented
- **Source:** code/DRYL.Components/Components/Inputs/DrylFileUpload.razor
              code/DRYL.Components/Components/Inputs/DrylFileUpload.razor.css

## User Story

As a Blazor developer, I want a drop zone that accepts files by drag-and-drop or
by clicking, lists what was picked and speaks my application's language, so that
I can collect attachments without building the zone, the list and the remove
buttons myself.

## Description

`DrylFileUpload` is a dashed, frosted-in-flow drop zone with a native file input
stretched invisibly over it, so a click anywhere in the zone opens the browser's
file picker and a drop anywhere lands in the same input. It does not upload
anything: every change of the selection — files added or one removed — is raised
through `FilesChanged` as the full current list of `IBrowserFile`, and reading
the streams is the host's concern.

The zone shows a leading upload glyph, a headline (`Title`) and a hint line
(`SubText`). Left unset, the hint is derived from `Accept` and
`MaxFileSizeBytes`, e.g. `image/*,.pdf · max 10.0 MB`. All consumer-visible
wording — the headline, the hint, the accessible name of each remove button —
is a parameter whose default is the component's original English text, so an
application in another language sets three parameters instead of rebuilding
the zone.

Under the zone the component lists the selected files, each with an icon
chosen from the extension, its name, its size and a remove button. A host that
renders its own list from `FilesChanged` switches that list off with
`ShowFileList="false"`.

## Public API

| Member | Type | Default | Purpose |
|---|---|---|---|
| `FilesChanged` | `EventCallback<IReadOnlyList<IBrowserFile>>` | — | Raised with the full current selection whenever it changes. |
| `Multiple` | `bool` | `false` | Allows picking several files; without it a new pick replaces the selection. |
| `Accept` | `string?` | `null` | Passed to the file input's `accept`; also feeds the derived hint. |
| `MaxFileSizeBytes` | `long` | 10 MiB | Shown in the derived hint. Not enforced. |
| `Label` | `string?` | `null` | Field label above the zone. |
| `HelperText` | `string?` | `null` | Helper line below the control. |
| `Title` | `string` | `"Drag & drop or click to browse"` | Headline inside the zone. |
| `SubText` | `string?` | `null` | Hint line; `null` derives it, `""` hides it. |
| `RemoveLabel` | `Func<string, string>?` | `null` | Builds a remove button's accessible name from the file name; `null` gives `"Remove {name}"`. |
| `ShowFileList` | `bool` | `true` | Whether the component renders the list of selected files. |
| `Disabled` | `bool` | `false` | Greys the zone out and makes it non-interactive. |
| `Ai` | `AiState` | `AiState.None` | Ambient AI state on the drop zone. |
| `Aura` | `AiAura?` | `null` | Pins the aura variant; `null` inherits a surrounding `DrylAiScope`. |

## Acceptance Criteria

### Structure

- The component renders a drop zone containing an invisible native file input
  that covers the whole zone.
- The native file input is not visible as a browser file button: the overlay
  rule reaches the `InputFile` child's input through `::deep`.
- `Label` set renders a field label above the zone.
- `HelperText` set renders a helper line below the control.
- The zone renders a leading `DrylIcon` whose name is in `DrylIcon.Icons`, so
  the glyph is never an empty `svg`.
- The zone renders `Title` as its headline.
- `Title` defaults to `"Drag & drop or click to browse"`.
- `SubText` set to a non-empty string renders exactly that string as the hint
  line.
- `SubText` set to an empty string renders no hint line.
- `SubText` left `null` renders the hint derived from `Accept` and
  `MaxFileSizeBytes`, joined by ` · `.
- `SubText` left `null` with neither `Accept` set nor a positive
  `MaxFileSizeBytes` renders no hint line.
- Sizes in the derived hint and in the file list are formatted with the
  invariant culture, so a German host still reads `10.0 MB`.

### Selection

- Picking or dropping files raises `FilesChanged` with the full current
  selection.
- `Multiple` unset replaces the selection with each new pick.
- `Multiple` set appends each new pick to the selection.
- Pressing a file's remove button removes that file and raises `FilesChanged`
  with the remaining selection.
- `ShowFileList` left `true` renders one row per selected file.
- `ShowFileList` set to `false` renders no file list.
- `ShowFileList` set to `false` still raises `FilesChanged` for every change.
- Each row renders a `DrylIcon` chosen from the file's extension, whose name is
  in `DrylIcon.Icons`.
- Each row renders the file name, truncated with an ellipsis rather than
  wrapping.
- Each row renders the formatted file size.
- Dragging files over the zone marks it active until the drag leaves or drops.

### Keyboard and accessibility

- The native file input is the zone's interactive element, so it is reachable
  by Tab and opened by the browser's own key handling.
- `Disabled` disables the native file input.
- Each remove button is a native `button` with `type="button"`, so it never
  submits a surrounding form.
- Each remove button's accessible name is `RemoveLabel` applied to the file
  name.
- `RemoveLabel` left `null` names each remove button `Remove {name}`.
- The glyphs in the zone and the rows are decorative and hidden from assistive
  technology.

### Appearance

- Every color the component renders comes from a token; the component names no
  literal color (`DESIGN-01`).
- The zone is filled with `--glass-1` and outlined with a dashed
  `--line-strong` border in `--r-lg`.
- Hovering the zone or dragging over it outlines it with `--accent-line`,
  fills it with `--glass-2` and adds an accent glow ring.
- Border, fill, glow and glyph color change over `--dur-med` with
  `--ease-out`, so the drag state glides rather than snaps.
- The zone sits in the flow and carries no frost of its own (`DESIGN-06`).
- The accent appears only as the border and glow ring, never as the fill of
  the zone (`DESIGN-08`).
- A remove button turns `--danger` on hover over `--dur-fast`.
- The component branches on no color mode and holds no mode-assuming value
  (`DESIGN-02`).

### AI mode

- `Ai` defaults to `AiState.None`, so AI styling is opt-in.
- The aura is rendered through the shared `DrylAuraElements` helper around the
  drop zone, echoing the zone's `--r-lg` radius (`AI-02`).
- The aura variant follows `Aura` when set and a surrounding `DrylAiScope`
  otherwise.
- With the aura absent, the wrapper around the zone carries no class and no
  styling.
- Entering `AiState.Generated` replays the one-shot completion wash.

## Recorded gaps

- **The file list does not animate.** A row appears and disappears between two
  frames (`DESIGN-11`, `DESIGN-12`); the rows are neither wrapped in
  `DrylPresence` nor revealed.
- **`MaxFileSizeBytes` is advisory only.** An oversized file is listed and
  reported like any other; the parameter feeds the hint text and nothing else.
- **The size unit `max` in the derived hint is English.** A host that needs it
  translated sets `SubText`.
- **Literal type sizes and remove-button box.** `.file-drop-title`,
  `.file-drop-sub`, `.file-item-name` and `.file-item-size` set literal font
  sizes and `.file-item-remove` a literal box in `dryl.css` (`DESIGN-01`).
- **The remove button shows no focus-visible ring of its own.**

## Cross-cutting evidence (`SPEC-05`)

- **Both color modes** — token-only colors, verified by
  `node scripts/check-light-sync.mjs` and
  `node scripts/validate-light-contrast.mjs`; no mode-specific rule.
- **Enter/exit animation** — the zone's drag and hover state glide; the file
  list's missing enter/exit is recorded above as debt.
- **Keyboard and a11y** — the "Keyboard and accessibility" criteria above.
- **AI mode** — yes, on the drop zone, as specified under "AI mode".
- **Demo page** — `DRYL.Website/Components/Pages/DemoFileUpload.razor`.
- **`ComponentCatalog`** — registered as `"File Upload"` / `file-upload` in
  `DRYL.Website/Components/ComponentCatalog.cs`, flagged AI-capable.
- **Tests** — `tests/DRYL.Components.Tests/DrylFileUploadTests.cs` guards the
  default wording, every text override, the hidden hint, `ShowFileList`, the
  non-empty glyphs and the `::deep` overlay rule. The overlay and both color
  modes were checked in the browser on the demo page on 2026-09-23.
