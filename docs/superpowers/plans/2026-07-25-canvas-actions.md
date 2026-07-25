# Canvas Actions (Phase 2) — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** A button inside a canvas artifact triggers a typed host command — registered in DI, executed with a service scope, confirmed when destructive — instead of turning every click into chat prose.

**Architecture:** A singleton `CanvasActionRegistry` holds named host actions registered via `AddDrylCanvasAction`; a scoped `ICanvasActionService` runs them against the current scope. A per-canvas `CanvasActionRunner` (sibling of Phase 1's `CanvasDataBinder`, reached through the same `CanvasContext` cascade) resolves `$field` arguments via a shared `CanvasArgs` helper, optionally confirms via `IDrylDialogService`, invokes the handler, then applies the result: patch ops through `CanvasPatcher`, refreshes through `ICanvasDataService.Invalidate`, a success toast through `IDrylToastService`, a failure inline at the button, and an optional `.AskAi(…)` through the existing `OnInteraction` path. The AI can author and label an action button but has no code path that triggers one (A4).

**Tech Stack:** .NET 8/9/10, Blazor (Server + WASM), `System.Text.Json`, bUnit + xUnit, Microsoft.Agents.AI (Agents package only).

## Global Constraints

- Core `DRYL.Components`: `<Version>` 2.12.0 → **2.13.0** (MINOR, purely additive).
- Agents `DRYL.Components.Agents`: `<Version>` 0.10.0 → **0.11.0** (MINOR).
- Both version bumps + the cut `CHANGELOG.md` release land in the **final** commit of the phase, not earlier — a half-done push must not publish.
- `publish.yml` already publishes both packages (core tag `v<ver>`, agents tag `agents-v<ver>`). No workflow change needed; verify only.
- **No new CSS tokens, durations, easings or colors.** `.canvas-action-error` reuses exactly the tokens `.canvas-data-error` already uses.
- `action` on a node is optional; a button without it behaves exactly as today (`intent` → `OnInteraction`).
- The AI never triggers an action. There must be **no** tool, no prompt path and no code path from a model output to `ICanvasActionService.InvokeAsync` (A4).
- Every icon-only button gets `DrylTooltip` + `aria-label` (CLAUDE.md 2.11).
- All numeric string interpolation for CSS/SVG/receipts uses `FormattableString.Invariant`.
- `Ai`/`AiState` vocabulary unchanged — no new AI states.
- Tests live in `tests/DRYL.Components.Tests/Canvas/` (core) and `tests/DRYL.Components.Tests/Agents/Canvas/` (Agents).
- Build with `dotnet build DRYL.slnx`, test with `dotnet test DRYL.slnx`.

---

## File Structure

**Core — new files in `DRYL.Components/Canvas/` (namespace `DRYL.Components.Canvas`):**

| File | Responsibility |
| --- | --- |
| `CanvasArgs.cs` | shared `$field` argument/parameter resolution (extracted from `CanvasDataBinder`) |
| `CanvasAction.cs` | `CanvasActionBinding`, `CanvasActionContext`, `CanvasActionResult`, `CanvasActionDescriptor`, `CanvasActionSource`, `CanvasActionOutcome` |
| `CanvasActionRegistry.cs` | singleton registry of named actions |
| `CanvasActionService.cs` | `ICanvasActionService` + internal `CanvasActionService` |
| `CanvasActionRunner.cs` | per-canvas runner: busy/error state, confirm, invoke, result handling |
| `CanvasActionPrompt.cs` | descriptor list → model-facing ACTIONS block |

**Core — modified:**
- `Canvas/CanvasSpec.cs` — `CanvasNode.Action`
- `Canvas/CanvasInteraction.cs` — optional `Message`
- `Canvas/CanvasCatalog.cs` — action validation, `CanvasValidationContext.Actions`, `kind: "danger"`, `intent` optional with an action
- `Canvas/CanvasContext.cs` — `Actions`, `Patch`
- `Canvas/CanvasDataBinder.cs` — delegate `$field` work to `CanvasArgs`
- `Canvas/CanvasNodeView.razor` — button render path
- `Components/Ai/DrylCanvas.razor` (+ `.razor.css`) — runner lifecycle, `OnAction`, `Ctx.Patch`, `.canvas-action-error`
- `Extensions/CanvasServiceCollectionExtensions.cs` — `AddDrylCanvasAction`

**Agents — modified:**
- `Canvas/CanvasPrompt.cs` — `"danger"` in the button line; ACTIONS block in `CreatePrompt`/`UpdatePrompt`
- `Canvas/DrylCanvasTools.cs` — optional `ICanvasActionService? actions`
- `Canvas/DrylAiCanvas.razor` — pass `OnAction` through

**Website:**
- `Components/Examples/Canvas/CanvasActions.razor` (new demo example)
- `Components/Pages/DemoCanvas.razor`, `ComponentCatalog.cs`, `Program.cs`

**Tests:**
- `tests/DRYL.Components.Tests/Canvas/CanvasArgsTests.cs`
- `tests/DRYL.Components.Tests/Canvas/CanvasActionRegistryTests.cs`
- `tests/DRYL.Components.Tests/Canvas/CanvasActionPromptTests.cs`
- `tests/DRYL.Components.Tests/Canvas/CanvasActionRunnerTests.cs`
- `tests/DRYL.Components.Tests/Canvas/CanvasActionValidationTests.cs`
- `tests/DRYL.Components.Tests/Canvas/CanvasActionRenderTests.cs`
- `tests/DRYL.Components.Tests/Agents/Canvas/CanvasActionReceiptTests.cs`

---

### Task 1: Shared `$field` resolution (`CanvasArgs`)

Binder and runner must read `{ "$field": "…" }` identically — the prompt promises the model the
same syntax in both places. One helper, two callers, one test that pins them against each other.

**Files:**
- Create: `DRYL.Components/Canvas/CanvasArgs.cs`
- Modify: `DRYL.Components/Canvas/CanvasDataBinder.cs`
- Test: `tests/DRYL.Components.Tests/Canvas/CanvasArgsTests.cs`

**Interfaces:**
- Consumes: `CanvasFormState.Get(string)` (existing), `CanvasJson.Options` (existing).
- Produces:

```csharp
namespace DRYL.Components.Canvas;

internal static class CanvasArgs
{
    /// Resolves a raw args/params object: literals stay, { "$field": "x" } becomes the
    /// current form value. Returns null when there is nothing to resolve.
    public static JsonElement? Resolve(JsonElement? raw, CanvasFormState form, out HashSet<string> fields);

    /// The field name of a { "$field": "…" } reference, or null for a literal.
    public static string? FieldReference(JsonElement value);

    /// True when any value of the object is a field reference.
    public static bool HasFieldReference(JsonElement? args);
}
```

- [ ] **Step 1: Write the failing tests**

Create `tests/DRYL.Components.Tests/Canvas/CanvasArgsTests.cs`:

```csharp
using System.Text.Json;
using DRYL.Components.Canvas;

namespace DRYL.Components.Tests.Canvas;

public class CanvasArgsTests
{
    private static JsonElement Json(string raw) => JsonDocument.Parse(raw).RootElement.Clone();

    [Fact]
    public void Literals_pass_through_unchanged()
    {
        var form = new CanvasFormState();

        var resolved = CanvasArgs.Resolve(Json("""{"year":2026,"region":"EMEA"}"""), form, out var fields);

        Assert.Empty(fields);
        Assert.Equal(2026, resolved!.Value.GetProperty("year").GetInt32());
        Assert.Equal("EMEA", resolved.Value.GetProperty("region").GetString());
    }

    [Fact]
    public void Field_reference_reads_the_current_form_value()
    {
        var form = new CanvasFormState();
        form.Set("region", "APAC");

        var resolved = CanvasArgs.Resolve(Json("""{"region":{"$field":"region"}}"""), form, out var fields);

        Assert.Equal(new[] { "region" }, fields);
        Assert.Equal("APAC", resolved!.Value.GetProperty("region").GetString());
    }

    [Fact]
    public void Field_reference_to_an_unset_field_resolves_to_null()
    {
        var resolved = CanvasArgs.Resolve(Json("""{"region":{"$field":"nope"}}"""),
                                          new CanvasFormState(), out _);

        Assert.Equal(JsonValueKind.Null, resolved!.Value.GetProperty("region").ValueKind);
    }

    [Fact]
    public void A_non_object_or_missing_args_resolves_to_null()
    {
        Assert.Null(CanvasArgs.Resolve(null, new CanvasFormState(), out _));
        Assert.Null(CanvasArgs.Resolve(Json("42"), new CanvasFormState(), out _));
    }

    [Fact]
    public void FieldReference_recognises_only_the_dollar_field_shape()
    {
        Assert.Equal("x", CanvasArgs.FieldReference(Json("""{"$field":"x"}""")));
        Assert.Null(CanvasArgs.FieldReference(Json("""{"field":"x"}""")));
        Assert.Null(CanvasArgs.FieldReference(Json("\"x\"")));
    }

    [Fact]
    public void HasFieldReference_detects_a_reference_anywhere_in_the_object()
    {
        Assert.True(CanvasArgs.HasFieldReference(Json("""{"a":1,"b":{"$field":"x"}}""")));
        Assert.False(CanvasArgs.HasFieldReference(Json("""{"a":1}""")));
        Assert.False(CanvasArgs.HasFieldReference(null));
    }

    // The binder and the runner must produce byte-identical JSON for the same input —
    // two copies of this logic would drift and the prompt promises one syntax.
    [Fact]
    public void The_binder_resolves_through_the_same_helper()
    {
        var form = new CanvasFormState();
        form.Set("region", "APAC");
        var raw = Json("""{"year":2026,"region":{"$field":"region"}}""");

        var direct = CanvasArgs.Resolve(raw, form, out _);
        var viaKey = CanvasDataKey.Of("s", direct);

        Assert.Equal(viaKey, CanvasDataKey.Of("s", CanvasArgs.Resolve(raw, form, out _)));
    }
}
```

`InternalsVisibleTo` for the test project already exists (Phase 1 tests reach
`CanvasDataBinder.TryParseInterval`); verify with
`grep -rn "InternalsVisibleTo" DRYL.Components/DRYL.Components.csproj` and add
`<InternalsVisibleTo Include="DRYL.Components.Tests" />` to the csproj `<ItemGroup>` if it is missing.

- [ ] **Step 2: Run the tests, confirm they fail**

Run: `dotnet test DRYL.slnx --filter FullyQualifiedName~CanvasArgsTests`
Expected: FAIL — `CanvasArgs` does not exist (compile error).

- [ ] **Step 3: Implement `CanvasArgs`**

Create `DRYL.Components/Canvas/CanvasArgs.cs`:

```csharp
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace DRYL.Components.Canvas;

/// <summary>
/// Resolves the argument/parameter objects of a canvas binding: literals stay as they are,
/// <c>{ "$field": "&lt;name&gt;" }</c> becomes the interactive node's current value.
/// <para>Shared by <see cref="CanvasDataBinder"/> (data params) and
/// <see cref="CanvasActionRunner"/> (action args) on purpose — the model is told the syntax is
/// the same in both places, so there must not be two implementations of it.</para>
/// </summary>
internal static class CanvasArgs
{
    /// <summary>Resolves <paramref name="raw"/> against <paramref name="form"/>. Returns
    /// <c>null</c> when there is no object to resolve; <paramref name="fields"/> collects every
    /// referenced field name so a caller can react to exactly those changing.</summary>
    public static JsonElement? Resolve(JsonElement? raw, CanvasFormState form, out HashSet<string> fields)
    {
        fields = new HashSet<string>(StringComparer.Ordinal);
        if (raw is not { ValueKind: JsonValueKind.Object } obj) return null;

        var result = new JsonObject();
        foreach (var p in obj.EnumerateObject())
        {
            if (FieldReference(p.Value) is { } field)
            {
                fields.Add(field);
                result[p.Name] = ToNode(form.Get(field));
            }
            else
            {
                result[p.Name] = JsonNode.Parse(p.Value.GetRawText());
            }
        }
        return JsonSerializer.SerializeToElement(result);
    }

    /// <summary>The field name of a <c>{ "$field": "…" }</c> reference, or <c>null</c> for a literal.</summary>
    public static string? FieldReference(JsonElement value) =>
        value.ValueKind == JsonValueKind.Object &&
        value.TryGetProperty("$field", out var f) &&
        f.ValueKind == JsonValueKind.String
            ? f.GetString()
            : null;

    /// <summary>True when at least one value of the object is a field reference.</summary>
    public static bool HasFieldReference(JsonElement? args) =>
        args is { ValueKind: JsonValueKind.Object } p &&
        p.EnumerateObject().Any(x => FieldReference(x.Value) is not null);

    private static JsonNode? ToNode(object? value) => value switch
    {
        null => null,
        string s => JsonValue.Create(s),
        bool b => JsonValue.Create(b),
        double d => JsonValue.Create(d),
        int i => JsonValue.Create(i),
        long l => JsonValue.Create(l),
        decimal m => JsonValue.Create(m),
        _ => JsonValue.Create(Convert.ToString(value, CultureInfo.InvariantCulture)),
    };
}
```

- [ ] **Step 4: Route `CanvasDataBinder` through the helper**

In `DRYL.Components/Canvas/CanvasDataBinder.cs`:

Replace the body of `Resolve` with:

```csharp
    private (string Key, JsonElement? Params, HashSet<string> Fields) Resolve(CanvasDataBinding binding)
    {
        var resolved = CanvasArgs.Resolve(binding.Params, _form, out var fields);
        return (CanvasDataKey.Of(binding.Source!, resolved), resolved, fields);
    }
```

Replace the two static helpers with forwarders (they keep their existing call sites in
`CanvasCatalog`):

```csharp
    /// <summary>The field name of a <c>{ "$field": "…" }</c> reference, or <c>null</c> for a literal.</summary>
    internal static string? FieldReference(JsonElement value) => CanvasArgs.FieldReference(value);

    internal static bool HasFieldReference(CanvasDataBinding binding) =>
        CanvasArgs.HasFieldReference(binding.Params);
```

Delete the now-unused private `ToNode` method and the `using System.Text.Json.Nodes;` import if
nothing else in the file needs it.

- [ ] **Step 5: Run the tests, confirm green**

Run: `dotnet test DRYL.slnx --filter FullyQualifiedName~Canvas`
Expected: PASS — the new `CanvasArgsTests` plus the full existing Phase 1 canvas set.

- [ ] **Step 6: Commit**

```bash
git add DRYL.Components/Canvas/CanvasArgs.cs DRYL.Components/Canvas/CanvasDataBinder.cs tests/DRYL.Components.Tests/Canvas/CanvasArgsTests.cs
git commit -m "refactor(canvas): share the \$field resolver between binder and actions

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>"
```

---

### Task 2: Action types, registry and DI registration

**Files:**
- Create: `DRYL.Components/Canvas/CanvasAction.cs`, `DRYL.Components/Canvas/CanvasActionRegistry.cs`
- Modify: `DRYL.Components/Extensions/CanvasServiceCollectionExtensions.cs`
- Test: `tests/DRYL.Components.Tests/Canvas/CanvasActionRegistryTests.cs`

**Interfaces:**
- Consumes: `CanvasParamSchema.Describe(Type)` and `CanvasParamInfo` from Task 0 of Phase 1 (existing, `CanvasDataRegistry.cs`); `CanvasInvalidation` (existing, `CanvasDataService.cs`); `CanvasOp` (existing, `CanvasPatch.cs`); `CanvasDataKey.Canonicalize` (existing).
- Produces:

```csharp
namespace DRYL.Components.Canvas;

public sealed class CanvasActionBinding
{
    public string? Name { get; set; }
    public JsonElement? Args { get; set; }
    public string? Confirm { get; set; }
}

public sealed class CanvasActionContext
{
    public IServiceProvider Services { get; }
    public string NodeId { get; }
    public IReadOnlyDictionary<string, object?> Values { get; }
    public T? Get<T>(string name);
}

public sealed class CanvasActionResult
{
    public static CanvasActionResult Ok(string? message = null);
    public static CanvasActionResult Fail(string message);
    public bool Succeeded { get; }
    public string? Message { get; }
    public IReadOnlyList<CanvasInvalidation> Refreshes { get; }
    public IReadOnlyList<CanvasOp> Ops { get; }
    public string? Ask { get; }
    public CanvasActionResult Refresh(params string[] sources);
    public CanvasActionResult Refresh(string source, object parameters);
    public CanvasActionResult Patch(params CanvasOp[] ops);
    public CanvasActionResult AskAi(string message);
}

public sealed record CanvasActionDescriptor(
    string Name, string Description, IReadOnlyList<CanvasParamInfo> Args);

public sealed record CanvasActionOutcome(
    string Action, string NodeId, bool Succeeded, string? Message);

public sealed class CanvasActionSource
{
    public CanvasActionDescriptor Descriptor { get; }
}

public sealed class CanvasActionRegistry
{
    public IReadOnlyList<CanvasActionDescriptor> Descriptors { get; }
    public bool TryGet(string name, out CanvasActionSource action);
}

public static class CanvasServiceCollectionExtensions   // existing class, new members
{
    public static IServiceCollection AddDrylCanvasAction<TArgs>(
        this IServiceCollection services, string name, string description,
        Func<TArgs, CanvasActionContext, CancellationToken, Task<CanvasActionResult>> handler)
        where TArgs : class;

    public static IServiceCollection AddDrylCanvasAction(
        this IServiceCollection services, string name, string description,
        Func<CanvasActionContext, CancellationToken, Task<CanvasActionResult>> handler);
}
```

- [ ] **Step 1: Write the failing tests**

Create `tests/DRYL.Components.Tests/Canvas/CanvasActionRegistryTests.cs`:

```csharp
using DRYL.Components;
using DRYL.Components.Canvas;
using Microsoft.Extensions.DependencyInjection;

namespace DRYL.Components.Tests.Canvas;

public sealed record ApproveArgs(string OrderId, string? Note = null);
public sealed record BadArgs(TimeSpan Window);

public class CanvasActionRegistryTests
{
    private static CanvasActionRegistry RegistryOf(IServiceCollection services) =>
        services.BuildServiceProvider().GetRequiredService<CanvasActionRegistry>();

    [Fact]
    public void Derives_required_and_optional_args_from_the_record()
    {
        var services = new ServiceCollection();
        services.AddDrylCanvasAction("order.approve", "Gibt einen Auftrag frei.",
            (ApproveArgs a, CanvasActionContext ctx, CancellationToken ct) =>
                Task.FromResult(CanvasActionResult.Ok()));

        var d = Assert.Single(RegistryOf(services).Descriptors);

        Assert.Equal("order.approve", d.Name);
        Assert.Equal("Gibt einen Auftrag frei.", d.Description);
        Assert.Collection(d.Args,
            a => { Assert.Equal("orderId", a.Name); Assert.Equal("string", a.TypeName); Assert.True(a.Required); },
            a => { Assert.Equal("note", a.Name); Assert.False(a.Required); });
    }

    [Fact]
    public void The_parameterless_overload_has_no_args()
    {
        var services = new ServiceCollection();
        services.AddDrylCanvasAction("cache.clear", "Leert den Cache.",
            (CanvasActionContext ctx, CancellationToken ct) => Task.FromResult(CanvasActionResult.Ok()));

        Assert.Empty(Assert.Single(RegistryOf(services).Descriptors).Args);
    }

    [Fact]
    public void An_unsupported_arg_type_throws_at_registration()
    {
        var services = new ServiceCollection();

        Assert.Throws<ArgumentException>(() =>
            services.AddDrylCanvasAction("bad", "…",
                (BadArgs a, CanvasActionContext ctx, CancellationToken ct) =>
                    Task.FromResult(CanvasActionResult.Ok())));
    }

    [Fact]
    public void A_duplicate_action_name_throws()
    {
        var services = new ServiceCollection();
        services.AddDrylCanvasAction("a", "…",
            (CanvasActionContext c, CancellationToken t) => Task.FromResult(CanvasActionResult.Ok()));

        Assert.Throws<InvalidOperationException>(() =>
            services.AddDrylCanvasAction("a", "…",
                (CanvasActionContext c, CancellationToken t) => Task.FromResult(CanvasActionResult.Ok())));
    }

    [Fact]
    public void An_empty_name_throws()
    {
        var services = new ServiceCollection();

        Assert.Throws<ArgumentException>(() =>
            services.AddDrylCanvasAction("  ", "…",
                (CanvasActionContext c, CancellationToken t) => Task.FromResult(CanvasActionResult.Ok())));
    }

    [Fact]
    public void The_result_builder_collects_refreshes_ops_and_ask()
    {
        var result = CanvasActionResult.Ok("done")
            .Refresh("orders.open", "orders.count")
            .Refresh("sales.byMonth", new { year = 2026 })
            .Patch(new CanvasOp { Op = "setProps", Id = "b", Props = null })
            .AskAi("Auftrag 4711 wurde freigegeben.");

        Assert.True(result.Succeeded);
        Assert.Equal("done", result.Message);
        Assert.Equal(3, result.Refreshes.Count);
        Assert.Null(result.Refreshes[0].ParamsKey);                 // whole-source refresh
        Assert.NotNull(result.Refreshes[2].ParamsKey);              // parameterised refresh
        Assert.Single(result.Ops);
        Assert.Equal("Auftrag 4711 wurde freigegeben.", result.Ask);
    }

    [Fact]
    public void Fail_carries_the_message_and_is_not_succeeded()
    {
        var result = CanvasActionResult.Fail("Auftrag ist bereits freigegeben.");

        Assert.False(result.Succeeded);
        Assert.Equal("Auftrag ist bereits freigegeben.", result.Message);
    }
}
```

- [ ] **Step 2: Run the tests, confirm they fail**

Run: `dotnet test DRYL.slnx --filter FullyQualifiedName~CanvasActionRegistryTests`
Expected: FAIL — the action types do not exist (compile error).

- [ ] **Step 3: Implement the action types**

Create `DRYL.Components/Canvas/CanvasAction.cs`:

```csharp
using System.Text.Json;

namespace DRYL.Components.Canvas;

/// <summary>
/// A node's link to a registered host action. Declared by the model as
/// <c>"action": { "name": "order.approve", "args": { … }, "confirm": "Really?" }</c>.
/// <para>The AI authors and labels the button; it never presses it (A4).</para>
/// </summary>
public sealed class CanvasActionBinding
{
    /// <summary>The registered action name, e.g. <c>order.approve</c>.</summary>
    public string? Name { get; set; }

    /// <summary>Argument object. Each value is a literal or a field reference
    /// <c>{ "$field": "&lt;name of an interactive node&gt;" }</c> — the same syntax
    /// a data binding uses for its params.</summary>
    public JsonElement? Args { get; set; }

    /// <summary>When set, the user must confirm this question in a dialog before the
    /// handler runs. Use it for anything destructive or irreversible.</summary>
    public string? Confirm { get; set; }
}

/// <summary>What an action handler gets besides its typed arguments.</summary>
public sealed class CanvasActionContext
{
    internal CanvasActionContext(IServiceProvider services, string nodeId,
                                 IReadOnlyDictionary<string, object?> values)
    {
        Services = services;
        NodeId = nodeId;
        Values = values;
    }

    /// <summary>The circuit's (or request's) service scope. Tenant and user come from here,
    /// never from the artifact spec — the model must not be able to influence them.</summary>
    public IServiceProvider Services { get; }

    /// <summary>The id of the button the user pressed.</summary>
    public string NodeId { get; }

    /// <summary>Every interactive field of the artifact, not only those named in <c>args</c>.</summary>
    public IReadOnlyDictionary<string, object?> Values { get; }

    /// <summary>A field from <see cref="Values"/>, or <c>default</c> when absent or of another type.</summary>
    public T? Get<T>(string name) =>
        Values.TryGetValue(name, out var value) && value is T typed ? typed : default;
}

/// <summary>
/// What the canvas should do once a handler returns. A fluent, mutating builder: every method
/// returns the same instance.
/// </summary>
public sealed class CanvasActionResult
{
    private readonly List<CanvasInvalidation> _refreshes = new();
    private readonly List<CanvasOp> _ops = new();

    private CanvasActionResult(bool succeeded, string? message)
    {
        Succeeded = succeeded;
        Message = message;
    }

    /// <summary>The action succeeded. <paramref name="message"/> is shown as a success toast.</summary>
    public static CanvasActionResult Ok(string? message = null) => new(true, message);

    /// <summary>The action failed. <paramref name="message"/> is shown inline at the button and
    /// stays there until the next attempt — the user has to react to it.</summary>
    public static CanvasActionResult Fail(string message) => new(false, message);

    /// <summary>False when the handler reported a domain failure.</summary>
    public bool Succeeded { get; }

    /// <summary>The success toast text, or the inline failure text.</summary>
    public string? Message { get; }

    /// <summary>Data sources to reload once the action has run.</summary>
    public IReadOnlyList<CanvasInvalidation> Refreshes => _refreshes;

    /// <summary>Patch ops applied to the artifact in one batch — one action is one movement (A8).</summary>
    public IReadOnlyList<CanvasOp> Ops => _ops;

    /// <summary>The message handed to the assistant afterwards, or <c>null</c> (the default).</summary>
    public string? Ask { get; private set; }

    /// <summary>Reloads every binding on these sources, in this and any other canvas.</summary>
    public CanvasActionResult Refresh(params string[] sources)
    {
        foreach (var s in sources)
            if (!string.IsNullOrWhiteSpace(s)) _refreshes.Add(new CanvasInvalidation(s, null));
        return this;
    }

    /// <summary>Reloads only the bindings on <paramref name="source"/> whose params match
    /// <paramref name="parameters"/> (an anonymous object or the param record).</summary>
    public CanvasActionResult Refresh(string source, object parameters)
    {
        _refreshes.Add(new CanvasInvalidation(source,
            CanvasDataKey.Canonicalize(JsonSerializer.SerializeToElement(parameters, CanvasJson.Options))));
        return this;
    }

    /// <summary>Applies patch ops to the artifact — e.g. flipping a badge to green without
    /// waiting for a data source or an AI turn.</summary>
    public CanvasActionResult Patch(params CanvasOp[] ops)
    {
        foreach (var op in ops) if (op is not null) _ops.Add(op);
        return this;
    }

    /// <summary>Tells the assistant what happened, as the next chat turn. Off by default:
    /// the AI reacts to an action, it never causes one (A4).</summary>
    public CanvasActionResult AskAi(string message)
    {
        Ask = message;
        return this;
    }
}

/// <summary>Everything the model and the validator need to know about one action.</summary>
public sealed record CanvasActionDescriptor(
    string Name, string Description, IReadOnlyList<CanvasParamInfo> Args);

/// <summary>Reported to the host after every completed action — successful or not.</summary>
public sealed record CanvasActionOutcome(
    string Action, string NodeId, bool Succeeded, string? Message);

/// <summary>A registered action: its descriptor plus the invoker that runs the host's handler.</summary>
public sealed class CanvasActionSource
{
    internal CanvasActionSource(
        CanvasActionDescriptor descriptor,
        Func<JsonElement?, CanvasActionContext, CancellationToken, Task<CanvasActionResult>> invoke)
    {
        Descriptor = descriptor;
        Invoke = invoke;
    }

    /// <summary>Name, description and argument schema.</summary>
    public CanvasActionDescriptor Descriptor { get; }

    internal Func<JsonElement?, CanvasActionContext, CancellationToken, Task<CanvasActionResult>> Invoke { get; }
}
```

Create `DRYL.Components/Canvas/CanvasActionRegistry.cs`:

```csharp
namespace DRYL.Components.Canvas;

/// <summary>
/// The application-wide set of named canvas actions, filled at startup by
/// <c>AddDrylCanvasAction</c>. Registered as a singleton; the per-scope
/// <see cref="ICanvasActionService"/> runs its entries against the current scope.
/// </summary>
public sealed class CanvasActionRegistry
{
    private readonly Dictionary<string, CanvasActionSource> _actions = new(StringComparer.Ordinal);
    private readonly List<CanvasActionDescriptor> _descriptors = new();

    /// <summary>Every registered action, in registration order.</summary>
    public IReadOnlyList<CanvasActionDescriptor> Descriptors => _descriptors;

    /// <summary>Looks up an action by its registered name.</summary>
    public bool TryGet(string name, out CanvasActionSource action) => _actions.TryGetValue(name, out action!);

    internal void Add(CanvasActionSource action)
    {
        if (!_actions.TryAdd(action.Descriptor.Name, action))
            throw new InvalidOperationException(
                $"A canvas action named '{action.Descriptor.Name}' is already registered.");
        _descriptors.Add(action.Descriptor);
    }
}
```

- [ ] **Step 4: Add the DI extension methods**

Append to `DRYL.Components/Extensions/CanvasServiceCollectionExtensions.cs` (inside the existing
class, after `AddDrylCanvasDataSource`):

```csharp
    /// <summary>
    /// Registers a named canvas action with typed arguments. A button in the artifact binds to it
    /// through <c>"action": { "name": … }</c>; only a user press ever runs it (A4).
    /// <code>
    /// public sealed record ApproveArgs(string OrderId, string? Note = null);
    ///
    /// builder.Services.AddDrylCanvasAction("order.approve",
    ///     "Gibt einen Auftrag frei.",
    ///     async (ApproveArgs a, CanvasActionContext ctx, CancellationToken ct) =>
    ///     {
    ///         await ctx.Services.GetRequiredService&lt;IOrderService&gt;().ApproveAsync(a.OrderId, ct);
    ///         return CanvasActionResult.Ok("Auftrag freigegeben").Refresh("orders.open");
    ///     });
    /// </code>
    /// </summary>
    /// <remarks>A success message is shown as a toast, which needs a
    /// <c>&lt;DrylToastProvider/&gt;</c> in the layout; a <c>confirm</c> on the binding needs a
    /// <c>&lt;DrylDialogProvider/&gt;</c>.</remarks>
    /// <param name="services">The service collection.</param>
    /// <param name="name">The key used in a node's <c>action.name</c>. Convention: <c>area.thing</c>.</param>
    /// <param name="description">One model-facing sentence: what the action does. Do <em>not</em>
    /// describe the arguments — those are derived from <typeparamref name="TArgs"/>.</param>
    /// <param name="handler">Runs the command. Tenant and user come from <c>ctx.Services</c>.</param>
    /// <typeparam name="TArgs">A record whose primary constructor declares the arguments.</typeparam>
    /// <exception cref="ArgumentException">An argument uses an unsupported type, or the name is empty.</exception>
    /// <exception cref="InvalidOperationException">The name is already registered.</exception>
    public static IServiceCollection AddDrylCanvasAction<TArgs>(
        this IServiceCollection services,
        string name,
        string description,
        Func<TArgs, CanvasActionContext, CancellationToken, Task<CanvasActionResult>> handler)
        where TArgs : class
    {
        ArgumentNullException.ThrowIfNull(handler);
        ValidateName(name);

        var descriptor = new CanvasActionDescriptor(
            name, description ?? string.Empty, CanvasParamSchema.Describe(typeof(TArgs)));

        ActionRegistry(services).Add(new CanvasActionSource(descriptor, async (json, ctx, ct) =>
        {
            var args = Deserialize<TArgs>(json, name);
            return await handler(args, ctx, ct).ConfigureAwait(false);
        }));

        return services;
    }

    /// <summary>Registers a named canvas action that takes no arguments.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="name">The key used in a node's <c>action.name</c>. Convention: <c>area.thing</c>.</param>
    /// <param name="description">One model-facing sentence: what the action does.</param>
    /// <param name="handler">Runs the command. Tenant and user come from <c>ctx.Services</c>.</param>
    /// <exception cref="ArgumentException">The name is empty.</exception>
    /// <exception cref="InvalidOperationException">The name is already registered.</exception>
    public static IServiceCollection AddDrylCanvasAction(
        this IServiceCollection services,
        string name,
        string description,
        Func<CanvasActionContext, CancellationToken, Task<CanvasActionResult>> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        ValidateName(name);

        var descriptor = new CanvasActionDescriptor(
            name, description ?? string.Empty, Array.Empty<CanvasParamInfo>());

        ActionRegistry(services).Add(new CanvasActionSource(descriptor,
            async (_, ctx, ct) => await handler(ctx, ct).ConfigureAwait(false)));

        return services;
    }

    // Same shape as Registry(services): a singleton *instance* so registrations are collected
    // while the collection is still being built.
    internal static CanvasActionRegistry ActionRegistry(IServiceCollection services)
    {
        foreach (var d in services)
            if (d.ServiceType == typeof(CanvasActionRegistry) &&
                d.ImplementationInstance is CanvasActionRegistry existing)
                return existing;

        var registry = new CanvasActionRegistry();
        services.AddSingleton(registry);
        services.AddScoped<ICanvasActionService>(sp => new CanvasActionService(registry, sp));
        return registry;
    }
```

Also relax `ValidateName` so it serves both registrars — it already does; leave it as is.

`ICanvasActionService` / `CanvasActionService` do not exist yet, so this step will not compile on
its own. That is expected — Task 3 adds them, and the two tasks build together. To keep Task 2
independently verifiable, add the service file **now** as part of this step (see Task 3 Step 3 for
its full body) or, simpler, order the work as written and run the build at the end of Task 3.
**Chosen order: implement `CanvasActionService.cs` in Task 3 and run Task 2's tests at the end of
Task 3 Step 4.** Do not commit a non-building tree.

- [ ] **Step 5: Continue directly into Task 3 (no commit yet)**

The registry and the service are one compile unit; they are committed together at the end of Task 3.

---

### Task 3: `ICanvasActionService` + prompt block

**Files:**
- Create: `DRYL.Components/Canvas/CanvasActionService.cs`, `DRYL.Components/Canvas/CanvasActionPrompt.cs`
- Test: `tests/DRYL.Components.Tests/Canvas/CanvasActionPromptTests.cs`

**Interfaces:**
- Consumes: `CanvasActionRegistry`, `CanvasActionSource`, `CanvasActionContext`, `CanvasActionResult` (Task 2).
- Produces:

```csharp
namespace DRYL.Components.Canvas;

public interface ICanvasActionService
{
    IReadOnlyList<CanvasActionDescriptor> Descriptors { get; }
    Task<CanvasActionResult> InvokeAsync(
        string name, JsonElement? args, string nodeId,
        IReadOnlyDictionary<string, object?> values, CancellationToken ct);
}

public static class CanvasActionPrompt
{
    public static string Block(IReadOnlyList<CanvasActionDescriptor>? descriptors);  // "" when empty
}
```

- [ ] **Step 1: Write the failing tests**

Create `tests/DRYL.Components.Tests/Canvas/CanvasActionPromptTests.cs`:

```csharp
using DRYL.Components;
using DRYL.Components.Canvas;
using Microsoft.Extensions.DependencyInjection;

namespace DRYL.Components.Tests.Canvas;

public class CanvasActionPromptTests
{
    private static ServiceProvider Provider(Action<IServiceCollection> configure)
    {
        var services = new ServiceCollection();
        configure(services);
        return services.BuildServiceProvider();
    }

    [Fact]
    public void Block_is_empty_without_registered_actions()
    {
        Assert.Equal(string.Empty, CanvasActionPrompt.Block(null));
        Assert.Equal(string.Empty, CanvasActionPrompt.Block(Array.Empty<CanvasActionDescriptor>()));
    }

    [Fact]
    public void Block_lists_name_signature_and_description()
    {
        var sp = Provider(s => s
            .AddDrylCanvasAction("order.approve", "Gibt einen Auftrag frei.",
                (ApproveArgs a, CanvasActionContext c, CancellationToken t) =>
                    Task.FromResult(CanvasActionResult.Ok()))
            .AddDrylCanvasAction("cache.clear", "Leert den Cache.",
                (CanvasActionContext c, CancellationToken t) =>
                    Task.FromResult(CanvasActionResult.Ok())));

        var block = CanvasActionPrompt.Block(
            sp.GetRequiredService<ICanvasActionService>().Descriptors);

        Assert.Contains("ACTIONS", block);
        Assert.Contains("order.approve(orderId: string, note?: string)", block);
        Assert.Contains("\"Gibt einen Auftrag frei.\"", block);
        Assert.Contains("cache.clear()", block);
        Assert.Contains("$field", block);
        Assert.Contains("confirm", block);
    }

    // A4 is a property of the architecture, but the model is told about it too — a generated
    // artifact that "helpfully" tries to trigger something has to read this line first.
    [Fact]
    public void Block_tells_the_model_it_never_triggers_an_action()
    {
        var sp = Provider(s => s.AddDrylCanvasAction("a", "…",
            (CanvasActionContext c, CancellationToken t) => Task.FromResult(CanvasActionResult.Ok())));

        var block = CanvasActionPrompt.Block(sp.GetRequiredService<ICanvasActionService>().Descriptors);

        Assert.Contains("NEVER trigger", block);
    }

    [Fact]
    public async Task InvokeAsync_runs_the_handler_with_args_and_the_scope()
    {
        string? seen = null;
        var sp = Provider(s => s.AddDrylCanvasAction("order.approve", "…",
            (ApproveArgs a, CanvasActionContext c, CancellationToken t) =>
            {
                seen = a.OrderId;
                return Task.FromResult(CanvasActionResult.Ok("done"));
            }));

        var args = System.Text.Json.JsonDocument.Parse("""{"orderId":"4711"}""").RootElement.Clone();
        var result = await sp.GetRequiredService<ICanvasActionService>()
            .InvokeAsync("order.approve", args, "btn",
                         new Dictionary<string, object?>(), CancellationToken.None);

        Assert.Equal("4711", seen);
        Assert.True(result.Succeeded);
        Assert.Equal("done", result.Message);
    }

    [Fact]
    public async Task InvokeAsync_on_an_unknown_action_throws_a_named_exception()
    {
        var sp = Provider(s => s.AddDrylCanvasAction("a", "…",
            (CanvasActionContext c, CancellationToken t) => Task.FromResult(CanvasActionResult.Ok())));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sp.GetRequiredService<ICanvasActionService>().InvokeAsync(
                "nope", null, "btn", new Dictionary<string, object?>(), CancellationToken.None));

        Assert.Contains("nope", ex.Message);
    }

    [Fact]
    public async Task The_handler_sees_the_full_form_snapshot()
    {
        string? note = null;
        var sp = Provider(s => s.AddDrylCanvasAction("a", "…",
            (CanvasActionContext c, CancellationToken t) =>
            {
                note = c.Get<string>("note");
                return Task.FromResult(CanvasActionResult.Ok());
            }));

        await sp.GetRequiredService<ICanvasActionService>().InvokeAsync(
            "a", null, "btn",
            new Dictionary<string, object?> { ["note"] = "hi" }, CancellationToken.None);

        Assert.Equal("hi", note);
    }
}
```

- [ ] **Step 2: Run the tests, confirm they fail**

Run: `dotnet test DRYL.slnx --filter FullyQualifiedName~CanvasActionPromptTests`
Expected: FAIL — `ICanvasActionService` / `CanvasActionPrompt` do not exist (compile error).

- [ ] **Step 3: Implement the service and the prompt block**

Create `DRYL.Components/Canvas/CanvasActionService.cs`:

```csharp
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace DRYL.Components.Canvas;

/// <summary>
/// The per-scope view onto the registered canvas actions: what exists, and how to run it.
/// <para>Its only caller is <see cref="CanvasActionRunner"/>, which is only ever reached from a
/// button's click handler. There is deliberately no path from a model output to here (A4).</para>
/// </summary>
public interface ICanvasActionService
{
    /// <summary>Every registered action. Also the material for the model's prompt block.</summary>
    IReadOnlyList<CanvasActionDescriptor> Descriptors { get; }

    /// <summary>Infrastructure — runs a registered action against this scope. A canvas calls it
    /// after a user press; hosts do not.</summary>
    Task<CanvasActionResult> InvokeAsync(
        string name, JsonElement? args, string nodeId,
        IReadOnlyDictionary<string, object?> values, CancellationToken ct);
}

/// <inheritdoc cref="ICanvasActionService" />
internal sealed class CanvasActionService : ICanvasActionService
{
    private readonly CanvasActionRegistry _registry;
    private readonly IServiceProvider _scope;

    private static int _crowdedWarned;

    public CanvasActionService(CanvasActionRegistry registry, IServiceProvider scope)
    {
        _registry = registry;
        _scope = scope;

        if (registry.Descriptors.Count >= CanvasActionPrompt.CrowdedAt &&
            Interlocked.Exchange(ref _crowdedWarned, 1) == 0)
        {
            (scope.GetService(typeof(ILogger<CanvasActionService>)) as ILogger)?.LogWarning(
                "{Count} canvas actions are registered. Their descriptors go into every artifact " +
                "generation — keep each description to one line, or the prompt grows with the catalog.",
                registry.Descriptors.Count);
        }
    }

    public IReadOnlyList<CanvasActionDescriptor> Descriptors => _registry.Descriptors;

    public Task<CanvasActionResult> InvokeAsync(
        string name, JsonElement? args, string nodeId,
        IReadOnlyDictionary<string, object?> values, CancellationToken ct)
    {
        if (!_registry.TryGet(name, out var action))
            throw new InvalidOperationException($"No canvas action named '{name}' is registered.");

        return action.Invoke(args, new CanvasActionContext(_scope, nodeId, values), ct);
    }
}
```

Create `DRYL.Components/Canvas/CanvasActionPrompt.cs`:

```csharp
using System.Text;

namespace DRYL.Components.Canvas;

/// <summary>
/// Turns the registered actions into the block an artifact generator sees. The model learns which
/// buttons it may offer — and that pressing them is not its job.
/// </summary>
public static class CanvasActionPrompt
{
    /// <summary>Past this many actions the block starts to dominate every generation; the real
    /// answer is catalog compression (phase 4), so until then this is a warning, not a limit.</summary>
    internal const int CrowdedAt = 40;

    /// <summary>
    /// The block for <paramref name="descriptors"/>, or an empty string when nothing is registered —
    /// in which case the generator's contract stays exactly as it was and existing chat artifacts
    /// keep using plain <c>intent</c> buttons.
    /// </summary>
    public static string Block(IReadOnlyList<CanvasActionDescriptor>? descriptors)
    {
        if (descriptors is null || descriptors.Count == 0) return string.Empty;

        var sb = new StringBuilder();
        sb.Append("\nACTIONS — wire buttons to these instead of inventing an intent:\n");
        foreach (var d in descriptors)
        {
            sb.Append("- ").Append(d.Name).Append('(').Append(Signature(d)).Append(')');
            if (!string.IsNullOrWhiteSpace(d.Description))
                sb.Append(" — \"").Append(d.Description).Append('"');
            sb.Append('\n');
        }
        sb.Append(
            """
            Wire like this: "action": { "name": "<name>", "args": { … }, "confirm": "<question>"? }
            An arg is a literal, or { "$field": "<name of an interactive node in this artifact>" } —
            the same reference syntax as a data param.
            "action" goes on a button node, next to "props" — never inside them.
            A button with an "action" may omit "intent".
            Add "confirm" to anything destructive or irreversible, and set "kind": "danger" on the button.
            You place the button and label it. You NEVER trigger an action — only the user presses it.

            """);
        return sb.ToString();
    }

    private static string Signature(CanvasActionDescriptor d) =>
        string.Join(", ", d.Args.Select(a => $"{a.Name}{(a.Required ? "" : "?")}: {a.TypeName}"));
}
```

- [ ] **Step 4: Run the tests, confirm green**

Run: `dotnet test DRYL.slnx --filter FullyQualifiedName~CanvasAction`
Expected: PASS — both `CanvasActionRegistryTests` and `CanvasActionPromptTests`.

- [ ] **Step 5: Commit**

```bash
git add DRYL.Components/Canvas/CanvasAction.cs DRYL.Components/Canvas/CanvasActionRegistry.cs DRYL.Components/Canvas/CanvasActionService.cs DRYL.Components/Canvas/CanvasActionPrompt.cs DRYL.Components/Extensions/CanvasServiceCollectionExtensions.cs tests/DRYL.Components.Tests/Canvas/CanvasActionRegistryTests.cs tests/DRYL.Components.Tests/Canvas/CanvasActionPromptTests.cs
git commit -m "feat(canvas): typed host actions, registry and model-facing prompt block

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>"
```

---

### Task 4: The runner

**Files:**
- Create: `DRYL.Components/Canvas/CanvasActionRunner.cs`
- Modify: `DRYL.Components/Canvas/CanvasSpec.cs`, `DRYL.Components/Canvas/CanvasInteraction.cs`, `DRYL.Components/Canvas/CanvasContext.cs`
- Test: `tests/DRYL.Components.Tests/Canvas/CanvasActionRunnerTests.cs`

**Interfaces:**
- Consumes: `ICanvasActionService` (Task 3), `ICanvasDataService.Invalidate` (existing), `CanvasArgs.Resolve` (Task 1), `CanvasFormState` (existing), `CanvasPatcher.Apply` (existing), `CanvasPulseTracker.Stamp` (existing), `IDrylDialogService.ShowConfirmAsync` (existing), `IDrylToastService.ShowSuccess` (existing).
- Produces:

```csharp
namespace DRYL.Components.Canvas;

public sealed class CanvasActionState
{
    public bool Busy { get; }
    public string? Error { get; }
}

public sealed class CanvasActionRunner
{
    public CanvasActionRunner(ICanvasActionService actions, ICanvasDataService? data,
                              CanvasFormState form, IServiceProvider services, ILogger? log = null);

    /// The op applier, set by DrylCanvas: takes an op, returns null on success or a skip reason.
    public Func<CanvasOp, string?>? Patch { get; set; }

    /// Raised for an AskAi result — DrylCanvas forwards it to OnInteraction.
    public Func<CanvasInteraction, Task>? Ask { get; set; }

    /// Raised after every completed action — DrylCanvas forwards it to OnAction.
    public Func<CanvasActionOutcome, Task>? Completed { get; set; }

    public CanvasActionState? StateOf(string nodeId);
    public Task InvokeAsync(string nodeId, string? label, CanvasActionBinding action);
    public event Action? OnChanged;
}
```

- Also produced (modifications):

```csharp
// CanvasNode gains:
public CanvasActionBinding? Action { get; set; }

// CanvasInteraction gains:
public string? Message { get; init; }
// ToPromptMessage() returns Message verbatim when it is set.

// CanvasContext gains:
public CanvasActionRunner? Actions { get; internal set; }
internal Func<CanvasOp, string?>? Patch { get; set; }
```

- [ ] **Step 1: Write the failing tests**

Create `tests/DRYL.Components.Tests/Canvas/CanvasActionRunnerTests.cs`:

```csharp
using System.Text.Json;
using DRYL.Components;
using DRYL.Components.Canvas;
using DRYL.Components.Dialogs;
using DRYL.Components.Toasts;
using Microsoft.Extensions.DependencyInjection;

namespace DRYL.Components.Tests.Canvas;

public class CanvasActionRunnerTests
{
    private static JsonElement Json(string raw) => JsonDocument.Parse(raw).RootElement.Clone();

    private static (CanvasActionRunner Runner, CanvasFormState Form, ServiceProvider Sp) Build(
        Action<IServiceCollection> configure, bool withDialogs = true, bool withToasts = true)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        if (withDialogs) services.AddScoped<IDrylDialogService, StubDialogService>();
        if (withToasts) services.AddScoped<IDrylToastService, StubToastService>();
        configure(services);
        var sp = services.BuildServiceProvider();

        var form = new CanvasFormState();
        var runner = new CanvasActionRunner(
            sp.GetRequiredService<ICanvasActionService>(),
            sp.GetService<ICanvasDataService>(),
            form, sp);
        return (runner, form, sp);
    }

    [Fact]
    public async Task A_successful_action_shows_a_toast_and_no_inline_error()
    {
        var (runner, _, sp) = Build(s => s.AddDrylCanvasAction("a", "…",
            (CanvasActionContext c, CancellationToken t) =>
                Task.FromResult(CanvasActionResult.Ok("Auftrag freigegeben"))));

        await runner.InvokeAsync("btn", "Freigeben", new CanvasActionBinding { Name = "a" });

        var toasts = (StubToastService)sp.GetRequiredService<IDrylToastService>();
        Assert.Equal(new[] { "Auftrag freigegeben" }, toasts.Successes);
        Assert.Null(runner.StateOf("btn")!.Error);
        Assert.False(runner.StateOf("btn")!.Busy);
    }

    [Fact]
    public async Task A_failed_action_sets_the_inline_error_and_shows_no_toast()
    {
        var (runner, _, sp) = Build(s => s.AddDrylCanvasAction("a", "…",
            (CanvasActionContext c, CancellationToken t) =>
                Task.FromResult(CanvasActionResult.Fail("Bereits freigegeben."))));

        await runner.InvokeAsync("btn", "Freigeben", new CanvasActionBinding { Name = "a" });

        Assert.Equal("Bereits freigegeben.", runner.StateOf("btn")!.Error);
        Assert.Empty(((StubToastService)sp.GetRequiredService<IDrylToastService>()).Successes);
    }

    [Fact]
    public async Task A_throwing_handler_becomes_an_inline_error_and_never_escapes()
    {
        var (runner, _, _) = Build(s => s.AddDrylCanvasAction("a", "…",
            (CanvasActionContext c, CancellationToken t) =>
                Task.FromResult<CanvasActionResult>(throw new InvalidOperationException("boom"))));

        await runner.InvokeAsync("btn", "Freigeben", new CanvasActionBinding { Name = "a" });

        Assert.Contains("'a'", runner.StateOf("btn")!.Error);
        Assert.DoesNotContain("boom", runner.StateOf("btn")!.Error);   // no leaking internals
    }

    [Fact]
    public async Task An_unknown_action_becomes_an_inline_error()
    {
        var (runner, _, _) = Build(s => s.AddDrylCanvasAction("a", "…",
            (CanvasActionContext c, CancellationToken t) => Task.FromResult(CanvasActionResult.Ok())));

        await runner.InvokeAsync("btn", "X", new CanvasActionBinding { Name = "nope" });

        Assert.Contains("nope", runner.StateOf("btn")!.Error);
    }

    [Fact]
    public async Task Args_resolve_literals_and_field_references()
    {
        string? seenId = null; string? seenNote = null;
        var (runner, form, _) = Build(s => s.AddDrylCanvasAction("order.approve", "…",
            (ApproveArgs a, CanvasActionContext c, CancellationToken t) =>
            {
                seenId = a.OrderId; seenNote = a.Note;
                return Task.FromResult(CanvasActionResult.Ok());
            }));
        form.Set("order", "4711");

        await runner.InvokeAsync("btn", "Freigeben", new CanvasActionBinding
        {
            Name = "order.approve",
            Args = Json("""{"orderId":{"$field":"order"},"note":"aus dem Dashboard"}"""),
        });

        Assert.Equal("4711", seenId);
        Assert.Equal("aus dem Dashboard", seenNote);
    }

    [Fact]
    public async Task Refresh_invalidates_the_named_sources()
    {
        var seen = new List<CanvasInvalidation>();
        var (runner, _, sp) = Build(s =>
        {
            s.AddDrylCanvasDataSource("orders.open", "…",
                (CanvasDataContext c, CancellationToken t) =>
                    Task.FromResult(CanvasData.Rows(new[] { "a" }, Array.Empty<string[]>())));
            s.AddDrylCanvasAction("a", "…",
                (CanvasActionContext c, CancellationToken t) =>
                    Task.FromResult(CanvasActionResult.Ok().Refresh("orders.open")));
        });
        sp.GetRequiredService<ICanvasDataService>().Invalidated += n => seen.Add(n);

        await runner.InvokeAsync("btn", "X", new CanvasActionBinding { Name = "a" });

        Assert.Equal("orders.open", Assert.Single(seen).Source);
    }

    [Fact]
    public async Task Patch_ops_run_through_the_supplied_applier()
    {
        var applied = new List<CanvasOp>();
        var op = new CanvasOp { Op = "setProps", Id = "badge", Props = Json("""{"kind":"success"}""") };
        var (runner, _, _) = Build(s => s.AddDrylCanvasAction("a", "…",
            (CanvasActionContext c, CancellationToken t) =>
                Task.FromResult(CanvasActionResult.Ok().Patch(op))));
        runner.Patch = o => { applied.Add(o); return null; };

        await runner.InvokeAsync("btn", "X", new CanvasActionBinding { Name = "a" });

        Assert.Same(op, Assert.Single(applied));
    }

    [Fact]
    public async Task AskAi_raises_an_interaction_whose_prompt_message_is_verbatim()
    {
        CanvasInteraction? raised = null;
        var (runner, _, _) = Build(s => s.AddDrylCanvasAction("a", "…",
            (CanvasActionContext c, CancellationToken t) =>
                Task.FromResult(CanvasActionResult.Ok().AskAi("Auftrag 4711 wurde freigegeben."))));
        runner.Ask = i => { raised = i; return Task.CompletedTask; };

        await runner.InvokeAsync("btn", "X", new CanvasActionBinding { Name = "a" });

        Assert.Equal("Auftrag 4711 wurde freigegeben.", raised!.ToPromptMessage());
        Assert.Equal("a", raised.Intent);
        Assert.Equal("btn", raised.NodeId);
    }

    [Fact]
    public async Task Without_AskAi_no_interaction_is_raised()
    {
        var raised = 0;
        var (runner, _, _) = Build(s => s.AddDrylCanvasAction("a", "…",
            (CanvasActionContext c, CancellationToken t) => Task.FromResult(CanvasActionResult.Ok("ok"))));
        runner.Ask = _ => { raised++; return Task.CompletedTask; };

        await runner.InvokeAsync("btn", "X", new CanvasActionBinding { Name = "a" });

        Assert.Equal(0, raised);
    }

    [Fact]
    public async Task Completed_fires_on_success_and_on_failure()
    {
        var outcomes = new List<CanvasActionOutcome>();
        var (runner, _, _) = Build(s => s
            .AddDrylCanvasAction("ok", "…",
                (CanvasActionContext c, CancellationToken t) => Task.FromResult(CanvasActionResult.Ok("y")))
            .AddDrylCanvasAction("no", "…",
                (CanvasActionContext c, CancellationToken t) => Task.FromResult(CanvasActionResult.Fail("n"))));
        runner.Completed = o => { outcomes.Add(o); return Task.CompletedTask; };

        await runner.InvokeAsync("b1", "X", new CanvasActionBinding { Name = "ok" });
        await runner.InvokeAsync("b2", "X", new CanvasActionBinding { Name = "no" });

        Assert.Collection(outcomes,
            o => { Assert.True(o.Succeeded); Assert.Equal("b1", o.NodeId); },
            o => { Assert.False(o.Succeeded); Assert.Equal("n", o.Message); });
    }

    [Fact]
    public async Task A_second_click_while_the_action_runs_is_discarded()
    {
        var calls = 0;
        var gate = new TaskCompletionSource();
        var (runner, _, _) = Build(s => s.AddDrylCanvasAction("a", "…",
            async (CanvasActionContext c, CancellationToken t) =>
            {
                calls++;
                await gate.Task;
                return CanvasActionResult.Ok();
            }));

        var first = runner.InvokeAsync("btn", "X", new CanvasActionBinding { Name = "a" });
        await runner.InvokeAsync("btn", "X", new CanvasActionBinding { Name = "a" });
        gate.SetResult();
        await first;

        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task A_declined_confirmation_does_not_run_the_handler()
    {
        var calls = 0;
        var (runner, _, sp) = Build(s => s.AddDrylCanvasAction("a", "…",
            (CanvasActionContext c, CancellationToken t) => { calls++; return Task.FromResult(CanvasActionResult.Ok()); }));
        ((StubDialogService)sp.GetRequiredService<IDrylDialogService>()).Confirm = false;

        await runner.InvokeAsync("btn", "Freigeben",
            new CanvasActionBinding { Name = "a", Confirm = "Wirklich?" });

        Assert.Equal(0, calls);
        Assert.Null(runner.StateOf("btn")?.Error);     // a cancellation is not a failure
    }

    [Fact]
    public async Task An_accepted_confirmation_runs_the_handler()
    {
        var calls = 0;
        var (runner, _, _) = Build(s => s.AddDrylCanvasAction("a", "…",
            (CanvasActionContext c, CancellationToken t) => { calls++; return Task.FromResult(CanvasActionResult.Ok()); }));

        await runner.InvokeAsync("btn", "Freigeben",
            new CanvasActionBinding { Name = "a", Confirm = "Wirklich?" });

        Assert.Equal(1, calls);
    }

    // E7: a deliberately confirmation-gated action must never run unconfirmed.
    [Fact]
    public async Task Without_a_dialog_service_a_confirm_action_refuses_to_run()
    {
        var calls = 0;
        var (runner, _, _) = Build(s => s.AddDrylCanvasAction("a", "…",
            (CanvasActionContext c, CancellationToken t) => { calls++; return Task.FromResult(CanvasActionResult.Ok()); }),
            withDialogs: false);

        await runner.InvokeAsync("btn", "Freigeben",
            new CanvasActionBinding { Name = "a", Confirm = "Wirklich?" });

        Assert.Equal(0, calls);
        Assert.Contains("Confirmation", runner.StateOf("btn")!.Error);
    }

    [Fact]
    public async Task A_missing_toast_service_is_not_an_error()
    {
        var (runner, _, _) = Build(s => s.AddDrylCanvasAction("a", "…",
            (CanvasActionContext c, CancellationToken t) => Task.FromResult(CanvasActionResult.Ok("done"))),
            withToasts: false);

        await runner.InvokeAsync("btn", "X", new CanvasActionBinding { Name = "a" });

        Assert.Null(runner.StateOf("btn")!.Error);
    }

    [Fact]
    public async Task A_retry_clears_the_previous_error()
    {
        var fail = true;
        var (runner, _, _) = Build(s => s.AddDrylCanvasAction("a", "…",
            (CanvasActionContext c, CancellationToken t) =>
                Task.FromResult(fail ? CanvasActionResult.Fail("nope") : CanvasActionResult.Ok())));

        await runner.InvokeAsync("btn", "X", new CanvasActionBinding { Name = "a" });
        Assert.NotNull(runner.StateOf("btn")!.Error);

        fail = false;
        await runner.InvokeAsync("btn", "X", new CanvasActionBinding { Name = "a" });
        Assert.Null(runner.StateOf("btn")!.Error);
    }
}
```

Add the two stubs to the same file (bottom):

```csharp
internal sealed class StubDialogService : IDrylDialogService
{
    public bool Confirm { get; set; } = true;

    public Task<IDrylDialogReference> ShowAsync<TDialog>(string? title = null,
        DialogParameters? parameters = null, DialogOptions? options = null)
        where TDialog : Microsoft.AspNetCore.Components.IComponent =>
        throw new NotSupportedException();

    public Task<DialogResult> ShowConfirmAsync(string title, string message,
        string confirmLabel = "Confirm", string cancelLabel = "Cancel", DialogOptions? options = null) =>
        Task.FromResult(Confirm ? DialogResult.Ok() : DialogResult.Cancel());

    public Task<DialogResult> ShowAlertAsync(string title, string message,
        string okLabel = "OK", DialogOptions? options = null) => Task.FromResult(DialogResult.Ok());

    public event Action<IDrylDialogReference>? OnDialogInstanceAdded { add { } remove { } }
    public event Action<IDrylDialogReference>? OnDialogCloseRequested { add { } remove { } }
    public event Action<IDrylDialogReference>? OnDialogInstanceUpdated { add { } remove { } }
}

internal sealed class StubToastService : IDrylToastService
{
    public List<string> Successes { get; } = new();
    public List<string> Errors { get; } = new();

    public IDrylToastReference Show(string message, ToastOptions? options = null) =>
        throw new NotSupportedException();
    public IDrylToastReference ShowSuccess(string message, string? title = null, ToastOptions? options = null)
    { Successes.Add(message); return null!; }
    public IDrylToastReference ShowWarning(string message, string? title = null, ToastOptions? options = null) => null!;
    public IDrylToastReference ShowError(string message, string? title = null, ToastOptions? options = null)
    { Errors.Add(message); return null!; }
    public IDrylToastReference ShowInfo(string message, string? title = null, ToastOptions? options = null) => null!;
    public IDrylToastReference Show<TComponent>(ToastParameters? parameters = null, ToastOptions? options = null)
        where TComponent : Microsoft.AspNetCore.Components.IComponent => throw new NotSupportedException();
    public void CloseAll() { }
    public event Action<IDrylToastReference>? OnToastAdded { add { } remove { } }
    public event Action<IDrylToastReference>? OnToastCloseRequested { add { } remove { } }
    public event Action<IDrylToastReference>? OnToastUpdated { add { } remove { } }
}
```

Before writing the stubs, verify the exact member list of `DialogResult`
(`Read DRYL.Components/Dialogs/DialogResult.cs`) and `IDrylToastReference`, and adjust the stubs to
compile against the real interfaces — do not invent members.

- [ ] **Step 2: Run the tests, confirm they fail**

Run: `dotnet test DRYL.slnx --filter FullyQualifiedName~CanvasActionRunnerTests`
Expected: FAIL — `CanvasActionRunner` does not exist (compile error).

- [ ] **Step 3: Extend the three existing types**

In `DRYL.Components/Canvas/CanvasSpec.cs`, add to `CanvasNode` right after `Data`:

```csharp
    /// <summary>
    /// Optional binding to a registered host action (see <c>AddDrylCanvasAction</c>). Only valid
    /// on a <c>button</c>. The AI authors and labels the button; only a user press runs the
    /// handler (A4). A node without a binding behaves exactly as it always has.
    /// </summary>
    public CanvasActionBinding? Action { get; set; }
```

In `DRYL.Components/Canvas/CanvasInteraction.cs`, replace the record body:

```csharp
public sealed record CanvasInteraction(
    string Intent, string NodeId, IReadOnlyDictionary<string, object?> Values)
{
    /// <summary>
    /// A ready-made chat turn that replaces the generated one. Set by an action's
    /// <c>CanvasActionResult.AskAi(…)</c>; <c>null</c> for a plain intent click.
    /// <para>Because <see cref="ToPromptMessage"/> returns it verbatim, a host that already
    /// wires <c>OnInteraction="i => _chat.Send(i.ToPromptMessage())"</c> gets AskAi for free.</para>
    /// </summary>
    public string? Message { get; init; }

    /// <summary>Structured chat message describing this interaction — send it as the next
    /// user turn so the assistant can react (typically with update_artifact).</summary>
    public string ToPromptMessage() =>
        Message ??
        "The user interacted with the artifact. intent: \"" + Intent + "\", values: "
        + JsonSerializer.Serialize(Values, CanvasJson.Options)
        + ". React accordingly; update the artifact via update_artifact if appropriate.";
}
```

In `DRYL.Components/Canvas/CanvasContext.cs`, add two members:

```csharp
    /// <summary>Runs this artifact's action bindings; <c>null</c> when no actions are
    /// registered (in which case a button falls back to its <c>intent</c>).</summary>
    public CanvasActionRunner? Actions { get; internal set; }

    /// <summary>Applies a patch op to the canvas's spec. Returns null on success, otherwise a
    /// skip reason. Set by <c>DrylCanvas</c>, which owns the spec.</summary>
    internal Func<CanvasOp, string?>? Patch { get; set; }
```

- [ ] **Step 4: Implement the runner**

Create `DRYL.Components/Canvas/CanvasActionRunner.cs`:

```csharp
using DRYL.Components.Dialogs;
using DRYL.Components.Toasts;
using Microsoft.Extensions.Logging;

namespace DRYL.Components.Canvas;

/// <summary>What one action button currently has to show.</summary>
public sealed class CanvasActionState
{
    internal CanvasActionState(bool busy, string? error)
    {
        Busy = busy;
        Error = error;
    }

    /// <summary>The handler is running — the button is in its loading beat.</summary>
    public bool Busy { get; }

    /// <summary>The last failure, shown inline under the button. It stays until the next
    /// attempt: a failure asks the user to do something, so it must not expire on its own.</summary>
    public string? Error { get; }
}

/// <summary>
/// Runs one canvas's action bindings. Owned by a <c>DrylCanvas</c> instance, like
/// <see cref="CanvasDataBinder"/> — two canvases on a page share nothing.
///
/// <para>Its only entry point is <see cref="InvokeAsync"/>, and its only caller is a rendered
/// button's click handler. There is deliberately no path from a model output to here: the AI
/// builds and labels the button, the human presses it (A4).</para>
///
/// <para>A completed action is one movement (A8): patch ops land in one batch and pulse, the
/// named sources reload through the existing binder, a success message is a toast and a failure
/// stays inline at the button.</para>
/// </summary>
public sealed class CanvasActionRunner
{
    private readonly ICanvasActionService _actions;
    private readonly ICanvasDataService? _data;
    private readonly CanvasFormState _form;
    private readonly IServiceProvider _services;
    private readonly ILogger? _log;

    private readonly Dictionary<string, CanvasActionState> _states = new(StringComparer.Ordinal);

    /// <summary>Creates a runner for one canvas.</summary>
    public CanvasActionRunner(ICanvasActionService actions, ICanvasDataService? data,
                              CanvasFormState form, IServiceProvider services, ILogger? log = null)
    {
        _actions = actions ?? throw new ArgumentNullException(nameof(actions));
        _data = data;
        _form = form ?? throw new ArgumentNullException(nameof(form));
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _log = log;
    }

    /// <summary>Applies a patch op from an action result. Set by <c>DrylCanvas</c>, which owns
    /// the spec; returns null on success or a skip reason.</summary>
    public Func<CanvasOp, string?>? Patch { get; set; }

    /// <summary>Invoked for a result that called <c>AskAi</c>. <c>DrylCanvas</c> forwards it to
    /// <c>OnInteraction</c>, so an existing chat wiring picks it up unchanged.</summary>
    public Func<CanvasInteraction, Task>? Ask { get; set; }

    /// <summary>Invoked after every completed action, successful or not.</summary>
    public Func<CanvasActionOutcome, Task>? Completed { get; set; }

    /// <summary>Raised whenever a button's busy/error state changed. The canvas re-renders.</summary>
    public event Action? OnChanged;

    /// <summary>What <paramref name="nodeId"/> should render, or <c>null</c> if it has never run.</summary>
    public CanvasActionState? StateOf(string nodeId) =>
        _states.TryGetValue(nodeId, out var state) ? state : null;

    /// <summary>
    /// Runs the action bound to <paramref name="nodeId"/>. <paramref name="label"/> is the button's
    /// visible label and titles the confirmation dialog. Never throws.
    /// </summary>
    public async Task InvokeAsync(string nodeId, string? label, CanvasActionBinding action)
    {
        if (string.IsNullOrWhiteSpace(action.Name)) return;
        if (StateOf(nodeId)?.Busy == true) return;      // a second press while the first runs

        var name = action.Name!;
        var args = CanvasArgs.Resolve(action.Args, _form, out _);
        var values = _form.Snapshot();

        if (!string.IsNullOrWhiteSpace(action.Confirm))
        {
            var decision = await ConfirmAsync(nodeId, label, action.Confirm!).ConfigureAwait(false);
            if (decision is not true) return;           // declined, or refused for lack of a dialog
        }

        Set(nodeId, new CanvasActionState(busy: true, error: null));

        CanvasActionResult result;
        try
        {
            result = await _actions.InvokeAsync(name, args, nodeId, values, CancellationToken.None)
                                   .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // A handler that throws must never reach the renderer, let alone the circuit — and
            // the user gets the action's name, not the exception's innards.
            _log?.LogError(ex, "Canvas action '{Action}' failed.", name);
            Finish(nodeId, name, CanvasActionResult.Fail($"Action '{name}' failed."));
            await NotifyAsync(nodeId, name, false, $"Action '{name}' failed.").ConfigureAwait(false);
            return;
        }

        if (result.Succeeded) ApplyResult(result);
        Finish(nodeId, name, result);

        if (result.Succeeded && result.Ask is { } ask && Ask is not null)
        {
            await Ask(new CanvasInteraction(name, nodeId, values) { Message = ask }).ConfigureAwait(false);
        }

        await NotifyAsync(nodeId, name, result.Succeeded, result.Message).ConfigureAwait(false);
    }

    // Ops first (the instant visual), then the data catch-up, then the toast: the user sees the
    // artifact move before being told that it moved.
    private void ApplyResult(CanvasActionResult result)
    {
        if (Patch is { } patch)
        {
            foreach (var op in result.Ops)
                if (patch(op) is { } reason)
                    _log?.LogWarning("Canvas action patch op skipped: {Reason}", reason);
        }

        if (_data is not null)
        {
            foreach (var r in result.Refreshes)
                Invalidate(r);
        }

        if (result.Message is { Length: > 0 } message)
            Toasts?.ShowSuccess(message);
    }

    private void Invalidate(CanvasInvalidation notice)
    {
        // ICanvasDataService only exposes the object overload; the canonical key already computed
        // by CanvasActionResult.Refresh(source, parameters) travels through the raw notice, so the
        // service's internal event is raised directly for the parameterised case.
        if (notice.ParamsKey is null) _data!.Invalidate(notice.Source);
        else _data!.Invalidate(notice);
    }

    private void Finish(string nodeId, string name, CanvasActionResult result) =>
        Set(nodeId, new CanvasActionState(busy: false, error: result.Succeeded ? null : result.Message));

    private async Task NotifyAsync(string nodeId, string name, bool ok, string? message)
    {
        if (Completed is { } completed)
            await completed(new CanvasActionOutcome(name, nodeId, ok, message)).ConfigureAwait(false);
    }

    // null = refused (no dialog service), false = declined, true = go ahead.
    private async Task<bool?> ConfirmAsync(string nodeId, string? label, string question)
    {
        if (Dialogs is not { } dialogs)
        {
            // An action the author deliberately gated behind a confirmation must never run
            // unconfirmed just because the host forgot the provider (E7).
            _log?.LogError("A canvas action requires confirmation but no IDrylDialogService is available.");
            Set(nodeId, new CanvasActionState(false,
                "Confirmation is unavailable — the action was not run."));
            return null;
        }

        var title = string.IsNullOrWhiteSpace(label) ? "Confirm" : label!;
        var result = await dialogs.ShowConfirmAsync(title, question, confirmLabel: title)
                                  .ConfigureAwait(false);
        return !result.Canceled;
    }

    private void Set(string nodeId, CanvasActionState state)
    {
        _states[nodeId] = state;
        OnChanged?.Invoke();
    }

    private IDrylDialogService? Dialogs =>
        _services.GetService(typeof(IDrylDialogService)) as IDrylDialogService;

    private IDrylToastService? Toasts =>
        _services.GetService(typeof(IDrylToastService)) as IDrylToastService;
}
```

The parameterised invalidation needs one additive member on the data service. In
`DRYL.Components/Canvas/CanvasDataService.cs`, add to the interface and the implementation:

```csharp
    /// <summary>Infrastructure — republishes a ready-made notice (an action result's refresh list).</summary>
    void Invalidate(CanvasInvalidation notice);
```

```csharp
    public void Invalidate(CanvasInvalidation notice) => Invalidated?.Invoke(notice);
```

and re-express the two existing overloads through it:

```csharp
    public void Invalidate(string source) => Invalidate(new CanvasInvalidation(source, null));

    public void Invalidate(string source, object parameters) =>
        Invalidate(new CanvasInvalidation(
            source, CanvasDataKey.Canonicalize(JsonSerializer.SerializeToElement(parameters, CanvasJson.Options))));
```

- [ ] **Step 5: Run the tests, confirm green**

Run: `dotnet test DRYL.slnx --filter FullyQualifiedName~Canvas`
Expected: PASS — the new runner tests plus every Phase 1 test.

- [ ] **Step 6: Commit**

```bash
git add DRYL.Components/Canvas/ tests/DRYL.Components.Tests/Canvas/CanvasActionRunnerTests.cs
git commit -m "feat(canvas): the action runner — confirm, invoke, patch, refresh, toast

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>"
```

---

### Task 5: Validation

**Files:**
- Modify: `DRYL.Components/Canvas/CanvasCatalog.cs`
- Test: `tests/DRYL.Components.Tests/Canvas/CanvasActionValidationTests.cs`

**Interfaces:**
- Consumes: `CanvasActionDescriptor` (Task 2), `CanvasActionBinding` (Task 4), `CanvasArgs.FieldReference` (Task 1).
- Produces:

```csharp
// CanvasValidationContext gains:
public IReadOnlyList<CanvasActionDescriptor> Actions { get; init; } = Array.Empty<CanvasActionDescriptor>();
```

- [ ] **Step 1: Write the failing tests**

Create `tests/DRYL.Components.Tests/Canvas/CanvasActionValidationTests.cs`:

```csharp
using System.Text.Json;
using DRYL.Components.Canvas;

namespace DRYL.Components.Tests.Canvas;

public class CanvasActionValidationTests
{
    private static JsonElement Json(string raw) => JsonDocument.Parse(raw).RootElement.Clone();

    private static CanvasValidationContext Context(params string[] fields) => new()
    {
        Actions = new[]
        {
            new CanvasActionDescriptor("order.approve", "Gibt einen Auftrag frei.", new[]
            {
                new CanvasParamInfo("orderId", "string", true),
                new CanvasParamInfo("note", "string", false),
            }),
            new CanvasActionDescriptor("cache.clear", "Leert den Cache.", Array.Empty<CanvasParamInfo>()),
        },
        FieldNames = fields,
    };

    private static CanvasNode Button(string? props, string? action) => new()
    {
        Id = "b", Type = "button",
        Props = Json(props ?? """{"label":"Freigeben","intent":"approve"}"""),
        Action = action is null
            ? null
            : JsonSerializer.Deserialize<CanvasActionBinding>(action, CanvasJson.Options),
    };

    [Fact]
    public void A_valid_action_button_passes()
    {
        var node = Button("""{"label":"Freigeben","kind":"danger"}""",
                          """{"name":"order.approve","args":{"orderId":"4711"},"confirm":"Wirklich?"}""");

        Assert.Null(CanvasCatalog.Validate(node, Context()));
    }

    [Fact]
    public void An_unknown_action_names_the_available_ones()
    {
        var node = Button(null, """{"name":"order.nope","args":{}}""");

        var error = CanvasCatalog.Validate(node, Context());

        Assert.Contains("unknown action 'order.nope'", error);
        Assert.Contains("order.approve", error);
    }

    [Fact]
    public void A_missing_required_arg_is_reported_with_the_signature()
    {
        var node = Button(null, """{"name":"order.approve","args":{"note":"x"}}""");

        var error = CanvasCatalog.Validate(node, Context());

        Assert.Contains("missing required arg", error);
        Assert.Contains("orderId", error);
    }

    [Fact]
    public void An_unknown_arg_is_reported()
    {
        var node = Button(null, """{"name":"order.approve","args":{"orderId":"1","nope":2}}""");

        Assert.Contains("no argument 'nope'", CanvasCatalog.Validate(node, Context()));
    }

    [Fact]
    public void A_field_reference_must_point_at_an_interactive_node_of_this_artifact()
    {
        var node = Button(null, """{"name":"order.approve","args":{"orderId":{"$field":"order"}}}""");

        Assert.Null(CanvasCatalog.Validate(node, Context("order")));
        Assert.Contains("references field 'order'", CanvasCatalog.Validate(node, Context("other")));
    }

    [Fact]
    public void An_action_on_a_non_button_is_rejected()
    {
        var node = new CanvasNode
        {
            Id = "s", Type = "stat",
            Props = Json("""{"label":"Umsatz","value":"10k"}"""),
            Action = JsonSerializer.Deserialize<CanvasActionBinding>(
                """{"name":"cache.clear"}""", CanvasJson.Options),
        };

        Assert.Contains("only a button", CanvasCatalog.Validate(node, Context()));
    }

    [Fact]
    public void An_empty_confirm_is_rejected()
    {
        var node = Button(null, """{"name":"cache.clear","confirm":"  "}""");

        Assert.Contains("confirm", CanvasCatalog.Validate(node, Context()));
    }

    [Fact]
    public void A_button_needs_an_intent_or_an_action()
    {
        var bare = Button("""{"label":"Freigeben"}""", null);

        Assert.Contains("intent or an action", CanvasCatalog.Validate(bare, Context()));
        Assert.Null(CanvasCatalog.Validate(
            Button("""{"label":"Freigeben"}""", """{"name":"cache.clear"}"""), Context()));
    }

    [Fact]
    public void Kind_danger_is_accepted()
    {
        Assert.Null(CanvasCatalog.Validate(
            Button("""{"label":"Löschen","intent":"delete","kind":"danger"}""", null)));
    }

    // Without a context nothing about actions is checked — a plain intent button is unchanged.
    [Fact]
    public void Without_a_context_the_old_behaviour_is_preserved()
    {
        Assert.Null(CanvasCatalog.Validate(Button(null, null)));
        Assert.Null(CanvasCatalog.Validate(Button(null, """{"name":"whatever"}""")));
    }
}
```

- [ ] **Step 2: Run the tests, confirm they fail**

Run: `dotnet test DRYL.slnx --filter FullyQualifiedName~CanvasActionValidationTests`
Expected: FAIL — `CanvasValidationContext.Actions` does not exist; the new rules are not implemented.

- [ ] **Step 3: Restructure `Validate` and add the action rules**

In `DRYL.Components/Canvas/CanvasCatalog.cs`:

Replace the body of the context-aware `Validate` — it currently bails out whenever there is no
`data` binding, which would skip action checks entirely:

```csharp
    public static string? Validate(CanvasNode node, CanvasValidationContext? context)
    {
        if (context is null) return ValidateShape(node);

        if (node.Action is { } action)
        {
            var actionError = ValidateAction(node, action, context);
            if (actionError is not null) return actionError;
        }

        if (node.Data is not { Source: not null }) return ValidateShape(node);

        // The binding comes first: a bound chart legitimately carries no "labels" — that is the
        // whole point — so reporting "labels must contain at least one label" would reject exactly
        // the artifacts data binding exists to enable.
        var bindingError = ValidateBinding(node, context);
        if (bindingError is not null) return bindingError;

        // The data itself only arrives at runtime, so validate the presentation props against a
        // stand-in of the declared shape. Everything the shape does not own is checked for real.
        var descriptor = context.Sources.First(d => d.Name == node.Data.Source);
        var props = CanvasDataMapper.Apply(node.Type, node.Props,
            CanvasDataMapper.Sample(descriptor.Shape), out _, out _);

        return ValidateShape(new CanvasNode
        {
            Id = node.Id, Type = node.Type, Props = props, Children = node.Children, Action = node.Action,
        });
    }
```

Add the action validator next to `ValidateBinding`:

```csharp
    private static string? ValidateAction(CanvasNode node, CanvasActionBinding action,
                                          CanvasValidationContext context)
    {
        // Phase 4 widens this to the form container; today a command has exactly one trigger.
        if (node.Type != "button")
            return Err(node, "an action can only sit on a button — move it to the button that triggers it.");

        if (string.IsNullOrWhiteSpace(action.Name))
            return Err(node, "action.name must name a registered action.");

        var descriptor = context.Actions.FirstOrDefault(a => a.Name == action.Name);
        if (descriptor is null)
        {
            var available = context.Actions.Take(5).Select(a => a.Name).ToList();
            return Err(node, available.Count == 0
                ? $"unknown action '{action.Name}' — no actions are registered."
                : $"unknown action '{action.Name}' — available: {string.Join(", ", available)}"
                  + (context.Actions.Count > available.Count ? ", …" : "") + ".");
        }

        if (action.Confirm is not null && string.IsNullOrWhiteSpace(action.Confirm))
            return Err(node, "action.confirm must be a question for the user, or be omitted entirely.");

        return ValidateActionArgs(node, action, descriptor, context);
    }

    private static string? ValidateActionArgs(CanvasNode node, CanvasActionBinding action,
                                              CanvasActionDescriptor descriptor,
                                              CanvasValidationContext context)
    {
        var given = new HashSet<string>(StringComparer.Ordinal);

        if (action.Args is { } a)
        {
            if (a.ValueKind != JsonValueKind.Object)
                return Err(node, "action.args must be an object.");

            foreach (var prop in a.EnumerateObject())
            {
                given.Add(prop.Name);
                if (descriptor.Args.All(x => x.Name != prop.Name))
                    return Err(node, $"action '{descriptor.Name}' has no argument '{prop.Name}' — it takes "
                                     + ActionSignature(descriptor) + ".");

                if (CanvasDataBinder.FieldReference(prop.Value) is { } field &&
                    !context.FieldNames.Contains(field))
                {
                    return Err(node, $"argument '{prop.Name}' references field '{field}', but this artifact has "
                                     + (context.FieldNames.Count == 0
                                         ? "no interactive nodes."
                                         : "no such interactive node — it has: "
                                           + string.Join(", ", context.FieldNames.Take(5)) + "."));
                }
            }
        }

        var missing = descriptor.Args.Where(x => x.Required && !given.Contains(x.Name))
                                     .Select(x => x.Name).ToList();
        return missing.Count == 0
            ? null
            : Err(node, $"action '{descriptor.Name}' is missing required arg"
                        + (missing.Count == 1 ? " " : "s ") + string.Join(", ", missing)
                        + " — it takes " + ActionSignature(descriptor) + ".");
    }

    private static string ActionSignature(CanvasActionDescriptor d) =>
        d.Args.Count == 0
            ? "no arguments"
            : "(" + string.Join(", ", d.Args.Select(a => $"{a.Name}{(a.Required ? "" : "?")}: {a.TypeName}")) + ")";
```

Update the `button` case in `ValidateShape`:

```csharp
            case "button":
            {
                if (!TryProps<ButtonNodeProps>(node, out var p)) return Err(node, "props are not valid JSON.");
                if (string.IsNullOrWhiteSpace(p!.Label))
                    return Err(node, "label must be non-empty.");
                // An action-bound button carries its meaning in the action, not in an invented
                // intent string; a plain button still needs one to be reachable at all.
                if (string.IsNullOrWhiteSpace(p.Intent) && string.IsNullOrWhiteSpace(node.Action?.Name))
                    return Err(node, "a button needs an intent or an action.");
                if (p.Kind is not (null or "primary" or "secondary" or "danger"))
                    return Err(node, $"kind '{p.Kind}' is invalid — use 'primary', 'secondary' or 'danger'.");
                return null;
            }
```

Add to `CanvasValidationContext`:

```csharp
    /// <summary>The registered actions (see <c>ICanvasActionService.Descriptors</c>).</summary>
    public IReadOnlyList<CanvasActionDescriptor> Actions { get; init; } = Array.Empty<CanvasActionDescriptor>();
```

- [ ] **Step 4: Run the tests, confirm green**

Run: `dotnet test DRYL.slnx --filter FullyQualifiedName~Canvas`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add DRYL.Components/Canvas/CanvasCatalog.cs tests/DRYL.Components.Tests/Canvas/CanvasActionValidationTests.cs
git commit -m "feat(canvas): validate action bindings and allow a danger button

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>"
```

---

### Task 6: Render path — button, busy beat, inline error

**Files:**
- Modify: `DRYL.Components/Canvas/CanvasNodeView.razor`, `DRYL.Components/Components/Ai/DrylCanvas.razor`, `DRYL.Components/Components/Ai/DrylCanvas.razor.css`
- Test: `tests/DRYL.Components.Tests/Canvas/CanvasActionRenderTests.cs`

**Interfaces:**
- Consumes: `CanvasActionRunner`, `CanvasActionState` (Task 4); `CanvasContext.Actions` / `.Patch` (Task 4).
- Produces:

```csharp
// DrylCanvas gains:
[Parameter] public EventCallback<CanvasActionOutcome> OnAction { get; set; }
```

- [ ] **Step 1: Write the failing tests**

Create `tests/DRYL.Components.Tests/Canvas/CanvasActionRenderTests.cs`:

```csharp
using System.Text.Json;
using DRYL.Components;
using DRYL.Components.Canvas;
using DRYL.Components.Dialogs;
using DRYL.Components.Toasts;
using Microsoft.Extensions.DependencyInjection;

namespace DRYL.Components.Tests.Canvas;

public class CanvasActionRenderTests : TestContext
{
    private const string SpecWithAction = """
        {"title":"Aufträge","root":{"id":"r","type":"stack","children":[
          {"id":"btn","type":"button","props":{"label":"Freigeben","kind":"danger"},
           "action":{"name":"order.approve","args":{"orderId":"4711"}}}]}}
        """;

    private const string SpecWithIntent = """
        {"title":"Aufträge","root":{"id":"r","type":"stack","children":[
          {"id":"btn","type":"button","props":{"label":"Mehr","intent":"more"}}]}}
        """;

    private static CanvasSpec Spec(string json) =>
        JsonSerializer.Deserialize<CanvasSpec>(json, CanvasJson.Options)!;

    private void Wire(Action<IServiceCollection>? configure = null)
    {
        Services.AddLogging();
        Services.AddDrylComponents();
        Services.AddScoped<IDrylToastService, StubToastService>();
        configure?.Invoke(Services);
        JSInterop.Mode = BunitJSInteropMode.Loose;
    }

    [Fact]
    public void A_bound_button_runs_the_action_and_shows_the_inline_error_on_failure()
    {
        Wire(s => s.AddDrylCanvasAction("order.approve", "…",
            (ApproveArgs a, CanvasActionContext c, CancellationToken t) =>
                Task.FromResult(CanvasActionResult.Fail("Bereits freigegeben."))));

        var cut = Render<DrylCanvas>(p => p.Add(x => x.Spec, Spec(SpecWithAction)));
        cut.Find("button.btn").Click();

        cut.WaitForAssertion(() =>
            Assert.Contains("Bereits freigegeben.", cut.Find(".canvas-action-error").TextContent));
    }

    [Fact]
    public void A_successful_action_renders_no_inline_error()
    {
        Wire(s => s.AddDrylCanvasAction("order.approve", "…",
            (ApproveArgs a, CanvasActionContext c, CancellationToken t) =>
                Task.FromResult(CanvasActionResult.Ok("Freigegeben"))));

        var cut = Render<DrylCanvas>(p => p.Add(x => x.Spec, Spec(SpecWithAction)));
        cut.Find("button.btn").Click();

        cut.WaitForAssertion(() => Assert.Empty(cut.FindAll(".canvas-action-error")));
    }

    [Fact]
    public void OnAction_fires_with_the_outcome()
    {
        CanvasActionOutcome? outcome = null;
        Wire(s => s.AddDrylCanvasAction("order.approve", "…",
            (ApproveArgs a, CanvasActionContext c, CancellationToken t) =>
                Task.FromResult(CanvasActionResult.Ok("Freigegeben"))));

        var cut = Render<DrylCanvas>(p => p
            .Add(x => x.Spec, Spec(SpecWithAction))
            .Add(x => x.OnAction, (CanvasActionOutcome o) => outcome = o));
        cut.Find("button.btn").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.NotNull(outcome);
            Assert.True(outcome!.Succeeded);
            Assert.Equal("order.approve", outcome.Action);
            Assert.Equal("btn", outcome.NodeId);
        });
    }

    [Fact]
    public void Kind_danger_renders_the_danger_variant()
    {
        Wire(s => s.AddDrylCanvasAction("order.approve", "…",
            (ApproveArgs a, CanvasActionContext c, CancellationToken t) =>
                Task.FromResult(CanvasActionResult.Ok())));

        var cut = Render<DrylCanvas>(p => p.Add(x => x.Spec, Spec(SpecWithAction)));

        Assert.Contains("btn-danger", cut.Find("button.btn").GetAttribute("class"));
    }

    [Fact]
    public void A_plain_intent_button_still_raises_OnInteraction()
    {
        CanvasInteraction? raised = null;
        Wire();

        var cut = Render<DrylCanvas>(p => p
            .Add(x => x.Spec, Spec(SpecWithIntent))
            .Add(x => x.OnInteraction, (CanvasInteraction i) => raised = i));
        cut.Find("button.btn").Click();

        cut.WaitForAssertion(() => Assert.Equal("more", raised!.Intent));
    }

    [Fact]
    public void An_action_button_without_a_registry_falls_back_to_its_intent()
    {
        CanvasInteraction? raised = null;
        Wire();   // no AddDrylCanvasAction at all

        var spec = Spec("""
            {"title":"x","root":{"id":"r","type":"stack","children":[
              {"id":"btn","type":"button","props":{"label":"Freigeben","intent":"approve"},
               "action":{"name":"order.approve"}}]}}
            """);
        var cut = Render<DrylCanvas>(p => p
            .Add(x => x.Spec, spec)
            .Add(x => x.OnInteraction, (CanvasInteraction i) => raised = i));
        cut.Find("button.btn").Click();

        cut.WaitForAssertion(() => Assert.Equal("approve", raised!.Intent));
    }

    [Fact]
    public void An_AskAi_result_reaches_OnInteraction_verbatim()
    {
        CanvasInteraction? raised = null;
        Wire(s => s.AddDrylCanvasAction("order.approve", "…",
            (ApproveArgs a, CanvasActionContext c, CancellationToken t) =>
                Task.FromResult(CanvasActionResult.Ok().AskAi("Auftrag 4711 wurde freigegeben."))));

        var cut = Render<DrylCanvas>(p => p
            .Add(x => x.Spec, Spec(SpecWithAction))
            .Add(x => x.OnInteraction, (CanvasInteraction i) => raised = i));
        cut.Find("button.btn").Click();

        cut.WaitForAssertion(() =>
            Assert.Equal("Auftrag 4711 wurde freigegeben.", raised!.ToPromptMessage()));
    }

    [Fact]
    public void A_patch_op_from_an_action_reaches_the_spec()
    {
        Wire(s => s.AddDrylCanvasAction("order.approve", "…",
            (ApproveArgs a, CanvasActionContext c, CancellationToken t) =>
                Task.FromResult(CanvasActionResult.Ok().Patch(new CanvasOp
                {
                    Op = "setProps", Id = "state",
                    Props = JsonDocument.Parse("""{"text":"Freigegeben","kind":"success"}""").RootElement.Clone(),
                }))));

        var spec = Spec("""
            {"title":"x","root":{"id":"r","type":"stack","children":[
              {"id":"state","type":"badge","props":{"text":"Offen","kind":"warning"}},
              {"id":"btn","type":"button","props":{"label":"Freigeben"},
               "action":{"name":"order.approve","args":{"orderId":"4711"}}}]}}
            """);
        var cut = Render<DrylCanvas>(p => p.Add(x => x.Spec, spec));
        cut.Find("button.btn").Click();

        cut.WaitForAssertion(() => Assert.Contains("Freigegeben", cut.Markup));
    }
}
```

Check how the existing canvas bUnit tests set up their `TestContext` base class and JS interop
(`Read tests/DRYL.Components.Tests/Canvas/DrylCanvasStandaloneTests.cs`) and mirror that setup
rather than the sketch above if it differs.

- [ ] **Step 2: Run the tests, confirm they fail**

Run: `dotnet test DRYL.slnx --filter FullyQualifiedName~CanvasActionRenderTests`
Expected: FAIL — no `.canvas-action-error`, no `OnAction`, the button ignores its action.

- [ ] **Step 3: Wire the runner into `DrylCanvas`**

In `DRYL.Components/Components/Ai/DrylCanvas.razor`:

Add the parameter, next to `OnInteraction`:

```csharp
    /// <summary>Raised after every completed action button — successful or not. The canvas has
    /// already applied the result (patch, refresh, toast, inline error) by then; this is for the
    /// host's own logging or reactions.</summary>
    [Parameter] public EventCallback<CanvasActionOutcome> OnAction { get; set; }
