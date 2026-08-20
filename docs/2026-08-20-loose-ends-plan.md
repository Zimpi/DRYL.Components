# Plan: the loose ends left after `I7`

**Branch:** `fix/loose-ends-after-i7` · **Base:** `main` at `131065c` (`2.24.1`,
Agents `0.17.5`, published)

This plan works the list the session handover of 2026-08-17 left open, minus the
two items that are programmes rather than loose ends (phase C's remaining specs
and the Trello backlog). One task per commit, each with its own verification.

---

## The finding that changed task 1

The handover recorded item 5 as: *"`dryl.timepicker.scrollToActive` ist am
verborgenen Panel wirkungslos — dieselbe Ursache wie der behobene Fokusfehler."*

**Measured on 2026-08-20 at `/components/timepicker`, that diagnosis is wrong.**
Three measurements, all on the example bound to `14:30`:

1. **The defect is real.** With the panel open, positioned and visible, both
   `.time-col` elements sit at `scrollTop: 0` while the selected cells are at
   `offsetTop` 490 and 1034 — a user opening a picker set to `14:30` is shown
   `00` / `00` and has to find their own value.
2. **`visibility: hidden` is not the cause.** Called by hand against a panel
   holding `.is-open` but *not* `data-dryl-positioned` — the exact gated state,
   `visibility: hidden` confirmed by `getComputedStyle` — the function scrolled
   the columns to `310` / `854`. A hidden element keeps its layout box, so
   `scrollIntoView` works on it; that is what separates this from the focus bug,
   where `.focus()` on a hidden element genuinely is a no-op.
3. **The portal is the cause.** Hooking the three `dryl.timepicker` entry points
   and opening the panel logs, in order:

   | call | `panel.parentElement` | `scrollTop` before | after |
   |---|---|---|---|
   | `scrollToActive` | `DIV` (anchor) | `[0, 0]` | `[310, 854]` |
   | `focusPanel` | `BODY` (portalled) | `[0, 0]` | `[0, 0]` |

   The scroll succeeds. Between the two calls `dryl.popover.open` runs
   `document.body.appendChild(panel)`, and **re-inserting a node into the DOM
   resets `scrollTop` on every scrollable descendant.** The scroll is not
   ineffective; it is applied and then discarded. Reproduced identically on a
   re-open.

**Therefore the defect belongs to `DrylPopover`, not to `DrylTimePicker`.** A
portal that moves a node is expected to move it *unchanged*; this one silently
drops the scroll state of whatever content it was handed. Any consumer that
scrolls panel content — today the time picker, tomorrow a select that reveals a
far-down option — loses it the same way, and nothing in the primitive says so.

That framing also settles `SPEC-01`. `DrylTimePicker` has no spec and may
therefore not be written to; `DrylPopover` has one (`specs/E11 Surfaces/F1
DrylPopover.md`), it has been read, and the fix lives entirely inside it. No
`DrylTimePicker` file is touched.

---

## Task 1 — the portal preserves the scroll state of what it moves

- **Files:** `code/DRYL.Components/wwwroot/js/dryl.js` (`dryl.popover` `open`
  and `close`) · `specs/E11 Surfaces/F1 DrylPopover.md` · `CHANGELOG.md` ·
  `code/DRYL.Components/DRYL.Components.csproj` · a test in
  `tests/DRYL.Components.Tests/DrylPopoverTests.cs` if bUnit can reach it.
- **Shape:** before each of the two `appendChild` moves, record
  `scrollTop`/`scrollLeft` of every scrollable descendant; restore them after
  the move. Symmetric on open and close, because the close move puts the node
  back under the anchor and resets it just as thoroughly.
- **Not** a `__drylPendingScroll` twin of `__drylPendingFocus`: the pending-focus
  mechanism exists because `.focus()` *cannot work* before the reveal, which is
  not true here — the scroll works, it is simply thrown away. Preserving state
  across the move fixes the class; a second parked-callback channel would fix
  one call site and leave the next one to rediscover it.
- **Version:** core PATCH → `2.24.2`, `CHANGELOG.md` block cut in the same
  commit (`REL-01`, `REL-02`). The spec goes to `State: Modified` and back to
  `Implemented` with the criterion written (`SPEC-01`).
- **Verification:** the three measurements above repeated at
  `/components/timepicker`, in both colour modes; `dotnet test`.

## Task 2 — the three dependency bumps

