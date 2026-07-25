using System.Text.Json;
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
                throw new InvalidOperationException("boom")));

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
        string? seenId = null;
        string? seenNote = null;
        var (runner, form, _) = Build(s => s.AddDrylCanvasAction("order.approve", "…",
            (ApproveArgs a, CanvasActionContext c, CancellationToken t) =>
            {
                seenId = a.OrderId;
                seenNote = a.Note;
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
    public async Task A_parameterised_refresh_keeps_its_canonical_key()
    {
        var seen = new List<CanvasInvalidation>();
        var (runner, _, sp) = Build(s =>
        {
            s.AddDrylCanvasDataSource("sales", "…",
                (CanvasDataContext c, CancellationToken t) =>
                    Task.FromResult(CanvasData.Rows(new[] { "a" }, Array.Empty<string[]>())));
            s.AddDrylCanvasAction("a", "…",
                (CanvasActionContext c, CancellationToken t) =>
                    Task.FromResult(CanvasActionResult.Ok().Refresh("sales", new { year = 2026 })));
        });
        sp.GetRequiredService<ICanvasDataService>().Invalidated += n => seen.Add(n);

        await runner.InvokeAsync("btn", "X", new CanvasActionBinding { Name = "a" });

        Assert.Contains("2026", Assert.Single(seen).ParamsKey);
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
    public async Task A_failed_action_applies_neither_ops_nor_refreshes()
    {
        var applied = 0;
        var (runner, _, _) = Build(s => s.AddDrylCanvasAction("a", "…",
            (CanvasActionContext c, CancellationToken t) =>
                Task.FromResult(CanvasActionResult.Fail("nope")
                    .Patch(new CanvasOp { Op = "setProps", Id = "x" }))));
        runner.Patch = _ => { applied++; return null; };

        await runner.InvokeAsync("btn", "X", new CanvasActionBinding { Name = "a" });

        Assert.Equal(0, applied);
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
            o => { Assert.True(o.Succeeded); Assert.Equal("b1", o.NodeId); Assert.Equal("ok", o.Action); },
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
            (CanvasActionContext c, CancellationToken t) =>
            {
                calls++;
                return Task.FromResult(CanvasActionResult.Ok());
            }));
        ((StubDialogService)sp.GetRequiredService<IDrylDialogService>()).Confirm = false;

        await runner.InvokeAsync("btn", "Freigeben",
            new CanvasActionBinding { Name = "a", Confirm = "Wirklich?" });

        Assert.Equal(0, calls);
        Assert.Null(runner.StateOf("btn")?.Error);     // a cancellation is not a failure
    }

    [Fact]
    public async Task An_accepted_confirmation_runs_the_handler_and_titles_the_dialog_with_the_label()
    {
        var calls = 0;
        var (runner, _, sp) = Build(s => s.AddDrylCanvasAction("a", "…",
            (CanvasActionContext c, CancellationToken t) =>
            {
                calls++;
                return Task.FromResult(CanvasActionResult.Ok());
            }));

        await runner.InvokeAsync("btn", "Freigeben",
            new CanvasActionBinding { Name = "a", Confirm = "Wirklich?" });

        Assert.Equal(1, calls);
        var asked = Assert.Single(((StubDialogService)sp.GetRequiredService<IDrylDialogService>()).Asked);
        Assert.Equal("Freigeben", asked.Title);
        Assert.Equal("Wirklich?", asked.Message);
    }

    // A deliberately confirmation-gated action must never run unconfirmed just because the host
    // forgot the dialog provider.
    [Fact]
    public async Task Without_a_dialog_service_a_confirm_action_refuses_to_run()
    {
        var calls = 0;
        var (runner, _, _) = Build(s => s.AddDrylCanvasAction("a", "…",
                (CanvasActionContext c, CancellationToken t) =>
                {
                    calls++;
                    return Task.FromResult(CanvasActionResult.Ok());
                }),
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
                (CanvasActionContext c, CancellationToken t) =>
                    Task.FromResult(CanvasActionResult.Ok("done"))),
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

    [Fact]
    public async Task A_binding_without_a_name_does_nothing()
    {
        var (runner, _, _) = Build(s => s.AddDrylCanvasAction("a", "…",
            (CanvasActionContext c, CancellationToken t) => Task.FromResult(CanvasActionResult.Ok())));

        await runner.InvokeAsync("btn", "X", new CanvasActionBinding());

        Assert.Null(runner.StateOf("btn"));
    }
}