```

Add the field:

```csharp
    private CanvasActionRunner? _runner;
```

Extend `OnInitialized`, after the binder block:

```csharp
        _ctx.Patch = op => Spec is null ? $"op '{op.Op}': there is no artifact to patch." : ApplyOp(op);

        // Actions are optional in exactly the same way data sources are: without a registration
        // a button falls back to its intent and nothing about today's behaviour changes.
        var actions = Services.GetService(typeof(ICanvasActionService)) as ICanvasActionService;
        if (actions is { Descriptors.Count: > 0 })
        {
            _runner = new CanvasActionRunner(actions, data, _ctx.Form, Services,
                Services.GetService(typeof(ILogger<CanvasActionRunner>)) as ILogger)
            {
                Patch = op => _ctx.Patch!(op),
                Ask = i => OnInteraction.InvokeAsync(i),
                Completed = o => OnAction.InvokeAsync(o),
            };
            _runner.OnChanged += HandleBinderChanged;
            _ctx.Actions = _runner;
        }
```

Add the op applier — the canvas owns the spec, so this is where a patch may touch it:

```csharp
    // An action result's ops go through the same patcher and the same pulse tracker as an AI
    // setProps — one language for "something changed here", whoever changed it (A8).
    private string? ApplyOp(CanvasOp op)
    {
        var reason = CanvasPatcher.Apply(Spec!, op);
        if (reason is null && op.Op == "setProps" && op.Id is not null) _ctx.Pulse.Stamp(op.Id);
        return reason;
    }
