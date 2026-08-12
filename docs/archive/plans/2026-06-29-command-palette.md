# DrylCommandPalette — Command/Registry/AI Layer Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add the spec's "a `DrylCommand` *is* the tool" layer — declarative `DrylCommand`/`DrylCommandArgument`, an `ICommandRegistry`, a fuzzy matcher, and an opt-in `ICommandResolver` AI seam — *additively* onto the already-shipped `DrylCommandPalette`, plus a `DrylAiCommandResolver`/`DrylAiCommandPalette` in `DRYL.Components.Agents`.

**Architecture:** The existing `CommandItem[]` / `SearchProvider` API stays untouched and fully working. A new unit of functionality — `DrylCommand` (with optional `DrylCommandArgument` children) — self-registers into a shared `ICommandRegistry`. The palette fuzzy-matches registry commands and renders them alongside `CommandItem` results in one unified, keyboard-navigable list. One execution path (`DrylCommand.OnRun(CommandContext)`) serves click, Enter, and the AI. When a `Resolver` is supplied, a natural-language query is resolved by the model into one real tool call, surfaced as a confirmable top suggestion (human-in-the-loop, never auto-fired). The core carries **zero** AI dependency; AI is injected through the narrow `ICommandResolver` seam.

**Tech Stack:** Blazor (Razor components, net8/9/10), C#, bUnit + xUnit tests, `Microsoft.Agents.AI` 1.10.0 (Agents package only), `dryl.js` interop (existing `dryl.commandpalette` + `dryl.motion` modules).

## Global Constraints

- **Non-breaking only.** Library is at **1.0.0** (API frozen). Every change is additive → MINOR bump. Never alter the public surface of the existing `DrylCommandPalette` (`Open`/`OpenChanged`, `Placeholder`, `Items`, `SearchProvider`, `Ai`, `AiContent`, `AriaLabel`, `Class`) or of `CommandItem`/`CommandItemType`.
- **Tokens, not literals** — every color/space/radius/shadow/duration references a `dryl.css` CSS variable (rule 2.1). Dark-only (2.2), glass surfaces (2.3), accents glow only (2.4).
- **Motion vocabulary fixed** — only `--dur-fast|med|slow` + `--ease-out|in-out|spring` (2.5). Reuse `dryl.motion.*` / `DrylPresence` / `.ai-aura*` primitives; honour `prefers-reduced-motion` (2.12).
- **AI vocabulary is shared** — opt-in parameter named `Ai` of type `AiState`, default `AiState.None`; only the existing `.ai-aura*` + `.ai-indicator` primitives; invent no new AI state/animation/color (2.10).
- **Naming** — components PascalCase `Dryl`-prefixed; CSS kebab-case no prefix; namespace `DRYL.Components` (core) / `DRYL.Components.Agents` (AI). Enums for variants (2.6, 2.7).
- **Zero external runtime deps in core** (2.8) — core references nothing from `Microsoft.Agents.AI`. Only the Agents package may.
- **Numeric interpolation** in any SVG/CSS string uses `FormattableString.Invariant(...)` / `CultureInfo.InvariantCulture` (German locale would emit `0,5`).
- **JS-interop lifecycle** — guard `Dispose`/JS calls with an `_attached` flag + catch `JSDisconnectedException` (prerender-safe), mirroring `DrylCommandPalette`/`DrylPresence`.
- **Accessibility** (2.9, 2.11) — full keyboard, `role=combobox/listbox/option`, `aria-live="polite"` for AI status, icon-only affordances get a `DrylTooltip` + matching `aria-label`, moving indicator is `aria-hidden`.
- **Docs** (rule 7) — `CHANGELOG.md` `[Unreleased] / Added`; update the website `ComponentCatalog` entry; sample/demo page. Do not bump `<Version>`.

**File locations** (verified):
- Core component: `DRYL.Components/Components/Navigation/DrylCommandPalette.razor` (existing).
- New core types: `DRYL.Components/Components/Navigation/` (namespace `DRYL.Components`).
- Core JS: `DRYL.Components/wwwroot/js/dryl.js` (modules `dryl.commandpalette`, `dryl.motion` already exist).
- Core DI: `DRYL.Components/Extensions/ServiceCollectionExtensions.cs` (`AddDrylComponents`).
- Agents: `DRYL.Components.Agents/CommandPalette/` (namespace `DRYL.Components.Agents`); DI in `DRYL.Components.Agents/Extensions/ServiceCollectionExtensions.cs`.
- Tests: `tests/DRYL.Components.Tests/` (bUnit `BunitContext`, references both projects, net10.0).
- Demo: `DRYL.Website/Components/Pages/DemoCommandPalette.razor` + `DRYL.Website/Components/Examples/CommandPalette/`.

---

## File Structure

**Create (core — `DRYL.Components/Components/Navigation/`):**
- `CommandArgType.cs` — `enum CommandArgType { Text, Number, Boolean, Choice }`.
- `CommandContext.cs` — `sealed class CommandContext` (args dict + typed getter + cancellation token).
- `DrylCommandArgument.cs` — declarative child component (`ComponentBase`, no DOM) of `DrylCommand`.
- `DrylCommand.razor` (+ `.razor.cs`) — declarative command; self-registers into the registry, hosts arguments.
- `ICommandRegistry.cs` + `CommandRegistry.cs` — shared command list service.
- `ICommandResolver.cs` + `CommandResolution.cs` — the AI seam.
- `CommandFuzzyMatcher.cs` — `internal static` matcher/ranker.

**Modify (core):**
- `DrylCommandPalette.razor` — cascade registry, host `DrylCommand` children, build a unified result list, render registry commands + argument-fill sub-view + resolver suggestion, glide active row, AI aura while resolving.
- `DrylCommandPalette.razor.css` — *create if absent* (or extend) — scoped styles for the active-row ink, stagger, arg-fill view, AI pill. **Tokens only.**
- `Extensions/ServiceCollectionExtensions.cs` — register `CommandRegistry` as scoped in `AddDrylComponents()`.

**Create (Agents — `DRYL.Components.Agents/CommandPalette/`):**
- `DrylAiCommandResolver.cs` — `ICommandResolver` backed by an `AIAgent`; one structured tool-call resolution, no execution.
- `DrylAiCommandPalette.razor` — thin wrapper injecting `ICommandResolver`, forwarding to `DrylCommandPalette`.

**Modify (Agents):**
- `Extensions/ServiceCollectionExtensions.cs` — add `AddDrylCommandResolver(Func<IServiceProvider, AIAgent>)`.

**Create (tests):** `tests/DRYL.Components.Tests/CommandPalette*Tests.cs`.

**Modify (docs/demo):** `CHANGELOG.md`, `DRYL.Website/Components/ComponentCatalog.cs`, `DRYL.Website/Components/Pages/DemoCommandPalette.razor`, new examples under `DRYL.Website/Components/Examples/CommandPalette/`.

---

## Task 1: Core value types — `CommandArgType`, `CommandContext`

**Files:**
- Create: `DRYL.Components/Components/Navigation/CommandArgType.cs`
- Create: `DRYL.Components/Components/Navigation/CommandContext.cs`
- Test: `tests/DRYL.Components.Tests/CommandContextTests.cs`

**Interfaces:**
- Produces: `enum CommandArgType { Text, Number, Boolean, Choice }`; `sealed class CommandContext` with ctor `(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken = default)`, props `IReadOnlyDictionary<string, object?> Arguments`, `CancellationToken CancellationToken`, method `T? GetArgument<T>(string name)`.

- [ ] **Step 1: Write the failing test**

```csharp
// tests/DRYL.Components.Tests/CommandContextTests.cs
using DRYL.Components;

namespace DRYL.Components.Tests;

public class CommandContextTests
{
    [Fact]
    public void GetArgument_returns_typed_value()
    {
        var ctx = new CommandContext(new Dictionary<string, object?> { ["status"] = "Paid" });
        Assert.Equal("Paid", ctx.GetArgument<string>("status"));
    }

    [Fact]
    public void GetArgument_converts_string_to_number_invariantly()
    {
        var ctx = new CommandContext(new Dictionary<string, object?> { ["amount"] = "0.5" });
        Assert.Equal(0.5d, ctx.GetArgument<double>("amount"));
    }

    [Fact]
    public void GetArgument_missing_returns_default()
    {
        var ctx = new CommandContext(new Dictionary<string, object?>());
        Assert.Null(ctx.GetArgument<string>("nope"));
        Assert.Equal(0, ctx.GetArgument<int>("nope"));
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/DRYL.Components.Tests --filter CommandContextTests`
Expected: FAIL — `CommandContext`/`CommandArgType` not defined (compile error).

- [ ] **Step 3: Write minimal implementation**

```csharp
// CommandArgType.cs
namespace DRYL.Components;

/// <summary>The input type of a <see cref="DrylCommandArgument"/> — drives both the
/// manual input rendered in the palette and the argument's JSON-schema type.</summary>
public enum CommandArgType
{
    /// <summary>Free text (default).</summary>
    Text,
    /// <summary>Numeric input.</summary>
    Number,
    /// <summary>Boolean toggle.</summary>
    Boolean,
    /// <summary>One value chosen from <see cref="DrylCommandArgument.Options"/>.</summary>
    Choice
}
```

