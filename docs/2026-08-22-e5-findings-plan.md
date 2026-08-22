# Plan: what writing the `E5 Data` specs found

**Branch:** not yet cut · **Base:** `spec/phase-c-e5-data`
(`2.24.3`, **published** — `v2.24.3` is tagged and on `origin/main`)

Writing the twenty-one `E5 Data` specs meant reading every component in the
category against its code rather than its doc comments. This is the register of
what that reading turned up, ranked, so the fixes can be planned as their own
work rather than smuggled into the spec branch. Nothing here is fixed yet.

Each entry names the spec that records it in full. The spec is the source; this
file is the queue.

---

## Rank 1 — a rule's enforcement has a blind spot

### 1.1 `DESIGN-01` greps stylesheets, and one component writes colours in markup

`DrylTableKpi` paints its sparkline from four literal colour values written into
`DrylTableKpi.razor`. `DESIGN-01`'s Check line greps `code/*/**/*.razor.css` and
reads **clean**, because these live in a `.razor` file. Searching `.razor` files
under `code/` for colour literals returns exactly these four hits and nothing
else in the library.

The visible consequence is that the tile ignores `DrylThemeProvider`: every
accent on a re-themed page follows the consumer's seed except this chart. The
stylesheet already contains the correct token-based rule for the same stroke —
it has never had an effect, being overridden by the inline `style` attribute on
the element it targets.

- **Recorded in:** `specs/E5 Data/F17 DrylTableKpi.md`
- **The fix has two halves:** replace the four literals with the tokens, and
  widen `DESIGN-01`'s Check to `.razor` as well as `.razor.css` so the next one
  is caught. The second half is the one that matters.

### 1.2 An icon name that does not exist, rendering as nothing

`DrylFileUpload` asks `DrylIcon` for `UploadCloud`; the set has only `Upload`.
An unknown name renders a correctly sized, correctly stroked, entirely empty
`svg` — no exception, no console warning, no fallback glyph — so the drop zone's
32px leading icon has been blank. Comparing every `Name` passed to `DrylIcon`
under `code/` against the keys of `Icons` finds exactly this one mismatch out of
38 names in use.

- **Recorded in:** `specs/E5 Data/F10 DrylIcon.md`
- **The fix has two halves:** correct the name (or add the icon), and add the
  test that compares the two sets, which is the only form in which this class of
  bug is catchable.

---

## Rank 2 — accessibility claims the code does not honour

### 2.1 `DrylTable` declares the grid role without the grid pattern

`role="grid"` promises arrow-key movement between cells, `Home`/`End` within a
row and one tab stop for the widget. The table offers none of it. A
screen-reader user is told they are in a grid and finds none of the behaviour.
The plain table role is the honest claim for what the component does today.

- **Recorded in:** `specs/E5 Data/F16 DrylTable/S9 Presentation, accessibility and AI.md`
- **Two routes:** change the role, or implement the pattern. The first is a
  one-line honesty fix; the second is a feature.

### 2.2 `DrylImage`'s AI state label is announced by nobody

`Ai` set makes the frame a polite live region and writes the state into the
frame's `aria-label`. A live region announces changes to its *content*, not to
its label, and the content does not change when the state does — so "Generating
image… 40 %" reaches no screen reader.

- **Recorded in:** `specs/E5 Data/F11 DrylImage.md`

### 2.3 `DrylCodeBlock`'s copy button never announces that it copied

Its `aria-label` is fixed, and an `aria-label` overrides the visible text — so
the label a screen reader reads stays "copy code" while the visible label reads
"Copied". The one user who most needs the confirmation is the one who does not
get it.

- **Recorded in:** `specs/E5 Data/F7 DrylCodeBlock.md`

### 2.4 `DrylAvatar`'s presence dot is silent

`Status` is `aria-hidden` and contributes nothing to the accessible name, so a
screen-reader user cannot tell an online colleague from an offline one — which
is the only information the dot exists to carry.

- **Recorded in:** `specs/E5 Data/F1 DrylAvatar.md`

### 2.5 `DrylCodeBlock`'s scrollable code has no keyboard access

The body scrolls horizontally and carries no `tabindex`, so a keyboard-only user
cannot scroll a long line into view — the classic WCAG 2.1.1 failure. Two nested
elements are scrollable, so the inner one is pointer-only.

- **Recorded in:** `specs/E5 Data/F7 DrylCodeBlock.md`

### 2.6 `DrylTreeNode`'s `Disabled` disables only selection

A disabled node still takes focus, still counts as a stop in the arrow-key walk,
still expands and collapses, and can be the roving-`tabindex` target — while
reporting `aria-disabled` to assistive technology.

- **Recorded in:** `specs/E5 Data/F21 DrylTreeNode.md`

---

## Rank 3 — behaviour that is wrong rather than missing

### 3.1 `DrylAvatar` remembers a failed image forever

The load-error flag is never reset, so assigning a new, working `Src` to an
avatar whose previous URL failed keeps showing the fallback for the lifetime of
that instance. A list that reuses avatar instances across rows can show the
wrong person's initials.

- **Recorded in:** `specs/E5 Data/F1 DrylAvatar.md`

### 3.2 `DrylPagination`'s summary is not clamped

The clamp that protects navigation does not protect the display path, so an
out-of-range `CurrentPage` renders "Showing 261–247 of 247" while the controls
behave correctly.

- **Recorded in:** `specs/E5 Data/F13 DrylPagination.md`