```

Detach in `DisposeAsync`, before the binder block:

```csharp
        if (_runner is not null) _runner.OnChanged -= HandleBinderChanged;
```

Add to `DRYL.Components/Components/Ai/DrylCanvas.razor.css`, right after the existing
`.canvas-data-error` rule (copy its declarations exactly — same tokens, same size, no new values):

```css
/*  An action that failed. Same shape and the same tokens as .canvas-data-error: the user
    should not have to learn two vocabularies for "this bit did not work".               */
.canvas-action-error {
    /* identical declaration list to .canvas-data-error */
}
```

Read the existing `.canvas-data-error` block first and duplicate its declarations verbatim; if the
two end up identical, merge the selectors into one rule (`.canvas-data-error, .canvas-action-error`)
instead of duplicating.

- [ ] **Step 4: Wire the button in `CanvasNodeView`**

In `DRYL.Components/Canvas/CanvasNodeView.razor`, replace the `case "button":` block:

```razor
                case "button":
                {
                    var p = Props<ButtonNodeProps>();
                    var actionState = Ctx.Actions?.StateOf(Node.Id);
                    <DrylButton Variant="MapButtonVariant(p!.Kind)"
                                Loading="@(actionState?.Busy == true)"
                                OnClick="@(_ => TriggerAsync(p))">
                        @p.Label
                    </DrylButton>
                    <DrylPresence Visible="@(actionState?.Error is not null)"
                                  Transition="PresenceTransition.SlideUp" Speed="PresenceSpeed.Fast">
                        <span class="canvas-action-error">
                            <DrylIcon Name="Alert" Size="14" />
                            @actionState?.Error
                        </span>
                    </DrylPresence>
                    break;
                }
