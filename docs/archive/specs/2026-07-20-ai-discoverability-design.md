# AI Discoverability & Comprehension — Design

**Date:** 2026-07-20
**Status:** Approved (design), pending implementation plan
**Scope:** `DRYL.Website` (served at `components.dryl.dev`). No change to the shipped `DRYL.Components` NuGet package.

---

## Goal

Two intertwined goals for **external** AIs (Claude Code, ChatGPT, Gemini, Perplexity, …) that are *not* the DRYL maintainer:

1. **Comprehension** — an AI that lands on DRYL's docs (or is pointed at them) understands the library *perfectly*: which components exist, their full API, how to use them, the design constraints.
2. **Discoverability** — when a developer asks an AI *"recommend me a Blazor UI component library"*, the AI is more likely to surface DRYL **and** already knows it well.

**Honest framing (drives the two-layer design):** these are different problems.
`llms.txt` solves comprehension. Getting *recommended* comes from (a) training-data presence and
(b) live web-search / RAG grounding at query time — a single file cannot deliver it. So we build a
comprehension artifact **and** a thin discoverability layer (crawlability, structured data,
positioning) that makes DRYL a strong, retrievable answer.

## Decisions (locked during brainstorming)

- **Primary channel:** hosted URLs on `components.dryl.dev` (the library's home). `dryl.dev` is the
  separate personal portfolio and only cross-links to the library.
- **Scope:** both layers — comprehension (`llms.txt`) *and* discoverability (SEO/crawler/structured data).
- **Positioning tone:** strong positioning **without naming competitors**. No competitor comparison table.
- **Generation strategy:** runtime-generated endpoints (Approach A) from the existing sources of truth,
  with a small hand-written editorial header. Correct by construction on every deploy; never drifts with
  the library's continuous version bumps.

## Non-goals (YAGNI)

- ❌ MCP server (a possible future, separate channel).
- ❌ Named-competitor comparison table (explicitly declined).
- ❌ Per-page `.md` raw routes (`llms-full.txt` covers the AI's needs in one fetch).
- ❌ Any `DRYL.Components.csproj` version bump — this is the website app, not the NuGet package.

---

## Existing assets we reuse (no re-invention)

- **`ComponentCatalog`** (`DRYL.Website/Components/ComponentCatalog.cs`) — curated single source of
  truth: `Title, Slug, Category, ClassName, Folder, Ai, Summary, Icon` for every documented component,
  plus `CategoryOrder`. Drives the `llms.txt` index and the sitemap.
- **`ComponentApiService` / `ApiCatalog`** (`DRYL.Website/Services/`) — reflects the full public API
  surface: components → parameters (type, default, kind, XML-doc description), enums (values + docs),
  services/models. Drives `llms-full.txt`.
- **`ExampleSourceProvider`** — embedded raw `.razor` source of example components. Supplies one
  canonical usage example per component in `llms-full.txt`.
- **Assembly version** of `DRYL.Components` — stamped into the generated text files so AIs cite the
  correct version.

---

## Layer 1 — Comprehension artifacts

| Artifact | Format | Purpose | Source |
|---|---|---|---|
| `/llms.txt` | Markdown ([llmstxt.org](https://llmstxt.org)) | **Index**: H1, positioning blockquote, curated link list of every component grouped by category, links to `/llms-full.txt`, `/api`, GitHub, NuGet. | editorial header + `ComponentCatalog` |
| `/llms-full.txt` | Markdown | **Full text**: per component — summary, full parameter table (name, type, default, description), referenced enums with value docs, AI-support flag, one canonical code example. An AI can both *understand* and *correctly use* DRYL from this single file. | `ComponentApiService` + `ExampleSourceProvider` |

Both files carry the current library version and a short "design rules for consumers" preamble
(token-driven, `Ai` opt-in, `Dryl`-prefixed, enum variants) so generated code follows DRYL conventions.

## Layer 2 — Discoverability

| Artifact / change | Purpose | Source |
|---|---|---|
| `/robots.txt` | Explicitly welcome AI crawlers (GPTBot, ClaudeBot, OAI-SearchBot, PerplexityBot, Google-Extended, Applebot-Extended, …); point to `sitemap.xml` and `llms.txt`. | static in `wwwroot` |
| `/sitemap.xml` | Every component route + static pages, valid XML. | `ComponentCatalog` + page list |
| `<SeoHead>` on every page | `meta description`, `canonical`, Open Graph, Twitter Card. Default description falls back to `ComponentCatalog.Summary` so no page ships description-less. | reusable component |
| schema.org JSON-LD | `SoftwareApplication` / `SoftwareSourceCode` on home/overview; `TechArticle` on component pages. Emitted in prerendered `<head>`. | `<SeoHead>` / page |
| Home positioning copy | "DRYL — the AI-native Blazor UI library"; differentiators (AI-State vocabulary, glass two-mode identity, zero JS deps, everything animated); "when DRYL fits / when it doesn't". No competitor names. Fully prerendered → crawlable. | editorial |
| Portfolio cross-link | `dryl.dev` links to `components.dryl.dev` — an authority signal. | `DRYL.Portfolio` |

**Crawlability note:** the site is interactive Blazor Server, but prerendering is on by default, so
the initial HTTP response contains full server-rendered HTML including `<head>` content emitted via
`HeadContent`/`HeadOutlet`. Meta tags, canonical and JSON-LD therefore appear in the crawled document.

---

## Units (isolated, testable)

- **`LlmsDocService`** (`DRYL.Website/Services/`) — builds the `llms.txt` and `llms-full.txt` strings.
  `Lazy<>`-cached. Depends only on `ComponentCatalog`, `ComponentApiService`, `ExampleSourceProvider`,
  and the library assembly version. Pure string production; no HTTP concerns.
- **`SitemapService`** (`DRYL.Website/Services/`) — builds `sitemap.xml`. Depends only on
  `ComponentCatalog` + a static-page list. Base URL injected (config), defaults to
  `https://components.dryl.dev`.
- **`<SeoHead>`** (`DRYL.Website/Components/Shared/`) — parameters `Title`, `Description`,
  `Canonical?`, `ImageUrl?`, `JsonLd?`. Renders one `HeadContent` block. Pages pass `Title` +
  `Description`; the rest defaults.
- **Endpoint mapping** in `Program.cs` — four `MapGet` routes:
  `/llms.txt` (text/plain), `/llms-full.txt` (text/plain), `/sitemap.xml` (application/xml),
  `/robots.txt` (static file). Sensible `Cache-Control`.

Each unit answers: *what it does* (produce one artifact), *how you use it* (inject + call one method,
or drop one component in a page), *what it depends on* (only the catalog/api sources above).

## Data flow

```
ComponentCatalog ─┐
ComponentApiService├─► LlmsDocService ─► /llms.txt, /llms-full.txt
ExampleSourceProvider┘
ComponentCatalog ──► SitemapService ─► /sitemap.xml
wwwroot/robots.txt ─────────────────► /robots.txt (static)
ComponentCatalog.Summary ─► <SeoHead> ─► per-page <head> (meta/canonical/OG/JSON-LD)
```

## Error handling & edge cases

- Generation is pure and deterministic; a missing example or XML doc degrades gracefully (section
  omitted, never throws). Services are `Lazy<>`-cached so a first-request cost is paid once.
- `<SeoHead>` with no `Description` falls back to the catalog summary, then to a site-wide default —
  never emits an empty `description`.
- Base URL is configurable so staging/local don't emit production canonicals.

## Testing (`DRYL.Website/tests`)

- **Drift guard:** `llms.txt` and `llms-full.txt` each contain **every** `ComponentCatalog` entry
  (title/slug) — fails if a new component is added but the generator misses it.
- **Sitemap:** contains every route, parses as valid XML, every `<loc>` is absolute.
- **Endpoints:** each returns `200` with the correct `Content-Type`.
- **`<SeoHead>`:** renders `canonical` and a non-empty `description`.

## Documentation / release impact

- No `CHANGELOG.md` entry and no `<Version>` bump: per `CLAUDE.md` §7 these track the shipped
  `DRYL.Components` library, and this work touches only `DRYL.Website`.
- `DRYL.Website/README.md` gets a short note on the new endpoints and how the SEO layer works.