```csharp
// CommandContext.cs
using System.Globalization;

namespace DRYL.Components;

/// <summary>The single payload passed to <see cref="DrylCommand.OnRun"/> — identical whether the
/// command was run by click, keyboard, or an AI resolution. Exposes the resolved arguments and a
/// cancellation token.</summary>
public sealed class CommandContext
{
    /// <summary>Creates a context from a resolved argument set.</summary>
    public CommandContext(
        IReadOnlyDictionary<string, object?> arguments,
        CancellationToken cancellationToken = default)
    {
        Arguments = arguments;
        CancellationToken = cancellationToken;
    }

    /// <summary>The resolved arguments by name (empty when the command takes none).</summary>
    public IReadOnlyDictionary<string, object?> Arguments { get; }

    /// <summary>Cancelled if the palette closes or the circuit tears down mid-run.</summary>
    public CancellationToken CancellationToken { get; }

    /// <summary>Reads an argument as <typeparamref name="T"/>; converts culture-invariantly and
    /// returns <c>default</c> when missing or unconvertible.</summary>
    public T? GetArgument<T>(string name)
    {
        if (!Arguments.TryGetValue(name, out var value) || value is null)
            return default;
        if (value is T typed)
            return typed;
        try
        {
            var target = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
            return (T)Convert.ChangeType(value, target, CultureInfo.InvariantCulture);
        }
        catch
        {
            return default;
        }
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/DRYL.Components.Tests --filter CommandContextTests`
Expected: PASS (3 tests).

- [ ] **Step 5: Commit**

```bash
git add DRYL.Components/Components/Navigation/CommandArgType.cs DRYL.Components/Components/Navigation/CommandContext.cs tests/DRYL.Components.Tests/CommandContextTests.cs
git commit -m "feat(DrylCommandPalette): add CommandArgType + CommandContext"
```

---

## Task 2: `DrylCommand` + `DrylCommandArgument` + registry

**Files:**
- Create: `DRYL.Components/Components/Navigation/ICommandRegistry.cs`
- Create: `DRYL.Components/Components/Navigation/CommandRegistry.cs`
- Create: `DRYL.Components/Components/Navigation/DrylCommandArgument.cs`
- Create: `DRYL.Components/Components/Navigation/DrylCommand.razor`
- Create: `DRYL.Components/Components/Navigation/DrylCommand.razor.cs`
- Modify: `DRYL.Components/Extensions/ServiceCollectionExtensions.cs`
- Test: `tests/DRYL.Components.Tests/DrylCommandRegistryTests.cs`

**Interfaces:**
- Consumes: `CommandContext` (Task 1).
- Produces:
  - `interface ICommandRegistry { IReadOnlyList<DrylCommand> Commands { get; } void Add(DrylCommand command); void Remove(string id); event Action? OnChanged; }`
  - `sealed class CommandRegistry : ICommandRegistry`.
  - `sealed class DrylCommandArgument : ComponentBase` — params `string Name`, `string? Description`, `CommandArgType Type`, `bool Required`, `string[]? Options`.
  - `partial class DrylCommand : ComponentBase, IDisposable` — params `string Title`, `string? Description`, `string? Icon`, `string? Group`, `string[]? Keywords`, `string? Shortcut`, `bool Destructive`, `bool Disabled`, `EventCallback<CommandContext> OnRun`, `string? Id`, `RenderFragment? ChildContent`; props `string Id` (resolved), `IReadOnlyList<DrylCommandArgument> Arguments`; methods `void AddArgument(DrylCommandArgument arg)`, `Task RunAsync(CommandContext ctx)`.

- [ ] **Step 1: Write the failing test**

```csharp
// tests/DRYL.Components.Tests/DrylCommandRegistryTests.cs
using Bunit;
using DRYL.Components;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace DRYL.Components.Tests;

public class DrylCommandRegistryTests : BunitContext
{
    public DrylCommandRegistryTests() => JSInterop.Mode = JSRuntimeMode.Loose;

    [Fact]
    public void Add_dedupes_by_id_and_raises_changed()
    {
        var reg = new CommandRegistry();
        var changes = 0;
        reg.OnChanged += () => changes++;

        var a = new DrylCommand();   // Id auto-generated lazily; set explicitly for the test
        typeof(DrylCommand).GetProperty(nameof(DrylCommand.Id))!.SetValue(a, "x");
        reg.Add(a);
        reg.Add(a); // same id → ignored

        Assert.Single(reg.Commands);
        Assert.Equal(1, changes);

        reg.Remove("x");
        Assert.Empty(reg.Commands);
        Assert.Equal(2, changes);
    }

    [Fact]
    public void DrylCommand_self_registers_into_cascaded_registry()
    {
        var reg = new CommandRegistry();
        Render<DrylCommand>(ps => ps
            .AddCascadingValue<ICommandRegistry>(reg)
            .Add(p => p.Title, "Neue Rechnung"));

        Assert.Single(reg.Commands);
        Assert.Equal("Neue Rechnung", reg.Commands[0].Title);
    }

    [Fact]
    public void DrylCommandArgument_registers_into_parent_command()
    {
        var reg = new CommandRegistry();
        var cut = Render<DrylCommand>(ps => ps
            .AddCascadingValue<ICommandRegistry>(reg)
            .Add(p => p.Title, "Status setzen")
            .AddChildContent<DrylCommandArgument>(a => a
                .Add(p => p.Name, "status")
                .Add(p => p.Type, CommandArgType.Choice)
                .Add(p => p.Options, new[] { "Draft", "Paid" })));

        var cmd = reg.Commands.Single();
        Assert.Single(cmd.Arguments);
        Assert.Equal("status", cmd.Arguments[0].Name);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/DRYL.Components.Tests --filter DrylCommandRegistryTests`
Expected: FAIL — types not defined.

- [ ] **Step 3: Write minimal implementation**

```csharp
// ICommandRegistry.cs
namespace DRYL.Components;

/// <summary>The shared, scoped registry of <see cref="DrylCommand"/>s a palette renders. Both the
/// declarative <see cref="DrylCommand"/> children and consumer code (for dynamic, context-dependent
/// commands) feed the same instance; the palette renders the union, de-duplicated by
/// <see cref="DrylCommand.Id"/>.</summary>
public interface ICommandRegistry
{
    /// <summary>The currently registered commands, in registration order.</summary>
    IReadOnlyList<DrylCommand> Commands { get; }

    /// <summary>Adds a command. A command whose <see cref="DrylCommand.Id"/> is already present is ignored.</summary>
    void Add(DrylCommand command);

    /// <summary>Removes the command with the given id, if present.</summary>
    void Remove(string id);

    /// <summary>Raised whenever the command set changes.</summary>
    event Action? OnChanged;
}
```

```csharp
// CommandRegistry.cs
namespace DRYL.Components;

/// <summary>Default in-memory <see cref="ICommandRegistry"/>. Registered scoped by
/// <c>AddDrylComponents()</c> (one per Blazor circuit).</summary>
public sealed class CommandRegistry : ICommandRegistry
{
    private readonly List<DrylCommand> _commands = new();

    /// <inheritdoc />
    public IReadOnlyList<DrylCommand> Commands => _commands;

    /// <inheritdoc />
    public void Add(DrylCommand command)
    {
        if (_commands.Any(c => c.Id == command.Id)) return;
        _commands.Add(command);
        OnChanged?.Invoke();
    }

    /// <inheritdoc />
    public void Remove(string id)
    {
        if (_commands.RemoveAll(c => c.Id == id) > 0)
            OnChanged?.Invoke();
    }

    /// <inheritdoc />
    public event Action? OnChanged;
}
```

```csharp
// DrylCommandArgument.cs
using Microsoft.AspNetCore.Components;

namespace DRYL.Components;

/// <summary>A declarative argument of a <see cref="DrylCommand"/>. Renders no DOM of its own: it
/// contributes (a) a manual input shown in the palette when the command is run with arguments, and
/// (b) one property in the AI tool's JSON schema.</summary>
public sealed class DrylCommandArgument : ComponentBase
{
    [CascadingParameter] internal DrylCommand? Parent { get; set; }

    /// <summary>The argument name (the schema property key and <see cref="CommandContext"/> key). Required.</summary>
    [Parameter, EditorRequired] public string Name { get; set; } = string.Empty;

    /// <summary>Human/model-facing description of the argument.</summary>
    [Parameter] public string? Description { get; set; }

    /// <summary>The input/schema type. Defaults to <see cref="CommandArgType.Text"/>.</summary>
    [Parameter] public CommandArgType Type { get; set; } = CommandArgType.Text;

    /// <summary>Whether the argument must be provided.</summary>
    [Parameter] public bool Required { get; set; }

    /// <summary>Allowed values when <see cref="Type"/> is <see cref="CommandArgType.Choice"/>.</summary>
    [Parameter] public string[]? Options { get; set; }

    protected override void OnInitialized() => Parent?.AddArgument(this);
}
```

