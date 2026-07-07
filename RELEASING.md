# Releasing DRYL.Components

DRYL ships **continuously**. There is no separate "cut a release" ritual: every
push to `main` that changes the published version number is a release.

---

## How it works

The source of truth for the published version is
**`DRYL.Components/DRYL.Components.csproj` → `<Version>`**.

On every push to `main`, the [`Publish`](.github/workflows/publish.yml) workflow:

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

To ship, bump `<Version>` in `DRYL.Components.csproj` in the same commit as your
change, keep `CHANGELOG.md` in step (see below), and push to `main`. That's it.

The version bump follows [Semantic Versioning](https://semver.org/):

| Change                          | Bump  |
| ------------------------------- | ----- |
| Breaking change to a public API | MAJOR |
| New component or feature        | MINOR |
| Bug fix / visual / docs tweak   | PATCH |

The public API surface frozen at 1.0 is defined by [`CONVENTIONS.md`](CONVENTIONS.md).
After 1.0, any rename of a public parameter / event / enum / slot is a **MAJOR**
change.

Pre-release versions use the SemVer pre-release suffix directly in `<Version>`,
e.g. `1.0.0-rc.1`. The tag becomes `v1.0.0-rc.1` and NuGet treats it as a
pre-release, hidden from the default "stable only" listing.

Changes that do **not** touch shippable library code (docs, samples, CI, tests
only) should leave `<Version>` untouched — the push is a no-op and publishes
nothing. That is expected and correct.

---

## Changelog

Accumulate entries under `[Unreleased]` in [`CHANGELOG.md`](CHANGELOG.md) as you
work. When you bump `<Version>`, cut the release in the changelog in the **same
commit**: rename the `[Unreleased]` block to `## [X.Y.Z] - YYYY-MM-DD` (the
version you just set, today's date) and start a fresh, empty `[Unreleased]` above
it. Every published version stays traceable to its entries.

---

## Release checklist

- [ ] `<Version>` in `DRYL.Components.csproj` bumped (PATCH / MINOR / MAJOR) for
      this change.
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
