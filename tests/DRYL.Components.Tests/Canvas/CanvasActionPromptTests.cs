using System.Text.Json;
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

    private static IReadOnlyList<CanvasActionDescriptor> Descriptors(ServiceProvider sp) =>
        sp.GetRequiredService<ICanvasActionService>().Descriptors;

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

        var block = CanvasActionPrompt.Block(Descriptors(sp));

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

        Assert.Contains("NEVER trigger", CanvasActionPrompt.Block(Descriptors(sp)));
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

        var args = JsonDocument.Parse("""{"orderId":"4711"}""").RootElement.Clone();
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
    public async Task The_handler_sees_the_full_form_snapshot_and_its_node_id()
    {
        string? note = null;
        string? nodeId = null;
        var sp = Provider(s => s.AddDrylCanvasAction("a", "…",
            (CanvasActionContext c, CancellationToken t) =>
            {
                note = c.Get<string>("note");
                nodeId = c.NodeId;
                return Task.FromResult(CanvasActionResult.Ok());
            }));

        await sp.GetRequiredService<ICanvasActionService>().InvokeAsync(
            "a", null, "btn",
            new Dictionary<string, object?> { ["note"] = "hi" }, CancellationToken.None);

        Assert.Equal("hi", note);
        Assert.Equal("btn", nodeId);
    }
}
