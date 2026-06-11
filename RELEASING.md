# Releasing DRYL.Components

This document describes how a DRYL release is cut and published. It is for
**maintainers**. Contributors never need to do any of this — they only write
into the `[Unreleased]` section of [`CHANGELOG.md`](CHANGELOG.md).

---

## Versioning policy

DRYL follows [Semantic Versioning](https://semver.org/):

| Change                          | Bump  |
| ------------------------------- | ----- |
| Breaking change to a public API | MAJOR |
| New component or feature        | MINOR |
| Bug fix / visual / docs tweak   | PATCH |

The public API surface frozen at 1.0 is defined by [`CONVENTIONS.md`](CONVENTIONS.md).
After 1.0, any rename of a public parameter / event / enum / slot is a **MAJOR**
change.

### Pre-release plan

```
0.1.0            ──►  0.1.0-preview.N   ──►  1.0.0-rc.N   ──►  1.0.0
(current dev)        (claim nuget id,        (API freeze)      (stable)
                      prove pipeline)
```

Pre-release tags use the SemVer pre-release suffix, e.g. `v0.1.0-preview.1`,
`v1.0.0-rc.1`. NuGet treats these as pre-release and hides them from the default
"stable only" listing.

---

## Where the version number comes from

There are **two** places a version appears, and the source of truth differs by
context:

1. **`DRYL.Components/DRYL.Components.csproj` → `<Version>`** — this is only the
   **local / dev-build default**. It is what you get from a plain
   `dotnet pack` on your machine. Keep it roughly in step with the changelog,
   but it is *not* what ships.
2. **The git tag** — the Release workflow derives the published version from the
   tag (`v1.2.3` → `1.2.3`) and passes it to the build with
   `-p:Version=…`, overriding the csproj value. **The tag is the source of
   truth for a published release.**

So a release version can never drift from its tag: whatever you tag is exactly
what is built, packed and pushed.

---

## Cutting a release

Once the one-time setup is done, every release is just a tag:

```bash
# 1. Make sure CHANGELOG.md [Unreleased] is complete and the working tree is clean.
# 2. (Optional) bump <Version> in DRYL.Components.csproj to match, commit.
# 3. Tag and push:
git tag v1.2.3
git push origin v1.2.3
```

The [`Release`](.github/workflows/release.yml) workflow then automatically, on
any `v*.*.*` tag:

1. restores, **builds** and **tests** the solution with the tag's version,
2. **packs** the `.nupkg` + symbol `.snupkg`,
3. logs in to NuGet via OIDC and **pushes** to nuget.org (`--skip-duplicate`),
4. creates a **GitHub Release** with auto-generated notes and the packages
   attached.

There is nothing to run by hand and no key to paste.

---

## Release checklist

- [ ] `CHANGELOG.md` `[Unreleased]` reflects everything in the release.
- [ ] At release time, promote `[Unreleased]` to a new `[X.Y.Z] — DATE` section
      and reset `[Unreleased]` to empty sub-headings (this is the maintainer's
      job; contributors must never create version sections).
- [ ] `<Version>` in the csproj matches the tag you are about to push.
- [ ] `README.md` component table is up to date.
- [ ] Working tree is clean and on the intended commit.
- [ ] `git tag vX.Y.Z && git push origin vX.Y.Z`.
- [ ] Watch the `Release` workflow go green; confirm the package appears on
      nuget.org and the GitHub Release was created.