```razor
@* DrylCommand.razor *@
@namespace DRYL.Components

<CascadingValue Value="this" IsFixed="true">
    @ChildContent
</CascadingValue>
```

```csharp
// DrylCommand.razor.cs
using Microsoft.AspNetCore.Components;

namespace DRYL.Components;

/// <summary>A single unit of application functionality declared once and usable three ways — click,
/// keyboard <c>Enter</c> on a fuzzy match, and (when a resolver is supplied) an AI tool call — all
/// flowing through the one <see cref="OnRun"/> handler. Place inside a
/// <see cref="DrylCommandPalette"/>; it self-registers and hosts optional
/// <see cref="DrylCommandArgument"/> children.</summary>
public partial class DrylCommand : ComponentBase, IDisposable
{
    private readonly List<DrylCommandArgument> _arguments = new();
    private bool _registered;

    [CascadingParameter] internal ICommandRegistry? Registry { get; set; }

    /// <summary>Primary label and the tool's display/selection hint. Required.</summary>
    [Parameter, EditorRequired] public string Title { get; set; } = string.Empty;

    /// <summary>Secondary line in the palette and the tool description sent to the model.</summary>
    [Parameter] public string? Description { get; set; }

    /// <summary>Lucide icon name passed to <c>DrylIcon</c>.</summary>
    [Parameter] public string? Icon { get; set; }

    /// <summary>Section grouping in the palette.</summary>
    [Parameter] public string? Group { get; set; }

    /// <summary>Extra fuzzy-search aliases.</summary>
    [Parameter] public string[]? Keywords { get; set; }

    /// <summary>Display-only shortcut hint, e.g. "⌘N".</summary>
    [Parameter] public string? Shortcut { get; set; }

    /// <summary>De-prioritised in sort, styled as destructive, and forced through human-in-the-loop
    /// confirmation before running.</summary>
    [Parameter] public bool Destructive { get; set; }

    /// <summary>Greyed and unselectable in the palette; excluded from the AI tool list.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>The one execution path. Receives a <see cref="CommandContext"/> carrying the resolved
    /// arguments (manual or AI-filled).</summary>
    [Parameter] public EventCallback<CommandContext> OnRun { get; set; }

    /// <summary>Stable id (schema/tool name and de-dup key). Auto-generated from the title if absent.</summary>
    [Parameter] public string? Id { get; set; }

    /// <summary>Hosts the command's <see cref="DrylCommandArgument"/> children.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>The resolved id used by the registry and AI tool name.</summary>
    public string ResolvedId { get; private set; } = string.Empty;

    /// <summary>The command's declared arguments.</summary>
    public IReadOnlyList<DrylCommandArgument> Arguments => _arguments;

    /// <summary>Registers an argument (called by <see cref="DrylCommandArgument"/>).</summary>
    public void AddArgument(DrylCommandArgument argument)
    {
        if (!_arguments.Contains(argument)) _arguments.Add(argument);
    }

    /// <summary>Runs the command with the supplied context.</summary>
    public Task RunAsync(CommandContext context) => OnRun.InvokeAsync(context);

    protected override void OnInitialized()
    {
        ResolvedId = string.IsNullOrWhiteSpace(Id) ? Slug(Title) : Id!;
        if (Registry is not null && !_registered)
        {
            Registry.Add(this);
            _registered = true;
        }
    }

    private static string Slug(string title)
    {
        var chars = title.Trim().ToLowerInvariant()
            .Select(c => char.IsLetterOrDigit(c) ? c : '-').ToArray();
        var slug = new string(chars);
        while (slug.Contains("--")) slug = slug.Replace("--", "-");
        slug = slug.Trim('-');
        return string.IsNullOrEmpty(slug) ? $"cmd-{Guid.NewGuid():N}" : slug;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_registered) Registry?.Remove(ResolvedId);
    }
}
```

> **Note:** the test's reflection sets `Id`; the public read of the resolved id is `ResolvedId`. Update the test in Step 1 to assert via `ResolvedId` if you set `Id` directly — i.e. set `a.Id` won't apply outside the render pipeline, so for the registry-only test construct the command and set the backing via the public `Id` parameter is not possible off-render. Instead, the first test should add two commands with distinct vs same `ResolvedId`. Adjust the unit test to register through a render (as the other two do) OR give `CommandRegistry` an internal `Add` keyed on a string overload for pure unit testing. **Simplest:** keep registry dedupe tested only through render-based tests (tests 2 and 3); drop test 1's reflection and instead assert dedupe by rendering the same `Id` twice.

Replace test 1 with:

```csharp
    [Fact]
    public void Same_id_registers_once()
    {
        var reg = new CommandRegistry();
        Render<DrylCommand>(ps => ps.AddCascadingValue<ICommandRegistry>(reg)
            .Add(p => p.Title, "A").Add(p => p.Id, "dup"));
        Render<DrylCommand>(ps => ps.AddCascadingValue<ICommandRegistry>(reg)
            .Add(p => p.Title, "B").Add(p => p.Id, "dup"));
        Assert.Single(reg.Commands);
    }
```

- [ ] **Step 4: Register the registry in DI**

In `DRYL.Components/Extensions/ServiceCollectionExtensions.cs`, inside `AddDrylComponents`, add before `return services;`:

```csharp
        services.AddScoped<ICommandRegistry, CommandRegistry>();
```

- [ ] **Step 5: Run tests to verify they pass**

Run: `dotnet test tests/DRYL.Components.Tests --filter DrylCommandRegistryTests`
Expected: PASS (3 tests).

- [ ] **Step 6: Commit**

```bash
git add DRYL.Components/Components/Navigation/ICommandRegistry.cs DRYL.Components/Components/Navigation/CommandRegistry.cs DRYL.Components/Components/Navigation/DrylCommandArgument.cs DRYL.Components/Components/Navigation/DrylCommand.razor DRYL.Components/Components/Navigation/DrylCommand.razor.cs DRYL.Components/Extensions/ServiceCollectionExtensions.cs tests/DRYL.Components.Tests/DrylCommandRegistryTests.cs
git commit -m "feat(DrylCommandPalette): DrylCommand/DrylCommandArgument + ICommandRegistry"
```

---

## Task 3: `CommandFuzzyMatcher`

**Files:**
- Create: `DRYL.Components/Components/Navigation/CommandFuzzyMatcher.cs`
- Test: `tests/DRYL.Components.Tests/CommandFuzzyMatcherTests.cs`
- Modify: `DRYL.Components/DRYL.Components.csproj` — add `<InternalsVisibleTo Include="DRYL.Components.Tests" />` if not already present (check first).

**Interfaces:**
- Consumes: `DrylCommand` (Task 2).
- Produces: `internal static class CommandFuzzyMatcher` with `static IReadOnlyList<DrylCommand> Match(IReadOnlyList<DrylCommand> commands, string query, int max)`.

- [ ] **Step 1: Verify InternalsVisibleTo**

Run: `grep -n "InternalsVisibleTo" DRYL.Components/DRYL.Components.csproj`
If absent, add inside an `<ItemGroup>`:
```xml
    <InternalsVisibleTo Include="DRYL.Components.Tests" />
```

- [ ] **Step 2: Write the failing test**

```csharp
// tests/DRYL.Components.Tests/CommandFuzzyMatcherTests.cs
using DRYL.Components;

namespace DRYL.Components.Tests;

public class CommandFuzzyMatcherTests
{
    private static DrylCommand Cmd(string title, bool destructive = false,
        bool disabled = false, string[]? keywords = null)
    {
        var c = new DrylCommand { Title = title, Destructive = destructive,
            Disabled = disabled, Keywords = keywords };
        typeof(DrylCommand).GetMethod("OnInitialized",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .Invoke(c, null); // resolve Id without a render
        return c;
    }

    [Fact]
    public void Empty_query_returns_all_up_to_max()
    {
        var list = new[] { Cmd("Alpha"), Cmd("Beta"), Cmd("Gamma") };
        var r = CommandFuzzyMatcher.Match(list, "", 2);
        Assert.Equal(2, r.Count);
    }

    [Fact]
    public void Subsequence_match_on_title()
    {
        var list = new[] { Cmd("Neue Rechnung"), Cmd("Status setzen") };
        var r = CommandFuzzyMatcher.Match(list, "rech", 8);
        Assert.Single(r);
        Assert.Equal("Neue Rechnung", r[0].Title);
    }

    [Fact]
    public void Matches_keywords()
    {
        var list = new[] { Cmd("Status setzen", keywords: new[] { "bezahlt", "paid" }) };
        var r = CommandFuzzyMatcher.Match(list, "paid", 8);
        Assert.Single(r);
    }

    [Fact]
    public void Destructive_sorts_after_equal_non_destructive()
    {
        var safe = Cmd("Delete safe");
        var danger = Cmd("Delete danger", destructive: true);
        var r = CommandFuzzyMatcher.Match(new[] { danger, safe }, "delete", 8);
        Assert.Equal("Delete safe", r[0].Title);
    }
}
```

- [ ] **Step 3: Run test to verify it fails**

Run: `dotnet test tests/DRYL.Components.Tests --filter CommandFuzzyMatcherTests`
Expected: FAIL — `CommandFuzzyMatcher` not defined.

