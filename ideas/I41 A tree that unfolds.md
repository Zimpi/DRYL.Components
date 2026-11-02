# A tree that unfolds

## Meta
- **State:** Adopted
- **Specs:** `specs/E5 Data/F20 DrylTreeView.md`, `specs/E5 Data/F21 DrylTreeNode.md`, `specs/E5 Data/_Interop.md`
- **Plan:** `docs/plans/2026-11-02-i41-tree-unfolds-plan.md`

## Problem

Raised by the Product Owner on 2026-11-02, for release 3.16.0:

> "Komponente um die es geht: DrylTreeView / DrylTreeNode. Mögliche Idee: Die
> Gruppe klappt mit dem 0fr → 1fr-Trick auf, Kinder kommen leicht gestaffelt;
> die aktive Zeile bekommt die Gleit-Pill."

Expanding is the tree's main gesture, and it is the one thing about the tree
that does not move: the children group is shown and hidden by the `hidden`
attribute. Both specs record this as debt against `DESIGN-11` and `DESIGN-12`.
The selection jumps from row to row as an `--accent-soft` fill, while the drawer
(I29) and the menus and option lists (I38) already mark their row with the
gliding ink. In a deep tree nothing but the indent says where the selected row
sits.

Target role: the end user moving through a hierarchy by pointer or keyboard; the
Blazor developer, who gets the motion with no new parameter.

## Solution Idea

Three variants in the prototype artifact
`https://claude.ai/artifact/9G3j1QFS1zf5VpbstxJwpq` (with today's look as a
fourth tab for comparison):

- **A — Ink.** The Product Owner's idea as stated: the group unfolds with
  `grid-template-rows` 0fr → 1fr, children fall out from under their folder, the
  selected row carries the ink with its small bar.
- **B — Thread.** A, plus a 1 px guide line per open level that draws itself on
  opening, and an accent thread running from the root through every level into
  the selected row. It withdraws and regrows when the selection moves. The ink
  drops its bar, because the thread ends in it. *Tech Lead recommendation.*
- **C — Shell.** An open folder becomes a `--glass-1` shell that grows with it,
  so nesting reads as layers; the shells on the path to the selection carry an
  `--accent-line` edge. Each level costs width.

Shared by all three: the group opens on `--dur-slow` and closes on `--dur-med`;
a closed group is `visibility: hidden`, so nothing in it can take focus; the
stagger uses half of `--reveal-step` and is capped at seven rows, so the last
row lands as the height finishes however long the folder is; the row's contents
are what rises, so the row's box stays still and the ink can measure it; the ink
glides when the selection moves and tracks frame by frame when a folder above
moves the row (`navGlide`'s mechanism); the first render places everything
without motion; without script the static `--accent-soft` fill stays; reduced
motion switches all of it off.

## Scope
- **In scope:** the group's open and close motion, the stagger, the chevron on
  the spring, the ink on the selected row, the hover light.
- **Out of scope:** type-ahead and the expand-all key; `Disabled` enforcement;
  clearing `SelectedValue`; AI styling; click-on-row-expands. These are recorded
  gaps or behaviour decisions, not part of a visual rework.

## Impact
- **Harness:** no new token, no new easing, no new dependency in any variant.
  The stagger reads `--reveal-step`. B's thread is a 1 px line and a small
  indicator (`DESIGN-08`).
- **Specs:** both have specs; their "Expansion", "Appearance" and "Motion"
  criteria change and the animation debt leaves "Recorded gaps".
- **Public API:** none planned.
- **Code:** `DrylTreeNode.razor` replaces `hidden` with the grid wrapper;
  `DrylTreeView` attaches an ink engine next to its key handling; rules in
  `DrylTreeNode.razor.css` and `wwwroot/dryl.css`. Risk: a consumer's CSS or test
  that relies on `.tree-group[hidden]`, which the changelog has to name.
- **Website:** the tree demo and its example.

## Decisions
- 2026-11-02: Variant **B — Thread** is built (Product Owner: "B Faden. Bring es
  dann in den Release branch rein"). A and C are dropped.
- 2026-11-02: A closed folder holding the selection takes the ink as an empty
  frame, and the ink returns when the folder opens. The focus moves to a folder
  that closes around it, and the ring moves to `--accent-line`. The ink follows
  the selection only, not the pointer. (Tech Lead proposal, shown in the round
  with the variants; the Product Owner's go-ahead covered them.)
- 2026-11-02: The ink is the root's `::before` and the thread each group's
  `::after`, in the components' own stylesheets. The drawer's and the list ink's
  rules stay as they are: this ink has no bar, and it needs a third mode ("track")
  that the list ink does not have (Tech Lead).
- 2026-11-02: A disabled node is now dimmed, as the prototype showed it. It reported
  `aria-disabled` with no visible difference. What `Disabled` enforces stays a
  recorded gap (Tech Lead).

## Open Points