```

Replace `RaiseIntent` with the dispatcher:

```csharp
    // Two ways out of a button, and the choice is deliberate. A registered action is a host
    // command — it runs C#, not a chat turn. Everything else keeps the original behaviour, so a
    // host without any registration sees exactly what it saw before (A2's spirit for actions).
    private async Task TriggerAsync(ButtonNodeProps p)
    {
        if (Node.Action is { Name: { Length: > 0 } } action && Ctx.Actions is { } runner)
        {
            await runner.InvokeAsync(Node.Id, p.Label, action);
            return;
        }

        if (!string.IsNullOrWhiteSpace(p.Intent)) await RaiseIntent(p.Intent!);
    }

    private async Task RaiseIntent(string intent) =>
        await Ctx.Intent.InvokeAsync(new CanvasInteraction(intent, Node.Id, Ctx.Form.Snapshot()));
```

Extend `MapButtonVariant`:

```csharp
    private static DrylButton.ButtonVariant MapButtonVariant(string? kind) => kind switch
    {
        "secondary" => DrylButton.ButtonVariant.Secondary,
        "danger" => DrylButton.ButtonVariant.Danger,
        _ => DrylButton.ButtonVariant.Primary,
    };
```

- [ ] **Step 5: Run the tests, confirm green**

Run: `dotnet test DRYL.slnx --filter FullyQualifiedName~Canvas`
Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add DRYL.Components/Canvas/CanvasNodeView.razor DRYL.Components/Components/Ai/DrylCanvas.razor DRYL.Components/Components/Ai/DrylCanvas.razor.css tests/DRYL.Components.Tests/Canvas/CanvasActionRenderTests.cs
git commit -m "feat(canvas): action buttons render busy, failure and the danger variant

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>"
```

