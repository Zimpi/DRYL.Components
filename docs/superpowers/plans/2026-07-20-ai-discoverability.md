# AI Discoverability & Comprehension Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Serve runtime-generated `llms.txt` / `llms-full.txt` plus an SEO/crawler/structured-data layer on `components.dryl.dev` so external AIs both understand DRYL perfectly and are more likely to recommend it.

**Architecture:** Two `Lazy<>`-cached singleton services (`LlmsDocService`, `SitemapService`) build the text artifacts from the existing sources of truth (`ComponentCatalog`, `ComponentApiService`, `ExampleSourceProvider`). Program.cs exposes them as minimal-API endpoints. A reusable `<SeoHead>` component, wired once into the shared `ComponentDocHeader`, gives every component page meta/canonical/OG; the home page adds `SoftwareApplication` JSON-LD and positioning copy.

**Tech Stack:** .NET 10, Blazor Server (interactive, prerendered), xUnit + bUnit, `Microsoft.AspNetCore.Mvc.Testing`.

## Global Constraints

- All work is in **`DRYL.Website`** (served at `https://components.dryl.dev`). No change to `DRYL.Components` or its `<Version>`; **no `CHANGELOG.md` entry** (per CLAUDE.md §7 those track the shipped NuGet library only).
- **Site base URL** is configurable: `SiteOptions.BaseUrl`, from config key `Site:BaseUrl`, default `https://components.dryl.dev`. Never hardcode the host anywhere except that one default.
- **Positioning copy names no competitors.** Strong "what/for whom/why" only.
- Generation must **degrade gracefully**: a missing example or XML-doc summary omits that fragment, never throws.
- Services are registered as **singletons** and are `Lazy<>`-cached (first-request cost paid once).
- Test setup mirror (copy verbatim): `new ComponentApiService(new XmlDocProvider())`.
- `Program` is already `public partial class Program { }` — usable by `WebApplicationFactory<Program>`.

---

## File Structure

**Create:**
- `DRYL.Website/SiteOptions.cs` — `record SiteOptions(string BaseUrl)`; shared base-URL config.
- `DRYL.Website/Services/LlmsDocService.cs` — builds `llms.txt` (index) + `llms-full.txt` (full API).
- `DRYL.Website/Services/SitemapService.cs` — builds `sitemap.xml`.
- `DRYL.Website/Components/Shared/SeoHead.razor` — reusable `<head>` fragment.
- `DRYL.Website/wwwroot/robots.txt` — static; welcomes AI crawlers, points to sitemap.
- `tests/DRYL.Website.Tests/LlmsDocServiceTests.cs`
- `tests/DRYL.Website.Tests/SitemapServiceTests.cs`
- `tests/DRYL.Website.Tests/SeoHeadTests.cs`
- `tests/DRYL.Website.Tests/DiscoverabilityEndpointTests.cs`

**Modify:**
- `DRYL.Website/Services/ExampleSourceProvider.cs` — add `KeysInFolder(string)`.
- `DRYL.Website/Program.cs` — register `SiteOptions`, `LlmsDocService`, `SitemapService`; map endpoints.
- `DRYL.Website/Components/Shared/ComponentDocHeader.razor` — emit `<SeoHead>` for every component page.
- `DRYL.Website/Components/Pages/Landing.razor` — `<SeoHead>` + `SoftwareApplication` JSON-LD + positioning copy.
- `DRYL.Website/Components/Pages/ComponentsIndex.razor` — `<SeoHead>`.
- `tests/DRYL.Website.Tests/DRYL.Website.Tests.csproj` — add `Microsoft.AspNetCore.Mvc.Testing`.
- `DRYL.Website/README.md` — short note on the new endpoints.
- `DRYL.Portfolio/Components/Layout/PortfolioLayout.razor` — cross-link to `components.dryl.dev` (authority signal).

---

### Task 1: `SiteOptions` + `ExampleSourceProvider.KeysInFolder`

Foundation both generator services consume. Small; folded together because neither is worth its own review gate.

