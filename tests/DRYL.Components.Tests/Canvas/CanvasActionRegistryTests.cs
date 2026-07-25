using DRYL.Components.Canvas;
using Microsoft.Extensions.DependencyInjection;

namespace DRYL.Components.Tests.Canvas;

public sealed record ApproveArgs(string OrderId, string? Note = null);
public sealed record UnsupportedArgs(TimeSpan Window);

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
                (UnsupportedArgs a, CanvasActionContext ctx, CancellationToken ct) =>
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
    public void Actions_and_data_sources_live_in_separate_registries()
    {
        var services = new ServiceCollection();
        services.AddDrylCanvasDataSource("a", "…",
            (CanvasDataContext c, CancellationToken t) =>
                Task.FromResult(CanvasData.Rows(new[] { "x" }, Array.Empty<string[]>())));
        services.AddDrylCanvasAction("a", "…",
            (CanvasActionContext c, CancellationToken t) => Task.FromResult(CanvasActionResult.Ok()));

        var sp = services.BuildServiceProvider();

        Assert.Single(sp.GetRequiredService<CanvasDataRegistry>().Descriptors);
        Assert.Single(sp.GetRequiredService<CanvasActionRegistry>().Descriptors);
    }

    [Fact]
    public void The_result_builder_collects_refreshes_ops_and_ask()
    {
        var result = CanvasActionResult.Ok("done")
            .Refresh("orders.open", "orders.count")
            .Refresh("sales.byMonth", new { year = 2026 })
            .Patch(new CanvasOp { Op = "setProps", Id = "b" })
            .AskAi("Auftrag 4711 wurde freigegeben.");

        Assert.True(result.Succeeded);
        Assert.Equal("done", result.Message);
        Assert.Equal(3, result.Refreshes.Count);
        Assert.Null(result.Refreshes[0].ParamsKey);      // whole-source refresh
        Assert.NotNull(result.Refreshes[2].ParamsKey);   // parameterised refresh
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

    // AskAi is opt-in: the AI reacts to an action, it never causes one (A4).
    [Fact]
    public void Ask_is_null_by_default()
    {
        Assert.Null(CanvasActionResult.Ok("done").Ask);
    }
}