---

### Task 7: The AI side — prompt block and receipts

**Files:**
- Modify: `DRYL.Components.Agents/Canvas/CanvasPrompt.cs`, `DRYL.Components.Agents/Canvas/DrylCanvasTools.cs`, `DRYL.Components.Agents/Canvas/DrylAiCanvas.razor`
- Test: `tests/DRYL.Components.Tests/Agents/Canvas/CanvasActionReceiptTests.cs`

**Interfaces:**
- Consumes: `ICanvasActionService`, `CanvasActionPrompt.Block` (Task 3), `CanvasValidationContext.Actions` (Task 5).
- Produces:

```csharp
// DrylCanvasTools — additional optional parameter on both factories:
public static DrylCanvasTools Create(DrylCanvasRun run, AIAgent generator,
    ICanvasDataService? data = null, ICanvasActionService? actions = null);

public static DrylCanvasTools CreateReplay(DrylCanvasRun run,
    Func<string, CancellationToken, IAsyncEnumerable<string>> generate,
    ICanvasDataService? data = null, ICanvasActionService? actions = null);

// DrylAiCanvas — passed straight through:
[Parameter] public EventCallback<CanvasActionOutcome> OnAction { get; set; }
```

- [ ] **Step 1: Write the failing tests**

Create `tests/DRYL.Components.Tests/Agents/Canvas/CanvasActionReceiptTests.cs`. Model it on the
existing `tests/DRYL.Components.Tests/Agents/Canvas/CanvasDataReceiptTests.cs` — read that file
first and reuse its replay-stream helper verbatim rather than inventing a second one.