- [ ] **Step 4: Write minimal implementation**

```csharp
// CommandFuzzyMatcher.cs
namespace DRYL.Components;

/// <summary>Ranks <see cref="DrylCommand"/>s against a query: case-insensitive substring/subsequence
/// scoring across title, keywords and description, with destructive commands de-prioritised on ties.
/// An empty query returns all commands (capped). Disabled commands are kept (the palette greys them).</summary>
internal static class CommandFuzzyMatcher
{
    public static IReadOnlyList<DrylCommand> Match(
        IReadOnlyList<DrylCommand> commands, string query, int max)
    {
        if (string.IsNullOrWhiteSpace(query))
            return commands.Take(max).ToList();

        var q = query.Trim();
        var scored = new List<(DrylCommand cmd, int score)>();
        foreach (var c in commands)
        {
            var best = Score(c.Title, q, 100);
            best = Math.Max(best, Score(c.Description, q, 40));
            if (c.Keywords is not null)
                foreach (var k in c.Keywords)
                    best = Math.Max(best, Score(k, q, 60));
            if (best > 0)
            {
                if (c.Destructive) best -= 1; // tie-break only
                scored.Add((c, best));
            }
        }

        return scored
            .OrderByDescending(s => s.score)
            .ThenBy(s => s.cmd.Title, StringComparer.OrdinalIgnoreCase)
            .Take(max)
            .Select(s => s.cmd)
            .ToList();
    }

    // Substring hit scores highest (weight), then subsequence; 0 = no match.
    private static int Score(string? haystack, string needle, int weight)
    {
        if (string.IsNullOrEmpty(haystack)) return 0;
        var h = haystack.ToLowerInvariant();
        var n = needle.ToLowerInvariant();
        var idx = h.IndexOf(n, StringComparison.Ordinal);
        if (idx == 0) return weight + 5;       // prefix
        if (idx > 0) return weight;            // substring
        return IsSubsequence(h, n) ? weight / 2 : 0;
    }

    private static bool IsSubsequence(string h, string n)
    {
        var i = 0;
        foreach (var ch in h)
            if (i < n.Length && ch == n[i]) i++;
        return i == n.Length;
    }
}
```

- [ ] **Step 5: Run tests to verify they pass**

Run: `dotnet test tests/DRYL.Components.Tests --filter CommandFuzzyMatcherTests`
Expected: PASS (4 tests).

- [ ] **Step 6: Commit**

```bash
git add DRYL.Components/Components/Navigation/CommandFuzzyMatcher.cs DRYL.Components/DRYL.Components.csproj tests/DRYL.Components.Tests/CommandFuzzyMatcherTests.cs
git commit -m "feat(DrylCommandPalette): add CommandFuzzyMatcher"
```

---

## Task 4: AI seam — `ICommandResolver` + `CommandResolution`

**Files:**
- Create: `DRYL.Components/Components/Navigation/ICommandResolver.cs`
- Create: `DRYL.Components/Components/Navigation/CommandResolution.cs`
- Test: `tests/DRYL.Components.Tests/CommandResolutionTests.cs`

**Interfaces:**
- Produces:
  - `interface ICommandResolver { Task<CommandResolution?> ResolveAsync(string query, IReadOnlyList<DrylCommand> commands, CancellationToken ct); }`
  - `sealed record CommandResolution(DrylCommand Command, IReadOnlyDictionary<string, object?> Arguments, double Confidence)`.

- [ ] **Step 1: Write the failing test**

```csharp
// tests/DRYL.Components.Tests/CommandResolutionTests.cs
using DRYL.Components;

namespace DRYL.Components.Tests;

public class CommandResolutionTests
{
    private sealed class StubResolver : ICommandResolver
    {
        public Task<CommandResolution?> ResolveAsync(
            string query, IReadOnlyList<DrylCommand> commands, CancellationToken ct)
        {
            var cmd = commands[0];
            var args = new Dictionary<string, object?> { ["status"] = "Paid" };
            return Task.FromResult<CommandResolution?>(new CommandResolution(cmd, args, 0.9));
        }
    }

    [Fact]
    public async Task Resolver_returns_command_args_and_confidence()
    {
        ICommandResolver resolver = new StubResolver();
        var commands = new[] { new DrylCommand { Title = "Status setzen" } };
        var res = await resolver.ResolveAsync("markiere als bezahlt", commands, default);

        Assert.NotNull(res);
        Assert.Equal("Status setzen", res!.Command.Title);
        Assert.Equal("Paid", res.Arguments["status"]);
        Assert.Equal(0.9, res.Confidence);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/DRYL.Components.Tests --filter CommandResolutionTests`
Expected: FAIL — types not defined.

- [ ] **Step 3: Write minimal implementation**

```csharp
// ICommandResolver.cs
namespace DRYL.Components;

/// <summary>The narrow AI seam. An implementation turns a natural-language query plus the registered
/// commands into at most one resolved command + filled arguments. The core ships none and never
/// references any AI package; supply one (e.g. <c>DRYL.Components.Agents.DrylAiCommandResolver</c>)
/// via <see cref="DrylCommandPalette.Resolver"/>.</summary>
public interface ICommandResolver
{
    /// <summary>Resolves a query against the available commands. Returns <c>null</c> when nothing fits.
    /// The implementation must not execute the command — execution is the palette's, after confirmation.</summary>
    Task<CommandResolution?> ResolveAsync(
        string query, IReadOnlyList<DrylCommand> commands, CancellationToken ct);
}
```

```csharp
// CommandResolution.cs
namespace DRYL.Components;

/// <summary>The outcome of an <see cref="ICommandResolver"/>: the chosen command, the model-filled
/// arguments, and a 0–1 confidence. Surfaced by the palette as a confirmable top suggestion.</summary>
public sealed record CommandResolution(
    DrylCommand Command,
    IReadOnlyDictionary<string, object?> Arguments,
    double Confidence);
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/DRYL.Components.Tests --filter CommandResolutionTests`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add DRYL.Components/Components/Navigation/ICommandResolver.cs DRYL.Components/Components/Navigation/CommandResolution.cs tests/DRYL.Components.Tests/CommandResolutionTests.cs
git commit -m "feat(DrylCommandPalette): add ICommandResolver + CommandResolution seam"
```

---

## Task 5: Palette integration — host commands, unified results, run path

**Files:**
- Modify: `DRYL.Components/Components/Navigation/DrylCommandPalette.razor`
- Create/Modify: `DRYL.Components/Components/Navigation/DrylCommandPalette.razor.css`
- Test: `tests/DRYL.Components.Tests/DrylCommandPaletteTests.cs`

**Interfaces:**
- Consumes: `ICommandRegistry`, `CommandFuzzyMatcher`, `DrylCommand`, `CommandContext`, `ICommandResolver`, `CommandResolution`.
- Produces (new public surface on `DrylCommandPalette`, all additive):
  - `[Parameter] bool HotKey { get; set; } = true;` (gate the existing global Ctrl/⌘+K attach)
  - `[Parameter] string EmptyText { get; set; } = "Keine Treffer";`
  - `[Parameter] int MaxResults { get; set; } = 8;`
  - `[Parameter] ICommandResolver? Resolver { get; set; }`
  - `[Parameter] RenderFragment? ChildContent { get; set; }`
  - Internal unified row model `private sealed record PaletteRow(string Id, string Label, string? Description, string? Icon, string? Group, bool Disabled, bool KeepOpen, bool Destructive, bool IsSuggestion, DrylCommand? Command, Func<CommandContext, Task>? Run, EventCallback Legacy);`

**Design (additive, non-breaking):**
1. Inject `IServiceProvider Services`. In `OnInitialized`, resolve `ICommandRegistry` (`Services.GetService<ICommandRegistry>() ?? new CommandRegistry()`) and an optional `IDrylDialogService` for destructive confirmation; subscribe to `registry.OnChanged → InvokeAsync(StateHasChanged)`.
2. Wrap `ChildContent` in a `<CascadingValue Value="_registry" IsFixed="true">` rendered **inside** the palette (so child `DrylCommand`s register) — render it hidden (it produces no visible DOM, only registration).
3. Keep `Items`/`SearchProvider` → `CommandItem[]` exactly as today, then **project** each `CommandItem` and each fuzzy-matched registry `DrylCommand` into a unified `List<PaletteRow>`, grouped by `Group`/`Category`. Registry commands are matched with `CommandFuzzyMatcher.Match(_registry.Commands, query, MaxResults)`.
4. The optional resolver suggestion is prepended as a single `IsSuggestion` row (Task 6).
5. Keyboard nav + `aria-activedescendant` operate over the flat unified list (replace `FlatItemAt`/`FlatItemCount` to walk `_rows`).
6. **Execution:** running a `PaletteRow`:
   - Legacy `CommandItem` rows → existing behaviour (`Navigate`/`Action` close; `AiIntent` keeps open) via the captured `Legacy`/`KeepOpen`.
   - Command rows with no arguments → build empty `CommandContext`, gate on `Destructive` (Task 7), run, close.
   - Command rows **with** arguments → open the argument-fill sub-view (Task 7).
7. **HotKey gate:** only call `dryl.commandpalette.attachGlobal` when `HotKey` is true (default true → unchanged behaviour).
8. **Active-row glide:** add a `<span data-dryl-ink aria-hidden="true" class="cmd-ink"></span>` inside `.cmd-results`; mark the focused row `data-dryl-ink-active="true"`; call `dryl.motion.moveIndicator(_list)` after focus changes in `OnAfterRenderAsync`.

- [ ] **Step 1: Write the failing test**

```csharp
// tests/DRYL.Components.Tests/DrylCommandPaletteTests.cs
using Bunit;
using DRYL.Components;