- **PRs:** `#31` bunit 2.8.6 → 2.9.0 · `#37` xunit.runner.visualstudio 3.1.5 →
  4.0.0 (a MAJOR) · `#36` Microsoft.NET.Test.Sdk 18.8.1 → 18.9.0 **and
  `Microsoft.Agents.AI` 1.15.0 → 1.18.0**.
- The first three are test-project only — `REL-03`, no version, no changelog.
  The fourth is the **shipped** `DRYL.Components.Agents` dependency: it changes
  the package's dependency floor for every consumer, so it takes an Agents PATCH
  (`0.17.5` → `0.17.6`) and a changelog entry, exactly as the
  `1.13.0 → 1.15.0` bump did under `[2.20.2]`.
- Taken on this branch rather than by merging the four Dependabot PRs, because
  merging `#36` as it stands would publish an Agents package whose dependency
  floor moved with no entry saying so.
- **Verification:** `dotnet build` + `dotnet test` after each bump; the xunit
  runner MAJOR gets its own commit so a regression is bisectable.

## Task 3 — `WaitForAssertion` without a timeout

- **Files:** 16 test files, 88 of 95 call sites.
- bUnit's default is 1 s; a hung assertion currently fails with no indication
  that it *timed out* rather than asserted false. Give every call an explicit
  timeout so a red test says which of the two happened.
- **Version:** untouched (`REL-03` — tests only).
- **Verification:** `dotnet test`, and the suite's wall-clock time compared
  before and after, so a raised timeout is not quietly buying green with time.

## Task 4 — the two website findings

- Separate repository (`../DRYL.Website`), separate branch, `REL-03` here.
- `Block` and `MenuPlacement` are undemonstrated on `/components/split-button`.
- Eleven carets announce identically as "More actions" to a screen reader.

---

## Order and why

1 first: it is the only user-visible defect in the list, and it is measured and
understood. 2 next, because a stale test runner makes every later verification
less trustworthy. 3 and 4 are hygiene and can be cut without loss if the session
ends early.

---

## Outcome, 2026-08-20

All four tasks done. Six commits on `fix/loose-ends-after-i7`, one on
`docs/split-button-loose-ends` in `DRYL.Website`.

| Task | Commit | Version |
|---|---|---|
| 1 — the portal carries scroll state | `1f4a9f8` | core `2.24.2` |
| 2a — bunit 2.9.0, Test.Sdk 18.9.0 | `3fd7d7b` | none (`REL-03`) |
| 2b — xunit.runner.visualstudio 4.0.0 | `39b5194` | none (`REL-03`) |
| 2c — Microsoft.Agents.AI 1.18.0 | `54f2cce` | Agents `0.17.6` |
| 3 — one suite-wide wait timeout | `e875c6f` | none (`REL-03`) |
| 4 — the two website findings | `b42302c` (website) | none |

**Task 3 took a different shape than planned.** The plan said "give every call
an explicit timeout" — 88 call sites, many of them multi-line, edited textually.
bUnit exposes `BunitContext.DefaultWaitTimeout` as a settable static, so the
same result is one module initializer: the value is a property of the machine
the suite runs on rather than of any single assertion, and a number repeated 95
times only invites drift. Measured 4 s before and 4 s after, over three full
runs — a raised ceiling is only ever spent by a test that was going to fail.

**Task 4 grew by one item.** The `Variants` example and its description still
described the world before `I6`: the copy read "Primary and Danger work too",
written when `Primary` was the filled treatment, and `Bold` was missing from the
one example whose job is to show the variants. Corrected with the rest.

### Evidence

`dotnet build DRYL.slnx -c Release` clean · `dotnet test` **1066 green** ·
`check-light-sync`, `validate-light-contrast`, `check-harness-links`,
`check-motion-tokens` all OK · coverage unchanged at `15/127` (no component
gained or lost a spec) · `DRYL.Website` 36 tests green · both colour modes
checked by eye, screenshots under `docs/screenshots/2026-08-20-*`.

### One thing seen and not fixed

`DrylAiStreamTests.Direct_mode_renders_each_chunk_as_it_arrives` failed once
during this session and passed in the three full runs after it, and in
isolation. It carries its own explicit 10-second timeouts, so the suite-wide
default of task 3 does not touch it and did not cause this — the file's own
comments already describe this area as flaky under load. It is a pre-existing
race, it is worth a ticket, and it is not this branch's to fix.