```csharp
using DRYL.Components;
using DRYL.Components.Agents;
using DRYL.Components.Canvas;
using Microsoft.Extensions.DependencyInjection;

namespace DRYL.Components.Tests.Agents.Canvas;

public class CanvasActionReceiptTests
{
    private static ICanvasActionService Actions()
    {
        var services = new ServiceCollection();
        services.AddDrylCanvasAction("order.approve", "Gibt einen Auftrag frei.",
            (ApproveArgs a, CanvasActionContext c, CancellationToken t) =>
                Task.FromResult(CanvasActionResult.Ok()));
        return services.BuildServiceProvider().GetRequiredService<ICanvasActionService>();
    }

    private static async Task<string> CreateAsync(string specJson)
    {
        var run = new DrylCanvasRun();
        var tools = DrylCanvasTools.CreateReplay(run, (_, _) => Stream(specJson), null, Actions());
        return await InvokeCreateAsync(tools, "brief");
    }

    // Reuse the helpers from CanvasDataReceiptTests (Stream / InvokeCreateAsync); if that file
    // keeps them private, lift them into a shared internal helper class in the same folder.

    [Fact]
    public async Task An_unknown_action_comes_back_as_a_corrective_sentence()
    {
        var receipt = await CreateAsync("""
            {"title":"x","root":{"id":"r","type":"stack","children":[
              {"id":"b","type":"button","props":{"label":"Los"},
               "action":{"name":"order.nope","args":{}}}]}}
            """);

        Assert.Contains("unknown action 'order.nope'", receipt);
        Assert.Contains("order.approve", receipt);
    }

    [Fact]
    public async Task A_missing_required_arg_comes_back_as_a_corrective_sentence()
    {
        var receipt = await CreateAsync("""
            {"title":"x","root":{"id":"r","type":"stack","children":[
              {"id":"b","type":"button","props":{"label":"Los"},
               "action":{"name":"order.approve","args":{}}}]}}
            """);

        Assert.Contains("missing required arg", receipt);
        Assert.Contains("orderId", receipt);
    }

    [Fact]
    public async Task A_dangling_field_reference_comes_back_as_a_corrective_sentence()
    {
        var receipt = await CreateAsync("""
            {"title":"x","root":{"id":"r","type":"stack","children":[
              {"id":"b","type":"button","props":{"label":"Los"},
               "action":{"name":"order.approve","args":{"orderId":{"$field":"nope"}}}}]}}
            """);

        Assert.Contains("references field 'nope'", receipt);
    }

    [Fact]
    public async Task An_action_on_a_non_button_comes_back_as_a_corrective_sentence()
    {
        var receipt = await CreateAsync("""
            {"title":"x","root":{"id":"r","type":"stack","children":[
              {"id":"s","type":"stat","props":{"label":"Umsatz","value":"10k"},
               "action":{"name":"order.approve","args":{"orderId":"1"}}}]}}
            """);

        Assert.Contains("only a button", receipt);
    }

    [Fact]
    public async Task A_button_without_intent_and_without_action_comes_back_as_a_corrective_sentence()
    {
        var receipt = await CreateAsync("""
            {"title":"x","root":{"id":"r","type":"stack","children":[
              {"id":"b","type":"button","props":{"label":"Los"}}]}}
            """);

        Assert.Contains("intent or an action", receipt);
    }

    [Fact]
    public async Task A_valid_action_button_produces_a_clean_receipt()
    {
        var receipt = await CreateAsync("""
            {"title":"x","root":{"id":"r","type":"stack","children":[
              {"id":"b","type":"button","props":{"label":"Freigeben","kind":"danger"},
               "action":{"name":"order.approve","args":{"orderId":"4711"},"confirm":"Sicher?"}}]}}
            """);

        Assert.DoesNotContain("invalid", receipt);
        Assert.Contains("Artifact created", receipt);
    }

    [Fact]
    public void The_create_prompt_carries_the_actions_block()
    {
        var prompt = CanvasPrompt.CreatePrompt("brief", null, null, null, Actions().Descriptors);

        Assert.Contains("ACTIONS", prompt);
        Assert.Contains("order.approve(orderId: string, note?: string)", prompt);
    }

    [Fact]
    public void Without_registered_actions_the_prompt_is_unchanged()
    {
        Assert.DoesNotContain("ACTIONS", CanvasPrompt.CreatePrompt("brief", null, null, null, null));
    }
}
```