namespace DRYL.Components.Tests;

public class DrylCommandPaletteTests : BunitContext
{
    public DrylCommandPaletteTests() => JSInterop.Mode = JSRuntimeMode.Loose;

    [Fact]
    public void Declarative_commands_render_as_options_when_open()
    {
        var cut = Render<DrylCommandPalette>(ps => ps
            .Add(p => p.Open, true)
            .AddChildContent<DrylCommand>(c => c.Add(x => x.Title, "Neue Rechnung")));

        var options = cut.FindAll("[role=option]");
        Assert.Contains(options, o => o.TextContent.Contains("Neue Rechnung"));
    }

    [Fact]
    public void Legacy_Items_still_render()
    {
        var items = new[] { new CommandItem { Label = "Legacy item" } };
        var cut = Render<DrylCommandPalette>(ps => ps
            .Add(p => p.Open, true)
            .Add(p => p.Items, items));

        Assert.Contains(cut.FindAll("[role=option]"), o => o.TextContent.Contains("Legacy item"));
    }

    [Fact]
    public void Running_argument_less_command_invokes_OnRun_with_empty_context()
    {
        CommandContext? captured = null;
        var cut = Render<DrylCommandPalette>(ps => ps
            .Add(p => p.Open, true)
            .AddChildContent<DrylCommand>(c => c
                .Add(x => x.Title, "Run me")
                .Add(x => x.OnRun, (CommandContext ctx) => { captured = ctx; })));

        cut.FindAll("[role=option]").First(o => o.TextContent.Contains("Run me")).Click();

        Assert.NotNull(captured);
        Assert.Empty(captured!.Arguments);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/DRYL.Components.Tests --filter DrylCommandPaletteTests`
Expected: FAIL — `ChildContent`/registry rendering not present.

- [ ] **Step 3: Implement the integration**

Edit `DrylCommandPalette.razor`:
- Add `@inject IServiceProvider Services` (keep existing `@inject IJSRuntime JS` + `@inject NavigationManager Nav`).
- Add the new `[Parameter]`s (`HotKey`, `EmptyText`, `MaxResults`, `Resolver`, `ChildContent`).
- Replace the `_grouped` (`Dictionary<string,List<CommandItem>>`) result model with a `List<PaletteRow> _rows` + grouped projection for rendering; keep the `CommandItem` search pipeline intact, projecting its results into `PaletteRow`s, then appending registry fuzzy matches.
- Render `ChildContent` once inside `<CascadingValue Value="_registry" IsFixed="true">…</CascadingValue>` placed inside the palette markup but visually inert.
- Render rows from `_rows` (grouped by `Group`), reusing the existing `.cmd-item` markup; for registry rows show `Shortcut` as a trailing `<kbd>` and an arrow/sparkle only where relevant.
- Add the `cmd-ink` span and `data-dryl-ink-active` on the focused row.
- Rework `HandleKeyDown`/`ExecuteItemAsync` to operate on `PaletteRow`; `Enter`/click → `RunRowAsync(row)`.
- `RunRowAsync`: legacy rows keep current semantics; command rows with `row.Command!.Arguments.Count == 0` → run immediately (Task 7 adds the arg-fill + destructive gate; for this task, run with empty context and close).
- Gate `attachGlobal` behind `HotKey`.
- Use the existing `EmptyText` for the empty state (replace the literal `"No results"`).

Scoped CSS — create `DrylCommandPalette.razor.css` (only if not present) with, at minimum, the moving-ink marker and stagger; **tokens only**:

```css
/* DrylCommandPalette.razor.css — additive scoped styles (tokens only) */
.cmd-results { position: relative; }

.cmd-ink {
    position: absolute;
    left: 0;
    height: var(--sp-7);
    border-radius: var(--r-sm);
    background: var(--accent-soft);
    box-shadow: inset 0 0 0 1px var(--accent-line);
    opacity: 0;
    pointer-events: none;
    transform: translateY(0);
    transition: transform var(--dur-med) var(--ease-spring),
                width var(--dur-med) var(--ease-spring),
                opacity var(--dur-fast) var(--ease-out);
}

@media (prefers-reduced-motion: reduce) {
    .cmd-ink { transition: none; }
}
```

> The existing `dryl.motion.moveIndicator` positions a `[data-dryl-ink]` element by X within the container; for a vertical list, also set its `top`. If a vertical glide proves awkward via `moveIndicator` (which sets `transform: translateX`), fall back to the established `.is-focused` per-row border/glow already used by the component for the active state — the `.is-focused` style already satisfies an animated active state (rule 2.12). Prefer keeping `.is-focused` and add the ink only if it lands cleanly. Do not invent new easings/durations either way.

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test tests/DRYL.Components.Tests --filter DrylCommandPaletteTests`
Expected: PASS (3 tests).

- [ ] **Step 5: Build the whole core (no AI dep) to prove dependency-free**

Run: `dotnet build DRYL.Components/DRYL.Components.csproj -c Release`
Expected: Build succeeded, 0 errors.

- [ ] **Step 6: Commit**

```bash
git add DRYL.Components/Components/Navigation/DrylCommandPalette.razor DRYL.Components/Components/Navigation/DrylCommandPalette.razor.css tests/DRYL.Components.Tests/DrylCommandPaletteTests.cs
git commit -m "feat(DrylCommandPalette): host DrylCommand registry, unified results, run path"
```

---

## Task 6: Resolver suggestion + AI aura while resolving

**Files:**
- Modify: `DRYL.Components/Components/Navigation/DrylCommandPalette.razor`
- Modify: `DRYL.Components/Components/Navigation/DrylCommandPalette.razor.css`
- Test: extend `tests/DRYL.Components.Tests/DrylCommandPaletteTests.cs`

**Interfaces:**
- Consumes: `Resolver` (Task 5), `CommandFuzzyMatcher`, `CommandResolution`.
- Produces: an `IsSuggestion` top row + `_resolving` state driving the input `.ai-aura` + `.ai-indicator` pill.

**Design:** In the search routine, when `Resolver != null` and the query is non-empty, after the existing debounce kick off `Resolver.ResolveAsync(query, _registry.Commands.Where(c => !c.Disabled).ToList(), cts.Token)`. While awaiting: `_resolving = true`, set the search row to wear `.ai-aura` + show a `.ai-indicator` "Denkt…" pill (`AiState.Thinking`) with `aria-live="polite"`. On result: prepend an `IsSuggestion` `PaletteRow` (label = resolved command title, with the `.ai-aura` accent + sparkle trailing) whose `Run` executes the resolved command with `res.Arguments`. Clear `_resolving` in `finally`. Cancelled queries (fast typing) are swallowed (`OperationCanceledException`). The suggestion row is the default-focused row (index 0).

- [ ] **Step 1: Write the failing test**

```csharp
    [Fact]
    public void Resolver_result_shows_a_suggestion_row()
    {
        var cut = Render<DrylCommandPalette>(ps => ps
            .Add(p => p.Open, true)
            .Add(p => p.Resolver, new EchoResolver())
            .AddChildContent<DrylCommand>(c => c.Add(x => x.Title, "Status setzen")));

        cut.Find(".cmd-search-input").Input("bezahlt");
        cut.WaitForAssertion(() =>
            Assert.Contains(cut.FindAll("[role=option]"),
                o => o.GetAttribute("class")!.Contains("ai-aura") || o.TextContent.Contains("Status setzen")),
            TimeSpan.FromSeconds(2));
    }

    private sealed class EchoResolver : ICommandResolver
    {
        public Task<CommandResolution?> ResolveAsync(
            string query, IReadOnlyList<DrylCommand> commands, CancellationToken ct)
            => Task.FromResult<CommandResolution?>(
                commands.Count == 0 ? null :
                new CommandResolution(commands[0],
                    new Dictionary<string, object?> { ["status"] = "Paid" }, 0.9));
    }
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/DRYL.Components.Tests --filter DrylCommandPaletteTests`
Expected: FAIL — no suggestion row.

- [ ] **Step 3: Implement** the resolver call, `_resolving` state, the `.ai-indicator` pill on the search row, and the prepended `IsSuggestion` row (with `.ai-aura` class). Reuse `DrylAiIndicator State="AiState.Thinking"` for the pill. No new tokens/animations.

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test tests/DRYL.Components.Tests --filter DrylCommandPaletteTests`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add DRYL.Components/Components/Navigation/DrylCommandPalette.razor DRYL.Components/Components/Navigation/DrylCommandPalette.razor.css tests/DRYL.Components.Tests/DrylCommandPaletteTests.cs
git commit -m "feat(DrylCommandPalette): resolver suggestion row + AI aura while resolving"
```

---

## Task 7: Argument-fill sub-view + destructive HITL confirm

**Files:**
- Modify: `DRYL.Components/Components/Navigation/DrylCommandPalette.razor`
- Modify: `DRYL.Components/Components/Navigation/DrylCommandPalette.razor.css`
- Test: extend `tests/DRYL.Components.Tests/DrylCommandPaletteTests.cs`

**Interfaces:**
- Consumes: `DrylCommandArgument`, `CommandArgType`, `CommandContext`, optional `IDrylDialogService` (`Services.GetService<IDrylDialogService>()`).
- Produces: an inline arg-fill state `_fillingCommand` + `_fillValues` and a `BuildContext()` → `CommandContext`.

**Design:**
- Running a command **with** arguments sets `_fillingCommand = cmd` and renders an inline panel inside the palette: one input per `DrylCommandArgument` —
  - `CommandArgType.Text` → `DrylInputText`
  - `CommandArgType.Number` → `DrylInputNumber`
  - `CommandArgType.Boolean` → `DrylToggle`
  - `CommandArgType.Choice` → `DrylSelect` over `Options`
  — plus a confirm `DrylButton`. A back affordance returns to the list. The arg-fill panel replaces the results list while active.
- On confirm: build `CommandContext` from `_fillValues` (respecting types), then run via `RunCommandAsync(cmd, ctx)`.
- `RunCommandAsync(cmd, ctx)`: if `cmd.Destructive` and an `IDrylDialogService` is available, `await _dialogs.ShowConfirmAsync("Aktion bestätigen", cmd.Title, "Ausführen", "Abbrechen")`; abort if `result.Canceled`. Then `await cmd.RunAsync(ctx)` and close (unless the command opts to keep open — commands always close here; AI-intent keep-open is a legacy `CommandItem` concept only).
- AI suggestion rows reuse `RunCommandAsync` with the resolver's args (so destructive AI suggestions are gated too).
- Resolver suggestions for commands with required args that the model **didn't** fill drop into the same arg-fill view pre-populated with whatever args were resolved.

- [ ] **Step 1: Write the failing test**

```csharp
    [Fact]
    public void Command_with_argument_opens_fill_view_then_runs_with_value()
    {
        CommandContext? captured = null;
        var cut = Render<DrylCommandPalette>(ps => ps
            .Add(p => p.Open, true)
            .AddChildContent<DrylCommand>(c => c
                .Add(x => x.Title, "Status setzen")
                .Add(x => x.OnRun, (CommandContext ctx) => { captured = ctx; })
                .AddChildContent<DrylCommandArgument>(a => a
                    .Add(p => p.Name, "status")
                    .Add(p => p.Type, CommandArgType.Choice)
                    .Add(p => p.Options, new[] { "Draft", "Paid" }))));

        cut.FindAll("[role=option]").First(o => o.TextContent.Contains("Status setzen")).Click();

        // Arg-fill view is shown
        Assert.NotEmpty(cut.FindAll(".cmd-args"));

        // Pick a value and confirm
        cut.Find(".cmd-args select").Change("Paid");
        cut.Find(".cmd-args-confirm").Click();

        Assert.NotNull(captured);
        Assert.Equal("Paid", captured!.GetArgument<string>("status"));
    }
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/DRYL.Components.Tests --filter DrylCommandPaletteTests`
Expected: FAIL — no `.cmd-args` panel.

- [ ] **Step 3: Implement** the arg-fill sub-view, `BuildContext()`, and `RunCommandAsync` with the destructive confirm. Bind each input to `_fillValues[argName]`. The confirm button carries class `cmd-args-confirm`. Style the panel in scoped CSS (tokens only: `--sp-*`, `--line`, `--glass-*`, `--r-*`). Icon-only back button (if any) gets a `DrylTooltip` + `aria-label` (rule 2.11).

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test tests/DRYL.Components.Tests --filter DrylCommandPaletteTests`
Expected: PASS.

- [ ] **Step 5: Build core Release again**

Run: `dotnet build DRYL.Components/DRYL.Components.csproj -c Release`
Expected: 0 errors.

- [ ] **Step 6: Commit**

```bash
git add DRYL.Components/Components/Navigation/DrylCommandPalette.razor DRYL.Components/Components/Navigation/DrylCommandPalette.razor.css tests/DRYL.Components.Tests/DrylCommandPaletteTests.cs
git commit -m "feat(DrylCommandPalette): argument-fill sub-view + destructive HITL confirm"
```

---

## Task 8: Agents — `DrylAiCommandResolver`

**Files:**
- Create: `DRYL.Components.Agents/CommandPalette/DrylAiCommandResolver.cs`
- Modify: `DRYL.Components.Agents/Extensions/ServiceCollectionExtensions.cs`
- Test: `tests/DRYL.Components.Tests/DrylAiCommandResolverTests.cs`

**Interfaces:**
- Consumes: `ICommandResolver`, `DrylCommand`, `CommandResolution`, `DrylCommandArgument`, `CommandArgType` (core); `AIAgent`, `AgentSession`, `AIFunctionFactory`, `ChatClientAgentRunOptions`, `ChatOptions` (`Microsoft.Agents.AI` / `Microsoft.Extensions.AI`).
- Produces:
  - `sealed class DrylAiCommandResolver : ICommandResolver` with ctor `(AIAgent agent, Func<AgentSession>? sessionFactory = null)`.
  - `IServiceCollection AddDrylCommandResolver(this IServiceCollection, Func<IServiceProvider, AIAgent> agentFactory)`.

**Design:** `ResolveAsync` builds one `AIFunction` per non-disabled command (`name = command.ResolvedId`, `description = $"{Title}. {Description}"` + an embedded JSON-schema hint built from the command's `DrylCommandArgument`s — mirroring `DrylAgentRunner.CreateUpdateTool<T>`'s schema-in-description approach). Each function body **records** `(command, argsJson)` into a captured field and returns a short receipt; it does **not** run `OnRun`. Run the agent once (`agent.RunAsync(prompt, session, runOptions, ct)`) with `Tools = functions` and `Instructions` = "Select exactly one command that fulfils the user's request and call its function with filled arguments. If none fit, call nothing." After the run, if a function was captured, parse the recorded args into `Dictionary<string, object?>` honouring each argument's `CommandArgType`, and return `new CommandResolution(command, args, 1.0)`; else `null`. Confidence is `1.0` on a tool call (the framework surfaces no probability). Honour `ct`.

- [ ] **Step 1: Write the failing test** (verifies the seam shape + that no execution happens without a model; we test the arg-parsing helper, which is deterministic)

```csharp
// tests/DRYL.Components.Tests/DrylAiCommandResolverTests.cs
using System.Text.Json;
using DRYL.Components;
using DRYL.Components.Agents;

namespace DRYL.Components.Tests;

public class DrylAiCommandResolverTests
{
    [Fact]
    public void ParseArguments_coerces_by_arg_type()
    {
        var cmd = new DrylCommand { Title = "Status setzen" };
        cmd.AddArgument(new DrylCommandArgument { Name = "status", Type = CommandArgType.Choice });
        cmd.AddArgument(new DrylCommandArgument { Name = "count", Type = CommandArgType.Number });
        cmd.AddArgument(new DrylCommandArgument { Name = "force", Type = CommandArgType.Boolean });

        var json = JsonSerializer.Deserialize<JsonElement>(
            """{"status":"Paid","count":3,"force":true}""");

        var args = DrylAiCommandResolver.ParseArguments(cmd, json);

        Assert.Equal("Paid", args["status"]);
        Assert.Equal(3d, Convert.ToDouble(args["count"]));
        Assert.Equal(true, args["force"]);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/DRYL.Components.Tests --filter DrylAiCommandResolverTests`
Expected: FAIL — type not defined.

- [ ] **Step 3: Write minimal implementation**

```csharp
// DRYL.Components.Agents/CommandPalette/DrylAiCommandResolver.cs
using System.Text;
using System.Text.Json;
using DRYL.Components;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace DRYL.Components.Agents;

/// <summary>An <see cref="ICommandResolver"/> that asks an <see cref="AIAgent"/> to resolve a
/// natural-language query into exactly one registered command + filled arguments — surfaced by the
/// palette as a confirmable suggestion. Each <see cref="DrylCommand"/> becomes an
/// <see cref="AIFunction"/> whose name is the command id; the function only records the selection,
/// so the model never executes anything (execution stays in the palette, after confirmation).</summary>
public sealed class DrylAiCommandResolver : ICommandResolver
{
    private static readonly JsonSerializerOptions JsonOpts =
        new(JsonSerializerDefaults.Web);

    private readonly AIAgent _agent;
    private readonly Func<AgentSession>? _sessionFactory;

    /// <summary>Creates the resolver over an agent (and optional per-resolution session factory).</summary>
    public DrylAiCommandResolver(AIAgent agent, Func<AgentSession>? sessionFactory = null)
    {
        _agent = agent;
        _sessionFactory = sessionFactory;
    }

    /// <inheritdoc />
    public async Task<CommandResolution?> ResolveAsync(
        string query, IReadOnlyList<DrylCommand> commands, CancellationToken ct)
    {
        DrylCommand? chosen = null;
        JsonElement chosenArgs = default;

        var tools = new List<AITool>();
        foreach (var command in commands.Where(c => !c.Disabled))
        {
            var c = command;
            tools.Add(AIFunctionFactory.Create(
                (JsonElement args) =>
                {
                    chosen = c;
                    chosenArgs = args.Clone();
                    return "recorded";
                },
                c.ResolvedId,
                BuildDescription(c)));
        }

        if (tools.Count == 0) return null;

        var session = _sessionFactory?.Invoke() ?? _agent.GetNewSession();
        var runOptions = new ChatClientAgentRunOptions
        {
            ChatOptions = new ChatOptions
            {
                Instructions =
                    "Select exactly one command that fulfils the user's request and call its " +
                    "function with filled arguments. If none fit, call nothing.",
                Tools = tools,
            }
        };

        await _agent.RunAsync(query, session, runOptions, ct).ConfigureAwait(false);

        if (chosen is null) return null;
        return new CommandResolution(chosen, ParseArguments(chosen, chosenArgs), 1.0);
    }

    private static string BuildDescription(DrylCommand c)
    {
        var sb = new StringBuilder();
        sb.Append(c.Title);
        if (!string.IsNullOrWhiteSpace(c.Description)) sb.Append(". ").Append(c.Description);
        if (c.Arguments.Count > 0)
        {
            sb.Append(" Arguments: ");
            sb.Append(string.Join(", ", c.Arguments.Select(a =>
                $"{a.Name} ({a.Type}{(a.Required ? ", required" : "")})" +
                (a.Options is { Length: > 0 } ? $" one of [{string.Join('|', a.Options)}]" : "") +
                (string.IsNullOrWhiteSpace(a.Description) ? "" : $" — {a.Description}"))));
        }
        return sb.ToString();
    }

    /// <summary>Coerces a model-produced JSON argument object into a typed dictionary using each
    /// argument's <see cref="CommandArgType"/>.</summary>
    public static IReadOnlyDictionary<string, object?> ParseArguments(DrylCommand command, JsonElement args)
    {
        var result = new Dictionary<string, object?>();
        if (args.ValueKind != JsonValueKind.Object) return result;

        foreach (var arg in command.Arguments)
        {
            if (!args.TryGetProperty(arg.Name, out var prop)) continue;
            result[arg.Name] = arg.Type switch
            {
                CommandArgType.Number  => prop.ValueKind == JsonValueKind.Number ? prop.GetDouble()
                                          : double.TryParse(prop.GetString(),
                                              System.Globalization.NumberStyles.Any,
                                              System.Globalization.CultureInfo.InvariantCulture, out var d) ? d : null,
                CommandArgType.Boolean => prop.ValueKind is JsonValueKind.True or JsonValueKind.False
                                          ? prop.GetBoolean()
                                          : bool.TryParse(prop.GetString(), out var b) ? b : null,
                _                      => prop.ValueKind == JsonValueKind.String ? prop.GetString() : prop.ToString(),
            };
        }
        return result;
    }
}
```

Add to `DRYL.Components.Agents/Extensions/ServiceCollectionExtensions.cs`:

```csharp
    /// <summary>Register a <see cref="DRYL.Components.ICommandResolver"/> backed by an agent, so a
    /// <c>DrylAiCommandPalette</c> (or <c>DrylCommandPalette Resolver=</c>) resolves natural-language
    /// queries into command tool calls. Supply the agent from your own DI.</summary>
    public static IServiceCollection AddDrylCommandResolver(
        this IServiceCollection services,
        Func<IServiceProvider, Microsoft.Agents.AI.AIAgent> agentFactory)
    {
        services.AddScoped<DRYL.Components.ICommandResolver>(
            sp => new DrylAiCommandResolver(agentFactory(sp)));
        return services;
    }
```

(Keep the existing `using Microsoft.Extensions.DependencyInjection;` and add `using DRYL.Components.Agents;` is unnecessary — same namespace.)

> **Verify** the exact session API at build time: if `AIAgent.GetNewSession()` is not the correct member name in `Microsoft.Agents.AI` 1.10.0, use the same construction the other runner methods use (they pass an `AgentSession` in). Adjust `_sessionFactory` default accordingly. The other runner methods receive the session from the caller; if no parameterless session creation exists, make `sessionFactory` **required** and document it in `DrylAiCommandPalette`/registration.

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test tests/DRYL.Components.Tests --filter DrylAiCommandResolverTests`
Expected: PASS.

- [ ] **Step 5: Build the Agents package**

Run: `dotnet build DRYL.Components.Agents/DRYL.Components.Agents.csproj -c Release`
Expected: 0 errors (all 3 TFMs).

- [ ] **Step 6: Commit**

```bash
git add DRYL.Components.Agents/CommandPalette/DrylAiCommandResolver.cs DRYL.Components.Agents/Extensions/ServiceCollectionExtensions.cs tests/DRYL.Components.Tests/DrylAiCommandResolverTests.cs
git commit -m "feat(Agents): DrylAiCommandResolver — commands as AIFunctions, one tool-call resolution"
```

---

## Task 9: Agents — `DrylAiCommandPalette` wrapper

**Files:**
- Create: `DRYL.Components.Agents/CommandPalette/DrylAiCommandPalette.razor`
- Test: `tests/DRYL.Components.Tests/DrylAiCommandPaletteTests.cs`

**Interfaces:**
- Consumes: `ICommandResolver` (DI), `DrylCommandPalette` (core).
- Produces: `DrylAiCommandPalette` forwarding `Open`/`OpenChanged`, `Placeholder`, `EmptyText`, `MaxResults`, `HotKey`, `Class`, `ChildContent` to `DrylCommandPalette` with `Resolver` set from DI.

- [ ] **Step 1: Write the failing test**

```csharp
// tests/DRYL.Components.Tests/DrylAiCommandPaletteTests.cs
using Bunit;
using DRYL.Components;
using DRYL.Components.Agents;
using Microsoft.Extensions.DependencyInjection;

namespace DRYL.Components.Tests;

public class DrylAiCommandPaletteTests : BunitContext
{
    public DrylAiCommandPaletteTests() => JSInterop.Mode = JSRuntimeMode.Loose;

    private sealed class NullResolver : ICommandResolver
    {
        public Task<CommandResolution?> ResolveAsync(
            string q, IReadOnlyList<DrylCommand> c, CancellationToken ct)
            => Task.FromResult<CommandResolution?>(null);
    }

    [Fact]
    public void Forwards_children_to_inner_palette()
    {
        Services.AddScoped<ICommandResolver>(_ => new NullResolver());
        var cut = Render<DrylAiCommandPalette>(ps => ps
            .Add(p => p.Open, true)
            .AddChildContent<DrylCommand>(c => c.Add(x => x.Title, "AI command")));

        Assert.Contains(cut.FindAll("[role=option]"), o => o.TextContent.Contains("AI command"));
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/DRYL.Components.Tests --filter DrylAiCommandPaletteTests`
Expected: FAIL — type not defined.

- [ ] **Step 3: Write minimal implementation**

```razor
@* DRYL.Components.Agents/CommandPalette/DrylAiCommandPalette.razor *@
@namespace DRYL.Components.Agents
@using DRYL.Components
@inject ICommandResolver Resolver

@*  Convenience wrapper: a DrylCommandPalette pre-wired with the DI-registered
    ICommandResolver, so natural-language queries resolve to command tool calls.
    Register the resolver with services.AddDrylCommandResolver(sp => yourAgent). *@

<DrylCommandPalette Open="Open" OpenChanged="OpenChanged"
                    Placeholder="@Placeholder" EmptyText="@EmptyText"
                    MaxResults="MaxResults" HotKey="HotKey"
                    Resolver="Resolver" Class="@Class">
    @ChildContent
</DrylCommandPalette>

@code {
    /// <summary>Two-way visibility.</summary>
    [Parameter] public bool Open { get; set; }
    /// <summary>Fires when <see cref="Open"/> changes.</summary>
    [Parameter] public EventCallback<bool> OpenChanged { get; set; }
    /// <summary>Search input placeholder.</summary>
    [Parameter] public string Placeholder { get; set; } = "Befehl suchen…";
    /// <summary>Empty-state text.</summary>
    [Parameter] public string EmptyText { get; set; } = "Keine Treffer";
    /// <summary>Maximum results shown.</summary>
    [Parameter] public int MaxResults { get; set; } = 8;
    /// <summary>Enable the global ⌘K / Ctrl+K toggle.</summary>
    [Parameter] public bool HotKey { get; set; } = true;
    /// <summary>Merged onto the palette panel.</summary>
    [Parameter] public string? Class { get; set; }
    /// <summary>Hosts declarative <c>DrylCommand</c>s.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test tests/DRYL.Components.Tests --filter DrylAiCommandPaletteTests`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add DRYL.Components.Agents/CommandPalette/DrylAiCommandPalette.razor tests/DRYL.Components.Tests/DrylAiCommandPaletteTests.cs
git commit -m "feat(Agents): DrylAiCommandPalette convenience wrapper"
```

---

## Task 10: Docs + demo

**Files:**
- Modify: `CHANGELOG.md`
- Modify: `DRYL.Website/Components/ComponentCatalog.cs`
- Modify: `DRYL.Website/Components/Pages/DemoCommandPalette.razor`
- Create: `DRYL.Website/Components/Examples/CommandPalette/DeclarativeCommands.razor`
- Create: `DRYL.Website/Components/Examples/CommandPalette/AiResolver.razor`

- [ ] **Step 1: CHANGELOG** — under `## [Unreleased] / ### Added`, append:

```markdown
- `DrylCommand` / `DrylCommandArgument` — Declarative command + typed arguments hosted in `DrylCommandPalette`; one `OnRun(CommandContext)` serves click, keyboard and AI. Self-register into `ICommandRegistry`
- `ICommandRegistry` / `CommandRegistry` — Scoped registry (registered by `AddDrylComponents()`) feeding the palette from both declarative `DrylCommand`s and consumer code; de-duplicated by `Id`
- `CommandContext` / `CommandArgType` — Execution payload (typed `GetArgument<T>`, cancellation) and argument input/schema types (`Text` / `Number` / `Boolean` / `Choice`)
- `ICommandResolver` / `CommandResolution` — Narrow, AI-free seam: a resolver turns a natural-language query into one command + filled arguments, surfaced as a confirmable top suggestion (HITL, never auto-fired)
- `DrylCommandPalette` — New `Resolver`, `HotKey`, `EmptyText`, `MaxResults`, and `ChildContent` parameters; hosts `DrylCommand`s, fuzzy-matches the registry alongside the existing `Items`/`SearchProvider`, fills arguments inline, glides the active row, and wears the shared AI aura while a resolver thinks. Existing `Items`/`SearchProvider` API unchanged
- `DrylAiCommandResolver` — (Agents) `ICommandResolver` that exposes each registered command to an agent as an `AIFunction` and resolves one structured tool call with filled arguments — execution deferred to confirmation; destructive commands gated by `DrylConfirmDialog`
- `DrylAiCommandPalette` — (Agents) Convenience wrapper pre-wired with the DI-registered `ICommandResolver`
- `AddDrylCommandResolver(...)` — (Agents) DI helper registering a `DrylAiCommandResolver` from a consumer-supplied `AIAgent`
```

- [ ] **Step 2: ComponentCatalog** — update the existing `Command Palette` entry description in `DRYL.Website/Components/ComponentCatalog.cs` to mention the new layer:

```csharp
        new("Command Palette", "command-palette",  "Actions",  "DrylCommandPalette","Navigation",  true,  "⌘K launcher — declarative commands, fuzzy search, inline arguments, AI tool-call resolution.", "Command"),
```

- [ ] **Step 3: Demo examples** — create the two example components showing (a) declarative `DrylCommand`s with an argument and (b) a palette driven by an `ICommandResolver` stub; add them to `DemoCommandPalette.razor` following the existing `DemoExample`/example-embedding pattern already used by the page (mirror `Basic.razor` / `AiIntent.razor`).

`DeclarativeCommands.razor`:
```razor
@* Declarative DrylCommands with a typed argument and one OnRun path. *@
<DrylButton OnClick="() => _open = true">Open (⌘K)</DrylButton>

<DrylCommandPalette @bind-Open="_open" Placeholder="Befehl suchen…">
    <DrylCommand Title="Neue Rechnung" Icon="Plus" Group="Erstellen"
                 Shortcut="⌘N" OnRun="@(_ => _last = "Neue Rechnung")" />
    <DrylCommand Title="Status setzen" Description="Status einer Rechnung ändern"
                 Group="Bearbeiten" OnRun="@SetStatus">
        <DrylCommandArgument Name="status" Description="Neuer Status"
                             Type="CommandArgType.Choice"
                             Options="@(new[] {"Draft","Sent","Paid"})" Required="true" />
    </DrylCommand>
    <DrylCommand Title="Alles löschen" Destructive="true"
                 OnRun="@(_ => _last = "Gelöscht")" />
</DrylCommandPalette>

<p class="text-muted">Zuletzt: @_last</p>

@code {
    private bool _open;
    private string _last = "—";
    private void SetStatus(CommandContext ctx) => _last = $"Status → {ctx.GetArgument<string>("status")}";
}
```

`AiResolver.razor`:
```razor
@* Palette with a (stub) ICommandResolver: a natural-language query resolves to a command. *@
<DrylButton OnClick="() => _open = true">Open (⌘K)</DrylButton>

<DrylCommandPalette @bind-Open="_open" Resolver="_resolver"
                    Placeholder="Sag, was du tun willst…">
    <DrylCommand Title="Status setzen" OnRun="@SetStatus">
        <DrylCommandArgument Name="status" Type="CommandArgType.Choice"
                             Options="@(new[] {"Draft","Sent","Paid"})" Required="true" />
    </DrylCommand>
</DrylCommandPalette>

@code {
    private bool _open;
    private readonly ICommandResolver _resolver = new DemoResolver();
    private void SetStatus(CommandContext ctx) { /* … */ }

    // Demo only — production uses DRYL.Components.Agents.DrylAiCommandResolver over a real agent.
    private sealed class DemoResolver : ICommandResolver
    {
        public Task<CommandResolution?> ResolveAsync(
            string q, IReadOnlyList<DrylCommand> c, CancellationToken ct)
            => Task.FromResult<CommandResolution?>(c.Count == 0 ? null
                : new CommandResolution(c[0],
                    new Dictionary<string, object?> { ["status"] = "Paid" }, 0.9));
    }
}
```

- [ ] **Step 4: Build the website** to confirm the demos compile

Run: `dotnet build DRYL.Website/DRYL.Website.csproj`
Expected: 0 errors.

- [ ] **Step 5: Commit**

```bash
git add CHANGELOG.md DRYL.Website/Components/ComponentCatalog.cs DRYL.Website/Components/Pages/DemoCommandPalette.razor DRYL.Website/Components/Examples/CommandPalette/
git commit -m "docs(DrylCommandPalette): changelog, catalog blurb, declarative + AI demos"
```

---

## Task 11: Full verification

- [ ] **Step 1: Run the whole test suite**

Run: `dotnet test tests/DRYL.Components.Tests`
Expected: all green (existing + ~14 new).

- [ ] **Step 2: Release build, both packages**

Run: `dotnet build DRYL.Components/DRYL.Components.csproj -c Release && dotnet build DRYL.Components.Agents/DRYL.Components.Agents.csproj -c Release`
Expected: 0 errors, 0 warnings introduced.

- [ ] **Step 3: Grep for accidental literals** in the new scoped CSS (rule 2.1)

Run: `grep -nE "#[0-9a-fA-F]{3,8}|rgba?\(|[0-9]+px|[0-9]+ms" DRYL.Components/Components/Navigation/DrylCommandPalette.razor.css`
Expected: no hardcoded colors/durations/arbitrary px — only `var(--…)` references. Fix any hit.

- [ ] **Step 4: Confirm core stayed AI-free**

Run: `grep -rn "Microsoft.Agents.AI\|Microsoft.Extensions.AI" DRYL.Components/Components/Navigation/`
Expected: no matches (the seam is `ICommandResolver`, defined in core, implemented only in Agents).

---

## Definition of Done (from spec §11)

- [ ] Core palette works with `Resolver = null` (fuzzy only), zero AI dependency, builds dependency-free.
- [ ] Declarative `DrylCommand` + `ICommandRegistry` both feed one de-duplicated list.
- [ ] `DrylAiCommandResolver` turns commands into `AIFunction`s and resolves one tool call with filled args; execution deferred to confirm; `Destructive` → `DrylConfirmDialog`.
- [ ] Gliding active row / AI aura on the fixed motion vocabulary; reduced-motion clean.
- [ ] Keyboard + ARIA intact; ⌘K hotkey prerender-safe (existing dispose guard, now gated by `HotKey`).
- [ ] `CHANGELOG.md`, `ComponentCatalog`, demo updated.
- [ ] Tests green; Release build clean.

## Self-Review notes

- **Existing API preserved:** `Items`, `SearchProvider`, `Ai`, `AiContent`, `CommandItem`, `CommandItemType` untouched (Task 5 projects, never removes). Non-breaking → MINOR.
- **Spec §6 enter/exit:** the shipped palette already animates via the dialog backdrop; this plan adds the *new* motion (active-row glide, AI aura, stagger) rather than re-architecting the open/close (which would risk the portal/focus-trap wiring). Noted as a deliberate scope choice in the PR — full `DrylPresence` re-wrap of open/close is deferred, not required for the additive feature.
- **Spec §5.3 registration:** `AddDrylAgents()` cannot know the consumer's `AIAgent`; the resolver is registered via `AddDrylCommandResolver(sp => agent)` instead — same effect, explicit about the agent dependency.
- **Confidence:** the framework exposes no probability, so `1.0` on a tool call, `null` (no resolution) otherwise — faithful to the record shape.
