# Releasing DRYL.Components

DRYL ships **continuously**. There is no separate "cut a release" ritual: every
push to `main` that changes the published version number is a release.

Every rule below has a stable ID. IDs are never reused: if a rule is dropped,
its number is burned.

**Status** — `binding` blocks the merge · `default` needs a reason in the PR ·
`guidance` is a recommendation.
**Enforced** — how compliance is established: `script`, `grep` or `review`.

---

## How it works

The source of truth for the published version is
**`code/DRYL.Components/DRYL.Components.csproj` → `<Version>`**.

On every push to `main`, the [`Publish`](../.github/workflows/publish.yml) workflow:

1. reads `<Version>` from the csproj,
2. checks whether a `v<Version>` git tag already exists,
3. **if the version is new** — restores, builds, **tests**, packs the `.nupkg` +
   symbol `.snupkg`, logs in to NuGet via OIDC and **pushes** to nuget.org
   (`--skip-duplicate`), then creates the `v<Version>` tag and a **GitHub
   Release** with auto-generated notes,
4. **if the version is unchanged** — the tag already exists, so it is a clean
   **no-op**. Nothing is published.

There is nothing to run by hand and no key to paste. Publishing happens *inline*
in this workflow (not via a tag that triggers another workflow — a tag pushed by
the default `GITHUB_TOKEN` does not fire further workflows).

---

## Releasing = bumping the version

To ship, bump `<Version>` in `code/DRYL.Components/DRYL.Components.csproj` in
the same commit as your change, keep `CHANGELOG.md` in step (see below), and
push to `main`. That's it.