`CanvasPrompt` is `internal`; the Agents test project already reaches it (verify
`InternalsVisibleTo` in `DRYL.Components.Agents/DRYL.Components.Agents.csproj` and add
`<InternalsVisibleTo Include="DRYL.Components.Tests" />` if missing).

- [ ] **Step 2: Run the tests, confirm they fail**

Run: `dotnet test DRYL.slnx --filter FullyQualifiedName~CanvasActionReceiptTests`
Expected: FAIL — `CreatePrompt` has no actions parameter; `CreateReplay` has no actions parameter.

- [ ] **Step 3: Extend `CanvasPrompt`**

In `DRYL.Components.Agents/Canvas/CanvasPrompt.cs`:

In `SchemaText`, replace the button line with:

```
        - button { "label": string, "intent": string?, "kind": "primary"|"secondary"|"danger"? } — "intent" is a short
          machine-readable action id; clicking sends the intent plus all current input values back to you.
```

Add the actions parameter to both builders:

```csharp
    internal static string CreatePrompt(string brief, string? title, int? width = null,
                                       IReadOnlyList<CanvasDataDescriptor>? sources = null,
                                       IReadOnlyList<CanvasActionDescriptor>? actions = null) =>
        $"{SchemaText}\n{LayoutBudget(width)}{CanvasDataPrompt.Block(sources)}{CanvasActionPrompt.Block(actions)}\nBuild a new artifact{(title is null ? "" : $" titled \"{title}\"")} for this request:\n{brief}";

    internal static string UpdatePrompt(string brief, string currentSpecJson, int? width = null,
                                       IReadOnlyList<CanvasDataDescriptor>? sources = null,
                                       IReadOnlyList<CanvasActionDescriptor>? actions = null) =>
        SchemaText + LayoutBudget(width) + CanvasDataPrompt.Block(sources) + CanvasActionPrompt.Block(actions) + """
            …unchanged tail…
            """ + currentSpecJson + "\n\nRequest:\n" + brief;
```