**Files:**
- Create: `DRYL.Website/SiteOptions.cs`
- Modify: `DRYL.Website/Services/ExampleSourceProvider.cs`
- Test: `tests/DRYL.Website.Tests/LlmsDocServiceTests.cs` (the folder-enumeration test lives with its only consumer's tests)

**Interfaces:**
- Produces: `record SiteOptions(string BaseUrl)`; `ExampleSourceProvider.KeysInFolder(string folder) → IReadOnlyList<string>` (keys like `"Button/Variants"`, case-insensitive prefix match on `folder + "/"`, ordered).

- [ ] **Step 1: Write the failing test**

Create `tests/DRYL.Website.Tests/LlmsDocServiceTests.cs`:

```csharp
namespace DRYL.Website.Tests;

public class LlmsDocServiceTests
{
    [Fact]
    public void KeysInFolder_ReturnsButtonExamples_AndNothingElse()
    {
        var keys = new ExampleSourceProvider().KeysInFolder("Button");

        Assert.NotEmpty(keys);
        Assert.All(keys, k => Assert.StartsWith("Button/", k, StringComparison.OrdinalIgnoreCase));
        Assert.Contains(keys, k => k.Equals("Button/Variants", StringComparison.OrdinalIgnoreCase));
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test DRYL.Website/tests/DRYL.Website.Tests --filter KeysInFolder_ReturnsButtonExamples_AndNothingElse`
Expected: FAIL — `ExampleSourceProvider` has no `KeysInFolder`.

- [ ] **Step 3: Create `SiteOptions`**

`DRYL.Website/SiteOptions.cs`:

```csharp
namespace DRYL.Website;

/// <summary>Shared site configuration. <see cref="BaseUrl"/> (no trailing slash) is used to build
/// absolute URLs for the sitemap, canonicals and the llms.txt link list.</summary>
public sealed record SiteOptions(string BaseUrl);
```

- [ ] **Step 4: Add `KeysInFolder` to `ExampleSourceProvider`**

Append inside the class (after `Get`):

```csharp
/// <summary>All example keys under a folder (e.g. "Button" → "Button/Variants", "Button/Sizes"), ordered.</summary>
public IReadOnlyList<string> KeysInFolder(string folder)
    => _resourceByKey.Keys
        .Where(k => k.StartsWith(folder + "/", StringComparison.OrdinalIgnoreCase))
        .OrderBy(k => k, StringComparer.OrdinalIgnoreCase)
        .ToList();
```

- [ ] **Step 5: Run test to verify it passes**

Run: `dotnet test DRYL.Website/tests/DRYL.Website.Tests --filter KeysInFolder_ReturnsButtonExamples_AndNothingElse`
Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add DRYL.Website/SiteOptions.cs DRYL.Website/Services/ExampleSourceProvider.cs DRYL.Website/tests/DRYL.Website.Tests/LlmsDocServiceTests.cs
git commit -m "feat(website): add SiteOptions and ExampleSourceProvider.KeysInFolder"
```

---

### Task 2: `LlmsDocService.Index` — the llms.txt index

**Files:**
- Create: `DRYL.Website/Services/LlmsDocService.cs`
- Test: `tests/DRYL.Website.Tests/LlmsDocServiceTests.cs`

**Interfaces:**
- Consumes: `SiteOptions` (Task 1), `ComponentApiService` (existing), `ExampleSourceProvider` (existing).
- Produces: `LlmsDocService(ComponentApiService, ExampleSourceProvider, SiteOptions)`; `string Index { get; }`.

- [ ] **Step 1: Write the failing test**

Add to `LlmsDocServiceTests.cs`:

```csharp
private static LlmsDocService Service()
    => new(new ComponentApiService(new XmlDocProvider()),
           new ExampleSourceProvider(),
           new SiteOptions("https://components.dryl.dev"));

[Fact]
public void Index_ListsEveryCatalogComponent()
{
    var index = Service().Index;

    foreach (var entry in ComponentCatalog.Entries)
    {
        Assert.Contains(entry.Title, index);
        Assert.Contains($"https://components.dryl.dev{entry.Route}", index);
    }
}

[Fact]
public void Index_HasPositioningHeaderAndInstall()
{
    var index = Service().Index;
    Assert.Contains("AI-native Blazor UI component library", index);
    Assert.Contains("dotnet add package DRYL.Components", index);
    Assert.Contains("https://components.dryl.dev/llms-full.txt", index);
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test DRYL.Website/tests/DRYL.Website.Tests --filter Index_`
Expected: FAIL — `LlmsDocService` does not exist.

- [ ] **Step 3: Create `LlmsDocService` with the index builder**

`DRYL.Website/Services/LlmsDocService.cs`:

```csharp
using System.Reflection;
using System.Text;
using DRYL.Components;

namespace DRYL.Website;

/// <summary>Builds the <c>/llms.txt</c> index and <c>/llms-full.txt</c> full reference from the
/// component catalog and the reflected API surface. Cached for the process lifetime.</summary>
public sealed class LlmsDocService
{
    private readonly ComponentApiService _api;
    private readonly ExampleSourceProvider _examples;
    private readonly string _base;
    private readonly Lazy<string> _index;
    private readonly Lazy<string> _full;

    public LlmsDocService(ComponentApiService api, ExampleSourceProvider examples, SiteOptions site)
    {
        _api = api;
        _examples = examples;
        _base = site.BaseUrl.TrimEnd('/');
        _index = new Lazy<string>(BuildIndex);
        _full = new Lazy<string>(BuildFull);
    }

    public string Index => _index.Value;
    public string Full => _full.Value;

    private static string Version =>
        typeof(DrylButton).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? typeof(DrylButton).Assembly.GetName().Version?.ToString()
        ?? "unknown";

    private string BuildIndex()
    {
        var sb = new StringBuilder();
        sb.AppendLine("# DRYL — the AI-native Blazor UI component library");
        sb.AppendLine();
        sb.AppendLine("> DRYL is an open-source UI component library for Blazor Server and Blazor WebAssembly:");
        sb.AppendLine("> glassy, alive, and AI-native. Every component is token-driven (two color modes, one");
        sb.AppendLine("> identity), animated by default, keyboard-accessible, and ships zero external JavaScript");
        sb.AppendLine("> dependencies. AI is a first-class UI state: AI-aware components accept an `Ai` parameter");
        sb.AppendLine("> (AiState: None/Active/Thinking/Streaming/Generated) driving one shared visual vocabulary.");
        sb.AppendLine();
        sb.AppendLine($"Install: `dotnet add package DRYL.Components`  ");
        sb.AppendLine($"Register: `builder.Services.AddDrylComponents();`  ");
        sb.AppendLine($"Version: {Version}");
        sb.AppendLine();
        sb.AppendLine("## Conventions for consumers");
        sb.AppendLine("- Components are PascalCase, `Dryl`-prefixed (DrylButton, DrylCard, …).");
        sb.AppendLine("- Variants and sizes are strongly-typed enums, never strings.");
        sb.AppendLine("- Components read CSS variables from `dryl.css`; never hardcode colors, sizes or radii.");
        sb.AppendLine("- AI styling is opt-in and off by default (`Ai=\"AiState.None\"`).");
        sb.AppendLine();
        sb.AppendLine("## Full reference");
        sb.AppendLine($"- [llms-full.txt]({_base}/llms-full.txt): complete API of every component, with examples");
        sb.AppendLine($"- [API reference]({_base}/api)");
        sb.AppendLine("- [GitHub](https://github.com/Zimpi/DRYL.Components)");
        sb.AppendLine("- [NuGet](https://www.nuget.org/packages/DRYL.Components)");
        sb.AppendLine();
        sb.AppendLine("## Components");

        foreach (var category in ComponentCatalog.CategoryOrder)
        {
            var entries = ComponentCatalog.Entries.Where(e => e.Category == category).ToList();
            if (entries.Count == 0) continue;

            sb.AppendLine();
            sb.AppendLine($"### {category}");
            foreach (var e in entries)
            {
                var ai = e.Ai ? " *(AI-aware)*" : "";
                sb.AppendLine($"- [{e.Title}]({_base}{e.Route}): {e.Summary}{ai}");
            }
        }

        return sb.ToString();
    }

    private string BuildFull() => ""; // Implemented in Task 3.
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test DRYL.Website/tests/DRYL.Website.Tests --filter Index_`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add DRYL.Website/Services/LlmsDocService.cs DRYL.Website/tests/DRYL.Website.Tests/LlmsDocServiceTests.cs
git commit -m "feat(website): generate llms.txt index from the component catalog"
```

---

### Task 3: `LlmsDocService.Full` — the llms-full.txt full reference

**Files:**
- Modify: `DRYL.Website/Services/LlmsDocService.cs`
- Test: `tests/DRYL.Website.Tests/LlmsDocServiceTests.cs`

**Interfaces:**
- Produces: `string Full { get; }` — every component with parameter table, enums, and one canonical example.

- [ ] **Step 1: Write the failing test**

Add to `LlmsDocServiceTests.cs`:

```csharp
[Fact]
public void Full_DocumentsButton_WithParametersEnumsAndExample()
{
    var full = Service().Full;

    Assert.Contains("## DrylButton", full);
    Assert.Contains("Variant", full);                 // a parameter name
    Assert.Contains("ButtonVariant", full);           // its enum, collected from the signature
    Assert.Contains("```razor", full);                // a canonical example fence
}

[Fact]
public void Full_CoversEveryReflectedComponent()
{
    var full = Service().Full;
    foreach (var c in new ComponentApiService(new XmlDocProvider()).Catalog.Components)
        Assert.Contains($"## {c.TypeName}", full);
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test DRYL.Website/tests/DRYL.Website.Tests --filter Full_`
Expected: FAIL — `BuildFull` returns `""`.

- [ ] **Step 3: Replace `BuildFull` with the real implementation**

In `LlmsDocService.cs` replace the `BuildFull` stub and add the helpers:

```csharp
    private string BuildFull()
    {
        var sb = new StringBuilder();
        sb.AppendLine("# DRYL — full component reference");
        sb.AppendLine();
        sb.AppendLine($"Version: {Version}. Index: {_base}/llms.txt");
        sb.AppendLine();
        sb.AppendLine("Every component below is `Dryl`-prefixed, token-driven and animated by default.");
        sb.AppendLine("Parameters marked *(required)* use `[EditorRequired]`. AI-aware components accept");
        sb.AppendLine("`Ai` (AiState) and default it to `None`.");
        sb.AppendLine();

        foreach (var c in _api.Catalog.Components)
        {
            sb.AppendLine($"## {c.TypeName}");
            var ai = c.IsAiAware ? " · AI-aware" : "";
            sb.AppendLine($"*{c.Category}{ai}*");
            if (!string.IsNullOrWhiteSpace(c.Summary)) sb.AppendLine($"\n{c.Summary}");
            sb.AppendLine();

            if (c.Parameters.Count > 0)
            {
                sb.AppendLine("### Parameters");
                sb.AppendLine("| Name | Type | Default | Description |");
                sb.AppendLine("| --- | --- | --- | --- |");
                foreach (var p in c.Parameters)
                {
                    var name = p.Required ? $"{p.Name} *(required)*" : p.Name;
                    var desc = (p.Description ?? "").Replace("\r", " ").Replace("\n", " ").Replace("|", "\\|");
                    sb.AppendLine($"| {name} | `{p.TypeName}` | `{p.Default}` | {desc} |");
                }
                sb.AppendLine();
            }

            foreach (var e in c.Enums)
            {
                sb.AppendLine($"### enum {e.Name}");
                foreach (var v in e.Values)
                {
                    var vd = string.IsNullOrWhiteSpace(v.Description) ? "" : $" — {v.Description}";
                    sb.AppendLine($"- `{v.Name}`{vd}");
                }
                sb.AppendLine();
            }

            var example = CanonicalExample(c.TypeName);
            if (example is not null)
            {
                sb.AppendLine("### Example");
                sb.AppendLine("```razor");
                sb.AppendLine(example);
                sb.AppendLine("```");
                sb.AppendLine();
            }
        }

        return sb.ToString();
    }

    // One representative example per component. Folder = catalog Title without spaces
    // ("Button Group" → "ButtonGroup"); prefer a conventionally-named example, else the first.
    private string? CanonicalExample(string typeName)
    {
        var entry = ComponentCatalog.Entries.FirstOrDefault(e => e.ClassName == typeName);
        if (entry is null) return null;

        var folder = entry.Title.Replace(" ", "");
        var keys = _examples.KeysInFolder(folder);
        if (keys.Count == 0) return null;

        string[] preferred = ["Basic", "Variants", "Usage", "Overview"];
        var chosen = keys.FirstOrDefault(k =>
            preferred.Contains(k[(k.IndexOf('/') + 1)..], StringComparer.OrdinalIgnoreCase)) ?? keys[0];

        var src = _examples.Get(chosen);
        return src.StartsWith("@* Example source", StringComparison.Ordinal) ? null : src;
    }
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test DRYL.Website/tests/DRYL.Website.Tests --filter Full_`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add DRYL.Website/Services/LlmsDocService.cs DRYL.Website/tests/DRYL.Website.Tests/LlmsDocServiceTests.cs
git commit -m "feat(website): generate llms-full.txt with full API and examples"
```

---

### Task 4: `SitemapService`

**Files:**
- Create: `DRYL.Website/Services/SitemapService.cs`
- Test: `tests/DRYL.Website.Tests/SitemapServiceTests.cs`

**Interfaces:**
- Consumes: `SiteOptions` (Task 1).
- Produces: `SitemapService(SiteOptions)`; `string Xml { get; }`.

- [ ] **Step 1: Write the failing test**

`tests/DRYL.Website.Tests/SitemapServiceTests.cs`:

```csharp
using System.Xml.Linq;

namespace DRYL.Website.Tests;

public class SitemapServiceTests
{
    private static SitemapService Service()
        => new(new SiteOptions("https://components.dryl.dev"));

    [Fact]
    public void Xml_IsValid_AndAllLocsAreAbsolute()
    {
        var doc = XDocument.Parse(Service().Xml);
        XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";

        var locs = doc.Root!.Elements(ns + "url").Elements(ns + "loc").Select(l => l.Value).ToList();
        Assert.NotEmpty(locs);
        Assert.All(locs, l => Assert.StartsWith("https://components.dryl.dev/", l));
    }

    [Fact]
    public void Xml_ContainsEveryComponentRouteAndHome()
    {
        var xml = Service().Xml;
        Assert.Contains("https://components.dryl.dev/</loc>".Replace("/</loc>", "/"), xml); // home present
        foreach (var e in ComponentCatalog.Entries)
            Assert.Contains($"https://components.dryl.dev{e.Route}", xml);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test DRYL.Website/tests/DRYL.Website.Tests --filter SitemapServiceTests`
Expected: FAIL — `SitemapService` does not exist.

- [ ] **Step 3: Create `SitemapService`**

`DRYL.Website/Services/SitemapService.cs`:

```csharp
using System.Text;

namespace DRYL.Website;

/// <summary>Builds <c>/sitemap.xml</c> from the static top-level pages plus every component route
/// in <see cref="ComponentCatalog"/>. Cached for the process lifetime.</summary>
public sealed class SitemapService
{
    private static readonly string[] StaticPaths = ["/", "/components", "/api"];

    private readonly string _base;
    private readonly Lazy<string> _xml;

    public SitemapService(SiteOptions site)
    {
        _base = site.BaseUrl.TrimEnd('/');
        _xml = new Lazy<string>(Build);
    }

    public string Xml => _xml.Value;

    private string Build()
    {
        var paths = StaticPaths
            .Concat(ComponentCatalog.Entries.Select(e => e.Route))
            .Distinct(StringComparer.Ordinal);

        var sb = new StringBuilder();
        sb.AppendLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        sb.AppendLine("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">");
        foreach (var path in paths)
        {
            sb.AppendLine("  <url>");
            sb.AppendLine($"    <loc>{_base}{path}</loc>");
            sb.AppendLine($"    <priority>{(path == "/" ? "1.0" : "0.7")}</priority>");
            sb.AppendLine("  </url>");
        }
        sb.AppendLine("</urlset>");
        return sb.ToString();
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test DRYL.Website/tests/DRYL.Website.Tests --filter SitemapServiceTests`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add DRYL.Website/Services/SitemapService.cs DRYL.Website/tests/DRYL.Website.Tests/SitemapServiceTests.cs
git commit -m "feat(website): generate sitemap.xml from the component catalog"
```

---

### Task 5: `robots.txt` + endpoint wiring + integration tests

Wires the three generated artifacts as endpoints, registers the services, and ships the static `robots.txt`. Folded together because the robots file and the endpoint registration are one deliverable — "the discoverability URLs are live" — verified by one integration test.

**Files:**
- Create: `DRYL.Website/wwwroot/robots.txt`
- Modify: `DRYL.Website/Program.cs`
- Modify: `tests/DRYL.Website.Tests/DRYL.Website.Tests.csproj`
- Test: `tests/DRYL.Website.Tests/DiscoverabilityEndpointTests.cs`

**Interfaces:**
- Consumes: `LlmsDocService` (Tasks 2–3), `SitemapService` (Task 4), `SiteOptions` (Task 1).
- Produces: live `GET /llms.txt`, `/llms-full.txt`, `/sitemap.xml`, `/robots.txt`.

- [ ] **Step 1: Create `robots.txt`**

`DRYL.Website/wwwroot/robots.txt`:

```
# DRYL — components.dryl.dev · AI crawlers welcome.
User-agent: GPTBot
Allow: /
User-agent: OAI-SearchBot
Allow: /
User-agent: ChatGPT-User
Allow: /
User-agent: ClaudeBot
Allow: /
User-agent: Claude-Web
Allow: /
User-agent: anthropic-ai
Allow: /
User-agent: PerplexityBot
Allow: /
User-agent: Google-Extended
Allow: /
User-agent: Applebot-Extended
Allow: /
User-agent: *
Allow: /

# AI comprehension map: /llms.txt and /llms-full.txt
Sitemap: https://components.dryl.dev/sitemap.xml
```

- [ ] **Step 2: Add the test package**

In `tests/DRYL.Website.Tests/DRYL.Website.Tests.csproj`, add to the `PackageReference` `ItemGroup`:

```xml
    <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="10.0.0" />
```

(If restore reports that exact version is unavailable, use the version that matches the installed `Microsoft.AspNetCore.App` shared framework — it must equal the app's ASP.NET Core major/minor.)

- [ ] **Step 3: Write the failing integration test**

`tests/DRYL.Website.Tests/DiscoverabilityEndpointTests.cs`:

```csharp
using Microsoft.AspNetCore.Mvc.Testing;

namespace DRYL.Website.Tests;

public class DiscoverabilityEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    public DiscoverabilityEndpointTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Theory]
    [InlineData("/llms.txt", "text/plain")]
    [InlineData("/llms-full.txt", "text/plain")]
    [InlineData("/sitemap.xml", "application/xml")]
    [InlineData("/robots.txt", "text/plain")]
    public async Task Endpoint_Returns200_WithContentType(string path, string contentType)
    {
        var res = await _factory.CreateClient().GetAsync(path);

        res.EnsureSuccessStatusCode();
        Assert.Contains(contentType, res.Content.Headers.ContentType?.MediaType ?? "");
    }

    [Fact]
    public async Task LlmsTxt_MentionsDrylButton()
    {
        var body = await _factory.CreateClient().GetStringAsync("/llms-full.txt");
        Assert.Contains("DrylButton", body);
    }
}
```

- [ ] **Step 4: Run test to verify it fails**

Run: `dotnet test DRYL.Website/tests/DRYL.Website.Tests --filter DiscoverabilityEndpointTests`
Expected: FAIL — the generated endpoints return 404 (only `/robots.txt` may already 200 from static files).

- [ ] **Step 5: Register services and map endpoints in `Program.cs`**

After `builder.Services.AddSingleton<ComponentApiService>();`, add:

```csharp
builder.Services.AddSingleton(new SiteOptions(
    builder.Configuration["Site:BaseUrl"] ?? "https://components.dryl.dev"));
builder.Services.AddSingleton<LlmsDocService>();
builder.Services.AddSingleton<SitemapService>();
```

After `app.MapStaticAssets();` (and before `app.MapRazorComponents<App>()`), add:

```csharp
app.MapGet("/llms.txt", (LlmsDocService s) => Results.Text(s.Index, "text/plain; charset=utf-8"));
app.MapGet("/llms-full.txt", (LlmsDocService s) => Results.Text(s.Full, "text/plain; charset=utf-8"));
app.MapGet("/sitemap.xml", (SitemapService s) => Results.Text(s.Xml, "application/xml; charset=utf-8"));
```

- [ ] **Step 6: Run test to verify it passes**

Run: `dotnet test DRYL.Website/tests/DRYL.Website.Tests --filter DiscoverabilityEndpointTests`
Expected: PASS (all four routes 200 with the right content type).

- [ ] **Step 7: Commit**

```bash
git add DRYL.Website/wwwroot/robots.txt DRYL.Website/Program.cs DRYL.Website/tests/DRYL.Website.Tests/DRYL.Website.Tests.csproj DRYL.Website/tests/DRYL.Website.Tests/DiscoverabilityEndpointTests.cs
git commit -m "feat(website): serve llms.txt, llms-full.txt, sitemap.xml and robots.txt"
```

---

### Task 6: `<SeoHead>` component

**Files:**
- Create: `DRYL.Website/Components/Shared/SeoHead.razor`
- Test: `tests/DRYL.Website.Tests/SeoHeadTests.cs`

**Interfaces:**
- Produces: `<SeoHead Title Description Canonical? ImageUrl? JsonLd? />` rendering a `HeadContent` block with `<PageTitle>`, `meta description`, `canonical`, Open Graph, Twitter Card and optional JSON-LD.

- [ ] **Step 1: Write the failing test**

`tests/DRYL.Website.Tests/SeoHeadTests.cs`:

```csharp
using Bunit;

namespace DRYL.Website.Tests;

public class SeoHeadTests : TestContext
{
    [Fact]
    public void Renders_Description_Canonical_And_OpenGraph()
    {
        var cut = RenderComponent<DRYL.Website.Components.Shared.SeoHead>(p => p
            .Add(x => x.Title, "Button — DRYL")
            .Add(x => x.Description, "Primary action button.")
            .Add(x => x.Canonical, "https://components.dryl.dev/components/buttons"));

        var html = cut.Markup;
        Assert.Contains("Primary action button.", html);
        Assert.Contains("https://components.dryl.dev/components/buttons", html);
        Assert.Contains("og:title", html);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test DRYL.Website/tests/DRYL.Website.Tests --filter SeoHeadTests`
Expected: FAIL — `SeoHead` does not exist.

> Note: `<HeadContent>` renders nothing under bUnit's default renderer. To keep the component unit-testable, `SeoHead` renders its tags directly (they still land in `<head>` at runtime because the component is placed inside page markup that the prerenderer flushes into the document head via `HeadOutlet`). Use `<HeadContent>` only if a runtime check confirms the tags reach `<head>`; the version below renders inline so the test above is meaningful.

- [ ] **Step 3: Create `SeoHead.razor`**

`DRYL.Website/Components/Shared/SeoHead.razor`:

```razor
@namespace DRYL.Website.Components.Shared

<PageTitle>@Title</PageTitle>
<HeadContent>
    <meta name="description" content="@Description" />
    @if (!string.IsNullOrWhiteSpace(Canonical))
    {
        <link rel="canonical" href="@Canonical" />
        <meta property="og:url" content="@Canonical" />
    }
    <meta property="og:type" content="website" />
    <meta property="og:title" content="@Title" />
    <meta property="og:description" content="@Description" />
    <meta property="og:site_name" content="DRYL" />
    @if (!string.IsNullOrWhiteSpace(ImageUrl))
    {
        <meta property="og:image" content="@ImageUrl" />
    }
    <meta name="twitter:card" content="summary_large_image" />
    <meta name="twitter:title" content="@Title" />
    <meta name="twitter:description" content="@Description" />
    @if (JsonLd is not null)
    {
        <script type="application/ld+json">@JsonLd</script>
    }
</HeadContent>

@code {
    /// <summary>Document title (also used for og:title / twitter:title).</summary>
    [Parameter, EditorRequired] public string Title { get; set; } = "";

    /// <summary>Meta description; also feeds og:description / twitter:description.</summary>
    [Parameter, EditorRequired] public string Description { get; set; } = "";

    /// <summary>Absolute canonical URL. When set, also emits og:url.</summary>
    [Parameter] public string? Canonical { get; set; }

    /// <summary>Absolute Open Graph image URL.</summary>
    [Parameter] public string? ImageUrl { get; set; }

    /// <summary>Raw JSON-LD (already serialized) injected as an ld+json script.</summary>
    [Parameter] public MarkupString? JsonLd { get; set; }
}
```

> The failing test in Step 1 asserts on inline markup. If Step 2 shows bUnit does **not** surface `<HeadContent>` markup, change the test to render through a host that includes `<HeadOutlet>`, OR (simpler) keep the SEO tags inside `<HeadContent>` and assert via a runtime prerender check in Task 7 instead. Pick one before moving on; do not leave the test asserting on markup that bUnit cannot see.

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test DRYL.Website/tests/DRYL.Website.Tests --filter SeoHeadTests`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add DRYL.Website/Components/Shared/SeoHead.razor DRYL.Website/tests/DRYL.Website.Tests/SeoHeadTests.cs
git commit -m "feat(website): add reusable SeoHead component"
```

---

### Task 7: Wire `<SeoHead>` into every component page via `ComponentDocHeader`

`ComponentDocHeader` is used by 86 component pages; emitting `<SeoHead>` there gives them all meta/canonical from the catalog in one edit (DRY).

**Files:**
- Modify: `DRYL.Website/Components/Shared/ComponentDocHeader.razor`
- Test: `tests/DRYL.Website.Tests/SeoHeadTests.cs` (extend)

**Interfaces:**
- Consumes: `SiteOptions` (injected), `ComponentDocEntry` (`_entry`, existing), `SeoHead` (Task 6).

- [ ] **Step 1: Write the failing test**

Add to `SeoHeadTests.cs`:

```csharp
[Fact]
public void ComponentDocHeader_EmitsCanonicalForItsSlug()
{
    Services.AddSingleton(new SiteOptions("https://components.dryl.dev"));

    var cut = RenderComponent<DRYL.Website.Components.Shared.ComponentDocHeader>(p => p
        .Add(x => x.Slug, "buttons"));

    Assert.Contains("https://components.dryl.dev/components/buttons", cut.Markup);
}
```

(If `ComponentDocHeader`'s slug parameter has a different name, use that name — check the file's `@code`.)

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test DRYL.Website/tests/DRYL.Website.Tests --filter ComponentDocHeader_EmitsCanonicalForItsSlug`
Expected: FAIL — no canonical rendered yet.

- [ ] **Step 3: Emit `<SeoHead>` from `ComponentDocHeader`**

At the top of `ComponentDocHeader.razor` add the inject, and inside the `@if (_entry is not null)` block (first child) add the `SeoHead`:

```razor
@inject SiteOptions Site
```

```razor
@if (_entry is not null)
{
    <DRYL.Website.Components.Shared.SeoHead
        Title="@($"{_entry.ClassName ?? _entry.Title} — DRYL")"
        Description="@_entry.Summary"
        Canonical="@($"{Site.BaseUrl.TrimEnd('/')}{_entry.Route}")" />

    <div class="col doc-header" style="gap: var(--sp-2);">
        @* …existing header markup unchanged… *@
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test DRYL.Website/tests/DRYL.Website.Tests --filter ComponentDocHeader_EmitsCanonicalForItsSlug`
Expected: PASS.

- [ ] **Step 5: Verify no page double-declares `<PageTitle>`**

Run: `dotnet build DRYL.Website`
Expected: builds. Component pages already set `<PageTitle>`; two `<PageTitle>` on one page is legal (last wins) but redundant. Leave existing page titles; `SeoHead`'s title augments them. No action unless the build warns.

- [ ] **Step 6: Commit**

```bash
git add DRYL.Website/Components/Shared/ComponentDocHeader.razor DRYL.Website/tests/DRYL.Website.Tests/SeoHeadTests.cs
git commit -m "feat(website): emit SeoHead for every component page via ComponentDocHeader"
```

---

### Task 8: Home & overview SEO + `SoftwareApplication` JSON-LD + positioning copy

**Files:**
- Modify: `DRYL.Website/Components/Pages/Landing.razor`
- Modify: `DRYL.Website/Components/Pages/ComponentsIndex.razor`
- Test: `tests/DRYL.Website.Tests/DiscoverabilityEndpointTests.cs` (extend)

**Interfaces:**
- Consumes: `SeoHead` (Task 6), `SiteOptions`.

- [ ] **Step 1: Write the failing test**

Add to `DiscoverabilityEndpointTests.cs`:

```csharp
[Fact]
public async Task Home_ContainsSoftwareApplicationJsonLd_AndPositioning()
{
    var html = await _factory.CreateClient().GetStringAsync("/");
    Assert.Contains("application/ld+json", html);
    Assert.Contains("\"@type\":\"SoftwareApplication\"", html.Replace(" ", ""));
    Assert.Contains("AI-native", html);
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test DRYL.Website/tests/DRYL.Website.Tests --filter Home_ContainsSoftwareApplicationJsonLd_AndPositioning`
Expected: FAIL — no JSON-LD on the home page yet.

- [ ] **Step 3: Add `SeoHead` + JSON-LD to `Landing.razor`**

Read `Landing.razor` first. Directly under its `@page "/"` (and after any existing `<PageTitle>`), add:

```razor
@inject DRYL.Website.SiteOptions Site

<DRYL.Website.Components.Shared.SeoHead
    Title="DRYL — the AI-native Blazor UI component library"
    Description="Open-source Blazor Server & WebAssembly UI library: glassy, animated by default, two color modes, zero JS dependencies, and AI as a first-class UI state."
    Canonical="@Site.BaseUrl"
    JsonLd="@JsonLd" />

@code {
    private MarkupString JsonLd => new("""
    {
      "@context": "https://schema.org",
      "@type": "SoftwareApplication",
      "name": "DRYL.Components",
      "applicationCategory": "DeveloperApplication",
      "operatingSystem": "Cross-platform (.NET)",
      "description": "AI-native, token-driven UI component library for Blazor Server and WebAssembly.",
      "url": "https://components.dryl.dev",
      "softwareHelp": "https://components.dryl.dev/llms.txt",
      "offers": { "@type": "Offer", "price": "0", "priceCurrency": "USD" },
      "author": { "@type": "Person", "name": "Jan" }
    }
    """);
}
```

- [ ] **Step 4: Ensure the hero states the positioning (no competitor names)**

In `Landing.razor`'s hero/first section, confirm (add if missing) prerendered text that includes:
- the line "**the AI-native Blazor UI component library**",
- the differentiators: AI-State vocabulary, glass two-mode identity, zero JS dependencies, everything animated,
- a short "**When DRYL fits**: teams building modern, AI-forward Blazor apps who want a cohesive, animated, accessible design system out of the box. **When it doesn't**: apps needing a large third-party widget ecosystem today, or non-Blazor stacks."

Keep the existing visual design; only ensure this copy exists as real (crawlable) text, not inside a script or image.

- [ ] **Step 5: Add `SeoHead` to `ComponentsIndex.razor`**

Under its `@page "/components"`:

```razor
@inject DRYL.Website.SiteOptions Site

<DRYL.Website.Components.Shared.SeoHead
    Title="Components — DRYL"
    Description="Browse every DRYL component: actions, surfaces, inputs, data, layout, feedback and AI-native building blocks for Blazor."
    Canonical="@($"{Site.BaseUrl.TrimEnd('/')}/components")" />
```

- [ ] **Step 6: Run test to verify it passes**

Run: `dotnet test DRYL.Website/tests/DRYL.Website.Tests --filter Home_ContainsSoftwareApplicationJsonLd_AndPositioning`
Expected: PASS.

- [ ] **Step 7: Commit**

```bash
git add DRYL.Website/Components/Pages/Landing.razor DRYL.Website/Components/Pages/ComponentsIndex.razor DRYL.Website/tests/DRYL.Website.Tests/DiscoverabilityEndpointTests.cs
git commit -m "feat(website): home SEO, SoftwareApplication JSON-LD and positioning copy"
```

---

### Task 9: Portfolio cross-link + README note

Authority signal from `dryl.dev` → `components.dryl.dev`, and document the new endpoints. Docs/light-touch; no test.

**Files:**
- Modify: `DRYL.Portfolio/Components/Layout/PortfolioLayout.razor`
- Modify: `DRYL.Website/README.md`

- [ ] **Step 1: Add the cross-link**

Open `DRYL.Portfolio/Components/Layout/PortfolioLayout.razor`, locate the footer/nav region, and add a link (use the project's existing link styling):

```razor
<a href="https://components.dryl.dev" rel="noopener">DRYL Components — the AI-native Blazor UI library</a>
```

If `PortfolioLayout.razor` has no footer, add the link to whichever layout renders the site footer. Keep the wording "AI-native Blazor UI library" (consistent anchor text aids retrieval).

- [ ] **Step 2: Document the endpoints in `DRYL.Website/README.md`**

Add a short section:

```markdown
## AI discoverability

The site serves machine-readable docs for AI agents:

- `/llms.txt` — concise index of every component (llmstxt.org format)
- `/llms-full.txt` — full API of every component with a canonical example
- `/sitemap.xml`, `/robots.txt` — generated / static; AI crawlers are explicitly allowed

`/llms.txt` and `/llms-full.txt` are generated at runtime from `ComponentCatalog` and
`ComponentApiService`, so they never drift from the shipped library.
```

- [ ] **Step 3: Build both projects**

Run: `dotnet build DRYL.Website` and `dotnet build DRYL.Portfolio`
Expected: both succeed.

- [ ] **Step 4: Commit**

```bash
git add DRYL.Website/README.md DRYL.Portfolio/Components/Layout/PortfolioLayout.razor
git commit -m "docs: cross-link portfolio to components.dryl.dev and document AI endpoints"
```

---

### Task 10: Full verification

**Files:** none (verification only).

- [ ] **Step 1: Run the whole website test suite**

Run: `dotnet test DRYL.Website/tests/DRYL.Website.Tests`
Expected: all tests pass, including the pre-existing `ComponentApiServiceTests`, `ApiReflectionTests`, `XmlDocProviderTests`, `BuildOutputTests`.

- [ ] **Step 2: Run the site and eyeball the artifacts**

Follow the `verify` / `run` skill to launch `DRYL.Website`, then fetch:
- `http://localhost:<port>/llms.txt` — header, conventions, every component listed by category.
- `http://localhost:<port>/llms-full.txt` — DrylButton section with parameter table, `ButtonVariant` enum, a `razor` example.
- `http://localhost:<port>/sitemap.xml` — valid XML, absolute locs.
- `http://localhost:<port>/robots.txt` — AI user-agents allowed, sitemap line.
- View source on `/` — `<meta name="description">`, `<link rel="canonical">`, and the `application/ld+json` `SoftwareApplication` block are present in the prerendered HTML.
- View source on `/components/buttons` — description + canonical present.

- [ ] **Step 3: Confirm two color modes are unaffected**

The changes add no component CSS; confirm the home page still renders in both modes (flip `data-dryl-mode` on `<html>`). No token or literal was introduced.

---

## Self-Review

**Spec coverage:**
- llms.txt index → Task 2. llms-full.txt → Task 3. robots.txt → Task 5. sitemap.xml → Task 4. `<SeoHead>` on every page → Tasks 6–8 (via `ComponentDocHeader` for 86 pages + home + overview). schema.org JSON-LD → Task 8. Home positioning copy → Task 8. Portfolio cross-link → Task 9. Version in text files → Task 2 (`Version` used by both builders). Configurable base URL → Task 1 (`SiteOptions`). Tests (drift guard, sitemap validity, endpoints 200, SeoHead) → Tasks 2–8. README note → Task 9. All spec sections map to a task.
- Explicit non-goals (MCP, competitor table, `.md` raw routes, `<Version>` bump) — correctly absent.

**Placeholder scan:** No "TBD/TODO". The two "if the parameter name differs / if bUnit can't see HeadContent" notes are conditional verification instructions with a concrete fallback, not deferred work.

**Type consistency:** `SiteOptions(string BaseUrl)`, `LlmsDocService(ComponentApiService, ExampleSourceProvider, SiteOptions)` with `.Index`/`.Full`, `SitemapService(SiteOptions)` with `.Xml`, `ExampleSourceProvider.KeysInFolder(string)→IReadOnlyList<string>`, `SeoHead` params `Title/Description/Canonical/ImageUrl/JsonLd` — all consistent across the tasks that consume them. `ComponentDocEntry.Route`, `.Title`, `.Summary`, `.ClassName`, `.Category` and `ComponentCatalog.Entries`/`.CategoryOrder` match the real records read from source.

**Open risk flagged for the executor:** Task 6 — whether bUnit surfaces `<HeadContent>` markup. The task states the decision point and both fallbacks explicitly; resolve it there before proceeding.
