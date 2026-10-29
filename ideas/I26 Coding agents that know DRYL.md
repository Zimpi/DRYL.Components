# Coding agents that know DRYL

## Meta
- **State:** Adopted
- **Specs:** `DRYL.Website/docs/specs/mcp-server.md` (in the `DRYL.Website`
  repository; see the decision on the spec location below)

## Problem

Developers increasingly write Blazor code through coding agents — Claude Code,
Codex, GitHub Copilot, Cursor. DRYL is young and niche, so these agents barely
know it from training. In practice an agent:

1. **Discovers DRYL from the consuming project**, not from a web search: it sees
   `DRYL.Components` in a `.csproj` or `@using DRYL.Components` in `_Imports.razor`.
2. **Guesses the API** — parameter names, enum members, child-content slots —
   and produces Razor that does not compile, or compiles and violates the
   library's intent (string literals where an enum exists, hardcoded colors
   instead of tokens, conditional markup without `DrylPresence`).
3. **Never finds the documentation that already exists.** `components.dryl.dev`
   serves `llms.txt` and `llms-full.txt` (generated at runtime by
   `LlmsDocService` from `ComponentApiService` + `ComponentCatalog`), but nothing
   points an agent there: the NuGet `PACKAGE.md` of both packages does not
   mention it, and no agent tooling registry lists DRYL.

Without this, the library's quality bar (tokens, typed parameters, motion)
erodes in every consuming app that is built with an agent's help — and a
developer's first impression of DRYL is an agent that keeps getting it wrong.

**Target role:** a .NET developer consuming DRYL who works with a coding agent;
secondarily the agent itself as the reader.

## Solution Idea

Three layers, cheapest first:

1. **Point from where the agent already is.** Both packages' `PACKAGE.md` gain a
   short "Using DRYL with AI coding agents" section: the MCP install line per
   client, the `llms.txt` URL, and a copy-ready `AGENTS.md` / `CLAUDE.md`
   snippet carrying the handful of rules that matter to consumers (tokens not
   literals, enums not strings, `DrylPresence` for conditional mounts, `Ai`
   parameter / `AiState`).
2. **A remote MCP server at `components.dryl.dev/mcp`** (Streamable HTTP, no
   install), hosted by `DRYL.Website` and fed from the same source as
   `llms.txt` — so there is no second data set to drift. Tools:
   - `search_components(query)` — matching components with a one-line summary
   - `get_component(name)` — parameters, types, enum members, events, an
     example, AI notes
   - `get_tokens(category?)` — the consumer-facing CSS variables
   - `validate_markup(razor)` — checks component names, parameter names and
     enum values against the real API and returns concrete corrections. This
     is the tool that actually prevents hallucination; the rest only re-serves
     what `llms.txt` already serves.
3. **Make it findable.** Register the server in the official MCP Registry
   (feeds the MCP galleries of VS Code/Copilot and other clients) and submit
   DRYL to Context7.

## Scope
- **In scope:**
  - `PACKAGE.md` of both packages: an "AI coding agents" section with the MCP
    install line per client (Claude Code, Codex, VS Code/Copilot, Cursor), the
    `llms.txt` URL and a copy-ready `AGENTS.md` / `CLAUDE.md` rules snippet.
  - `ComponentApiService` also reflects `DRYL.Components.Agents`; `llms.txt` /
    `llms-full.txt` gain the Agents surface as a side effect.
  - A remote MCP server at `components.dryl.dev/mcp` (Streamable HTTP,
    read-only, anonymous) with four tools: `search_components`,
    `get_component`, `get_tokens`, `validate_markup` (tag scan, no Razor
    compilation). Every answer names the documented DRYL version.
  - Per-IP rate limiting on `/mcp`.
  - Listing in the official MCP Registry and submission to Context7.
- **Out of scope:**
  - A local NuGet-packaged MCP server (`PackageType McpServer`, run via `dnx`)
    — revisit once the remote server is used.
  - Per-version answers / API snapshots per release.
  - Write tools: scaffolding, code generation or file edits by the server.
  - Authentication, API keys, usage accounts.
  - `validate_markup` beyond names and enum values: no type checking of
    expressions, no Razor compilation, no CSS / token-usage linting.
  - MCP resources and prompts — tools only in the first cut.