Keep the `UpdatePrompt` raw string literal exactly as it is; only the block concatenation and the
signature change.

- [ ] **Step 4: Extend `DrylCanvasTools`**

In `DRYL.Components.Agents/Canvas/DrylCanvasTools.cs`:

Add the field and constructor parameter:

```csharp
    private readonly ICanvasActionService? _actions;
```

```csharp
    private DrylCanvasTools(
        DrylCanvasRun run, Func<string, CancellationToken, IAsyncEnumerable<string>> generate,
        ICanvasDataService? data, ICanvasActionService? actions)
    {
        _run = run;
        _generate = generate;
        _data = data;
        _actions = actions;
        // …existing tool creation unchanged…
    }
```

Update both factories:

```csharp
    /// <summary>Create the tools; <paramref name="generator"/> runs the artifact generations
    /// (a fresh session per call — generations are stateless, the current spec travels in the prompt).</summary>
    /// <param name="actions">Registered host actions the model may wire buttons to. It can place
    /// and label them; it can never trigger one.</param>
    public static DrylCanvasTools Create(DrylCanvasRun run, AIAgent generator,
                                        ICanvasDataService? data = null,
                                        ICanvasActionService? actions = null) =>
        new(run, LiveGenerate(generator), data, actions);

    /// <summary>Replay/demo/test seam: like <see cref="Create"/>, but generations come from
    /// <paramref name="generate"/> (prompt → raw JSON delta stream) instead of a live agent.</summary>
    public static DrylCanvasTools CreateReplay(
        DrylCanvasRun run, Func<string, CancellationToken, IAsyncEnumerable<string>> generate,
        ICanvasDataService? data = null, ICanvasActionService? actions = null) =>
        new(run, generate, data, actions);
```

Pass the descriptors into both prompt calls:

```csharp
                CanvasPrompt.CreatePrompt(brief, title, _run.AvailableWidth,
                                          _data?.Descriptors, _actions?.Descriptors), ct))
```

```csharp
                CanvasPrompt.UpdatePrompt(brief, current, _run.AvailableWidth,
                                          _data?.Descriptors, _actions?.Descriptors), ct))
```

Widen `ValidationContext` so it is built when **either** registry has entries:

```csharp
    /// <summary>The binding-validation context for one artifact, or null when neither data
    /// sources nor actions are registered — in which case validation stays exactly as it was (A2).</summary>
    private CanvasValidationContext? ValidationContext(CanvasNode? root) =>
        (_data?.Descriptors.Count ?? 0) == 0 && (_actions?.Descriptors.Count ?? 0) == 0
            ? null
            : new CanvasValidationContext
            {
                Sources = _data?.Descriptors ?? Array.Empty<CanvasDataDescriptor>(),
                Actions = _actions?.Descriptors ?? Array.Empty<CanvasActionDescriptor>(),
                FieldNames = CanvasValidationContext.FieldNamesOf(root),
            };
```

In `UpdateArtifactImpl`, replace the `if (_data is not null && …)` guard so the post-patch walk also
runs for actions:

```csharp
            var problems = new List<string>();
            if (ValidationContext(_run.Spec?.Root) is { } context && _run.Spec?.Root is { } root)
            {
                Walk(root, n =>
                {
                    if (CanvasCatalog.Validate(n, context) is { } e) problems.Add(e);
                });
            }
```

and widen the receipt sentence:

```csharp
            if (problems.Count > 0)
                receipt += " Some bindings are invalid and render as placeholders — fix via "
                         + "update_artifact: " + string.Join(" ", problems.Take(3));
```

- [ ] **Step 5: Pass `OnAction` through `DrylAiCanvas`**

In `DRYL.Components.Agents/Canvas/DrylAiCanvas.razor`, add the parameter:

```csharp
    /// <summary>Raised after every completed action button in the artifact — successful or not.</summary>
    [Parameter] public EventCallback<CanvasActionOutcome> OnAction { get; set; }
```

and the attribute on the wrapped `<DrylCanvas …>`, right after `OnInteraction`:

```razor
            OnAction="OnAction"
```

- [ ] **Step 6: Run the tests, confirm green**

Run: `dotnet test DRYL.slnx`
Expected: PASS — the whole suite, core and Agents.

- [ ] **Step 7: Commit**

```bash
git add DRYL.Components.Agents/Canvas/ tests/DRYL.Components.Tests/Agents/Canvas/CanvasActionReceiptTests.cs
git commit -m "feat(canvas): teach the model about host actions and validate them in the receipt

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>"
```

---

### Task 8: Demo, catalog, changelog, versions

**Files:**
- Create: `DRYL.Website/Components/Examples/Canvas/CanvasActions.razor`
- Modify: `DRYL.Website/Components/Pages/DemoCanvas.razor`, `DRYL.Website/Components/ComponentCatalog.cs`, `DRYL.Website/Program.cs`
- Modify: `CHANGELOG.md`, `DRYL.Components/DRYL.Components.csproj`, `DRYL.Components.Agents/DRYL.Components.Agents.csproj`

**Interfaces:**
- Consumes: everything from Tasks 1–7.
- Produces: nothing further tasks depend on.

- [ ] **Step 1: Register demo actions in the website**

In `DRYL.Website/Program.cs`, next to the existing `AddDrylCanvasDataSource` demo registrations,
add an in-memory order board so the demo needs no model and no database:

```csharp
// ── Canvas actions demo (Phase 2) ──────────────────────────────────────────
builder.Services.AddSingleton<DemoOrderBoard>();

builder.Services.AddDrylCanvasDataSource("demo.orders", "Offene Aufträge mit Status.",
    (CanvasDataContext ctx, CancellationToken ct) =>
        Task.FromResult(ctx.Services.GetRequiredService<DemoOrderBoard>().Rows()));

builder.Services.AddDrylCanvasAction("demo.order.approve",
    "Gibt einen Auftrag frei.",
    (DemoApproveArgs a, CanvasActionContext ctx, CancellationToken ct) =>
    {
        var board = ctx.Services.GetRequiredService<DemoOrderBoard>();
        return Task.FromResult(board.Approve(a.OrderId)
            ? CanvasActionResult.Ok($"Auftrag {a.OrderId} freigegeben.").Refresh("demo.orders")
            : CanvasActionResult.Fail($"Auftrag {a.OrderId} ist bereits freigegeben."));
    });

builder.Services.AddDrylCanvasAction("demo.orders.reset", "Setzt das Demo-Board zurück.",
    (CanvasActionContext ctx, CancellationToken ct) =>
    {
        ctx.Services.GetRequiredService<DemoOrderBoard>().Reset();
        return Task.FromResult(CanvasActionResult.Ok("Board zurückgesetzt.").Refresh("demo.orders"));
    });
```

with the supporting types in the same file (or a small `Demo/DemoOrderBoard.cs`):

```csharp
public sealed record DemoApproveArgs(string OrderId);

public sealed class DemoOrderBoard
{
    private readonly Dictionary<string, bool> _approved = new()
    {
        ["4711"] = false, ["4712"] = false, ["4713"] = true,
    };

    public bool Approve(string id)
    {
        if (!_approved.TryGetValue(id, out var done) || done) return false;
        _approved[id] = true;
        return true;
    }

    public void Reset()
    {
        foreach (var key in _approved.Keys.ToList()) _approved[key] = key == "4713";
    }

    public CanvasRowData Rows() => CanvasData.Rows(
        new[] { "Auftrag", "Status" },
        _approved.Select(kv => new[] { kv.Key, kv.Value ? "Freigegeben" : "Offen" }));
}
```

`DemoOrderBoard` is a singleton on purpose: the demo must show a refresh actually changing a value,
which needs the state to survive the action's own request.

- [ ] **Step 2: Write the demo example**

Create `DRYL.Website/Components/Examples/Canvas/CanvasActions.razor` following the structure of the
existing `Components/Examples/Canvas/CanvasDataBinding.razor` (read it first — it is the template
for how a canvas example is embedded). It renders a `DrylCanvas` over a hand-written spec:

```razor
<DrylCanvas Spec="_spec" OnAction="LogOutcome" />

@if (_last is not null)
{
    <p class="muted">Letzte Aktion: @_last.Action — @(_last.Succeeded ? "ok" : "fehlgeschlagen")</p>
}

@code {
    private CanvasActionOutcome? _last;

    private void LogOutcome(CanvasActionOutcome outcome)
    {
        _last = outcome;
        StateHasChanged();
    }

    private readonly CanvasSpec _spec = JsonSerializer.Deserialize<CanvasSpec>("""
        {
          "title": "Auftragsfreigabe",
          "root": { "id": "root", "type": "stack", "props": { "gap": "md" }, "children": [
            { "id": "orders", "type": "table",
              "props": { "columns": ["Auftrag", "Status"] },
              "data": { "source": "demo.orders" } },
            { "id": "pick", "type": "select",
              "props": { "name": "order", "label": "Auftrag", "options": ["4711", "4712", "4713"], "value": "4711" } },
            { "id": "approve", "type": "button",
              "props": { "label": "Freigeben", "kind": "danger" },
              "action": { "name": "demo.order.approve",
                          "args": { "orderId": { "$field": "order" } },
                          "confirm": "Diesen Auftrag wirklich freigeben?" } },
            { "id": "reset", "type": "button",
              "props": { "label": "Zurücksetzen", "kind": "secondary" },
              "action": { "name": "demo.orders.reset" } }
          ] }
        }
        """, CanvasJson.Options)!;
}
```

The example demonstrates every part of the phase in one screen: a `$field` argument, a confirm
dialog, a success toast, a failed action's inline error (approve 4713, which is already approved)
and the table refreshing through the Phase 1 binder.

- [ ] **Step 3: Register the example on the demo page and in the catalog**

Add the example to `DRYL.Website/Components/Pages/DemoCanvas.razor` next to the data-binding one,
following that page's existing `DemoExample` usage. In
`DRYL.Website/Components/ComponentCatalog.cs`, extend the `DrylCanvas` and `DrylAiCanvas`
descriptions to mention actions (no new catalog entry — no new component ships).

- [ ] **Step 4: Verify the demo by hand**

Run the site, open the canvas demo page and check, writing down the result of each:

1. „Freigeben" mit 4711 → Dialog erscheint, Bestätigen → Toast, Tabellenzeile wechselt auf
   „Freigegeben" **mit Pulse, ohne Skeleton**.
2. „Freigeben" mit 4713 → Inline-Fehler unter dem Button, kein Toast.
3. Dialog abbrechen → nichts passiert, kein Fehler.
4. Beide Farbmodi (`data-dryl-mode` auf `<html>` umschalten).
5. 375 px Breite.
6. `prefers-reduced-motion: reduce` — Button, Dialog und Pulse bleiben benutzbar.

Consult the `verify` skill for how to launch and drive the site.

- [ ] **Step 5: Changelog and versions**

In `CHANGELOG.md`, under `[Unreleased]`:

```markdown
### Added
- `DrylCanvas` / `DrylAiCanvas` — **Canvas Actions**: a button node binds to a registered host command via
  `"action": { "name", "args", "confirm" }`. Register with `AddDrylCanvasAction(name, description, handler)`;
  arguments are a C# record, `{ "$field": "…" }` reads a live form value. The handler returns a
  `CanvasActionResult` that can carry a success toast, a refresh list, patch ops and an optional `AskAi(…)`.
  The AI authors and labels the button — only the user presses it.
- `DrylCanvas` — New `OnAction` parameter reporting every completed action (`CanvasActionOutcome`).
- `CanvasInteraction` — New optional `Message`; `ToPromptMessage()` returns it verbatim, which is how an
  action's `AskAi(…)` reaches an existing chat wiring unchanged.
- Canvas catalog — `button` accepts `"kind": "danger"`, and may omit `intent` when it carries an `action`.
- `ICanvasDataService` — New `Invalidate(CanvasInvalidation)` overload.
```

Cut the release: rename `[Unreleased]` to `## [2.13.0] - 2026-07-25` (core) with the matching
Agents entry `## [0.11.0] - 2026-07-25`, following the file's existing two-package layout exactly,
and start a fresh empty `[Unreleased]` above.

Bump `<Version>` to `2.13.0` in `DRYL.Components/DRYL.Components.csproj` and to `0.11.0` in
`DRYL.Components.Agents/DRYL.Components.Agents.csproj`.

- [ ] **Step 6: Full build, full test, light-sync guard**

Run:
```bash
dotnet build DRYL.slnx
dotnet test DRYL.slnx
node scripts/check-light-sync.mjs
```
Expected: build clean, all tests pass, light-sync green (no new tokens were added, so it must be
unaffected).

- [ ] **Step 7: Commit**

```bash
git add -A
git commit -m "feat(canvas): actions demo, catalog copy, 2.13.0 / 0.11.0

Co-Authored-By: Claude Opus 5 <noreply@anthropic.com>"
```

- [ ] **Step 8: Update the project memory**

Append the phase to `C:\Users\janzi\.claude\projects\c--Users-janzi-Desktop-DRYL-DRYL-Components\memory\project_canvas_platform.md`:
Phase 2 done, what shipped (registry / runner / `CanvasArgs` / `OnAction` / `CanvasInteraction.Message`),
the four decisions E1–E4 and the two gotchas worth remembering (`confirm` refuses without a dialog
service; the `$field` resolver is shared and must stay shared).