The version bump follows [Semantic Versioning](https://semver.org/):

| Change                          | Bump  |
| ------------------------------- | ----- |
| Breaking change to a public API | MAJOR |
| New component or feature        | MINOR |
| Bug fix / visual / docs tweak   | PATCH |

The public API surface frozen at 1.0 is defined by [`conventions.md`](conventions.md).
After 1.0, any rename of a public parameter / event / enum / slot is a **MAJOR**
change.

Pre-release versions use the SemVer pre-release suffix directly in `<Version>`,
e.g. `1.0.0-rc.1`. The tag becomes `v1.0.0-rc.1` and NuGet treats it as a
pre-release, hidden from the default "stable only" listing.

Changes that do **not** touch shippable library code (docs, CI, tests
only) should leave `<Version>` untouched — the push is a no-op and publishes
nothing. That is expected and correct. See `REL-03`.

---

## Rules

### REL-01 — You own the version

Status: **binding** | Enforced: **review**

DRYL ships continuously. **You are the version owner**, not the maintainer.
Every push to `main` is a potential release. Whenever you touch library code,
bump `<Version>` in `code/DRYL.Components/DRYL.Components.csproj` in the same
commit as the change: bug fix → **PATCH**, new component/parameter/feature →
**MINOR**, breaking API change → **MAJOR**. Never publish by hand or push a
`v*` tag yourself — the workflow owns tagging (see `REL-05`). Keep `<Version>`
and `CHANGELOG.md` in lockstep (see `REL-02`).

**An unpublished version carries the whole stack.** Once `<Version>` names a
version that has not shipped yet, later commits add their entries to that same
block and leave `<Version>` alone; raise it only when a change needs a larger
bump than the block already carries (a stack of fixes that gains a public
parameter becomes MINOR). Nothing is published between the commits of an
unreleased block, so bumping per commit would invent versions that never
existed — and when the bump no longer matches the newest entry on its own, say
why in the block, as `2.23.0` does.

Check: reviewer confirms the version bump matches the change type — PATCH for
a fix, MINOR for a new component/parameter/feature, MAJOR for a breaking
change — and, where a block collects several commits, that its bump matches the
largest change in it. No automated scan documented yet, since the correct bump
depends on judgment about the change, not a pattern a script can match.

### REL-02 — CHANGELOG and every consumer-facing artefact is written in English

Status: **binding** | Enforced: **review**

`CHANGELOG.md` lives at the repository root and follows
[Keep a Changelog](https://keepachangelog.com/) (v1.1.0) format with
[Semantic Versioning](https://semver.org/). **Write it in English — always,
without exception.** `CHANGELOG.md` is public-facing: it ships in the NuGet
package, and every release cut from it becomes a GitHub Release read by
people who do not speak German. This holds for the release intro line as much
as for the bullets, and it holds no matter what language the conversation
that produced the change was in. Same rule for `README.md`, XML doc comments
and every other artefact a consumer of the library sees.

Accumulate entries under `[Unreleased]` in [`CHANGELOG.md`](../CHANGELOG.md)
as you work. **When you bump `<Version>` (`REL-01`), cut a release in the
changelog in the same commit:** rename the `[Unreleased]` block to
`## [X.Y.Z] - YYYY-MM-DD` (the version you just set, today's date) and start
a fresh, empty `[Unreleased]` above it. That keeps every published version
traceable to its entries.

Pick the right sub-heading for each change:

| Sub-heading  | When to use                                                                    |
| ------------ | ------------------------------------------------------------------------------- |
| `Added`      | New component, new parameter, new CSS token, new service method                 |
| `Changed`    | Altered behaviour or API of an existing component (non-breaking)                |
| `Deprecated` | Something that still works but will be removed in a future MAJOR version        |
| `Removed`    | Something deleted (only allowed in a MAJOR bump — coordinate with maintainer)    |
| `Fixed`      | Bug fix, visual regression, accessibility issue                                 |

**Entry format** — one bullet per logical change, component name in backticks:

```markdown
### Added
- `DrylSpinner` — New loading indicator; variants: Ring / Dots / Pulse; AI-Mode
- `DrylCard` — New `Elevation` parameter (`Low / Mid / High`) controls shadow depth
```

**Versioning rules** — you apply these yourself by bumping `<Version>`
(`REL-01`):

| Change type              | Bump  |
| ------------------------ | ----- |
| New component or feature | MINOR |
| Bug fix / visual tweak   | PATCH |
| Breaking API change      | MAJOR |

Check: reviewer confirms the changelog entry and every consumer-facing
artefact touched by the PR (`README.md`, XML doc comments, release notes) is
in English — no automated scan documented yet, since language correctness is
not something a grep can verify. Today's `CHANGELOG.md` and `README.md` are
entirely English; no violation found.

### REL-03 — Changes that do not touch shippable library code leave `<Version>` alone

Status: **default** | Enforced: **review**

If a change does **not** touch shippable library code (docs, CI, tests
only), leave the version alone. A push with an unchanged version finds
the tag already present and is a clean no-op — nothing is published, and that
is correct, not a bug.

The following do **not** need a changelog entry either, for the same reason —
they carry no shippable, consumer-visible change:

- Internal refactoring with no visible effect
- Changes to demo pages in `DRYL.Website` only
- Typo fixes in comments or XML doc strings
- Changes to CI/build configuration

Check: reviewer confirms a docs/CI/tests-only PR leaves `<Version>`
unchanged — no automated scan documented yet, since "touches shippable
library code" is a judgment call a script cannot make reliably (e.g. a
`harness/`-only change and a `code/DRYL.Components/`-only change are both
just diffs).

### REL-04 — Register the component in `ComponentCatalog` (`DRYL.Website`)

Status: **binding** | Enforced: **review**

The canonical, browsable component list lives at **components.dryl.dev**,
driven by the website's `ComponentCatalog` (in `DRYL.Website`). There is no
component table in `README.md` — do not add one.

When you add a new component or make a user-visible change to an existing
one:

1. **Register it in `ComponentCatalog`** in the website project — this is
   what powers the nav, search and overview page on the docs site.
2. **Add a changelog entry** under `[Unreleased]` in `CHANGELOG.md`
   (`REL-02`).

That is all. Do not maintain a duplicate list in `README.md` or any other
markdown file.

Check: reviewer confirms new/changed components are registered in
`ComponentCatalog` in `DRYL.Website` — no automated scan documented yet, since
the `DRYL.Website` project is not part of this repository's `code/` tree, so
no grep against it is possible from here. `grep -in component README.md`
confirms `README.md` still carries no component table — currently **green**
(mentions of "component" are prose and links, not a table).

### REL-05 — Never publish by hand or push a `v*` tag yourself

Status: **binding** | Enforced: **review**

The [`Publish`](../.github/workflows/publish.yml) workflow owns tagging and
publishing end to end: it reads `<Version>`, builds, tests, packs, pushes to
nuget.org and creates the `v<Version>` tag plus the GitHub Release. Never run
`dotnet nuget push` by hand and never push a `v*` tag yourself — just bump
the number and commit; the push does the rest.

Check: reviewer confirms no manual `dotnet nuget push` or manually pushed
`v*` tag appears in the change under review — no automated scan documented
yet, since this is about what a contributor did outside the diff, not
something visible in the diff itself.

---

## Changelog

Accumulate entries under `[Unreleased]` in [`CHANGELOG.md`](../CHANGELOG.md) as you
work. When you bump `<Version>`, cut the release in the changelog in the **same
commit**: rename the `[Unreleased]` block to `## [X.Y.Z] - YYYY-MM-DD` (the
version you just set, today's date) and start a fresh, empty `[Unreleased]` above
it. Every published version stays traceable to its entries.

---

## Release checklist

- [ ] `<Version>` in `code/DRYL.Components/DRYL.Components.csproj` bumped
      (PATCH / MINOR / MAJOR) for this change.
- [ ] `CHANGELOG.md` — `[Unreleased]` promoted to `## [X.Y.Z] - DATE` matching the
      new version, fresh empty `[Unreleased]` added on top.
- [ ] Working tree clean and on the intended commit; push to `main`.
- [ ] Watch the `Publish` workflow go green; confirm the package appears on
      nuget.org and the GitHub Release was created.

---

## One-time setup (already done)

- **Trusted Publishing (OIDC)** configured on nuget.org for
  Owner → Trusted Publishing → GitHub Actions, repository `DRYL.Components`,
  workflow file `publish.yml`.
- Repository secret `NUGET_USER` set to the nuget.org account/owner used for OIDC
  login.

---

## Before you finish a task

- [ ] `CHANGELOG.md` — entry under `[Unreleased]` with the correct sub-heading (`REL-02`)
- [ ] `ComponentCatalog` in `DRYL.Website` — registered or updated (`REL-04`)
- [ ] `<Version>` bumped and in lockstep with the changelog release you cut (`REL-01`)
- [ ] The component's spec updated and its `State` correct (`SPEC-01`, `SPEC-04`)