## Impact
- **Harness:** no new token, animation or `AiState`. **Dependency:** the MCP
  server needs the `ModelContextProtocol.AspNetCore` NuGet package in
  `DRYL.Website` — not in the library, so `CODE-03` does not bind, but it is a
  new dependency and is surfaced here for sign-off.
- **Specs:** no component spec changes. The website is not covered by
  `specs/`; the MCP contract (tool names, inputs, outputs) needs a home —
  open point.
- **Public API:** none in `DRYL.Components` / `DRYL.Components.Agents`. The MCP
  tool surface becomes a new public contract of its own and is hard to rename
  once registered.
- **Code:** library side only `PACKAGE.md` of both packages (→ version bump +
  changelog per `REL-01`/`REL-02`). Website side: an MCP endpoint in
  `Program.cs`, tools on top of `ComponentApiService`, `ComponentCatalog`,
  `ExampleSourceProvider`; tests in `tests/DRYL.Website.Tests` alongside
  `DiscoverabilityEndpointTests`. Findings from the feasibility check:
  - `ComponentApiService` reflects **only** `DRYL.Components`
    (`typeof(DrylButton).Assembly`) — the Agents package is not in the API
    catalog today.
  - The website references the library by `ProjectReference`, so it only
    knows the **current** version. Version-aware answers are not free.
  - A public, unauthenticated endpoint needs rate limiting.

## Decisions
- 2026-10-29: The Product Owner chooses the direction "remote MCP on the
  website + pointers in the NuGet package + registry listings" over a
  web-search-only approach, because agents meet DRYL in the consuming project,
  not through search.
- 2026-10-29: **Current version only.** The server documents the version the
  website is built against and states it in every answer ("documented for
  DRYL x.y.z"). Since the 1.0 API freeze, drift between versions is almost
  always additive, so an older project at worst sees a parameter it does not
  have yet. Per-release API snapshots (option B) are deferred until a real
  complaint asks for them.
- 2026-10-29: **The Agents package is in from the start.** `ComponentApiService`
  reflects `DRYL.Components.Agents` too — which also completes `llms.txt`. The
  agent-facing traps live there (`DrylAiStream.Source` reference stability,
  run/tool wiring), and AI-native is DRYL's differentiator.
- 2026-10-29: **`validate_markup` is in the first cut, deliberately lean:** no
  Razor compilation — a scan of `<Dryl…>` tags that checks component exists,
  parameter exists, enum value is valid, and suggests the nearest match for
  each miss (e.g. `Variant="primary"` → `ButtonVariant.Primary`). Without it
  the server only re-serves `llms.txt` in pieces.
- 2026-10-29: **The MCP contract is specified in `DRYL.Website`**
  (`docs/specs/mcp-server.md`, same format as here: Meta with State and
  Source, INVEST criteria), next to the code it describes. Deliberate
  deviation from `IDEA-07`'s "Adopted links a spec under `specs/`":
  `check-spec-coverage.mjs` requires every `Source` path to exist in this
  repository, so a spec here could not name the website code it governs, and
  spec and code would drift — the opposite of `SPEC-01`. When adopted, this
  document links the website spec instead.
- 2026-10-29: **Rate limiting with ASP.NET Core's built-in limiter:** fixed
  window per client IP on `/mcp`, starting at 60 requests per minute, `429`
  with `Retry-After` on exceed. `ForwardedHeaders` is configured so clients
  behind the hosting proxy are not collapsed onto one IP. No extra package.
- 2026-10-29: **Sign-off:** `ModelContextProtocol.AspNetCore` (the official
  C# MCP SDK) may be added to `DRYL.Website`. The library packages stay
  dependency-free; hand-rolling JSON-RPC, sessions and Streamable HTTP would
  be fragile and trail every protocol revision.

- 2026-10-29: Out of scope confirmed as listed under `## Scope`.
- 2026-10-29: The Product Owner confirmed the final version; the idea is
  `Ready` and carried into the website spec the same day.

## Open Points