### 3.3 `DrylTreeView` cannot be deselected

The view adopts an externally supplied `SelectedValue` only when it is non-null,
so setting the bound value back to `null` leaves the node highlighted and still
reported as selected.

- **Recorded in:** `specs/E5 Data/F20 DrylTreeView.md`

### 3.4 `DrylTable`'s "Select all rows" selects the page

The header checkbox's accessible label says it selects all rows; it selects the
rows in the current view, which under paging is one page of them.

- **Recorded in:** `specs/E5 Data/F16 DrylTable/S4 Selection.md`

### 3.5 `DrylTable`'s view-transition names can collide silently

Without a `RowIdSelector`, the name falls back to the item's hash code. A
duplicate name aborts the whole transition, so the symptom is not a wrong
animation but no animation at all, intermittently and without a message.

- **Recorded in:** `specs/E5 Data/F16 DrylTable/S7 Row reordering and motion.md`

### 3.6 `DrylAvatarGroup`'s cap hides the wrong avatar after a change

Members are appended in registration order and removed by identity, so an avatar
added later always lands at the end. Remove the first participant and add
another, and the cap hides an avatar that is not the last one in the markup.

- **Recorded in:** `specs/E5 Data/F2 DrylAvatarGroup.md`

---

## Rank 4 — floating surfaces that were built twice

`DrylTable`'s per-column filter surface and its column-visibility menu are
hand-built rather than `DrylPopover`s. Both are rendered in place, so both are
clipped inside a scrolling table; neither closes on an outside click; and each
answers `Escape` only if the user has focused it first, which nothing does when
it opens. That last one is the same defect already recorded against
`DrylPopover` itself, reproduced here because these surfaces were built
separately.

- **Recorded in:** `specs/E5 Data/F16 DrylTable/S2 Search and filtering.md` and
  `.../S5 Columns.md`
- **Note:** the right fix depends on `ideas/I4`, which is still waiting on a
  maintainer decision about `DrylPopover`'s exit animation. Moving two more
  surfaces onto that component before that is settled would multiply the
  problem.

---

## Rank 5 — documented debt with no user-visible symptom today

These are recorded in their specs and listed here only so the register is
complete. None of them changes what a user sees right now.

- **`DrylTableKpi` has no demo page and no `ComponentCatalog` entry**
  (`CODE-20`, `REL-04`). This is the direct reason 1.1 survived: the chart with
  the hardcoded colours has never been rendered on the docs site.
  → `F17`
- **Nothing in this category is animated except six components.** `DrylCitation`
  (chip hover), `DrylCodeBlock`, `DrylImage`, `DrylStat`, `DrylTimelineItem` and
  `DrylTable` (aura and morphs), and `DrylTreeNode` (row and chevron). The other
  fifteen have no enter, no exit and no state transition at all — including
  `DrylTimeline`, whose content arrives over time, and `DrylTreeView`, whose main
  gesture is expansion. `DESIGN-11` and `DESIGN-12`, recorded per component
  rather than waived.
- **A dead class and a dead field.** `DrylImage` adds an image-specific class for
  `AiState.Active` that no rule anywhere matches; `DrylTable` declares a
  documented flag for the pinned-column re-measure that nothing reads or writes,
  with a comment describing an intent the code does not implement. → `F11`,
  `F16/S5`
- **Literal geometry in almost every component.** Sizes, type scales and
  spacings written as raw lengths rather than tokens, usually in files whose
  gaps and radii *are* tokens — half-converted rather than untouched. Recorded
  per component; the fullest examples are `F1`, `F3` and `F19`.
- **`DrylPagination`'s size selector is a raw `select`**, not a `DrylSelect`, and
  carries both a visible `label` and a competing `aria-label`. → `F13`
- **`DrylTable` reports misconfiguration with `Console.WriteLine`**, three times,
  with no `ILogger` anywhere in the component. On Blazor Server that is the
  server's console. → `F16/S1`
- **`FilterOperator` has ten members and the client pipeline implements two.**
  The other eight are inert unless a `DataProvider` translates them.
  → `_Api.md`
- **Two components carry neither `Class` nor `AdditionalAttributes`** —
  `DrylDescriptionItem` and `DrylTreeNode`. → `F9`, `F21`, `_Api.md`
- **Sixteen of the twenty-one components have no test of their own.** The
  category's tested components are `DrylBadge`, `DrylIcon` (three names),
  `DrylPagination`, `DrylStat` (count-up only) and `DrylTable` (the morph, and
  little else). Notably untested: `DrylSparkline`'s invariant formatting, which
  is this repository's best-documented recurring failure and which the
  component's own header comment calls out.

---

## Suggested sequencing

1. **1.1 and 1.2 together**, because both are one-line fixes whose second half
   is a check that prevents the recurrence. They are also the two findings a
   reader can verify in a minute.
2. **Rank 2 as one pass.** Six accessibility fixes, all small, all in different
   components — the kind of work that is cheap in a batch and expensive one at a
   time.
3. **Rank 3 individually**, each with the test that would have caught it.
4. **Rank 4 after `ideas/I4` is decided**, not before.
5. **Rank 5 as it is touched.** Recorded debt, not a queue.

Every fix touches a component whose spec now exists, so every fix updates that
spec in the same commit (`SPEC-01`) and leaves it on `Implemented` (`SPEC-04`).
`<Version>` is bumped and a changelog entry cut with the first of them
(`REL-01`, `REL-02`).
