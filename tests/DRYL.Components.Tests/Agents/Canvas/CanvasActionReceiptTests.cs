using DRYL.Components.Agents;
using DRYL.Components.Canvas;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DRYL.Components.Tests.Agents.Canvas;

/// <summary>
/// The model contract for canvas actions, driven through the real tools with a replayed
/// generation. Every mistake must come back as one corrective sentence in the receipt — and the
/// artifact must render anyway (the invalid node becomes a placeholder).
/// </summary>
public class CanvasActionReceiptTests
{
    public sealed record ApproveArgs(string OrderId, string? Note = null);

    private static ICanvasActionService Actions()
    {
        var services = new ServiceCollection();
        services.AddDrylComponents();
        services.AddDrylCanvasAction("order.approve", "Gibt einen Auftrag frei.",
            (ApproveArgs _, CanvasActionContext _, CancellationToken _) =>
                Task.FromResult(CanvasActionResult.Ok()));
        services.AddDrylCanvasAction("cache.clear", "Leert den Cache.",
            (CanvasActionContext _, CancellationToken _) => Task.FromResult(CanvasActionResult.Ok()));
        return services.BuildServiceProvider().CreateScope().ServiceProvider
            .GetRequiredService<ICanvasActionService>();
    }

    private static async IAsyncEnumerable<string> Script(string json)
    {
        await Task.Yield();
        yield return json;
    }

    private static async Task<(string Receipt, string Prompt)> CreateAsync(
        string specJson, ICanvasActionService? actions = null)
    {
        string? prompt = null;
        var tools = DrylCanvasTools.CreateReplay(new DrylCanvasRun(),
            (p, _) => { prompt = p; return Script(specJson); }, null, actions ?? Actions());
        var result = await ((AIFunction)tools.CreateArtifact)
            .InvokeAsync(new AIFunctionArguments(new Dictionary<string, object?> { ["brief"] = "b" }!));
        return (result?.ToString() ?? string.Empty, prompt ?? string.Empty);
    }

    [Fact]
    public async Task The_generation_prompt_carries_the_action_catalog()
    {
        var (_, prompt) = await CreateAsync("""
            {"title":"T","root":{"id":"b","type":"button","props":{"label":"Los"},
             "action":{"name":"cache.clear"}}}
            """);

        Assert.Contains("ACTIONS", prompt);
        Assert.Contains("order.approve(orderId: string, note?: string)", prompt);
        Assert.Contains("cache.clear()", prompt);
        Assert.Contains("NEVER trigger", prompt);
    }

    [Fact]
    public async Task Without_registered_actions_the_prompt_is_unchanged()
    {
        var (_, prompt) = await CreateAsync(
            """{"title":"T","root":{"id":"b","type":"button","props":{"label":"Los","intent":"go"}}}""",
            NoActions());

        Assert.DoesNotContain("ACTIONS", prompt);
    }

    [Fact]
    public async Task An_unknown_action_comes_back_as_a_corrective_sentence()
    {
        var (receipt, _) = await CreateAsync("""
            {"title":"x","root":{"id":"b","type":"button","props":{"label":"Los"},
             "action":{"name":"order.nope","args":{}}}}
            """);

        Assert.Contains("unknown action 'order.nope'", receipt);
        Assert.Contains("order.approve", receipt);
    }

    [Fact]
    public async Task A_missing_required_arg_comes_back_as_a_corrective_sentence()
    {
        var (receipt, _) = await CreateAsync("""
            {"title":"x","root":{"id":"b","type":"button","props":{"label":"Los"},
             "action":{"name":"order.approve","args":{}}}}
            """);

        Assert.Contains("missing required arg", receipt);
        Assert.Contains("orderId", receipt);
    }

    [Fact]
    public async Task An_unknown_arg_comes_back_as_a_corrective_sentence()
    {
        var (receipt, _) = await CreateAsync("""
            {"title":"x","root":{"id":"b","type":"button","props":{"label":"Los"},
             "action":{"name":"order.approve","args":{"orderId":"1","nope":2}}}}
            """);

        Assert.Contains("no argument 'nope'", receipt);
    }

    [Fact]
    public async Task A_dangling_field_reference_comes_back_as_a_corrective_sentence()
    {
        var (receipt, _) = await CreateAsync("""
            {"title":"x","root":{"id":"b","type":"button","props":{"label":"Los"},
             "action":{"name":"order.approve","args":{"orderId":{"$field":"nope"}}}}}
            """);

        Assert.Contains("references field 'nope'", receipt);
    }

    [Fact]
    public async Task An_action_on_a_non_button_comes_back_as_a_corrective_sentence()
    {
        var (receipt, _) = await CreateAsync("""
            {"title":"x","root":{"id":"s","type":"stat","props":{"label":"Umsatz","value":"10k"},
             "action":{"name":"order.approve","args":{"orderId":"1"}}}}
            """);

        Assert.Contains("can only sit on a button", receipt);
    }

    [Fact]
    public async Task A_button_without_intent_and_without_action_comes_back_as_a_corrective_sentence()
    {
        var (receipt, _) = await CreateAsync(
            """{"title":"x","root":{"id":"b","type":"button","props":{"label":"Los"}}}""");

        Assert.Contains("intent or an action", receipt);
    }

    [Fact]
    public async Task A_valid_action_button_produces_a_clean_receipt()
    {
        var (receipt, _) = await CreateAsync("""
            {"title":"x","root":{"id":"r","type":"stack","children":[
              {"id":"pick","type":"select","props":{"name":"order","label":"Auftrag","options":["4711"]}},
              {"id":"b","type":"button","props":{"label":"Freigeben","kind":"danger"},
               "action":{"name":"order.approve","args":{"orderId":{"$field":"order"}},
                         "confirm":"Sicher?"}}]}}
            """);

        Assert.DoesNotContain("invalid", receipt);
        Assert.Contains("Artifact created", receipt);
    }

    private static ICanvasActionService NoActions()
    {
        var services = new ServiceCollection();
        services.AddDrylComponents();
        services.AddDrylCanvasAction("placeholder", "…",
            (CanvasActionContext _, CancellationToken _) => Task.FromResult(CanvasActionResult.Ok()));
        var sp = services.BuildServiceProvider();
        // Build a service over an empty registry: the "nothing registered" contract must be
        // provably unchanged, not merely assumed.
        return new EmptyActionService(sp.CreateScope().ServiceProvider);
    }

    private sealed class EmptyActionService : ICanvasActionService
    {
        private readonly IServiceProvider _sp;
        public EmptyActionService(IServiceProvider sp) => _sp = sp;
        public IReadOnlyList<CanvasActionDescriptor> Descriptors => Array.Empty<CanvasActionDescriptor>();
        public Task<CanvasActionResult> InvokeAsync(string name, System.Text.Json.JsonElement? args,
            string nodeId, IReadOnlyDictionary<string, object?> values, CancellationToken ct) =>
            throw new InvalidOperationException($"No canvas action named '{name}' is registered.");
    }
}
