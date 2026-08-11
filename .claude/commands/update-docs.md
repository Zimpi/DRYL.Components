# update-docs

Close the loop after a change to the library: changelog, version, spec, catalog.

**The rules live in [`harness/releasing.md`](../../harness/releasing.md) — read it
rather than working from this file.** This command exists to route you there and
to make sure nothing in the loop is skipped. It deliberately restates no rule: an
earlier version of this file carried its own copy of the release process, drifted
from the harness, and ended up instructing the opposite of two binding rules.

## The loop

1. **`CHANGELOG.md`** — add the entry under `[Unreleased]`, English, correct
   sub-heading. → `REL-02`
2. **`<Version>`** in `code/DRYL.Components/DRYL.Components.csproj` (or
   `code/DRYL.Components.Agents/…` for the agents package) — bump it in the same
   commit. **You own the version, not the maintainer.** PATCH for a fix, MINOR
   for a new component/parameter/feature/token, MAJOR for a breaking change. When
   you bump, cut the release in the changelog in the same commit. → `REL-01`,
   `REL-02`
3. **The component's spec** under `specs/` — every change to behaviour or public
   API updates it in the same commit; a spec that no longer matches its code goes
   back to `State: Modified`. → `SPEC-01`, `SPEC-04`
4. **`ComponentCatalog`** in `DRYL.Website` — register or update the entry. There
   is no component table in `README.md`; do not add one. → `REL-04`

The full checklist is at the end of
[`harness/releasing.md`](../../harness/releasing.md); walk it, don't recall it.

## What needs no changelog entry

Internal refactoring with no visible effect, demo-page-only changes in
`DRYL.Website`, typo fixes in comments or XML docs, CI configuration.

Note that a change needing no changelog entry usually needs no version bump
either — but a change that touches library code and *is* visible needs both, in
one commit.

## Before you claim it is done

Evidence first, assertion second — the command list is in
[`CLAUDE.md`](../../CLAUDE.md) under stage 5. If a step was skipped, say so.
